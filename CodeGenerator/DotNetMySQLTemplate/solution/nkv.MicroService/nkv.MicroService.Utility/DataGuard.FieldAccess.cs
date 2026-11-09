using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Per-role field and row visibility (ui.config.json), enforced by the API rather than only by the UI:
    ///   "hiddenFields": { "Client": { "claim_calculations": ["ppa_cap"], "*": ["created_by_user"] } }
    ///     the role never receives these columns (API responses, AI answers) and can't set them (writes keep the stored value).
    ///   "readFilters": [ { "roles": ["Client"], "table": "intake_bundles", "column": "status", "in": ["sent", "approved"] } ]
    ///     the role only sees (and can only change) rows matching the filter ("in" or "notIn"); other rows behave as missing.
    /// </summary>
    public static partial class DataGuard
    {
        sealed class ReadFilter
        {
            public HashSet<string> Roles;
            public string Table, Column;
            public List<string> Values;
            public bool Negate;
        }

        static readonly Dictionary<string, Dictionary<string, HashSet<string>>> _hidden = new(StringComparer.OrdinalIgnoreCase);
        static readonly List<ReadFilter> _readFilters = new();
        static readonly HashSet<string> NeverHidden = new(StringComparer.OrdinalIgnoreCase) { "createdby", "modifiedby", "createdat", "modifiedat", "isactive" };

        static void LoadFieldAccess()
        {
            _hidden.Clear(); _readFilters.Clear();
            if (Config["hiddenFields"] is JsonObject hf)
                foreach (var (role, tables) in hf)
                {
                    if (tables is not JsonObject to) continue;
                    var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (table, cols) in to)
                    {
                        var set = Set(cols);
                        set.RemoveWhere(c => NeverHidden.Contains(c) || (table != "*" && PrimaryKeys.TryGetValue(table, out var pk) && pk.Equals(c, StringComparison.OrdinalIgnoreCase)));
                        foreach (var c in set) SafeIdentifier(c);
                        map[table] = set;
                    }
                    _hidden[role] = map;
                }
            if (Config["readFilters"] is JsonArray rf)
                foreach (var o in rf.OfType<JsonObject>())
                {
                    var table = o["table"]?.GetValue<string>()?.ToLowerInvariant();
                    var column = o["column"]?.GetValue<string>();
                    if (table == null || column == null) continue;
                    SafeIdentifier(table); SafeIdentifier(column);
                    var negate = o["notIn"] is JsonArray;
                    _readFilters.Add(new ReadFilter
                    {
                        Roles = Set(o["roles"]), Table = table, Column = column, Negate = negate,
                        Values = (negate ? o["notIn"] : o["in"]) is JsonArray vs ? vs.Select(v => v?.ToString() ?? "").ToList() : new List<string>(),
                    });
                }
            if (_hidden.Count + _readFilters.Count > 0)
                Console.WriteLine($"DataGuard: hidden fields for {_hidden.Count} role(s), {_readFilters.Count} read filter(s).");
        }

        static string CurrentRole(Caller caller) => caller == null || caller.IsSystem ? null : RoleName(null, caller);

        /// <summary>Columns of `table` the current caller may not see or set (empty for administrators, jobs and unlisted roles).</summary>
        public static HashSet<string> HiddenFields(string table)
        {
            var empty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_hidden.Count == 0 || string.IsNullOrEmpty(table)) return empty;
            var role = CurrentRole(Current);
            if (role == null || !_hidden.TryGetValue(role, out var map)) return empty;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (map.TryGetValue(table, out var t)) set.UnionWith(t);
            if (map.TryGetValue("*", out var all) && AppTables.Contains(table))
                set.UnionWith(all.Where(c => !(PrimaryKeys.TryGetValue(table, out var pk) && pk.Equals(c, StringComparison.OrdinalIgnoreCase))));
            return set;
        }

        /// <summary>table -> hidden columns for the current caller (tables without hidden columns are left out).</summary>
        public static Dictionary<string, string[]> HiddenFieldMap() =>
            AppTables.Select(t => (t, h: HiddenFields(t))).Where(x => x.h.Count > 0).ToDictionary(x => x.t, x => x.h.OrderBy(c => c).ToArray());

        public static bool IsHidden(string table, string column) => HiddenFields(table).Contains(column ?? "");

        /// <summary>Removes the caller's hidden columns of `table` from every object inside `node` (records, pages, lists).</summary>
        public static JsonNode StripHidden(string table, JsonNode node)
        {
            var hidden = HiddenFields(table);
            if (hidden.Count > 0) Strip(node, hidden);
            return node;
        }

        static void Strip(JsonNode node, HashSet<string> hidden)
        {
            if (node is JsonObject o)
            {
                foreach (var k in o.Select(kv => kv.Key).Where(hidden.Contains).ToList()) o.Remove(k);
                foreach (var kv in o.ToList()) Strip(kv.Value, hidden);
            }
            else if (node is JsonArray a) foreach (var x in a) Strip(x, hidden);
        }

        static List<string> ReadFilterConditions(string table, string prefix, Caller caller, MySqlCommand cmd)
        {
            var conds = new List<string>();
            if (_readFilters.Count == 0) return conds;
            var role = CurrentRole(caller);
            if (role == null) return conds;
            var i = 0;
            foreach (var f in _readFilters)
            {
                i++;
                if (!f.Table.Equals(table, StringComparison.OrdinalIgnoreCase) || !f.Roles.Contains(role) || !Columns[table].Contains(f.Column)) continue;
                if (f.Values.Count == 0) { conds.Add(f.Negate ? "1 = 1" : "1 = 0"); continue; }
                var ps = f.Values.Select((v, k) => { var p = $"@__rf{i}_{k}"; Bind(cmd, p, v); return p; }).ToList();
                var col = $"{prefix}`{f.Column}`";
                conds.Add(f.Negate ? $"({col} IS NULL OR {col} NOT IN ({string.Join(", ", ps)}))" : $"{col} IN ({string.Join(", ", ps)})");
            }
            return conds;
        }

        /// <summary>UPDATE: assignments to hidden columns keep the stored value.</summary>
        static void KeepHiddenOnUpdate(MySqlCommand cmd, string table, int afterSet)
        {
            var hidden = HiddenFields(table);
            if (hidden.Count == 0) return;
            var sql = cmd.CommandText;
            var setPart = sql.Substring(afterSet);
            var end = FindTopLevel(setPart, 0, @"\bWHERE\b");
            var head = end < 0 ? setPart : setPart.Substring(0, end);
            foreach (var c in hidden)
                head = Regex.Replace(head, $@"`?\b{Regex.Escape(c)}\b`?\s*=\s*@\w+", $"`{c}` = `{c}`", RegexOptions.IgnoreCase);
            cmd.CommandText = sql.Substring(0, afterSet) + head + (end < 0 ? "" : setPart.Substring(end));
        }

        /// <summary>INSERT: hidden columns get their column default instead of what was sent.</summary>
        static void DefaultHiddenOnInsert(MySqlCommand cmd, string table)
        {
            var hidden = HiddenFields(table);
            if (hidden.Count == 0) return;
            var m = InsertHead.Match(cmd.CommandText);
            if (!m.Success) return;
            var cols = m.Groups[2].Value.Split(',').Select(x => x.Trim().Trim('`').ToLowerInvariant()).ToList();
            var open = cmd.CommandText.IndexOf('(', m.Groups[2].Index + m.Groups[2].Length + 1);
            var inner = ExtractParens(cmd.CommandText, open);
            var exprs = SplitTopLevel(inner);
            var changed = false;
            for (var k = 0; k < cols.Count && k < exprs.Count; k++)
                if (hidden.Contains(cols[k]) && exprs[k].Trim().StartsWith("@")) { exprs[k] = "DEFAULT"; changed = true; }
            if (changed)
                cmd.CommandText = cmd.CommandText.Substring(0, open + 1) + string.Join(", ", exprs.Select(e => e.Trim())) + cmd.CommandText.Substring(open + 1 + inner.Length);
        }
    }
}
