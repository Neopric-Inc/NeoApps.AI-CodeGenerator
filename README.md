<p align="center">
    <img src="https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/logo.png" alt="NeoApps.AI Logo" width="500">
</p>

# NeoApps.AI Code Generator

Describe an app, or hand over a MySQL schema, and get a working app: a **.NET 10 Web API** (CRUD, relational and reporting endpoints, JWT auth, role-based permissions, Swagger) plus a **React admin UI** and an optional **Claude-powered AI assistant**, running locally in Docker.

Use it three ways:

| You are | Use |
|---|---|
| Anyone with a Claude account | [Ask Claude](#ask-claude-nothing-to-install): nothing to install |
| A developer | [`neoapps` command line](#command-line) |
| A coding agent (Claude Code, Claude Desktop, Cursor, any MCP client) | [MCP server](#coding-agents-mcp) |

## Install

```bash
curl -fsSL https://raw.githubusercontent.com/Neopric-Inc/NeoApps.AI-CodeGenerator/main/install.sh | bash
```

The installer:
1. checks for git, the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (macOS: `brew install --cask dotnet-sdk`) and [Docker Desktop](https://www.docker.com/products/docker-desktop/),
2. downloads the generator to `~/.neoapps`,
3. builds it and puts the `neoapps` command on your PATH (`~/.local/bin`),
4. connects the MCP server to **Claude Code** and **Claude Desktop** if they're installed, and prints the config for other MCP clients.

Re-run it to update. Updates are fast-forward only: if you've changed or committed anything in `~/.neoapps`, it leaves your work alone and tells you.

| Option | Default | Purpose |
|---|---|---|
| `--dir <path>` / `NEOAPPS_DIR` | `~/.neoapps` | Where the generator lives |
| `--bin <path>` / `NEOAPPS_BIN` | `~/.local/bin` | Where the `neoapps` command goes |
| `--ref <branch>` / `NEOAPPS_REF` | `main` | Branch or tag to install |
| `--no-mcp` | | Don't register with Claude |
| `--no-update` | | Don't pull an existing install |
| `--infra` | | Also start the shared MySQL/Redis/RabbitMQ containers now |

Pass options through curl with `| bash -s -- --no-mcp`. **Already cloned the repo?** Run `./scripts/setup.sh` inside it: same steps, using your clone. **Windows:** run it in WSL.

You don't need a local MySQL, Redis, RabbitMQ, Node or Python: the shared services run from [`deploy/docker-compose.infra.yml`](deploy/docker-compose.infra.yml).

## Ask Claude (nothing to install)

Open a new chat in [Claude](https://claude.ai) or a [Claude Code](https://claude.ai/code) session, name this repo in your first message, and describe the app in plain words:

> Use the GitHub repo **Neopric-Inc/NeoApps.AI-CodeGenerator** to build me an app for a dental clinic: patients, appointments, dentists, treatments and invoices. The front desk books appointments, dentists edit treatments, and the owner sees everything. Add an assistant that answers questions from our uploaded clinic policies.

Claude attaches the repo and follows its [instructions](CLAUDE.md). It designs the database, screens and roles (plus the AI assistant if you ask for one), generates and builds the app, tests it, and replies with the login.

Tips for a good result:
- **What it tracks:** name the main things and how they relate (a patient has many appointments…).
- **Who does what:** say who uses it and what each kind of user may do; these become roles and permissions.
- **AI:** say if you want questions answered from documents, questions about your data, or AI that creates records from a document.
- **Changes:** after it's built, just ask ("make status a dropdown", "let the front desk edit invoices"). Admins can also adjust screens in the app under **Admin → Customize UI**.

**Not sure exactly what you need yet?** Say so:

> Use the GitHub repo **Neopric-Inc/NeoApps.AI-CodeGenerator**. I want an app to split shared expenses with roommates and friends, like Splitwise. Help me work out the requirements first and show me a preview before you build it.

Claude asks a few short rounds of questions and keeps a plain-language requirements doc. Before generating anything it shows a **one-page preview of the plan**: a diagram of the data, every screen's list and form, who can do what, the AI features, and any issues. Each new preview starts with **what changed since the last version**. When you're happy, Claude builds it.

An app built in Claude's cloud sandbox can't be opened from your browser. To run it on your own computer, [install](#install) and use the CLI or a local agent.

## Command line

```bash
neoapps new TodoApp --schema ~/.neoapps/samples/todo-app.sql --deploy   # generate + run in Docker
neoapps preview MyApp --schema my.sql --ui my.ui.json                   # visual plan + checks before generating
neoapps help                                                            # all commands
```

The app opens at `http://localhost:3100` and Swagger at `http://localhost:5100/swagger`. The command prints the admin login. Each new app gets the next free ports.

| Command | What it does |
|---|---|
| `new <App> --schema f.sql [--ui f.json] [--deploy]` | Database + .NET 10 API + React UI (`--template dnd` for the drag-and-drop designer, `--no-frontend`, `--keep-db`, `--consumer`) |
| `preview <App> [--schema] [--ui] [--save]` | Writes `workspace/<App>/preview.html`: data model, screens, roles, AI, issues, changes since the last saved version |
| `evolve <App> [--name "…"] [--plan]` | Applies a requirements change to a running app, keeping its data ([migrations](#changing-a-running-app-migrations)) |
| `migrate <App> [status\|up\|baseline\|down:<v>]` | Runs the app's migrations |
| `ui <App> [--set changes.json]` | Shows the UI config, or merges changes and regenerates the UI |
| `frontend <App>` · `deploy <App>` | Regenerates the UI · builds and runs the app in Docker |
| `test <App>` | Data-rule tests (tenant isolation, row scopes, append-only tables, lifecycles) against the running API |
| `status` · `logs` · `stop <App>` · `list` | Inspect, stop or list apps |
| `doctor` · `guidelines` · `infra [down]` | Check .NET/Docker · schema rules · start/stop shared services |

App names are PascalCase (`TodoApp`); the name becomes the C# namespace. Schema and UI paths are relative to your current folder.

**No Docker** (Claude Code cloud sessions, CI): run `sudo ./scripts/sandbox-deps.sh` once, then `export NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=3306` and run `neoapps new ...` without `--deploy`. [CLAUDE.md](CLAUDE.md) shows how to start the generated API and UI by hand.

## Coding agents (MCP)

The installer registers the `neoapps` MCP server with Claude Code and Claude Desktop (restart Desktop afterwards). Check Claude Code with `claude mcp list`. For Cursor, Windsurf, VS Code or any other MCP client, paste the snippet the installer prints into that client's MCP config:

```json
{ "mcpServers": { "neoapps": {
    "command": "/path/to/dotnet",
    "args": ["/Users/you/.neoapps/src/NeoApps.Mcp/bin/Release/net10.0/neoapps-mcp.dll"],
    "env": { "NEOAPPS_HOME": "/Users/you/.neoapps", "DOTNET_ROOT": "/path/to/dotnet-dir", "PATH": "/path/to/dotnet-dir:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin" } } } }
```

Use absolute paths. `PATH` is set because apps launched from the Dock don't inherit your shell's PATH, and the server runs `dotnet` and `docker`.

Then ask:

> Build me a task tracker with projects, tasks and comments, and run it locally.

The agent reads the schema guidelines, writes the schema and UI config, calls `neoapps_create_app` and `neoapps_deploy_app`, and replies with the web URL, Swagger URL and admin login. Agents working inside a clone also get the full workflow from [CLAUDE.md](CLAUDE.md) and [AGENTS.md](AGENTS.md).

| Tool | What it does |
|---|---|
| `neoapps_schema_guidelines` | Schema rules and an example schema |
| `neoapps_check_environment` | Checks for the .NET SDK, Docker, and the shared containers |
| `neoapps_create_app` | Schema → database + .NET 10 API + React UI (`frontendTemplate`: `app` or `dnd`) |
| `neoapps_preview_app` | One-page visual plan with issues, before generating |
| `neoapps_evolve_app` / `neoapps_migrate_app` | Apply a requirements change keeping the data / migration status, apply, roll back |
| `neoapps_generate_frontend` | Regenerates the UI |
| `neoapps_deploy_app` | Builds and runs the app's containers |
| `neoapps_app_status` / `neoapps_app_logs` / `neoapps_stop_app` | Inspect or stop a running app |
| `neoapps_list_apps` / `neoapps_list_sample_schemas` | List your apps / sample schemas |
| `neoapps_start_infrastructure` / `neoapps_stop_infrastructure` | Start or stop the shared MySQL/Redis/RabbitMQ |
| `neoapps_ui_config_guide` / `neoapps_get_ui_config` / `neoapps_update_ui_config` | Design the UI, roles and AI |
| `neoapps_job_status` | Polls long-running jobs |

Generation and deployment run as background jobs: a call waits about 40 seconds, then returns a job id the agent polls with `neoapps_job_status`.

## Designing the UI and access

Every generated app gets a schema-driven admin UI: grouped menu, list pages with search and paging, forms with real
widgets (selects, checkboxes, currency, dates, related-record pickers), record pages with related tabs, a dashboard,
and **Admin > Members / Roles / Customize UI**.

- **`ui.config.json`** sets labels, widgets, select options, status colours, list columns, form order, menu groups,
  icons, dashboard cards, theme and **roles**. Without it, the UI infers all of this from the schema.
  - CLI: `neoapps new MyApp --schema my.sql --ui my.ui.json`, `neoapps ui MyApp` (show), `neoapps ui MyApp --set changes.json` (merge and regenerate)
  - MCP: `neoapps_ui_config_guide`, `neoapps_get_ui_config`, `neoapps_update_ui_config`, or just ask: "make status a dropdown with these options", "group the menu by …"
  - In the app: **Admin > Customize UI** edits and previews it live. The saved copy applies to everyone in that workspace and overrides the file.
  - Example: [`samples/clinical-research-site.ui.json`](samples/clinical-research-site.ui.json)
  - Dashboard cards show a row count by default, or a total: `{ "table": "expenses", "label": "Total spent", "sum": "amount", "where": { "status": "open" }, "format": "currency", "currencyCode": "INR" }`. Currency fields take `"currencyCode"` (default USD).
- **Data rules.** Multi-tenancy, customer portals (customers see only their rows), status lifecycles and approvals, field
  validation, automatic numbering (`INV-2026-00001`), append-only tables, audit trails, outbox events, SLAs and plan
  limits are declared in `ui.config.json` (`tenancy`, `rowScopes`, `lifecycles`, `validations`, `numbering`,
  `immutable`, `audit`, `outbox`, `jobs`, `limits`). The generated API enforces them on every query; `neoapps test <App>` checks them.
- **Custom screens.** When the schema-driven UI isn't enough (a personal "my balances" page, a calculator, a one-click
  action), put React code in `workspace/<App>/custom/index.tsx`. The generator copies it into the frontend's
  `src/custom/` on every `new` / `ui` run, so it survives regeneration. Export `extensions` with any of `routes`,
  `nav`, `dashboardTop` and `recordActions`; see `CodeGenerator/ReactUiKit/src/neoapps/extensions.ts` and
  [`samples/splitshare/`](samples/splitshare).
- **RBAC**
  - Roles grant view/create/edit/delete per table. Declare them in `ui.config.json` (`"roles": {"Coordinator": {"*": "r", "documents": "cru"}}`, defaulting to Member + Viewer), or manage them in **Admin > Roles**.
  - **Admin > Members** adds users and assigns roles. The API enforces every permission, and the UI hides menus and buttons you can't use.
  - Passwords are stored as BCrypt hashes. Sign-in has lockout, per-IP rate limits and password rules.

## AI assistant (Claude)

Add an `ai` section to `ui.config.json` and run the API with `ANTHROPIC_API_KEY` set. The app then gets:

- **Assistant page.** Answers questions from uploaded documents, with `[S1]` citations that open the file and page, and from the app's own records ("ask your data"). It says plainly when the sources don't contain the answer, keeps a conversation history per user, and is limited to the tables each user may read.
- **Files panel** on the record pages of `documentTables`. Upload PDF, DOCX, TXT, MD, CSV or JSON. Text is extracted (PDF tables keep their column layout) and indexed in MySQL full-text search.
- **Workflows.** Buttons that have Claude read a record's documents (or external data such as ClinicalTrials.gov) and propose new records across several tables. They run as [background jobs](#background-jobs): the dialog shows each step live, the user can close it and come back, and the job pauses with the proposal. The user reviews it, unticks rows if needed, and approves; the rows are created in one transaction and stamped `<user> via AI`.
- **Ask AI** on every record page, which opens the assistant focused on that record.

The key is read only from the environment, so it never touches generated files. Keep it in a private file and load it
before running: `set -a; . ~/secrets/anthropic.env; set +a`. `--deploy` passes it into the container and keeps uploads
in a Docker volume. Without a key the app works normally and the assistant reports itself as not configured. See the
`ai` section of [`samples/clinical-research-site.ui.json`](samples/clinical-research-site.ui.json): protocol Q&A, a
Schedule of Assessments → visits, assessments and source worksheets workflow, and a ClinicalTrials.gov trial finder.

## Background jobs

Long-running work runs on a worker instead of inside a web request, so it doesn't time out and the user doesn't wait
on a spinner. Every generated app includes it; AI workflows use it out of the box.

```
UI ──POST /Jobs──▶ API ──job id──▶ RabbitMQ ──▶ worker ── runs the job as the user who started it
 ▲                  │                               │
 └── live feed ◀────┴──── MySQL (record) + Redis pub/sub (instant updates) ◀── progress, pause, result
```

- **Live feed.** `GET /v1/api/Jobs/{id}/events?after=<last id>` returns new steps at once, or waits up to 20 s for the
  next one (Redis pub/sub wakes it). The UI shows it as a timeline with a progress bar.
- **Pause for approval, then resume.** A job can stop with a proposal (`awaiting_approval`). `POST /Jobs/{id}/approve`
  with the (possibly edited) decision puts it back on the queue, and a worker continues where it left off.
  `/reject` and `/cancel` stop it. By default the person who started it or an administrator can approve.
- **Safe by default.** Each job runs as its user: every query is scoped exactly like their own request (workspace,
  tenant, row scopes), permissions are re-checked, and a job whose user was deactivated stops. A job interrupted by a
  restart or crash is picked up again by another worker (up to `Jobs:MaxAttempts`).
- **Background jobs page.** Users see their jobs (administrators: everyone's), reopen running ones, and approve
  waiting ones. It's in the menu when AI is enabled, or with `"jobs": {"menu": true}` in `ui.config.json`.

**Your own job types.** Put a class deriving from `JobHandler` in `workspace/<App>/custom-api/`. It's copied into
`<App>.API/Custom/` on every generate (write the namespace as `nkv.MicroService`; it's replaced with the app name) and
registered automatically:

```csharp
public sealed class MonthlyInvoicesJob : JobHandler
{
    public override string Kind => "billing.monthly-invoices";
    public override string Validate(JobUser user, JsonObject input) => "Create this month's invoices"; // runs in the request: check permissions here
    public override async Task RunAsync(JobContext ctx)
    {
        ctx.Progress("Reading subscriptions", 10);
        // ... query with DataGuard.Apply(cmd): scoped to ctx.User ...
        ctx.AwaitApproval(proposal, "12 invoices ready. Approve to create them.");
    }
    public override Task ResumeAsync(JobContext ctx, JsonObject decision) { /* create them */ ctx.Complete(result, "Created 12 invoices."); return Task.CompletedTask; }
}
```

Start it with `POST /v1/api/Jobs {"kind": "billing.monthly-invoices", "input": {...}}`, or from a custom screen with
`startJob`, `useJobFeed` and `<JobFeed/>` from `src/neoapps/jobs/`. Full example:
[`samples/custom-api-example/ActivateAccountsJob.cs`](samples/custom-api-example/ActivateAccountsJob.cs).

**Evidence files.** `ctx.AttachFile("page.png", "image/png", bytes, "Screenshot")` (or a path, e.g. a recorded video)
keeps a file with the job: screenshots and videos show inline in the live feed and under *Evidence* on the job, and
`GET /Jobs/{id}/files/{name}` downloads them with the job's visibility rules. They're stored under `Jobs:FilesPath`
(default `App_Data/job-files`, the persisted volume with `--deploy`). `ctx.TempDir` is scratch space deleted after each run.

**NuGet packages and browsers.** `workspace/<App>/custom-api/packages.json` adds packages to the API project, and
browsers (with their system libraries) to its Docker image:

```json
{ "packages": { "Microsoft.Playwright": "1.63.0" }, "browsers": ["chromium"] }
```

`browsers` defaults to Chromium when `Microsoft.Playwright` is listed; outside Docker run
`dotnet <App>.API.dll --install-browsers chromium` once. Example browser job:
[`samples/custom-api-browser/`](samples/custom-api-browser) opens a page, records a video, takes a screenshot and reports
console errors and failed requests. Only hosts in `Browser:AllowedHosts` may be opened (default `localhost,127.0.0.1`),
because the server's browser can reach whatever the server can.

**Settings** (`appsettings.json` or env vars): `Jobs__Worker` (default `true`; to scale, run extra instances of the API
image as workers and set it to `false` on the web-facing ones), `Jobs__Concurrency` (jobs per instance, 2),
`Jobs__TimeoutMinutes` (30), `Jobs__MaxAttempts` (3).

## Changing a running app (migrations)

Requirements change after an app has data in it. `evolve` applies the change without recreating the database:

```bash
# edit workspace/<App>/schema.sql and ui.config.json (or pass new files), then:
neoapps evolve ConstructOS --name "add equipment tracking" --plan    # show the plan and the migration code
neoapps evolve ConstructOS --name "add equipment tracking"           # apply it
neoapps migrate ConstructOS                                          # status: applied / pending
```

What `evolve` does:

1. Compares the **live database** with the new `schema.sql` and writes the difference as a versioned
   [FluentMigrator](https://fluentmigrator.github.io/) class with `Up()` and `Down()` in
   `workspace/<App>/migrations/M<timestamp>_<Name>.cs`. You can review and edit it, or write migrations by hand
   (data fixes, backfills); `evolve` applies pending hand-written ones first.
2. Backs up the database (`mysqldump` into `workspace/<App>/backups/`), then applies the migration. MySQL can't roll
   back DDL, so a failed migration is moved to `migrations/failed/` and the backup path is reported.
3. Keeps access and UI in step: new tables are added to `entities`, granted to administrators, to each role's template
   from `ui.config.json` `roles`, and to every existing member of those roles; roles that are new in the requirements are
   created (existing roles keep the permissions admins set in the app); new screens are added to menus saved from
   **Admin > Customize UI**.
4. Regenerates the API and UI on the kept data, checks that the API builds, and saves the plan as the next version
   (`history/vN`). Restart a locally running API afterwards: its files were rewritten.

Every generated API applies pending migrations **on startup** before serving traffic (one instance at a time, using a
MySQL lock), so deploying the new version moves staging and production databases forward in place, with their data.
If a migration fails, the API doesn't start and the previous version keeps running. Turn it off with
`Migrations__RunOnStartup=false`, and run `dotnet <App>.API.dll --migrate` (or `--migrations-status`,
`--migrate-down=<version>`) from your deploy pipeline instead.

Rules that keep it safe:

- **Additive by default.** Dropping tables or columns is skipped unless you pass `--allow-destructive`.
- **Renames need a hint**, since a diff can't tell a rename from a drop plus an add:
  `--renames renames.json` (or `workspace/<App>/migrate.json`) with `{"old_table": "new_table", "table.old_column": "new_column"}`.
- **Primary key changes** aren't automated; write that migration by hand.
- `neoapps new` still builds a database from scratch. If the app already has migrations, it records them as applied
  (baseline), because the full `schema.sql` already contains their changes.

## Writing a schema

Run `neoapps guidelines` (MCP: `neoapps_schema_guidelines`) for the full rules. In short:

- Every table has `<singular>_id INT AUTO_INCREMENT PRIMARY KEY` plus the audit columns `createdBy`, `modifiedBy` (`VARCHAR(255) NOT NULL`), `createdAt`, `modifiedAt` (`DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP`) and `isActive` (`TINYINT(1) NOT NULL DEFAULT '1'`).
- Lowercase snake_case names, relations as `FOREIGN KEY ... REFERENCES`, 3–15 tables. Plain `CREATE TABLE` / `INSERT` only.
- Keep status/type columns as `VARCHAR` and give them options in `ui.config.json`.
- Don't reuse built-in table names (`roles`, `users`, `entities`, `permissionmatrix`, `workflows`, `ai_files`, `background_jobs`, …; the guidelines list them all). Use e.g. `members` instead of `users`.
- Records are owner-scoped: the API returns rows whose `createdBy` matches the workspace owner.

## Samples

| Sample | What it shows |
|---|---|
| [`samples/todo-app.sql`](samples/todo-app.sql) | Smallest possible app |
| [`samples/clinical-research-site.*`](samples/clinical-research-site.ui.json) | Roles, document Q&A, AI workflows, ClinicalTrials.gov search |
| [`samples/splitshare/`](samples/splitshare) | Custom screens (My balances, settle-up, Split now) |
| [`samples/constructos/`](samples/constructos) | 41 tables, seven roles, nine AI workflows, and a v1 → v2 `evolve` you can replay |
| [`samples/expertos.*`](samples/expertos.ui.json) | 135 tables using every data-rule section |
| [`samples/custom-api-example/`](samples/custom-api-example) | A custom background job: progress, approval, resume |
| [`samples/custom-api-browser/`](samples/custom-api-browser) | A browser job (Playwright): screenshot, video, console and network errors |

Run one: `neoapps new ClinicalSite --schema ~/.neoapps/samples/clinical-research-site.sql --ui ~/.neoapps/samples/clinical-research-site.ui.json --deploy`.

## Reference

### Where things go

Paths are inside the generator folder (`~/.neoapps` when installed).

| Path | Contents |
|---|---|
| `workspace/<App>/` | `project.json` (ports, paths, login), `schema.sql`, `ui.config.json`, `preview.html`, `history/`, `docker-compose.yml` |
| `workspace/<App>/migrations/` | Versioned C# migrations (copied into `<App>.API/Migrations/` on every generate); `backups/` holds the dump taken before each one |
| `1_<timestamp>/<App>/DotNet_Output/solution/<App>/` | Generated .NET solution (open `<App>.sln`) |
| `1_<timestamp>/<App>/ReactTs_Output3/` (`ReactTs_Output1` for `dnd`) | Generated React app |

### Environment variables

| Variable | Default | Purpose |
|---|---|---|
| `NEOAPPS_HOME` | the install folder | Generator root |
| `NEOAPPS_MYSQL_HOST` / `_PORT` / `_USER` / `_PASSWORD` | `127.0.0.1` / `3307` / `root` / `neoapps` | MySQL used while generating |
| `NEOAPPS_SKIP_INFRA` | `false` | `true` = don't start the Docker infra (bring your own MySQL/Redis/RabbitMQ) |
| `NEOAPPS_RABBITMQ_URL` / `NEOAPPS_REDIS` | `amqp://neoapps:neoapps@localhost:5672/` / `localhost:6379` | Written into the generated `appsettings.json` for `dotnet run` |
| `NEOAPPS_DOCKER_*` | `mysql`, `rabbitmq`, `redis` | Hostnames apps use inside the `neoapps` Docker network |
| `NEOAPPS_FIRST_API_PORT` / `NEOAPPS_FIRST_WEB_PORT` | `5100` / `3100` | Port ranges for new apps |
| `NEOAPPS_SKIP_BUILD` | `false` | Skip the compile check of the generated solution |
| `ANTHROPIC_API_KEY` | (unset) | Enables the AI assistant in generated APIs (read at runtime, never written to disk) |
| `Jobs__Worker` / `Jobs__Concurrency` / `Jobs__TimeoutMinutes` / `Jobs__MaxAttempts` | `true` / `2` / `30` / `3` | [Background jobs](#background-jobs) in generated APIs |

Generated APIs read production settings from `appsettings.json` or env vars (`Cors__AllowedOrigins`, `Swagger__Enabled`, `Api__MaxPageSize`, `Auth__*`); see [CLAUDE.md](CLAUDE.md).

### Repository layout

```
install.sh                one-line installer (CLI on PATH + MCP registration)
neoapps                   CLI entry point
CodeGenerator/            generator (.NET 10 console app)
  DotNetMySQLTemplate/    backend templates (generated solution targets net10.0)
  ReactTsTemplate1..4/    frontend templates (1 = drag-and-drop, 3 = admin UI)
  ReactUiKit/             schema-driven admin UI copied into every app
  Infrastructure/         config loading, schema reader, SQL script runner
src/NeoApps.Mcp/          CLI and MCP server
src/NeoApps.Migrator/     runs an app's migrations (used by evolve/migrate)
deploy/                   docker-compose for the shared local services
scripts/                  setup for clones, no-Docker deps, test suites (see scripts/README.md)
samples/                  example schemas and UI configs
NeoApps.slnx              solution (generator + CLI/MCP server + migrator)
```

## Troubleshooting

- **`neoapps: command not found`:** add `~/.local/bin` to your PATH (the installer prints the line), then open a new terminal.
- **The neoapps tools don't show up:** Claude Code: `claude mcp list`. Claude Desktop: restart it, then check `~/Library/Logs/Claude/mcp-server-neoapps.log`; `command` and `NEOAPPS_HOME` must be absolute paths. Re-running the installer re-registers both.
- **"Docker is not running":** start Docker Desktop, then run `neoapps doctor`.
- **Port 3307/5672/6379 is already in use:** stop the other service, or change the ports in `deploy/docker-compose.infra.yml` and `NEOAPPS_MYSQL_PORT`.
- **MySQL table names:** the bundled MySQL runs with `lower_case_table_names=1`. If you bring your own MySQL on Linux, configure it the same way.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). After changing the generator, generate an app and build it (`neoapps new TestApp --schema samples/todo-app.sql`, then build the `.sln` and run `npm run build`). The drag-and-drop designer has its own [tutorial](tutorials.md).
