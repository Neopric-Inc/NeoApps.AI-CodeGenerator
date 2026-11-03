using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Business rules from ui.config.json, enforced for every write that passes through DataGuard.NonQuery:
    ///  "lifecycles": allowed status transitions per role/actor, required and forbidden evidence, separation of duties
    ///                (distinctFrom), configurable admin bypass, frozen states,
    ///                status history rows and status timestamps.
    ///  "immutable":  append-only tables ("*") and read-only columns (AI output, etc.).
    ///  "audit":      one audit row per insert/update/delete, written in the same transaction.
    ///  "outbox":     domain events written to an outbox table in the same transaction (published by GuardJobs).
    /// </summary>
    public static partial class DataGuard
    {
        sealed class Lifecycle
        {
            public string Column;
            public HashSet<string> Initial;
            public List<Transition> Transitions = new();
            public HashSet<string> Frozen = new(StringComparer.OrdinalIgnoreCase);
            public string HistoryTable, HistoryFk, HistoryFrom, HistoryTo, HistoryActor, HistoryReason;
            public Dictionary<string, string> Timestamps = new(StringComparer.OrdinalIgnoreCase);
            public string SlaTable, SlaLink, SlaType, SlaDue, SlaMet;
            public Dictionary<string, double> SlaHours = new(StringComparer.OrdinalIgnoreCase);
            public string AdminBypass = "any";   // any | platform | none
        }
        sealed class Limit
        {
            public string Table, Measure = "count", SumColumn, Period, Message, OnMissing = "allow";
            public Dictionary<string, string> Where = new(StringComparer.OrdinalIgnoreCase);
            public string MaxFrom, MaxColumn, MaxJoin, MaxJoinOn;
            public Dictionary<string, string> MaxWhere = new(StringComparer.OrdinalIgnoreCase);
        }
        static readonly List<Limit> _limits = new();
        sealed class Transition
        {
            public HashSet<string> From, To;   // "*" = any
            public HashSet<string> Roles;      // role names, "*" = any role
            public HashSet<string> Actors;     // human / ai
            public List<Requirement> Requires = new();
            public List<Requirement> Forbids = new();      // none of these may exist
            public List<DistinctRule> DistinctFrom = new(); // the actor must not be the user in these columns
            public string AdminBypass;                       // overrides the lifecycle's
        }
        sealed class DistinctRule { public string Column, Table, Key, UserColumn = "user_id", Message; }
        sealed class Requirement
        {
            public string Table, Link, LinkFrom, Message;
            public Dictionary<string, string> Where = new(StringComparer.OrdinalIgnoreCase);
        }
        sealed class OutboxRule { public string Table, On, Event; public Dictionary<string, string> When = new(StringComparer.OrdinalIgnoreCase); }

        static readonly Dictionary<string, Lifecycle> _lifecycles = new(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, HashSet<string>> _immutable = new(StringComparer.OrdinalIgnoreCase);
        static string _auditTable, _auditPlatformTable, _outboxTable;
        static HashSet<string> _auditExclude = new(StringComparer.OrdinalIgnoreCase);
        static readonly List<OutboxRule> _outboxRules = new();
        static bool _outboxAll;
        static readonly ConcurrentDictionary<int, string> _roleNames = new();
        static readonly ConditionalWeakTable<MySqlCommand, List<Action<MySqlCommand, int>>> _after = new();

        public static string OutboxTable => _outboxTable;
        public static JsonObject RulesSummary()
        {
            return new JsonObject
            {
                ["tenancy"] = TenancyEnabled ? new JsonObject { ["column"] = TenantColumn, ["tenantTable"] = TenantTable, ["tenantTables"] = TenantTables.Count, ["unassignedMembersAtStartup"] = UnassignedMembers } : null,
                ["lifecycles"] = new JsonObject(_lifecycles.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode)new JsonObject
                {
                    ["column"] = kv.Value.Column,
                    ["transitions"] = kv.Value.Transitions.Count,
                    ["adminBypass"] = kv.Value.AdminBypass,
                    ["frozen"] = new JsonArray(kv.Value.Frozen.Select(x => (JsonNode)x).ToArray()),
                }))),
                ["immutable"] = new JsonObject(_immutable.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode)new JsonArray(kv.Value.Select(x => (JsonNode)x).ToArray())))),
                ["rowScopes"] = ScopesSummary(),
                ["numbering"] = new JsonArray(_numbering.Values.SelectMany(l => l).Select(n => (JsonNode)$"{n.Table}.{n.Column}: {n.Pattern} (reset {n.Reset})").ToArray()),
                ["audit"] = _auditTable, ["outbox"] = _outboxTable, ["outboxRules"] = _outboxAll ? -1 : _outboxRules.Count,
            };
        }

        static HashSet<string> Set(JsonNode n, string dflt = null)
        {
            var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (n is JsonArray a) foreach (var x in a) s.Add(x!.GetValue<string>());
            else if (n is JsonValue v) s.Add(v.GetValue<string>());
            else if (dflt != null) s.Add(dflt);
            return s;
        }

        static void ReadRequirements(JsonNode n, List<Requirement> into)
        {
            if (n is not JsonArray rq) return;
            foreach (var r in rq.OfType<JsonObject>())
            {
                var req = new Requirement { Table = r["table"]!.GetValue<string>(), Link = r["link"]?.GetValue<string>(), LinkFrom = r["linkFrom"]?.GetValue<string>(), Message = r["message"]?.GetValue<string>() };
                if (r["where"] is JsonObject w) foreach (var (k, v) in w) req.Where[k] = v?.ToString();
                into.Add(req);
            }
        }

        /// <summary>adminBypass: true/"any" (default), "platform" (only platform administrators, never tenant-bound ones), false/"none".</summary>
        static string Bypass(JsonNode n)
        {
            if (n is JsonValue v && v.TryGetValue<bool>(out var b)) return b ? "any" : "none";
            var s = n.ToString().ToLowerInvariant();
            return s is "platform" or "none" ? s : "any";
        }

        static partial void InitExtensionsImpl(MySqlConnection c)
        {
            _lifecycles.Clear(); _immutable.Clear(); _outboxRules.Clear();
            if (Config["lifecycles"] is JsonObject lcs)
                foreach (var (table, node) in lcs)
                {
                    if (node is not JsonObject o) continue;
                    var lc = new Lifecycle { Column = o["column"]?.GetValue<string>() ?? "status", Initial = o["initial"] == null ? null : Set(o["initial"]), Frozen = Set(o["frozen"]) };
                    if (o["transitions"] is JsonArray ts)
                        foreach (var tn in ts.OfType<JsonObject>())
                        {
                            var t = new Transition { From = Set(tn["from"], "*"), To = Set(tn["to"]), Roles = Set(tn["roles"], "*"), Actors = Set(tn["actors"], "human") };
                            ReadRequirements(tn["requires"], t.Requires);
                            ReadRequirements(tn["forbids"], t.Forbids);
                            foreach (var d in tn["distinctFrom"] is JsonArray da ? da.ToList() : new List<JsonNode> { tn["distinctFrom"] })
                            {
                                if (d is JsonValue dv) t.DistinctFrom.Add(new DistinctRule { Column = dv.GetValue<string>() });
                                else if (d is JsonObject dob) t.DistinctFrom.Add(new DistinctRule
                                {
                                    Column = dob["column"]!.GetValue<string>(), Table = dob["table"]?.GetValue<string>(), Key = dob["key"]?.GetValue<string>(),
                                    UserColumn = dob["userColumn"]?.GetValue<string>() ?? "user_id", Message = dob["message"]?.GetValue<string>(),
                                });
                            }
                            if (tn["adminBypass"] != null) t.AdminBypass = Bypass(tn["adminBypass"]);
                            lc.Transitions.Add(t);
                        }
                    if (o["adminBypass"] != null) lc.AdminBypass = Bypass(o["adminBypass"]);
                    if (o["history"] is JsonObject h)
                    {
                        lc.HistoryTable = h["table"]?.GetValue<string>(); lc.HistoryFk = h["link"]?.GetValue<string>();
                        lc.HistoryFrom = h["from"]?.GetValue<string>(); lc.HistoryTo = h["to"]?.GetValue<string>();
                        lc.HistoryActor = h["actor"]?.GetValue<string>(); lc.HistoryReason = h["reason"]?.GetValue<string>();
                    }
                    if (o["timestamps"] is JsonObject tsm) foreach (var (k, v) in tsm) lc.Timestamps[k] = v!.GetValue<string>();
                    if (o["sla"] is JsonObject sla)
                    {
                        lc.SlaTable = sla["table"]?.GetValue<string>(); lc.SlaLink = sla["link"]?.GetValue<string>();
                        lc.SlaType = sla["type"]?.GetValue<string>() ?? "deadline_type"; lc.SlaDue = sla["due"]?.GetValue<string>() ?? "due_at";
                        lc.SlaMet = sla["met"]?.GetValue<string>() ?? "met_at";
                        if (sla["hours"] is JsonObject hs) foreach (var (k, v) in hs) lc.SlaHours[k] = v!.GetValue<double>();
                    }
                    _lifecycles[table] = lc;
                }
            _limits.Clear();
            if (Config["limits"] is JsonArray lims)
                foreach (var l in lims.OfType<JsonObject>())
                {
                    var lim = new Limit
                    {
                        Table = l["table"]!.GetValue<string>(), Measure = l["sum"] != null ? "sum" : "count", SumColumn = l["sum"]?.GetValue<string>(),
                        Period = l["period"]?.GetValue<string>(), Message = l["message"]?.GetValue<string>(), OnMissing = l["onMissing"]?.GetValue<string>() ?? "allow",
                    };
                    if (l["where"] is JsonObject w) foreach (var (k, v) in w) lim.Where[k] = v?.ToString();
                    if (l["max"] is JsonObject mx)
                    {
                        lim.MaxFrom = mx["from"]?.GetValue<string>(); lim.MaxColumn = mx["column"]?.GetValue<string>();
                        if (mx["join"] is JsonObject j) { lim.MaxJoin = j["table"]?.GetValue<string>(); lim.MaxJoinOn = j["on"]?.GetValue<string>(); }
                        if (mx["where"] is JsonObject mw) foreach (var (k, v) in mw) lim.MaxWhere[k] = v?.ToString();
                    }
                    _limits.Add(lim);
                }
            LoadValidations();
            if (Config["immutable"] is JsonObject im)
                foreach (var (table, node) in im) _immutable[table] = Set(node);
            if (Config["audit"] is JsonObject au && au["enabled"]?.GetValue<bool>() != false)
            {
                _auditTable = au["table"]?.GetValue<string>();
                _auditPlatformTable = au["platformTable"]?.GetValue<string>();
                _auditExclude = Set(au["exclude"]);
            }
            if (Config["outbox"] is JsonObject ob && ob["enabled"]?.GetValue<bool>() != false)
            {
                _outboxTable = ob["table"]?.GetValue<string>();
                _outboxAll = ob["allChanges"]?.GetValue<bool>() == true;
                if (ob["events"] is JsonArray evs)
                    foreach (var e in evs.OfType<JsonObject>())
                    {
                        var r = new OutboxRule { Table = e["table"]!.GetValue<string>(), On = e["on"]?.GetValue<string>() ?? "insert", Event = e["event"]!.GetValue<string>() };
                        if (e["when"] is JsonObject w) foreach (var (k, v) in w) r.When[k] = v?.ToString();
                        _outboxRules.Add(r);
                    }
            }
            foreach (var t in new[] { _auditTable, _auditPlatformTable, _outboxTable }.Where(x => x != null)) { _auditExclude.Add(t); if (!_immutable.ContainsKey(t)) _immutable[t] = new(StringComparer.OrdinalIgnoreCase) { "*" }; }
            foreach (var lc in _lifecycles.Values.Where(l => l.HistoryTable != null))
                if (!_immutable.ContainsKey(lc.HistoryTable)) _immutable[lc.HistoryTable] = new(StringComparer.OrdinalIgnoreCase) { "*" };
            if (_outboxTable != null) _immutable[_outboxTable] = new(StringComparer.OrdinalIgnoreCase) { "*" };
            LoadNumbering(c);
        }

        // ------------------------------------------------------------------ write execution

        /// <summary>Runs an insert/update/delete with all rules applied; history, audit and outbox rows are written in the same transaction.</summary>
        public static int NonQuery(MySqlCommand cmd)
        {
            Apply(cmd);
            if (!_after.TryGetValue(cmd, out var actions) || actions.Count == 0) return cmd.ExecuteNonQuery();
            var own = cmd.Transaction == null ? cmd.Connection.BeginTransaction() : null;
            if (own != null) cmd.Transaction = own;
            try
            {
                var n = cmd.ExecuteNonQuery();
                if (n > 0) foreach (var a in actions) a(cmd, n);
                own?.Commit();
                return n;
            }
            catch { own?.Rollback(); throw; }
            finally { if (own != null) cmd.Transaction = null; _after.Remove(cmd); }
        }

        static void After(MySqlCommand cmd, Action<MySqlCommand, int> action)
        {
            if (!_after.TryGetValue(cmd, out var list)) _after.Add(cmd, list = new List<Action<MySqlCommand, int>>());
            list.Add(action);
        }

        // ------------------------------------------------------------------ hooks

        static partial void BeforeInsertImpl(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values)
        {
            Validate(table, values, null);
            foreach (var lim in _limits.Where(l => l.Table.Equals(table, StringComparison.OrdinalIgnoreCase)))
                CheckLimit(cmd, caller, lim, values);
            if (_lifecycles.TryGetValue(table, out var lc) && values.TryGetValue(lc.Column, out var sv) && lc.Initial != null)
            {
                var s = Str(sv);
                if (s != null && !lc.Initial.Contains(s))
                    throw new DataGuardException($"New {table} records must start in {string.Join(" / ", lc.Initial)}, not '{s}'.", 409);
            }
            After(cmd, (c, _) =>
            {
                var id = c.LastInsertedId;
                var row = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
                if (PrimaryKeys.TryGetValue(table, out var pk)) row[pk] = id;
                if (_lifecycles.TryGetValue(table, out var l) && row.TryGetValue(l.Column, out var st) && Str(st) != null)
                {
                    WriteHistory(c, caller, l, row, id, null, Str(st));
                    StampTimestamp(c, table, l, Str(st), id);
                    UpdateSla(c, caller, l, row, id, null, Str(st));
                }
                WriteAudit(c, caller, table, "create", id, row, null);
                WriteOutbox(c, caller, table, "insert", id, row, null);
            });
        }

        static partial void BeforeUpdateImpl(MySqlCommand cmd, Caller caller, string table, Dictionary<string, object> values)
        {
            var isSoftDelete = values.Count == 1 && values.TryGetValue("isactive", out var ia) && Str(ia) == "0";
            if (_immutable.TryGetValue(table, out var im) && im.Contains("*"))
                throw new DataGuardException($"{table} is append-only: records can't be changed or deleted.", 409);
            var id = KeyValue(cmd, table, values);
            var current = id == null ? null : LoadVisible(cmd, table, id);
            if (current == null) return; // not visible: the scoped UPDATE affects nothing
            string fromState = null, toState = null;
            if (_lifecycles.TryGetValue(table, out var lc))
            {
                fromState = Str(current.GetValueOrDefault(lc.Column));
                if (lc.Frozen.Contains(fromState ?? "") && !isSoftDelete)
                    throw new DataGuardException($"This {table} record is {fromState} and can no longer be changed.", 409);
                if (values.TryGetValue(lc.Column, out var nv) && Str(nv) != null && !string.Equals(Str(nv), fromState, StringComparison.OrdinalIgnoreCase))
                {
                    toState = Str(nv);
                    CheckTransition(cmd, caller, table, lc, fromState, toState, id, current);
                }
            }
            if (im != null && !isSoftDelete)
                foreach (var col in im)
                    if (values.TryGetValue(col, out var v) && !SameValue(v, current.GetValueOrDefault(col)))
                        throw new DataGuardException($"{table}.{col} is read-only once created.", 409);
            var changes = values.Where(kv => !kv.Key.Equals("modifiedat", StringComparison.OrdinalIgnoreCase) && !kv.Key.Equals("modifiedby", StringComparison.OrdinalIgnoreCase)
                                             && current.ContainsKey(kv.Key) && !SameValue(kv.Value, current[kv.Key]))
                                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            if (!isSoftDelete && changes.Count > 0)
            {
                var merged = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in changes) merged[k] = v;
                Validate(table, merged, changes.Keys);
            }
            After(cmd, (c, _) =>
            {
                var after = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in changes) after[k] = v;
                if (toState != null)
                {
                    WriteHistory(c, caller, lc, after, id, fromState, toState);
                    StampTimestamp(c, table, lc, toState, id);
                    UpdateSla(c, caller, lc, after, id, fromState, toState);
                }
                WriteAudit(c, caller, table, isSoftDelete ? "delete" : "update", id, changes, current);
                WriteOutbox(c, caller, table, isSoftDelete ? "delete" : "update", id, after, current);
            });
        }

        static partial void BeforeDeleteImpl(MySqlCommand cmd, Caller caller, string table)
        {
            if (_immutable.TryGetValue(table, out var im) && im.Contains("*"))
                throw new DataGuardException($"{table} is append-only: records can't be deleted.", 409);
            After(cmd, (c, _) => WriteAudit(c, caller, table, "hard_delete", null, null, null));
        }

        // ------------------------------------------------------------------ lifecycle

        static void CheckTransition(MySqlCommand cmd, Caller caller, string table, Lifecycle lc, string from, string to, object id, Dictionary<string, object> current)
        {
            var role = RoleName(cmd.Connection, caller);
            bool Bypasses(Transition t)
            {
                if (caller.RoleId != 1) return false;
                var mode = t.AdminBypass ?? lc.AdminBypass;
                return mode == "any" || (mode == "platform" && caller.IsPlatform);
            }
            var candidates = lc.Transitions.Where(t => (t.From.Contains("*") || t.From.Contains(from ?? "")) && t.To.Contains(to)).ToList();
            if (candidates.Count == 0)
                throw new DataGuardException($"{table}: moving from '{from}' to '{to}' is not an allowed step.", 409);
            var actor = caller.IsAi ? "ai" : "human";
            var byActor = candidates.Where(t => t.Actors.Contains(actor) || t.Actors.Contains("*")).ToList();
            if (byActor.Count == 0)
                throw new DataGuardException($"{table}: '{from}' to '{to}' must be done by a person, not the AI.", 403);
            var byRole = byActor.Where(t => t.Roles.Contains("*") || t.Roles.Contains(role ?? "") || Bypasses(t)).ToList();
            if (byRole.Count == 0)
                throw new DataGuardException($"{table}: your role ({role}) can't move a record from '{from}' to '{to}'.", 403);
            string firstFailure = null;
            foreach (var t in byRole)
            {
                // separation of duties and negative requirements apply to everyone, administrators included
                var same = t.DistinctFrom.FirstOrDefault(d => IsSamePerson(cmd, caller, d, current.GetValueOrDefault(d.Column)));
                if (same != null)
                {
                    firstFailure ??= same.Message ?? $"the person in {same.Column} can't also do this step";
                    continue;
                }
                var failed = t.Requires.FirstOrDefault(r => !RequirementMet(cmd, table, r, r.LinkFrom != null ? current.GetValueOrDefault(r.LinkFrom) : id));
                if (failed != null) { firstFailure ??= failed.Message ?? $"needs a {failed.Table} record matching {JsonSerializer.Serialize(failed.Where)}"; continue; }
                var present = t.Forbids.FirstOrDefault(r => RequirementMet(cmd, table, r, r.LinkFrom != null ? current.GetValueOrDefault(r.LinkFrom) : id));
                if (present != null) { firstFailure ??= present.Message ?? $"there is a {present.Table} record matching {JsonSerializer.Serialize(present.Where)}"; continue; }
                return;
            }
            throw new DataGuardException($"{table}: can't move to '{to}' yet: {firstFailure}.", 409);
        }

        /// <summary>
        /// True when the caller is the person recorded in the column: a users id or login name directly, or (with Table/Key)
        /// a row of Table whose UserColumn is the caller (e.g. members.user_id).
        /// </summary>
        static bool IsSamePerson(MySqlCommand cmd, Caller caller, DistinctRule d, object value)
        {
            var v = Str(value);
            if (string.IsNullOrEmpty(v)) return false;
            if (d.Table == null)
                return v == caller.UserId.ToString() || string.Equals(v, caller.User, StringComparison.OrdinalIgnoreCase);
            var key = d.Key ?? PrimaryKeys.GetValueOrDefault(d.Table);
            using var q = new MySqlCommand($"SELECT `{SafeIdentifier(d.UserColumn)}` FROM `{SafeIdentifier(d.Table)}` WHERE `{SafeIdentifier(key)}` = @v LIMIT 1", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@v", value);
            var u = Str(q.ExecuteScalar());
            return u != null && (u == caller.UserId.ToString() || string.Equals(u, caller.User, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>A related row must exist: r.Table where r.Link = (this row's r.LinkFrom column, default its primary key) and r.Where matches.</summary>
        static bool RequirementMet(MySqlCommand cmd, string table, Requirement r, object id)
        {
            if (id == null) return false;
            var link = r.Link ?? PrimaryKeys.GetValueOrDefault(table);
            var where = string.Join("", r.Where.Select((kv, i) => $" AND t.`{SafeIdentifier(kv.Key)}` = @w{i}"));
            using var q = new MySqlCommand($"SELECT COUNT(*) FROM `{SafeIdentifier(r.Table)}` t WHERE t.`{SafeIdentifier(link)}` = @id AND t.isActive = 1{where}", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@id", id);
            var i2 = 0; foreach (var kv in r.Where) q.Parameters.AddWithValue("@w" + i2++, kv.Value);
            return Convert.ToInt64(Apply(q).ExecuteScalar()) > 0;
        }

        static void WriteHistory(MySqlCommand c, Caller caller, Lifecycle lc, Dictionary<string, object> row, object id, string from, string to)
        {
            if (lc.HistoryTable == null) return;
            var vals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [lc.HistoryFk ?? "id"] = id, [lc.HistoryTo ?? "new_status"] = to,
            };
            if (lc.HistoryFrom != null) vals[lc.HistoryFrom] = from;
            if (lc.HistoryActor != null) vals[lc.HistoryActor] = caller.IsSystem ? "system" : caller.IsAi ? "ai" : "human";
            if (lc.HistoryReason != null) vals[lc.HistoryReason] = $"by {caller.User}";
            InsertInternal(c, lc.HistoryTable, vals, caller, row);
        }

        static void StampTimestamp(MySqlCommand c, string table, Lifecycle lc, string state, object id)
        {
            if (!lc.Timestamps.TryGetValue(state, out var col) || !Columns[table].Contains(col)) return;
            using var u = new MySqlCommand($"UPDATE `{table}` SET `{SafeIdentifier(col)}` = UTC_TIMESTAMP() WHERE `{PrimaryKeys[table]}` = @id AND `{col}` IS NULL", c.Connection, c.Transaction);
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
        }

        /// <summary>Leaving a state closes its open deadline (met); entering a state with SLA hours opens a new deadline row.</summary>
        static void UpdateSla(MySqlCommand c, Caller caller, Lifecycle lc, Dictionary<string, object> row, object id, string from, string to)
        {
            if (lc.SlaTable == null || !Columns.TryGetValue(lc.SlaTable, out var cols)) return;
            if (from != null && lc.SlaHours.ContainsKey(from))
            {
                using var u = new MySqlCommand($"UPDATE `{lc.SlaTable}` SET `{lc.SlaMet}` = UTC_TIMESTAMP() WHERE `{lc.SlaLink}` = @id AND `{lc.SlaType}` = @t AND `{lc.SlaMet}` IS NULL AND isActive = 1",
                    c.Connection, c.Transaction);
                u.Parameters.AddWithValue("@id", id); u.Parameters.AddWithValue("@t", from);
                u.ExecuteNonQuery();
            }
            if (lc.SlaHours.TryGetValue(to, out var hours))
                InsertInternal(c, lc.SlaTable, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    [lc.SlaLink] = id, [lc.SlaType] = to, [lc.SlaDue] = DateTime.UtcNow.AddHours(hours),
                }, caller, row);
        }

        /// <summary>Plan/usage limits: rows (or the sum of a column) per tenant, optionally per month, against a maximum read from the tenant's subscription/plan.</summary>
        static void CheckLimit(MySqlCommand cmd, Caller caller, Limit lim, Dictionary<string, object> values)
        {
            if (caller.Tenant == null || !TenantTables.Contains(lim.Table)) return;
            if (lim.Where.Any(kv => !string.Equals(Str(values.GetValueOrDefault(kv.Key)), kv.Value, StringComparison.OrdinalIgnoreCase))) return;
            decimal? max = null;
            if (lim.MaxFrom != null)
            {
                var from = SafeIdentifier(lim.MaxFrom);
                var src = lim.MaxJoin != null ? $"`{SafeIdentifier(lim.MaxJoin)}` j JOIN `{from}` m ON j.`{SafeIdentifier(lim.MaxJoinOn)}` = m.`{SafeIdentifier(lim.MaxJoinOn)}`" : $"`{from}` m";
                var owner = lim.MaxJoin ?? from;
                var alias = lim.MaxJoin != null ? "j" : "m";
                var where = string.Join("", lim.MaxWhere.Select((kv, i) => $" AND {alias}.`{SafeIdentifier(kv.Key)}` = @mw{i}"));
                using var q = new MySqlCommand($"SELECT m.`{SafeIdentifier(lim.MaxColumn)}` FROM {src} WHERE {alias}.`{TenantColumn}` = @t AND {alias}.isActive = 1{where} ORDER BY 1 DESC LIMIT 1",
                    cmd.Connection, cmd.Transaction);
                q.Parameters.AddWithValue("@t", caller.Tenant.Value);
                var i2 = 0; foreach (var kv in lim.MaxWhere) q.Parameters.AddWithValue("@mw" + i2++, kv.Value);
                var v = q.ExecuteScalar();
                if (v != null && v is not DBNull) max = Convert.ToDecimal(v);
            }
            if (max == null)
            {
                if (lim.OnMissing == "block") throw new DataGuardException(lim.Message ?? $"No active plan allows creating {lim.Table}.", 402);
                return;
            }
            var measure = lim.Measure == "sum" ? $"COALESCE(SUM(`{SafeIdentifier(lim.SumColumn)}`), 0)" : "COUNT(*)";
            var conds = string.Join("", lim.Where.Select((kv, i) => $" AND `{SafeIdentifier(kv.Key)}` = @w{i}"));
            if (lim.Period == "month") conds += " AND createdAt >= DATE_FORMAT(UTC_TIMESTAMP(), '%Y-%m-01')";
            using var u = new MySqlCommand($"SELECT {measure} FROM `{lim.Table}` WHERE `{TenantColumn}` = @t AND isActive = 1{conds}", cmd.Connection, cmd.Transaction);
            u.Parameters.AddWithValue("@t", caller.Tenant.Value);
            var i3 = 0; foreach (var kv in lim.Where) u.Parameters.AddWithValue("@w" + i3++, kv.Value);
            var used = Convert.ToDecimal(u.ExecuteScalar());
            var adding = lim.Measure == "sum" ? (decimal.TryParse(Str(values.GetValueOrDefault(lim.SumColumn)), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var a) ? a : 0) : 1;
            if (used + adding > max)
                throw new DataGuardException(lim.Message ?? $"Plan limit reached for {lim.Table}: {used} of {max} used.", 402);
        }

        // ------------------------------------------------------------------ audit & outbox

        static void WriteAudit(MySqlCommand c, Caller caller, string table, string action, object id, Dictionary<string, object> changes, Dictionary<string, object> before)
        {
            if (_auditTable == null || _auditExclude.Contains(table) || !Columns.ContainsKey(_auditTable)) return;
            var details = new JsonObject();
            if (changes != null)
                foreach (var (k, v) in changes.Where(kv => kv.Key != "createdby" && kv.Key != "modifiedby"))
                {
                    var after = Short(v);
                    details[k] = before != null && before.ContainsKey(k) ? new JsonObject { ["from"] = Short(before[k]), ["to"] = after } : after;
                }
            var row = before ?? changes;
            var rowTenant = (object)caller.Tenant ?? row?.GetValueOrDefault(TenantColumn);
            var target = rowTenant == null && _auditPlatformTable != null && Columns.ContainsKey(_auditPlatformTable) ? _auditPlatformTable : _auditTable;
            InsertInternal(c, target, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["actor_type"] = caller.IsSystem ? "system" : caller.IsAi ? "ai" : "human",
                ["actor_ref"] = caller.User,
                ["action_code"] = $"{table}.{action}",
                ["entity_type"] = table,
                ["entity_ref"] = id?.ToString(),
                ["details"] = details.ToJsonString(),
                ["correlation_ref"] = caller.CorrelationId,
                ["ip_address"] = caller.Ip,
            }, caller, row);
        }

        static void WriteOutbox(MySqlCommand c, Caller caller, string table, string on, object id, Dictionary<string, object> row, Dictionary<string, object> before)
        {
            if (_outboxTable == null || table.Equals(_outboxTable, StringComparison.OrdinalIgnoreCase) || !Columns.ContainsKey(_outboxTable)) return;
            var events = new List<string>();
            foreach (var r in _outboxRules.Where(r => r.Table.Equals(table, StringComparison.OrdinalIgnoreCase) && r.On.Equals(on, StringComparison.OrdinalIgnoreCase)))
            {
                var match = r.When.All(kv => string.Equals(Str(row.GetValueOrDefault(kv.Key)), kv.Value, StringComparison.OrdinalIgnoreCase)
                                             && (before == null || !string.Equals(Str(before.GetValueOrDefault(kv.Key)), kv.Value, StringComparison.OrdinalIgnoreCase)));
                if (match) events.Add(r.Event);
            }
            if (events.Count == 0 && _outboxAll) events.Add($"{table}.{on}");
            foreach (var ev in events)
            {
                var payload = new JsonObject();
                foreach (var (k, v) in row) if (k != "createdby" && k != "modifiedby") payload[k] = Short(v, 2000);
                InsertInternal(c, _outboxTable, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["event_type"] = ev, ["aggregate_type"] = table, ["aggregate_ref"] = id?.ToString(), ["schema_version"] = "1.0",
                    ["correlation_ref"] = caller.CorrelationId, ["payload"] = new JsonObject
                    {
                        ["eventType"] = ev, ["tenantId"] = Str(row.GetValueOrDefault(TenantColumn)), ["aggregateId"] = id?.ToString(),
                        ["occurredAt"] = DateTime.UtcNow.ToString("o"), ["schemaVersion"] = "1.0", ["correlationId"] = caller.CorrelationId, ["payload"] = payload,
                    }.ToJsonString(),
                    ["status"] = "pending", ["attempts"] = 0,
                }, caller, row);
            }
        }

        /// <summary>Inserts a system row (history/audit/outbox) using only the columns the table has; tenant copied from the source row.</summary>
        static void InsertInternal(MySqlCommand c, string table, Dictionary<string, object> vals, Caller caller, Dictionary<string, object> sourceRow)
        {
            if (!Columns.TryGetValue(table, out var cols)) return;
            var v = vals.Where(kv => cols.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            if (cols.Contains(TenantColumn))
                v[TenantColumn] = (object)caller.Tenant ?? sourceRow?.GetValueOrDefault(TenantColumn) ?? DBNull.Value;
            if (cols.Contains("createdby")) v["createdBy"] = caller.Owner ?? caller.User;
            if (cols.Contains("modifiedby")) v["modifiedBy"] = caller.User ?? caller.Owner;
            if (cols.Contains(TenantColumn) && v[TenantColumn] is DBNull or null) return; // tenant row without a tenant: nothing to attach to
            var names = v.Keys.ToList();
            using var ins = new MySqlCommand($"INSERT INTO `{table}` ({string.Join(", ", names.Select(n => "`" + n + "`"))}) VALUES ({string.Join(", ", names.Select((_, i) => "@p" + i))})",
                c.Connection, c.Transaction);
            for (var i = 0; i < names.Count; i++) ins.Parameters.AddWithValue("@p" + i, v[names[i]] ?? DBNull.Value);
            ins.ExecuteNonQuery();
        }

        /// <summary>Background jobs: insert a row (plus its audit and outbox rows) as the system, in the source row's workspace and tenant.</summary>
        public static void SystemInsert(MySqlConnection c, MySqlTransaction tx, string table, Dictionary<string, object> values, Dictionary<string, object> sourceRow)
        {
            var caller = new Caller { User = "system", Owner = Str(sourceRow.GetValueOrDefault("createdby")), IsSystem = true, CorrelationId = Guid.NewGuid().ToString("N") };
            using var holder = new MySqlCommand("", c, tx);
            InsertInternal(holder, table, values, caller, sourceRow);
            WriteAudit(holder, caller, table, "create", null, values, null);
            WriteOutbox(holder, caller, table, "insert", null, new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase) { [TenantColumn] = sourceRow.GetValueOrDefault(TenantColumn) }, null);
        }

        // ------------------------------------------------------------------ helpers

        static object KeyValue(MySqlCommand cmd, string table, Dictionary<string, object> values)
        {
            if (!PrimaryKeys.TryGetValue(table, out var pk)) return null;
            var m = Regex.Match(cmd.CommandText, $@"\bWHERE\b.*?`?\b{Regex.Escape(pk)}\b`?\s*=\s*(@\w+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success && cmd.Parameters.Contains(m.Groups[1].Value)) return cmd.Parameters[m.Groups[1].Value].Value;
            if (!values.TryGetValue(pk, out var v)) return null;
            return v is string s && s.StartsWith("@") ? null : v;
        }

        static Dictionary<string, object> LoadVisible(MySqlCommand cmd, string table, object id)
        {
            using var q = new MySqlCommand($"SELECT t.* FROM `{table}` t WHERE t.`{PrimaryKeys[table]}` = @id", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@id", id);
            using var r = Apply(q).ExecuteReader();
            if (!r.Read()) return null;
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            return d;
        }

        static string RoleName(MySqlConnection _, Caller caller)
        {
            if (_roleNames.TryGetValue(caller.RoleId, out var n)) return n;
            // own connection: callers may be in the middle of reading from theirs
            using var c = new MySqlConnection(ConnectionString);
            c.Open();
            using var q = new MySqlCommand("SELECT role_name FROM roles WHERE role_id = @r", c);
            q.Parameters.AddWithValue("@r", caller.RoleId);
            n = q.ExecuteScalar()?.ToString();
            if (n != null) _roleNames[caller.RoleId] = n;
            return n;
        }

        static string Str(object v) => v == null || v is DBNull ? null : v is DateTime d ? d.ToString("yyyy-MM-dd HH:mm:ss") : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
        static bool SameValue(object a, object b)
        {
            var x = Str(a); var y = Str(b);
            if (x == y) return true;
            if (x == null || y == null) return string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y);
            if (decimal.TryParse(x, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dx) &&
                decimal.TryParse(y, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dy)) return dx == dy;
            if (DateTime.TryParse(x, out var tx) && DateTime.TryParse(y, out var ty)) return tx == ty;
            return string.Equals(x, y, StringComparison.Ordinal);
        }
        static JsonNode Short(object v, int max = 500)
        {
            var s = Str(v);
            return s == null ? null : JsonValue.Create(s.Length > max ? s.Substring(0, max) + "…" : s);
        }
    }
}
