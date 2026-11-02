using System.Text.Json;
using NeoApps.Mcp.Services;
using NeoApps.Mcp.Tools;

namespace NeoApps.Mcp;

/// <summary>
/// Command-line front end over the same services the MCP tools use, so people (and Claude Code sessions)
/// can generate apps without Claude Desktop:  neoapps new TodoApp --schema todo.sql --deploy
/// </summary>
public static class Cli
{
    private static readonly string[] Commands = ["new", "preview", "frontend", "ui", "deploy", "status", "logs", "stop", "list", "doctor", "guidelines", "infra", "help", "--help", "-h"];
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static bool IsCommand(string arg) => Commands.Contains(arg, StringComparer.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(string[] args)
    {
        var settings = NeoAppsSettings.FromEnvironment();
        var store = new ProjectStore(settings);
        var jobs = new JobManager(settings) { Echo = line => Console.WriteLine("  " + line) };
        var tools = new NeoAppsTools(settings, store, jobs, new GeneratorRunner(settings, store), new DockerService(settings, store));
        var ct = CancellationToken.None;

        string? Opt(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        bool Flag(string name) => args.Contains(name);
        string Name() => args.Length > 1 && !args[1].StartsWith('-') ? args[1] : throw new ArgumentException("Missing app name.");

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "new":
                {
                    var schema = Opt("--schema") ?? throw new ArgumentException("--schema <file.sql> is required.");
                    var ui = Opt("--ui");
                    var ok = await RunJob(jobs, tools.CreateApp(Name(), sqlFilePath: Path.GetFullPath(schema),
                        uiConfig: ui == null ? null : await File.ReadAllTextAsync(ui),
                        generateFrontend: !Flag("--no-frontend"), frontendTemplate: Opt("--template") ?? "app",
                        resetDatabase: !Flag("--keep-db"), ct: ct));
                    if (ok && Flag("--deploy"))
                        ok = await RunJob(jobs, tools.DeployApp(Name(), Flag("--consumer"), ct));
                    if (ok) Print(await tools.AppStatus(Name(), ct));
                    return ok ? 0 : 1;
                }
                case "preview":
                    Print(Requirements.Preview(store, Name(),
                        sqlSchema: Opt("--schema") is { } sf ? await File.ReadAllTextAsync(sf) : null,
                        uiConfig: Opt("--ui") is { } uf ? await File.ReadAllTextAsync(uf) : null,
                        saveVersion: Flag("--save"), copyTo: Opt("--out")));
                    return 0;
                case "frontend":
                    return await RunJob(jobs, tools.GenerateFrontend(Name(), Opt("--template") ?? "app", ct)) ? 0 : 1;
                case "ui":
                    if (Opt("--set") is { } file)
                        return await RunJob(jobs, tools.UpdateUiConfig(Name(), await File.ReadAllTextAsync(file), replace: Flag("--replace"), ct: ct)) ? 0 : 1;
                    Print(tools.GetUiConfig(Name()));
                    return 0;
                case "deploy":
                {
                    var ok = await RunJob(jobs, tools.DeployApp(Name(), Flag("--consumer"), ct));
                    if (ok) Print(await tools.AppStatus(Name(), ct));
                    return ok ? 0 : 1;
                }
                case "status": Print(await tools.AppStatus(Name(), ct)); return 0;
                case "logs": Console.WriteLine(await tools.AppLogs(Name(), Opt("--service"), int.TryParse(Opt("--tail"), out var t) ? t : 100, ct)); return 0;
                case "stop": Print(await tools.StopApp(Name(), ct)); return 0;
                case "list": Print(tools.ListApps()); return 0;
                case "doctor": Print(await tools.CheckEnvironment(ct)); return 0;
                case "guidelines": Console.WriteLine(NeoAppsTools.SchemaGuidelinesTool()); return 0;
                case "infra":
                    if (args.Length > 1 && args[1] == "down") { Print(await tools.StopInfrastructure(Flag("--remove-data"), ct)); return 0; }
                    return await RunJob(jobs, tools.StartInfrastructure(ct)) ? 0 : 1;
                default:
                    Console.WriteLine(Help);
                    return 0;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ModelContextProtocol.McpException)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 2;
        }
    }

    /// <summary>Awaits the tool call, then the background job it started, and prints the outcome.</summary>
    private static async Task<bool> RunJob(JobManager jobs, Task<object> call)
    {
        await call;
        var job = jobs.All.First();
        await job.Completion;
        if (job.State == JobState.Succeeded)
        {
            Print(job.Result);
            return true;
        }
        Console.Error.WriteLine($"\n{job.Kind} failed: {job.Error}");
        return false;
    }

    private static void Print(object? o) => Console.WriteLine(o is string s ? s : JsonSerializer.Serialize(o, Pretty));

    private const string Help = """
        NeoApps: generate and run apps from a MySQL schema.

        Usage: neoapps <command> [options]   (from the repo: dotnet run --project src/NeoApps.Mcp -- <command>)

          new <App> --schema <file.sql> [--ui ui.config.json] [--template app|dnd] [--no-frontend] [--keep-db] [--deploy] [--consumer]
                                  generate database + .NET 10 API + React UI, optionally run it in Docker
          preview <App> [--schema f.sql] [--ui f.json] [--save] [--out file.html]
                                  plan before generating: writes workspace/<App>/preview.html (data model diagram,
                                  screens, roles, AI, issues, and what changed since the last saved version).
                                  --save records the plan as the next version in workspace/<App>/history/
          frontend <App> [--template app|dnd]   regenerate the UI
          ui <App> [--set changes.json [--replace]]   show the UI config, or merge changes into it and regenerate the UI
          deploy <App> [--consumer]             build and run the app in Docker
          status <App> | logs <App> [--service api|web] [--tail N] | stop <App>
          list                                  apps created so far
          doctor                                check .NET / Docker / shared services
          guidelines                            schema rules for writing <file.sql>
          infra [down [--remove-data]]          start/stop shared MySQL, Redis, RabbitMQ

        With no command, runs as an MCP server on stdio (for Claude Desktop).
        """;
}
