# NeoApps.AI Code Generator: instructions for Claude

This repo turns a MySQL schema into an app: a .NET 10 Web API plus a React admin UI. You'll usually be asked to
**generate an app from someone's requirements**, or to **work on the generator itself**.

## Generating an app from requirements

1. **Clarify briefly.** Get the core entities, their key fields and the relations. Make reasonable assumptions and state them.
   Don't over-ask.
2. **Write the schema.** Follow the rules from `./neoapps guidelines` (also in `src/NeoApps.Mcp/Tools/SchemaGuidelines.cs`):
   - every table: `<singular>_id INT AUTO_INCREMENT PRIMARY KEY` plus the audit columns
     `createdBy, modifiedBy VARCHAR(255) NOT NULL`, `createdAt, modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP`,
     `isActive TINYINT(1) NOT NULL DEFAULT '1'`
   - lowercase snake_case names, `FOREIGN KEY ... REFERENCES`, 3-15 tables
   - **never** reuse a built-in table name: `roles`, `users`, `entities`, `permissionmatrix`, `messagequeue`,
     `s3bucket`, `s3bucket_folders`, `dnd_ui_versions`, `project_dnd_ui_versions`, `workflow`, `workflows`,
     `workflows_projects`, `workflow_builds`, `workflow_runs`, `workflow_deployments`, `workflow_triggers`,
     `workflow_trigger_conditions` (use e.g. `members`, `automation_runs`)
   - plain `CREATE TABLE` / `INSERT` only: no `CREATE DATABASE`, `USE`, `DELIMITER`, procedures
   Save it as `workspace/<App>.sql` (git-ignored), or under `samples/` if it should be committed.
3. **Generate:** `./neoapps new <App> --schema workspace/<App>.sql` (App = PascalCase identifier, e.g. `TodoApp`).
   Add `--deploy` to build and run it in Docker, and `--template dnd` for the drag-and-drop designer UI.
4. **Report back:** web URL, Swagger URL, admin username/password (printed by the command and kept in
   `workspace/<App>/project.json`), and where the code is (`1_<ticks>/<App>/DotNet_Output/solution/<App>` and
   `.../ReactTs_Output3`).

### Environments

| Situation | What to do |
|---|---|
| Docker available (user's Mac/PC) | `./neoapps new ... --deploy`. Shared MySQL (3307), Redis and RabbitMQ start automatically. |
| No Docker (Claude Code cloud session, CI) | `sudo ./scripts/sandbox-deps.sh` once, then `export NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=3306` before `./neoapps new ...` (without `--deploy`). Run the app as shown below. |
| Claude Desktop | `./scripts/setup.sh` registers the MCP server. The same operations are exposed as `neoapps_*` tools. |

Running a generated app without Docker:

```bash
cd 1_*/<App>/DotNet_Output/solution/<App>/<App>.API
ASPNETCORE_URLS=http://0.0.0.0:5100 dotnet run            # GET /health, /swagger
cd ../../../../ReactTs_Output3                            # .env already points at the API port
CYPRESS_INSTALL_BINARY=0 npm install --force && npm run build && npx serve -s build -l 3100
```

Login: `POST /v1/api/Token {"username","password"}` → `document.accessToken`. Use it as a Bearer token on `/v1/api/<Table>`.
Rows are owner-scoped (`createdBy` = the logged-in user), so seed rows inserted with another `createdBy` won't show up.

### Verify before you say it works

- `./neoapps new` exits 0 and `backendWarnings` / `frontendWarnings` are empty.
- The generated solution builds: `dotnet build <App>.sln`. The UI builds: `npm run build`.
- `/health` returns ok, login returns a token, and one create + list call succeeds.

## Working on the generator

- `CodeGenerator/`: generator (.NET 10 console app). `Program.cs` is the backend generator and `Reactjs/Functions.cs`
  the frontend one. Both are large legacy files built on string templates, so make surgical edits.
- Templates: `CodeGenerator/DotNetMySQLTemplate/` (`*.txt` snippets plus `solution/nkv.MicroService`, renamed per app) and
  `CodeGenerator/ReactTsTemplate{1..4}/` (1 = drag-and-drop, 3 = admin UI).
- `CodeGenerator/Infrastructure/`: config loading (`--config` JSON or the legacy `PARAMETER` env var), `SchemaReader`
  (always scoped to `DATABASE()`), `MySqlScript` (statement splitter), `ProcessRunner`.
- `src/NeoApps.Mcp/`: MCP server and CLI (`Cli.cs`). Both use the same services; the generator runs as a child process.
- Build: `dotnet build NeoApps.slnx`. **Test any generator change by generating an app and building it**
  (`./neoapps new TestApp --schema samples/todo-app.sql`, then build the generated `.sln` and run `npm run build`).
- The generator reads templates relative to `CodeGenerator/` and writes apps to `../1_<ticks>/`. Never commit
  `1_*/` or `workspace/` (both are git-ignored).
- MySQL must run with `lower_case_table_names=1`: generated SQL mixes the casing of table names.
- Never commit credentials. Generated apps get a random JWT secret, and S3 keys come from env vars.
