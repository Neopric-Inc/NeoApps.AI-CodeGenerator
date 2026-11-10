using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Packs a generated app for NeoApps Cloud: one .tar.gz with the API and UI sources, a compose file the
/// host runs as-is, the database to create on first deploy (db/init.sql) and a manifest. The cloud host
/// builds the images itself, so nothing here depends on the CPU of the machine that packs it.
/// No secrets leave this machine: appsettings.json is stripped of connection strings, the JWT secret and
/// keys; the host supplies its own through the app's .env file.
/// </summary>
public sealed partial class CloudBundle(NeoAppsSettings settings)
{
    public const int Format = 1;

    /// <summary>Tables whose rows the new database needs: roles, permission templates, the admin, saved UI, migration history.</summary>
    private static readonly Dictionary<string, string> SeedRows = new(StringComparer.OrdinalIgnoreCase)
    {
        ["roles"] = "1=1",
        ["entities"] = "1=1",
        ["versioninfo"] = "1=1",
        ["users"] = "username = @admin",
        ["permissionmatrix"] = "user_id IS NULL OR user_id = (SELECT user_id FROM users WHERE username = @admin)",
        ["user_tenants"] = "user_id = (SELECT user_id FROM users WHERE username = @admin)",
        ["dnd_ui_versions"] = "dnd_ui_type = 'neoapps-ui-config'",
    };

