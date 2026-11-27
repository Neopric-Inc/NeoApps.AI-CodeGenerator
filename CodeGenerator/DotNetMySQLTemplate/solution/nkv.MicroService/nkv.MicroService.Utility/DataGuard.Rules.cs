using MySqlConnector;
using System.Data.Common;
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
            public string HistoryTable, HistoryFk, HistoryFrom, HistoryTo, HistoryActor, HistoryReason, HistoryBy;
            public Dictionary<string, string> Timestamps = new(StringComparer.OrdinalIgnoreCase);
            public string SlaTable, SlaLink, SlaType, SlaDue, SlaMet;
            public Dictionary<string, double> SlaHours = new(StringComparer.OrdinalIgnoreCase);
            public string AdminBypass = "any";   // any | platform | none
            /// <summary>"editableIn": role -> states in which that role may change or delete a record (other roles: any state).</summary>
            public Dictionary<string, HashSet<string>> EditableIn = new(StringComparer.OrdinalIgnoreCase);
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
            public string Meaning;                           // electronic signature: password re-entry + this statement
            public string SignedBy, SignedAt;                // optional columns stamped with the signer and UTC time
            public List<JsonObject> Checks = new();          // conditions on the record and the person ("check")
            public JsonArray Creates;                        // records created by this step ("creates")
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
        static readonly ConditionalWeakTable<DbCommand, List<Action<DbCommand, int>>> _after = new();

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
                    ["signatures"] = kv.Value.Transitions.Count(t => t.Meaning != null),
                    ["adminBypass"] = kv.Value.AdminBypass,
                    ["frozen"] = new JsonArray(kv.Value.Frozen.Select(x => (JsonNode)x).ToArray()),
                    ["editableIn"] = new JsonObject(kv.Value.EditableIn.Select(e => KeyValuePair.Create(e.Key, (JsonNode)new JsonArray(e.Value.Select(x => (JsonNode)x).ToArray())))),
                }))),
                ["immutable"] = new JsonObject(_immutable.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode)new JsonArray(kv.Value.Select(x => (JsonNode)x).ToArray())))),
                ["rowScopes"] = ScopesSummary(),
                ["numbering"] = new JsonArray(_numbering.Values.SelectMany(l => l).Select(n => (JsonNode)$"{n.Table}.{n.Column}: {n.Pattern} (reset {n.Reset})").ToArray()),
                ["notifications"] = _notifyRules.Count,
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

        static partial void InitExtensionsImpl(DbConnection c)
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
                            if (tn["check"] is JsonObject chk) t.Checks.Add(chk); else if (tn["check"] is JsonArray chks) t.Checks.AddRange(chks.OfType<JsonObject>());
                            if (tn["creates"] is JsonObject cro) t.Creates = new JsonArray(cro.DeepClone()); else if (tn["creates"] is JsonArray cra) t.Creates = cra;
                            if (tn["signature"] is JsonValue sv) t.Meaning = sv.TryGetValue<bool>(out var on) ? (on ? $"I approve moving this record to {string.Join("/", t.To)}." : null) : sv.GetValue<string>();
                            else if (tn["signature"] is JsonObject so)
                            {
                                t.Meaning = so["meaning"]?.GetValue<string>() ?? $"I approve moving this record to {string.Join("/", t.To)}.";
                                t.SignedBy = so["by"]?.GetValue<string>(); t.SignedAt = so["at"]?.GetValue<string>();
                            }
                            if (t.Meaning != null) t.Actors = new(StringComparer.OrdinalIgnoreCase) { "human" }; // only a person can sign
                            lc.Transitions.Add(t);
                        }
                    if (o["adminBypass"] != null) lc.AdminBypass = Bypass(o["adminBypass"]);
                    if (o["editableIn"] is JsonObject ei) foreach (var (role, states) in ei) lc.EditableIn[role] = Set(states);
                    if (o["history"] is JsonObject h)
                    {
                        lc.HistoryTable = h["table"]?.GetValue<string>(); lc.HistoryFk = h["link"]?.GetValue<string>();
                        lc.HistoryFrom = h["from"]?.GetValue<string>(); lc.HistoryTo = h["to"]?.GetValue<string>();
                        lc.HistoryActor = h["actor"]?.GetValue<string>(); lc.HistoryReason = h["reason"]?.GetValue<string>();
                        lc.HistoryBy = h["by"]?.GetValue<string>();
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
            InitNotifications(c);
            LoadPolicies();
        }

        // ------------------------------------------------------------------ write execution

        /// <summary>Runs an insert/update/delete with all rules applied; history, audit and outbox rows are written in the same transaction.</summary>
        public static int NonQuery(DbCommand cmd)
        {
            var previous = _writing.Value;
            try { return NonQueryCore(cmd); }
            catch (DataGuardException e) { RecordDenial(e); throw; }
            finally { _writing.Value = previous; }
        }

        static int NonQueryCore(DbCommand cmd)
        {
            Apply(cmd);
            if (!_after.TryGetValue(cmd, out var actions) || actions.Count == 0) return Db.Execute(cmd);
            var own = cmd.Transaction == null ? cmd.Connection.BeginTransaction() : null;
            if (own != null) cmd.Transaction = own;
            try
            {
                var n = Db.Execute(cmd);
                if (n > 0) foreach (var a in actions) a(cmd, n);
                own?.Commit();
                return n;
            }
            catch { own?.Rollback(); throw; }
            finally { if (own != null) cmd.Transaction = null; _after.Remove(cmd); }
        }

        static void After(DbCommand cmd, Action<DbCommand, int> action)
        {
            if (!_after.TryGetValue(cmd, out var list)) _after.Add(cmd, list = new List<Action<DbCommand, int>>());
            list.Add(action);
        }

        // ------------------------------------------------------------------ hooks

        static partial void BeforeInsertImpl(DbCommand cmd, Caller caller, string table, Dictionary<string, object> values)
        {
            _writing.Value = (table, null, values);
            Validate(table, values, null);
            CheckAllowedReferences(cmd, caller, table, values, null);
            foreach (var lim in _limits.Where(l => l.Table.Equals(table, StringComparison.OrdinalIgnoreCase)))
                CheckLimit(cmd, caller, lim, values);
            if (_lifecycles.TryGetValue(table, out var lc) && values.TryGetValue(lc.Column, out var sv) && lc.Initial != null)
            {
                var s = Str(sv);
                if (s != null && !lc.Initial.Contains(s))
                    throw new DataGuardException($"A new {Label(table).ToLowerInvariant()} must start as {string.Join(" or ", lc.Initial)}, not {s}.", 409);
            }
            After(cmd, (c, _) =>
            {
                var id = Db.LastInsertedId(c);
                var row = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
                if (PrimaryKeys.TryGetValue(table, out var pk)) row[pk] = id;
                CheckBudgets(c, caller, table, row, null);
                if (_lifecycles.TryGetValue(table, out var l) && row.TryGetValue(l.Column, out var st) && Str(st) != null)
                {
                    WriteHistory(c, caller, l, row, id, null, Str(st));
                    StampTimestamp(c, table, l, Str(st), id);
                    UpdateSla(c, caller, l, row, id, null, Str(st));
                }
                WriteAudit(c, caller, table, "create", id, row, null);
                WriteOutbox(c, caller, table, "insert", id, row, null);
                Notify(c, caller, table, "create", id, row, null);
            });
        }

        static partial void BeforeUpdateImpl(DbCommand cmd, Caller caller, string table, Dictionary<string, object> values)
        {
            var isSoftDelete = values.Count == 1 && values.TryGetValue("isactive", out var ia) && Str(ia) == "0";
            if (_immutable.TryGetValue(table, out var im) && im.Contains("*"))
                throw new DataGuardException($"{Label(table)} records can't be changed or deleted once saved.", 409);
            var id = KeyValue(cmd, table, values);
            if (id == null)
            {
                // the rules can't tell which record changes (several rows, or a key that isn't a value): refuse rather than skip them
                var guarded = (_lifecycles.TryGetValue(table, out var lcx) && values.ContainsKey(lcx.Column))
                    || (_immutable.TryGetValue(table, out var imx) && values.Keys.Any(k => imx.Contains(k)));
                if (guarded)
                    throw new DataGuardException($"Change one {Label(table).ToLowerInvariant()} at a time (by its key), so its rules can be checked.", 400);
                return;
            }
            var current = LoadVisible(cmd, table, id);
            if (current == null) return; // not visible: the scoped UPDATE affects nothing
            _writing.Value = (table, id, values);
            var proposed = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in values) if (!(v is string ps && ps.StartsWith("@"))) proposed[k] = v;
            string fromState = null, toState = null;
            Transition signed = null, matched = null;
            if (_lifecycles.TryGetValue(table, out var lc))
            {
                fromState = Str(current.GetValueOrDefault(lc.Column));
                if (lc.Frozen.Contains(fromState ?? "") && !isSoftDelete)
                    throw new DataGuardException($"This {Label(table).ToLowerInvariant()} is {fromState} and can no longer be changed.", 409);
                CheckEditable(cmd, caller, table, lc, fromState, isSoftDelete ? "delete" : "change");
                if (values.TryGetValue(lc.Column, out var nv) && Str(nv) != null && !string.Equals(Str(nv), fromState, StringComparison.OrdinalIgnoreCase))
                {
                    toState = Str(nv);
                    matched = CheckTransition(cmd, caller, table, lc, fromState, toState, id, current, proposed);
                    signed = matched;
                    if (signed?.Meaning != null) VerifySignature(cmd, caller, table, toState, signed.Meaning);
                    else signed = null;
                }
            }
            if (im != null && !isSoftDelete)
                foreach (var col in im)
                    if (values.TryGetValue(col, out var v) && !SameValue(v, current.GetValueOrDefault(col)))
                    {
                        // a full-record update that leaves a read-only column empty (an API caller or an AI tool that
                        // omitted the automatic number) keeps the stored value instead of failing
                        if ((v == null || v is DBNull || (v is string sv && sv.Length == 0)) && current.GetValueOrDefault(col) is { } kept && kept is not DBNull
                            && KeepValue(cmd, col, kept))
                        {
                            values[col] = kept;
                            continue;
                        }
                        throw new DataGuardException($"{FieldLabel(table, col)} can't be changed once the {Label(table).ToLowerInvariant()} is created.", 409);
                    }
            var changes = values.Where(kv => !kv.Key.Equals("modifiedat", StringComparison.OrdinalIgnoreCase) && !kv.Key.Equals("modifiedby", StringComparison.OrdinalIgnoreCase)
                                             && current.ContainsKey(kv.Key) && !SameValue(kv.Value, current[kv.Key]))
                                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            if (!isSoftDelete && changes.Count > 0)
            {
                var merged = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in changes) merged[k] = v;
                Validate(table, merged, changes.Keys);
                CheckAllowedReferences(cmd, caller, table, changes, current);
            }
            After(cmd, (c, _) =>
            {
                var after = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in changes) after[k] = v;
                if (!isSoftDelete) CheckBudgets(c, caller, table, after, current);
                if (toState != null && matched?.Creates != null) CreateOnTransition(c, caller, table, matched.Creates, id, after);
                if (toState != null)
                {
                    WriteHistory(c, caller, lc, after, id, fromState, toState, signed?.Meaning);
                    if (signed != null) StampSignature(c, table, signed, caller, id);
                    StampTimestamp(c, table, lc, toState, id);
                    UpdateSla(c, caller, lc, after, id, fromState, toState);
                }
                var audited = changes;
                if (signed != null) audited = new Dictionary<string, object>(changes, StringComparer.OrdinalIgnoreCase) { ["signature"] = $"{caller.User} re-entered their password: {signed.Meaning}" };
                WriteAudit(c, caller, table, isSoftDelete ? "delete" : "update", id, audited, current);
                WriteOutbox(c, caller, table, isSoftDelete ? "delete" : "update", id, after, current);
                if (!isSoftDelete) Notify(c, caller, table, "update", id, after, current);
            });
        }

        static partial void BeforeDeleteImpl(DbCommand cmd, Caller caller, string table)
        {
            _writing.Value = (table, null, null);
            if (_immutable.TryGetValue(table, out var im) && im.Contains("*"))
                throw new DataGuardException($"{Label(table)} records can't be deleted.", 409);
            if (_lifecycles.TryGetValue(table, out var lc) && lc.EditableIn.Count > 0
                && KeyValue(cmd, table, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)) is { } id && LoadVisible(cmd, table, id) is { } current)
                CheckEditable(cmd, caller, table, lc, Str(current.GetValueOrDefault(lc.Column)), "delete");
            After(cmd, (c, _) => WriteAudit(c, caller, table, "hard_delete", null, null, null));
        }

        // ------------------------------------------------------------------ lifecycle

        /// <summary>"editableIn": a listed role may change (or delete) a record only while it is in one of its states. The change
        /// that moves a record out of such a state (submitting a draft) is made in that state, so it is allowed.</summary>
        static void CheckEditable(DbCommand cmd, Caller caller, string table, Lifecycle lc, string state, string verb)
        {
            if (lc.EditableIn.Count == 0) return;
            var role = RoleName(cmd.Connection, caller);
            if (role == null || !lc.EditableIn.TryGetValue(role, out var states)) return;
            if (states.Contains("*") || states.Contains(state ?? "")) return;
            throw new DataGuardException($"This {Label(table).ToLowerInvariant()} is {state}: your role ({role}) can only {verb} it while it is {string.Join(" or ", states)}.", 409);
        }

        static Transition CheckTransition(DbCommand cmd, Caller caller, string table, Lifecycle lc, string from, string to, object id, Dictionary<string, object> current, Dictionary<string, object> proposed = null)
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
                throw new DataGuardException($"{char.ToUpper(A(Label(table))[0]) + A(Label(table))[1..]} can't go from {from} to {to}.", 409);
            var actor = caller.IsAi ? "ai" : "human";
            var byActor = candidates.Where(t => t.Actors.Contains(actor) || t.Actors.Contains("*")).ToList();
            if (byActor.Count == 0)
                throw new DataGuardException($"Moving {A(Label(table))} from {from} to {to} must be done by a person, not the AI.", 403);
            var byRole = byActor.Where(t => t.Roles.Contains("*") || t.Roles.Contains(role ?? "") || Bypasses(t)).ToList();
            if (byRole.Count == 0)
                throw new DataGuardException($"Your role ({role}) can't move {A(Label(table))} from {from} to {to}.", 403);
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
                var check = t.Checks.Select(ch => CheckFails(table, ch, proposed ?? current, caller)).FirstOrDefault(x => x != null);
                if (check != null) { firstFailure ??= check; continue; }
                return t;
            }
            throw new DataGuardException($"This {Label(table).ToLowerInvariant()} can't move to {to}: {firstFailure}.", 409);
        }

        /// <summary>
        /// True when the caller is the person recorded in the column: a users id or login name directly, or (with Table/Key)
        /// a row of Table whose UserColumn is the caller (e.g. members.user_id).
        /// </summary>
        static bool IsSamePerson(DbCommand cmd, Caller caller, DistinctRule d, object value)
        {
            var v = Str(value);
            if (string.IsNullOrEmpty(v)) return false;
            if (d.Table == null)
                return v == caller.UserId.ToString() || string.Equals(v, caller.User, StringComparison.OrdinalIgnoreCase);
            var key = d.Key ?? PrimaryKeys.GetValueOrDefault(d.Table);
            using var q = Db.Command($"SELECT {Db.Top(1)}{Db.Q(SafeIdentifier(d.UserColumn))} FROM {Db.Q(SafeIdentifier(d.Table))} WHERE {Db.Q(SafeIdentifier(key))} = @v{Db.LimitEnd(1)}", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@v", value);
            var u = Str(q.ExecuteScalar());
            return u != null && (u == caller.UserId.ToString() || string.Equals(u, caller.User, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>A related row must exist: r.Table where r.Link = (this row's r.LinkFrom column, default its primary key) and r.Where matches.</summary>
        static bool RequirementMet(DbCommand cmd, string table, Requirement r, object id)
        {
            if (id == null) return false;
            var link = r.Link ?? PrimaryKeys.GetValueOrDefault(table);
            var where = string.Join("", r.Where.Select((kv, i) => $" AND t.{Db.Q(SafeIdentifier(kv.Key))} = @w{i}"));
            using var q = Db.Command($"SELECT COUNT(*) FROM {Db.Q(SafeIdentifier(r.Table))} t WHERE t.{Db.Q(SafeIdentifier(link))} = @id AND t.isActive = 1{where}", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@id", id);
            var i2 = 0; foreach (var kv in r.Where) q.Parameters.AddWithValue("@w" + i2++, kv.Value);
            return Convert.ToInt64(Apply(q).ExecuteScalar()) > 0;
        }

        /// <summary>Header carrying the signer's password (base64 UTF-8) for a step with "signature". Never stored or logged.</summary>
        public const string SignatureHeader = "X-NeoApps-Signature";

        /// <summary>
        /// Electronic signature: the person doing this step re-enters their password. Wrong passwords count toward the
        /// sign-in lockout. Administrators are not exempt, and the AI or a background job can never sign.
        /// </summary>
        static void VerifySignature(DbCommand cmd, Caller caller, string table, string to, string meaning)
        {
            if (caller.IsAi || caller.IsSystem || caller.UserId == 0)
                throw new DataGuardException($"{table}: moving to '{to}' needs a person's signature.", 403);
            string password = null;
            var h = Accessor?.HttpContext?.Request.Headers[SignatureHeader].ToString();
            if (!string.IsNullOrEmpty(h))
                try { password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(h)); } catch { password = null; }
            if (string.IsNullOrEmpty(password))
                throw new DataGuardException($"Signature required: {meaning}", 428);
            if (AuthGuard.LockedMinutes(caller.User) > 0)
                throw new DataGuardException("Too many wrong passwords. Try again later.", 423);
            using var q = Db.Command($"SELECT {Db.Top(1)}password_hash FROM users WHERE user_id = @u{Db.LimitEnd(1)}", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@u", caller.UserId);
            if (!PasswordHasher.Verify(password, q.ExecuteScalar() as string))
            {
                AuthGuard.RecordFailure(caller.User);
                throw new DataGuardException("Signature not accepted: the password didn't match.", 403);
            }
        }


        static void WriteHistory(DbCommand c, Caller caller, Lifecycle lc, Dictionary<string, object> row, object id, string from, string to, string meaning = null)
        {
            if (lc.HistoryTable == null) return;
            var vals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [lc.HistoryFk ?? "id"] = id, [lc.HistoryTo ?? "new_status"] = to,
            };
            if (lc.HistoryFrom != null) vals[lc.HistoryFrom] = from;
            if (lc.HistoryActor != null) vals[lc.HistoryActor] = caller.IsSystem ? "system" : caller.IsAi ? "ai" : "human";
            if (lc.HistoryReason != null) vals[lc.HistoryReason] = meaning == null ? $"by {caller.User}" : $"signed by {caller.User}: {meaning}";
            // who did it: "by" in the history settings, or a column named like it
            var by = lc.HistoryBy ?? (Columns.TryGetValue(lc.HistoryTable, out var hc)
                ? new[] { "changed_by", "actor_ref", "actor_name", "performed_by", "changed_by_user", "user_name" }.FirstOrDefault(n => hc.Contains(n) && n != lc.HistoryActor) : null);
            if (by != null) vals[by] = caller.IsSystem ? $"system ({caller.User})" : caller.IsAi ? $"{caller.User} via AI" : caller.User;
            InsertInternal(c, lc.HistoryTable, vals, caller, row);
        }

        static void StampSignature(DbCommand c, string table, Transition t, Caller caller, object id)
        {
            var sets = new List<string>();
            using var u = Db.Command("", c.Connection, c.Transaction);
            if (t.SignedBy != null && Columns[table].Contains(t.SignedBy)) { sets.Add($"{Db.Q(SafeIdentifier(t.SignedBy))} = @by"); u.Parameters.AddWithValue("@by", caller.User); }
            if (t.SignedAt != null && Columns[table].Contains(t.SignedAt)) sets.Add($"{Db.Q(SafeIdentifier(t.SignedAt))} = {Db.UtcNow}");
            if (sets.Count == 0) return;
            u.CommandText = $"UPDATE {Db.Q(table)} SET {string.Join(", ", sets)} WHERE {Db.Q(PrimaryKeys[table])} = @id";
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
        }

        static void StampTimestamp(DbCommand c, string table, Lifecycle lc, string state, object id)
        {
            if (!lc.Timestamps.TryGetValue(state, out var col) || !Columns[table].Contains(col)) return;
            using var u = Db.Command($"UPDATE {Db.Q(table)} SET {Db.Q(SafeIdentifier(col))} = {Db.UtcNow} WHERE {Db.Q(PrimaryKeys[table])} = @id AND {Db.Q(col)} IS NULL", c.Connection, c.Transaction);
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
        }

        /// <summary>Leaving a state closes its open deadline (met); entering a state with SLA hours opens a new deadline row.</summary>
        static void UpdateSla(DbCommand c, Caller caller, Lifecycle lc, Dictionary<string, object> row, object id, string from, string to)
        {
            if (lc.SlaTable == null || !Columns.TryGetValue(lc.SlaTable, out var cols)) return;
            if (from != null && lc.SlaHours.ContainsKey(from))
            {
                using var u = Db.Command($"UPDATE {Db.Q(lc.SlaTable)} SET {Db.Q(lc.SlaMet)} = {Db.UtcNow} WHERE {Db.Q(lc.SlaLink)} = @id AND {Db.Q(lc.SlaType)} = @t AND {Db.Q(lc.SlaMet)} IS NULL AND isActive = 1",
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
        static void CheckLimit(DbCommand cmd, Caller caller, Limit lim, Dictionary<string, object> values)
        {
            if (caller.Tenant == null || !TenantTables.Contains(lim.Table)) return;
            if (lim.Where.Any(kv => !string.Equals(Str(values.GetValueOrDefault(kv.Key)), kv.Value, StringComparison.OrdinalIgnoreCase))) return;
            decimal? max = null;
            if (lim.MaxFrom != null)
            {
                var from = SafeIdentifier(lim.MaxFrom);
                var src = lim.MaxJoin != null ? $"{Db.Q(SafeIdentifier(lim.MaxJoin))} j JOIN {Db.Q(from)} m ON j.{Db.Q(SafeIdentifier(lim.MaxJoinOn))} = m.{Db.Q(SafeIdentifier(lim.MaxJoinOn))}" : $"{Db.Q(from)} m";
                var owner = lim.MaxJoin ?? from;
                var alias = lim.MaxJoin != null ? "j" : "m";
                var where = string.Join("", lim.MaxWhere.Select((kv, i) => $" AND {alias}.{Db.Q(SafeIdentifier(kv.Key))} = @mw{i}"));
                using var q = Db.Command($"SELECT {Db.Top(1)}m.{Db.Q(SafeIdentifier(lim.MaxColumn))} FROM {src} WHERE {alias}.{Db.Q(TenantColumn)} = @t AND {alias}.isActive = 1{where} ORDER BY 1 DESC{Db.LimitEnd(1)}",
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
            var measure = lim.Measure == "sum" ? $"COALESCE(SUM({Db.Q(SafeIdentifier(lim.SumColumn))}), 0)" : "COUNT(*)";
            var conds = string.Join("", lim.Where.Select((kv, i) => $" AND {Db.Q(SafeIdentifier(kv.Key))} = @w{i}"));
            if (lim.Period == "month") conds += " AND createdAt >= " + Db.MonthStart;
            using var u = Db.Command($"SELECT {measure} FROM {Db.Q(lim.Table)} WHERE {Db.Q(TenantColumn)} = @t AND isActive = 1{conds}", cmd.Connection, cmd.Transaction);
            u.Parameters.AddWithValue("@t", caller.Tenant.Value);
            var i3 = 0; foreach (var kv in lim.Where) u.Parameters.AddWithValue("@w" + i3++, kv.Value);
            var used = Convert.ToDecimal(u.ExecuteScalar());
            var adding = lim.Measure == "sum" ? (decimal.TryParse(Str(values.GetValueOrDefault(lim.SumColumn)), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var a) ? a : 0) : 1;
            if (used + adding > max)
                throw new DataGuardException(lim.Message ?? $"Plan limit reached for {lim.Table}: {used} of {max} used.", 402);
        }

        // ------------------------------------------------------------------ audit & outbox

        static void WriteAudit(DbCommand c, Caller caller, string table, string action, object id, Dictionary<string, object> changes, Dictionary<string, object> before)
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

        static void WriteOutbox(DbCommand c, Caller caller, string table, string on, object id, Dictionary<string, object> row, Dictionary<string, object> before)
        {
            if (_outboxTable == null || table.Equals(_outboxTable, StringComparison.OrdinalIgnoreCase) || !Columns.ContainsKey(_outboxTable)) return;
            GuardJobs.WakeOutbox(); // serverless: publish after this write instead of polling
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
        static void InsertInternal(DbCommand c, string table, Dictionary<string, object> vals, Caller caller, Dictionary<string, object> sourceRow)
        {
            if (!Columns.TryGetValue(table, out var cols)) return;
            var v = vals.Where(kv => cols.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            if (cols.Contains(TenantColumn))
                v[TenantColumn] = (object)caller.Tenant ?? sourceRow?.GetValueOrDefault(TenantColumn) ?? DBNull.Value;
            if (cols.Contains("createdby")) v["createdBy"] = caller.Owner ?? caller.User;
            if (cols.Contains("modifiedby")) v["modifiedBy"] = caller.User ?? caller.Owner;
            if (cols.Contains(TenantColumn) && v[TenantColumn] is DBNull or null) return; // tenant row without a tenant: nothing to attach to
            var names = v.Keys.ToList();
            using var ins = Db.Command($"INSERT INTO {Db.Q(table)} ({string.Join(", ", names.Select(Db.Q))}) VALUES ({string.Join(", ", names.Select((_, i) => "@p" + i))})",
                c.Connection, c.Transaction);
            for (var i = 0; i < names.Count; i++) ins.Parameters.AddWithValue("@p" + i, v[names[i]] ?? DBNull.Value);
            Db.Execute(ins);
        }

        /// <summary>Background jobs: insert a row (plus its audit and outbox rows) as the system, in the source row's workspace and tenant.</summary>
        public static void SystemInsert(DbConnection c, DbTransaction tx, string table, Dictionary<string, object> values, Dictionary<string, object> sourceRow)
        {
            var caller = new Caller { User = "system", Owner = Str(sourceRow.GetValueOrDefault("createdby")), IsSystem = true, CorrelationId = Guid.NewGuid().ToString("N") };
            using var holder = Db.Command("", c, tx);
            InsertInternal(holder, table, values, caller, sourceRow);
            WriteAudit(holder, caller, table, "create", null, values, null);
            WriteOutbox(holder, caller, table, "insert", null, new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase) { [TenantColumn] = sourceRow.GetValueOrDefault(TenantColumn) }, null);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Rebinds the parameter assigned to <paramref name="col"/> in an UPDATE's SET list; false when it isn't a parameter.</summary>
        static bool KeepValue(DbCommand cmd, string col, object value)
        {
            var m = Regex.Match(cmd.CommandText, $@"\bSET\b.*?[`\[\]]?\b{Regex.Escape(col)}\b[`\[\]]?\s*=\s*(@\w+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!m.Success || !cmd.Parameters.Contains(m.Groups[1].Value)) return false;
            cmd.Parameters[m.Groups[1].Value].Value = value;
            return true;
        }

        static object KeyValue(DbCommand cmd, string table, Dictionary<string, object> values)
        {
            if (!PrimaryKeys.TryGetValue(table, out var pk)) return null;
            var m = Regex.Match(cmd.CommandText, $@"\bWHERE\b.*?[`\[\]]?\b{Regex.Escape(pk)}\b[`\[\]]?\s*=\s*(@\w+|'(?:[^']|'')*'|-?\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success)
            {
                var key = SqlValue(m.Groups[1].Value, cmd); // a parameter, or the key written in the SQL (WHERE id = 4)
                if (key != null && !(key is string p && p.StartsWith("@"))) return key;
            }
            if (!values.TryGetValue(pk, out var v)) return null;
            return v is string s && s.StartsWith("@") ? null : v;
        }

        static Dictionary<string, object> LoadVisible(DbCommand cmd, string table, object id)
        {
            using var q = Db.Command($"SELECT t.* FROM {Db.Q(table)} t WHERE t.{Db.Q(PrimaryKeys[table])} = @id", cmd.Connection, cmd.Transaction);
            q.Parameters.AddWithValue("@id", id);
            using var r = Apply(q).ExecuteReader();
            if (!r.Read()) return null;
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            return d;
        }

        static string RoleName(DbConnection _, Caller caller)
        {
            if (caller.IsPublic) return "Public";
            if (_roleNames.TryGetValue(caller.RoleId, out var n)) return n;
            // own connection: callers may be in the middle of reading from theirs
            using var c = Db.Connection(ConnectionString);
            c.Open();
            using var q = Db.Command("SELECT role_name FROM roles WHERE role_id = @r", c);
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
