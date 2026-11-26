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
        // DataGuard tenancy (created by the API on startup)
        "user_tenants", "login_attempts", "user_security", "number_sequences",
        // background jobs (created by the API on startup)
        "background_jobs", "background_job_events", "job_schedules", "user_notifications", "user_notification_settings", "share_links", "member_invites", "user_emails", "login_codes", "user_preferences",
        // migration history (FluentMigrator)
        "versioninfo",
    };

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static void Install(string generatorRoot, string frontendDir, string connectionString, string appName, string uiConfigPath)
    {
        // The previous per-table UI (redux pages, Syncfusion/antd/bootstrap components) is no longer routed or built:
        // remove it so the app only contains the schema-driven kit and its custom screens.
        foreach (var legacy in new[] { "components", "redux", "pages", "services", "Dnd", "template", "test", "styles" })
        {
            var d = Path.Combine(frontendDir, "src", legacy);
            if (Directory.Exists(d)) Directory.Delete(d, true);
        }
        foreach (var legacy in new[] { "i18n.js", "meta.json", "react-app-env.d.ts", "reportWebVitals.ts", "setupTests.ts" })
        {
            var f = Path.Combine(frontendDir, "src", legacy);
            if (File.Exists(f)) File.Delete(f);
        }
        CopyDirectory(Path.Combine(generatorRoot, "ReactUiKit", "src"), Path.Combine(frontendDir, "src"));
        // App-specific screens: <dir of ui.config>/custom replaces the kit's empty src/custom extension point.
        var customDir = string.IsNullOrEmpty(uiConfigPath) ? null : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(uiConfigPath))!, "custom");
        if (customDir != null && Directory.Exists(customDir))
        {
            CopyDirectory(customDir, Path.Combine(frontendDir, "src", "custom"));
            Console.WriteLine($"Custom screens installed from {customDir}.");
        }

        var publicDir = Path.Combine(frontendDir, "public");
        Directory.CreateDirectory(publicDir);
        File.WriteAllText(Path.Combine(publicDir, "schema.json"), JsonSerializer.Serialize(ReadSchema(connectionString, appName), Pretty));

        var config = LoadConfig(uiConfigPath) ?? new JsonObject();
        config["appName"] ??= Humanize(appName);
        File.WriteAllText(Path.Combine(publicDir, "ui.config.json"), config.ToJsonString(Pretty));
        WritePwa(generatorRoot, publicDir, config);
        // The API reads the "ai" section (assistant instructions, tools, workflows) from its own copy.
        var apiDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(frontendDir).TrimEnd('/', '\\'))!, "DotNet_Output", "solution", appName, appName + ".API");
        if (Directory.Exists(apiDir))
        {
            File.WriteAllText(Path.Combine(apiDir, "ui.config.json"), config.ToJsonString(Pretty));
            // The app's own UI files are also served by its API (/app-files/...), so one built web image can serve any
            // app: the web container asks its API for them (default.conf) and falls back to its own copies.
            var appFiles = Path.Combine(apiDir, "app-files");
            Directory.CreateDirectory(appFiles);
            foreach (var f in AppFiles)
                if (File.Exists(Path.Combine(publicDir, f))) File.Copy(Path.Combine(publicDir, f), Path.Combine(appFiles, f), true);
        }
        // the page title comes from the app's name at run time (nginx, or the app itself), not from the build
        var index = Path.Combine(frontendDir, "index.html");
        if (File.Exists(index))
            File.WriteAllText(index, System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(index), "<title>[^<]*</title>", "<title>__APP_NAME__</title>"),
                "(name=\"(apple-mobile-web-app-title|description)\" content=\")[^\"]*\"", "${1}__APP_NAME__\""));
        // nothing else in the UI project names the app, so the same build can serve any app
        foreach (var lockOrPackage in new[] { "package.json", "package-lock.json" })
        {
            var f = Path.Combine(frontendDir, lockOrPackage);
            if (!File.Exists(f) || JsonNode.Parse(File.ReadAllText(f)) is not JsonObject pkg) continue;
            pkg["name"] = "neoapps-ui";
            if (pkg["packages"]?[""] is JsonObject root) root["name"] = "neoapps-ui";
            File.WriteAllText(f, pkg.ToJsonString(Pretty));
        }
        var readme = Path.Combine(frontendDir, "README.MD");
        if (File.Exists(readme)) File.WriteAllText(readme, "# App UI\n\nGenerated by NeoApps.AI. Schema, settings and icons for this app are in public/ (and served by the app's API).\n");
        Console.WriteLine($"UI kit installed (schema.json, ui.config.json{(uiConfigPath != null ? " from " + uiConfigPath : "")}).");
    }

    /// <summary>Files that differ per app (everything else in the built UI is the same for every app).</summary>
    public static readonly string[] AppFiles =
        { "schema.json", "ui.config.json", "manifest.json", "icon.svg", "icon-192.png", "icon-512.png", "icon-maskable-512.png", "apple-touch-icon.png", "favicon-32.png" };

    /// <summary>
    /// Makes the app installable (desktop, Android, iPhone home screen): manifest.json named and coloured from
    /// ui.config.json, generated icons (SVG with the app's initials + solid PNGs), and the service worker
    /// (ReactUiKit/pwa/sw.js: caches the app shell only; API calls always go to the network).
    /// </summary>
    static void WritePwa(string generatorRoot, string publicDir, JsonObject config)
    {
        var name = config["appName"]?.GetValue<string>() ?? "App";
        var color = config["theme"]?["primary"]?.GetValue<string>() is { } c && System.Text.RegularExpressions.Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$") ? c : "#3b5bdb";
        var words = name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        // the same two letters as the sidebar logo (Shell.tsx), so the installed icon matches the app
        var letters = new string(name.Where(char.IsAsciiLetter).ToArray());
        var initials = letters.Length > 0 ? letters[..Math.Min(2, letters.Length)].ToUpperInvariant() : "A";
        var manifest = new JsonObject
        {
            ["name"] = name,
            ["short_name"] = name.Length > 12 ? (words.Length > 0 ? words[0] : name)[..Math.Min(12, (words.Length > 0 ? words[0] : name).Length)] : name,
            ["description"] = config["tagline"]?.GetValue<string>() ?? name,
            ["start_url"] = "/",
            ["scope"] = "/",
            ["display"] = "standalone",
            ["background_color"] = "#ffffff",
            ["theme_color"] = color,
            ["icons"] = new JsonArray(
                new JsonObject { ["src"] = "/icon.svg", ["sizes"] = "any", ["type"] = "image/svg+xml" },
                new JsonObject { ["src"] = "/icon-192.png", ["sizes"] = "192x192", ["type"] = "image/png" },
                new JsonObject { ["src"] = "/icon-512.png", ["sizes"] = "512x512", ["type"] = "image/png" },
                new JsonObject { ["src"] = "/icon-maskable-512.png", ["sizes"] = "512x512", ["type"] = "image/png", ["purpose"] = "maskable" }),
        };
        File.WriteAllText(Path.Combine(publicDir, "manifest.json"), manifest.ToJsonString(Pretty));
        File.WriteAllText(Path.Combine(publicDir, "icon.svg"),
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\"><rect width=\"512\" height=\"512\" rx=\"112\" fill=\"{color}\"/>" +
            $"<text x=\"256\" y=\"256\" dy=\".35em\" text-anchor=\"middle\" font-family=\"Inter, Segoe UI, Arial, sans-serif\" font-size=\"{(initials.Length > 1 ? 205 : 266)}\" font-weight=\"600\" fill=\"{((0.2126 * Convert.ToByte(color[1..3], 16) + 0.7152 * Convert.ToByte(color[3..5], 16) + 0.0722 * Convert.ToByte(color[5..7], 16)) / 255 > 0.6 ? "#1d2330" : "#fff")}\">{System.Net.WebUtility.HtmlEncode(initials)}</text></svg>");
        var (r, g, b) = (Convert.ToByte(color[1..3], 16), Convert.ToByte(color[3..5], 16), Convert.ToByte(color[5..7], 16));
        var font = Path.Combine(generatorRoot, "ReactUiKit", "pwa", "Inter-SemiBold.ttf");
        foreach (var (file, size, shape) in new[] { ("icon-192.png", 192, IconShape.Rounded), ("icon-512.png", 512, IconShape.Rounded),
                     ("icon-maskable-512.png", 512, IconShape.Maskable), ("apple-touch-icon.png", 180, IconShape.Square), ("favicon-32.png", 32, IconShape.Rounded) })
        {
            byte[] png;
            try { png = IconPng(size, initials, r, g, b, shape, font); }
            catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or IOException)
            {
                Console.WriteLine($"PWA icon {file}: drawing unavailable ({ex.GetType().Name}); using a plain colour square.");
                png = SolidPng(size, r, g, b);
            }
            File.WriteAllBytes(Path.Combine(publicDir, file), png);
        }
        var sw = Path.Combine(generatorRoot, "ReactUiKit", "pwa", "sw.js");
        if (File.Exists(sw)) File.Copy(sw, Path.Combine(publicDir, "sw.js"), true);
    }

    enum IconShape { Rounded, Square, Maskable }

    /// <summary>
    /// The app's icon: its letters (Inter SemiBold, bundled under its open font licence) on the theme colour, white or
    /// dark text by contrast. Rounded for "any"; full-bleed with letters inside the 80% safe zone for "maskable"; full
    /// square for iOS, which rounds the corners itself.
    /// </summary>
    static byte[] IconPng(int size, string text, byte r, byte g, byte b, IconShape shape, string fontPath)
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(size, size, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        var bg = new SkiaSharp.SKColor(r, g, b);
        using (var fill = new SkiaSharp.SKPaint { Color = bg, IsAntialias = true })
        {
            if (shape == IconShape.Rounded) canvas.DrawRoundRect(new SkiaSharp.SKRect(0, 0, size, size), size * 0.22f, size * 0.22f, fill);
            else canvas.DrawRect(0, 0, size, size, fill);
        }
        var luminance = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
        using var typeface = File.Exists(fontPath) ? SkiaSharp.SKTypeface.FromFile(fontPath) : SkiaSharp.SKTypeface.Default;
        var scale = shape == IconShape.Maskable ? 0.8f : 1f;
        using var font = new SkiaSharp.SKFont(typeface, size * (text.Length > 1 ? 0.40f : 0.52f) * scale);
        using var ink = new SkiaSharp.SKPaint { Color = luminance > 0.6 ? new SkiaSharp.SKColor(0x1d, 0x23, 0x30) : SkiaSharp.SKColors.White, IsAntialias = true };
        var width = font.MeasureText(text);
        font.GetFontMetrics(out var m);
        canvas.DrawText(text, (size - width) / 2, size / 2f - (m.Ascent + m.Descent) / 2, SkiaSharp.SKTextAlign.Left, font, ink);
        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>A square PNG of one colour (no image library needed).</summary>
    static byte[] SolidPng(int size, byte r, byte g, byte b)
    {
        var raw = new byte[size * (size * 3 + 1)];
        for (var y = 0; y < size; y++)
        {
            var row = y * (size * 3 + 1);
            for (var x = 0; x < size; x++) { raw[row + 1 + x * 3] = r; raw[row + 2 + x * 3] = g; raw[row + 3 + x * 3] = b; }
        }
        using var zipped = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(zipped, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(string type, byte[] data)
        {
            var len = BitConverter.GetBytes(data.Length); if (BitConverter.IsLittleEndian) Array.Reverse(len);
            png.Write(len);
            var td = System.Text.Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
            png.Write(td);
            var crc = BitConverter.GetBytes(Crc32(td)); if (BitConverter.IsLittleEndian) Array.Reverse(crc);
            png.Write(crc);
        }
        var ihdr = new byte[13];
        var s = BitConverter.GetBytes(size); if (BitConverter.IsLittleEndian) Array.Reverse(s);
        Array.Copy(s, 0, ihdr, 0, 4); Array.Copy(s, 0, ihdr, 4, 4);
        ihdr[8] = 8; ihdr[9] = 2; // 8-bit RGB
        Chunk("IHDR", ihdr);
        Chunk("IDAT", zipped.ToArray());
        Chunk("IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var d in data)
        {
            crc ^= d;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
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
    /// <summary>
    /// Sample rows from schema.sql belong to the workspace owner. Rows are owner-scoped (createdBy holds the owner), so
    /// seed rows written with another createdBy ('sample', 'system', ...) were invisible: lists came back empty,
    /// "Requested by" pickers had nothing to choose, and row scopes that look a user up in a seeded table found no one.
    /// Runs once, when the database is first set up.
    /// </summary>
    public static void ClaimSeedRows(MySqlConnection connection, string tenant)
    {
        var tables = new List<string>();
        using (var cmd = new MySqlCommand(@"SELECT c.TABLE_NAME FROM information_schema.COLUMNS c
            JOIN information_schema.COLUMNS m ON m.TABLE_SCHEMA = c.TABLE_SCHEMA AND m.TABLE_NAME = c.TABLE_NAME AND LOWER(m.COLUMN_NAME) = 'modifiedby'
            WHERE c.TABLE_SCHEMA = DATABASE() AND LOWER(c.COLUMN_NAME) = 'createdby'", connection))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) tables.Add(r.GetString(0));
        var claimed = 0;
        foreach (var t in tables.Where(t => !SystemTables.Contains(t) && t.All(ch => char.IsLetterOrDigit(ch) || ch == '_')))
        {
            using var upd = new MySqlCommand($"UPDATE `{t}` SET createdBy = @t, modifiedBy = @t WHERE createdBy <> @t OR modifiedBy <> @t", connection);
            upd.Parameters.AddWithValue("@t", tenant);
            claimed += upd.ExecuteNonQuery();
        }
        if (claimed > 0) Console.WriteLine($"Sample data: {claimed} row(s) assigned to the workspace owner.");
    }

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