    private static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase)
        { "bin", "obj", "node_modules", ".git", ".vs", ".idea", "build", "dist", "App_Data", "TestResults" };

    /// <summary>Lowercase DNS label for the app: ClaimKit → claimkit, Todo_App → todo-app.</summary>
    public static string Slug(string app)
    {
        var s = NonLabel().Replace(app.ToLowerInvariant().Replace('_', '-'), "").Trim('-');
        return s.Length > 30 ? s[..30].TrimEnd('-') : s;
    }

    public async Task<string> PackAsync(ProjectState p, string region, Action<string> log, CancellationToken ct)
    {
        if (p.BackendPath is null || !Directory.Exists(p.BackendPath))
            throw new InvalidOperationException("The backend has not been generated yet. Run neoapps_create_app first.");
        var hasWeb = p.FrontendPath is not null && Directory.Exists(p.FrontendPath);
        var slug = Slug(p.Name);

        var outDir = Path.Combine(settings.WorkspaceDir, p.Name, ".cloud");
        Directory.CreateDirectory(outDir);
        var file = Path.Combine(outDir, $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}.tar.gz");

        log("Exporting the database structure and seed rows");
        var initSql = await ExportDatabaseAsync(p, ct);

        var manifest = new JsonObject
        {
            ["format"] = Format,
            ["app"] = p.Name,
            ["slug"] = slug,
            ["region"] = region,
            ["database"] = p.DatabaseName,
            ["adminUsername"] = p.AdminUsername,
            ["hasWeb"] = hasWeb,
            ["initSql"] = "db/init.sql",
            ["packedAt"] = DateTime.UtcNow.ToString("o"),
        };

        await using (var fs = File.Create(file))
        await using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        await using (var tar = new TarWriter(gz, TarEntryFormat.Pax))
        {
            await AddTextAsync(tar, "manifest.json", manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), ct);
            await AddTextAsync(tar, "docker-compose.yml", ComposeFile(slug, hasWeb), ct);
            await AddTextAsync(tar, "db/init.sql", initSql, ct);
            log("Packing the API");
            await AddTreeAsync(tar, p.BackendPath, "api", ct);
            if (hasWeb)
            {
                log("Packing the UI");
                await AddTreeAsync(tar, p.FrontendPath!, "web", ct);
            }
        }
        log($"Bundle ready: {new FileInfo(file).Length / 1024} KB");
        return file;
    }

    /// <summary>
    /// The compose file the cloud host runs from the bundle folder. Each app has its own Redis on its own
    /// network; only the web container (for Caddy) and the API (for the shared RabbitMQ) join "neoapps".
    /// Connection strings, CORS origin and the JWT secret come from .env, which the host writes.
    /// </summary>
    public static string ComposeFile(string slug, bool hasWeb)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# NeoApps Cloud bundle for '{slug}'. The host writes .env (mode 600) next to this file.");
        sb.AppendLine($"name: app-{slug}");
        sb.AppendLine("services:");
        sb.AppendLine("  api:");
        sb.AppendLine("    build:");
        sb.AppendLine("      context: ./api");
        sb.AppendLine("      dockerfile: Dockerfile");
        sb.AppendLine("    env_file: .env");
        sb.AppendLine("    environment:");
        sb.AppendLine("      ASPNETCORE_ENVIRONMENT: Production");
        sb.AppendLine("      ConnectionStrings__Redis: \"redis:6379\"");
        sb.AppendLine("      Swagger__Enabled: \"false\"");
        sb.AppendLine("    volumes:");
        sb.AppendLine("      - app_data:/app/App_Data");
        sb.AppendLine("    networks: [default, neoapps]");
        sb.AppendLine("    depends_on: [redis]");
        sb.AppendLine("    mem_limit: 768m");
        sb.AppendLine("    cpus: 1.0");
        sb.AppendLine("    restart: unless-stopped");
        sb.AppendLine("  redis:");
        sb.AppendLine("    image: redis:7-alpine");
        sb.AppendLine("    command: [\"redis-server\", \"--save\", \"\", \"--appendonly\", \"no\", \"--maxmemory\", \"64mb\", \"--maxmemory-policy\", \"allkeys-lru\"]");
        sb.AppendLine("    mem_limit: 96m");
        sb.AppendLine("    restart: unless-stopped");
        if (hasWeb)
        {
            sb.AppendLine("  web:");
            sb.AppendLine("    build:");
            sb.AppendLine("      context: ./web");
            sb.AppendLine("      args:");
            sb.AppendLine("        REACT_APP_API_BASE_URL: /v1/api/");
            sb.AppendLine("    environment:");
            sb.AppendLine("      API_UPSTREAM: \"http://api:8080\"");
            sb.AppendLine("    networks:");
            sb.AppendLine("      default: {}");
            sb.AppendLine("      neoapps:");
            sb.AppendLine($"        aliases: [{slug}-web]");
            sb.AppendLine("    depends_on: [api]");
            sb.AppendLine("    mem_limit: 128m");
            sb.AppendLine("    cpus: 0.5");
            sb.AppendLine("    restart: unless-stopped");
        }
        sb.AppendLine("volumes:");
        sb.AppendLine("  app_data:");
        sb.AppendLine("networks:");
        sb.AppendLine("  neoapps:");
        sb.AppendLine("    external: true");
        return sb.ToString();
    }

    /// <summary>
    /// CREATE TABLE for every table, plus the rows a fresh copy needs to start: roles and permission templates,
    /// the admin account (the host sets a new password), saved UI settings and migration history. App data stays here.
    /// </summary>
    public async Task<string> ExportDatabaseAsync(ProjectState p, CancellationToken ct)
    {
        var cs = $"server={settings.MySqlHost};port={settings.MySqlPort};user={settings.MySqlUser};password={settings.MySqlPassword};database={p.DatabaseName};";
        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync(ct);

        var tables = new List<string>();
        await using (var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'BASE TABLE'", conn))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct)) tables.Add(r.GetString(0));

        var sb = new StringBuilder();
        sb.AppendLine($"-- NeoApps Cloud: database for {p.Name}, exported {DateTime.UtcNow:o}");
        sb.AppendLine("SET NAMES utf8mb4;");
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
        foreach (var t in tables)
        {
            await using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{t}`", conn);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) continue;
            sb.AppendLine().Append(AutoIncrement().Replace(r.GetString(1), "")).AppendLine(";");
        }
        foreach (var t in tables)
        {
            if (!SeedRows.TryGetValue(t, out var where)) continue;
            await using var cmd = new MySqlCommand($"SELECT * FROM `{t}` WHERE {where}", conn);
            cmd.Parameters.AddWithValue("@admin", p.AdminUsername ?? "");
            await using var r = await cmd.ExecuteReaderAsync(ct);
            var cols = Enumerable.Range(0, r.FieldCount).Select(i => $"`{r.GetName(i)}`").ToArray();
            while (await r.ReadAsync(ct))
            {
                var values = Enumerable.Range(0, r.FieldCount).Select(i => Literal(r.GetValue(i)));
                sb.AppendLine($"INSERT INTO `{t}` ({string.Join(",", cols)}) VALUES ({string.Join(",", values)});");
            }
        }
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
        return sb.ToString();
    }

    internal static string Literal(object? v) => v switch
    {
        null or DBNull => "NULL",
        bool b => b ? "1" : "0",
        DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss.ffffff}'",
        DateTimeOffset d => $"'{d.UtcDateTime:yyyy-MM-dd HH:mm:ss.ffffff}'",
        TimeSpan ts => $"'{ts}'",
        byte[] bytes => bytes.Length == 0 ? "''" : "0x" + Convert.ToHexString(bytes),
        Guid g => $"'{g}'",
        IFormattable f when v is not string => f.ToString(null, CultureInfo.InvariantCulture),
        _ => "'" + v.ToString()!.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\0", "\\0") + "'",
    };

    private static async Task AddTreeAsync(TarWriter tar, string root, string prefix, CancellationToken ct)
    {
        var stack = new Stack<string>([root]);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var sub in Directory.EnumerateDirectories(dir))
                if (!SkipDirs.Contains(Path.GetFileName(sub)) && !File.GetAttributes(sub).HasFlag(FileAttributes.ReparsePoint))
                    stack.Push(sub);
            foreach (var f in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(f);
                if (name is ".env.local" or ".DS_Store" || name.EndsWith(".user", StringComparison.Ordinal)) continue;
                var rel = prefix + "/" + Path.GetRelativePath(root, f).Replace('\\', '/');
                if (name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    await AddTextAsync(tar, rel, StripSecrets(await File.ReadAllTextAsync(f, ct)), ct);
                else
                    await tar.WriteEntryAsync(f, rel, ct);
            }
        }
    }

    /// <summary>Blanks connection strings, the JWT secret, stored passwords and keys; the cloud host supplies its own.</summary>
    internal static string StripSecrets(string json)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }); }
        catch (JsonException) { return "{}"; }
        if (root is not JsonObject o) return "{}";
        if (o["ConnectionStrings"] is JsonObject conns)
            foreach (var key in conns.Select(kv => kv.Key).ToList())
                if (key is "MySQLDatabase" or "RabitMQ" or "Redis") conns[key] = "";
        if (o["AppSettings"] is JsonObject app)
            foreach (var key in new[] { "Secret", "DefaultTokenPassword" })
                if (app.ContainsKey(key)) app[key] = "";
        if (o["AWS"] is JsonObject aws && aws.ContainsKey("SecretKey")) aws["SecretKey"] = "";
        return o.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task AddTextAsync(TarWriter tar, string name, string text, CancellationToken ct)
    {
        var entry = new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(text)), Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead };
        await tar.WriteEntryAsync(entry, ct);
    }

    [GeneratedRegex("[^a-z0-9-]")] private static partial Regex NonLabel();
    [GeneratedRegex(@" AUTO_INCREMENT=\d+")] private static partial Regex AutoIncrement();
}
