using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Notifications driven by ui.config.json "notifications": in-app rows (the bell) written in the same transaction as the
    /// change that caused them, plus email queued on the same row and sent by <see cref="NotificationMailer"/> with retries.
    ///
    ///   "notifications": [ { "table": "engagements", "when": { "to": "Report" }, "notify": ["role:Partner", "manager_user"],
    ///                        "title": "{engagement_number} is ready for sign-off", "channels": ["inApp", "email"] } ]
    ///
    /// when: { "on": "create" } | { "to": state or [states], "column": lifecycle column by default } | { "changed": column }
    /// notify: "creator" (who created the row) | "role:Name" | a column holding a users id or login name |
    ///         { "column", "table", "userColumn" } for a column pointing at a row that holds the user (e.g. members.user_id).
    /// The person who made the change is never notified about it.
    /// </summary>
    public static partial class DataGuard
    {
        public const string NotificationsTable = "user_notifications", NotificationSettingsTable = "user_notification_settings";

        sealed class NotifyRule
        {
            public string Table, On, Column, Changed, Title;
            public HashSet<string> To;
            public List<JsonNode> Notify = new();
            public bool InApp = true, Email;
        }
        static readonly List<NotifyRule> _notifyRules = new();
        public static int NotificationRules => _notifyRules.Count;

        static void InitNotifications(MySqlConnection c)
        {
            _notifyRules.Clear();
            using (var t = new MySqlCommand(@"CREATE TABLE IF NOT EXISTS user_notifications (notification_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    user_id INT NOT NULL, title VARCHAR(500) NOT NULL, table_name VARCHAR(100) NULL, record_id VARCHAR(100) NULL,
                    actor VARCHAR(255) NULL, createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, read_at DATETIME NULL,
                    email_status VARCHAR(10) NOT NULL DEFAULT 'none', email_attempts INT NOT NULL DEFAULT 0, email_error VARCHAR(500) NULL,
                    KEY ix_notifications_user (user_id, read_at, notification_id), KEY ix_notifications_email (email_status))", c)) t.ExecuteNonQuery();
            using (var t = new MySqlCommand(@"CREATE TABLE IF NOT EXISTS user_notification_settings (user_id INT NOT NULL PRIMARY KEY,
                    email VARCHAR(255) NULL, email_on TINYINT(1) NOT NULL DEFAULT 1,
                    modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP)", c)) t.ExecuteNonQuery();
            if (Config["notifications"] is not JsonArray rules) return;
            foreach (var n in rules.OfType<JsonObject>())
            {
                var table = n["table"]?.GetValue<string>();
                if (table == null || !Columns.ContainsKey(table)) { Console.WriteLine($"DataGuard: notification rule for unknown table '{table}' skipped."); continue; }
                var r = new NotifyRule { Table = table, Title = n["title"]?.GetValue<string>() };
                var when = n["when"] as JsonObject ?? new JsonObject();
                r.On = when["on"]?.GetValue<string>();
                r.To = when["to"] == null ? null : Set(when["to"]);
                r.Column = when["column"]?.GetValue<string>() ?? (_lifecycles.TryGetValue(table, out var lc) ? lc.Column : "status");
                r.Changed = when["changed"]?.GetValue<string>();
                if (r.On == null && r.To == null && r.Changed == null) r.On = "create";
                foreach (var x in n["notify"] is JsonArray na ? na.ToList() : new List<JsonNode> { n["notify"] }) if (x != null) r.Notify.Add(x.DeepClone());
                var ch = Set(n["channels"], "inApp");
                r.InApp = ch.Contains("inApp"); r.Email = ch.Contains("email");
                _notifyRules.Add(r);
            }
        }

        /// <summary>Called after a write commits its own rows (same transaction): writes one notification per recipient.</summary>
        static void Notify(MySqlCommand c, Caller caller, string table, string action, object id, Dictionary<string, object> row, Dictionary<string, object> before)
        {
            if (_notifyRules.Count == 0 || row == null) return;
            foreach (var r in _notifyRules)
            {
                if (!r.Table.Equals(table, StringComparison.OrdinalIgnoreCase)) continue;
                bool Changed(string col) => before != null && row.ContainsKey(col) && !SameValue(row[col], before.GetValueOrDefault(col));
                var hit = r.On switch
                {
                    "create" => action == "create",
                    "update" => action == "update",
                    _ => r.To != null
                        ? Str(row.GetValueOrDefault(r.Column)) is { } st && r.To.Contains(st) && (action == "create" || Changed(r.Column))
                        : r.Changed != null && action == "update" && Changed(r.Changed),
                };
                if (r.On != null && hit && r.To != null) hit = Str(row.GetValueOrDefault(r.Column)) is { } s2 && r.To.Contains(s2);
                if (!hit) continue;
                var users = new HashSet<int>();
                foreach (var who in r.Notify) foreach (var u in Recipients(c, caller, table, who, row)) users.Add(u);
                users.Remove(caller?.UserId ?? 0);
                if (users.Count == 0) continue;
                var title = Render(r.Title ?? (r.To != null ? $"{table} #{{id}} is now {{{r.Column}}}" : $"New {table} #{{id}}"), row, id);
                foreach (var u in users)
                {
                    var email = r.Email ? EmailFor(c, u) : null;
                    if (!r.InApp && email == null) continue;
                    using var ins = new MySqlCommand(@"INSERT INTO user_notifications (user_id, title, table_name, record_id, actor, email_status, read_at, createdAt)
                        VALUES (@u, @t, @tb, @id, @a, @es, @read, UTC_TIMESTAMP())", c.Connection, c.Transaction);
                    ins.Parameters.AddWithValue("@u", u);
                    ins.Parameters.AddWithValue("@t", title.Length > 500 ? title[..500] : title);
                    ins.Parameters.AddWithValue("@tb", table);
                    ins.Parameters.AddWithValue("@id", id?.ToString());
                    ins.Parameters.AddWithValue("@a", ActorName(caller));
                    ins.Parameters.AddWithValue("@es", email != null ? "pending" : "none");
                    ins.Parameters.AddWithValue("@read", r.InApp ? DBNull.Value : DateTime.UtcNow); // email-only rules don't fill the bell
                    ins.ExecuteNonQuery();
                }
            }
        }

        static string Render(string template, Dictionary<string, object> row, object id) =>
            Regex.Replace(template, @"\{(\w+)\}", m => m.Groups[1].Value.Equals("id", StringComparison.OrdinalIgnoreCase) ? id?.ToString() ?? ""
                : Str(row.GetValueOrDefault(m.Groups[1].Value)) ?? "");

        static string EmailFor(MySqlCommand c, int userId)
        {
            using var q = new MySqlCommand("SELECT email FROM user_notification_settings WHERE user_id = @u AND email_on = 1 AND email IS NOT NULL AND email <> ''", c.Connection, c.Transaction);
            q.Parameters.AddWithValue("@u", userId);
            return q.ExecuteScalar() as string;
        }

        /// <summary>Active users behind one "notify" entry. Within a tenant, only users of the same tenant.</summary>
        static IEnumerable<int> Recipients(MySqlCommand c, Caller caller, string table, JsonNode who, Dictionary<string, object> row)
        {
            var ids = new List<int>();
            string sql = null; object p = null;
            if (who is JsonValue v)
            {
                var s = v.GetValue<string>();
                if (s.Equals("creator", StringComparison.OrdinalIgnoreCase))
                {
                    var name = Str(row.GetValueOrDefault(CreatedByUser));
                    if (name != null) { sql = "SELECT user_id FROM users WHERE username = @p AND isActive = 1"; p = Regex.Replace(name, " via AI$", ""); }
                }
                else if (s.StartsWith("role:", StringComparison.OrdinalIgnoreCase))
                { sql = "SELECT u.user_id FROM users u JOIN roles r ON r.role_id = u.role_id WHERE r.role_name = @p AND u.isActive = 1"; p = s[5..]; }
                else if (row.GetValueOrDefault(s) is { } val && Str(val) is { } sv)
                { sql = "SELECT user_id FROM users WHERE (user_id = @p OR username = @p) AND isActive = 1"; p = sv; }
            }
            else if (who is JsonObject o && o["column"]?.GetValue<string>() is { } col && Str(row.GetValueOrDefault(col)) is { } fk)
            {
                var t = o["table"]?.GetValue<string>();
                var uc = o["userColumn"]?.GetValue<string>() ?? "user_id";
                if (t != null && PrimaryKeys.TryGetValue(t, out var key))
                { sql = $"SELECT u.user_id FROM `{SafeIdentifier(t)}` x JOIN users u ON (u.user_id = x.`{SafeIdentifier(uc)}` OR u.username = x.`{SafeIdentifier(uc)}`) WHERE x.`{SafeIdentifier(key)}` = @p AND u.isActive = 1"; p = fk; }
            }
            if (sql == null) return ids;
            if (TenancyEnabled && caller?.Tenant != null)
                sql = $"SELECT q.user_id FROM ({sql}) q JOIN user_tenants ut ON ut.user_id = q.user_id WHERE ut.tenant_id = {caller.Tenant.Value}";
            using var cmd = new MySqlCommand(sql, c.Connection, c.Transaction);
            cmd.Parameters.AddWithValue("@p", p);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) ids.Add(Convert.ToInt32(rd.GetValue(0)));
            return ids;
        }
    }
}
