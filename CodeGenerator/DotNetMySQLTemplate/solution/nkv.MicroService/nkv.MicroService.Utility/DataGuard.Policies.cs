using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Attribute-based rules from ui.config.json, enforced on the server for every write (people, imports, AI):
    ///  "actorProfile":      the signed-in person's own row ({"table": "employee_profiles", "match": "email"}; "matchBy": "user_id"
    ///                       when the match column holds the user id). Rules read its columns with {"me": "approval_limit"}.
    ///  lifecycle "check":   a transition only happens when a condition holds, e.g. approve only up to the approver's limit:
    ///                       {"column": "amount", "lte": {"me": "approval_limit"}, "message": "Above your limit: escalate it."}
    ///                       (operators lt, lte, gt, gte, eq, ne, in, notIn; values: a literal, {"me": col} or {"column": col}).
    ///  lifecycle "creates": records created by a transition in the same transaction, once, e.g. an invoice on approval:
    ///                       {"table": "invoices", "values": {"expense_request_id": "$id", "amount": "$amount"}, "unique": "expense_request_id"}.
    ///  "budgets":           totals per person and period, checked atomically (row lock) when a record enters a counted state:
    ///                       {"table": "expense_requests", "sum": "amount", "per": "submitter_id", "states": {"column": "status",
    ///                        "count": ["Submitted", "Approved"]}, "period": {"column": "expense_date", "unit": "month"},
    ///                        "sameAs": ["currency"], "max": {"table": "employee_profiles", "column": "monthly_budget"}}.
    ///  "allowedReferences": which records a foreign key may point at, for pickers and on save, e.g. only projects the person is
    ///                       assigned to and that are active: {"table": "expense_requests", "column": "project_id",
    ///                        "through": {"table": "project_assignments", "column": "project_id", "me": "profile_id"},
    ///                        "targetWhere": {"is_active": 1}}.
    /// Refused writes are recorded in the audit table (action "table.denied") with the person, the rule and the attempted values.
    /// </summary>
    public static partial class DataGuard
    {
        static string _profileTable, _profileMatch;
        static bool _profileByUserId;
        static readonly ConcurrentDictionary<string, (Dictionary<string, object> row, DateTime at)> _profileCache = new();
        static readonly List<JsonObject> _budgets = new();
        static readonly List<JsonObject> _allowedRefs = new();
        static readonly System.Threading.AsyncLocal<(string table, object id, Dictionary<string, object> values)?> _writing = new();

        static void LoadPolicies()
        {
            _profileTable = _profileMatch = null; _profileByUserId = false; _profileCache.Clear(); _budgets.Clear(); _allowedRefs.Clear();
            if (Config["actorProfile"] is JsonObject ap && ap["table"] != null)
            {
                _profileTable = SafeIdentifier(ap["table"]!.GetValue<string>());
                _profileMatch = SafeIdentifier(ap["match"]?.GetValue<string>() ?? "email");
                _profileByUserId = ap["matchBy"]?.GetValue<string>() == "user_id";
            }
            if (Config["budgets"] is JsonArray b) _budgets.AddRange(b.OfType<JsonObject>().Where(x => x["table"] != null && x["sum"] != null && x["per"] != null));
            if (Config["allowedReferences"] is JsonArray r) _allowedRefs.AddRange(r.OfType<JsonObject>().Where(x => x["table"] != null && x["column"] != null));
        }

        // ------------------------------------------------------------------ the signed-in person's profile

        /// <summary>The caller's actorProfile row (null when not configured or the person has none). Cached 30 seconds.</summary>
        static Dictionary<string, object> Me(Caller caller)
        {
            if (_profileTable == null || caller == null || caller.IsSystem || caller.IsPublic) return null;
            var key = caller.UserId + ":" + caller.User;
            if (_profileCache.TryGetValue(key, out var hit) && (DateTime.UtcNow - hit.at).TotalSeconds < 30) return hit.row;
            Dictionary<string, object> row = null;
            using (var c = Db.Connection(ConnectionString))
            {
                c.Open();
                var tenantCond = caller.Tenant != null && TenantTables.Contains(_profileTable) ? $" AND {Db.Q(TenantColumn)} = @t" : "";
                var ownerCond = caller.Owner != null && Columns.TryGetValue(_profileTable, out var pc) && pc.Contains("createdby") ? " AND createdBy = @o" : "";
                using var q = Db.Command($"SELECT {Db.Top(1)}* FROM {Db.Q(_profileTable)} WHERE {Db.Q(_profileMatch)} = @u AND isActive = 1{tenantCond}{ownerCond}{Db.LimitEnd(1)}", c);
                q.Parameters.AddWithValue("@u", _profileByUserId ? caller.UserId : (object)caller.User);
                if (tenantCond.Length > 0) q.Parameters.AddWithValue("@t", caller.Tenant.Value);
                if (ownerCond.Length > 0) q.Parameters.AddWithValue("@o", caller.Owner);
                using var rd = q.ExecuteReader();
                if (rd.Read())
                {
                    row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < rd.FieldCount; i++) row[rd.GetName(i)] = rd.IsDBNull(i) ? null : rd.GetValue(i);
                }
            }
            _profileCache[key] = (row, DateTime.UtcNow);
            return row;
        }

        static object MeKey(Caller caller) => Me(caller) is { } me && PrimaryKeys.TryGetValue(_profileTable, out var pk) ? me.GetValueOrDefault(pk) : null;

        /// <summary>A rule value: a literal, {"me": col} (the caller's profile), or {"column": col} (the record).</summary>
        static object RuleValue(JsonNode spec, Dictionary<string, object> row, Caller caller, out string label)
        {
            label = null;
            if (spec is JsonObject o)
            {
                if (o["me"]?.GetValue<string>() is { } mc)
                {
                    label = $"your {Humanize(mc).ToLowerInvariant()}";
                    return Me(caller)?.GetValueOrDefault(mc);
                }
                if (o["column"]?.GetValue<string>() is { } rc) { label = Humanize(rc).ToLowerInvariant(); return row.GetValueOrDefault(rc); }
                return null;
            }
            return spec is JsonValue v ? (v.TryGetValue<string>(out var s) ? s : v.ToString()) : null;
        }

        static int Compare(object a, object b)
        {
            var x = Str(a); var y = Str(b);
            if (decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out var dx) && decimal.TryParse(y, NumberStyles.Any, CultureInfo.InvariantCulture, out var dy)) return dx.CompareTo(dy);
            if (DateTime.TryParse(x, out var tx) && DateTime.TryParse(y, out var ty)) return tx.CompareTo(ty);
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Null when the check passes, otherwise the reason (lifecycle "check").</summary>
        static string CheckFails(string table, JsonObject check, Dictionary<string, object> row, Caller caller)
        {
            var col = check["column"]?.GetValue<string>();
            if (col == null) return null;
            var value = row.GetValueOrDefault(col);
            var field = FieldLabel(table, col);
            foreach (var (op, spec) in check)
            {
                if (op is "column" or "message") continue;
                if (op is "in" or "notIn")
                {
                    var set = (spec as JsonArray ?? new JsonArray()).Select(x => x?.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var inside = set.Contains(Str(value) ?? "");
                    if (inside != (op == "in")) return check["message"]?.GetValue<string>() ?? $"{field} must {(op == "in" ? "" : "not ")}be one of {string.Join(", ", set)}";
                    continue;
                }
                var limit = RuleValue(spec, row, caller, out var what);
                if (limit == null || limit is DBNull)
                    return check["message"]?.GetValue<string>() ?? $"{what ?? "the limit"} isn't set, so {field.ToLowerInvariant()} can't be checked";
                if (value == null || value is DBNull) return check["message"]?.GetValue<string>() ?? $"{field} is empty";
                var cmp = Compare(value, limit);
                var ok = op switch { "lt" => cmp < 0, "lte" => cmp <= 0, "gt" => cmp > 0, "gte" => cmp >= 0, "eq" => cmp == 0, "ne" => cmp != 0, _ => true };
                if (!ok)
                {
                    var sign = op switch { "lt" => "below", "lte" => "at most", "gt" => "above", "gte" => "at least", "eq" => "equal to", _ => "different from" };
                    return check["message"]?.GetValue<string>() ?? $"{field} ({Str(value)}) must be {sign} {what ?? Str(limit)} ({Str(limit)})";
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ allowed references

        static IEnumerable<JsonObject> RefRules(string table, string column, Caller caller)
        {
            if (caller == null || caller.IsSystem) yield break;
            string role = null;
            foreach (var r in _allowedRefs)
            {
                if (!table.Equals(r["table"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase)) continue;
                if (!column.Equals(r["column"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase)) continue;
                var roles = Set(r["roles"]);
                role ??= RoleName(null, caller) ?? "";
                if (roles.Count > 0 ? !(roles.Contains("*") || roles.Contains(role)) : caller.RoleId == 1) continue; // default: everyone but administrators
                yield return r;
            }
        }

        /// <summary>SQL selecting the ids this foreign key may point at for the caller under one rule (params bound on cmd).</summary>
        static string AllowedIdsSql(JsonObject rule, string refTable, string refPk, Caller caller, DbCommand cmd, string p)
        {
            var parts = new List<string>();
            if (rule["through"] is JsonObject th)
            {
                var tt = SafeIdentifier(th["table"]!.GetValue<string>());
                var tc = SafeIdentifier(th["column"]?.GetValue<string>() ?? refPk);
                var meCol = th["me"]?.GetValue<string>();
                var key = meCol != null ? MeKey(caller) : null;
                Bind(cmd, p + "me", key ?? DBNull.Value);
                var extra = string.Join("", (th["where"] as JsonObject ?? new JsonObject()).Select((kv, i) => { Bind(cmd, $"{p}tw{i}", Str(RuleValue(kv.Value, new(), caller, out _)) ?? (object)DBNull.Value); return $" AND a.{Db.Q(SafeIdentifier(kv.Key))} = {p}tw{i}"; }));
                parts.Add($"x.{Db.Q(refPk)} IN (SELECT a.{Db.Q(tc)} FROM {Db.Q(tt)} a WHERE a.isActive = 1" + (meCol != null ? $" AND a.{Db.Q(SafeIdentifier(meCol))} = {p}me" : "") + extra + ")");
            }
            if (rule["targetWhere"] is JsonObject tw)
            {
                var i = 0;
                foreach (var (k, v) in tw)
                {
                    Bind(cmd, $"{p}w{i}", Str(RuleValue(v, new(), caller, out _)) ?? (object)DBNull.Value);
                    parts.Add($"x.{Db.Q(SafeIdentifier(k))} = {p}w{i}");
                    i++;
                }
            }
            return $"SELECT x.{Db.Q(refPk)} FROM {Db.Q(refTable)} x WHERE x.isActive = 1" + string.Concat(parts.Select(c => " AND " + c));
        }

        /// <summary>The ids a picker for table.column may offer this caller, or null when any visible record is allowed.</summary>
        public static List<string> AllowedReferenceIds(DbConnection c, string table, string column)
        {
            var caller = Current;
            table = (table ?? "").ToLowerInvariant(); column = (column ?? "").ToLowerInvariant();
            var rules = RefRules(table, column, caller).ToList();
            if (rules.Count == 0 || !ForeignKeys.TryGetValue(table, out var fks)) return null;
            var fk = fks.FirstOrDefault(f => f.Column.Equals(column, StringComparison.OrdinalIgnoreCase));
            if (fk.RefTable == null) return null;
            HashSet<string> ids = null;
            foreach (var (rule, i) in rules.Select((r, i) => (r, i)))
            {
                using var q = Db.Command("", c);
                q.CommandText = AllowedIdsSql(rule, fk.RefTable, fk.RefColumn, caller, q, $"@__ar{i}");
                var these = new HashSet<string>();
                using (var r = Apply(q).ExecuteReader()) while (r.Read()) if (!r.IsDBNull(0)) these.Add(Convert.ToString(r.GetValue(0), CultureInfo.InvariantCulture));
                ids = ids == null ? these : ids.Intersect(these).ToHashSet();
            }
            return ids?.ToList();
        }

        /// <summary>Refuses a foreign-key value outside the allowedReferences rules (insert, or update that changes it).</summary>
        static void CheckAllowedReferences(DbCommand cmd, Caller caller, string table, Dictionary<string, object> values, Dictionary<string, object> current)
        {
            if (_allowedRefs.Count == 0 || !ForeignKeys.TryGetValue(table, out var fks)) return;
            foreach (var (col, refTable, refCol) in fks)
            {
                if (!values.TryGetValue(col, out var v) || v == null || v is DBNull || (v is string s && (s.Length == 0 || s.StartsWith("@")))) continue;
                if (current != null && SameValue(v, current.GetValueOrDefault(col))) continue;
                foreach (var (rule, i) in RefRules(table, col, caller).Select((r, i) => (r, i)))
                {
                    using var q = Db.Command("", cmd.Connection, cmd.Transaction);
                    var p = $"@__arw{i}";
                    q.CommandText = $"SELECT COUNT(*) FROM ({AllowedIdsSql(rule, refTable, refCol, caller, q, p)}) z WHERE z.{Db.Q(refCol)} = {p}v";
                    q.Parameters.AddWithValue(p + "v", v);
                    if (Convert.ToInt64(q.ExecuteScalar()) == 0)
                        throw new DataGuardException(rule["message"]?.GetValue<string>()
                            ?? $"You can't choose this {Label(refTable).ToLowerInvariant()} for {FieldLabel(table, col).ToLowerInvariant()}: it isn't one available to you.", 409);
                }
            }
        }

        // ------------------------------------------------------------------ budgets

        /// <summary>Inside the write's transaction, after the row is written: totals for the person and period, under a row lock.</summary>
        static void CheckBudgets(DbCommand c, Caller caller, string table, Dictionary<string, object> row, Dictionary<string, object> before)
        {
            foreach (var b in _budgets.Where(x => table.Equals(x["table"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase)))
            {
                var sumCol = SafeIdentifier(b["sum"]!.GetValue<string>()); var perCol = SafeIdentifier(b["per"]!.GetValue<string>());
                var stateCol = b["states"]?["column"]?.GetValue<string>();
                var counted = Set(b["states"]?["count"]);
                var on = b["states"]?["on"] is JsonArray ? Set(b["states"]?["on"]) : counted;
                var state = stateCol != null ? Str(row.GetValueOrDefault(stateCol)) : null;
                if (stateCol != null && !on.Contains(state ?? "")) continue;
                var periodCol = b["period"]?["column"]?.GetValue<string>();
                var sameAs = (b["sameAs"] as JsonArray ?? new JsonArray()).Select(x => SafeIdentifier(x!.GetValue<string>())).ToList();
                if (before != null)
                {
                    // only when the record enters a counted state or a counted value changes
                    var watched = new[] { stateCol, sumCol, perCol, periodCol }.Concat(sameAs).Where(x => x != null);
                    if (watched.All(k => SameValue(row.GetValueOrDefault(k), before.GetValueOrDefault(k)))) continue;
                }
                var per = row.GetValueOrDefault(perCol);
                if (per == null || per is DBNull) continue;

                // max: a number, {"me": col}, or {"table": T, "column": C} read from the per-row (its primary key = the per value)
                object max = null;
                string perTable = null;
                if (b["max"] is JsonObject mo && mo["table"] != null)
                {
                    perTable = SafeIdentifier(mo["table"]!.GetValue<string>());
                    var pk = PrimaryKeys.GetValueOrDefault(perTable) ?? perCol;
                    // lock the person's row: concurrent submissions for the same person wait for each other
                    var lockSql = Db.IsSqlServer
                        ? $"SELECT {Db.Q(SafeIdentifier(mo["column"]!.GetValue<string>()))} FROM {Db.Q(perTable)} WITH (UPDLOCK, ROWLOCK) WHERE {Db.Q(pk)} = @p"
                        : $"SELECT {Db.Q(SafeIdentifier(mo["column"]!.GetValue<string>()))} FROM {Db.Q(perTable)} WHERE {Db.Q(pk)} = @p FOR UPDATE";
                    using var lq = Db.Command(lockSql, c.Connection, c.Transaction);
                    lq.Parameters.AddWithValue("@p", per);
                    max = lq.ExecuteScalar();
                }
                else max = RuleValue(b["max"], row, caller, out _);
                if (max == null || max is DBNull) continue; // no budget set for this person

                var conds = new List<string> { $"{Db.Q(perCol)} = @per", "isActive = 1" };
                using var sq = Db.Command("", c.Connection, c.Transaction);
                sq.Parameters.AddWithValue("@per", per);
                if (stateCol != null && counted.Count > 0)
                {
                    conds.Add($"{Db.Q(SafeIdentifier(stateCol))} IN ({string.Join(", ", counted.Select((s, i) => "@st" + i))})");
                    var k = 0; foreach (var s in counted) sq.Parameters.AddWithValue("@st" + k++, s);
                }
                if (periodCol != null && DateTime.TryParse(Str(row.GetValueOrDefault(periodCol)), out var d))
                {
                    var unit = b["period"]?["unit"]?.GetValue<string>() ?? "month";
                    var start = unit switch { "year" => new DateTime(d.Year, 1, 1), "week" => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)), "day" => d.Date, _ => new DateTime(d.Year, d.Month, 1) };
                    var end = unit switch { "year" => start.AddYears(1), "week" => start.AddDays(7), "day" => start.AddDays(1), _ => start.AddMonths(1) };
                    conds.Add($"{Db.Q(SafeIdentifier(periodCol))} >= @ps AND {Db.Q(SafeIdentifier(periodCol))} < @pe");
                    sq.Parameters.AddWithValue("@ps", start); sq.Parameters.AddWithValue("@pe", end);
                }
                foreach (var (col, i) in sameAs.Select((x, i) => (x, i)))
                {
                    conds.Add($"{Db.Q(col)} = @sa{i}");
                    sq.Parameters.AddWithValue("@sa" + i, row.GetValueOrDefault(col) ?? DBNull.Value);
                }
                if (Columns.TryGetValue(table, out var tc) && tc.Contains("createdby") && caller.Owner != null) { conds.Add("createdBy = @own"); sq.Parameters.AddWithValue("@own", caller.Owner); }
                if (caller.Tenant != null && TenantTables.Contains(table)) { conds.Add($"{Db.Q(TenantColumn)} = @ten"); sq.Parameters.AddWithValue("@ten", caller.Tenant.Value); }
                // a locking read sees rows committed by transactions that held the lock before us (MySQL); this row is our own
                sq.CommandText = $"SELECT COALESCE(SUM({Db.Q(sumCol)}), 0) FROM {Db.Q(table)}{(Db.IsSqlServer ? " WITH (UPDLOCK, HOLDLOCK)" : "")} WHERE {string.Join(" AND ", conds)}{(Db.IsSqlServer ? "" : " FOR UPDATE")}";
                var total = Convert.ToDecimal(sq.ExecuteScalar(), CultureInfo.InvariantCulture);
                var limit = Convert.ToDecimal(max, CultureInfo.InvariantCulture);
                if (total > limit)
                    throw new DataGuardException(b["message"]?.GetValue<string>()
                        ?? $"This would bring {FieldLabel(table, sumCol).ToLowerInvariant()} for the {(b["period"]?["unit"]?.GetValue<string>() ?? "period")} to {total.ToString("0.##", CultureInfo.InvariantCulture)}, above the limit of {limit.ToString("0.##", CultureInfo.InvariantCulture)}.", 409);
            }
        }

        // ------------------------------------------------------------------ records created by a transition

        static void CreateOnTransition(DbCommand c, Caller caller, string table, JsonArray creates, object id, Dictionary<string, object> row)
        {
            foreach (var cr in creates.OfType<JsonObject>())
            {
                var target = SafeIdentifier(cr["table"]!.GetValue<string>());
                if (!Columns.TryGetValue(target, out var cols)) continue;
                var vals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in cr["values"] as JsonObject ?? new JsonObject())
                {
                    var s = v is JsonValue jv && jv.TryGetValue<string>(out var str) ? str : v?.ToString();
                    vals[SafeIdentifier(k)] = s == "$id" ? id : s == "$user" ? caller.User : s == "$now" ? DateTime.UtcNow
                        : s != null && s.StartsWith("$") ? row.GetValueOrDefault(s.Substring(1)) : s;
                }
                if (cr["unique"]?.GetValue<string>() is { } u && vals.TryGetValue(u, out var uv))
                {
                    using var ex = Db.Command($"SELECT COUNT(*) FROM {Db.Q(target)} WHERE {Db.Q(SafeIdentifier(u))} = @v AND isActive = 1", c.Connection, c.Transaction);
                    ex.Parameters.AddWithValue("@v", uv ?? DBNull.Value);
                    if (Convert.ToInt64(ex.ExecuteScalar()) > 0) continue; // already created: approving again makes no second one
                }
                var now = DateTime.UtcNow;
                if (cols.Contains("createdby")) vals["createdBy"] = caller.Owner ?? caller.User;
                if (cols.Contains("modifiedby")) vals["modifiedBy"] = caller.Owner ?? caller.User;
                if (cols.Contains("createdat")) vals["createdAt"] = now;
                if (cols.Contains("modifiedat")) vals["modifiedAt"] = now;
                if (cols.Contains("isactive")) vals["isActive"] = 1;
                var names = vals.Keys.Where(k => cols.Contains(k.ToLowerInvariant())).ToList();
                using var ins = Db.Command($"INSERT INTO {Db.Q(target)} ({string.Join(", ", names.Select(Db.Q))}) VALUES ({string.Join(", ", names.Select((_, i) => "@c" + i))})", c.Connection, c.Transaction);
                for (var i = 0; i < names.Count; i++) ins.Parameters.AddWithValue("@c" + i, vals[names[i]] ?? DBNull.Value);
                // as the system (the person may not create these themselves), with numbering, audit and notifications
                var sys = new Caller { User = caller.User, UserId = caller.UserId, RoleId = caller.RoleId, Owner = caller.Owner, Tenant = caller.Tenant, IsSystem = true,
                                       Via = "automatic", CorrelationId = caller.CorrelationId, Ip = caller.Ip };
                using (RunAs(sys)) NonQuery(ins);
            }
        }

        // ------------------------------------------------------------------ refused writes

        static void RecordDenial(DataGuardException e)
        {
            try
            {
                var ctx = _writing.Value; var caller = Current;
                if (ctx == null || caller == null || _auditTable == null || !Columns.ContainsKey(_auditTable)) return;
                var (table, id, values) = ctx.Value;
                if (_auditExclude.Contains(table)) return;
                var details = new JsonObject { ["rule"] = e.Message, ["status"] = e.StatusCode };
                var attempted = new JsonObject();
                foreach (var (k, v) in values ?? new()) if (k is not ("createdby" or "modifiedby" or "createdat" or "modifiedat")) attempted[k] = Short(v);
                details["attempted"] = attempted;
                using var c = Db.Connection(ConnectionString);
                c.Open();
                using var holder = Db.Command("", c);
                InsertInternal(holder, _auditTable, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["actor_type"] = caller.IsSystem ? "system" : caller.IsAi ? "ai" : "human", ["actor_ref"] = caller.User,
                    ["action_code"] = $"{table}.denied", ["entity_type"] = table, ["entity_ref"] = id?.ToString(),
                    ["details"] = details.ToJsonString(ReadableJson), ["correlation_ref"] = caller.CorrelationId, ["ip_address"] = caller.Ip,
                }, caller, values);
            }
            catch (Exception) { /* recording a refusal must never hide the refusal itself */ }
        }

        // ------------------------------------------------------------------ readable names for messages

        /// <summary>The name people see for a table ("Expense request"), from ui.config.json or the table name.</summary>
        static string Label(string table)
        {
            var t = Config?["tables"]?[table ?? ""];
            return t?["singular"]?.GetValue<string>() ?? Singular(Humanize(table));
        }

        static string FieldLabel(string table, string column)
        {
            var f = Config?["tables"]?[table ?? ""]?["fields"]?[column ?? ""];
            return f?["label"]?.GetValue<string>() ?? Humanize(column?.EndsWith("_id") == true && column.Length > 3 ? column[..^3] : column);
        }

        /// <summary>"a finding", "an engagement" (lower case).</summary>
        static string A(string label)
        {
            var w = (label ?? "").ToLowerInvariant();
            return (w.Length > 0 && "aeiou".IndexOf(w[0]) >= 0 ? "an " : "a ") + w;
        }

        static string Humanize(string name)
        {
            if (string.IsNullOrEmpty(name)) return name ?? "";
            var s = name.Replace('_', ' ').Trim();
            return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
        }

        static string Singular(string s) =>
            s.EndsWith("ies") ? s[..^3] + "y" : s.EndsWith("sses") ? s[..^2] : s.EndsWith("s") && !s.EndsWith("ss") ? s[..^1] : s;
    }
}
