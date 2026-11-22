using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Sign in with an emailed code (no password), like Google Docs or Slack: the person types their email, gets a 6-digit
    /// code that works once for 10 minutes (5 tries), and is signed in. Members have a sign-in email when they joined
    /// through an email invite, or when an administrator set one in Members. Answers never say whether an email belongs
    /// to a member. Off when the app can't send email (NeoApps Cloud apps can, through the NeoApps relay).
    /// </summary>
    public partial class AccessController
    {
        private const string PublicCodeRoute = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/public/email-code";
        private static readonly Regex EmailPattern = new(@"^[^@\s<>""',;]+@[^@\s<>""',;]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);
        private const int CodeMinutes = 10, CodeTries = 5, CodesPerHour = 5;

        private static bool _emailReady;
        private static void EnsureEmailTables(DbConnection c)
        {
            if (_emailReady) return;
            using (var a = Db.Command(Db.CreateTableIfMissing("user_emails", @"user_id INT NOT NULL PRIMARY KEY, email VARCHAR(254) NOT NULL,
                verified_at DATETIME NULL, createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP"), c)) Db.Execute(a);
            using (var b = Db.Command(Db.CreateTableIfMissing("login_codes", $@"code_id {Db.BigAutoId}, email_hash CHAR(64) NOT NULL,
                code_hash CHAR(64) NOT NULL, expires_at DATETIME NOT NULL, attempts INT NOT NULL DEFAULT 0, used {Db.BoolType} NOT NULL DEFAULT 0,
                createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP"), c)) Db.Execute(b);
            _emailReady = true;
        }

        private static string NormalEmail(string e) => (e ?? "").Trim().ToLowerInvariant();
        private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
        private string CodeHash(string email, string code) =>
            Convert.ToHexString(HMACSHA256.HashData(_secret ?? Array.Empty<byte>(), Encoding.UTF8.GetBytes($"code:{email}:{code}"))).ToLowerInvariant();
        private static string AppName => DataGuard.Config["appName"]?.ToString() is { Length: > 0 } n ? n : "the app";

        /// <summary>The active member with this sign-in email: (user id, username, role), or null.</summary>
        private static (int id, string username, int role)? MemberByEmail(DbConnection c, string email)
        {
            using var q = Db.Command(@"SELECT u.user_id, u.username, u.role_id FROM user_emails e JOIN users u ON u.user_id = e.user_id
                WHERE e.email = @e AND u.isActive = 1", c);
            q.Parameters.AddWithValue("@e", email);
            using var r = q.ExecuteReader();
            return r.Read() ? (r.GetInt32(0), r.GetString(1), r.GetInt32(2)) : null;
        }

        private static string OwnerOf(DbConnection c, int userId) =>
            Scalar<string>(c, "SELECT MIN(owner_name) FROM permissionmatrix WHERE user_id = @u AND isActive = 1", ("@u", userId));

        /// <summary>Sets (or replaces) a member's sign-in email. False when another member already uses it.</summary>
        private static bool SetEmail(DbConnection c, int userId, string email, bool verified)
        {
            EnsureEmailTables(c);
            if (Scalar<long>(c, "SELECT COUNT(*) FROM user_emails WHERE email = @e AND user_id <> @u", ("@e", email), ("@u", userId)) > 0) return false;
            Exec(c, "DELETE FROM user_emails WHERE user_id = @u", ("@u", userId));
            using var cmd = Db.Command($"INSERT INTO user_emails (user_id, email, verified_at) VALUES (@u, @e, {(verified ? Db.UtcNow : "NULL")})", c);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@e", email);
            Db.Execute(cmd);
            return true;
        }

        [AllowAnonymous, HttpGet, Route(PublicCodeRoute + "/available")]
        public ActionResult EmailCodeAvailable() => Ok(new { enabled = NotificationMailer.CanSendSignIn && _secret != null });

        public class EmailInput { public string email { get; set; } public string code { get; set; } }

        [AllowAnonymous, EnableRateLimiting("login"), HttpPost, Route(PublicCodeRoute)]
        public ActionResult SendEmailCode(EmailInput input)
        {
            const string sent = "If that email belongs to a member, a code is on its way. Check your inbox.";
            if (!NotificationMailer.CanSendSignIn || _secret == null) return StatusCode(404, new { message = "Email sign-in isn't available for this app." });
            var email = NormalEmail(input?.email);
            if (email.Length > 254 || !EmailPattern.IsMatch(email)) return BadRequest(new { message = "Enter a valid email address." });
            using var c = Open();
            EnsureEmailTables(c);
            var member = MemberByEmail(c, email);
            if (member == null) return Ok(new { message = sent }); // same answer: emails can't be probed
            var eh = Hash(email);
            if (Scalar<long>(c, "SELECT COUNT(*) FROM login_codes WHERE email_hash = @h AND createdAt > @since",
                    ("@h", eh), ("@since", DateTime.UtcNow.AddHours(-1))) >= CodesPerHour)
                return StatusCode(429, new { message = "Too many codes asked for. Wait a while and try again." });
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            Exec(c, "UPDATE login_codes SET used = 1 WHERE email_hash = @h AND used = 0", ("@h", eh)); // only the newest code works
            Exec(c, "INSERT INTO login_codes (email_hash, code_hash, expires_at, createdAt) VALUES (@h, @c, @x, @n)",
                ("@h", eh), ("@c", CodeHash(email, code)), ("@x", DateTime.UtcNow.AddMinutes(CodeMinutes)), ("@n", DateTime.UtcNow));
            if (!NotificationMailer.SendCode(email, code, AppName))
                return StatusCode(502, new { message = "The email couldn't be sent. Try again in a minute." });
            return Ok(new { message = sent });
        }

        [AllowAnonymous, EnableRateLimiting("login"), HttpPost, Route(PublicCodeRoute + "/verify")]
        public ActionResult VerifyEmailCode(EmailInput input)
        {
            const string wrong = "That code isn't right or has expired. Ask for a new one.";
            if (!NotificationMailer.CanSendSignIn || _secret == null) return StatusCode(404, new { message = "Email sign-in isn't available for this app." });
            var email = NormalEmail(input?.email);
            var code = (input?.code ?? "").Trim().Replace(" ", "");
            if (!EmailPattern.IsMatch(email) || !Regex.IsMatch(code, @"^\d{6}$")) return BadRequest(new { message = wrong });
            using var c = Open();
            EnsureEmailTables(c);
            var eh = Hash(email);
            long id = 0; string stored = null;
            using (var q = Db.Command($@"SELECT {Db.Top(1)}code_id, code_hash FROM login_codes WHERE email_hash = @h AND used = 0 AND attempts < @t
                AND expires_at > @now ORDER BY code_id DESC{Db.LimitEnd(1)}", c))
            {
                q.Parameters.AddWithValue("@h", eh);
                q.Parameters.AddWithValue("@t", CodeTries);
                q.Parameters.AddWithValue("@now", DateTime.UtcNow);
                using var r = q.ExecuteReader();
                if (r.Read()) { id = r.GetInt64(0); stored = r.GetString(1); }
            }
            if (stored == null) return BadRequest(new { message = wrong });
            // one try used, atomically; the code must still be unused
            using (var take = Db.Command("UPDATE login_codes SET attempts = attempts + 1 WHERE code_id = @id AND used = 0 AND attempts < @t", c))
            {
                take.Parameters.AddWithValue("@id", id);
                take.Parameters.AddWithValue("@t", CodeTries);
                if (take.ExecuteNonQuery() == 0) return BadRequest(new { message = wrong });
            }
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stored), Encoding.ASCII.GetBytes(CodeHash(email, code))))
                return BadRequest(new { message = wrong });
            using (var use = Db.Command("UPDATE login_codes SET used = 1 WHERE code_id = @id AND used = 0", c))
            {
                use.Parameters.AddWithValue("@id", id);
                if (use.ExecuteNonQuery() == 0) return BadRequest(new { message = wrong }); // used by a parallel request
            }
            var member = MemberByEmail(c, email);
            if (member == null) return BadRequest(new { message = wrong });
            Exec(c, $"UPDATE user_emails SET verified_at = {Db.UtcNow} WHERE user_id = @u AND verified_at IS NULL", ("@u", member.Value.id));
            AuthGuard.RecordSuccess(member.Value.username);
            TokenController.IssueSession(this, _secretText, member.Value.id, member.Value.username, member.Value.role, OwnerOf(c, member.Value.id));
            return Ok(new { message = "Signed in." });
        }

        public class MemberEmailInput { public string email { get; set; } }

        /// <summary>Administrators set or clear a member's sign-in email (empty = remove).</summary>
        [HttpPut]
        [Route(Route + "/members/{userId:int}/email")]
        public ActionResult SetMemberEmail(int userId, MemberEmailInput input)
        {
            if (!IsAdmin) return Forbid();
            using var c = Open();
            if (!MembersOfTenant(c).Contains(userId)) return NotFound(new APIResponse(ResponseCode.ERROR, "Member not found"));
            EnsureEmailTables(c);
            var email = NormalEmail(input?.email);
            if (email.Length == 0) { Exec(c, "DELETE FROM user_emails WHERE user_id = @u", ("@u", userId)); return Ok(new APIResponse(ResponseCode.SUCCESS, "Sign-in email removed")); }
            if (email.Length > 254 || !EmailPattern.IsMatch(email)) return BadRequest(new APIResponse(ResponseCode.ERROR, "Enter a valid email address"));
            if (!SetEmail(c, userId, email, verified: false)) return BadRequest(new APIResponse(ResponseCode.ERROR, "Another member already uses that email"));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Sign-in email saved"));
        }
    }
}
