using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MySqlConnector;
using static NeoApps.Mcp.Services.AppPreview;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Compares an app's live database with its schema.sql and writes the difference as a FluentMigrator migration class
/// (C#, Up and Down). The class is applied by the migrator now and by the generated API on every startup, so the same
/// change moves dev, staging and production databases forward in place.
/// </summary>
public static class SchemaMigrations
{
    private static readonly HashSet<string> BuiltInAi = new(StringComparer.OrdinalIgnoreCase) { "ai_files", "ai_chunks", "ai_conversations", "ai_messages", "ai_runs", "user_tenants", "login_attempts", "user_security", "number_sequences", "background_jobs", "background_job_events", "user_notifications", "user_notification_settings" };
    public sealed record LiveTable(Table Table, Dictionary<string, string> ForeignKeyNames);

    public sealed class Plan
    {
        public List<string> Changes { get; } = [];
        public List<string> Warnings { get; } = [];
        public List<string> Skipped { get; } = [];
        public List<string> NewTables { get; } = [];
        internal List<(string up, string down)> Ops { get; } = [];
        public bool HasChanges => Ops.Count > 0;
        public string? Code { get; set; }
        public string? ClassName { get; set; }
        public long Version { get; set; }
    }

    private static readonly string[] Audit = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive"];
    /// <summary>Who really created/changed a row: added to every app table and stamped by DataGuard, never part of the diff.</summary>
    public static readonly string[] UserColumns = ["created_by_user", "modified_by_user"];

    // ------------------------------------------------------------------ live schema

