using System.ComponentModel;
using ModelContextProtocol.Server;
using NeoApps.Mcp.Services;

namespace NeoApps.Mcp.Tools;

[McpServerToolType]
public sealed class NeoAppsTools(
    NeoAppsSettings settings,
    ProjectStore store,
    JobManager jobs,
    GeneratorRunner generator,
    DockerService docker)
{
    // ---------------------------------------------------------------- environment

    [McpServerTool(Name = "neoapps_schema_guidelines", ReadOnly = true, Idempotent = true)]
    [Description("Rules and an example for writing the MySQL schema that NeoApps turns into an app. Read this before calling neoapps_create_app.")]
    public static string SchemaGuidelinesTool() => SchemaGuidelines.Text;

    [McpServerTool(Name = "neoapps_check_environment", ReadOnly = true, Idempotent = true)]
    [Description("Checks the local prerequisites (.NET SDK, Docker, Docker Compose) and the shared MySQL/Redis/RabbitMQ containers.")]
    public async Task<object> CheckEnvironment(CancellationToken ct)
    {
        var dotnet = await CommandRunner.RunAsync("dotnet", ["--list-sdks"], settings.RepoRoot, timeout: TimeSpan.FromSeconds(30), ct: ct);
        var dockerInfo = await CommandRunner.RunAsync("docker", ["version", "--format", "{{.Server.Version}}"], settings.RepoRoot, timeout: TimeSpan.FromSeconds(30), ct: ct);
        var compose = await CommandRunner.RunAsync("docker", ["compose", "version", "--short"], settings.RepoRoot, timeout: TimeSpan.FromSeconds(30), ct: ct);
        var infra = dockerInfo.Ok ? await docker.InfraStatusAsync(ct) : null;

        var hasNet10 = dotnet.Ok && dotnet.Output.Split('\n').Any(l => l.StartsWith("10.", StringComparison.Ordinal));
        var problems = new List<string>();
        if (!hasNet10) problems.Add("Install the .NET 10 SDK: https://dotnet.microsoft.com/download (macOS: brew install --cask dotnet-sdk).");
        if (!dockerInfo.Ok) problems.Add("Docker is not running or not installed. Start Docker Desktop.");
        else if (!compose.Ok) problems.Add("Docker Compose v2 is missing (it ships with Docker Desktop).");

        return new
        {
            ready = problems.Count == 0,
            repoRoot = settings.RepoRoot,
            dotnetSdks = dotnet.Ok ? dotnet.Output.Trim() : dotnet.Tail(5),
            dockerServer = dockerInfo.Ok ? dockerInfo.Output.Trim() : null,
            dockerCompose = compose.Ok ? compose.Output.Trim() : null,
            infrastructure = infra?.Ok == true ? infra.Output.Trim() : "not running (started automatically by neoapps_create_app / neoapps_deploy_app)",
            mysql = $"{settings.MySqlHost}:{settings.MySqlPort} (user {settings.MySqlUser})",
            problems,
        };
    }

    [McpServerTool(Name = "neoapps_start_infrastructure", Idempotent = true)]
    [Description("Starts (or confirms) the shared local MySQL, Redis and RabbitMQ containers used by every generated app.")]
    public Task<object> StartInfrastructure(CancellationToken ct) =>
        jobs.StartAndWaitAsync("infra", "(shared)", async (job, jct) =>
        {
            var r = await docker.InfraUpAsync(job.Log, jct);
            if (!r.Ok) throw new InvalidOperationException("docker compose up failed:\n" + r.Tail());
            return new { status = "running", mysql = $"localhost:{settings.MySqlPort}", rabbitMqUi = "http://localhost:15672 (neoapps/neoapps)" };
        }, ct);

    [McpServerTool(Name = "neoapps_stop_infrastructure", Destructive = true)]
    [Description("Stops the shared MySQL/Redis/RabbitMQ containers. Data is kept unless removeData is true.")]
    public async Task<object> StopInfrastructure(
        [Description("Also delete the database/queue volumes (irreversible).")] bool removeData = false,
        CancellationToken ct = default)
    {
        var r = await docker.InfraDownAsync(removeData, ct);
        return new { ok = r.Ok, output = r.Tail() };
    }

    // ---------------------------------------------------------------- generation

    [McpServerTool(Name = "neoapps_list_sample_schemas", ReadOnly = true, Idempotent = true)]
    [Description("Lists the example SQL schemas shipped with the repo (usable as sqlFilePath in neoapps_create_app).")]
    public object ListSampleSchemas()
    {
        var root = Path.Combine(settings.RepoRoot, "GeneratedApps");
        if (!Directory.Exists(root)) return Array.Empty<object>();
        return Directory.EnumerateFiles(root, "*.sql", SearchOption.AllDirectories)
            .Order()
            .Select(f => new { name = Path.GetFileName(Path.GetDirectoryName(f)), path = f, tables = File.ReadLines(f).Count(l => l.TrimStart().StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)) })
            .ToList();
    }

    [McpServerTool(Name = "neoapps_create_app")]
    [Description("""
        Generates a full app from a MySQL schema: creates the database, generates the .NET 10 Web API
        (CRUD + relational + reporting endpoints, JWT auth, RBAC) and, by default, the React frontend.
        Provide the schema inline (sqlSchema) or as a file (sqlFilePath). Follow neoapps_schema_guidelines.
        Long-running: returns a job; poll neoapps_job_status until it finishes. Then call neoapps_deploy_app.
        """)]
    public Task<object> CreateApp(
        [Description("App name: letters/digits/underscores, starts with a letter (becomes the C# namespace), e.g. TaskTracker.")] string name,
        [Description("Full MySQL CREATE TABLE script.")] string? sqlSchema = null,
        [Description("Absolute path to a .sql file instead of sqlSchema.")] string? sqlFilePath = null,
        [Description("Also generate the React frontend.")] bool generateFrontend = true,
        [Description("Frontend template: 'app' (ready-made admin UI) or 'dnd' (drag-and-drop page designer).")] string frontendTemplate = "app",
        [Description("Drop and recreate the app's database if it already exists. Default true for a clean generation.")] bool resetDatabase = true,
        CancellationToken ct = default)
    {
        name = ProjectStore.ValidateName(name);
        var template = NormalizeTemplate(frontendTemplate);
        if (string.IsNullOrWhiteSpace(sqlSchema) == string.IsNullOrWhiteSpace(sqlFilePath))
            throw new ArgumentException("Provide exactly one of sqlSchema or sqlFilePath.");
        if (sqlFilePath is not null && !File.Exists(sqlFilePath))
            throw new ArgumentException($"SQL file not found: {sqlFilePath}");

        var existing = store.Get(name);
        var (apiPort, webPort) = existing is { ApiPort: > 0 } ? (existing.ApiPort, existing.WebPort) : store.AllocatePorts();
        var schemaFile = Path.Combine(store.ProjectDir(name), "schema.sql");
        Directory.CreateDirectory(store.ProjectDir(name));
        File.WriteAllText(schemaFile, sqlSchema ?? File.ReadAllText(sqlFilePath!));

        var state = existing ?? new ProjectState { Name = name, DatabaseName = ProjectStore.ToDatabaseName(name), SchemaFile = schemaFile };
        state.SchemaFile = schemaFile;
        state.ApiPort = apiPort;
        state.WebPort = webPort;
        store.Save(state);

        return jobs.StartAndWaitAsync("create", name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);

            var p = generator.BaseParameters(state);
            p["DBexists"] = "No";
            p["resetDatabase"] = resetDatabase ? "Yes" : "No";
            p["Backend_technology"] = "dotnet";
            p["Technology_Frontend"] = null;
            p["projectType"] = null;
            p["rabbitMQConn"] = settings.HostRabbitMqUrl;
            p["redisConn"] = settings.HostRedis;
            var backend = await generator.RunAsync(name, "backend", p, job, jct);
            if (backend.OutputPath is null)
                throw new InvalidOperationException("Backend generation failed:\n" + string.Join('\n', backend.Errors));

            state.BackendPath = Path.Combine(backend.OutputPath, "DotNet_Output", "solution", name);
            state.AdminUsername = backend.AdminUsername ?? state.AdminUsername;
            state.AdminPassword = backend.AdminPassword ?? state.AdminPassword;
            store.Save(state);

            GeneratorResult? frontend = null;
            if (generateFrontend)
                frontend = await GenerateFrontendCoreAsync(state, template, job, jct);

            return new
            {
                app = name,
                backendSucceeded = backend.Success,
                backendPath = state.BackendPath,
                backendWarnings = backend.Errors,
                frontendPath = state.FrontendPath,
                frontendWarnings = frontend?.Errors,
                database = state.DatabaseName,
                admin = new { username = state.AdminUsername, password = state.AdminPassword },
                next = "Call neoapps_deploy_app to build and run it in Docker.",
            };
        }, ct);
    }

    [McpServerTool(Name = "neoapps_generate_frontend")]
    [Description("(Re)generates the React frontend for an existing app, wired to its API URL. Long-running: returns a job to poll.")]
    public Task<object> GenerateFrontend(
        [Description("App name used in neoapps_create_app.")] string name,
        [Description("'app' (ready-made admin UI) or 'dnd' (drag-and-drop designer).")] string frontendTemplate = "app",
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        var template = NormalizeTemplate(frontendTemplate);
        return jobs.StartAndWaitAsync("frontend", state.Name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);
            var r = await GenerateFrontendCoreAsync(state, template, job, jct);
            return new { app = state.Name, frontendPath = state.FrontendPath, warnings = r.Errors, apiBaseUrl = state.ApiBaseUrl };
        }, ct);
    }

    // ---------------------------------------------------------------- deployment

    [McpServerTool(Name = "neoapps_deploy_app")]
    [Description("Builds Docker images for the app's API (and frontend if generated) and runs them locally next to the shared MySQL/Redis/RabbitMQ. Long-running (first build downloads images): returns a job to poll.")]
    public Task<object> DeployApp(
        [Description("App name.")] string name,
        [Description("Also run the RabbitMQ consumer/workflow worker (only needed for workflow automations).")] bool includeConsumer = false,
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        return jobs.StartAndWaitAsync("deploy", state.Name, async (job, jct) =>
        {
            var r = await docker.DeployAsync(state, includeConsumer, job.Log, jct);
            if (!r.Ok) throw new InvalidOperationException("docker compose failed:\n" + r.Tail(60));
            return Urls(state, deployed: true);
        }, ct);
    }

    [McpServerTool(Name = "neoapps_app_status", ReadOnly = true)]
    [Description("Shows an app's containers, URLs, ports, paths and login.")]
    public async Task<object> AppStatus([Description("App name.")] string name, CancellationToken ct = default)
    {
        var state = store.Require(name);
        var ps = docker.IsDeployed(state) ? await docker.StatusAsync(state, ct) : null;
        return new
        {
            info = Urls(state, deployed: docker.IsDeployed(state)),
            containers = ps is null ? "not deployed" : ps.Output.Trim(),
            backendPath = state.BackendPath,
            frontendPath = state.FrontendPath,
            schemaFile = state.SchemaFile,
            database = state.DatabaseName,
        };
    }

    [McpServerTool(Name = "neoapps_app_logs", ReadOnly = true)]
    [Description("Returns recent container logs for a deployed app.")]
    public async Task<string> AppLogs(
        [Description("App name.")] string name,
        [Description("Service: api, web or consumer. Omit for all.")] string? service = null,
        [Description("Number of lines per service.")] int tail = 100,
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        if (!docker.IsDeployed(state)) return "The app is not deployed. Call neoapps_deploy_app first.";
        var r = await docker.LogsAsync(state, service, Math.Clamp(tail, 10, 2000), ct);
        return r.Output;
    }

    [McpServerTool(Name = "neoapps_stop_app", Destructive = true, Idempotent = true)]
    [Description("Stops and removes an app's containers. Generated code and database are kept.")]
    public async Task<object> StopApp([Description("App name.")] string name, CancellationToken ct = default)
    {
        var state = store.Require(name);
        if (!docker.IsDeployed(state)) return new { ok = true, output = "Not deployed." };
        var r = await docker.StopAsync(state, ct);
        return new { ok = r.Ok, output = r.Tail() };
    }

    [McpServerTool(Name = "neoapps_list_apps", ReadOnly = true, Idempotent = true)]
    [Description("Lists apps created through this server with their URLs and paths.")]
    public object ListApps() => store.All().Select(p => new
    {
        p.Name,
        p.DatabaseName,
        api = $"http://localhost:{p.ApiPort}/swagger",
        web = p.FrontendPath is null ? null : $"http://localhost:{p.WebPort}",
        p.BackendPath,
        p.FrontendPath,
        p.CreatedAt,
    }).ToList();

    [McpServerTool(Name = "neoapps_job_status", ReadOnly = true)]
    [Description("Status, result and recent log of a background job started by create/generate/deploy/start tools. Waits up to waitSeconds for it to finish.")]
    public async Task<object> JobStatus(
        [Description("Job id returned by a previous call.")] string jobId,
        [Description("Seconds to wait for completion before returning (0-50).")] int waitSeconds = 30,
        [Description("Log lines to include.")] int logLines = 30,
        CancellationToken ct = default)
    {
        var job = jobs.Get(jobId);
        if (job is null)
            return new { error = $"Unknown job '{jobId}'. Jobs are kept in memory and are lost when Claude Desktop restarts.", recent = jobs.All.Take(5).Select(j => new { j.Id, j.Kind, j.Project, state = j.State.ToString() }) };
        if (job.State == JobState.Running && waitSeconds > 0)
            await Task.WhenAny(job.Completion, Task.Delay(TimeSpan.FromSeconds(Math.Clamp(waitSeconds, 0, 50)), ct));
        return job.Summary(Math.Clamp(logLines, 5, 400));
    }

    // ---------------------------------------------------------------- helpers

    private async Task<GeneratorResult> GenerateFrontendCoreAsync(ProjectState state, string template, Job job, CancellationToken ct)
    {
        var p = generator.BaseParameters(state);
        p["DBexists"] = "Yes";
        p["Backend_technology"] = null;
        p["Technology_Frontend"] = "reactts";
        p["projectType"] = template == "dnd" ? "dnd" : null;
        p["rabbitMQConn"] = state.ApiBaseUrl; // the frontend generator reads the API base URL from this key
        var r = await generator.RunAsync(state.Name, "frontend", p, job, ct);
        if (r.OutputPath is null)
            throw new InvalidOperationException("Frontend generation failed:\n" + string.Join('\n', r.Errors));
        state.FrontendPath = Path.Combine(r.OutputPath, template == "dnd" ? "ReactTs_Output1" : "ReactTs_Output3");
        state.FrontendTemplate = template;
        store.Save(state);
        return r;
    }

    private async Task EnsureInfraAsync(Job job, CancellationToken ct)
    {
        if (!string.Equals(settings.MySqlHost, "127.0.0.1", StringComparison.Ordinal) && !string.Equals(settings.MySqlHost, "localhost", StringComparison.Ordinal))
            return; // user points at their own MySQL
        job.Log("Ensuring shared infrastructure (MySQL, Redis, RabbitMQ) is running...");
        var r = await docker.InfraUpAsync(job.Log, ct);
        if (!r.Ok)
            throw new InvalidOperationException("Could not start the shared containers. Is Docker Desktop running?\n" + r.Tail());
    }

    private object Urls(ProjectState p, bool deployed) => new
    {
        app = p.Name,
        deployed,
        web = p.FrontendPath is null ? null : $"http://localhost:{p.WebPort}",
        swagger = $"http://localhost:{p.ApiPort}/swagger",
        apiBaseUrl = p.ApiBaseUrl,
        health = $"http://localhost:{p.ApiPort}/health",
        admin = new { username = p.AdminUsername, password = p.AdminPassword },
        note = "Images build on first deploy (a few minutes). Credentials are for local development only.",
    };

    private static string NormalizeTemplate(string t) => t?.Trim().ToLowerInvariant() switch
    {
        null or "" or "app" or "frontend" or "admin" => "app",
        "dnd" or "designer" or "drag-and-drop" => "dnd",
        _ => throw new ArgumentException("frontendTemplate must be 'app' or 'dnd'."),
    };
}
