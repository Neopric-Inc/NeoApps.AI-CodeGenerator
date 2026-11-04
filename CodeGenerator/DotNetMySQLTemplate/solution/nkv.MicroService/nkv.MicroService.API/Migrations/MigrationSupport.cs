// Database migrations for NeoApps apps, built on FluentMigrator.
//
// Every migration class in this folder (M<version>_<Name>.cs) is a versioned schema change with Up() and Down().
// `neoapps evolve` writes them from the difference between the live database and the new schema.sql; you can also
// write or edit them by hand. The API applies pending migrations on startup (set Migrations:RunOnStartup=false to
// turn that off) and records them in the VersionInfo table, so a database is only ever moved forward in place.
//
// This file is shared by every generated API and by the generator's migrator (src/NeoApps.Migrator).
// Keep the NeoApps.Migrations namespace: generated migration classes depend on it.
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace NeoApps.Migrations;

/// <summary>Runs the migrations compiled into an assembly against a MySQL database.</summary>
public static class MigrationHost
{
    /// <summary>
    /// mode "up": apply pending migrations. "baseline": record every migration as applied without running it (after a
    /// database was created from the full schema). "status": list applied and pending migrations.
    /// "down:&lt;version&gt;": roll back to that version. Returns 0 on success.
    /// </summary>
    public static int Run(string connectionString, Assembly assembly, string mode, Action<string>? log = null)
    {
        log ??= Console.WriteLine;
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb.AddMySql8().WithGlobalConnectionString(connectionString).ScanIn(assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole().SetMinimumLevel(LogLevel.Information))
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var versions = scope.ServiceProvider.GetRequiredService<FluentMigrator.Runner.IVersionLoader>();

        // Several API instances may start at once: only one applies migrations at a time.
        using var lockConn = new MySqlConnection(connectionString);
        lockConn.Open();
        using (var take = new MySqlCommand("SELECT GET_LOCK('neoapps_migrations', 120)", lockConn))
            if (Convert.ToInt32(take.ExecuteScalar() ?? 0) != 1)
                throw new InvalidOperationException("Timed out waiting for another process to finish migrating.");
        try
        {
            // A new app has no migrations yet; FluentMigrator reports that as an error.
            SortedList<long, IMigrationInfo> all;
            try { all = runner.MigrationLoader.LoadMigrations(); }
            catch (FluentMigrator.Runner.Exceptions.MissingMigrationsException) { all = new SortedList<long, IMigrationInfo>(); }
            versions.LoadVersionInfo();
            var applied = versions.VersionInfo;
            switch (mode.Split(':')[0].ToLowerInvariant())
            {
                case "status":
                    foreach (var m in all.Values)
                        log($"{(applied.HasAppliedMigration(m.Version) ? "applied" : "pending")}  {m.Version}  {Describe(m)}");
                    if (all.Count == 0) log("No migrations.");
                    return 0;
                case "baseline":
                    foreach (var m in all.Values.Where(m => !applied.HasAppliedMigration(m.Version)))
                    {
                        versions.UpdateVersionInfo(m.Version, Describe(m) + " (baseline)");
                        log($"baselined {m.Version} {Describe(m)}");
                    }
                    return 0;
                case "down":
                    runner.MigrateDown(long.Parse(mode.Split(':')[1]));
                    return 0;
                default:
                    if (all.Count == 0) { log("No migrations."); return 0; }
                    var pending = all.Values.Where(m => !applied.HasAppliedMigration(m.Version)).ToList();
                    if (pending.Count == 0) { log("Database is up to date."); return 0; }
                    foreach (var m in pending) log($"applying {m.Version} {Describe(m)}");
                    runner.MigrateUp();
                    return 0;
            }
        }
        finally
        {
            using var release = new MySqlCommand("SELECT RELEASE_LOCK('neoapps_migrations')", lockConn);
            release.ExecuteScalar();
        }
    }

    private static string Describe(IMigrationInfo m) => m.Description ?? m.Migration.GetType().Name;

