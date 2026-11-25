using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Time-based jobs from ui.config.json "jobs": { "schedules": [ ... ] }. Each schedule runs at the end of every period:
    ///   { "id": "biweekly_reports", "every": "14d", "from": "programs.start_date", "until": "programs.end_date", "at": "08:00",
    ///     "timezone": "America/Moncton", "weekdays": false, "runAs": "admin@x.com",
    ///     "workflow": "prepare_university_report",                       // an AI workflow, once per matching row ...
    ///     "forEach": { "table": "interns", "where": { "status": "active" } },
    ///     "note": "Reporting period {periodStart} to {periodEnd}",
    ///     "notify": ["role:Administrator"] }                             // ... told when its proposal waits for approval
    ///   { "id": "nightly", "every": "1d", "at": "02:00", "job": "billing.close-day", "input": { } }   // a JobHandler kind
    ///   { "id": "timesheet_reminder", "every": "1d", "at": "17:00", "weekdays": true,                  // a reminder, no AI
    ///     "remind": { "table": "interns", "where": { "status": "active" }, "userColumn": "email",
    ///                 "unless": { "table": "timesheets", "column": "intern_id", "date": "work_date" },
    ///                 "title": "Please log today's timesheet", "summaryTo": ["role:Administrator"] } }
    /// every: Nm (minutes, at least 5), Nh, Nd or Nw. from/until: a date, or table.column (earliest / latest active row).
    /// State lives in job_schedules (next run, last run, last result). Runs are claimed with a counter, so with several
    /// copies of the API each run happens once; a run missed while the app was down happens once when it is back.
    ///
    /// Waking up: with Azure Service Bus (serverless: the app scales to zero) the scheduler books a scheduled message on
    /// the app's own jobs queue for the next run (at most a day ahead); the queue trigger wakes the app, the worker hands
    /// the message here, and the next one is booked. Every start re-checks, so a lost wake-up heals on the next request.
    /// With RabbitMQ (always-on servers) it checks every Jobs:SchedulerSeconds (60).
    /// </summary>
    public sealed class JobScheduler : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly JobStore _store;
        private readonly JobQueue _queue;
        private readonly ILogger<JobScheduler> _log;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private const string TickRow = "__wake__";

        public JobScheduler(IServiceProvider services, IConfiguration configuration, JobStore store, JobQueue queue, ILogger<JobScheduler> log)
        {
            _services = services; _configuration = configuration; _store = store; _queue = queue; _log = log;
        }

        // ---------------------------------------------------------------- schedule definitions

        public sealed class Schedule
        {
            public string Id, Unit, From, Until, Workflow, Job, Note, RunAs, Spec, Error;
            public int Amount;
            public TimeSpan At;
            public TimeZoneInfo Zone = TimeZoneInfo.Utc;
            public bool Weekdays;
            public JsonObject ForEach, Input, Remind;
            public JsonArray Notify;
            public TimeSpan Step => Unit switch { "m" => TimeSpan.FromMinutes(Amount), "h" => TimeSpan.FromHours(Amount), "w" => TimeSpan.FromDays(7 * Amount), _ => TimeSpan.FromDays(Amount) };
            public bool Daily => Unit is "d" or "w";
        }

        static readonly Regex Every = new(@"^\s*(\d+)\s*(m|h|d|w)\s*$", RegexOptions.IgnoreCase);

        /// <summary>The schedules in ui.config.json, each with Error set when it can't run.</summary>
        public static List<Schedule> Load()
        {
            var list = new List<Schedule>();
            if (DataGuard.Config["jobs"]?["schedules"] is not JsonArray arr) return list;
            foreach (var o in arr.OfType<JsonObject>())
            {
                var s = new Schedule
                {
                    Id = o["id"]?.ToString(), From = o["from"]?.ToString(), Until = o["until"]?.ToString(),
                    Workflow = o["workflow"]?.ToString(), Job = o["job"]?.ToString(), Note = o["note"]?.ToString(), RunAs = o["runAs"]?.ToString(),
                    Weekdays = o["weekdays"]?.GetValueKind() == System.Text.Json.JsonValueKind.True,
                    ForEach = o["forEach"] as JsonObject, Input = o["input"] as JsonObject, Remind = o["remind"] as JsonObject,
                    Notify = o["notify"] as JsonArray, Spec = o.ToJsonString(),
                };
                list.Add(s);
                if (string.IsNullOrWhiteSpace(s.Id) || s.Id.Length > 90 || s.Id.StartsWith("__")) { s.Error = "needs an \"id\" (up to 90 characters)"; continue; }
                var m = Every.Match(o["every"]?.ToString() ?? "");
                if (!m.Success || (s.Amount = int.Parse(m.Groups[1].Value)) < 1) { s.Error = "\"every\" must look like 30m, 6h, 1d, 14d or 1w"; continue; }
                s.Unit = m.Groups[2].Value.ToLowerInvariant();
                if (s.Unit == "m" && s.Amount < 5) { s.Error = "runs at most every 5 minutes"; continue; }
                if (o["at"]?.ToString() is { Length: > 0 } at && !TimeSpan.TryParseExact(at, @"hh\:mm", CultureInfo.InvariantCulture, out s.At))
                { s.Error = "\"at\" must be HH:mm, e.g. 08:00"; continue; }
                if (o["timezone"]?.ToString() is { Length: > 0 } tz)
                    try { s.Zone = TimeZoneInfo.FindSystemTimeZoneById(tz); }
                    catch (Exception) { s.Error = $"unknown timezone '{tz}' (use an IANA name such as America/Toronto)"; continue; }
                var actions = new[] { s.Workflow, s.Job, s.Remind != null ? "remind" : null }.Count(a => !string.IsNullOrEmpty(a));
                if (actions != 1) { s.Error = "needs exactly one of \"workflow\", \"job\" or \"remind\""; continue; }
                if (s.Workflow != null && s.ForEach?["table"] is not JsonValue) { s.Error = "a workflow schedule needs \"forEach\": {\"table\": ...}"; continue; }
                foreach (var t in new[] { s.ForEach?["table"]?.ToString(), s.Remind?["table"]?.ToString(), s.Remind?["unless"]?["table"]?.ToString() })
                    if (t != null && !DataGuard.Columns.ContainsKey(t)) s.Error = $"unknown table '{t}'";
            }
            var dup = list.Where(s => s.Id != null).GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1);
            if (dup != null) foreach (var s in dup) s.Error = $"duplicate id '{s.Id}'";
            return list;
        }

        // ---------------------------------------------------------------- timing

        static DateTime Sec(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        static DateTime Now() => Sec(DateTime.UtcNow);

        DateTime? DateOf(string spec, bool latest)
        {
            if (string.IsNullOrWhiteSpace(spec)) return null;
            if (DateTime.TryParse(spec, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
            var parts = spec.Split('.');
            if (parts.Length != 2 || !DataGuard.Columns.TryGetValue(parts[0], out var cols) || !cols.Contains(parts[1])) return null;
            using var c = _store.Open();
            using var q = Db.Command($"SELECT {(latest ? "MAX" : "MIN")}({Db.Q(DataGuard.SafeIdentifier(parts[1]))}) FROM {Db.Q(DataGuard.SafeIdentifier(parts[0]))} WHERE isActive = 1", c);
            var v = q.ExecuteScalar();
            return v == null || v is DBNull ? null : Convert.ToDateTime(v, CultureInfo.InvariantCulture);
        }

        DateTime ToUtc(Schedule s, DateTime local)
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            while (s.Zone.IsInvalidTime(local)) local = local.AddMinutes(30); // skipped by a daylight-saving change
            return Sec(TimeZoneInfo.ConvertTimeToUtc(local, s.Zone));
        }

        /// <summary>The first run strictly after <paramref name="afterUtc"/>, or null when the schedule has ended or can't start.
        /// Runs fall at the end of each period: from + 1 period, from + 2 periods, ...</summary>
        public DateTime? Next(Schedule s, DateTime afterUtc)
        {
            var from = DateOf(s.From, false);
            if (s.From != null && from == null) return null;              // e.g. no program yet: nothing to anchor to
            var until = DateOf(s.Until, true);
            if (s.Until != null && until == null) return null;
            DateTime? result = null;
            if (s.Daily)
            {
                var days = s.Unit == "w" ? 7 * s.Amount : s.Amount;
                var anchor = (from ?? new DateTime(2000, 1, 3)).Date;       // default phase: weekly runs fall on Mondays
                var afterLocal = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, s.Zone);
                var k = Math.Max(1, (int)Math.Floor((afterLocal.Date - anchor).TotalDays / days));
                for (var i = 0; i < 1000; i++, k++)
                {
                    var day = anchor.AddDays((double)k * days);
                    if (s.Weekdays && days == 1 && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                    var utc = ToUtc(s, day + s.At);
                    if (utc > afterUtc) { result = utc; break; }
                }
            }
            else
            {
                var anchor = from.HasValue ? ToUtc(s, from.Value) : new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var k = Math.Max(1, (long)Math.Floor((afterUtc - anchor).Ticks / (double)s.Step.Ticks));
                for (var i = 0; i < 10; i++, k++)
                {
                    var utc = Sec(anchor + TimeSpan.FromTicks(s.Step.Ticks * k));
                    if (utc > afterUtc) { result = utc; break; }
                }
            }
            if (result == null) return null;
            if (until.HasValue)
            {
                // the last run is the one whose period starts on or before the end date (it covers the final days)
                if (s.Daily)
                {
                    var days = s.Unit == "w" ? 7 * s.Amount : s.Amount;
                    if (TimeZoneInfo.ConvertTimeFromUtc(result.Value, s.Zone).Date.AddDays(-days) > until.Value.Date) return null;
                }
                else if (result.Value - s.Step > ToUtc(s, until.Value.Date.AddDays(1))) return null;
            }
            return result;
        }

        /// <summary>The period a run covers: daily schedules as local dates (the run's day is not included), others as UTC times.</summary>
        public (string start, string end, string runDate) Period(Schedule s, DateTime runUtc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(runUtc, s.Zone);
            if (s.Daily)
            {
                var days = s.Unit == "w" ? 7 * s.Amount : s.Amount;
                return (local.Date.AddDays(-days).ToString("yyyy-MM-dd"), local.Date.AddDays(-1).ToString("yyyy-MM-dd"), local.ToString("yyyy-MM-dd"));
            }
            return ((runUtc - s.Step).ToString("yyyy-MM-ddTHH:mm:ssZ"), runUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), local.ToString("yyyy-MM-dd"));
        }

        // ---------------------------------------------------------------- state (job_schedules)

        sealed class State { public string Spec, Status; public DateTime? NextRun, LastRun; public int Runs; public long? Seq; }

        State Read(DbConnection c, string id)
        {
            using var q = Db.Command("SELECT spec, next_run_at, last_run_at, runs, tick_seq, last_status FROM job_schedules WHERE schedule_id = @id", c);
            q.Parameters.AddWithValue("@id", id);
            using var r = q.ExecuteReader();
            if (!r.Read()) return null;
            return new State
            {
                Spec = r.IsDBNull(0) ? null : r.GetString(0),
                NextRun = r.IsDBNull(1) ? null : DateTime.SpecifyKind(r.GetDateTime(1), DateTimeKind.Utc),
                LastRun = r.IsDBNull(2) ? null : DateTime.SpecifyKind(r.GetDateTime(2), DateTimeKind.Utc),
                Runs = Convert.ToInt32(r.GetValue(3)), Seq = r.IsDBNull(4) ? null : Convert.ToInt64(r.GetValue(4)),
                Status = r.IsDBNull(5) ? null : r.GetString(5),
            };
        }

        static int Exec(DbConnection c, string sql, params (string, object)[] ps)
        {
            using var cmd = Db.Command(sql, c);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            return cmd.ExecuteNonQuery();
        }

        static string Cut(string s, int n) => s == null ? null : s.Length <= n ? s : s[..(n - 1)] + "…";

        void Upsert(DbConnection c, string id, string spec, DateTime? next, string status, string message)
        {
            var set = Exec(c, $"UPDATE job_schedules SET spec = @s, next_run_at = @n, last_status = @st, last_message = @m, modifiedAt = {Db.UtcNow} WHERE schedule_id = @id",
                ("@s", spec), ("@n", next), ("@st", status), ("@m", Cut(message, 1000)), ("@id", id));
            if (set > 0) return;
            try
            {
                Exec(c, $"INSERT INTO job_schedules (schedule_id, spec, next_run_at, last_status, last_message, runs, modifiedAt) VALUES (@id, @s, @n, @st, @m, 0, {Db.UtcNow})",
                    ("@id", id), ("@s", spec), ("@n", next), ("@st", status), ("@m", Cut(message, 1000)));
            }
            catch (DbException) { /* another copy of the API inserted it first */ }
        }

        // ---------------------------------------------------------------- running

        /// <summary>Checks every schedule once: sets up new or changed ones and runs the ones that are due. Safe to call any time.</summary>
        public async Task<JsonArray> RunDue(CancellationToken ct = default)
        {
            var report = new JsonArray();
            await _gate.WaitAsync(ct);
            try
            {
                var schedules = Load();
                using var c = _store.Open();
                foreach (var s in schedules)
                {
                    if (ct.IsCancellationRequested) break;
                    try { report.Add(RunOne(c, s)); }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Schedule {Id} failed", s.Id);
                        report.Add(new JsonObject { ["id"] = s.Id, ["status"] = "failed", ["message"] = ex.Message });
                    }
                }
            }
            finally { _gate.Release(); }
            return report;
        }

        JsonObject RunOne(DbConnection c, Schedule s)
        {
            var now = Now();
            var row = new JsonObject { ["id"] = s.Id };
            if (s.Error != null)
            {
                _log.LogWarning("Schedule {Id} can't run: {Error}", s.Id, s.Error);
                if (s.Id != null && !s.Id.StartsWith("__")) Upsert(c, s.Id, s.Spec, null, "invalid", s.Error);
                row["status"] = "invalid"; row["message"] = s.Error;
                return row;
            }
            var st = Read(c, s.Id);
            if (st == null || st.Spec != s.Spec || (st.NextRun == null && st.Status != "ended"))
            {
                // new or changed: start counting from now (past runs are not made up)
                var next = Next(s, now);
                Upsert(c, s.Id, s.Spec, next, next == null ? "waiting" : "scheduled",
                    next == null ? "Nothing to run yet (no start date, or the end date has passed)." : $"Next run {next:yyyy-MM-dd HH:mm} UTC.");
                row["status"] = next == null ? "waiting" : "scheduled"; row["nextRun"] = next?.ToString("o");
                return row;
            }
            if (st.NextRun == null) { row["status"] = st.Status; return row; }
            if (st.NextRun > now) { row["status"] = "scheduled"; row["nextRun"] = st.NextRun.Value.ToString("o"); return row; }

            // due (possibly late): claim it, so only one copy of the API runs it
            var due = st.NextRun.Value;
            var following = Next(s, now);
            var claimed = Exec(c, $@"UPDATE job_schedules SET runs = runs + 1, last_run_at = @now, next_run_at = @n, last_status = 'running',
                                     modifiedAt = {Db.UtcNow} WHERE schedule_id = @id AND runs = @runs",
                ("@now", now), ("@n", following), ("@id", s.Id), ("@runs", st.Runs)) == 1;
            if (!claimed) { row["status"] = "taken"; return row; }

            string status = "ok", message;
            try { message = Fire(s, due); }
            catch (Exception ex) when (ex is JobException or DataGuardException or InvalidOperationException)
            { status = "failed"; message = ex.Message; }
            catch (Exception ex) { status = "failed"; message = "Error: " + ex.Message; _log.LogError(ex, "Schedule {Id} run failed", s.Id); }
            if (now - due > TimeSpan.FromMinutes(10)) message = $"(late: due {due:yyyy-MM-dd HH:mm} UTC) " + message;
            Exec(c, $"UPDATE job_schedules SET last_status = @st, last_message = @m, modifiedAt = {Db.UtcNow} WHERE schedule_id = @id",
                ("@st", following == null && status == "ok" ? "ended" : status), ("@m", Cut(message, 1000)), ("@id", s.Id));
            _log.LogInformation("Schedule {Id}: {Status} {Message}", s.Id, status, message);
            row["status"] = status; row["message"] = message; row["nextRun"] = following?.ToString("o");
            return row;
        }

        /// <summary>The account a schedule runs as: runAs (username or sign-in email), else the first active administrator.</summary>
        (JobUser user, DataGuard.Caller caller) RunAs(Schedule s)
        {
            using var c = _store.Open();
            using var q = Db.Command(s.RunAs != null
                ? $"SELECT {Db.Top(1)}user_id, username, role_id FROM users WHERE isActive = 1 AND username = @u ORDER BY user_id{Db.LimitEnd(1)}"
                : $"SELECT {Db.Top(1)}user_id, username, role_id FROM users WHERE isActive = 1 AND role_id = 1 ORDER BY user_id{Db.LimitEnd(1)}", c);
            q.Parameters.AddWithValue("@u", (object)s.RunAs ?? DBNull.Value);
            int id, role; string name;
            using (var r = q.ExecuteReader())
            {
                if (!r.Read()) throw new JobException(s.RunAs != null ? $"runAs '{s.RunAs}' isn't an active member." : "There is no active administrator to run it as.");
                id = Convert.ToInt32(r.GetValue(0)); name = r.GetString(1); role = Convert.ToInt32(r.GetValue(2));
            }
            using var o = Db.Command("SELECT MIN(owner_name) FROM permissionmatrix WHERE user_id = @u AND isActive = 1", c);
            o.Parameters.AddWithValue("@u", id);
            var owner = o.ExecuteScalar() as string ?? name;
            var caller = new DataGuard.Caller { User = name, UserId = id, RoleId = role, Owner = owner, CorrelationId = "schedule-" + s.Id };
            if (DataGuard.TenancyEnabled)
            {
                var (tenant, unassigned) = DataGuard.ResolveTenant(id, role);
                if (unassigned) throw new JobException($"'{name}' isn't assigned to a tenant.");
                caller.Tenant = tenant;
            }
            return (new JobUser(id, name, role, owner), caller);
        }

        /// <summary>Ids (and rows) of a table matching a simple where, read as the schedule's user (row scopes apply).</summary>
        static List<Dictionary<string, object>> Rows(string table, JsonObject where, DateTime today, int max = 500)
        {
            table = DataGuard.SafeIdentifier(table);
            var cols = DataGuard.Columns[table];
            var sql = new System.Text.StringBuilder($"SELECT {Db.Top(max)}* FROM {Db.Q(table)} WHERE isActive = 1");
            using var c = Db.Connection(DataGuard.ConnectionString);
            c.Open();
            using var cmd = Db.Command("", c);
            var i = 0;
            foreach (var (col, val) in where ?? new JsonObject())
            {
                if (!cols.Contains(col)) throw new JobException($"'{table}' has no column '{col}'.");
                var q = Db.Q(DataGuard.SafeIdentifier(col));
                object V(JsonNode n) => n?.ToString() is "today" ? today.Date : n?.ToString();
                if (val is JsonArray arr && arr.Count > 0)
                {
                    var names = arr.Select(v => { var p = "@w" + i++; cmd.Parameters.AddWithValue(p, V(v)); return p; });
                    sql.Append($" AND {q} IN ({string.Join(", ", names)})");
                }
                else if (val is JsonObject ops)
                    foreach (var (op, v) in ops)
                    {
                        var sqlOp = op switch { "<" => "<", "<=" => "<=", ">" => ">", ">=" => ">=", "!=" => "<>", _ => throw new JobException($"Unknown operator '{op}' (use <, <=, >, >=, !=).") };
                        var p = "@w" + i++;
                        cmd.Parameters.AddWithValue(p, V(v));
                        sql.Append($" AND {q} {sqlOp} {p}");
                    }
                else if (val == null) sql.Append($" AND {q} IS NULL");
                else { var p = "@w" + i++; cmd.Parameters.AddWithValue(p, V(val)); sql.Append($" AND {q} = {p}"); }
            }
            sql.Append(Db.LimitEnd(max));
            cmd.CommandText = sql.ToString();
            DataGuard.Apply(cmd);
            var rows = new List<Dictionary<string, object>>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var k = 0; k < r.FieldCount; k++) d[r.GetName(k)] = r.IsDBNull(k) ? null : r.GetValue(k);
                rows.Add(d);
            }
            return rows;
        }

        static string Render(string template, Dictionary<string, object> row, (string start, string end, string runDate) period) =>
            template == null ? null : Regex.Replace(template, @"\{(\w+)\}", m => m.Groups[1].Value switch
            {
                "periodStart" => period.start, "periodEnd" => period.end, "runDate" => period.runDate,
                var k => row != null && row.TryGetValue(k, out var v) ? Convert.ToString(v is DateTime dt ? dt.ToString("yyyy-MM-dd") : v, CultureInfo.InvariantCulture) : m.Value,
            });

        /// <summary>Does one run; returns what happened in a sentence.</summary>
        string Fire(Schedule s, DateTime dueUtc)
        {
            var period = Period(s, dueUtc);
            var (user, caller) = RunAs(s);
            var today = DateTime.ParseExact(period.runDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var meta = new JsonObject
            {
                ["id"] = s.Id, ["runAt"] = dueUtc.ToString("o"), ["periodStart"] = period.start, ["periodEnd"] = period.end,
                ["notify"] = s.Notify?.DeepClone(),
            };
            using var _ = DataGuard.RunAs(caller);
            using var scope = _services.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<JobHandler>().ToList();

            if (s.Remind != null) return Remind(s, period, today);

            if (s.Workflow != null)
            {
                var handler = handlers.First(h => h.Kind == "ai.workflow");
                var table = s.ForEach["table"]!.ToString();
                var pk = DataGuard.PrimaryKeys[table];
                var rows = Rows(table, s.ForEach["where"] as JsonObject, today);
                if (rows.Count == 0) return $"No {table} matched, nothing to run.";
                var started = 0; var skipped = new List<string>();
                foreach (var r in rows)
                {
                    var input = new JsonObject { ["workflow"] = s.Workflow, ["recordId"] = r[pk]?.ToString(), ["note"] = Render(s.Note, r, period) ?? $"Scheduled run for {period.start} to {period.end}.", ["schedule"] = meta.DeepClone() };
                    try
                    {
                        var title = handler.Validate(user, input);
                        input["schedule"]!["title"] = $"{title}, {period.start} to {period.end}";
                        var id = _store.Create(handler.Kind, $"{title} (scheduled)", user, caller.Tenant, input);
                        _queue.Enqueue(id);
                        started++;
                    }
                    catch (Exception ex) when (ex is JobException or DataGuardException or Ai.AiException) { skipped.Add($"{r[pk]}: {ex.Message}"); }
                }
                if (started == 0) throw new JobException("No workflow job could start: " + string.Join("; ", skipped));
                return $"Started {started} workflow job{(started == 1 ? "" : "s")} for {period.start} to {period.end}." + (skipped.Count > 0 ? " Skipped " + string.Join("; ", skipped) : "");
            }

            var kind = handlers.FirstOrDefault(h => string.Equals(h.Kind, s.Job, StringComparison.OrdinalIgnoreCase))
                ?? throw new JobException($"No job kind '{s.Job}' (a JobHandler in custom-api).");
            var jobInput = s.Input?.DeepClone() as JsonObject ?? new JsonObject();
            jobInput["schedule"] = meta;
            var jobTitle = kind.Validate(user, jobInput);
            meta["title"] = $"{jobTitle}, {period.start} to {period.end}";
            var jobId = _store.Create(kind.Kind, $"{jobTitle} (scheduled)", user, caller.Tenant, jobInput);
            _queue.Enqueue(jobId);
            return $"Started job {jobId} ({kind.Kind}).";
        }

        /// <summary>A reminder: notify the person behind each matching row, unless they already have a row for the day.</summary>
        string Remind(Schedule s, (string start, string end, string runDate) period, DateTime today)
        {
            var rm = s.Remind;
            var table = rm["table"]!.ToString();
            var pk = DataGuard.PrimaryKeys[table];
            var rows = Rows(table, rm["where"] as JsonObject, today);
            var unless = rm["unless"] as JsonObject;
            var userColumn = rm["userColumn"]?.ToString();
            var missing = new List<Dictionary<string, object>>();
            foreach (var r in rows)
            {
                if (unless != null)
                {
                    var t = unless["table"]!.ToString();
                    var col = unless["column"]?.ToString() ?? pk;
                    var where = new JsonObject { [col] = r[pk]?.ToString() };
                    if (unless["date"]?.ToString() is { } dc) where[dc] = unless["on"]?.ToString() == "periodStart" ? period.start : today.ToString("yyyy-MM-dd");
                    foreach (var (k, v) in unless["where"] as JsonObject ?? new JsonObject()) where[k] = v?.DeepClone();
                    if (Rows(t, where, today, 1).Count > 0) continue;
                }
                missing.Add(r);
            }
            var notified = 0; var noAccount = new List<string>();
            foreach (var r in missing)
            {
                if (userColumn == null) break;
                var who = r.GetValueOrDefault(userColumn)?.ToString();
                var users = DataGuard.UsersFor(who);
                if (users.Count == 0) { noAccount.Add(who ?? r[pk]?.ToString()); continue; }
                notified += DataGuard.NotifyUsers(users, Render(rm["title"]?.ToString() ?? "Reminder", r, period), table, r[pk]?.ToString(), actor: "schedule");
            }
            if (missing.Count > 0 && rm["summaryTo"] is JsonArray to)
            {
                var names = string.Join(", ", missing.Take(20).Select(r => Render("{" + (rm["label"]?.ToString() ?? userColumn ?? pk) + "}", r, period)));
                var title = Render(rm["summaryTitle"]?.ToString() ?? $"{missing.Count} without an entry on {{runDate}}: ", null, period) + names + (missing.Count > 20 ? "…" : "");
                var ids = new HashSet<int>();
                foreach (var w in to) if (w?.ToString() is { } x) ids.UnionWith(DataGuard.UsersFor(x));
                DataGuard.NotifyUsers(ids, title, table, null, actor: "schedule");
            }
            return $"{missing.Count} of {rows.Count} {table} needed a reminder; {notified} notified." +
                   (noAccount.Count > 0 ? $" No account for: {string.Join(", ", noAccount.Take(10))}." : "");
        }

        // ---------------------------------------------------------------- waking up

        /// <summary>Service Bus: makes sure exactly one wake-up is booked, at the next run (at most a day ahead).</summary>
        void Arm()
        {
            if (_queue.Bus == null) return;
            var now = Now();
            DateTime? earliest = null;
            using var c = _store.Open();
            using (var q = Db.Command($"SELECT MIN(next_run_at) FROM job_schedules WHERE schedule_id <> '{TickRow}' AND next_run_at IS NOT NULL", c))
                if (q.ExecuteScalar() is DateTime d) earliest = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            var target = earliest == null || earliest > now.AddDays(1) ? now.AddDays(1) : earliest.Value;
            if (target < now.AddSeconds(15)) target = now.AddSeconds(15);
            var wake = Read(c, TickRow);
            // already booked for that time (or earlier, still in the future): nothing to do
            if (wake?.NextRun is { } booked && booked > now && booked <= target.AddSeconds(30)) return;
            var seq = _queue.ScheduleTick(target);
            if (wake?.Seq is long old && wake.NextRun > now) _queue.CancelTick(old);
            if (wake == null) Upsert(c, TickRow, null, target, "booked", "Next wake-up booked on Service Bus.");
            Exec(c, $"UPDATE job_schedules SET next_run_at = @n, tick_seq = @s, last_status = 'booked', modifiedAt = {Db.UtcNow} WHERE schedule_id = '{TickRow}'",
                ("@n", target), ("@s", seq));
            _log.LogInformation("Scheduler: next wake-up {Target:o}", target);
        }

        /// <summary>A wake-up message arrived (Service Bus): run what's due and book the next one.</summary>
        public async Task OnTick(CancellationToken ct)
        {
            if (Load().Count == 0) return;
            await RunDue(ct);
            try { Arm(); } catch (Exception ex) { _log.LogError(ex, "Couldn't book the next scheduler wake-up"); }
        }

        /// <summary>For the Jobs status endpoint: every schedule with its state.</summary>
        public JsonArray Status()
        {
            var list = new JsonArray();
            var schedules = Load();
            if (schedules.Count == 0) return list;
            using var c = _store.Open();
            foreach (var s in schedules)
            {
                var st = s.Id == null ? null : Read(c, s.Id);
                list.Add(new JsonObject
                {
                    ["id"] = s.Id, ["error"] = s.Error, ["status"] = st?.Status, ["nextRun"] = st?.NextRun?.ToString("o"),
                    ["lastRun"] = st?.LastRun?.ToString("o"), ["runs"] = st?.Runs ?? 0,
                    ["lastMessage"] = Scalar(c, "SELECT last_message FROM job_schedules WHERE schedule_id = @id", s.Id),
                });
            }
            var wake = Read(c, TickRow);
            if (wake != null) list.Add(new JsonObject { ["id"] = TickRow, ["nextRun"] = wake.NextRun?.ToString("o") });
            return list;
        }

        static string Scalar(DbConnection c, string sql, string id)
        {
            using var q = Db.Command(sql, c);
            q.Parameters.AddWithValue("@id", id ?? "");
            return q.ExecuteScalar() as string;
        }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            if (string.Equals(_configuration["Jobs:Scheduler"], "false", StringComparison.OrdinalIgnoreCase)) return;
            while (!DataGuard.Ready && !stop.IsCancellationRequested) await Task.Delay(500, stop);
            if (stop.IsCancellationRequested) return;
            var schedules = Load();
            if (schedules.Count == 0) return;
            foreach (var bad in schedules.Where(s => s.Error != null)) _log.LogWarning("Schedule {Id} can't run: {Error}", bad.Id, bad.Error);
            if (!_queue.HasBroker) { _log.LogWarning("Schedules are off: no Azure Service Bus or RabbitMQ configured."); return; }

            if (_queue.Bus != null)
            {
                // serverless: run anything due now (catch-up after sleeping), then book the next wake-up; later
                // wake-ups arrive as queue messages (JobWorker -> OnTick)
                try { await OnTick(stop); }
                catch (Exception ex) when (!stop.IsCancellationRequested) { _log.LogError(ex, "Scheduler start failed"); }
                return;
            }
            var every = TimeSpan.FromSeconds(Math.Max(10, int.TryParse(_configuration["Jobs:SchedulerSeconds"], out var v) ? v : 60));
            _log.LogInformation("Scheduler: {Count} schedule(s), checking every {Seconds}s", schedules.Count, every.TotalSeconds);
            while (!stop.IsCancellationRequested)
            {
                try { await RunDue(stop); }
                catch (Exception ex) when (!stop.IsCancellationRequested) { _log.LogError(ex, "Scheduler check failed"); }
                try { await Task.Delay(every, stop); } catch (OperationCanceledException) { }
            }
        }
    }
}
