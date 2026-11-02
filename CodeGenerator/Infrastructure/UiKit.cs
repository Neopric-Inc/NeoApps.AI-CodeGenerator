using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using MySqlConnector;

namespace NoCodeAppGenerator.Infrastructure;

/// <summary>
/// Installs the schema-driven React UI (CodeGenerator/ReactUiKit) into a generated admin frontend and writes its
/// two runtime files: public/schema.json (read from the database) and public/ui.config.json (labels, widgets,
/// menu, theme... supplied by the user/Claude, empty by default). Also seeds RBAC roles declared in the config.
/// </summary>
public static class UiKit
{
    /// <summary>Tables the generator creates for itself; never shown as app screens.</summary>
    public static readonly HashSet<string> SystemTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders",
        "dnd_ui_versions", "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds",
        "workflow_runs", "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions",
        // AI assistant (created by the API on first use)
        "ai_files", "ai_chunks", "ai_conversations", "ai_messages", "ai_runs",
    };

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static void Install(string generatorRoot, string frontendDir, string connectionString, string appName, string uiConfigPath)
    {
        CopyDirectory(Path.Combine(generatorRoot, "ReactUiKit", "src"), Path.Combine(frontendDir, "src"));

        var publicDir = Path.Combine(frontendDir, "public");
        Directory.CreateDirectory(publicDir);
        File.WriteAllText(Path.Combine(publicDir, "schema.json"), JsonSerializer.Serialize(ReadSchema(connectionString, appName), Pretty));

        var config = LoadConfig(uiConfigPath) ?? new JsonObject();
        config["appName"] ??= Humanize(appName);
        File.WriteAllText(Path.Combine(publicDir, "ui.config.json"), config.ToJsonString(Pretty));
        // The API reads the "ai" section (assistant instructions, tools, workflows) from its own copy.
        var apiDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(frontendDir).TrimEnd('/', '\\'))!, "DotNet_Output", "solution", appName, appName + ".API");
        if (Directory.Exists(apiDir)) File.WriteAllText(Path.Combine(apiDir, "ui.config.json"), config.ToJsonString(Pretty));
        Console.WriteLine($"UI kit installed (schema.json, ui.config.json{(uiConfigPath != null ? " from " + uiConfigPath : "")}).");
    }

    public static JsonObject LoadConfig(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject;
    }

    /// <summary>
    /// Creates the roles declared in ui.config.json ("roles": {"Coordinator": {"*": "r", "documents": "crud"}}) as
    /// tenant role templates (permissionmatrix rows with user_id NULL), the same shape the in-app Roles screen edits.
    /// Letters: c = create, r = read, u = update, d = delete. "*" applies to every app table.
    /// </summary>
    public static void SeedRoles(MySqlConnection connection, string tenant, string uiConfigPath)
    {
        var roles = LoadConfig(uiConfigPath)?["roles"] as JsonObject;
        if (roles == null || roles.Count == 0)
            roles = new JsonObject { ["Member"] = new JsonObject { ["*"] = "cru" }, ["Viewer"] = new JsonObject { ["*"] = "r" } };

        var entities = new List<(int id, string name)>();
        using (var cmd = new MySqlCommand("SELECT entity_id, entity_name FROM Entities", connection))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) entities.Add((r.GetInt32(0), r.GetString(1).ToLowerInvariant()));

        foreach (var (roleName, value) in roles)
        {
            if (string.Equals(roleName, "Administrator", StringComparison.OrdinalIgnoreCase)) continue;
            var grants = value as JsonObject ?? new JsonObject { ["*"] = value?.GetValue<string>() ?? "r" };
            var fallback = grants["*"]?.GetValue<string>() ?? "";

            using var insRole = new MySqlCommand("INSERT INTO Roles (role_name, createdBy, modifiedBy, isActive) VALUES (@n, @t, @t, 1)", connection);
            insRole.Parameters.AddWithValue("@n", roleName);
            insRole.Parameters.AddWithValue("@t", tenant);
            insRole.ExecuteNonQuery();
            var roleId = (int)insRole.LastInsertedId;

            foreach (var (entityId, name) in entities)
            {
                var letters = grants[name]?.GetValue<string>() ?? (SystemTables.Contains(name) ? "" : fallback);
                using var ins = new MySqlCommand(@"INSERT INTO PermissionMatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                    VALUES (@r, @e, @cr, @cw, @cu, @cd, NULL, @t, @t, @t, 1)", connection);
                ins.Parameters.AddWithValue("@r", roleId);
                ins.Parameters.AddWithValue("@e", entityId);
                ins.Parameters.AddWithValue("@cr", letters.Contains('r') ? 1 : 0);
                ins.Parameters.AddWithValue("@cw", letters.Contains('c') ? 1 : 0);
                ins.Parameters.AddWithValue("@cu", letters.Contains('u') ? 1 : 0);
                ins.Parameters.AddWithValue("@cd", letters.Contains('d') ? 1 : 0);
                ins.Parameters.AddWithValue("@t", tenant);
                ins.ExecuteNonQuery();
            }
            Console.WriteLine($"Seeded role '{roleName}'.");
        }
    }

    private static object ReadSchema(string connectionString, string appName)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        var fks = new Dictionary<(string, string), (string table, string column)>();
        using (var cmd = new MySqlCommand(@"SELECT table_name, column_name, referenced_table_name, referenced_column_name
                FROM information_schema.key_column_usage WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) fks[(r.GetString(0).ToLowerInvariant(), r.GetString(1))] = (r.GetString(2).ToLowerInvariant(), r.GetString(3));

        var tables = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
        var primaryKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new MySqlCommand(@"SELECT c.table_name, c.column_name, c.data_type, c.column_type, c.is_nullable, c.column_default,
                       c.character_maximum_length, c.column_key, c.extra
                FROM information_schema.columns c JOIN information_schema.tables t
                  ON t.table_schema = c.table_schema AND t.table_name = c.table_name AND t.table_type = 'BASE TABLE'
                WHERE c.table_schema = DATABASE() ORDER BY c.table_name, c.ordinal_position", conn))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var table = r.GetString(0).ToLowerInvariant();
                if (SystemTables.Contains(table)) continue;
                var column = r.GetString(1);
                var isPk = r.GetString(7) == "PRI";
                if (isPk && !primaryKeys.ContainsKey(table)) primaryKeys[table] = column;
                fks.TryGetValue((table, column), out var fk);
                if (!tables.TryGetValue(table, out var cols)) tables[table] = cols = new List<object>();
                cols.Add(new
                {
                    name = column,
                    dataType = r.GetString(2).ToLowerInvariant(),
                    columnType = r.GetString(3).ToLowerInvariant(),
                    nullable = r.GetString(4) == "YES",
                    hasDefault = !r.IsDBNull(5),
                    defaultValue = r.IsDBNull(5) ? null : r.GetString(5),
                    maxLength = r.IsDBNull(6) ? (long?)null : Convert.ToInt64(r.GetValue(6)),
                    isPrimaryKey = isPk,
                    isAutoIncrement = r.GetString(8).Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                    references = fk.table == null ? null : new { table = fk.table, column = fk.column },
                });
            }
        }

        return new
        {
            app = appName,
            generatedAt = DateTime.UtcNow.ToString("o"),
            tables = tables.OrderBy(t => t.Key).Select(t => new
            {
                name = t.Key,
                primaryKey = primaryKeys.GetValueOrDefault(t.Key),
                columns = t.Value,
            }),
        };
    }

    private static string Humanize(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s ?? "", "([a-z0-9])([A-Z])", "$1 $2").Replace('_', ' ');

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, destination), overwrite: true);
    }
}
