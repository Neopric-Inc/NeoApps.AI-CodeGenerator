using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NeoApps.Mcp.Services;

public sealed class ProjectState
{
    public required string Name { get; set; }
    public required string DatabaseName { get; set; }
    public required string SchemaFile { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public int ApiPort { get; set; }
    public int WebPort { get; set; }
    /// <summary>Generated .NET solution folder (contains the .sln and Dockerfile).</summary>
    public string? BackendPath { get; set; }
    /// <summary>Generated React project folder.</summary>
    public string? FrontendPath { get; set; }
    public string? FrontendTemplate { get; set; }
    public string? AdminUsername { get; set; }
    public string? AdminPassword { get; set; }

    public string ApiBaseUrl => $"http://localhost:{ApiPort}/v1/api/";
}

public sealed partial class ProjectStore(NeoAppsSettings settings)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly Lock _gate = new();

    public string ProjectDir(string name) => Path.Combine(settings.WorkspaceDir, name);
    private string StateFile(string name) => Path.Combine(ProjectDir(name), "project.json");

    public static string ValidateName(string name)
    {
        name = name?.Trim() ?? "";
        if (!ValidName().IsMatch(name))
            throw new ArgumentException(
                $"Invalid project name '{name}'. Use letters, digits and underscores, starting with a letter (it becomes a C# namespace), max 40 chars.");
        return name;
    }

    public static string ToDatabaseName(string name) => SnakeCase().Replace(name, "$1_$2").ToLowerInvariant() + "_db";

    public ProjectState? Get(string name)
    {
        var file = StateFile(name);
        return File.Exists(file) ? JsonSerializer.Deserialize<ProjectState>(File.ReadAllText(file)) : null;
    }

    public ProjectState Require(string name) =>
        Get(ValidateName(name)) ?? throw new InvalidOperationException(
            $"Project '{name}' not found. Create it first with neoapps_create_app (see neoapps_list_apps).");

    public void Save(ProjectState state)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(ProjectDir(state.Name));
            File.WriteAllText(StateFile(state.Name), JsonSerializer.Serialize(state, Json));
        }
    }

    public IReadOnlyList<ProjectState> All()
    {
        if (!Directory.Exists(settings.WorkspaceDir)) return [];
        return Directory.GetDirectories(settings.WorkspaceDir)
            .Select(d => Get(Path.GetFileName(d)))
            .OfType<ProjectState>()
            .OrderBy(p => p.Name)
            .ToList();
    }

    /// <summary>Picks API/web ports not used by other projects and currently free on this machine.</summary>
    public (int api, int web) AllocatePorts()
    {
        lock (_gate)
        {
            var used = All().SelectMany(p => new[] { p.ApiPort, p.WebPort }).ToHashSet();
            return (Next(settings.FirstApiPort), Next(settings.FirstWebPort));

            int Next(int start)
            {
                for (var port = start; port < start + 500; port++)
                    if (!used.Contains(port) && IsFree(port)) { used.Add(port); return port; }
                throw new InvalidOperationException($"No free port found from {start}.");
            }
        }
    }

    private static bool IsFree(int port)
    {
        try
        {
            using var l = new TcpListener(IPAddress.Loopback, port);
            l.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,39}$")]
    private static partial Regex ValidName();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCase();
}
