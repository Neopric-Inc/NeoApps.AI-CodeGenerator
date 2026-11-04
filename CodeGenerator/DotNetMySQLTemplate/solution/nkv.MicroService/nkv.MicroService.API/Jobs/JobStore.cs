using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Jobs and their event feed in MySQL (background_jobs, background_job_events: created on first use, not app tables).
    /// MySQL is the record; every change also pings Redis so open feeds update at once (<see cref="JobSignals"/>).
    /// Status flow: queued → running → (awaiting_approval → approved → running)* → completed | failed | cancelled | rejected.
    /// </summary>
    public sealed class JobStore
    {
        public static readonly string[] Terminal = { "completed", "failed", "cancelled", "rejected" };
        private static bool _schemaReady;
        private static readonly object SchemaLock = new();
        private readonly string _cs;
        private readonly JobSignals _signals;

        public JobStore(IConfiguration configuration, JobSignals signals)
        {
            _cs = configuration.GetConnectionString("MySQLDatabase");
            _signals = signals;
        }

        public MySqlConnection Open()
        {
            var c = new MySqlConnection(_cs);
            c.Open();
            if (!_schemaReady)
                lock (SchemaLock)
                    if (!_schemaReady)
                    {
                        Exec(c, @"CREATE TABLE IF NOT EXISTS background_jobs (job_id CHAR(32) NOT NULL PRIMARY KEY, kind VARCHAR(100) NOT NULL,
                            title VARCHAR(255) NULL, owner_name VARCHAR(255) NOT NULL, tenant_id INT NULL, user_id INT NOT NULL, username VARCHAR(255) NOT NULL,
                            status VARCHAR(30) NOT NULL, progress INT NULL, input JSON NULL, state JSON NULL, result JSON NULL, decision JSON NULL,
                            error TEXT NULL, attempts INT NOT NULL DEFAULT 0, createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            startedAt DATETIME NULL, finishedAt DATETIME NULL,
                            modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                            KEY ix_jobs_owner (owner_name, user_id, createdAt), KEY ix_jobs_status (status))");
                        Exec(c, @"CREATE TABLE IF NOT EXISTS background_job_events (event_id BIGINT AUTO_INCREMENT PRIMARY KEY, job_id CHAR(32) NOT NULL,
                            level VARCHAR(10) NOT NULL, message VARCHAR(1000) NOT NULL, progress INT NULL, data JSON NULL,
                            createdAt DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3), KEY ix_job_events (job_id, event_id))");
                        _schemaReady = true;
                    }
            return c;
        }

        private static void Exec(MySqlConnection c, string sql, params (string, object)[] ps)
        {
            using var cmd = Cmd(c, sql, ps);
            cmd.ExecuteNonQuery();
        }

        private static MySqlCommand Cmd(MySqlConnection c, string sql, params (string, object)[] ps)
        {
            var cmd = new MySqlCommand(sql, c);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            return cmd;
        }

        public string Create(string kind, string title, JobUser user, int? tenantId, JsonObject input)
        {
            var id = Guid.NewGuid().ToString("N");
            using var c = Open();
            Exec(c, @"INSERT INTO background_jobs (job_id, kind, title, owner_name, tenant_id, user_id, username, status, input, createdAt)
                      VALUES (@id, @k, @t, @o, @tn, @u, @un, 'queued', @in, UTC_TIMESTAMP())",
                ("@id", id), ("@k", kind), ("@t", Trim(title, 255)), ("@o", user.Owner), ("@tn", tenantId), ("@u", user.UserId),
                ("@un", user.Username), ("@in", input?.ToJsonString()));
            AddEvent(id, "info", "Queued.", 0, null);
            return id;
        }

        public JobRow Get(string id)
        {
            using var c = Open();
            using var cmd = Cmd(c, "SELECT * FROM background_jobs WHERE job_id=@id", ("@id", id));
            using var r = cmd.ExecuteReader();
            return r.Read() ? JobRow.From(r) : null;
        }

        /// <summary>Jobs the user may see: their own; administrators see everyone's in their workspace and tenant.</summary>
        public List<JobRow> List(JobUser user, int? tenantId, string status, int limit)
        {
            using var c = Open();
            using var cmd = Cmd(c, $@"SELECT * FROM background_jobs WHERE owner_name=@o AND tenant_id <=> @tn
                                      {(user.IsAdmin ? "" : "AND user_id=@u")} {(string.IsNullOrEmpty(status) ? "" : "AND status=@s")}
                                      ORDER BY createdAt DESC LIMIT @l",
                ("@o", user.Owner), ("@tn", tenantId), ("@u", user.UserId), ("@s", status), ("@l", Math.Clamp(limit, 1, 200)));
            using var r = cmd.ExecuteReader();
            var list = new List<JobRow>();
            while (r.Read()) list.Add(JobRow.From(r));
            return list;
        }

        public List<JsonObject> Events(string id, long after)
        {
            using var c = Open();
            using var cmd = Cmd(c, "SELECT event_id, level, message, progress, data, createdAt FROM background_job_events WHERE job_id=@id AND event_id>@a ORDER BY event_id LIMIT 500",
                ("@id", id), ("@a", after));
            using var r = cmd.ExecuteReader();
            var list = new List<JsonObject>();
            while (r.Read())
                list.Add(new JsonObject
                {
                    ["id"] = r.GetInt64(0),
                    ["level"] = r.GetString(1),
                    ["message"] = r.GetString(2),
                    ["progress"] = r.IsDBNull(3) ? null : r.GetInt32(3),
                    ["data"] = r.IsDBNull(4) ? null : JsonNode.Parse(r.GetString(4)),
                    ["at"] = DateTime.SpecifyKind(r.GetDateTime(5), DateTimeKind.Utc).ToString("o"),
                });
            return list;
        }

        public void AddEvent(string id, string level, string message, int? progress, JsonNode data)
        {
            using (var c = Open())
            {
                Exec(c, "INSERT INTO background_job_events (job_id, level, message, progress, data, createdAt) VALUES (@id, @l, @m, @p, @d, UTC_TIMESTAMP(3))",
                    ("@id", id), ("@l", level), ("@m", Trim(message, 1000)), ("@p", progress), ("@d", data?.ToJsonString()));
                if (progress != null) Exec(c, "UPDATE background_jobs SET progress=@p WHERE job_id=@id", ("@p", progress), ("@id", id));
            }
            _signals.Changed(id);
        }

        /// <summary>
        /// Moves a job from <paramref name="from"/> to <paramref name="to"/> atomically. False when it was no longer in
        /// <paramref name="from"/> (someone cancelled it, another worker claimed it), so the caller must not continue.
        /// </summary>
        public bool Transition(string id, string from, string to, JsonNode result = null, JsonNode decision = null, string error = null,
            bool started = false, bool finished = false, bool countAttempt = false)
        {
            using var c = Open();
            var sets = new List<string> { "status=@to" };
            if (result != null) sets.Add("result=@res");
            if (decision != null) sets.Add("decision=@dec");
            if (error != null) sets.Add("error=@err");
            if (started) sets.Add("startedAt=COALESCE(startedAt, UTC_TIMESTAMP())");
            if (finished) sets.Add("finishedAt=UTC_TIMESTAMP()");
            if (countAttempt) sets.Add("attempts=attempts+1");
            using var cmd = Cmd(c, $"UPDATE background_jobs SET {string.Join(", ", sets)} WHERE job_id=@id AND status=@from",
                ("@to", to), ("@res", result?.ToJsonString()), ("@dec", decision?.ToJsonString()), ("@err", Trim(error, 4000)), ("@id", id), ("@from", from));
            var ok = cmd.ExecuteNonQuery() == 1;
            if (ok) _signals.Changed(id);
            return ok;
        }

        public void SaveState(string id, JsonObject state)
        {
            using var c = Open();
            Exec(c, "UPDATE background_jobs SET state=@s WHERE job_id=@id", ("@s", state?.ToJsonString()), ("@id", id));
        }

        /// <summary>Role and active flag of the job's user now, so a job never outlives a removed account or a changed role.</summary>
        public (int roleId, bool active)? CurrentUser(int userId)
        {
            using var c = Open();
            using var cmd = Cmd(c, "SELECT role_id, isActive FROM users WHERE user_id=@u", ("@u", userId));
            using var r = cmd.ExecuteReader();
            return r.Read() ? (r.GetInt32(0), !r.IsDBNull(1) && Convert.ToInt32(r.GetValue(1)) == 1) : null;
        }

        private static string Trim(string s, int n) => s == null ? null : s.Length <= n ? s : s.Substring(0, n - 1) + "…";
    }

    public sealed class JobRow
    {
        public string JobId, Kind, Title, Owner, Username, Status, Error;
        public int? TenantId, Progress;
        public int UserId, Attempts;
        public JsonObject Input, State, Decision;
        public JsonNode Result;
        public DateTime CreatedAt;
        public DateTime? StartedAt, FinishedAt;

        public bool IsTerminal => JobStore.Terminal.Contains(Status);

        internal static JobRow From(MySqlDataReader r)
        {
            string S(string n) => r.IsDBNull(r.GetOrdinal(n)) ? null : r.GetString(n);
            int? I(string n) => r.IsDBNull(r.GetOrdinal(n)) ? null : r.GetInt32(n);
            DateTime? D(string n) => r.IsDBNull(r.GetOrdinal(n)) ? null : DateTime.SpecifyKind(r.GetDateTime(n), DateTimeKind.Utc);
            JsonNode J(string n) => S(n) is { } s ? JsonNode.Parse(s) : null;
            return new JobRow
            {
                JobId = S("job_id"), Kind = S("kind"), Title = S("title"), Owner = S("owner_name"), Username = S("username"),
                Status = S("status"), Error = S("error"), TenantId = I("tenant_id"), Progress = I("progress"), UserId = r.GetInt32("user_id"),
                Attempts = r.GetInt32("attempts"), Input = J("input") as JsonObject, State = J("state") as JsonObject,
                Decision = J("decision") as JsonObject, Result = J("result"), CreatedAt = D("createdAt") ?? DateTime.UtcNow,
                StartedAt = D("startedAt"), FinishedAt = D("finishedAt"),
            };
        }

        /// <summary>The job as clients see it. The result (e.g. a proposal) is included only with <paramref name="withResult"/>.</summary>
        public JsonObject ToJson(bool withResult) => new()
        {
            ["job_id"] = JobId, ["kind"] = Kind, ["title"] = Title, ["status"] = Status, ["progress"] = Progress,
            ["username"] = Username, ["error"] = Error, ["attempts"] = Attempts,
            ["createdAt"] = CreatedAt.ToString("o"), ["startedAt"] = StartedAt?.ToString("o"), ["finishedAt"] = FinishedAt?.ToString("o"),
            ["input"] = Input?.DeepClone(),
            ["result"] = withResult ? Result?.DeepClone() : null,
        };
    }
}
