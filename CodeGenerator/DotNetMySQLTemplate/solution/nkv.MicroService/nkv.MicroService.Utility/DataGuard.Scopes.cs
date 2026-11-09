using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Row-level scopes inside a tenant (ui.config.json "rowScopes"): users in the listed roles only see and change rows
    /// linked to their own value of a column (their customer, their assignments...). Example:
    ///   { "name": "customer", "roles": ["Customer User"], "column": "customer_id",
    ///     "userValue": { "table": "tenant_users", "match": "login_username", "column": "customer_id" } }
    /// Applies to tables that have the column, to the "home" table whose primary key is the column (customers), and to tables
    /// linked to those through foreign keys (up to 3 hops, e.g. case_tasks.case_id -> cases.customer_id). Other tables are not
    /// filtered (role permissions decide). A user without a value sees nothing. The column is forced on writes, foreign keys
    /// must point at rows inside the scope, and AI files of out-of-scope records are hidden from the assistant.
    /// </summary>
    public static partial class DataGuard
    {
        sealed class RowScope
        {
            public string Name, Column, ValueTable, MatchColumn, ValueColumn, Home;
            public bool MatchByUserId;
            public HashSet<string> Roles = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Only, Exclude = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Direct = new(StringComparer.OrdinalIgnoreCase);
            /// <summary>table -> first hop (fk column, parent table, parent pk) towards a direct/home table</summary>
            public Dictionary<string, (string Col, string Parent, string ParentPk)> Via = new(StringComparer.OrdinalIgnoreCase);
            public bool Covers(string t) => Direct.Contains(t) || t.Equals(Home, StringComparison.OrdinalIgnoreCase) || Via.ContainsKey(t);
        }
        static readonly List<RowScope> _scopes = new();
        static readonly ConcurrentDictionary<string, (object value, DateTime at)> _scopeValueCache = new();

        public static JsonArray ScopesSummary() => new JsonArray(_scopes.Select(s => (JsonNode)new JsonObject
        {
            ["name"] = s.Name, ["roles"] = new JsonArray(s.Roles.Select(r => (JsonNode)r).ToArray()), ["column"] = s.Column, ["home"] = s.Home,
            ["directTables"] = s.Direct.Count, ["linkedTables"] = s.Via.Count,
        }).ToArray());

        static void LoadScopes()
        {
            _scopes.Clear(); _scopeValueCache.Clear();
            if (Config["rowScopes"] is not JsonArray arr) return;
            foreach (var o in arr.OfType<JsonObject>())
            {
                var uv = o["userValue"] as JsonObject;
                var s = new RowScope
                {
                    Name = o["name"]?.GetValue<string>() ?? o["column"]!.GetValue<string>(),
                    Column = o["column"]!.GetValue<string>().ToLowerInvariant(),
                    ValueTable = uv?["table"]?.GetValue<string>(), MatchColumn = uv?["match"]?.GetValue<string>(),
                    ValueColumn = uv?["column"]?.GetValue<string>(),
                    MatchByUserId = uv?["matchBy"]?.GetValue<string>() == "user_id",
                    Roles = Set(o["roles"]),
                    Only = o["tables"] is JsonArray ? Set(o["tables"]) : null,
                    Exclude = Set(o["exclude"]),
                };
                if (s.ValueTable != null) { SafeIdentifier(s.ValueTable); SafeIdentifier(s.MatchColumn); SafeIdentifier(s.ValueColumn); }
                bool Allowed(string t) => (s.Only == null || s.Only.Contains(t)) && !s.Exclude.Contains(t) && AppTables.Contains(t);
                s.Home = PrimaryKeys.FirstOrDefault(kv => kv.Value.Equals(s.Column, StringComparison.OrdinalIgnoreCase) && AppTables.Contains(kv.Key)).Key;
                foreach (var t in AppTables) if (Allowed(t) && Columns[t].Contains(s.Column) && !t.Equals(s.Home, StringComparison.OrdinalIgnoreCase)) s.Direct.Add(t);
                // breadth-first over foreign keys: tables that reach a direct/home table in <= 3 hops
                var reached = new HashSet<string>(s.Direct, StringComparer.OrdinalIgnoreCase);
                if (s.Home != null) reached.Add(s.Home);
                for (var depth = 0; depth < 3; depth++)
                {
                    var added = new List<(string t, string col, string parent, string pk)>();
                    foreach (var t in AppTables.Where(t => Allowed(t) && !reached.Contains(t)))
                        if (ForeignKeys.TryGetValue(t, out var fks))
                        {
                            var hop = fks.FirstOrDefault(f => reached.Contains(f.RefTable));
                            if (hop.RefTable != null) added.Add((t, hop.Column, hop.RefTable, hop.RefColumn));
                        }
                    if (added.Count == 0) break;
                    foreach (var a in added) { s.Via[a.t] = (a.col, a.parent, a.pk); reached.Add(a.t); }
                }
                _scopes.Add(s);
            }
        }

        /// <summary>Scopes that apply to this caller, with their value (null value = caller has no value: sees nothing).</summary>
        static List<(RowScope scope, object value)> ActiveScopes(MySqlConnection c, Caller caller)
        {
            var list = new List<(RowScope, object)>();
            if (_scopes.Count == 0 || caller.IsSystem) return list;
            var role = RoleName(c, caller);
            foreach (var s in _scopes.Where(s => s.Roles.Contains(role ?? "")))
                list.Add((s, ScopeValue(caller, s)));
            return list;
        }

        static object ScopeValue(Caller caller, RowScope s)
        {
            var key = $"{caller.UserId}:{s.Name}";
            if (_scopeValueCache.TryGetValue(key, out var hit) && (DateTime.UtcNow - hit.at).TotalSeconds < 30) return hit.value;
            object v = null;
            if (s.ValueTable != null)
            {
                using var c = new MySqlConnection(ConnectionString);
                c.Open();
                var tenantCond = TenantTables.Contains(s.ValueTable) && caller.Tenant != null ? $" AND `{TenantColumn}` = @t" : "";
                using var q = new MySqlCommand($"SELECT `{s.ValueColumn}` FROM `{s.ValueTable}` WHERE `{s.MatchColumn}` = @u AND isActive = 1{tenantCond} LIMIT 1", c);
                q.Parameters.AddWithValue("@u", s.MatchByUserId ? caller.UserId : caller.User);
                if (tenantCond.Length > 0) q.Parameters.AddWithValue("@t", caller.Tenant.Value);
                var r = q.ExecuteScalar();
                v = r == null || r is DBNull ? null : r;
            }
            _scopeValueCache[key] = (v, DateTime.UtcNow);
            return v;
        }

        /// <summary>SQL condition limiting `table` (as `alias`) to rows inside the scope; the value is bound as `param`.</summary>
        static string ScopeCondition(RowScope s, string table, string alias, string param, int depth = 0)
        {
            if (s.Direct.Contains(table)) return $"{alias}.`{s.Column}` = {param}";
            if (table.Equals(s.Home, StringComparison.OrdinalIgnoreCase)) return $"{alias}.`{PrimaryKeys[table]}` = {param}";
            var (col, parent, pk) = s.Via[table];
            var sub = $"rs{depth}";
            return $"{alias}.`{col}` IN (SELECT {sub}.`{pk}` FROM `{parent}` {sub} WHERE {ScopeCondition(s, parent, sub, param, depth + 1)})";
        }

        static List<string> RowScopeConditions(string table, string prefix, Caller caller, MySqlCommand cmd)
        {
            var conds = new List<string>();
            var alias = prefix.TrimEnd('.');
            var i = 0;
            foreach (var (s, v) in ActiveScopes(cmd.Connection, caller))
            {
                i++;
                if (!s.Covers(table)) continue;
                if (v == null) { conds.Add("1 = 0"); continue; }
                var p = $"@__rs{i}";
                Bind(cmd, p, v);
                conds.Add(ScopeCondition(s, table, alias.Length == 0 ? $"`{table}`" : alias, p));
            }
            return conds;
        }

        /// <summary>AI files/text: hide those attached to records outside the caller's scopes.</summary>
        static List<string> RowScopeFileConditions(string table, string alias, Caller caller, MySqlCommand cmd)
        {
            var conds = new List<string>();
            var i = 0;
            foreach (var (s, v) in ActiveScopes(cmd.Connection, caller))
            {
                i++;
                var tables = AppTables.Where(s.Covers).ToList();
                if (tables.Count == 0) continue;
                var p = $"@__rsf{i}";
                Bind(cmd, p, v ?? DBNull.Value);
                string FilesCond(string f) =>
                    $"({f}.table_name NOT IN ({string.Join(", ", tables.Select(t => $"'{t}'"))})" +
                    (v == null ? ")" : " OR " + string.Join(" OR ", tables.Select((t, k) =>
                        $"({f}.table_name = '{t}' AND {f}.record_id IN (SELECT CAST(x{k}.`{PrimaryKeys[t]}` AS CHAR) FROM `{t}` x{k} WHERE {ScopeCondition(s, t, "x" + k, p)}))")) + ")");
                conds.Add(table.Equals("ai_files", StringComparison.OrdinalIgnoreCase)
                    ? FilesCond(alias)
                    : $"{alias}.file_id IN (SELECT ff.file_id FROM ai_files ff WHERE {FilesCond("ff")})");
            }
            return conds;
        }

        /// <summary>Writes by scoped users: the scope column is forced to their value, new home rows are refused, and every
        /// foreign key must point at a row they can see.</summary>
        static void GuardScopedWrite(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values, Func<string, string> paramOf, bool insert)
        {
            var active = ActiveScopes(cmd.Connection, caller);
            if (active.Count == 0) return;
            foreach (var (s, v) in active)
            {
                if (!s.Covers(table)) continue;
                if (v == null) throw new DataGuardException($"Your account isn't linked to a {s.Name} yet, so you can't change {table}.", 403);
                if (insert && table.Equals(s.Home, StringComparison.OrdinalIgnoreCase))
                    throw new DataGuardException($"You can't create {table} records.", 403);
                if (s.Direct.Contains(table) && paramOf(s.Column) is string p)
                {
                    Bind(cmd, p, v);
                    values[s.Column] = v;
                }
            }
            if (!ForeignKeys.TryGetValue(table, out var fks)) return;
            foreach (var (col, refTable, refCol) in fks)
            {
                if (!values.TryGetValue(col, out var val) || val == null || val is DBNull || (val is string sv && (sv.Length == 0 || sv.StartsWith("@")))) continue;
                if (!active.Any(a => a.scope.Covers(refTable))) continue;
                // When the referenced table IS the tenant table, don't apply tenant filter (it's the root of tenancy)
                var sql = $"SELECT COUNT(*) FROM `{refTable}` t WHERE t.`{refCol}` = @v AND t.isActive = 1";
                if (!refTable.Equals(TenantTable, StringComparison.OrdinalIgnoreCase) && caller.Tenant != null)
                    sql += $" AND t.`{TenantColumn}` = @__g_tenant";
                using var q = new MySqlCommand(sql, cmd.Connection, cmd.Transaction);
                q.Parameters.AddWithValue("@v", val);
                if (refTable.Equals(TenantTable, StringComparison.OrdinalIgnoreCase) == false && caller.Tenant != null)
                    q.Parameters.AddWithValue("@__g_tenant", caller.Tenant.Value);
                if (Convert.ToInt64(Apply(q).ExecuteScalar()) == 0)
                    throw new DataGuardException($"{table}.{col} = {val} is outside what you can access.", 403);
            }
        }
    }
}
