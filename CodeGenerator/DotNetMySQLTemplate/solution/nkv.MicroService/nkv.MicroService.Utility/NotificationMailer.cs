using Microsoft.Extensions.Configuration;
using System.Data.Common;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Sends the email part of notifications (rows with email_status 'pending'), up to 5 attempts each. Off unless configured,
    /// with secrets from the environment, never from files. Either a relay (NeoApps Cloud sets these; the app holds no email key):
    ///   Notifications__Relay__Url / Notifications__Relay__Token   POST {to, title, path}; the relay owns sender and template
    /// or the app's own sender:
    ///   Notifications__Email__From             sender address (required)
    ///   Notifications__Email__AcsConnectionString  Azure Communication Services ("endpoint=...;accesskey=...") or
    ///   Notifications__Email__SmtpHost / SmtpPort (587) / SmtpUser / SmtpPassword
    ///   Notifications__AppUrl                  e.g. https://myapp.example.com, for the "Open" link
    /// </summary>
    public static class NotificationMailer
    {
        static Timer _timer;
        static int _busy;
        static string _from, _acs, _smtpHost, _smtpUser, _smtpPassword, _appUrl, _relayUrl, _relayToken;
        static int _smtpPort = 587;
        static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
        public static bool Enabled { get; private set; }
        public static int Sent, Failed;

        public static void Start(IConfiguration cfg)
        {
            _from = cfg?["Notifications:Email:From"];
            _acs = cfg?["Notifications:Email:AcsConnectionString"];
            _smtpHost = cfg?["Notifications:Email:SmtpHost"];
            _smtpUser = cfg?["Notifications:Email:SmtpUser"];
            _smtpPassword = cfg?["Notifications:Email:SmtpPassword"];
            if (int.TryParse(cfg?["Notifications:Email:SmtpPort"], out var port)) _smtpPort = port;
            _appUrl = cfg?["Notifications:AppUrl"]?.TrimEnd('/');
            _relayUrl = cfg?["Notifications:Relay:Url"];
            _relayToken = cfg?["Notifications:Relay:Token"];
            var relay = !string.IsNullOrEmpty(_relayUrl) && !string.IsNullOrEmpty(_relayToken);
            if (!relay) _relayUrl = null;
            Enabled = relay || (!string.IsNullOrEmpty(_from) && (!string.IsNullOrEmpty(_acs) || !string.IsNullOrEmpty(_smtpHost)));
            if (!Enabled || _timer != null || _wake != null) return;
            if (Db.PauseFriendly)
            {
                // send after notifications are written (Wake), plus once now for anything left from before a restart
                _wake = new WakeTimer(() => { SendPending(); if (_retry) Wake(TimeSpan.FromSeconds(60)); });
                _wake.Wake(TimeSpan.FromSeconds(10));
            }
            else _timer = new Timer(_ => SendPending(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            Console.WriteLine($"Notifications: email on via {(_relayUrl != null ? "the NeoApps relay" : string.IsNullOrEmpty(_acs) ? "SMTP" : "Azure Communication Services")}.");
        }

        static WakeTimer _wake;
        static volatile bool _retry;

        /// <summary>New email-pending notifications were written (serverless: no polling).</summary>
        public static void Wake(TimeSpan? after = null) => _wake?.Wake(after);

        public static int SendPending()
        {
            _retry = false;
            if (!Enabled || Interlocked.Exchange(ref _busy, 1) == 1) return 0;
            var sent = 0;
            try
            {
                using var c = Db.Connection(DataGuard.ConnectionString);
                c.Open();
                var rows = new List<(long id, string title, string table, string record, string to)>();
                using (var q = Db.Command($@"SELECT {Db.Top(50)}n.notification_id, n.title, n.table_name, n.record_id, s.email FROM user_notifications n
                    JOIN user_notification_settings s ON s.user_id = n.user_id
                    WHERE n.email_status = 'pending' AND s.email_on = 1 AND s.email IS NOT NULL ORDER BY n.notification_id{Db.LimitEnd(50)}", c))
                using (var r = q.ExecuteReader())
                    while (r.Read()) rows.Add((r.GetInt64(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4)));
                if (rows.Count == 50) _retry = true; // more waiting
                foreach (var row in rows)
                {
                    string error = null;
                    try
                    {
                        if (_relayUrl != null) Relay(row.to, row.title, row.table, row.record);
                        else Send(row.to, row.title, Body(row.title, row.table, row.record));
                        sent++; Sent++;
                    }
                    catch (Exception ex) { error = ex.Message.Length > 450 ? ex.Message[..450] : ex.Message; Failed++; _retry = true; }
                    using var u = Db.Command(error == null
                        ? "UPDATE user_notifications SET email_status = 'sent', email_error = NULL WHERE notification_id = @id"
                        : Db.IsSqlServer // SQL Server evaluates every SET from the old row, MySQL left to right
                            ? "UPDATE user_notifications SET email_attempts = email_attempts + 1, email_error = @e, email_status = CASE WHEN email_attempts + 1 >= 5 THEN 'failed' ELSE 'pending' END WHERE notification_id = @id"
                            : "UPDATE user_notifications SET email_attempts = email_attempts + 1, email_error = @e, email_status = IF(email_attempts >= 5, 'failed', 'pending') WHERE notification_id = @id", c);
                    u.Parameters.AddWithValue("@id", row.id);
                    u.Parameters.AddWithValue("@e", error);
                    u.ExecuteNonQuery();
                }
                // people who turned email off since: don't keep their rows pending forever
                using (var off = Db.Command(@"UPDATE user_notifications SET email_status = 'none' WHERE email_status = 'pending'
                    AND NOT EXISTS (SELECT 1 FROM user_notification_settings s WHERE s.user_id = user_notifications.user_id
                                    AND s.email_on = 1 AND s.email IS NOT NULL AND s.email <> '')", c))
                    off.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("Notifications: email run failed, will retry: " + ex.Message); }
            finally { Interlocked.Exchange(ref _busy, 0); }
            return sent;
        }

        static string Body(string title, string table, string record)
        {
            var link = _appUrl != null && table != null && record != null ? $"{_appUrl}/r/{Uri.EscapeDataString(table)}/{Uri.EscapeDataString(record)}" : null;
            var t = WebUtility.HtmlEncode(title);
            return $"<p style=\"font:15px sans-serif\">{t}</p>" + (link == null ? "" : $"<p style=\"font:15px sans-serif\"><a href=\"{link}\">Open it</a></p>")
                 + "<p style=\"font:12px sans-serif;color:#666\">You get this because email notifications are on. Turn them off from the bell in the app.</p>";
        }

        // ------------------------------------------------------------------ sign-in emails (sent at once, not queued)

        /// <summary>True when the app can send sign-in emails (the NeoApps relay, or its own sender with Notifications:AppUrl).</summary>
        public static bool CanSendSignIn => Enabled && (_relayUrl != null || _appUrl != null);

        /// <summary>"X invited you to App" with the one-time join link (path "/join/&lt;token&gt;"). False when it couldn't be sent.</summary>
        public static bool SendInvite(string to, string inviter, string role, string joinPath, string appName)
        {
            if (!CanSendSignIn || !System.Text.RegularExpressions.Regex.IsMatch(joinPath ?? "", @"^/join/\d+\.[A-Za-z0-9_-]{20,64}$")) return false;
            if (_relayUrl != null) return RelayAccess(new { kind = "invite", to, inviter, role, path = joinPath });
            var link = _appUrl + joinPath;
            var e = (Func<string, string>)WebUtility.HtmlEncode;
            return SendNow(to, $"{inviter} invited you to {appName}",
                $"<p style=\"font:15px sans-serif\">{e(inviter)} invited you to <b>{e(appName)}</b> as {e(role)}.</p>"
                + $"<p style=\"font:15px sans-serif\"><a href=\"{e(link)}\">Join {e(appName)}</a></p>"
                + "<p style=\"font:12px sans-serif;color:#666\">The link works once. If you didn't expect this, ignore this email.</p>");
        }

        /// <summary>A 6-digit sign-in code. False when it couldn't be sent.</summary>
        public static bool SendCode(string to, string code, string appName)
        {
            if (!CanSendSignIn || !System.Text.RegularExpressions.Regex.IsMatch(code ?? "", @"^\d{6}$")) return false;
            if (_relayUrl != null) return RelayAccess(new { kind = "code", to, code });
            return SendNow(to, $"{code} is your {appName} sign-in code",
                $"<p style=\"font:15px sans-serif\">Your sign-in code for <b>{WebUtility.HtmlEncode(appName)}</b>:</p>"
                + $"<p style=\"font:28px monospace;letter-spacing:4px\">{code}</p>"
                + "<p style=\"font:12px sans-serif;color:#666\">It works for 10 minutes. If you didn't ask for it, ignore this email.</p>");
        }

        static bool RelayAccess(object body)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, _relayUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
                };
                req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _relayToken);
                using var res = Http.Send(req);
                if (!res.IsSuccessStatusCode) Console.WriteLine($"Sign-in email: relay answered {(int)res.StatusCode}");
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex) { Console.WriteLine("Sign-in email failed: " + ex.Message); return false; }
        }

        static bool SendNow(string to, string subject, string html)
        {
            try { Send(to, subject, html); return true; }
            catch (Exception ex) { Console.WriteLine("Sign-in email failed: " + ex.Message); return false; }
        }

        /// <summary>Hands the email to the relay: only the recipient, the title and the record's path inside the app.</summary>
        static void Relay(string to, string title, string table, string record)
        {
            var path = table != null && record != null && System.Text.RegularExpressions.Regex.IsMatch(table, "^[A-Za-z0-9_]{1,64}$")
                && System.Text.RegularExpressions.Regex.IsMatch(record, "^[A-Za-z0-9_-]{1,64}$") ? $"/r/{table}/{record}" : null;
            using var req = new HttpRequestMessage(HttpMethod.Post, _relayUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { to, title, path }), Encoding.UTF8, "application/json"),
            };
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _relayToken);
            using var res = Http.Send(req);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Relay {(int)res.StatusCode}: {res.Content.ReadAsStringAsync().Result}");
        }

        static void Send(string to, string subject, string html)
        {
            if (!string.IsNullOrEmpty(_acs)) { SendAcs(to, subject, html); return; }
            using var smtp = new SmtpClient(_smtpHost, _smtpPort) { EnableSsl = true };
            if (!string.IsNullOrEmpty(_smtpUser)) smtp.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
            using var m = new MailMessage(_from, to, subject, html) { IsBodyHtml = true };
            smtp.Send(m);
        }

        /// <summary>Azure Communication Services email REST API, HMAC-SHA256 signed with the access key.</summary>
        static void SendAcs(string to, string subject, string html)
        {
            string endpoint = null, key = null;
            foreach (var part in _acs.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var i = part.IndexOf('=');
                if (i < 0) continue;
                var (k, v) = (part[..i].Trim().ToLowerInvariant(), part[(i + 1)..].Trim());
                if (k == "endpoint") endpoint = v.TrimEnd('/'); else if (k == "accesskey") key = v;
            }
            if (endpoint == null || key == null) throw new InvalidOperationException("AcsConnectionString needs endpoint and accesskey.");
            var uri = new Uri(endpoint + "/emails:send?api-version=2023-03-31");
            var body = JsonSerializer.Serialize(new
            {
                senderAddress = _from,
                recipients = new { to = new[] { new { address = to } } },
                content = new { subject, html },
            });
            var date = DateTime.UtcNow.ToString("R");
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
            var toSign = $"POST\n{uri.PathAndQuery}\n{date};{uri.Authority};{hash}";
            var sig = Convert.ToBase64String(new HMACSHA256(Convert.FromBase64String(key)).ComputeHash(Encoding.UTF8.GetBytes(toSign)));
            using var req = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            req.Headers.Add("x-ms-date", date);
            req.Headers.Add("x-ms-content-sha256", hash);
            req.Headers.TryAddWithoutValidation("Authorization", $"HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={sig}");
            using var res = Http.Send(req);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Azure email {(int)res.StatusCode}: {res.Content.ReadAsStringAsync().Result}");
        }
    }
}
