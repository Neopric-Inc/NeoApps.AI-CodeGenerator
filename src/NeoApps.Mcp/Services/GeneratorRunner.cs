using System.Text.Json;
using System.Text.Json.Nodes;

namespace NeoApps.Mcp.Services;

public sealed record GeneratorResult(
    bool Success,
    string? OutputPath,
    string? Link,
    IReadOnlyList<string> Errors,
    string? AdminUsername,
    string? AdminPassword);

/// <summary>
/// Drives CodeGenerator/NoCodeAppGenerator as a child process. It relies on static state and the working
/// directory, so runs are isolated in their own process and serialised.
/// </summary>
public sealed class GeneratorRunner(NeoAppsSettings settings, ProjectStore store)
{
    private const string ResultMarker = "NEOAPPS_RESULT ";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private bool _built;

    public async Task<GeneratorResult> RunAsync(string projectName, string runName, IDictionary<string, string?> parameters, Job job, CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            await EnsureBuiltAsync(job, ct);

            var configFile = Path.Combine(settings.WorkspaceDir, projectName, $"generator-{runName}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configFile)!);
            await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true }), ct);

            job.Log($"Running generator ({runName}) for {projectName}...");
            var result = await CommandRunner.RunAsync(
                "dotnet",
                ["run", "--no-build", "--no-launch-profile", "-c", "Release", "--project", "NoCodeAppGenerator.csproj", "--", "--config", configFile],
                settings.GeneratorDir,
                job.Log,
                env: new Dictionary<string, string> { ["NEOAPPS_GENERATOR_ROOT"] = settings.GeneratorDir },
                timeout: TimeSpan.FromMinutes(30),
                ct: ct);

            var line = result.Output.Split('\n').LastOrDefault(l => l.StartsWith(ResultMarker, StringComparison.Ordinal));
            if (line is null)
                return new GeneratorResult(false, null, null, [$"Generator exited with code {result.ExitCode} without a result.", result.Tail(30)], null, null);

            var json = JsonNode.Parse(line[ResultMarker.Length..])!;
            var errors = json["error_log"]?.AsArray().Select(e => e?.GetValue<string>() ?? "").Where(e => e.Length > 0).ToList() ?? [];
            return new GeneratorResult(
                Success: json["is_successful"]?.GetValue<int>() == 1,
                OutputPath: json["output_path"]?.GetValue<string>(),
                Link: json["link"]?.GetValue<string>(),
                Errors: errors,
                AdminUsername: json["admin_username"]?.GetValue<string>(),
                AdminPassword: json["admin_password"]?.GetValue<string>());
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task EnsureBuiltAsync(Job job, CancellationToken ct)
    {
        if (_built) return;
        job.Log("Building the code generator (first run only)...");
        var build = await CommandRunner.RunAsync(
            "dotnet", ["build", "NoCodeAppGenerator.csproj", "-c", "Release", "-nologo", "-v:q"],
            settings.GeneratorDir, job.Log, timeout: TimeSpan.FromMinutes(10), ct: ct);
        if (!build.Ok)
            throw new InvalidOperationException("Building the code generator failed:\n" + build.Tail());
        _built = true;
    }

    /// <summary>Parameters shared by backend and frontend runs (database the generator introspects).</summary>
    public Dictionary<string, string?> BaseParameters(ProjectState p) => new()
    {
        ["project_id"] = "1",
        ["uid"] = "1",
        ["server"] = settings.MySqlHost,
        ["port"] = settings.MySqlPort.ToString(),
        ["username"] = settings.MySqlUser,
        ["password"] = settings.MySqlPassword,
        ["databaseName"] = p.DatabaseName,
        ["script"] = p.SchemaFile,
        ["projectName"] = p.Name,
        ["buttonClicked"] = "generate",
        ["statusOfGeneration"] = null,
        ["apiflowurl"] = null,
        ["fronttemplateurl"] = null,
        ["swgurl"] = $"http://localhost:{p.ApiPort}",
        ["noderedurl"] = null,
        ["uiConfig"] = File.Exists(store.UiConfigFile(p.Name)) ? store.UiConfigFile(p.Name) : null,
    };
}
