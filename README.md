<p align="center">
    <img src="https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/logo.png" alt="NeoApps.AI Logo" width="500">
</p>

# NeoApps.AI Code Generator

Turn a MySQL schema into a working app: a **.NET 10 Web API** (CRUD, relational and reporting endpoints, JWT auth, role-based permissions, Swagger) plus a **React admin UI**, then run it locally in Docker.

## Build an app by asking Claude (easiest)

Nothing to install. Open a new chat in [Claude](https://claude.ai) or a [Claude Code](https://claude.ai/code) session, name this repo in your first message, and describe the app in plain words:

> Use the GitHub repo **Neopric-Inc/NeoApps.AI-CodeGenerator** to build me an app for a dental clinic: patients, appointments, dentists, treatments and invoices. The front desk books appointments, dentists edit treatments, and the owner sees everything. Add an assistant that answers questions from our uploaded clinic policies.

Claude attaches the repo and follows its [instructions](CLAUDE.md). It designs the database, screens and roles (plus the AI assistant if you ask for one), generates and builds the app, tests it, and replies with the web address and admin login.

Tips for a good result:
- **What it tracks:** name the main things and how they relate (a patient has many appointments…).
- **Who does what:** say who uses it and what each kind of user may do; these become roles and permissions.
- **AI:** say if you want questions answered from documents, questions about your data, or AI that creates records from a document.
- **Changes:** after it's built, just ask ("make status a dropdown", "let the front desk edit invoices"). Admins can also adjust screens in the app under **Admin → Customize UI**.

**Not sure exactly what you need yet?** Say so. For example:

> Use the GitHub repo **Neopric-Inc/NeoApps.AI-CodeGenerator**. I want an app to split shared expenses with roommates and friends, like Splitwise. Help me work out the requirements first and show me a preview before you build it.

Claude asks a few short rounds of questions and keeps a plain-language requirements doc. Before generating anything it shows a **one-page preview of the plan**: a diagram of the data, every screen's list and form, who can do what, the AI features, and any issues. Change your mind as often as you like. Each new preview starts with **what changed since the last version**, and new or changed items are highlighted. When you're happy, Claude builds it. Developers can run the same step with `./neoapps preview <App>`.

To run the app on your own computer instead of in Claude's sandbox, use one of the options below. You'll need Docker Desktop.

## Other ways to use it

- **From Claude Desktop:** describe the app in chat. Claude writes the schema, generates the code and deploys it on your machine through the bundled MCP server.
- **From the command line:** run the generator with a JSON config.

▶️ Demo: [manual setup](https://www.youtube.com/watch?v=i2SOFnlYknU) · [tutorial](tutorials.md)

## Prerequisites

| Tool | Notes |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | macOS: `brew install --cask dotnet-sdk` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Runs MySQL, Redis, RabbitMQ and the generated apps |
| Claude Desktop | Only needed for the MCP workflow |

You don't need XAMPP, Python, Node, or a local MySQL/Redis/RabbitMQ install. The shared services run from [`deploy/docker-compose.infra.yml`](deploy/docker-compose.infra.yml).

## Quick start

```bash
git clone https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator.git && cd NeoApps.AI-CodeGenerator
./scripts/setup.sh                    # checks .NET 10 + Docker, builds the MCP server, registers it with Claude Desktop
```

Then either restart Claude Desktop and ask *"Build me a todo app and run it locally"*, or use the CLI:

```bash
./neoapps new TodoApp --schema samples/todo-app.sql --deploy   # generate + run in Docker
./neoapps preview MyApp --schema my.sql --ui my.ui.json      # visual plan + checks before generating
./neoapps help                                                 # all commands
```

No Docker (e.g. a Claude Code cloud session)? Run `sudo ./scripts/sandbox-deps.sh`, then
`export NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=3306`, then run `./neoapps new ...` without `--deploy`. See [CLAUDE.md](CLAUDE.md).

## Option 1: Claude Desktop (MCP)

`./scripts/setup.sh` performs steps 1 and 2 for you. To do them by hand:

### 1. Build the MCP server

```bash
git clone https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator.git
cd NeoApps.AI-CodeGenerator
dotnet publish src/NeoApps.Mcp -c Release -o ~/.neoapps-mcp
```

### 2. Register it with Claude Desktop

Open **Claude Desktop → Settings → Developer → Edit Config** (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS) and add the following. Replace `/Users/you/...` with your clone path:

```json
{
  "mcpServers": {
    "neoapps": {
      "command": "/usr/local/share/dotnet/dotnet",
      "args": ["/Users/you/.neoapps-mcp/neoapps-mcp.dll"],
      "env": {
        "NEOAPPS_HOME": "/Users/you/repos/NeoApps.AI-CodeGenerator",
        "DOTNET_ROOT": "/usr/local/share/dotnet",
        "PATH": "/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin"
      }
    }
  }
}
```

Apps launched from the Dock don't inherit your shell `PATH`. That's why `PATH` is set here: the server runs `dotnet` and `docker` as child processes. On Windows, use `"command": "dotnet"` and Windows paths.

Restart Claude Desktop. **neoapps** should now appear in the tools menu.

### 3. Ask Claude

> Build me a task tracker with projects, tasks and comments, and run it locally.

Claude will:
1. read `neoapps_schema_guidelines` and write the schema,
2. call `neoapps_create_app`. This starts MySQL/Redis/RabbitMQ in Docker and generates the API and UI,
3. call `neoapps_deploy_app`. This builds the images and starts the app,
4. reply with the web URL, the Swagger URL, and the admin login.

### MCP tools

| Tool | What it does |
|---|---|
| `neoapps_schema_guidelines` | Schema rules and an example schema |
| `neoapps_check_environment` | Checks for the .NET SDK, Docker, and the shared containers |
| `neoapps_create_app` | Schema → database + .NET 10 API + React UI (`frontendTemplate`: `app` or `dnd`) |
| `neoapps_generate_frontend` | Regenerates the UI (`app` admin UI or `dnd` drag-and-drop designer) |
| `neoapps_deploy_app` | Builds and runs the app's containers (`includeConsumer` adds the workflow worker) |
| `neoapps_app_status` / `neoapps_app_logs` / `neoapps_stop_app` | Inspect or stop a running app |
| `neoapps_list_apps` / `neoapps_list_sample_schemas` | List your apps / the sample schemas in `GeneratedApps/` |
| `neoapps_start_infrastructure` / `neoapps_stop_infrastructure` | Start or stop the shared MySQL/Redis/RabbitMQ |
| `neoapps_ui_config_guide` / `neoapps_get_ui_config` / `neoapps_update_ui_config` | Design the UI and roles (see below) |
| `neoapps_job_status` | Polls long-running jobs (generation, Docker builds) |

Generation and deployment run as background jobs. A tool call waits about 40 seconds, then returns a job id that Claude polls with `neoapps_job_status`.

### Where things go

| Path | Contents |
|---|---|
| `workspace/<App>/` | `project.json` (ports, paths, login), `schema.sql`, generator configs, `docker-compose.yml` |
| `1_<timestamp>/<App>/DotNet_Output/solution/<App>/` | Generated .NET solution (open `<App>.sln`) |
| `1_<timestamp>/<App>/ReactTs_Output3/` (`ReactTs_Output1` for `dnd`) | Generated React app |

Each app gets its own ports: API from 5100 (`http://localhost:5100/swagger`), web UI from 3100.

### Configuration (environment variables)

| Variable | Default | Purpose |
|---|---|---|
| `NEOAPPS_HOME` | auto-detected | Repository root |
| `NEOAPPS_MYSQL_HOST` / `_PORT` / `_USER` / `_PASSWORD` | `127.0.0.1` / `3307` / `root` / `neoapps` | MySQL used while generating |
| `NEOAPPS_SKIP_INFRA` | `false` | `true` = don't start the Docker infra (bring your own MySQL/Redis/RabbitMQ) |
| `NEOAPPS_RABBITMQ_URL` / `NEOAPPS_REDIS` | `amqp://neoapps:neoapps@localhost:5672/` / `localhost:6379` | Written into the generated `appsettings.json` for `dotnet run` |
| `NEOAPPS_DOCKER_*` | `mysql`, `rabbitmq`, `redis` | Hostnames apps use inside the `neoapps` Docker network |
| `NEOAPPS_FIRST_API_PORT` / `NEOAPPS_FIRST_WEB_PORT` | `5100` / `3100` | Port ranges for new apps |
| `ANTHROPIC_API_KEY` | (unset) | Enables the AI assistant in generated APIs (read at runtime, never written to disk) |

## Option 2: Command line

```bash
# 1. Shared services (MySQL on localhost:3307, Redis 6379, RabbitMQ 5672 / UI 15672)
docker compose -f deploy/docker-compose.infra.yml up -d --wait

# 2. Backend: database + .NET 10 API
cd CodeGenerator
dotnet run -- --config ../samples/generator.backend.json

# 3. Frontend, pointed at the API URL
dotnet run -- --config ../samples/generator.frontend.json
```

The last output line is machine-readable:

```text
NEOAPPS_RESULT {"is_successful":1,"output_path":".../1_<ticks>/TaskTracker","link":".../zip/...zip","admin_username":"...","admin_password":"...","error_log":[]}
```

Run the generated API:

```bash
cd ../1_*/TaskTracker/DotNet_Output/solution/TaskTracker/TaskTracker.API
dotnet run            # Swagger: https://localhost:5001/swagger
```

### Generator config keys

| Key | Example | Notes |
|---|---|---|
| `projectName` | `TaskTracker` | Becomes the C# namespace |
| `databaseName` | `task_tracker_db` | Created if missing |
| `script` | `schema.sql` | Local path (relative to the config file), `file://` or `http(s)://` |
| `DBexists` | `No` | `No` runs `script`; `Yes` uses the existing database |
| `resetDatabase` | `Yes` | Drop and recreate the database first |
| `server` / `port` / `username` / `password` | `127.0.0.1` / `3307` / `root` / `neoapps` | MySQL connection |
| `Backend_technology` | `dotnet` | Set to generate the API |
| `Technology_Frontend` | `reactts` | Set to generate the UI |
| `projectType` | `null` / `dnd` | Admin UI or drag-and-drop designer |
| `rabbitMQConn` | `amqp://...` (backend) / `http://localhost:5100/v1/api/` (frontend) | For a frontend run, this key carries the **API base URL** |
| `redisConn` | `localhost:6379` | Redis for the generated API |
| `swgurl`, `noderedurl`, `apiflowurl`, `fronttemplateurl` | | Optional integrations |

The legacy `PARAMETER` environment variable (`{key:value,...}`) is still accepted.

Optional settings: set `NEOAPPS_SKIP_BUILD=true` to skip the compile check of the generated solution. `DigitalOceanSpacesConfig` in `appsettings.json` (or `NEOAPPS_DigitalOceanSpacesConfig__*` env vars) uploads the zip to S3-compatible storage. Upload is off unless real keys are set.

## Designing the UI and access

Every generated app gets a schema-driven admin UI: grouped menu, list pages with search and paging, forms with real
widgets (selects, checkboxes, currency, dates, related-record pickers), record pages with related tabs, a dashboard,
and **Admin > Members / Roles / Customize UI**.

- **`ui.config.json`** sets labels, widgets, select options, status colours, list columns, form order, menu groups,
  icons, dashboard cards, theme and **roles**. Without it, the UI infers all of this from the schema.
  - CLI: `./neoapps new MyApp --schema my.sql --ui my.ui.json`, `./neoapps ui MyApp` (show), `./neoapps ui MyApp --set changes.json` (merge and regenerate)
  - MCP: `neoapps_ui_config_guide`, `neoapps_get_ui_config`, `neoapps_update_ui_config`, or just ask Claude: "make status a dropdown with these options", "group the menu by …"
  - In the app: **Admin > Customize UI** edits and previews it live. The saved copy applies to everyone in that workspace and overrides the file.
  - Example: [`samples/clinical-research-site.ui.json`](samples/clinical-research-site.ui.json)
  - Dashboard cards show a row count by default, or a total: `{ "table": "expenses", "label": "Total spent", "sum": "amount", "where": { "status": "open" }, "format": "currency", "currencyCode": "INR" }`. Currency fields take `"currencyCode"` (default USD).
- **Custom screens.** When the schema-driven UI isn't enough (a personal "my balances" page, a calculator, a one-click
  action), put React code in `workspace/<App>/custom/index.tsx` next to the app's `ui.config.json`. The generator copies
  that folder into the frontend's `src/custom/` on every `new` / `ui` run, so it survives regeneration. Export
  `extensions` with any of `routes` (extra pages), `nav` (sidebar items), `dashboardTop` (panel above the cards) and
  `recordActions` (buttons on record pages); see `CodeGenerator/ReactUiKit/src/neoapps/extensions.ts`. Example:
  [`samples/splitshare/`](samples/splitshare), a shared-expenses app with a My balances page, settle-up suggestions,
  a "Split now" button and admin "Create login".
- **RBAC**
  - Roles grant view/create/edit/delete per table. Declare them in `ui.config.json` (`"roles": {"Coordinator": {"*": "r", "documents": "cru"}}`, defaulting to Member + Viewer), or manage them in **Admin > Roles**.
  - **Admin > Members** adds users and assigns roles. The API enforces every permission, and the UI hides menus and buttons you can't use.
  - Passwords are stored as BCrypt hashes.

## AI assistant (Claude)

Add an `ai` section to `ui.config.json` and run the API with `ANTHROPIC_API_KEY` set. The app then gets:

- **Assistant page.** Answers questions from uploaded documents, with `[S1]` citations that open the file and page, and from the app's own records ("ask your data"). It says plainly when the sources don't contain the answer, keeps a conversation history per user, and is limited to the tables each user may read.
- **Files panel** on the record pages of `documentTables`. Upload PDF, DOCX, TXT, MD, CSV or JSON. Text is extracted (PDF tables keep their column layout) and indexed in MySQL full-text search.
- **Workflows.** Buttons that have Claude read a record's documents (or external data such as ClinicalTrials.gov) and propose new records across several tables. The user reviews the proposal, unticks rows if needed, and applies it in one transaction.
- **Ask AI** on every record page, which opens the assistant focused on that record.

The key is read only from the environment, so it never touches generated files. `--deploy` passes it into the
container and keeps uploads in a Docker volume. See the `ai` section of
[`samples/clinical-research-site.ui.json`](samples/clinical-research-site.ui.json): protocol Q&A, a Schedule of
Assessments → visits, assessments and source worksheets workflow, and a ClinicalTrials.gov trial finder.

## Writing a schema

Run `neoapps_schema_guidelines`, or follow these rules:

- Every table has an `INT AUTO_INCREMENT PRIMARY KEY` plus the audit columns `createdBy`, `modifiedBy`, `createdAt`, `modifiedAt`, `isActive`.
- Use lowercase snake_case names and declare relations with `FOREIGN KEY`. Table order doesn't matter.
- Don't create `Roles`, `Users`, `Entities` or `PermissionMatrix`: the generator adds them for login and RBAC.
- Records are owner-scoped: the API returns rows whose `createdBy` matches the logged-in user.

Examples live in [`GeneratedApps/`](GeneratedApps). Some older samples define their own `users` table and need it renamed (e.g. `app_users`).

## Repository layout

```
CodeGenerator/            generator (.NET 10 console app)
  DotNetMySQLTemplate/    backend templates (generated solution targets net10.0)
  ReactTsTemplate1..4/    frontend templates (1 = drag-and-drop, 3 = admin UI)
  Infrastructure/         config loading, schema reader, SQL script runner
src/NeoApps.Mcp/          MCP server for Claude Desktop
deploy/                   docker-compose for shared local services
samples/                  example generator configs
GeneratedApps/            example schemas
NeoApps.slnx              solution (generator + MCP server)
```

## Troubleshooting

- **The neoapps tools don't show up in Claude Desktop:** check `~/Library/Logs/Claude/mcp-server-neoapps.log`, and make sure `command` and `NEOAPPS_HOME` are absolute paths.
- **"Docker is not running":** start Docker Desktop, then run `neoapps_check_environment`.
- **Port 3307/5672/6379 is already in use:** stop the other service, or change the ports in `deploy/docker-compose.infra.yml` and `NEOAPPS_MYSQL_PORT`.
- **MySQL table names:** the bundled MySQL runs with `lower_case_table_names=1`, matching the Windows/macOS MySQL the templates were written against. If you bring your own MySQL on Linux, configure it the same way.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
