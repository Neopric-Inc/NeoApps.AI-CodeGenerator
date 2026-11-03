using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>Raised when a request breaks a data rule (tenant isolation, read-only data, lifecycle...). Returned as 403/409.</summary>
    public class DataGuardException : Exception
    {
        public int StatusCode { get; }
        public DataGuardException(string message, int statusCode = 403) : base(message) { StatusCode = statusCode; }
    }

    /// <summary>
    /// Single choke point every generated query passes through right before it executes (DataGuard.Apply(cmd)).
    /// Rules come from ui.config.json and the live schema, so they apply to every generated table without per-table code:
    ///  - tenancy: rows of tables that have the tenant column are filtered to the caller's tenant (taken only from the signed token),
    ///    the tenant column is forced on insert/update, foreign keys may not point at another tenant's rows, and tables without the
    ///    tenant column are read-only for tenant users. Users with no tenant are platform users and work across tenants.
    ///  - workspace: rows are always limited to the caller's workspace owner, including updates and deletes.
    /// Commands run outside an authenticated request (startup, migrations, background jobs) are not rewritten.
    /// </summary>
    public static partial class DataGuard
    {
        public const string TenantClaim = "neoapps:tenant";
        public static IHttpContextAccessor Accessor { get; private set; }
        public static bool Ready { get; private set; }
        public static string ConnectionString { get; private set; }
        public static JsonObject Config { get; private set; } = new JsonObject();

        // tenancy
        public static bool TenancyEnabled { get; private set; }
        public static string TenantColumn { get; private set; } = "tenant_id";
        public static string TenantTable { get; private set; }
        public static string TenantTablePk { get; private set; }
        static HashSet<string> _sharedWritable = new(StringComparer.OrdinalIgnoreCase);

        // schema metadata
        public static readonly HashSet<string> AppTables = new(StringComparer.OrdinalIgnoreCase);
        public static readonly HashSet<string> TenantTables = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, HashSet<string>> Columns = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, string> PrimaryKeys = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, List<(string Column, string RefTable, string RefColumn)>> ForeignKeys = new(StringComparer.OrdinalIgnoreCase);

        static readonly HashSet<string> BuiltIn = new(StringComparer.OrdinalIgnoreCase)
        {
            "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders", "dnd_ui_versions",
            "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds", "workflow_runs",
            "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions", "ai_files", "ai_chunks", "ai_conversations",
            "ai_messages", "ai_runs", "versioninfo", "user_tenants", "guard_audit", "guard_outbox"
        };

        static readonly ConditionalWeakTable<MySqlCommand, object> _applied = new();

        /// <summary>Loads the rules (ui.config.json next to the API) and the schema metadata. Call once at startup.</summary>
        public static void Init(IHttpContextAccessor accessor, string connectionString, string contentRoot)
        {
            Accessor = accessor;
            ConnectionString = connectionString;
            try
            {
                var file = Path.Combine(contentRoot ?? AppContext.BaseDirectory, "ui.config.json");
                if (File.Exists(file)) Config = JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject();
            }
            catch (Exception ex) { Console.WriteLine("DataGuard: could not read ui.config.json: " + ex.Message); }

            if (Config["tenancy"] is JsonObject t)
            {
                TenancyEnabled = t["enabled"]?.GetValue<bool>() ?? true;
                TenantColumn = t["column"]?.GetValue<string>() ?? "tenant_id";
                TenantTable = t["tenantTable"]?.GetValue<string>();
                if (t["sharedWritable"] is JsonArray sw) _sharedWritable = new(sw.Select(x => x!.GetValue<string>()), StringComparer.OrdinalIgnoreCase);
            }

            using var c = new MySqlConnection(connectionString);
            c.Open();
            using (var cmd = new MySqlCommand(@"CREATE TABLE IF NOT EXISTS user_tenants (user_id INT NOT NULL PRIMARY KEY, tenant_id INT NULL,
                createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)", c)) cmd.ExecuteNonQuery();
            AddUserColumns(c);
            LoadSchema(c);
            if (TenancyEnabled) UpgradeForTenancy(c);
            LoadScopes();
            InitExtensions(c);
            Ready = true;
            Console.WriteLine($"DataGuard: {AppTables.Count} app tables, tenancy {(TenancyEnabled ? $"on ({TenantTables.Count} tenant tables, column {TenantColumn})" : "off")}.");
        }

        public static int UnassignedMembers { get; private set; }

        /// <summary>
        /// Upgrading an existing app to tenancy: tag AI files and their text with the tenant of the record they are attached
        /// to (deterministic, repeatable), and count members that still need a tenant (they get no data access until assigned).
        /// </summary>
        static void UpgradeForTenancy(MySqlConnection c)
        {
            static long Scalar(MySqlConnection c, string sql, params (string, object)[] ps)
            {
                using var q = new MySqlCommand(sql, c);
                foreach (var (n, v) in ps) q.Parameters.AddWithValue(n, v);
                var v2 = q.ExecuteScalar();
                return v2 == null || v2 is DBNull ? 0 : Convert.ToInt64(v2);
            }
            static void Exec(MySqlConnection c, string sql, params (string, object)[] ps)
            {
                using var q = new MySqlCommand(sql, c);
                foreach (var (n, v) in ps) q.Parameters.AddWithValue(n, v);
                q.ExecuteNonQuery();
            }
            foreach (var t in new[] { "ai_files", "ai_chunks" })
                if (Scalar(c, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @t", ("@t", t)) > 0 &&
                    Scalar(c, "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @t AND column_name = 'tenant_id'", ("@t", t)) == 0)
                    Exec(c, $"ALTER TABLE {t} ADD COLUMN tenant_id INT NULL");
            if (Scalar(c, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'ai_files'") > 0)
            {
                var tagged = 0L;
                var tables = new List<string>();
                using (var q = new MySqlCommand("SELECT DISTINCT LOWER(table_name) FROM ai_files WHERE tenant_id IS NULL", c))
                using (var r = q.ExecuteReader()) while (r.Read()) tables.Add(r.GetString(0));
                foreach (var t in tables.Where(t => TenantTables.Contains(t) && PrimaryKeys.ContainsKey(t)))
                {
                    using var u = new MySqlCommand($@"UPDATE ai_files f JOIN `{t}` r ON CAST(r.`{PrimaryKeys[t]}` AS CHAR) = f.record_id
                        SET f.tenant_id = r.`{TenantColumn}` WHERE f.table_name = @t AND f.tenant_id IS NULL", c);
                    u.Parameters.AddWithValue("@t", t);
                    tagged += u.ExecuteNonQuery();
                }
                if (Scalar(c, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'ai_chunks'") > 0)
                    Exec(c, "UPDATE ai_chunks k JOIN ai_files f ON f.file_id = k.file_id SET k.tenant_id = f.tenant_id WHERE k.tenant_id IS NULL AND f.tenant_id IS NOT NULL");
                if (tagged > 0) Console.WriteLine($"DataGuard: tagged {tagged} existing AI files with their record's tenant.");
            }
            UnassignedMembers = (int)Scalar(c, @"SELECT COUNT(*) FROM users u LEFT JOIN user_tenants ut ON ut.user_id = u.user_id
                WHERE ut.user_id IS NULL AND u.role_id <> @admin AND u.isActive = 1", ("@admin", AdministratorRoleId));
            if (UnassignedMembers > 0)
                Console.WriteLine($"DataGuard: WARNING {UnassignedMembers} member(s) have no tenant and can't access data until assigned (Admin > Members).");
        }

        // ------------------------------------------------------------------ who really did it
        // createdBy/modifiedBy hold the workspace owner (rows are owner-scoped by it). created_by_user/modified_by_user record
        // the person (or "<user> via AI", "system") who created / last changed the row. Added to every app table at startup,
        // stamped on every write here and never taken from the client.
        public const string CreatedByUser = "created_by_user", ModifiedByUser = "modified_by_user";

        static void AddUserColumns(MySqlConnection c)
        {
            var missing = new List<(string table, string col)>();
            using (var q = new MySqlCommand(@"SELECT t.table_name, x.col FROM information_schema.columns t
                    JOIN (SELECT 'created_by_user' col UNION ALL SELECT 'modified_by_user') x
                    WHERE t.table_schema = DATABASE() AND LOWER(t.column_name) = 'createdby'
                      AND NOT EXISTS (SELECT 1 FROM information_schema.columns k WHERE k.table_schema = t.table_schema
                                      AND k.table_name = t.table_name AND LOWER(k.column_name) = x.col)", c))
            using (var r = q.ExecuteReader()) while (r.Read()) if (!BuiltIn.Contains(r.GetString(0))) missing.Add((r.GetString(0), r.GetString(1)));
            foreach (var (table, col) in missing)
                using (var a = new MySqlCommand($"ALTER TABLE `{SafeIdentifier(table)}` ADD COLUMN `{col}` VARCHAR(255) NULL", c)) a.ExecuteNonQuery();
            if (missing.Count > 0) Console.WriteLine($"DataGuard: added {CreatedByUser}/{ModifiedByUser} to {missing.Select(m => m.table).Distinct().Count()} table(s).");
        }

        /// <summary>The person behind a write, as stored in created_by_user / modified_by_user.</summary>
        public static string ActorName(Caller caller) =>
            caller == null ? "system" : caller.IsSystem ? "system" : caller.IsAi ? $"{caller.User} via AI" : caller.User;

        static bool HasCol(string table, string col) => Columns.TryGetValue(table, out var cs) && cs.Contains(col);

        static void StampInsert(MySqlCommand cmd, Caller caller, string table, List<string> cols, List<string> exprs, Dictionary<string, object> values)
        {
            var actor = ActorName(caller);
            var missing = false;
            foreach (var col in new[] { CreatedByUser, ModifiedByUser })
            {
                if (!HasCol(table, col)) continue;
                var i = cols.IndexOf(col);
                if (i >= 0 && i < exprs.Count && exprs[i].Trim().StartsWith("@")) { Bind(cmd, exprs[i].Trim(), actor); values[col] = actor; }
                else missing = true;
            }
            if (missing && PrimaryKeys.TryGetValue(table, out var pk))
                After(cmd, (c, _) =>
                {
                    using var u = new MySqlCommand($"UPDATE `{table}` SET `{CreatedByUser}` = @__u, `{ModifiedByUser}` = @__u WHERE `{pk}` = @__id", c.Connection, c.Transaction);
                    u.Parameters.AddWithValue("@__u", actor); u.Parameters.AddWithValue("@__id", c.LastInsertedId);
                    u.ExecuteNonQuery();
                });
        }

        /// <summary>created_by_user can't be changed; modified_by_user is set to the caller.</summary>
        static void StampUpdate(MySqlCommand cmd, Caller caller, string table, int afterSet, Dictionary<string, object> values)
        {
            if (!HasCol(table, ModifiedByUser)) return;
            var head = cmd.CommandText.Substring(0, afterSet);
            var tail = Regex.Replace(cmd.CommandText.Substring(afterSet), $@"`?\b{CreatedByUser}\b`?\s*=\s*@\w+", $"`{CreatedByUser}` = `{CreatedByUser}`", RegexOptions.IgnoreCase);
            cmd.CommandText = head + tail;
            values.Remove(CreatedByUser); values.Remove(ModifiedByUser);
            var actor = ActorName(caller);
            var pm = Regex.Match(tail, $@"`?\b{ModifiedByUser}\b`?\s*=\s*(@\w+)", RegexOptions.IgnoreCase);
            if (pm.Success) { Bind(cmd, pm.Groups[1].Value, actor); return; }
            var id = KeyValue(cmd, table, values);
            if (id == null || !PrimaryKeys.TryGetValue(table, out var pk)) return;
            After(cmd, (c, _) =>
            {
                using var u = new MySqlCommand($"UPDATE `{table}` SET `{ModifiedByUser}` = @__u WHERE `{pk}` = @__id", c.Connection, c.Transaction);
                u.Parameters.AddWithValue("@__u", actor); u.Parameters.AddWithValue("@__id", id);
                u.ExecuteNonQuery();
            });
        }

        public static void LoadSchema(MySqlConnection c)
        {
            AppTables.Clear(); TenantTables.Clear(); Columns.Clear(); PrimaryKeys.Clear(); ForeignKeys.Clear();
            using (var cmd = new MySqlCommand(@"SELECT LOWER(table_name), LOWER(column_name), column_key FROM information_schema.columns
                WHERE table_schema = DATABASE() ORDER BY table_name, ordinal_position", c))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    var table = r.GetString(0); var col = r.GetString(1);
                    if (BuiltIn.Contains(table)) continue;
                    if (!Columns.TryGetValue(table, out var set)) Columns[table] = set = new(StringComparer.OrdinalIgnoreCase);
                    set.Add(col);
                    if (r.GetString(2) == "PRI" && !PrimaryKeys.ContainsKey(table)) PrimaryKeys[table] = col;
                }
            foreach (var (table, cols) in Columns)
            {
                if (!cols.Contains("createdby")) continue; // only generated app tables
                AppTables.Add(table);
                if (TenancyEnabled && cols.Contains(TenantColumn)) TenantTables.Add(table);
            }
            if (TenancyEnabled && TenantTable != null && PrimaryKeys.TryGetValue(TenantTable, out var tpk)) TenantTablePk = tpk;
            using (var cmd = new MySqlCommand(@"SELECT LOWER(table_name), LOWER(column_name), LOWER(referenced_table_name), LOWER(referenced_column_name)
                FROM information_schema.key_column_usage WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL", c))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    if (!ForeignKeys.TryGetValue(r.GetString(0), out var list)) ForeignKeys[r.GetString(0)] = list = new();
                    list.Add((r.GetString(1), r.GetString(2), r.GetString(3)));
                }
        }

        // ------------------------------------------------------------------ caller

        public sealed class Caller
        {
            public string User;        // login name
            public int UserId;
            public int RoleId;
            public bool IsAi;          // request came through the AI assistant/workflows
            public bool IsSystem;      // background job
            public bool Unassigned;    // tenancy on, but this account has no tenant mapping yet: no data access
            public string Owner;       // workspace owner (ClaimTypes.Actor)
            public int? Tenant;        // null = platform user
            public string Ip;
            public string CorrelationId;
            public bool IsPlatform => Tenant == null;
        }

        /// <summary>The authenticated caller of the current request, or null outside a request.</summary>
        public static Caller Current
        {
            get
            {
                var ctx = Accessor?.HttpContext;
                if (ctx?.User?.Identity?.IsAuthenticated != true) return null;
                if (ctx.Items.TryGetValue("DataGuard.Caller", out var cached)) return (Caller)cached;
                var u = ctx.User;
                var caller = new Caller
                {
                    User = u.FindFirst(ClaimTypes.Name)?.Value,
                    UserId = int.TryParse(u.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0,
                    RoleId = int.TryParse(u.FindFirst(ClaimTypes.Role)?.Value, out var rid) ? rid : 0,
                    IsAi = Regex.IsMatch(ctx.Request.Path.Value ?? "", @"/api/Ai(/|$)", RegexOptions.IgnoreCase), // the AI controller only, not tables named ai_*
                    Owner = u.FindFirst(ClaimTypes.Actor)?.Value,

                    Ip = ctx.Connection?.RemoteIpAddress?.ToString(),
                    CorrelationId = ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) && cid.Count > 0 ? cid[0] : ctx.TraceIdentifier,
                };
                if (TenancyEnabled)
                {
                    // resolved from user_tenants on every request (short cache), never from the token or the request,
                    // so remapping a user takes effect at once and tokens issued before tenancy can't keep platform access
                    var (tenant, unassigned) = ResolveTenant(caller.UserId, caller.RoleId);
                    caller.Tenant = tenant; caller.Unassigned = unassigned;
                }
                ctx.Items["DataGuard.Caller"] = caller;
                return caller;
            }
        }

        const int AdministratorRoleId = 1;
        static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (int? tenant, bool unassigned, DateTime at)> _tenantCache = new();

        /// <summary>
        /// Tenant of a login user. A user_tenants row decides (tenant_id NULL = platform user). Without a row, only an
        /// Administrator (the workspace owner) is a platform user; everyone else is unassigned and gets no data access,
        /// so turning tenancy on for an existing app never turns its members into platform users.
        /// </summary>
        public static (int? tenant, bool unassigned) ResolveTenant(int userId, int roleId)
        {
            if (!TenancyEnabled || ConnectionString == null) return (null, false);
            if (_tenantCache.TryGetValue(userId, out var hit) && (DateTime.UtcNow - hit.at).TotalSeconds < 30) return (hit.tenant, hit.unassigned);
            using var c = new MySqlConnection(ConnectionString);
            c.Open();
            using var cmd = new MySqlCommand("SELECT tenant_id FROM user_tenants WHERE user_id = @u", c);
            cmd.Parameters.AddWithValue("@u", userId);
            (int? tenant, bool unassigned) result;
            using (var r = cmd.ExecuteReader())
                result = r.Read() ? (r.IsDBNull(0) ? null : r.GetInt32(0), false) : (null, roleId != AdministratorRoleId);
            _tenantCache[userId] = (result.tenant, result.unassigned, DateTime.UtcNow);
            return result;
        }

        /// <summary>Informational tenant claim for tokens: tenant id, "" for platform users, "unassigned".</summary>
        public static string TenantClaimValue(int userId, int roleId)
        {
            var (t, unassigned) = ResolveTenant(userId, roleId);
            return unassigned ? "unassigned" : t?.ToString() ?? "";
        }

        public static void SetTenantOfUser(MySqlConnection c, int userId, int? tenantId)
        {
            _tenantCache.TryRemove(userId, out _);
            using var cmd = new MySqlCommand(@"INSERT INTO user_tenants (user_id, tenant_id) VALUES (@u, @t)
                ON DUPLICATE KEY UPDATE tenant_id = VALUES(tenant_id)", c);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@t", (object)tenantId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // ------------------------------------------------------------------ the choke point

        static readonly Regex SelectFrom = new(@"\bFROM\s+`?(\w+)`?(?:\s+(?:AS\s+)?`?(\w+)`?)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex UpdateHead = new(@"^\s*UPDATE\s+`?(\w+)`?\s+SET\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex InsertHead = new(@"^\s*INSERT\s+INTO\s+`?(\w+)`?\s*\(([^)]*)\)\s*VALUES\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex DeleteHead = new(@"^\s*DELETE\s+FROM\s+`?(\w+)`?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly HashSet<string> NotAlias = new(StringComparer.OrdinalIgnoreCase) { "where", "order", "group", "limit", "join", "left", "right", "inner", "outer", "cross", "having", "for", "union", "on" };

        /// <summary>Rewrites/validates the command for the current caller. Call right before Execute*. Idempotent.</summary>
        public static MySqlCommand Apply(MySqlCommand cmd)
        {
            if (!Ready || cmd == null || _applied.TryGetValue(cmd, out _)) return cmd;
            var caller = Current;
            if (caller == null) return cmd;
            if (caller.Unassigned && TouchesData(cmd.CommandText))
                throw new DataGuardException("Your account isn't assigned to a tenant yet. Ask a platform administrator to assign you.", 403);
            _applied.Add(cmd, null);

            var sql = cmd.CommandText ?? "";
            Match m;
            if ((m = InsertHead.Match(sql)).Success) GuardInsert(cmd, caller, m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Value);
            else if ((m = UpdateHead.Match(sql)).Success) GuardUpdate(cmd, caller, m.Groups[1].Value.ToLowerInvariant(), m.Index + m.Length);
            else if ((m = DeleteHead.Match(sql)).Success) GuardDelete(cmd, caller, m.Groups[1].Value.ToLowerInvariant(), m.Index + m.Length);
            else if (Regex.IsMatch(sql, @"^\s*SELECT\b", RegexOptions.IgnoreCase) && (m = SelectFrom.Match(sql)).Success)
            {
                var hasAlias = m.Groups[2].Success && !NotAlias.Contains(m.Groups[2].Value);
                // without an alias the optional group may have swallowed a keyword (FROM t WHERE ...): continue right after the table name
                GuardSelect(cmd, caller, m.Groups[1].Value.ToLowerInvariant(), hasAlias ? m.Groups[2].Value : null,
                            hasAlias ? m.Index + m.Length : m.Groups[1].Index + m.Groups[1].Length + (sql.Length > m.Groups[1].Index + m.Groups[1].Length && sql[m.Groups[1].Index + m.Groups[1].Length] == '`' ? 1 : 0));
            }
            return cmd;
        }

        static readonly Regex AnyTable = new(@"\b(?:FROM|JOIN|UPDATE|INTO)\s+`?(\w+)`?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static bool TouchesData(string sql) =>
            AnyTable.Matches(sql ?? "").Any(m => AppTables.Contains(m.Groups[1].Value) || TaggedBuiltIns.Contains(m.Groups[1].Value));

        static List<string> ScopeConditions(string table, string prefix, Caller caller, MySqlCommand cmd)
        {
            var conds = new List<string>();
            if (caller.Owner != null && Columns[table].Contains("createdby"))
            {
                conds.Add($"{prefix}createdBy = @__g_owner");
                Bind(cmd, "@__g_owner", caller.Owner);
            }
            if (caller.Tenant != null)
            {
                if (TenantTables.Contains(table)) conds.Add($"{prefix}`{TenantColumn}` = @__g_tenant");
                else if (table.Equals(TenantTable, StringComparison.OrdinalIgnoreCase) && TenantTablePk != null) conds.Add($"{prefix}`{TenantTablePk}` = @__g_tenant");
                if (conds.Count > 0) Bind(cmd, "@__g_tenant", caller.Tenant.Value);
            }
            conds.AddRange(RowScopeConditions(table, prefix, caller, cmd));
            return conds;
        }

        /// <summary>Built-in tables that carry a tenant_id tag (AI files and their text) and are filtered like tenant tables.</summary>
        static readonly HashSet<string> TaggedBuiltIns = new(StringComparer.OrdinalIgnoreCase) { "ai_files", "ai_chunks" };

        /// <summary>True when the caller may see this active record (workspace + tenant rules).</summary>
        public static bool RecordVisible(MySqlConnection c, string table, string id)
        {
            table = (table ?? "").ToLowerInvariant();
            if (!AppTables.Contains(table) || !PrimaryKeys.TryGetValue(table, out var pk)) return false;
            using var q = new MySqlCommand($"SELECT COUNT(*) FROM `{table}` t WHERE t.`{pk}` = @id AND t.isActive = 1", c);
            q.Parameters.AddWithValue("@id", id);
            return Convert.ToInt64(Apply(q).ExecuteScalar()) > 0;
        }

        static void GuardSelect(MySqlCommand cmd, Caller caller, string table, string alias, int afterFrom)
        {
            if (TaggedBuiltIns.Contains(table))
            {
                var fc = new List<string>();
                if (caller.Tenant != null) { Bind(cmd, "@__g_tenant", caller.Tenant.Value); fc.Add($"{alias ?? table}.tenant_id = @__g_tenant"); }
                fc.AddRange(RowScopeFileConditions(table, alias ?? table, caller, cmd));
                if (fc.Count > 0) cmd.CommandText = InjectWhere(cmd.CommandText, afterFrom, string.Join(" AND ", fc));
                return;
            }
            if (!AppTables.Contains(table)) return;
            var conds = ScopeConditions(table, (alias ?? $"`{table}`") + ".", caller, cmd);
            if (conds.Count > 0) cmd.CommandText = InjectWhere(cmd.CommandText, afterFrom, string.Join(" AND ", conds));
        }

        static void GuardUpdate(MySqlCommand cmd, Caller caller, string table, int afterSet)
        {
            if (!AppTables.Contains(table)) return;
            CheckWritable(table, caller);
            var setPart = cmd.CommandText.Substring(afterSet);
            // tenant column can't be moved to another tenant
            if (caller.Tenant != null && TenantTables.Contains(table))
                foreach (Match pm in Regex.Matches(setPart, $@"`?\b{Regex.Escape(TenantColumn)}\b`?\s*=\s*(@\w+)", RegexOptions.IgnoreCase))
                    Bind(cmd, pm.Groups[1].Value, caller.Tenant.Value);
            var values = SetAssignments(setPart, cmd);
            StampUpdate(cmd, caller, table, afterSet, values);
            GuardScopedWrite(cmd, caller, table, values, col =>
            {
                var pm = Regex.Match(setPart, $@"`?\b{Regex.Escape(col)}\b`?\s*=\s*(@\w+)", RegexOptions.IgnoreCase);
                return pm.Success ? pm.Groups[1].Value : null;
            }, insert: false);
            CheckForeignKeys(cmd, caller, table, values);
            BeforeUpdate(cmd, caller, table, values);
            var conds = ScopeConditions(table, "", caller, cmd);
            if (conds.Count > 0) cmd.CommandText = InjectWhere(cmd.CommandText, afterSet, string.Join(" AND ", conds));
        }

        static void GuardDelete(MySqlCommand cmd, Caller caller, string table, int afterTable)
        {
            if (!AppTables.Contains(table)) return;
            CheckWritable(table, caller);
            BeforeDelete(cmd, caller, table);
            var conds = ScopeConditions(table, "", caller, cmd);
            if (conds.Count > 0) cmd.CommandText = InjectWhere(cmd.CommandText, afterTable, string.Join(" AND ", conds));
        }

        static void GuardInsert(MySqlCommand cmd, Caller caller, string table, string columnList)
        {
            if (!AppTables.Contains(table)) return;
            CheckWritable(table, caller);
            var cols = columnList.Split(',').Select(x => x.Trim().Trim('`').ToLowerInvariant()).ToList();
            var valuesStart = cmd.CommandText.IndexOf('(', InsertHead.Match(cmd.CommandText).Groups[2].Index + columnList.Length + 1);
            var exprs = SplitTopLevel(ExtractParens(cmd.CommandText, valuesStart));
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < cols.Count && i < exprs.Count; i++)
            {
                var e = exprs[i].Trim();
                if (caller.Tenant != null && cols[i] == TenantColumn.ToLowerInvariant() && TenantTables.Contains(table))
                {
                    if (e.StartsWith("@")) Bind(cmd, e, caller.Tenant.Value);
                    else throw new DataGuardException($"{table}.{TenantColumn} must be a parameter");
                }
                values[cols[i]] = e.StartsWith("@") && cmd.Parameters.Contains(e) ? cmd.Parameters[e].Value : (object)e;
            }
            if (caller.Owner != null && cols.Contains("createdby"))
            {
                var i = cols.IndexOf("createdby");
                if (i < exprs.Count && exprs[i].Trim().StartsWith("@")) Bind(cmd, exprs[i].Trim(), caller.Owner);
            }
            if (caller.Tenant != null && TenantTables.Contains(table) && !cols.Contains(TenantColumn.ToLowerInvariant()))
                throw new DataGuardException($"{table} rows must include {TenantColumn}");
            StampInsert(cmd, caller, table, cols, exprs, values);
            GuardScopedWrite(cmd, caller, table, values, col =>
            {
                var i = cols.IndexOf(col.ToLowerInvariant());
                return i >= 0 && i < exprs.Count && exprs[i].Trim().StartsWith("@") ? exprs[i].Trim() : null;
            }, insert: true);
            CheckForeignKeys(cmd, caller, table, values);
            BeforeInsert(cmd, caller, table, values);
        }

        static void CheckWritable(string table, Caller caller)
        {
            if (caller.Tenant == null) return;
            if (TenantTables.Contains(table) || _sharedWritable.Contains(table)) return;
            throw new DataGuardException($"{table} is shared across tenants and is read-only for tenant users.");
        }

        /// <summary>Every foreign key of a written row must point at a row of the same tenant (composite tenant integrity).</summary>
        static void CheckForeignKeys(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values)
        {
            if (!TenancyEnabled || !ForeignKeys.TryGetValue(table, out var fks)) return;
            object rowTenant = caller.Tenant;
            if (rowTenant == null && values.TryGetValue(TenantColumn, out var vt)) rowTenant = vt;
            if (rowTenant == null || rowTenant is DBNull) return;
            foreach (var (col, refTable, refCol) in fks)
            {
                if (!values.TryGetValue(col, out var v) || v == null || v is DBNull || (v is string s && (s.Length == 0 || s.StartsWith("@")))) continue;
                string check = null;
                if (refTable.Equals(TenantTable, StringComparison.OrdinalIgnoreCase))
                {
                    if (TenantTables.Contains(table) && col.Equals(TenantColumn, StringComparison.OrdinalIgnoreCase)) continue;
                    if (Convert.ToString(v) != Convert.ToString(rowTenant)) throw new DataGuardException($"{table}.{col} points at another tenant.");
                    continue;
                }
                if (TenantTables.Contains(refTable)) check = $"SELECT COUNT(*) FROM `{refTable}` WHERE `{refCol}` = @v AND `{TenantColumn}` = @t";
                if (check == null) continue;
                using var q = new MySqlCommand(check, cmd.Connection, cmd.Transaction);
                q.Parameters.AddWithValue("@v", v);
                q.Parameters.AddWithValue("@t", rowTenant);
                if (Convert.ToInt64(q.ExecuteScalar()) == 0)
                    throw new DataGuardException($"{table}.{col} = {v} does not exist in your tenant ({refTable}).");
            }
        }

        // ------------------------------------------------------------------ SQL helpers

        public static void Bind(MySqlCommand cmd, string name, object value)
        {
            if (cmd.Parameters.Contains(name)) cmd.Parameters[name].Value = value ?? DBNull.Value;
            else cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        /// <summary>Column -> bound value for "a=@a, b=@b" (only parameter assignments).</summary>
        static Dictionary<string, object> SetAssignments(string setPart, MySqlCommand cmd)
        {
            var end = FindTopLevel(setPart, 0, @"\bWHERE\b");
            var assignments = SplitTopLevel(end < 0 ? setPart : setPart.Substring(0, end));
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in assignments)
            {
                var m = Regex.Match(a, @"^\s*`?(\w+)`?\s*=\s*(.+?)\s*$", RegexOptions.Singleline);
                if (!m.Success) continue;
                var e = m.Groups[2].Value;
                d[m.Groups[1].Value.ToLowerInvariant()] = e.StartsWith("@") && cmd.Parameters.Contains(e) ? cmd.Parameters[e].Value : (object)e;
            }
            return d;
        }

        /// <summary>Adds "cond AND (existing where)" after position `from`, or a new WHERE before ORDER/GROUP/LIMIT.</summary>
        public static string InjectWhere(string sql, int from, string cond)
        {
            var w = FindTopLevel(sql, from, @"\bWHERE\b");
            if (w >= 0)
            {
                var restStart = w + 5;
                var end = FindTopLevel(sql, restStart, @"\b(ORDER\s+BY|GROUP\s+BY|LIMIT|HAVING|FOR\s+UPDATE)\b");
                var rest = (end < 0 ? sql.Substring(restStart) : sql.Substring(restStart, end - restStart)).Trim().TrimEnd(';');
                var tail = end < 0 ? "" : " " + sql.Substring(end);
                return sql.Substring(0, w) + "WHERE " + cond + (rest.Length > 0 ? " AND (" + rest + ")" : "") + tail;
            }
            var e = FindTopLevel(sql, from, @"\b(ORDER\s+BY|GROUP\s+BY|LIMIT|HAVING)\b");
            var body = (e < 0 ? sql : sql.Substring(0, e)).TrimEnd().TrimEnd(';');
            return body + " WHERE " + cond + (e < 0 ? "" : " " + sql.Substring(e));
        }

        /// <summary>Index of the first match of pattern at paren depth 0 and outside quotes, starting at `from`; -1 if none.</summary>
        static int FindTopLevel(string sql, int from, string pattern)
        {
            var re = new Regex(@"\G" + pattern, RegexOptions.IgnoreCase);
            int depth = 0; char quote = '\0';
            for (var i = from; i < sql.Length; i++)
            {
                var ch = sql[i];
                if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
                if (ch == '\'' || ch == '"' || ch == '`') { quote = ch; continue; }
                if (ch == '(') { depth++; continue; }
                if (ch == ')') { depth--; continue; }
                if (depth == 0 && (i == 0 || !char.IsLetterOrDigit(sql[i - 1]) && sql[i - 1] != '_') && re.IsMatch(sql, i)) return i;
            }
            return -1;
        }

        static string ExtractParens(string sql, int open)
        {
            int depth = 0; char quote = '\0';
            for (var i = open; i < sql.Length; i++)
            {
                var ch = sql[i];
                if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
                if (ch == '\'' || ch == '"' || ch == '`') { quote = ch; continue; }
                if (ch == '(') depth++;
                else if (ch == ')' && --depth == 0) return sql.Substring(open + 1, i - open - 1);
            }
            return sql.Substring(open + 1);
        }

        static List<string> SplitTopLevel(string s)
        {
            var parts = new List<string>(); int depth = 0, start = 0; char quote = '\0';
            for (var i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
                if (ch == '\'' || ch == '"' || ch == '`') { quote = ch; continue; }
                if (ch == '(') depth++;
                else if (ch == ')') depth--;
                else if (ch == ',' && depth == 0) { parts.Add(s.Substring(start, i - start)); start = i + 1; }
            }
            parts.Add(s.Substring(start));
            return parts;
        }

        /// <summary>Allows only plain identifiers (optionally alias-qualified) in dynamic SQL built from request input.</summary>
        public static string SafeIdentifier(string name)
        {
            if (name == null || !Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$"))
                throw new DataGuardException($"Invalid column name '{name}'.", 400);
            return name;
        }

        // ------------------------------------------------------------------ storage

        static string Segment(string s) => Regex.Replace(Path.GetFileName((s ?? "").Replace('\\', '/').TrimEnd('/')) ?? "", @"[^A-Za-z0-9._-]", "_").Trim('.');
        /// <summary>Storage namespace of the caller: tenant-{id} for tenant users, platform for platform users.</summary>
        public static string StoragePrefix() => Current?.Tenant is int t ? $"tenant-{t}" : "platform";

        /// <summary>Object key under the caller's namespace; folder and file names are reduced to safe single path segments.</summary>
        public static string StorageKey(string root, string folder, string fileName)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(root)) parts.Add(Segment(root));
            parts.Add(StoragePrefix());
            foreach (var f in (folder ?? "").Split('/', '\\').Select(Segment).Where(x => x.Length > 0)) parts.Add(f);
            var name = Segment(fileName);
            parts.Add(name.Length == 0 ? Guid.NewGuid().ToString("N") : name);
            return string.Join("/", parts);
        }

        /// <summary>Tenant users may only request keys inside their own namespace.</summary>
        public static string CheckStorageKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Contains("..")) throw new DataGuardException("Invalid file key.", 400);
            var caller = Current;
            if (caller?.Tenant != null && !key.Split('/').Contains($"tenant-{caller.Tenant}"))
                throw new DataGuardException("That file belongs to another tenant.", 403);
            return key;
        }

        static byte[] _signingKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        public static void SetSigningSecret(string secret) { if (!string.IsNullOrEmpty(secret)) _signingKey = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("files:" + secret)); }

        /// <summary>Signature for a short-lived download link of a resource (e.g. "ai_file:12"), valid until `expires` (unix seconds).</summary>
        public static string Sign(string resource, long expires)
        {
            using var h = new System.Security.Cryptography.HMACSHA256(_signingKey);
            return Convert.ToHexString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{resource}|{expires}"))).ToLowerInvariant();
        }
        public static bool VerifySignature(string resource, long expires, string sig) =>
            expires >= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && sig != null &&
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Sign(resource, expires)), System.Text.Encoding.ASCII.GetBytes(sig));

        // Extension points filled in by the other DataGuard parts (lifecycle, audit/outbox, immutability).
        static partial void InitExtensionsImpl(MySqlConnection c);
        static partial void BeforeInsertImpl(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values);
        static partial void BeforeUpdateImpl(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values);
        static partial void BeforeDeleteImpl(MySqlCommand cmd, Caller caller, string table);
        static void InitExtensions(MySqlConnection c) => InitExtensionsImpl(c);
        static void BeforeInsert(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values) => BeforeInsertImpl(cmd, caller, table, values);
        static void BeforeUpdate(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values) => BeforeUpdateImpl(cmd, caller, table, values);
        static void BeforeDelete(MySqlCommand cmd, Caller caller, string table) => BeforeDeleteImpl(cmd, caller, table);
    }
}