    /// <summary>
    /// Command-line hook for a generated API: --migrate, --baseline, --migrations-status, --migrate-down=VERSION.
    /// Returns null when none of them was passed (the API should start normally).
    /// </summary>
    public static int? RunFromArgs(string[] args, string connectionString, Assembly assembly)
    {
        string? mode = args.Contains("--migrate") ? "up"
            : args.Contains("--baseline") ? "baseline"
            : args.Contains("--migrations-status") ? "status"
            : args.FirstOrDefault(a => a.StartsWith("--migrate-down=")) is { } d ? "down:" + d.Split('=')[1]
            : null;
        return mode == null ? null : Run(connectionString, assembly, mode);
    }
}

/// <summary>Helpers used by generated migrations. They keep NeoApps' own tables (entities, permissions, saved UI) in step with schema changes.</summary>
public static class NeoAppsMigrationExtensions
{
    private const int AdministratorRoleId = 1;
    private const string SavedUiType = "neoapps-ui-config";

    /// <summary>The audit columns every NeoApps table has.</summary>
    public static ICreateTableColumnOptionOrWithColumnSyntax WithAuditColumns(this ICreateTableWithColumnSyntax table) =>
        table.WithColumn("createdBy").AsCustom("VARCHAR(255)").NotNullable()
             .WithColumn("modifiedBy").AsCustom("VARCHAR(255)").NotNullable()
             .WithColumn("createdAt").AsCustom("DATETIME").NotNullable().WithDefault(SystemMethods.CurrentDateTime)
             .WithColumn("modifiedAt").AsCustom("DATETIME").NotNullable().WithDefault(SystemMethods.CurrentDateTime)
             .WithColumn("isActive").AsCustom("TINYINT(1)").NotNullable().WithDefaultValue(1)
             .WithColumn("created_by_user").AsCustom("VARCHAR(255)").Nullable()
             .WithColumn("modified_by_user").AsCustom("VARCHAR(255)").Nullable();

    /// <summary>
    /// Registers a new table with RBAC: adds it to entities, gives administrators full access, adds role template rows
    /// from <paramref name="roleGrants"/> (role name -> letters c/r/u/d; roles not listed get no access) and copies
    /// those rows to every existing member of each role. Safe to run more than once.
    /// </summary>
    public static void GrantNewTable(this Migration m, string table, string primaryKey, IDictionary<string, string> roleGrants) =>
        m.Execute.WithConnection((c, tx) =>
        {
            Exec(c, tx, @"INSERT INTO entities (entity_name, createdBy, modifiedBy, isActive)
                SELECT @n, 'migration', 'migration', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM entities WHERE LOWER(entity_name) = LOWER(@n))", ("@n", table));
            var entityId = Convert.ToInt32(Scalar(c, tx, "SELECT entity_id FROM entities WHERE LOWER(entity_name) = LOWER(@n) ORDER BY entity_id LIMIT 1", ("@n", table)));

            // Administrators (every admin user and every member holding the Administrator role).
            Exec(c, tx, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                SELECT DISTINCT p.role_id, @e, 1, 1, 1, 1, p.user_id, p.owner_name, 'migration', 'migration', 1 FROM permissionmatrix p
                WHERE p.role_id = @admin AND p.user_id IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM permissionmatrix x WHERE x.user_id = p.user_id AND x.entity_id = @e)", ("@e", entityId), ("@admin", AdministratorRoleId));

            // Role templates, per tenant that already has a template for the role.
            foreach (var (role, letters) in roleGrants)
                Exec(c, tx, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                    SELECT DISTINCT r.role_id, @e, @cr, @cw, @cu, @cd, NULL, p.owner_name, 'migration', 'migration', 1
                    FROM roles r JOIN permissionmatrix p ON p.role_id = r.role_id AND p.user_id IS NULL
                    WHERE r.role_name = @role AND r.role_id <> @admin
                      AND NOT EXISTS (SELECT 1 FROM permissionmatrix x WHERE x.role_id = r.role_id AND x.entity_id = @e AND x.user_id IS NULL AND x.owner_name = p.owner_name)",
                    ("@e", entityId), ("@role", role), ("@admin", AdministratorRoleId),
                    ("@cr", Has(letters, 'r')), ("@cw", Has(letters, 'c')), ("@cu", Has(letters, 'u')), ("@cd", Has(letters, 'd')));

            CopyTemplatesToMembers(c, tx, entityId);

            // The consumer service keys its queues on the table's model name.
            if (TableExists(c, tx, "messagequeue"))
                Exec(c, tx, "REPLACE INTO messagequeue (queueName, PrimaryKey) VALUES (@q, @pk)",
                    ("@q", char.ToUpperInvariant(table[0]) + table.Substring(1) + "Model"), ("@pk", primaryKey));
        });

