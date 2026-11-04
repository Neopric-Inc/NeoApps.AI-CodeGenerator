using ModelContextProtocol;
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
    private readonly MigrationRunner migrations = new(settings, store);

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
        [Description("Optional ui.config.json content (see neoapps_ui_config_guide): labels, widgets, menu, theme, dashboard, roles.")] string? uiConfig = null,
        CancellationToken ct = default)
    {
        name = ProjectStore.ValidateName(name);
        var template = NormalizeTemplate(frontendTemplate);
        if (string.IsNullOrWhiteSpace(sqlSchema) == string.IsNullOrWhiteSpace(sqlFilePath))
            throw new McpException("Provide exactly one of sqlSchema or sqlFilePath.");
        if (sqlFilePath is not null && !File.Exists(sqlFilePath))
            throw new McpException($"SQL file not found: {sqlFilePath}");

        var existing = store.Get(name);
        var (apiPort, webPort) = existing is { ApiPort: > 0 } ? (existing.ApiPort, existing.WebPort) : store.AllocatePorts();
        var schemaFile = Path.Combine(store.ProjectDir(name), "schema.sql");
        Directory.CreateDirectory(store.ProjectDir(name));
        File.WriteAllText(schemaFile, sqlSchema ?? File.ReadAllText(sqlFilePath!));
        if (!string.IsNullOrWhiteSpace(uiConfig))
            File.WriteAllText(store.UiConfigFile(name), UiConfigTools.Normalize(uiConfig));

        var state = existing ?? new ProjectState { Name = name, DatabaseName = ProjectStore.ToDatabaseName(name), SchemaFile = schemaFile };
        state.SchemaFile = schemaFile;
        state.ApiPort = apiPort;
        state.WebPort = webPort;
        store.Save(state);

        return jobs.StartAndWaitAsync("create", name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);

            var backend = await GenerateBackendCoreAsync(state, databaseExists: false, resetDatabase, job, jct);
            if (resetDatabase && migrations.HasMigrations(name))
            {
                // The database was just built from the full schema.sql, which already contains every migration's change.
                job.Log("Recording existing migrations as applied (baseline)...");
                var b = await migrations.RunAsync(state, "baseline", job.Log, jct);
                if (!b.Ok) throw new InvalidOperationException("Could not baseline migrations:\n" + b.Tail());
            }

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

    [McpServerTool(Name = "neoapps_evolve_app")]
    [Description("""
        Applies a requirements change to an existing app WITHOUT losing its data. Compares the live database with the new
        schema.sql, writes the difference as a versioned C# migration (FluentMigrator, Up/Down) in
        workspace/<name>/migrations/, backs up the database, applies the migration, grants new tables to existing roles
        (from ui.config "roles"), adds them to saved menus, regenerates the API and UI on the kept data, and records the
        version. The generated API applies the same migrations on startup, so deploying moves other databases forward in
        place. Dropping tables/columns needs allowDestructive. Renames need hints ({"old_table":"new_table",
        "table.old_col":"new_col"}, or workspace/<name>/migrate.json). Use planOnly=true first to show the user the plan.
        Long-running: returns a job.
        """)]
    public Task<object> EvolveApp(
        [Description("App name.")] string name,
        [Description("New full MySQL schema; omit to use workspace/<name>/schema.sql as edited.")] string? sqlSchema = null,
        [Description("Absolute path to a .sql file instead of sqlSchema.")] string? sqlFilePath = null,
        [Description("New ui.config.json content; omit to use workspace/<name>/ui.config.json as edited.")] string? uiConfig = null,
        [Description("Short name for the migration, e.g. 'add equipment tracking'.")] string migrationName = "schema change",
        [Description("Allow dropping tables and columns (deletes their data).")] bool allowDestructive = false,
        [Description("Rename hints as JSON, e.g. {\"tasks.title\":\"name\"}.")] string? renames = null,
        [Description("Only compute and return the plan and the migration code; change nothing.")] bool planOnly = false,
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        if (sqlSchema is not null && sqlFilePath is not null) throw new McpException("Provide at most one of sqlSchema or sqlFilePath.");
        var desiredSql = sqlSchema ?? (sqlFilePath is not null ? File.ReadAllText(sqlFilePath) : File.ReadAllText(state.SchemaFile));
        var uiJson = uiConfig is not null ? UiConfigTools.Normalize(uiConfig) : (File.Exists(store.UiConfigFile(name)) ? File.ReadAllText(store.UiConfigFile(name)) : null);
        // The last approved requirements (history/vN), else the files as they are before this change.
        var historyDir = Path.Combine(store.ProjectDir(name), "history");
        var lastVersion = Directory.Exists(historyDir) ? Directory.GetDirectories(historyDir, "v*").Select(d => int.TryParse(Path.GetFileName(d)[1..], out var v) ? v : 0).DefaultIfEmpty(0).Max() : 0;
        var previousUiFile = lastVersion > 0 ? Path.Combine(historyDir, $"v{lastVersion}", "ui.config.json") : store.UiConfigFile(name);
        var previousUiJson = File.Exists(previousUiFile) ? File.ReadAllText(previousUiFile) : null;
        var renamesJson = renames ?? (File.Exists(Path.Combine(store.ProjectDir(name), "migrate.json")) ? File.ReadAllText(Path.Combine(store.ProjectDir(name), "migrate.json")) : null);

        return jobs.StartAndWaitAsync(planOnly ? "plan" : "evolve", state.Name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);
            var cs = migrations.ConnectionString(state);

            // Hand-written or earlier migrations that aren't applied yet go first, so the diff starts from the real state.
            if (migrations.HasMigrations(name) && planOnly)
            {
                var status = await migrations.RunAsync(state, "status", job.Log, jct);
                var pending = status.Output.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("pending ")).ToArray();
                if (pending.Length > 0)
                    return new { app = name, pending, note = $"Apply these first (neoapps migrate {name} up, or run evolve without --plan, which applies them before comparing); the plan would otherwise repeat their changes." };
            }
            else if (migrations.HasMigrations(name))
            {
                job.Log("Applying pending migrations before comparing...");
                var pre = await migrations.RunAsync(state, "up", job.Log, jct);
                if (!pre.Ok) throw new InvalidOperationException("Pending migrations failed:\n" + pre.Tail());
            }

            var ui = string.IsNullOrWhiteSpace(uiJson) ? null : System.Text.Json.Nodes.JsonNode.Parse(uiJson) as System.Text.Json.Nodes.JsonObject;
            var plan = SchemaMigrations.Build(SchemaMigrations.ReadLive(cs), AppPreview.Parse(desiredSql), ui,
                string.IsNullOrWhiteSpace(renamesJson) ? null : System.Text.Json.Nodes.JsonNode.Parse(renamesJson) as System.Text.Json.Nodes.JsonObject,
                allowDestructive, SchemaMigrations.RoleNames(cs), t => SchemaMigrations.TableHasRows(cs, t), migrationName, DateTimeOffset.Now,
                string.IsNullOrWhiteSpace(previousUiJson) ? null : System.Text.Json.Nodes.JsonNode.Parse(previousUiJson) as System.Text.Json.Nodes.JsonObject);
            if (planOnly)
                return new { app = name, changes = plan.Changes, warnings = plan.Warnings, skipped = plan.Skipped, migration = plan.Code ?? "(no database changes)" };

            // From here on the change is real: save the new requirements.
            File.WriteAllText(state.SchemaFile, desiredSql);
            if (uiConfig is not null) File.WriteAllText(store.UiConfigFile(name), uiJson!);

            string? migrationFile = null, backup = null;
            if (plan.HasChanges)
            {
                Directory.CreateDirectory(migrations.MigrationsDir(name));
                migrationFile = Path.Combine(migrations.MigrationsDir(name), plan.ClassName + ".cs");
                File.WriteAllText(migrationFile, plan.Code);
                job.Log($"Wrote {migrationFile}");
                backup = await BackupAsync(state, plan.Version, job, jct);
                var r = await migrations.RunAsync(state, "up", job.Log, jct);
                if (!r.Ok)
                {
                    // MySQL can't roll back DDL: keep the file for reference and let the next evolve diff the remainder.
                    var failedDir = Path.Combine(migrations.MigrationsDir(name), "failed");
                    Directory.CreateDirectory(failedDir);
                    File.Move(migrationFile, Path.Combine(failedDir, Path.GetFileName(migrationFile)), overwrite: true);
                    throw new InvalidOperationException($"Migration failed (backup: {backup ?? "none"}). Steps before the failure stay applied; fix schema.sql and run evolve again.\n" + r.Tail());
                }
            }
            else job.Log("No database changes; regenerating the API and UI only.");

            var backend = await GenerateBackendCoreAsync(state, databaseExists: true, resetDatabase: false, job, jct);
            GeneratorResult? frontend = null;
            if (state.FrontendPath is not null)
                frontend = await GenerateFrontendCoreAsync(state, state.FrontendTemplate ?? "app", job, jct);
            var version = Requirements.Preview(store, name, saveVersion: true);

            return new
            {
                app = name,
                changes = plan.Changes,
                warnings = plan.Warnings,
                skipped = plan.Skipped,
                migrationFile,
                backup,
                backendPath = state.BackendPath,
                backendWarnings = backend.Errors,
                frontendPath = state.FrontendPath,
                frontendWarnings = frontend?.Errors,
                version,
                next = "Restart the API (or redeploy with neoapps_deploy_app). Other environments get the change when the new API starts: it applies pending migrations first.",
            };
        }, ct);
    }

    [McpServerTool(Name = "neoapps_migrate_app")]
    [Description("Runs an app's migrations against its database: status (default), up (apply pending), baseline (mark all applied), or down:<version> (roll back to that version). Long-running: returns a job.")]
    public Task<object> MigrateApp(
        [Description("App name.")] string name,
        [Description("status | up | baseline | down:<version>")] string mode = "status",
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        return jobs.StartAndWaitAsync("migrate", state.Name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);
            var r = await migrations.RunAsync(state, mode, job.Log, jct);
            if (!r.Ok) throw new InvalidOperationException(r.Tail());
            if (mode.StartsWith("down", StringComparison.OrdinalIgnoreCase))
                job.Log("Rolled back. Regenerate the app (neoapps_evolve_app with the matching schema.sql) so the code matches the database.");
            return new { app = name, mode, output = r.Tail(60).Split('\n').Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("---")).ToArray() };
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

    // ---------------------------------------------------------------- UI configuration

    [McpServerTool(Name = "neoapps_preview_app")]
    [Description("""
        Plans an app before generating it (and re-plans when requirements change). Saves the schema/ui config into
        workspace/<name>/ and writes preview.html: data-model diagram, screens, who-can-do-what matrix, AI features,
        issues to fix, and what changed since the last saved version. Show it to the user and iterate; when they approve,
        call again with saveVersion=true, then neoapps_create_app. Keep workspace/<name>/requirements.md up to date too.
        """)]
    public object PreviewApp(
        [Description("App name (PascalCase), e.g. SplitBills.")] string name,
        [Description("Full MySQL CREATE TABLE script; omit to reuse workspace/<name>/schema.sql.")] string? sqlSchema = null,
        [Description("ui.config.json content; omit to reuse the saved one.")] string? uiConfig = null,
        [Description("Record this plan as the next approved version (history/vN).")] bool saveVersion = false) =>
        Requirements.Preview(store, name, sqlSchema, uiConfig, saveVersion);

    [McpServerTool(Name = "neoapps_ui_config_guide", ReadOnly = true, Idempotent = true)]
    [Description("Format of ui.config.json: how to set labels, field widgets (select/checkbox/currency/textarea/reference...), list columns, form order, menu groups, icons, dashboard cards, theme and RBAC roles.")]
    public static string UiConfigGuide() => UiConfigTools.Guide;

    [McpServerTool(Name = "neoapps_get_ui_config", ReadOnly = true, Idempotent = true)]
    [Description("Returns an app's current ui.config.json plus a compact summary of its tables and columns, to plan UI changes.")]
    public object GetUiConfig([Description("App name.")] string name)
    {
        var state = store.Require(name);
        return new
        {
            app = state.Name,
            uiConfigFile = store.UiConfigFile(state.Name),
            uiConfig = UiConfigTools.Read(store.UiConfigFile(state.Name)),
            tables = UiConfigTools.SchemaSummary(state.FrontendPath),
            note = "Admins can also change the UI in the running app (Admin > Customize UI); that saved copy overrides this file.",
        };
    }

    [McpServerTool(Name = "neoapps_update_ui_config")]
    [Description("""
        Changes an app's UI: deep-merges the given JSON into ui.config.json (null removes a key; replace=true overwrites the
        whole file), then regenerates the frontend. Redeploy (neoapps_deploy_app) to see it in Docker. Long-running: returns a job.
        Role changes in "roles" apply to newly created databases (neoapps_create_app); for a running app use Admin > Roles,
        or neoapps_evolve_app to add new roles and grant new tables.
        """)]
    public Task<object> UpdateUiConfig(
        [Description("App name.")] string name,
        [Description("JSON object with the changes, e.g. {\"tables\":{\"studies\":{\"fields\":{\"status\":{\"widget\":\"select\",\"options\":[\"startup\",\"enrolling\"]}}}}}")] string changes,
        [Description("Replace the whole config instead of merging.")] bool replace = false,
        CancellationToken ct = default)
    {
        var state = store.Require(name);
        var file = store.UiConfigFile(state.Name);
        File.WriteAllText(file, UiConfigTools.Merge(replace ? null : UiConfigTools.Read(file), changes));
        return jobs.StartAndWaitAsync("frontend", state.Name, async (job, jct) =>
        {
            await EnsureInfraAsync(job, jct);
            var r = await GenerateFrontendCoreAsync(state, state.FrontendTemplate ?? "app", job, jct);
            return new { app = state.Name, uiConfigFile = file, frontendPath = state.FrontendPath, warnings = r.Errors, next = "Rebuild/redeploy to see the change (neoapps_deploy_app)." };
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

    /// <summary>
    /// App-specific backend code: workspace/App/custom-api/**/*.cs is copied into the generated API's Custom/ folder on every
    /// generate (so it survives regeneration), with the template namespace "nkv.MicroService" replaced by the app name.
    /// Background job kinds (JobHandler subclasses) placed there are registered automatically.
    /// </summary>
    private int CopyCustomApi(ProjectState state)
    {
        if (state.BackendPath is null) return 0;
        var source = Path.Combine(store.ProjectDir(state.Name), "custom-api");
        var target = Path.Combine(state.BackendPath, $"{state.Name}.API", "Custom");
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        if (!Directory.Exists(source)) return 0;
        var n = 0;
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, File.ReadAllText(file).Replace("nkv.MicroService", state.Name));
            n++;
        }
        return n;
    }

    /// <summary>The generated API only accepts browser calls from its own UI (Cors:AllowedOrigins), not from any website.</summary>
    private static void AllowUiOrigin(ProjectState state, Job job)
    {
        var file = Path.Combine(state.BackendPath!, $"{state.Name}.API", "appsettings.json");
        if (!File.Exists(file)) return;
        try
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            if (json["Cors"] is not System.Text.Json.Nodes.JsonObject cors) json["Cors"] = cors = new System.Text.Json.Nodes.JsonObject();
            cors["AllowedOrigins"] = $"http://localhost:{state.WebPort},http://127.0.0.1:{state.WebPort}";
            File.WriteAllText(file, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { job.Log("Could not set Cors:AllowedOrigins: " + ex.Message); }
    }

    private async Task<GeneratorResult> GenerateBackendCoreAsync(ProjectState state, bool databaseExists, bool resetDatabase, Job job, CancellationToken ct)
    {
        var p = generator.BaseParameters(state);
        p["DBexists"] = databaseExists ? "YES" : "No";   // YES: keep the database and its data, only read its schema
        p["resetDatabase"] = resetDatabase ? "Yes" : "No";
        p["Backend_technology"] = "dotnet";
        p["Technology_Frontend"] = null;
        p["projectType"] = null;
        p["rabbitMQConn"] = settings.HostRabbitMqUrl;
        p["redisConn"] = settings.HostRedis;
        var backend = await generator.RunAsync(state.Name, "backend", p, job, ct);
        if (backend.OutputPath is null)
            throw new InvalidOperationException("Backend generation failed:\n" + string.Join('\n', backend.Errors));

        state.BackendPath = Path.Combine(backend.OutputPath, "DotNet_Output", "solution", state.Name);
        state.AdminUsername = backend.AdminUsername ?? state.AdminUsername;
        state.AdminPassword = backend.AdminPassword ?? state.AdminPassword;
        store.Save(state);
        AllowUiOrigin(state, job);
        var n = migrations.CopyIntoApi(state);
        if (n > 0) job.Log($"Copied {n} migration(s) into {state.Name}.API/Migrations.");
        var custom = CopyCustomApi(state);
        if (custom > 0) job.Log($"Copied {custom} custom API file(s) into {state.Name}.API/Custom.");

        // The generator compiled the solution before the migrations were copied in; rebuild so the output includes them
        // (and so a broken hand-written migration, or a column named after a C# keyword, fails here, not at deploy).
        if (n > 0 || custom > 0 || databaseExists)
        {
            job.Log("Building the API...");
            var build = await CommandRunner.RunAsync("dotnet", ["build", $"{state.Name}.sln", "-nologo", "-v", "q", "-clp:ErrorsOnly"], state.BackendPath, job.Log, timeout: TimeSpan.FromMinutes(10), ct: ct);
            if (!build.Ok)
                throw new InvalidOperationException((databaseExists ? "The database was migrated, but the regenerated API doesn't compile. " : "The generated API doesn't compile. ") +
                    "A column named after a C# keyword or an error in a hand-written migration is the usual cause. Fix it (for a column already in the " +
                    "database use a rename hint) and run again.\n" + string.Join('\n', build.Output.Split('\n').Where(l => l.Contains("error CS")).Distinct().Take(8)));
        }
        return backend;
    }

    /// <summary>mysqldump before a migration (MySQL can't roll back DDL). Returns the file, or null if mysqldump isn't available.</summary>
    private async Task<string?> BackupAsync(ProjectState state, long version, Job job, CancellationToken ct)
    {
        if (!CommandRunner.Exists("mysqldump")) { job.Log("mysqldump not found: no backup taken before migrating."); return null; }
        var dir = Path.Combine(store.ProjectDir(state.Name), "backups");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{state.DatabaseName}-before-{version}.sql");
        var r = await CommandRunner.RunAsync("mysqldump",
            ["-h", settings.MySqlHost, "-P", settings.MySqlPort.ToString(), "-u", settings.MySqlUser, "--single-transaction", "--routines", "--triggers", $"--result-file={file}", state.DatabaseName],
            store.ProjectDir(state.Name), null, new Dictionary<string, string> { ["MYSQL_PWD"] = settings.MySqlPassword }, TimeSpan.FromMinutes(30), ct);
        if (!r.Ok) { job.Log("Backup failed, continuing without one: " + r.Tail(3)); return null; }
        job.Log($"Backed up {state.DatabaseName} to {file}");
        return file;
    }

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
        if (settings.SkipInfra)
            return; // user runs their own MySQL/Redis/RabbitMQ
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
        _ => throw new McpException("frontendTemplate must be 'app' or 'dnd'."),
    };
}
