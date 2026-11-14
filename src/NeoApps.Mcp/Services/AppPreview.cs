using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Turns a schema script (+ optional ui.config.json) into a one-page visual of the planned app, before anything is
/// generated: entity diagram, screens, roles, AI features and problems to fix. Given the previous version it also
/// shows what changed, so requirements can be iterated with the user. Parses the SQL itself; no database needed.
/// </summary>
public static class AppPreview
{
    public sealed record Column(string Name, string Type, bool NotNull, string? Default, bool Pk, bool AutoIncrement);
    public sealed record ForeignKey(string Column, string RefTable, string RefColumn);
    public sealed record Table(string Name, List<Column> Columns, List<ForeignKey> ForeignKeys);

    public sealed record Result(string Html, List<string> Warnings, List<string> Changes, int Tables, int Relations);

    private static readonly string[] Audit = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive"];
    internal static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders", "dnd_ui_versions",
        "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds", "workflow_runs",
        "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions",
        "ai_files", "ai_chunks", "ai_conversations", "ai_messages", "ai_runs", "versioninfo", "user_tenants", "login_attempts", "user_security", "number_sequences", "background_jobs", "background_job_events",
    };
    // Column names become C# property names in the generated API, unescaped.
    private static readonly HashSet<string> CSharpWords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
        "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
        "volatile", "while",
    };
    private static readonly HashSet<string> SqlWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "order", "group", "key", "condition", "desc", "asc", "select", "from", "where", "table", "index", "range", "rank", "limit", "option",
    };

    // ------------------------------------------------------------------ SQL parsing

    public static List<Table> Parse(string sql)
    {
        sql = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        sql = Regex.Replace(sql, @"(--|#)[^\n]*", " ");
        var tables = new List<Table>();
        foreach (Match m in Regex.Matches(sql, @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`?(\w+)`?\s*\(", RegexOptions.IgnoreCase))
        {
            // Body = everything up to the matching closing parenthesis.
            int depth = 1, i = m.Index + m.Length, start = i;
            for (; i < sql.Length && depth > 0; i++) depth += sql[i] == '(' ? 1 : sql[i] == ')' ? -1 : 0;
            var body = sql.Substring(start, Math.Max(0, i - start - 1));
            var cols = new List<Column>();
            var fks = new List<ForeignKey>();
            var pks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in SplitTopLevel(body))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                Match x;
                if ((x = Regex.Match(part, @"^(?:CONSTRAINT\s+`?\w+`?\s+)?FOREIGN\s+KEY\s*\(\s*`?(\w+)`?\s*\)\s*REFERENCES\s+`?(\w+)`?\s*\(\s*`?(\w+)`?", RegexOptions.IgnoreCase)).Success)
                    fks.Add(new ForeignKey(x.Groups[1].Value, x.Groups[2].Value.ToLowerInvariant(), x.Groups[3].Value));
                else if ((x = Regex.Match(part, @"^(?:CONSTRAINT\s+`?\w+`?\s+)?PRIMARY\s+KEY\s*\(([^)]*)\)", RegexOptions.IgnoreCase)).Success)
                    foreach (var c in x.Groups[1].Value.Split(',')) pks.Add(c.Trim(' ', '`'));
                else if (Regex.IsMatch(part, @"^(UNIQUE|KEY|INDEX|CONSTRAINT|CHECK|FULLTEXT)\b", RegexOptions.IgnoreCase)) { }
                else if ((x = Regex.Match(part, @"^`?(\w+)`?\s+(\w+(?:\s*\([^)]*\))?(?:\s+unsigned)?)(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline)).Success)
                {
                    var rest = x.Groups[3].Value;
                    var def = Regex.Match(rest, @"DEFAULT\s+('(?:[^']|'')*'|\S+)", RegexOptions.IgnoreCase);
                    var inlineFk = Regex.Match(rest, @"REFERENCES\s+`?(\w+)`?\s*\(\s*`?(\w+)`?", RegexOptions.IgnoreCase);
                    if (inlineFk.Success) fks.Add(new ForeignKey(x.Groups[1].Value, inlineFk.Groups[1].Value.ToLowerInvariant(), inlineFk.Groups[2].Value));
                    cols.Add(new Column(x.Groups[1].Value, Regex.Replace(x.Groups[2].Value, @"\s+", "").ToUpperInvariant(),
                        Regex.IsMatch(rest, @"NOT\s+NULL", RegexOptions.IgnoreCase) || Regex.IsMatch(rest, @"PRIMARY\s+KEY", RegexOptions.IgnoreCase),
                        def.Success ? def.Groups[1].Value.Trim('\'') : null,
                        Regex.IsMatch(rest, @"PRIMARY\s+KEY", RegexOptions.IgnoreCase),
                        Regex.IsMatch(rest, "AUTO_INCREMENT", RegexOptions.IgnoreCase)));
                }
            }
            cols = cols.Select(c => pks.Contains(c.Name) ? c with { Pk = true, NotNull = true } : c).ToList();
            tables.Add(new Table(m.Groups[1].Value.ToLowerInvariant(), cols, fks));
        }
        return tables;
    }

    private static IEnumerable<string> SplitTopLevel(string body)
    {
        int depth = 0; var quote = '\0'; var sb = new StringBuilder();
        foreach (var ch in body)
        {
            if (quote != '\0') { if (ch == quote) quote = '\0'; }
            else if (ch is '\'' or '"') quote = ch;
            else if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if (ch == ',' && depth == 0) { yield return sb.ToString(); sb.Clear(); continue; }
            sb.Append(ch);
        }
        yield return sb.ToString();
    }

    // ------------------------------------------------------------------ checks and diff

    public static List<string> Check(List<Table> tables, JsonObject? ui)
    {
        var w = new List<string>();
        var names = tables.Select(t => t.Name).ToHashSet();
        if (tables.Count == 0) w.Add("No CREATE TABLE statements found.");
        else if (tables.Count < 3 || tables.Count > 15) w.Add($"{tables.Count} tables; the generator works best with 3-15.");
        foreach (var t in tables)
        {
            if (Reserved.Contains(t.Name)) w.Add($"Table '{t.Name}' clashes with a built-in table. Rename it (e.g. members, automation_runs).");
            if (t.Name != t.Name.ToLowerInvariant() || t.Name.Contains('-')) w.Add($"Table '{t.Name}' should be lowercase snake_case.");
            var pk = t.Columns.Where(c => c.Pk).ToList();
            if (pk.Count != 1) w.Add($"{t.Name}: needs exactly one primary key column (<singular>_id INT AUTO_INCREMENT PRIMARY KEY).");
            else if (!pk[0].AutoIncrement) w.Add($"{t.Name}.{pk[0].Name}: primary key should be AUTO_INCREMENT.");
            var missing = Audit.Where(a => !t.Columns.Any(c => c.Name == a)).ToList();
            if (missing.Count > 0) w.Add($"{t.Name}: missing audit columns {string.Join(", ", missing)}.");
            foreach (var c in t.Columns.Where(c => SqlWords.Contains(c.Name))) w.Add($"{t.Name}.{c.Name}: '{c.Name}' is an SQL keyword; rename it.");
            foreach (var c in t.Columns.Where(c => CSharpWords.Contains(c.Name))) w.Add($"{t.Name}.{c.Name}: '{c.Name}' is a C# keyword and breaks the generated API; rename it (e.g. {c.Name}_name).");
            foreach (var fk in t.ForeignKeys)
            {
                if (!names.Contains(fk.RefTable)) w.Add($"{t.Name}.{fk.Column} references unknown table '{fk.RefTable}'.");
                if (!t.Columns.Any(c => c.Name == fk.Column)) w.Add($"{t.Name}: foreign key on unknown column '{fk.Column}'.");
            }
            var idx = tables.IndexOf(t);
            foreach (var fk in t.ForeignKeys.Where(f => f.RefTable != t.Name))
                if (tables.FindIndex(x => x.Name == fk.RefTable) > idx) w.Add($"{t.Name} references {fk.RefTable}, which is created later in the script; move {fk.RefTable} up.");
        }
        if (ui == null) return w;

        bool HasCol(string table, string col) => tables.FirstOrDefault(t => t.Name == table)?.Columns.Any(c => c.Name == col) == true;
        foreach (var g in ui["menu"] as JsonArray ?? [])
            foreach (var item in g?["items"] as JsonArray ?? [])
                if (!names.Contains(item!.GetValue<string>())) w.Add($"ui menu '{g!["group"]}' lists unknown table '{item}'.");
        foreach (var (table, cfg) in ui["tables"] as JsonObject ?? [])
        {
            if (!names.Contains(table)) { w.Add($"ui.tables has config for unknown table '{table}'."); continue; }
            foreach (var key in new[] { "listColumns", "formFields" })
                foreach (var c in cfg?[key] as JsonArray ?? [])
                    if (!HasCol(table, c!.GetValue<string>())) w.Add($"ui.tables.{table}.{key}: unknown column '{c}'.");
            if (cfg?["displayField"]?.GetValue<string>() is { } df && !HasCol(table, df)) w.Add($"ui.tables.{table}.displayField: unknown column '{df}'.");
            foreach (var (field, _) in cfg?["fields"] as JsonObject ?? [])
                if (!HasCol(table, field)) w.Add($"ui.tables.{table}.fields: unknown column '{field}'.");
            foreach (var c in cfg?["children"] as JsonArray ?? [])
                if (!names.Contains(c!.GetValue<string>())) w.Add($"ui.tables.{table}.children: unknown table '{c}'.");
        }
        foreach (var (role, grants) in ui["roles"] as JsonObject ?? [])
            foreach (var (table, _) in grants as JsonObject ?? [])
                if (table != "*" && !names.Contains(table)) w.Add($"ui.roles.{role}: unknown table '{table}'.");
        if (ui["ai"] is JsonObject ai)
        {
            foreach (var d in ai["documentTables"] as JsonArray ?? [])
                if (!names.Contains(d!.GetValue<string>())) w.Add($"ai.documentTables: unknown table '{d}'.");
            foreach (var wf in ai["workflows"] as JsonArray ?? [])
            {
                var id = wf?["id"]?.ToString();
                if (wf?["table"]?.GetValue<string>() is { } src && !names.Contains(src)) w.Add($"ai workflow '{id}': unknown source table '{src}'.");
                foreach (var t in wf?["targets"] as JsonArray ?? [])
                    if (!names.Contains(t!.GetValue<string>())) w.Add($"ai workflow '{id}': unknown target table '{t}'.");
                if (string.IsNullOrWhiteSpace(wf?["instructions"]?.ToString())) w.Add($"ai workflow '{id}': add step-by-step instructions.");
            }
        }
        CheckRules(tables, ui, w);
        return w;
    }

    /// <summary>
    /// Data-rule sections of ui.config.json (tenancy, rowScopes, hiddenFields, readFilters, lifecycles, validations,
    /// numbering, immutable, audit, outbox, jobs, limits): a typo in a table, column, role or state would otherwise
    /// only show at runtime, or silently do nothing.
    /// </summary>
    private static void CheckRules(List<Table> tables, JsonObject ui, List<string> w)
    {
        var names = tables.Select(t => t.Name).ToHashSet();
        bool HasCol(string table, string col) => tables.FirstOrDefault(t => t.Name == table)?.Columns.Any(c => c.Name == col) == true;
        static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
        static IEnumerable<string> Strs(JsonNode? n) => (n as JsonArray ?? []).Select(Str).Where(x => x != null)!;
        // one rule or a list of rules
        static IEnumerable<JsonObject> Objs(JsonNode? n) => n is JsonObject o ? [o] : (n as JsonArray ?? []).OfType<JsonObject>();

        bool Table(string where, string? table)
        {
            if (table == null) return false;
            if (names.Contains(table)) return true;
            w.Add($"{where}: unknown table '{table}'."); return false;
        }
        void Col(string where, string table, string? col)
        {
            if (col != null && names.Contains(table) && !HasCol(table, col)) w.Add($"{where}: unknown column '{table}.{col}'.");
        }
        void Where(string where, string table, JsonNode? cond)
        {
            foreach (var (col, _) in cond as JsonObject ?? []) Col(where, table, col);
        }

        var roleNames = (ui["roles"] as JsonObject)?.Select(r => r.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Roles(string where, JsonNode? list)
        {
            if (roleNames == null || roleNames.Count == 0) return; // roles not designed here: nothing to compare with
            foreach (var r in Strs(list))
                if (r != "*" && !r.Equals("Administrator", StringComparison.OrdinalIgnoreCase) && !roleNames.Contains(r))
                    w.Add($"{where}: unknown role '{r}' (roles: {string.Join(", ", roleNames)}).");
        }
        HashSet<string>? Options(string table, string col)
        {
            var opts = ui["tables"]?[table]?["fields"]?[col]?["options"] as JsonArray;
            if (opts == null || opts.Count == 0) return null;
            return opts.Select(o => Str(o) ?? Str(o?["value"])).Where(x => x != null).ToHashSet()!;
        }

        if (ui["tenancy"] is JsonObject ten)
        {
            var col = Str(ten["column"]) ?? "tenant_id";
            if (Str(ten["tenantTable"]) is { } tt) Table("tenancy.tenantTable", tt);
            foreach (var t in Strs(ten["sharedWritable"])) Table("tenancy.sharedWritable", t);
            if (!tables.Any(t => t.Columns.Any(c => c.Name == col))) w.Add($"tenancy: no table has the column '{col}', so nothing is isolated.");
            // a table without the tenant column that points at a tenant-owned table is shared (read-only for tenants)
            foreach (var t in tables.Where(t => !t.Columns.Any(c => c.Name == col) && t.Name != Str(ten["tenantTable"])))
                foreach (var fk in t.ForeignKeys.Where(f => f.RefTable != Str(ten["tenantTable"]) && HasCol(f.RefTable, col)))
                    w.Add($"tenancy: {t.Name} references tenant-owned {fk.RefTable} but has no '{col}' column; tenants can't write to it. Add the column if it is tenant data.");
        }
        foreach (var (i, sc) in Objs(ui["rowScopes"]).Select((x, i) => (i, x)))
        {
            var where = $"rowScopes[{Str(sc["name"]) ?? i.ToString()}]";
            Roles(where, sc["roles"]);
            var col = Str(sc["column"]);
            if (col == null) w.Add($"{where}: 'column' is required.");
            else if (!tables.Any(t => t.Columns.Any(c => c.Name == col))) w.Add($"{where}: no table has the column '{col}'.");
            if (sc["userValue"] is JsonObject uv && Table(where + ".userValue", Str(uv["table"])))
            {
                if (Str(uv["matchBy"]) != "user_id") Col(where + ".userValue.match", Str(uv["table"])!, Str(uv["match"]));
                Col(where + ".userValue.column", Str(uv["table"])!, Str(uv["column"]));
            }
            foreach (var t in Strs(sc["tables"]).Concat(Strs(sc["exclude"]))) Table(where, t);
        }
        foreach (var (role, perTable) in ui["hiddenFields"] as JsonObject ?? [])
        {
            Roles("hiddenFields", new JsonArray(role));
            foreach (var (table, cols) in perTable as JsonObject ?? [])
                if (table != "*" && Table($"hiddenFields.{role}", table))
                    foreach (var c in Strs(cols)) Col($"hiddenFields.{role}", table, c);
        }
        foreach (var rf in Objs(ui["readFilters"]))
        {
            Roles("readFilters", rf["roles"]);
            if (Table("readFilters", Str(rf["table"]))) Col("readFilters", Str(rf["table"])!, Str(rf["column"]));
        }
        foreach (var key in new[] { "menuByRole", "openByRole" })
            foreach (var (role, list) in ui[key] as JsonObject ?? [])
            {
                Roles(key, new JsonArray(role));
                foreach (var t in Strs(list)) if (t != "jobs") Table($"{key}.{role}", t);
            }

        foreach (var (table, node) in ui["lifecycles"] as JsonObject ?? [])
        {
            var where = $"lifecycles.{table}";
            if (!Table("lifecycles", table) || node is not JsonObject lc) continue;
            var col = Str(lc["column"]);
            if (col == null) { w.Add($"{where}: 'column' is required."); continue; }
            Col(where, table, col);
            var options = Options(table, col);
            void State(string what, string? state)
            {
                if (state != null && state != "*" && options != null && !options.Contains(state))
                    w.Add($"{where}: {what} state '{state}' is not one of the options of {table}.{col} ({string.Join(", ", options)}).");
            }
            if (options == null && HasCol(table, col)) w.Add($"{where}: give {table}.{col} select options (ui.tables.{table}.fields.{col}.options) so the states are checked and shown as a dropdown.");
            foreach (var st in Strs(lc["initial"])) State("initial", st);
            foreach (var st in Strs(lc["frozen"])) State("frozen", st);
            foreach (var (role, states) in lc["editableIn"] as JsonObject ?? [])
            {
                Roles($"{where}.editableIn", new JsonArray(role));
                foreach (var st in Strs(states)) State("editableIn", st);
            }
            var steps = (lc["transitions"] as JsonArray ?? []).OfType<JsonObject>().ToList();
            if (steps.Count == 0) w.Add($"{where}: no transitions, so the {col} can never change.");
            var reachable = Strs(lc["initial"]).ToHashSet();
            foreach (var tr in steps)
            {
                // "from" / "to": one state or a list; "from" defaults to any state
                static List<string> Set(JsonNode? n) => n is JsonArray ? Strs(n).ToList() : Str(n) is { } one ? [one] : [];
                var (from, to) = (Set(tr["from"]), Set(tr["to"]));
                if (from.Count == 0) from = ["*"];
                var step = $"{where} step {string.Join("/", from)} → {string.Join("/", to)}";
                if (to.Count == 0) { w.Add($"{where}: every transition needs a 'to' state."); continue; }
                foreach (var st in from.Concat(to)) State("transition", st);
                Roles(step, tr["roles"]);
                foreach (var a in Strs(tr["actors"])) if (a is not ("human" or "ai" or "system")) w.Add($"{step}: unknown actor '{a}' (human, ai, system).");
                foreach (var kind in new[] { "requires", "forbids" })
                    foreach (var rq in Objs(tr[kind]))
                        if (Table($"{step} {kind}", Str(rq["table"])))
                        {
                            Col($"{step} {kind}.link", Str(rq["table"])!, Str(rq["link"]));
                            Col($"{step} {kind}.linkFrom", table, Str(rq["linkFrom"]));
                            Where($"{step} {kind}.where", Str(rq["table"])!, rq["where"]);
                        }
                foreach (var d in tr["distinctFrom"] as JsonArray ?? [])
                    if (d is JsonObject dob) { Col($"{step} distinctFrom", table, Str(dob["column"])); if (Table($"{step} distinctFrom", Str(dob["table"]))) Col($"{step} distinctFrom", Str(dob["table"])!, Str(dob["userColumn"])); }
                    else Col($"{step} distinctFrom", table, Str(d));
                reachable.UnionWith(to);
            }
            if (options != null)
                foreach (var orphan in options.Where(o => !reachable.Contains(o)))
                    w.Add($"{where}: state '{orphan}' can't be reached (not initial and no transition leads to it).");
            if (lc["history"] is JsonObject h && Table(where + ".history", Str(h["table"])))
            {
                foreach (var k in new[] { "link", "from", "to", "actor" }) Col($"{where}.history.{k}", Str(h["table"])!, Str(h[k]));
                // the first history row (record created) has no previous state
                var fromCol = tables.FirstOrDefault(t => t.Name == Str(h["table"]))?.Columns.FirstOrDefault(c => c.Name == Str(h["from"]));
                if (fromCol is { NotNull: true, Default: null })
                    w.Add($"{where}.history: {Str(h["table"])}.{fromCol.Name} must allow NULL: the entry written when a record is created has no previous state.");
            }
            foreach (var (st, tsCol) in lc["timestamps"] as JsonObject ?? []) { State("timestamps", st); Col(where + ".timestamps", table, Str(tsCol)); }
            if (lc["sla"] is JsonObject sla && Table(where + ".sla", Str(sla["table"])))
            {
                foreach (var k in new[] { "link", "type", "due", "met" }) Col($"{where}.sla.{k}", Str(sla["table"])!, Str(sla[k]));
                foreach (var (st, _) in sla["hours"] as JsonObject ?? []) State("sla.hours", st);
            }
        }

        foreach (var (table, rules) in ui["validations"] as JsonObject ?? [])
        {
            if (!Table("validations", table)) continue;
            foreach (var (col, rule) in rules as JsonObject ?? [])
            {
                Col($"validations.{table}", table, col);
                foreach (var cmp in new[] { "gt", "gte", "lt", "lte", "ne" }) Col($"validations.{table}.{col}.{cmp}", table, Str(rule?[cmp]));
                Where($"validations.{table}.{col}.requiredWhen", table, rule?["requiredWhen"]);
                if (Str(rule?["pattern"]) is { } pattern)
                    try { _ = new Regex(pattern); } catch (ArgumentException) { w.Add($"validations.{table}.{col}: the pattern is not a valid regular expression."); }
                if (Str(rule?["format"]) is { } f && f is not ("email" or "url" or "phone")) w.Add($"validations.{table}.{col}: unknown format '{f}' (email, url, phone).");
                if (Str(rule?["equals"]) is { } expr)
                    foreach (Match m in Regex.Matches(expr, "[A-Za-z_][A-Za-z0-9_]*")) Col($"validations.{table}.{col}.equals", table, m.Value);
            }
        }
        foreach (var (table, node) in ui["numbering"] as JsonObject ?? [])
        {
            if (!Table("numbering", table)) continue;
            foreach (var n in Objs(node))
            {
                Col($"numbering.{table}", table, Str(n["column"]));
                // the number is assigned by the API, so forms and API callers leave it out
                var numCol = tables.FirstOrDefault(t => t.Name == table)?.Columns.FirstOrDefault(c => c.Name == Str(n["column"]));
                if (numCol is { NotNull: true, Default: null })
                    w.Add($"numbering.{table}: make {table}.{numCol.Name} DEFAULT NULL (keep it UNIQUE) so records can be created without typing a number.");
                if (Str(n["pattern"]) is not { } pat || !pat.Contains("{seq")) w.Add($"numbering.{table}: the pattern needs a {{seq}} or {{seq:N}} token.");
            }
        }
        foreach (var (table, cols) in ui["immutable"] as JsonObject ?? [])
            if (Table("immutable", table))
                foreach (var c in Strs(cols)) if (c != "*") Col("immutable", table, c);
        if (ui["audit"] is JsonObject audit)
        {
            Table("audit.table", Str(audit["table"]));
            if (Str(audit["platformTable"]) is { } pt) Table("audit.platformTable", pt);
            foreach (var t in Strs(audit["exclude"])) Table("audit.exclude", t);
        }
        if (ui["outbox"] is JsonObject outbox)
        {
            Table("outbox.table", Str(outbox["table"]));
            foreach (var ev in Objs(outbox["events"]))
            {
                var name = Str(ev["event"]) ?? "?";
                if (Table($"outbox event {name}", Str(ev["table"]))) Where($"outbox event {name}.when", Str(ev["table"])!, ev["when"]);
                if (Str(ev["on"]) is { } on && on is not ("insert" or "update" or "delete" or "create")) w.Add($"outbox event {name}: 'on' must be insert, update or delete.");
            }
        }
        foreach (var dl in Objs(ui["jobs"]?["deadlines"]))
        {
            if (!Table("jobs.deadlines", Str(dl["table"]))) continue;
            foreach (var k in new[] { "due", "met", "breached" }) Col($"jobs.deadlines.{k}", Str(dl["table"])!, Str(dl[k]));
            if (dl["escalate"] is JsonObject esc && Table("jobs.deadlines.escalate", Str(esc["table"])))
                foreach (var (c, _) in esc["values"] as JsonObject ?? []) Col("jobs.deadlines.escalate.values", Str(esc["table"])!, c);
        }
        foreach (var lim in Objs(ui["limits"]))
        {
            if (!Table("limits", Str(lim["table"]))) continue;
            var table = Str(lim["table"])!;
            Where($"limits.{table}.where", table, lim["where"]);
            Col($"limits.{table}.sum", table, Str(lim["sum"]));
            if (lim["max"] is JsonObject max && Table($"limits.{table}.max.from", Str(max["from"])))
            {
                Col($"limits.{table}.max.column", Str(max["from"])!, Str(max["column"]));
                Where($"limits.{table}.max.where", Str(max["from"])!, max["where"]);
                if (max["join"] is JsonObject j && Table($"limits.{table}.max.join", Str(j["table"]))) Col($"limits.{table}.max.join.on", Str(j["table"])!, Str(j["on"]));
            }
        }
    }

    /// <summary>The data rules as short sentences for preview.html: one card per topic.</summary>
    private static List<(string Title, List<string> Lines)> RuleLines(JsonObject ui)
    {
        static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
        static List<string> Set(JsonNode? n) => n is JsonArray a ? a.Select(Str).Where(x => x != null).ToList()! : Str(n) is { } one ? [one] : [];
        static IEnumerable<JsonObject> Objs(JsonNode? n) => n is JsonObject o ? [o] : (n as JsonArray ?? []).OfType<JsonObject>();
        static string Who(JsonNode? roles) => Set(roles) is { Count: > 0 } r && !r.Contains("*") ? string.Join(", ", r) : "anyone allowed to edit";
        var cards = new List<(string, List<string>)>();

        var access = new List<string>();
        if (ui["tenancy"] is JsonObject ten) access.Add($"Each {Singular(Label(ui, Str(ten["tenantTable"]) ?? "tenants")).ToLowerInvariant()} only sees its own data.");
        foreach (var sc in Objs(ui["rowScopes"])) access.Add($"{Who(sc["roles"])}: only records of their own {Str(sc["column"])?.Replace("_id", "").Replace('_', ' ')}.");
        foreach (var (role, per) in ui["hiddenFields"] as JsonObject ?? [])
            access.Add($"{role} never sees: {string.Join("; ", (per as JsonObject ?? []).Select(kv => $"{(kv.Key == "*" ? "everywhere" : Label(ui, kv.Key))}: {string.Join(", ", Set(kv.Value))}"))}.");
        foreach (var rf in Objs(ui["readFilters"]))
            access.Add($"{Who(rf["roles"])} only see {Label(ui, Str(rf["table"]) ?? "")} where {Str(rf["column"])} is {(rf["in"] != null ? "" : "not ")}{string.Join(" / ", Set(rf["in"] ?? rf["notIn"]))}.");
        if (access.Count > 0) cards.Add(("Who sees what", access));

        foreach (var (table, node) in ui["lifecycles"] as JsonObject ?? [])
        {
            if (node is not JsonObject lc) continue;
            var lines = new List<string>();
            if (Set(lc["initial"]) is { Count: > 0 } init) lines.Add($"Starts as: {string.Join(" / ", init)}.");
            foreach (var tr in (lc["transitions"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var from = Set(tr["from"]); var extra = new List<string>();
                foreach (var rq in Objs(tr["requires"])) extra.Add(Str(rq["message"]) ?? $"needs {Label(ui, Str(rq["table"]) ?? "")}");
                foreach (var fb in Objs(tr["forbids"])) extra.Add(Str(fb["message"]) ?? $"no {Label(ui, Str(fb["table"]) ?? "")}");
                if (tr["distinctFrom"] is JsonArray { Count: > 0 }) extra.Add("by a different person");
                if (Set(tr["actors"]).Contains("ai")) extra.Add("AI may do it");
                lines.Add($"{(from.Count == 0 ? "any" : string.Join(" / ", from))} → {string.Join(" / ", Set(tr["to"]))}: {Who(tr["roles"])}{(extra.Count > 0 ? $" ({string.Join("; ", extra)})" : "")}.");
            }
            if (Set(lc["frozen"]) is { Count: > 0 } frozen) lines.Add($"Locked once {string.Join(" / ", frozen)}.");
            foreach (var (role, states) in lc["editableIn"] as JsonObject ?? [])
                lines.Add($"{role} can only change it while {string.Join(" / ", Set(states))}.");
            cards.Add(($"{Label(ui, table)}: {Str(lc["column"])} steps", lines));
        }

        var data = new List<string>();
        foreach (var (table, rules) in ui["validations"] as JsonObject ?? [])
            data.Add($"{Label(ui, table)}: checks on {string.Join(", ", (rules as JsonObject ?? []).Select(kv => kv.Key))}.");
        foreach (var (table, node) in ui["numbering"] as JsonObject ?? [])
            foreach (var n in Objs(node)) data.Add($"{Label(ui, table)}: {Str(n["column"])} is numbered automatically ({Str(n["pattern"])}).");
        foreach (var (table, cols) in ui["immutable"] as JsonObject ?? [])
            data.Add(Set(cols).Contains("*") ? $"{Label(ui, table)}: can be added to, never changed or deleted." : $"{Label(ui, table)}: {string.Join(", ", Set(cols))} can't be changed after creation.");
        if (ui["audit"] is JsonObject au) data.Add($"Every change is recorded in {Label(ui, Str(au["table"]) ?? "the audit log")}.");
        foreach (var lim in Objs(ui["limits"])) data.Add($"{Label(ui, Str(lim["table"]) ?? "")}: limited by the {Str(lim["max"]?["column"]) ?? "plan"} of the {Singular(Label(ui, Str(lim["max"]?["from"]) ?? "plan")).ToLowerInvariant()}.");
        foreach (var dl in Objs(ui["jobs"]?["deadlines"])) data.Add($"{Label(ui, Str(dl["table"]) ?? "")}: overdue items are flagged automatically.");
        if (data.Count > 0) cards.Add(("Data checks", data));
        return cards;
    }

    private enum Mark { None, Added, Changed }

    private sealed class Diff
    {
        public readonly List<string> Lines = [];
        public readonly Dictionary<string, Mark> Tables = [];
        public readonly Dictionary<string, Mark> Columns = [];   // "table.column"
    }

    private static Diff Compare(List<Table> now, List<Table>? before, JsonObject? ui, JsonObject? uiBefore)
    {
        var d = new Diff();
        if (before == null) return d;
        foreach (var t in now)
        {
            var old = before.FirstOrDefault(x => x.Name == t.Name);
            if (old == null) { d.Tables[t.Name] = Mark.Added; d.Lines.Add($"Added table {t.Name}"); continue; }
            foreach (var c in t.Columns)
            {
                var oc = old.Columns.FirstOrDefault(x => x.Name == c.Name);
                if (oc == null) { d.Columns[$"{t.Name}.{c.Name}"] = Mark.Added; d.Lines.Add($"Added {t.Name}.{c.Name} ({c.Type})"); }
                else if (oc.Type != c.Type || oc.NotNull != c.NotNull || oc.Default != c.Default)
                {
                    d.Columns[$"{t.Name}.{c.Name}"] = Mark.Changed;
                    d.Lines.Add($"Changed {t.Name}.{c.Name}: {Describe(oc)} → {Describe(c)}");
                }
            }
            foreach (var oc in old.Columns.Where(oc => !t.Columns.Any(c => c.Name == oc.Name))) d.Lines.Add($"Removed {t.Name}.{oc.Name}");
            foreach (var fk in t.ForeignKeys.Where(f => !old.ForeignKeys.Contains(f))) d.Lines.Add($"New relation: {t.Name}.{fk.Column} → {fk.RefTable}");
            foreach (var fk in old.ForeignKeys.Where(f => !t.ForeignKeys.Contains(f))) d.Lines.Add($"Removed relation: {t.Name}.{fk.Column} → {fk.RefTable}");
            if (d.Lines.Any(l => l.Contains($" {t.Name}.")) && !d.Tables.ContainsKey(t.Name)) d.Tables[t.Name] = Mark.Changed;
        }
        foreach (var old in before.Where(o => !now.Any(t => t.Name == o.Name))) d.Lines.Add($"Removed table {old.Name}");

        if (uiBefore != null && ui != null)
        {
            var roles = Roles(ui, now); var oldRoles = Roles(uiBefore, before);
            foreach (var r in roles.Keys.Except(oldRoles.Keys)) d.Lines.Add($"Added role {r}");
            foreach (var r in oldRoles.Keys.Except(roles.Keys)) d.Lines.Add($"Removed role {r}");
            foreach (var r in roles.Keys.Intersect(oldRoles.Keys))
                foreach (var t in now.Select(x => x.Name))
                    if (oldRoles[r].GetValueOrDefault(t, "") != roles[r].GetValueOrDefault(t, "") && before.Any(b => b.Name == t))
                        d.Lines.Add($"Role {r} on {t}: {Perm(oldRoles[r].GetValueOrDefault(t, ""))} → {Perm(roles[r].GetValueOrDefault(t, ""))}");
            var wf = Workflows(ui); var oldWf = Workflows(uiBefore);
            foreach (var x in wf.Keys.Except(oldWf.Keys)) d.Lines.Add($"Added AI workflow '{wf[x]}'");
            foreach (var x in oldWf.Keys.Except(wf.Keys)) d.Lines.Add($"Removed AI workflow '{oldWf[x]}'");
            if ((ui["ai"] == null) != (uiBefore["ai"] == null)) d.Lines.Add(ui["ai"] != null ? "Added the AI assistant" : "Removed the AI assistant");
            if (ui["menu"]?.ToJsonString() != uiBefore["menu"]?.ToJsonString()) d.Lines.Add("Menu changed");
        }
        return d;
    }

    private static string Describe(Column c) => c.Type + (c.NotNull ? " required" : "") + (c.Default != null ? $" default {c.Default}" : "");
    private static string Perm(string letters) => letters.Length == 0 ? "no access" : string.Join("/", letters.Select(ch => ch switch { 'c' => "create", 'r' => "view", 'u' => "edit", 'd' => "delete", _ => "?" }));

    private static Dictionary<string, string> Workflows(JsonObject ui) =>
        (ui["ai"]?["workflows"] as JsonArray ?? []).Where(w => w?["id"] != null)
            .ToDictionary(w => w!["id"]!.ToString(), w => w!["label"]?.ToString() ?? w["id"]!.ToString());

    /// <summary>role -> table -> letters (c/r/u/d), Administrator first, defaults when the config has no roles.</summary>
    private static Dictionary<string, Dictionary<string, string>> Roles(JsonObject? ui, List<Table> tables)
    {
        var result = new Dictionary<string, Dictionary<string, string>> { ["Administrator"] = tables.ToDictionary(t => t.Name, _ => "crud") };
        var cfg = ui?["roles"] as JsonObject ?? new JsonObject { ["Member"] = new JsonObject { ["*"] = "cru" }, ["Viewer"] = new JsonObject { ["*"] = "r" } };
        foreach (var (role, grants) in cfg)
        {
            var star = grants?["*"]?.GetValue<string>() ?? "";
            result[role] = tables.ToDictionary(t => t.Name, t => new string((grants?[t.Name]?.GetValue<string>() ?? star).ToLowerInvariant().Where("crud".Contains).Distinct().OrderBy("crud".IndexOf).ToArray()));
        }
        return result;
    }

    // ------------------------------------------------------------------ rendering

    public static Result Build(string sql, string? uiJson, string? previousSql = null, string? previousUiJson = null, string? title = null)
    {
        var tables = Parse(sql);
        var ui = ParseUi(uiJson);
        var before = previousSql == null ? null : Parse(previousSql);
        var diff = Compare(tables, before, ui, ParseUi(previousUiJson));
        var warnings = Check(tables, ui);
        var appName = title ?? ui?["appName"]?.ToString() ?? "App preview";
        var relations = tables.Sum(t => t.ForeignKeys.Count);

        var h = new StringBuilder();
        h.Append($$"""
            <!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>{{E(appName)}} · plan</title>
            <style>
            :root{--p:{{E(ui?["theme"]?["primary"]?.ToString() ?? "#3b5bdb")}};--ink:#1d2330;--mut:#667085;--line:#e4e7ec;--bg:#f7f8fa;--add:#12b76a;--chg:#f79009}
            *{box-sizing:border-box}body{margin:0;font:14px/1.5 Inter,"Segoe UI",system-ui,sans-serif;color:var(--ink);background:var(--bg)}
            header{background:#fff;border-bottom:1px solid var(--line);padding:22px 32px}h1{margin:0;font-size:22px}.tag{color:var(--mut)}
            .stats{display:flex;gap:10px;flex-wrap:wrap;margin-top:12px}.stat{background:var(--bg);border:1px solid var(--line);border-radius:8px;padding:4px 10px;font-size:13px}
            nav{display:flex;gap:4px;padding:0 32px;background:#fff;border-bottom:1px solid var(--line);position:sticky;top:0;z-index:2;overflow-x:auto}
            nav a{padding:10px 12px;color:var(--mut);text-decoration:none;font-weight:600;font-size:13px;border-bottom:2px solid transparent;white-space:nowrap}nav a:hover{color:var(--p);border-color:var(--p)}
            main{padding:24px 32px;max-width:1500px}section{margin-bottom:36px}h2{font-size:17px;margin:0 0 12px}
            .card{background:#fff;border:1px solid var(--line);border-radius:10px;padding:16px}
            .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:14px}
            .chip{display:inline-block;border:1px solid var(--line);border-radius:999px;padding:0 8px;margin:2px 3px 2px 0;font-size:12px;background:#fff}
            .chip.p{border-color:var(--p);color:var(--p)}.mut{color:var(--mut)}.small{font-size:12px}
            .warn{border-left:4px solid var(--chg);background:#fffaeb}.ok{border-left:4px solid var(--add);background:#ecfdf3}
            .chg{border-left:4px solid var(--p);background:#f0f4ff}ul.tight{margin:6px 0;padding-left:18px}
            table.m{border-collapse:collapse;width:100%;background:#fff;font-size:13px}table.m th,table.m td{border:1px solid var(--line);padding:6px 8px;text-align:left}
            table.m th{background:var(--bg)}table.m td.c{text-align:center;font-family:ui-monospace,monospace}td.none{color:#c0c4cc}
            .er{overflow:auto;background:#fff;border:1px solid var(--line);border-radius:10px}
            .group{font-size:11px;text-transform:uppercase;letter-spacing:.6px;color:var(--mut);margin:18px 0 8px;font-weight:700}
            .f{display:flex;justify-content:space-between;gap:8px;border-top:1px dashed var(--line);padding:3px 0;font-size:13px}.f:first-child{border:0}
            .new{color:var(--add);font-weight:600}.changed{color:var(--chg);font-weight:600}
            </style></head><body>
            <header><h1>{{E(appName)}}</h1><div class="tag">{{E(ui?["tagline"]?.ToString() ?? "Planned app, before generation")}}</div>
            <div class="stats"><span class="stat">{{tables.Count}} entities</span><span class="stat">{{relations}} relationships</span>
            <span class="stat">{{Roles(ui, tables).Count}} roles</span><span class="stat">{{(ui?["ai"] != null ? "AI assistant on" : "No AI")}}</span>
            <span class="stat" style="{{(warnings.Count > 0 ? "border-color:var(--chg)" : "")}}">{{warnings.Count}} issue{{(warnings.Count == 1 ? "" : "s")}}</span></div></header>
            <nav>{{(before != null ? "<a href=\"#changes\">What changed</a>" : "")}}<a href="#data">Data model</a><a href="#screens">Screens</a><a href="#roles">Who can do what</a>{{(ui != null && RuleLines(ui).Count > 0 ? "<a href=\"#rules\">Rules</a>" : "")}}{{(ui?["ai"] != null ? "<a href=\"#ai\">AI</a>" : "")}}<a href="#issues">Issues</a></nav><main>
            """);

        if (before != null)
        {
            h.Append("<section id=\"changes\"><h2>What changed since the last version</h2><div class=\"card chg\">");
            h.Append(diff.Lines.Count == 0 ? "No changes." : "<ul class=\"tight\">" + string.Concat(diff.Lines.Select(l => $"<li>{E(l)}</li>")) + "</ul>");
            h.Append("</div></section>");
        }

        h.Append("<section id=\"data\"><h2>Data model</h2><p class=\"mut small\">Each box is something the app keeps track of; lines show how they connect (the arrow points from the \"many\" side to the \"one\" side).");
        if (before != null) h.Append(" <span class=\"new\">Green</span> = new, <span class=\"changed\">orange</span> = changed.");
        h.Append("</p><div class=\"er\">").Append(Diagram(tables, diff)).Append("</div>");
        h.Append("<div class=\"grid\" style=\"margin-top:14px\">");
        foreach (var t in tables)
        {
            var parents = t.ForeignKeys.Select(f => f.RefTable).Distinct().ToList();
            var children = tables.Where(x => x.ForeignKeys.Any(f => f.RefTable == t.Name) && x.Name != t.Name).Select(x => x.Name).ToList();
            var sentences = parents.Select(p => $"Each {Singular(t.Name)} belongs to {A(Singular(p))}.").Concat(children.Select(c => $"{Cap(A(Singular(t.Name)))} has many {Humanize(c).ToLowerInvariant()}."));
            h.Append($"<div class=\"card small\"><b>{E(Label(ui, t.Name))}</b> <span class=\"mut\">{E(t.Name)}</span>{(diff.Tables.TryGetValue(t.Name, out var tm) ? $" <span class=\"{(tm == Mark.Added ? "new" : "changed")}\">{(tm == Mark.Added ? "new" : "changed")}</span>" : "")}");
            h.Append("<ul class=\"tight\">" + string.Concat(sentences.Select(s => $"<li>{E(s)}</li>")) + (parents.Count + children.Count == 0 ? "<li class=\"mut\">Not linked to anything else.</li>" : "") + "</ul></div>");
        }
        h.Append("</div></section>");

        // Screens
        h.Append("<section id=\"screens\"><h2>Screens</h2><p class=\"mut small\">The sidebar menu and what each list and form will show. Every screen has search, paging, a detail page with related records, and create/edit/delete as permissions allow.</p>");
        var menu = (ui?["menu"] as JsonArray ?? []).Select(g => (Group: g!["group"]?.ToString() ?? "", Items: (g["items"] as JsonArray ?? []).Select(i => i!.GetValue<string>()).ToList())).ToList();
        var listed = menu.SelectMany(g => g.Items).ToHashSet();
        var rest = tables.Select(t => t.Name).Where(n => !listed.Contains(n) && ui?["tables"]?[n]?["hidden"]?.GetValue<bool>() != true).ToList();
        if (rest.Count > 0) menu.Add((menu.Count == 0 ? "Menu" : "More", rest));
        foreach (var (group, items) in menu)
        {
            h.Append($"<div class=\"group\">{E(group)}</div><div class=\"grid\">");
            foreach (var name in items)
            {
                var t = tables.FirstOrDefault(x => x.Name == name);
                if (t == null) continue;
                var cfg = ui?["tables"]?[name];
                var visible = t.Columns.Where(c => !Audit.Contains(c.Name) && !c.Pk && cfg?["fields"]?[c.Name]?["hidden"]?.GetValue<bool>() != true).ToList();
                var listCols = (cfg?["listColumns"] as JsonArray)?.Select(x => x!.GetValue<string>()).ToList() ?? visible.Take(5).Select(c => c.Name).ToList();
                var formOrder = (cfg?["formFields"] as JsonArray)?.Select(x => x!.GetValue<string>()).ToList() ?? visible.Select(c => c.Name).ToList();
                h.Append($"<div class=\"card\"><b>{E(Label(ui, name))}</b>{(diff.Tables.GetValueOrDefault(name) == Mark.Added ? " <span class=\"new small\">new</span>" : "")}");
                if (cfg?["description"]?.ToString() is { } desc) h.Append($"<div class=\"mut small\">{E(desc)}</div>");
                h.Append($"<div class=\"small mut\" style=\"margin:8px 0 4px\">List shows</div>{string.Concat(listCols.Select(c => $"<span class=\"chip\">{E(FieldLabel(cfg, c))}</span>"))}");
                h.Append("<div class=\"small mut\" style=\"margin:10px 0 2px\">Form</div>");
                foreach (var f in formOrder.Select(n => visible.FirstOrDefault(c => c.Name == n)).OfType<Column>().Concat(visible.Where(c => !formOrder.Contains(c.Name))))
                {
                    var fc = cfg?["fields"]?[f.Name];
                    var fk = t.ForeignKeys.FirstOrDefault(x => x.Column == f.Name);
                    var widget = fk != null ? $"pick {A(Singular(fk.RefTable))}" : fc?["widget"]?.ToString() ?? Widget(f);
                    var mark = diff.Columns.TryGetValue($"{name}.{f.Name}", out var cm) ? (cm == Mark.Added ? " new" : " changed") : "";
                    var opts = fc?["options"] as JsonArray;
                    h.Append($"<div class=\"f\"><span class=\"{mark.Trim()}\">{E(FieldLabel(cfg, f.Name))}{(f.NotNull && f.Default == null ? " *" : "")}</span><span class=\"mut\">{E(widget)}</span></div>");
                    if (opts != null) h.Append($"<div>{string.Concat(opts.Select(o => $"<span class=\"chip p\">{E(o!.ToString())}</span>"))}</div>");
                }
                h.Append("</div>");
            }
            h.Append("</div>");
        }
        h.Append("</section>");

        // Roles
        var roles = Roles(ui, tables);
        h.Append("<section id=\"roles\"><h2>Who can do what</h2><p class=\"mut small\">C = create, R = view, U = edit, D = delete. Administrators can also add members and change these permissions in the app.</p><div style=\"overflow:auto\"><table class=\"m\"><tr><th>Data</th>");
        foreach (var r in roles.Keys) h.Append($"<th>{E(r)}</th>");
        h.Append("</tr>");
        foreach (var t in tables)
        {
            h.Append($"<tr><td>{E(Label(ui, t.Name))}</td>");
            foreach (var r in roles.Values) { var l = r.GetValueOrDefault(t.Name, ""); h.Append($"<td class=\"c{(l == "" ? " none" : "")}\">{(l == "" ? "—" : E(l.ToUpperInvariant()))}</td>"); }
            h.Append("</tr>");
        }
        h.Append("</table></div></section>");

        // AI
        if (ui?["ai"] is JsonObject ai)
        {
            h.Append($"<section id=\"ai\"><h2>AI assistant: {E(ai["assistantName"]?.ToString() ?? "Assistant")}</h2><div class=\"grid\"><div class=\"card small\">");
            var tools = (ai["tools"] as JsonArray)?.Select(x => x!.ToString()).ToList() ?? ["documents", "records"];
            h.Append("<b>Can use</b><ul class=\"tight\">");
            if (tools.Contains("documents")) h.Append($"<li>Uploaded documents{((ai["documentTables"] as JsonArray)?.Count > 0 ? " on " + E(string.Join(", ", (ai["documentTables"] as JsonArray)!.Select(x => Label(ui, x!.ToString())))) : "")}, answers cite file and page</li>");
            if (tools.Contains("records")) h.Append("<li>The app's data (\"ask your data\"), limited to what each user may view</li>");
            if (tools.Contains("clinicaltrials_gov")) h.Append("<li>Live ClinicalTrials.gov search</li>");
            h.Append("</ul>");
            if (ai["suggestions"] is JsonArray sug && sug.Count > 0) h.Append("<b>Example questions</b><ul class=\"tight\">" + string.Concat(sug.Select(s => $"<li>{E(s!.ToString())}</li>")) + "</ul>");
            h.Append("</div>");
            foreach (var wf in ai["workflows"] as JsonArray ?? [])
            {
                var targets = (wf?["targets"] as JsonArray ?? []).Select(x => Label(ui, x!.ToString()));
                var where = wf?["table"] != null ? $"Button on {A(Singular(wf["table"]!.ToString()))}" + (wf["when"] is JsonObject when ? " when " + string.Join(", ", when.Select(kv => $"{kv.Key} = {kv.Value}")) : "") : "Button on the " + Label(ui, (wf?["targets"] as JsonArray)?.FirstOrDefault()?.ToString() ?? "") + " list";
                h.Append($"<div class=\"card small\"><b>⚡ {E(wf?["label"]?.ToString() ?? wf?["id"]?.ToString() ?? "")}</b><div class=\"mut\">{E(where)}</div><p>{E(wf?["description"]?.ToString() ?? "")}</p>");
                h.Append($"<div class=\"mut\">Creates</div>{string.Concat(targets.Select(x => $"<span class=\"chip p\">{E(x)}</span>"))}<div class=\"mut\" style=\"margin-top:6px\">The user reviews the proposed rows before anything is saved.</div></div>");
            }
            h.Append("</div></section>");
        }

        // Data rules: plain-language summary of what the API will enforce
        if (ui != null && RuleLines(ui) is { Count: > 0 } ruleCards)
        {
            h.Append("<section id=\"rules\"><h2>Rules the app enforces</h2><p class=\"mut small\">Checked by the app on every change, whoever makes it (people, imports or AI).</p><div class=\"grid\">");
            foreach (var (cardTitle, lines) in ruleCards)
                h.Append($"<div class=\"card small\"><b>{E(cardTitle)}</b><ul class=\"tight\">{string.Concat(lines.Select(x => $"<li>{E(x)}</li>"))}</ul></div>");
            h.Append("</div></section>");
        }

        h.Append("<section id=\"issues\"><h2>Issues to fix before generating</h2>");
        h.Append(warnings.Count == 0 ? "<div class=\"card ok\">None found. Ready to generate.</div>" : "<div class=\"card warn\"><ul class=\"tight\">" + string.Concat(warnings.Select(x => $"<li>{E(x)}</li>")) + "</ul></div>");
        h.Append($"</section><p class=\"mut small\">Generated by NeoApps preview · {DateTime.Now:yyyy-MM-dd HH:mm}</p></main></body></html>");
        return new Result(h.ToString(), warnings, diff.Lines, tables.Count, relations);
    }

    /// <summary>Entity diagram as inline SVG: parents to the left, children to the right, FK lines between rows.</summary>
    private static string Diagram(List<Table> tables, Diff diff)
    {
        if (tables.Count == 0) return "<p class=\"mut\" style=\"padding:16px\">No tables yet.</p>";
        var names = tables.Select(t => t.Name).ToHashSet();
        var level = new Dictionary<string, int>();
        int Level(string n, int guard)
        {
            if (level.TryGetValue(n, out var l)) return l;
            if (guard > tables.Count) return 0;
            var t = tables.First(x => x.Name == n);
            var parents = t.ForeignKeys.Where(f => f.RefTable != n && names.Contains(f.RefTable)).Select(f => f.RefTable).ToList();
            return level[n] = parents.Count == 0 ? 0 : parents.Max(p => Level(p, guard + 1)) + 1;
        }
        foreach (var t in tables) Level(t.Name, 0);

        const int W = 230, Row = 19, Head = 30, GapX = 110, GapY = 26, Pad = 24, MaxRows = 14;
        var pos = new Dictionary<string, (int x, int y, List<Column> rows)>();
        var colHeights = new Dictionary<int, int>();
        foreach (var t in tables.OrderBy(t => level[t.Name]))
        {
            var rows = t.Columns.Where(c => !Audit.Contains(c.Name)).OrderByDescending(c => c.Pk).ThenByDescending(c => t.ForeignKeys.Any(f => f.Column == c.Name)).Take(MaxRows).ToList();
            var lv = level[t.Name];
            var y = colHeights.GetValueOrDefault(lv, Pad);
            pos[t.Name] = (Pad + lv * (W + GapX), y, rows);
            colHeights[lv] = y + Head + rows.Count * Row + 10 + GapY;
        }
        var width = Pad * 2 + (level.Values.Max() + 1) * (W + GapX) - GapX;
        var height = colHeights.Values.Max() + Pad;
        var s = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" font-family=\"Inter,Segoe UI,system-ui,sans-serif\">");
        s.Append("<defs><marker id=\"arr\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"7\" markerHeight=\"7\" orient=\"auto\"><path d=\"M0,0 L10,5 L0,10 z\" fill=\"#98a2b3\"/></marker></defs>");
        foreach (var t in tables)
            foreach (var fk in t.ForeignKeys.Where(f => pos.ContainsKey(f.RefTable) && f.RefTable != t.Name))
            {
                var (cx, cy, crows) = pos[t.Name];
                var (px, py, _) = pos[fk.RefTable];
                var ri = crows.FindIndex(c => c.Name == fk.Column);
                var y1 = cy + Head + Math.Max(0, ri) * Row + Row / 2 + 4;
                var y2 = py + Head / 2;
                double x1 = cx, x2 = px + W;
                if (x2 >= x1) { x1 = cx + W; x2 = px + W + 2; }   // same column (cycle): loop on the right
                var mid = (x1 + x2) / 2;
                s.Append($"<path d=\"M{x1},{y1} C{(x2 > x1 ? x1 + 60 : mid)},{y1} {(x2 > x1 ? x2 + 60 : mid)},{y2} {x2},{y2}\" fill=\"none\" stroke=\"#98a2b3\" stroke-width=\"1.4\" marker-end=\"url(#arr)\"/>");
            }
        foreach (var t in tables)
        {
            var (x, y, rows) = pos[t.Name];
            var h = Head + rows.Count * Row + 10;
            var mark = diff.Tables.GetValueOrDefault(t.Name);
            var stroke = mark == Mark.Added ? "#12b76a" : mark == Mark.Changed ? "#f79009" : "#d0d5dd";
            s.Append($"<g><rect x=\"{x}\" y=\"{y}\" width=\"{W}\" height=\"{h}\" rx=\"8\" fill=\"#fff\" stroke=\"{stroke}\" stroke-width=\"{(mark == Mark.None ? 1 : 2)}\"/>");
            s.Append($"<path d=\"M{x},{y + 8} a8,8 0 0 1 8,-8 h{W - 16} a8,8 0 0 1 8,8 v{Head - 8} h-{W} z\" fill=\"var(--p)\"/>");
            s.Append($"<text x=\"{x + 10}\" y=\"{y + 20}\" fill=\"#fff\" font-weight=\"700\" font-size=\"13\">{E(t.Name)}</text>");
            for (var i = 0; i < rows.Count; i++)
            {
                var c = rows[i];
                var ry = y + Head + i * Row + 16;
                var isFk = t.ForeignKeys.Any(f => f.Column == c.Name);
                var cm = diff.Columns.GetValueOrDefault($"{t.Name}.{c.Name}");
                var color = cm == Mark.Added ? "#12b76a" : cm == Mark.Changed ? "#f79009" : "#1d2330";
                s.Append($"<text x=\"{x + 10}\" y=\"{ry}\" font-size=\"12\" fill=\"{color}\"{(c.Pk ? " font-weight=\"700\"" : "")}>{(c.Pk ? "🔑 " : isFk ? "↗ " : "")}{E(c.Name)}{(c.NotNull && !c.Pk && c.Default == null ? " *" : "")}</text>");
                s.Append($"<text x=\"{x + W - 10}\" y=\"{ry}\" font-size=\"11\" fill=\"#98a2b3\" text-anchor=\"end\">{E(c.Type.ToLowerInvariant())}</text>");
            }
            var hidden = t.Columns.Count(c => !Audit.Contains(c.Name)) - rows.Count;
            if (hidden > 0) s.Append($"<text x=\"{x + 10}\" y=\"{y + h - 2}\" font-size=\"11\" fill=\"#98a2b3\">+{hidden} more</text>");
            s.Append("</g>");
        }
        return s.Append("</svg>").ToString();
    }

    private static JsonObject? ParseUi(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject;

    private static string Widget(Column c) => c.Type switch
    {
        var t when t.StartsWith("TINYINT(1)") => "checkbox",
        var t when t.StartsWith("DECIMAL") => "number",
        var t when t.StartsWith("INT") || t.StartsWith("BIGINT") || t.StartsWith("SMALLINT") => "number",
        var t when t.StartsWith("DATETIME") || t.StartsWith("TIMESTAMP") => "date & time",
        var t when t.StartsWith("DATE") => "date",
        var t when t.StartsWith("TEXT") || t.StartsWith("LONGTEXT") || t.StartsWith("MEDIUMTEXT") => "long text",
        _ => "text",
    };

    private static string Label(JsonObject? ui, string table) => ui?["tables"]?[table]?["label"]?.ToString() ?? Humanize(table);
    private static string FieldLabel(JsonNode? cfg, string col) => cfg?["fields"]?[col]?["label"]?.ToString() ?? Humanize(Regex.Replace(col, "_id$", ""));
    private static string Humanize(string s) => string.Join(" ", s.Split('_', StringSplitOptions.RemoveEmptyEntries).Select((w, i) => i == 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
    private static string Singular(string table)
    {
        var w = Humanize(table).ToLowerInvariant();
        return w.EndsWith("ies") ? w[..^3] + "y" : w.EndsWith("sses") || w.EndsWith("xes") || w.EndsWith("ches") ? w[..^2] : w.EndsWith('s') && !w.EndsWith("ss") ? w[..^1] : w;
    }
    private static string A(string word) => ("aeiou".Contains(char.ToLowerInvariant(word.FirstOrDefault())) ? "an " : "a ") + word;
    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