    /// <summary>Removes a table from RBAC (used by Down() and when a table is dropped).</summary>
    public static void RevokeTable(this Migration m, string table) =>
        m.Execute.WithConnection((c, tx) =>
        {
            Exec(c, tx, "DELETE FROM permissionmatrix WHERE entity_id IN (SELECT entity_id FROM (SELECT entity_id FROM entities WHERE LOWER(entity_name) = LOWER(@n)) e)", ("@n", table));
            Exec(c, tx, "DELETE FROM entities WHERE LOWER(entity_name) = LOWER(@n)", ("@n", table));
            if (TableExists(c, tx, "messagequeue"))
                Exec(c, tx, "DELETE FROM messagequeue WHERE queueName = @q", ("@q", char.ToUpperInvariant(table[0]) + table.Substring(1) + "Model"));
        });

    /// <summary>Keeps permissions attached to a renamed table.</summary>
    public static void RenameEntity(this Migration m, string from, string to) =>
        m.Execute.WithConnection((c, tx) => Exec(c, tx, "UPDATE entities SET entity_name = @to WHERE LOWER(entity_name) = LOWER(@from)", ("@from", from), ("@to", to)));

    /// <summary>
    /// Creates a role that the new requirements introduce, with a template for every tenant: letters per table,
    /// "*" for every other app table. Existing roles are left alone (admins may have tuned them in the app).
    /// </summary>
    public static void EnsureRole(this Migration m, string role, IDictionary<string, string> grants) =>
        m.Execute.WithConnection((c, tx) =>
        {
            if (Scalar(c, tx, "SELECT COUNT(*) FROM roles WHERE role_name = @r", ("@r", role)) is long n && n > 0) return;
            Exec(c, tx, "INSERT INTO roles (role_name, createdBy, modifiedBy, isActive) VALUES (@r, 'migration', 'migration', 1)", ("@r", role));
            var roleId = Convert.ToInt32(Scalar(c, tx, "SELECT LAST_INSERT_ID()"));
            var tenants = Strings(c, tx, "SELECT DISTINCT owner_name FROM permissionmatrix WHERE role_id = @admin AND user_id IS NOT NULL AND owner_name IS NOT NULL", ("@admin", AdministratorRoleId));
            var entities = Pairs(c, tx, "SELECT entity_id, entity_name FROM entities WHERE isActive = 1");
            grants.TryGetValue("*", out var fallback);
            foreach (var tenant in tenants)
                foreach (var (id, name) in entities)
                {
                    var letters = grants.TryGetValue(name.ToLowerInvariant(), out var l) ? l : (SystemTables.Contains(name.ToLowerInvariant()) ? "" : fallback ?? "");
                    Exec(c, tx, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                        VALUES (@r, @e, @cr, @cw, @cu, @cd, NULL, @t, 'migration', 'migration', 1)",
                        ("@r", roleId), ("@e", id), ("@t", tenant),
                        ("@cr", Has(letters, 'r')), ("@cw", Has(letters, 'c')), ("@cu", Has(letters, 'u')), ("@cd", Has(letters, 'd')));
                }
        });

    /// <summary>
    /// Adds a table to a menu group in every UI configuration saved from Admin > Customize UI (a saved menu replaces
    /// the file's menu, so without this a new table would only show under "More").
    /// </summary>
    public static void AddToSavedMenu(this Migration m, string group, string table) => EditSavedMenus(m, menu =>
    {
        if (menu.OfType<JsonObject>().Any(g => (g["items"] as JsonArray)?.Any(i => i?.ToString() == table) == true)) return false;
        var target = menu.OfType<JsonObject>().FirstOrDefault(g => g["group"]?.ToString() == group);
        if (target == null) menu.Add(target = new JsonObject { ["group"] = group, ["items"] = new JsonArray() });
        ((target["items"] as JsonArray) ?? (JsonArray)(target["items"] = new JsonArray())).Add(table);
        return true;
    });