    public static List<LiveTable> ReadLive(string connectionString)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        var cols = new Dictionary<string, List<Column>>(StringComparer.OrdinalIgnoreCase);
        var names = new List<string>();
        using (var cmd = new MySqlCommand(@"SELECT c.table_name, c.column_name, c.column_type, c.is_nullable, c.column_default, c.column_key, c.extra
                FROM information_schema.columns c JOIN information_schema.tables t
                  ON t.table_schema = c.table_schema AND t.table_name = c.table_name AND t.table_type = 'BASE TABLE'
                WHERE c.table_schema = DATABASE() ORDER BY c.table_name, c.ordinal_position", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var table = r.GetString(0).ToLowerInvariant();
                if (Reserved.Contains(table) || BuiltInAi.Contains(table)) continue;
                if (UserColumns.Contains(r.GetString(1), StringComparer.OrdinalIgnoreCase)) continue;
                if (!cols.TryGetValue(table, out var list)) { cols[table] = list = []; names.Add(table); }
                list.Add(new Column(r.GetString(1), r.GetString(2).ToUpperInvariant(), r.GetString(3) == "NO",
                    r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5) == "PRI", r.GetString(6).Contains("auto_increment", StringComparison.OrdinalIgnoreCase)));
            }
        var fks = new Dictionary<string, List<ForeignKey>>(StringComparer.OrdinalIgnoreCase);
        var fkNames = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new MySqlCommand(@"SELECT table_name, column_name, referenced_table_name, referenced_column_name, constraint_name
                FROM information_schema.key_column_usage WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var table = r.GetString(0).ToLowerInvariant();
                var fk = new ForeignKey(r.GetString(1), r.GetString(2).ToLowerInvariant(), r.GetString(3));
                (fks.TryGetValue(table, out var l) ? l : fks[table] = []).Add(fk);
                (fkNames.TryGetValue(table, out var n) ? n : fkNames[table] = new(StringComparer.OrdinalIgnoreCase))[fk.Column] = r.GetString(4);
            }
        return names.Select(t => new LiveTable(new Table(t, cols[t], fks.GetValueOrDefault(t) ?? []),
            fkNames.GetValueOrDefault(t) ?? new(StringComparer.OrdinalIgnoreCase))).ToList();
    }

    public static bool TableHasRows(string connectionString, string table)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        using var cmd = new MySqlCommand($"SELECT EXISTS(SELECT 1 FROM `{table.Replace("`", "``")}`)", conn);
        return Convert.ToInt64(cmd.ExecuteScalar()) == 1;
    }

    public static List<string> RoleNames(string connectionString)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        using var cmd = new MySqlCommand("SELECT role_name FROM roles", conn);
        using var r = cmd.ExecuteReader();
        var list = new List<string>();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    // ------------------------------------------------------------------ plan

    /// <param name="renames">{"old_table": "new_table", "table.old_column": "new_column"}</param>
    public static Plan Build(List<LiveTable> live, List<Table> desired, JsonObject? ui, JsonObject? renames, bool allowDestructive,
        ICollection<string> existingRoles, Func<string, bool> hasRows, string name, DateTimeOffset now, JsonObject? previousUi = null)
    {
        var plan = new Plan();
        desired = desired.Where(t => !Reserved.Contains(t.Name))
            .Select(t => t with { Columns = t.Columns.Where(c => !UserColumns.Contains(c.Name, StringComparer.OrdinalIgnoreCase)).ToList() }).ToList();
        var liveByName = live.ToDictionary(l => l.Table.Name, StringComparer.OrdinalIgnoreCase);
        var roleGrants = RoleGrants(ui, desired);

        // Roles the new requirements introduce: missing here, or new since the last saved version (so the migration also
        // creates them in databases that don't have them yet). Existing roles are left as admins tuned them.
        var previousRoles = (previousUi?["roles"] as JsonObject)?.Select(kv => kv.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (role, roleMap) in roleGrants.Where(r => !existingRoles.Contains(r.Key, StringComparer.OrdinalIgnoreCase) || (previousRoles != null && !previousRoles.Contains(r.Key))))
        {
            var star = roleMap.GetValueOrDefault("*", "");
            var compact = roleMap.Where(kv => kv.Key == "*" ? star.Length > 0 : kv.Value != star).ToDictionary(kv => kv.Key, kv => kv.Value);
            plan.Ops.Add(($"this.EnsureRole({Lit(role)}, {Dict(compact)});", $"// role {role} is kept (it may have members)"));
            plan.Changes.Add($"New role {role}");
        }

        // Explicit renames: a diff alone can't tell a rename from a drop plus an add.
        var tableRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var columnRenames = new Dictionary<(string table, string from), string>();
        foreach (var (from, to) in renames ?? [])
        {
            var target = to?.ToString() ?? "";
            if (!from.Contains('.')) tableRenames[from.ToLowerInvariant()] = target.ToLowerInvariant();
            else { var p = from.Split('.'); columnRenames[(p[0].ToLowerInvariant(), p[1])] = target.Contains('.') ? target.Split('.')[1] : target; }
        }
        foreach (var (from, to) in tableRenames)
        {
            if (!liveByName.TryGetValue(from, out var lt) || !desired.Any(d => d.Name == to) || liveByName.ContainsKey(to))
            { plan.Warnings.Add($"Rename {from} → {to} ignored: {from} must exist in the database and {to} only in schema.sql."); continue; }
            plan.Ops.Add(($"Rename.Table({Lit(from)}).To({Lit(to)});\n        this.RenameEntity({Lit(from)}, {Lit(to)});",
                          $"this.RenameEntity({Lit(to)}, {Lit(from)});\n        Rename.Table({Lit(to)}).To({Lit(from)});"));
            plan.Changes.Add($"Renamed table {from} → {to}");
            liveByName.Remove(from);
            liveByName[to] = lt with { Table = lt.Table with { Name = to } };
        }
        // MySQL repoints foreign keys when their target table is renamed.
        if (tableRenames.Count > 0)
            foreach (var key in liveByName.Keys.ToList())
            {
                var lt = liveByName[key];
                liveByName[key] = lt with { Table = lt.Table with { ForeignKeys = lt.Table.ForeignKeys.Select(f => tableRenames.TryGetValue(f.RefTable, out var nn) ? f with { RefTable = nn } : f).ToList() } };
            }

        var dropFks = new List<(string up, string down)>();
        var createTables = new List<(string up, string down)>();
        var addColumns = new List<(string up, string down)>();
        var alterColumns = new List<(string up, string down)>();
        var addFks = new List<(string up, string down)>();
        var grants = new List<(string up, string down)>();
        var drops = new List<(string up, string down)>();

        // New tables.
        foreach (var t in desired.Where(d => !liveByName.ContainsKey(d.Name)))
        {
            plan.NewTables.Add(t.Name);
            plan.Changes.Add($"New table {t.Name} ({t.Columns.Count} columns)");
            createTables.Add((CreateTable(t), $"Delete.Table({Lit(t.Name)});"));
            foreach (var fk in t.ForeignKeys)
                addFks.Add((CreateFk(t.Name, fk), $"Delete.ForeignKey({Lit(FkName(t.Name, fk.Column))}).OnTable({Lit(t.Name)});"));
            var pk = t.Columns.FirstOrDefault(c => c.Pk)?.Name ?? t.Columns[0].Name;
            var tableGrants = roleGrants.ToDictionary(r => r.Key, r => r.Value.GetValueOrDefault(t.Name, ""));
            var group = MenuGroup(ui, t.Name);
            grants.Add(($"this.GrantNewTable({Lit(t.Name)}, {Lit(pk)}, {Dict(tableGrants)});" + (group != null ? $"\n        this.AddToSavedMenu({Lit(group)}, {Lit(t.Name)});" : ""),
                        $"this.RemoveFromSavedMenu({Lit(t.Name)});\n        this.RevokeTable({Lit(t.Name)});"));
        }

        // Changed tables.
        foreach (var t in desired.Where(d => liveByName.ContainsKey(d.Name)))
        {
            var lt = liveByName[t.Name];
            var old = lt.Table;
            var renamedFrom = columnRenames.Where(kv => kv.Key.table == t.Name).ToDictionary(kv => kv.Value, kv => kv.Key.from, StringComparer.OrdinalIgnoreCase);
            foreach (var (to, from) in renamedFrom)
            {
                if (old.Columns.All(c => !c.Name.Equals(from, StringComparison.OrdinalIgnoreCase)) || t.Columns.All(c => !c.Name.Equals(to, StringComparison.OrdinalIgnoreCase)))
                { plan.Warnings.Add($"Rename {t.Name}.{from} → {to} ignored: {from} must exist in the database and {to} in schema.sql."); continue; }
                alterColumns.Add(($"Rename.Column({Lit(from)}).OnTable({Lit(t.Name)}).To({Lit(to)});", $"Rename.Column({Lit(to)}).OnTable({Lit(t.Name)}).To({Lit(from)});"));
                plan.Changes.Add($"Renamed {t.Name}.{from} → {to}");
            }
            Column? Old(string col) => old.Columns.FirstOrDefault(c => c.Name.Equals(renamedFrom.GetValueOrDefault(col, col), StringComparison.OrdinalIgnoreCase));

            foreach (var c in t.Columns)
            {
                var oc = Old(c.Name);
                if (oc == null)
                {
                    if (c.Pk || c.AutoIncrement) { plan.Warnings.Add($"{t.Name}.{c.Name}: adding a primary key column to an existing table isn't automated. Write that migration by hand."); continue; }
                    addColumns.Add(($"Alter.Table({Lit(t.Name)}).AddColumn({Lit(c.Name)}){ColumnDef(c)};", $"Delete.Column({Lit(c.Name)}).FromTable({Lit(t.Name)});"));
                    plan.Changes.Add($"New column {t.Name}.{c.Name} {c.Type}{(c.NotNull ? " NOT NULL" : "")}");
                    if (c.NotNull && NormDefault(c.Default) == null && hasRows(t.Name))
                        plan.Warnings.Add($"{t.Name}.{c.Name} is NOT NULL without a default and {t.Name} has rows: MySQL fills existing rows with its implicit default (0, '' or a zero date). Consider a DEFAULT.");
                }
                else if (!SameColumn(oc, c))
                {
                    if (oc.Pk != c.Pk || oc.AutoIncrement != c.AutoIncrement)
                    { plan.Warnings.Add($"{t.Name}.{c.Name}: primary key / AUTO_INCREMENT changes aren't automated. Write that migration by hand."); continue; }
                    alterColumns.Add(($"Alter.Column({Lit(c.Name)}).OnTable({Lit(t.Name)}){ColumnDef(c)};", $"Alter.Column({Lit(c.Name)}).OnTable({Lit(t.Name)}){ColumnDef(oc with { Name = c.Name })};"));
                    plan.Changes.Add($"Changed {t.Name}.{c.Name}: {Describe(oc)} → {Describe(c)}");
                    if (NormType(oc.Type) != NormType(c.Type) && !IsWidening(oc.Type, c.Type))
                        plan.Warnings.Add($"{t.Name}.{c.Name} changes type {oc.Type} → {c.Type}; values that don't fit will fail the migration or be truncated.");
                    if (!oc.NotNull && c.NotNull) plan.Warnings.Add($"{t.Name}.{c.Name} becomes NOT NULL: the migration fails if any row has NULL there. Backfill first.");
                }
            }
            foreach (var oc in old.Columns.Where(oc => !t.Columns.Any(c => c.Name.Equals(oc.Name, StringComparison.OrdinalIgnoreCase)) && !renamedFrom.Values.Contains(oc.Name, StringComparer.OrdinalIgnoreCase)))
            {
                if (!allowDestructive) { plan.Skipped.Add($"Drop column {t.Name}.{oc.Name} (pass --allow-destructive; its data is deleted)"); continue; }
                if (lt.ForeignKeyNames.TryGetValue(oc.Name, out var fkn))
                {
                    var ofk = old.ForeignKeys.First(f => f.Column.Equals(oc.Name, StringComparison.OrdinalIgnoreCase));
                    dropFks.Add(($"Delete.ForeignKey({Lit(fkn)}).OnTable({Lit(t.Name)});", CreateFk(t.Name, ofk, fkn)));
                }
                drops.Add(($"Delete.Column({Lit(oc.Name)}).FromTable({Lit(t.Name)});", $"// data in {t.Name}.{oc.Name} is not restored\n        Alter.Table({Lit(t.Name)}).AddColumn({Lit(oc.Name)}){ColumnDef(oc with { NotNull = false })};"));
                plan.Changes.Add($"Dropped column {t.Name}.{oc.Name}");
            }
            foreach (var fk in t.ForeignKeys.Where(f => !old.ForeignKeys.Any(o => SameFk(o, f, renamedFrom))))
            {
                addFks.Add((CreateFk(t.Name, fk), $"Delete.ForeignKey({Lit(FkName(t.Name, fk.Column))}).OnTable({Lit(t.Name)});"));
                plan.Changes.Add($"New relation {t.Name}.{fk.Column} → {fk.RefTable}");
            }
            foreach (var ofk in old.ForeignKeys.Where(o => !t.ForeignKeys.Any(f => SameFk(o, f, renamedFrom))))
            {
                if (!t.Columns.Any(c => c.Name.Equals(ofk.Column, StringComparison.OrdinalIgnoreCase))) continue; // handled with the column drop
                if (!lt.ForeignKeyNames.TryGetValue(ofk.Column, out var fkn)) continue;
                dropFks.Add(($"Delete.ForeignKey({Lit(fkn)}).OnTable({Lit(t.Name)});", CreateFk(t.Name, ofk, fkn)));
                plan.Changes.Add($"Removed relation {t.Name}.{ofk.Column} → {ofk.RefTable}");
            }
        }

        // Tables no longer in schema.sql.
        foreach (var lt in liveByName.Values.Where(l => !desired.Any(d => d.Name == l.Table.Name)))
        {
            if (!allowDestructive) { plan.Skipped.Add($"Drop table {lt.Table.Name} (pass --allow-destructive; its data is deleted)"); continue; }
            foreach (var (col, fkn) in lt.ForeignKeyNames)
                dropFks.Add(($"Delete.ForeignKey({Lit(fkn)}).OnTable({Lit(lt.Table.Name)});", CreateFk(lt.Table.Name, lt.Table.ForeignKeys.First(f => f.Column == col), fkn)));
            drops.Add(($"this.RemoveFromSavedMenu({Lit(lt.Table.Name)});\n        this.RevokeTable({Lit(lt.Table.Name)});\n        Delete.Table({Lit(lt.Table.Name)});",
                       $"// data in {lt.Table.Name} is not restored; re-grant its permissions in Admin > Roles\n        {CreateTable(lt.Table)}"));
            plan.Changes.Add($"Dropped table {lt.Table.Name}");
        }
        if (drops.Count > 0) plan.Warnings.Add("This migration deletes data. Take a backup before deploying it.");

        plan.Ops.AddRange(dropFks);
        plan.Ops.AddRange(createTables);
        plan.Ops.AddRange(addColumns);
        plan.Ops.AddRange(alterColumns);
        plan.Ops.AddRange(addFks);
        plan.Ops.AddRange(grants);
        plan.Ops.AddRange(drops);
        if (plan.HasChanges) Render(plan, name, now);
        return plan;
    }

    private static void Render(Plan plan, string name, DateTimeOffset now)
    {
        plan.Version = long.Parse(now.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
        var pascal = string.Concat(Regex.Split(name, @"[^A-Za-z0-9]+").Where(w => w.Length > 0).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        if (pascal.Length == 0) pascal = "SchemaChange";
        plan.ClassName = $"M{plan.Version}_{pascal}";
        var sb = new StringBuilder();
        sb.AppendLine($"// Generated by `neoapps evolve` on {now:yyyy-MM-dd HH:mm} from schema.sql. Review it before deploying; you can edit it.");
        sb.AppendLine("// The API applies pending migrations on startup and records them in VersionInfo.");
        sb.AppendLine("//");
        foreach (var c in plan.Changes) sb.AppendLine($"//   {c}");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using FluentMigrator;");
        sb.AppendLine();
        sb.AppendLine("namespace NeoApps.Migrations;");
        sb.AppendLine();
        sb.AppendLine($"[Migration({plan.Version}, {Lit(name)})]");
        sb.AppendLine($"public class {plan.ClassName} : Migration");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Up()");
        sb.AppendLine("    {");
        foreach (var (up, _) in plan.Ops) sb.AppendLine("        " + up);
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down()");
        sb.AppendLine("    {");
        foreach (var (_, down) in Enumerable.Reverse(plan.Ops)) sb.AppendLine("        " + down);
        sb.AppendLine("    }");
        sb.AppendLine("}");
        plan.Code = sb.ToString();
    }

    // ------------------------------------------------------------------ code fragments

    private static string CreateTable(Table t)
    {
        var sb = new StringBuilder($"Create.Table({Lit(t.Name)})");
        var auditStandard = Audit.All(a => t.Columns.Any(c => c.Name == a)) && IsStandardAudit(t);
        foreach (var c in t.Columns.Where(c => !auditStandard || !Audit.Contains(c.Name)))
            sb.Append($"\n            .WithColumn({Lit(c.Name)}){ColumnDef(c)}");
        if (auditStandard) sb.Append("\n            .WithAuditColumns()");
        return sb.Append(';').ToString();
    }

    private static bool IsStandardAudit(Table t)
    {
        Column C(string n) => t.Columns.First(c => c.Name == n);
        return NormType(C("createdBy").Type) == "VARCHAR(255)" && C("createdBy").NotNull
            && NormType(C("modifiedBy").Type) == "VARCHAR(255)" && C("modifiedBy").NotNull
            && NormType(C("createdAt").Type) == "DATETIME" && NormDefault(C("createdAt").Default) == "CURRENT_TIMESTAMP"
            && NormType(C("modifiedAt").Type) == "DATETIME" && NormDefault(C("modifiedAt").Default) == "CURRENT_TIMESTAMP"
            && NormType(C("isActive").Type) == "TINYINT(1)" && NormDefault(C("isActive").Default) == "1";
    }

    private static string ColumnDef(Column c)
    {
        // schema.sql types arrive with spaces squeezed out (INTUNSIGNED); put them back for the DDL.
        var type = Regex.Replace(c.Type, @"(?<=\S)(UNSIGNED|ZEROFILL)", " $1");
        var sb = new StringBuilder($".AsCustom({Lit(type)})");
        sb.Append(c.NotNull || c.Pk ? ".NotNullable()" : ".Nullable()");
        if (c.Pk) sb.Append(".PrimaryKey()");
        if (c.AutoIncrement) sb.Append(".Identity()");
        var d = NormDefault(c.Default);
        if (d == "CURRENT_TIMESTAMP") sb.Append(".WithDefault(SystemMethods.CurrentDateTime)");
        else if (d != null) sb.Append($".WithDefaultValue({Lit(d)})");
        return sb.ToString();
    }

    private static string FkName(string table, string column) => $"fk_{table}_{column}";

    private static string CreateFk(string table, ForeignKey fk, string? name = null) =>
        $"Create.ForeignKey({Lit(name ?? FkName(table, fk.Column))}).FromTable({Lit(table)}).ForeignColumn({Lit(fk.Column)}).ToTable({Lit(fk.RefTable)}).PrimaryColumn({Lit(fk.RefColumn)});";

    private static string Lit(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";

    private static string Dict(IDictionary<string, string> d) =>
        d.Count == 0 ? "new Dictionary<string, string>()"
            : "new Dictionary<string, string> { " + string.Join(", ", d.Select(kv => $"[{Lit(kv.Key)}] = {Lit(kv.Value)}")) + " }";

    // ------------------------------------------------------------------ comparison

    /// <summary>Canonical MySQL type text so schema.sql and information_schema compare equal.</summary>
    public static string NormType(string type)
    {
        var t = Regex.Replace(type.Trim().ToUpperInvariant(), @"\s+", " ");
        t = Regex.Replace(t, @"\s*\(\s*", "(");
        t = Regex.Replace(t, @"\s*,\s*", ",");
        t = Regex.Replace(t, @"\s*\)", ")");
        t = Regex.Replace(t, @"^INTEGER\b", "INT");
        if (t is "BOOL" or "BOOLEAN") return "TINYINT(1)";
        t = Regex.Replace(t, @"^(INT|BIGINT|SMALLINT|MEDIUMINT)\(\d+\)", "$1");   // display widths are ignored by MySQL 8
        t = Regex.Replace(t, @"^TINYINT\((?!1\))\d+\)", "TINYINT");
        if (t == "DECIMAL" || t == "NUMERIC") t = "DECIMAL(10,0)";
        t = Regex.Replace(t, @"^NUMERIC", "DECIMAL");
        t = Regex.Replace(t, @"^DECIMAL\((\d+)\)$", "DECIMAL($1,0)");
        return t.Replace(" ", "");
    }

    public static string? NormDefault(string? d)
    {
        if (d == null) return null;
        var v = d.Trim().Trim('\'');
        if (v.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
        if (Regex.IsMatch(v, @"^(CURRENT_TIMESTAMP|NOW)(\(\))?$", RegexOptions.IgnoreCase)) return "CURRENT_TIMESTAMP";
        return v;
    }

    private static bool SameDefault(Column a, Column b)
    {
        var (x, y) = (NormDefault(a.Default), NormDefault(b.Default));
        if (x == y) return true;
        return x != null && y != null && decimal.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var dx)
               && decimal.TryParse(y, NumberStyles.Number, CultureInfo.InvariantCulture, out var dy) && dx == dy;
    }

    private static bool SameColumn(Column live, Column want) =>
        NormType(live.Type) == NormType(want.Type) && (live.NotNull || live.Pk) == (want.NotNull || want.Pk) && SameDefault(live, want)
        && live.Pk == want.Pk && live.AutoIncrement == want.AutoIncrement;

    /// <summary>Same base type with more room (VARCHAR(100) → VARCHAR(255), DECIMAL(10,2) → DECIMAL(14,2), INT → BIGINT, TEXT → LONGTEXT).</summary>
    private static bool IsWidening(string from, string to)
    {
        static (string b, int[] n) Split(string t)
        {
            var m = Regex.Match(NormType(t), @"^([A-Z]+)(?:\(([\d,]+)\))?");
            return (m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value.Split(',').Select(int.Parse).ToArray() : []);
        }
        var (fb, fn) = Split(from);
        var (tb, tn) = Split(to);
        string[] ints = ["TINYINT", "SMALLINT", "MEDIUMINT", "INT", "BIGINT"], texts = ["TINYTEXT", "TEXT", "MEDIUMTEXT", "LONGTEXT"];
        if (Array.IndexOf(ints, fb) is var fi and >= 0 && Array.IndexOf(ints, tb) is var ti and >= 0) return ti >= fi && !(fb == "TINYINT" && fn.SequenceEqual([1]));
        if (Array.IndexOf(texts, fb) is var fx and >= 0 && Array.IndexOf(texts, tb) is var tx and >= 0) return tx >= fx;
        if (fb != tb) return fb == "VARCHAR" && texts.Contains(tb);
        if (fb is "VARCHAR" or "CHAR" or "VARBINARY") return tn.Length == 1 && fn.Length == 1 && tn[0] >= fn[0];
        if (fb == "DECIMAL" && fn.Length == 2 && tn.Length == 2) return tn[1] >= fn[1] && tn[0] - tn[1] >= fn[0] - fn[1];
        return false;
    }

    private static bool SameFk(ForeignKey live, ForeignKey want, Dictionary<string, string> renamedTo) =>
        string.Equals(renamedTo.GetValueOrDefault(want.Column, want.Column), live.Column, StringComparison.OrdinalIgnoreCase)
        && live.RefTable.Equals(want.RefTable, StringComparison.OrdinalIgnoreCase) && live.RefColumn.Equals(want.RefColumn, StringComparison.OrdinalIgnoreCase);

    private static string Describe(Column c) => $"{c.Type}{(c.NotNull ? " NOT NULL" : "")}{(NormDefault(c.Default) is { } d ? $" DEFAULT {d}" : "")}";

    // ------------------------------------------------------------------ ui.config

    /// <summary>Role name -> table -> letters, from ui.config "roles" (same defaults as the generator's role seeding).</summary>
    private static Dictionary<string, Dictionary<string, string>> RoleGrants(JsonObject? ui, List<Table> tables)
    {
        var cfg = ui?["roles"] as JsonObject ?? new JsonObject { ["Member"] = new JsonObject { ["*"] = "cru" }, ["Viewer"] = new JsonObject { ["*"] = "r" } };
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (role, value) in cfg)
        {
            if (role.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) continue;
            var grants = value as JsonObject ?? new JsonObject { ["*"] = value?.ToString() ?? "r" };
            var star = grants["*"]?.ToString() ?? "";
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (star.Length > 0) map["*"] = Letters(star);
            foreach (var t in tables) map[t.Name] = Letters(grants[t.Name]?.ToString() ?? star);
            result[role] = map;
        }
        return result;
    }

    private static string Letters(string s) => new(s.ToLowerInvariant().Where("crud".Contains).Distinct().OrderBy("crud".IndexOf).ToArray());

    private static string? MenuGroup(JsonObject? ui, string table) =>
        (ui?["menu"] as JsonArray)?.OfType<JsonObject>()
            .FirstOrDefault(g => (g["items"] as JsonArray)?.Any(i => i?.ToString() == table) == true)?["group"]?.ToString();
}
