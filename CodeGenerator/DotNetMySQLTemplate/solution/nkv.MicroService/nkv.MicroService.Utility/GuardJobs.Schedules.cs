using MySqlConnector;
using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Scheduled rules from ui.config.json "jobs":
    ///   "deadlines": [{ "table": "case_deadlines", "due": "due_at", "met": "met_at", "breached": "is_breached",
    ///                   "escalate": { "table": "case_escalations", "values": { "case_id": "{case_id}", "reason": "Missed {deadline_type}" } } }]
    /// Overdue, unmet, unflagged rows are flagged as breached and (optionally) an escalation row is created from the template,
    /// in the deadline's own tenant and workspace, with audit and outbox rows like any other write.
    /// </summary>
    public static partial class GuardJobs
    {
        static Timer _jobsTimer;
        static int _jobsBusy;
        static DateTime? _lastRun;
        static int _breaches, _escalations;

        static partial void StartImpl(IServiceProvider services)
        {
            // deadline checks only; "schedules" are run by the API's JobScheduler (it also works when the app scales to zero)
            if (DataGuard.Config["jobs"] is not JsonObject jobs || jobs["deadlines"] is not JsonArray { Count: > 0 }) return;
            var every = jobs["everySeconds"]?.GetValue<int>() ?? 60;
            _jobsTimer = new Timer(_ => RunScheduledNow(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(every));
            if (Db.PauseFriendly)
                Console.WriteLine($"DataGuard: scheduled checks (\"jobs\") query the database every {every}s, so a serverless database won't auto-pause while this API runs.");
        }

        public static JsonObject JobsSummary() => new JsonObject
        {
            ["lastRun"] = _lastRun?.ToString("o"), ["breachesFlagged"] = _breaches, ["escalationsCreated"] = _escalations,
            ["deadlineRules"] = (DataGuard.Config["jobs"]?["deadlines"] as JsonArray)?.Count ?? 0,
        };

        public static JsonObject RunScheduledNow()
        {
            if (Interlocked.Exchange(ref _jobsBusy, 1) == 1) return JobsSummary();
            try
            {
                if (DataGuard.Config["jobs"]?["deadlines"] is JsonArray rules)
                    foreach (var rule in rules.OfType<JsonObject>()) RunDeadlineRule(rule);
                _lastRun = DateTime.UtcNow;
            }
            catch (Exception ex) { Console.WriteLine("Scheduled jobs error: " + ex.Message); }
            finally { Interlocked.Exchange(ref _jobsBusy, 0); }
            return JobsSummary();
        }

        static void RunDeadlineRule(JsonObject rule)
        {
            var table = DataGuard.SafeIdentifier(rule["table"]!.GetValue<string>());
            if (!DataGuard.PrimaryKeys.TryGetValue(table, out var pk)) return;
            var due = DataGuard.SafeIdentifier(rule["due"]?.GetValue<string>() ?? "due_at");
            var met = DataGuard.SafeIdentifier(rule["met"]?.GetValue<string>() ?? "met_at");
            var flag = DataGuard.SafeIdentifier(rule["breached"]?.GetValue<string>() ?? "is_breached");
            using var c = Db.Connection(DataGuard.ConnectionString);
            c.Open();
            using var tx = c.BeginTransaction();
            var rows = new List<Dictionary<string, object>>();
            using (var q = Db.Command($@"SELECT {Db.Top(500)}* FROM {Db.Q(table)}{(Db.IsSqlServer ? " WITH (UPDLOCK, READPAST, ROWLOCK)" : "")} WHERE isActive = 1 AND {Db.Q(met)} IS NULL AND COALESCE({Db.Q(flag)}, 0) = 0
                AND {Db.Q(due)} < {Db.UtcNow}{(Db.IsSqlServer ? "" : " LIMIT 500 FOR UPDATE SKIP LOCKED")}", c, tx))
            using (var r = q.ExecuteReader())
                while (r.Read())
                {
                    var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                    rows.Add(d);
                }
            foreach (var row in rows)
            {
                using (var u = Db.Command($"UPDATE {Db.Q(table)} SET {Db.Q(flag)} = 1 WHERE {Db.Q(pk)} = @id", c, tx))
                { u.Parameters.AddWithValue("@id", row[pk]); u.ExecuteNonQuery(); }
                _breaches++;
                if (rule["escalate"] is JsonObject esc && esc["table"] is JsonValue et && esc["values"] is JsonObject tmpl)
                {
                    var values = tmpl.ToDictionary(kv => kv.Key, kv => (object)Fill(kv.Value?.ToString(), row), StringComparer.OrdinalIgnoreCase);
                    DataGuard.SystemInsert(c, tx, DataGuard.SafeIdentifier(et.GetValue<string>()), values, row);
                    _escalations++;
                }
            }
            tx.Commit();
        }

        static string Fill(string template, Dictionary<string, object> row) =>
            template == null ? null : Regex.Replace(template, @"\{(\w+)\}", m => row.TryGetValue(m.Groups[1].Value, out var v) && v != null ? Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) : "");
    }
}
