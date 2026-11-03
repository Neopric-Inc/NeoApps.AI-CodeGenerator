namespace NeoApps.Mcp.Services;

/// <summary>
/// Builds src/NeoApps.Migrator with an app's migrations compiled in and runs it (up / baseline / status / down:V),
/// and copies the migrations into the generated API so it applies the same classes on startup.
/// </summary>
public sealed class MigrationRunner(NeoAppsSettings settings, ProjectStore store)
{
    public string MigrationsDir(string app) => Path.Combine(store.ProjectDir(app), "migrations");

    public string ConnectionString(ProjectState p) =>
        $"server={settings.MySqlHost};port={settings.MySqlPort};user={settings.MySqlUser};password={settings.MySqlPassword};database={p.DatabaseName};";

    public bool HasMigrations(string app) => Directory.Exists(MigrationsDir(app)) && Directory.EnumerateFiles(MigrationsDir(app), "*.cs").Any();

    public async Task<CommandResult> RunAsync(ProjectState p, string mode, Action<string> log, CancellationToken ct)
    {
        var dir = MigrationsDir(p.Name);
        Directory.CreateDirectory(dir);
        var output = Path.Combine(store.ProjectDir(p.Name), ".migrator");
        var project = Path.Combine(settings.RepoRoot, "src", "NeoApps.Migrator", "NeoApps.Migrator.csproj");
        var build = await CommandRunner.RunAsync("dotnet",
            ["build", project, "-c", "Release", "-nologo", "-v", "q", "-clp:ErrorsOnly", $"-p:MigrationsDir={dir}", "-o", output],
            settings.RepoRoot, log, timeout: TimeSpan.FromMinutes(5), ct: ct);
        if (!build.Ok) return build;
        return await CommandRunner.RunAsync("dotnet", [Path.Combine(output, "neoapps-migrator.dll"), mode, "--connection", ConnectionString(p)],
            settings.RepoRoot, log, timeout: TimeSpan.FromMinutes(30), ct: ct);
    }

    /// <summary>Copies workspace/App/migrations/*.cs into the generated API's Migrations folder (next to MigrationSupport.cs).</summary>
    public int CopyIntoApi(ProjectState p)
    {
        if (p.BackendPath is null) return 0;
        var target = Path.Combine(p.BackendPath, $"{p.Name}.API", "Migrations");
        Directory.CreateDirectory(target);
        foreach (var stale in Directory.EnumerateFiles(target, "M*.cs").Where(f => Path.GetFileName(f) != "MigrationSupport.cs"))
            File.Delete(stale);
        var n = 0;
        if (Directory.Exists(MigrationsDir(p.Name)))
            foreach (var f in Directory.EnumerateFiles(MigrationsDir(p.Name), "*.cs"))
            {
                File.Copy(f, Path.Combine(target, Path.GetFileName(f)), overwrite: true);
                n++;
            }
        return n;
    }
}
