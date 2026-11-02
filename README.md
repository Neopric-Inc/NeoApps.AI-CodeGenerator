<p align="center">
    <img src="https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/logo.png" alt="NeoApps.AI Logo" width="500">
</p>

# NeoApps.AI Code Generator

Turn a MySQL schema into a working app: a **.NET 10 Web API** (CRUD, relational and reporting endpoints, JWT auth, role-based permissions, Swagger) plus a **React admin UI**, then run it locally in Docker.

Use it in one of two ways:

- **From Claude Desktop (recommended):** describe the app in chat. Claude writes the schema, generates the code and deploys it on your machine through the bundled MCP server.
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