    public static void RemoveFromSavedMenu(this Migration m, string table) => EditSavedMenus(m, menu =>
    {
        var changed = false;
        foreach (var g in menu.OfType<JsonObject>())
            if (g["items"] is JsonArray items)
                foreach (var i in items.Where(i => i?.ToString() == table).ToList()) { items.Remove(i); changed = true; }
        return changed;
    });

    // ------------------------------------------------------------------ internals

    private static readonly HashSet<string> SystemTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders", "dnd_ui_versions",
        "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds", "workflow_runs",
        "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions",
        "ai_files", "ai_chunks", "ai_conversations", "ai_messages", "ai_runs", "versioninfo", "background_jobs", "background_job_events",
    };

    private static void EditSavedMenus(Migration m, Func<JsonArray, bool> edit) =>
        m.Execute.WithConnection((c, tx) =>
        {
            if (!TableExists(c, tx, "dnd_ui_versions")) return;
            var rows = Pairs(c, tx, "SELECT dnd_ui_version_id, layout FROM dnd_ui_versions WHERE dnd_ui_type = @t AND isActive = 1", ("@t", SavedUiType));
            foreach (var (id, layout) in rows)
            {
                if (JsonNode.Parse(layout) is not JsonObject cfg || cfg["menu"] is not JsonArray menu) continue;
                if (edit(menu)) Exec(c, tx, "UPDATE dnd_ui_versions SET layout = @l WHERE dnd_ui_version_id = @id", ("@l", cfg.ToJsonString()), ("@id", id));
            }
        });

    private static void CopyTemplatesToMembers(IDbConnection c, IDbTransaction tx, int entityId) =>
        Exec(c, tx, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
            SELECT t.role_id, t.entity_id, t.can_read, t.can_write, t.can_update, t.can_delete, mbr.user_id, mbr.owner_name, 'migration', 'migration', 1
            FROM (SELECT DISTINCT user_id, role_id, owner_name FROM permissionmatrix WHERE user_id IS NOT NULL AND role_id <> @admin) mbr
            JOIN permissionmatrix t ON t.role_id = mbr.role_id AND t.owner_name = mbr.owner_name AND t.user_id IS NULL AND t.entity_id = @e
            WHERE NOT EXISTS (SELECT 1 FROM permissionmatrix x WHERE x.user_id = mbr.user_id AND x.entity_id = @e)",
            ("@e", entityId), ("@admin", AdministratorRoleId));

    private static int Has(string letters, char c) => letters.Contains(c) ? 1 : 0;

    private static bool TableExists(IDbConnection c, IDbTransaction tx, string table) =>
        Convert.ToInt64(Scalar(c, tx, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND LOWER(table_name) = LOWER(@t)", ("@t", table))) > 0;

    private static IDbCommand Command(IDbConnection c, IDbTransaction tx, string sql, (string, object)[] ps)
    {
        var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in ps)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        return cmd;
    }

    private static void Exec(IDbConnection c, IDbTransaction tx, string sql, params (string, object)[] ps)
    {
        using var cmd = Command(c, tx, sql, ps);
        cmd.ExecuteNonQuery();
    }

    private static object? Scalar(IDbConnection c, IDbTransaction tx, string sql, params (string, object)[] ps)
    {
        using var cmd = Command(c, tx, sql, ps);
        return cmd.ExecuteScalar();
    }

    private static List<string> Strings(IDbConnection c, IDbTransaction tx, string sql, params (string, object)[] ps)
    {
        using var cmd = Command(c, tx, sql, ps);
        using var r = cmd.ExecuteReader();
        var list = new List<string>();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    private static List<(int, string)> Pairs(IDbConnection c, IDbTransaction tx, string sql, params (string, object)[] ps)
    {
        using var cmd = Command(c, tx, sql, ps);
        using var r = cmd.ExecuteReader();
        var list = new List<(int, string)>();
        while (r.Read()) list.Add((Convert.ToInt32(r.GetValue(0)), r.GetString(1)));
        return list;
    }
}
