# NeoApps.AI Code Generator: instructions for Claude

This repo turns a MySQL schema into an app: a .NET 10 Web API plus a React admin UI. You'll usually be asked to
**generate an app from someone's requirements**, or to **work on the generator itself**.

## Generating an app from requirements

Work in the app's planning folder `workspace/<App>/` (git-ignored): `requirements.md`, `schema.sql`, `ui.config.json`.

1. **Collect requirements with the user.** Users often don't know exactly what they want yet, so help them shape it.
   Ask in short rounds (2-4 questions each, with multiple-choice options where the tool exists), starting from what they
   already said: the goal and who uses it; the things to track and their key details; how those things relate; who may
   do what; the screens and dashboard numbers they care about; and whether they want AI (questions over documents,
   questions over their data, or AI that creates records). Suggest sensible defaults, and don't ask about what you can
   assume. If the user just wants it built, assume and state your assumptions.
   Keep **`requirements.md`** as the living record, in plain language for the user: Goal · Users & roles · What it
   tracks (with key details) · How things relate · Screens & dashboard · AI · Assumptions · Open questions ·
   Changelog (one dated line per version).
2. **Write the schema.** Follow the rules from `./neoapps guidelines` (also in `src/NeoApps.Mcp/Tools/SchemaGuidelines.cs`):
   - every table: `<singular>_id INT AUTO_INCREMENT PRIMARY KEY` plus the audit columns
     `createdBy, modifiedBy VARCHAR(255) NOT NULL`, `createdAt, modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP`,
     `isActive TINYINT(1) NOT NULL DEFAULT '1'`
   - lowercase snake_case names, `FOREIGN KEY ... REFERENCES`, 3-15 tables
   - **never** reuse a built-in table name: `roles`, `users`, `entities`, `permissionmatrix`, `messagequeue`,
     `s3bucket`, `s3bucket_folders`, `dnd_ui_versions`, `project_dnd_ui_versions`, `workflow`, `workflows`,
     `workflows_projects`, `workflow_builds`, `workflow_runs`, `workflow_deployments`, `workflow_triggers`,
     `workflow_trigger_conditions`, `ai_files`, `ai_chunks`, `ai_conversations`, `ai_messages`, `ai_runs`, `versioninfo`, `user_tenants`, `login_attempts`, `user_security` (use e.g. `members`, `automation_runs`)
   - plain `CREATE TABLE` / `INSERT` only: no `CREATE DATABASE`, `USE`, `DELIMITER`, procedures
   Save it as `workspace/<App>/schema.sql` (copy it to `samples/` if it should be committed).
3. **Design the UI** (recommended): write `workspace/<App>/ui.config.json` following `neoapps_ui_config_guide`
   (`src/NeoApps.Mcp/Tools/UiConfigTools.cs`). It sets menu groups, labels, select options and colours for
   status/type columns, list columns, form order, dashboard cards, theme, and `roles` matching how the user described
   who does what. Model enum-like columns as VARCHAR in the schema and give them options here.
   **Preview before generating:** `./neoapps preview <App>` (MCP: `neoapps_preview_app`) checks the plan and writes
   `workspace/<App>/preview.html`, a one-page visual with a data-model diagram, every screen's list and form fields, a
   who-can-do-what matrix, the AI features, and issues to fix. Fix the issues, then show the page to the user
   (publish it as an artifact if you can, otherwise send the file) together with a short plain-language summary, and
   iterate. When the user approves, run `./neoapps preview <App> --save` to record the version (`history/vN/`).
   **Generate:** `./neoapps new <App> --schema workspace/<App>/schema.sql --ui workspace/<App>/ui.config.json` (App = PascalCase identifier, e.g. `TodoApp`).
   Add `--deploy` to build and run it in Docker, and `--template dnd` for the drag-and-drop designer UI (it generates
   components per table: very large schemas, e.g. 100+ tables, exceed the CRA type checker's memory; use the admin UI for those).
   **Custom screens:** if the user needs something the schema-driven UI can't do (a personal summary page, a calculation,
   a one-click action), write it in `workspace/<App>/custom/index.tsx` (exports `extensions`: `routes`, `nav`,
   `dashboardTop`, `recordActions`; example `samples/splitshare/custom`). It is copied into the app on every generate.
   **AI:** if the app needs document Q&A, "ask your data", or AI-driven record creation, add an `ai` section to the
   ui.json (assistant instructions, `documentTables`, `tools`, `workflows`; see the guide) instead of modelling chat
   tables. It runs on the Claude API when the API process has `ANTHROPIC_API_KEY` set (never put the key in files).
   **Data rules:** for multi-tenant apps, customer portals (customers see only their own rows), status workflows, approvals,
   audit trails, domain events, SLAs or plan limits, add the `tenancy`, `rowScopes`, `lifecycles`, `immutable`, `audit`, `outbox`, `jobs` and `limits` sections to ui.config.json (see the guide)
   instead of hand-writing checks: the generated API enforces them on every query (`Utility/DataGuard*.cs`). After deploying,
   run `./neoapps test <App>` and report the result (tenant isolation per table, row scopes, append-only tables, lifecycle steps).
4. **When requirements change** (they will): add a changelog line to `requirements.md`, update the schema and UI config,
   and run `./neoapps preview <App>`. It opens with **What changed since the last version**: tables and columns added,
   removed or changed, relations, role permissions, AI workflows and the menu, with changes highlighted in the diagram.
   Confirm with the user, then apply it **keeping the data**: `./neoapps evolve <App> --name "<change>" --plan` shows the
   migration (show it to the user; mention anything under `skipped` or `warnings`), and `./neoapps evolve <App> --name "<change>"`
   writes `workspace/<App>/migrations/M<ts>_<Name>.cs` (FluentMigrator, Up/Down), backs up, migrates, grants new tables to
   existing roles, regenerates API + UI and saves the version (MCP: `neoapps_evolve_app`). Drops need `--allow-destructive`;
   renames need a hint (`--renames file.json`: `{"old_table":"new_table","table.old_col":"new_col"}`). The generated API
   applies pending migrations on startup, so a deploy moves other databases forward in place.
   Only use `./neoapps new` again when the user wants a fresh database (it resets test data; existing migrations are
   baselined). UI-only changes (labels, options, menu, list columns) can use `./neoapps ui <App> --set changes.json`.
5. **Report back:** web URL, Swagger URL, admin username/password (printed by the command and kept in
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

Production settings of a generated API (`appsettings.json`, override with env vars such as `Cors__AllowedOrigins`):
`Cors:AllowedOrigins` (set to the generated UI's origin; any origin only in Development), `Swagger:Enabled` (Swagger is on
in Development only), `Api:MaxPageSize` (default 1000), `Logging:LogSql` (off). Unexpected errors return a generic message
with a reference (the `X-Correlation-Id` header) and are logged server-side; `/health/ready` checks MySQL, Redis and RabbitMQ.
`--deploy` runs the API in Production with the UI origin allowed and Swagger on for local use.
Sign-in protection (`Auth` in appsettings): lockout after `MaxFailedLogins` failures for `LockoutMinutes`, per-IP rate limit
(`LoginsPerMinutePerIp`), password rules (`MinPasswordLength`, letters + digits), access tokens of `TokenMinutes` (8h).
Sign-out (`POST /Access/logout`), password/role/active changes and `POST /Access/members/{id}/sign-out` end existing sessions
(checked per request, 30s cache per API instance); `POST /Access/members/{id}/unlock` clears a lockout; `PUT /Access/me/password`.
Tests that sign in many users quickly need a higher `Auth__LoginsPerMinutePerIp`.

Pass the key to the API from the environment, e.g. `set -a; . ~/secrets/anthropic.env; set +a` before `dotnet run`
(`--deploy` forwards `ANTHROPIC_API_KEY` into the container). AI endpoints: `/v1/api/Ai/status`, `files`, `ask`,
`conversations`, `workflows/{id}/preview|apply`.

Login: `POST /v1/api/Token {"username","password"}` → `document.accessToken`. Use it as a Bearer token on `/v1/api/<Table>`.
Rows are owner-scoped (`createdBy`/`modifiedBy` hold the workspace owner), so seed rows inserted with another `createdBy` won't show up.
Who really created or last changed a row is in `created_by_user`/`modified_by_user` (`<user> via AI` for AI writes, `system` for jobs):
added to every app table automatically and stamped by DataGuard on every write, so don't put them in schema.sql.

### Verify before you say it works

- `./neoapps new` exits 0 and `backendWarnings` / `frontendWarnings` are empty.
- The generated solution builds: `dotnet build <App>.sln`. The UI builds: `npm run build`.
- `/health` returns ok, login returns a token, and one create + list call succeeds.
- When ui.config.json has data rules: `./neoapps test <App>` passes (writes workspace/<App>/guard-test-report.json).
- `GET /v1/api/Access/me` returns the admin's permissions. Log into the UI and open a list page and a form.

## Working on the generator

- `CodeGenerator/`: generator (.NET 10 console app). `Program.cs` is the backend generator and `Reactjs/Functions.cs`
  the frontend one. Both are large legacy files built on string templates, so make surgical edits.
- Templates: `CodeGenerator/DotNetMySQLTemplate/` (`*.txt` snippets plus `solution/nkv.MicroService`, renamed per app) and
  `CodeGenerator/ReactTsTemplate{1..4}/` (1 = drag-and-drop, 3 = admin UI).
- `CodeGenerator/Infrastructure/`: config loading (`--config` JSON or the legacy `PARAMETER` env var), `SchemaReader`
  (always scoped to `DATABASE()`), `MySqlScript` (statement splitter), `ProcessRunner`.
- `CodeGenerator/ReactUiKit/`: the schema-driven admin UI copied into every admin frontend (`src/neoapps/*`).
  `Infrastructure/UiKit.cs` installs it and writes `public/schema.json` (from the DB) and `public/ui.config.json`,
  and seeds roles. The old per-table components are still generated but no longer routed. `src/custom/` is the
  extension point for app-specific screens (empty by default; replaced by `workspace/<App>/custom/` when present).
- Migrations: `src/NeoApps.Mcp/Services/SchemaMigrations.cs` diffs `information_schema` against the parsed schema.sql
  (`AppPreview.Parse`) and renders the FluentMigrator class; `MigrationRunner.cs` builds `src/NeoApps.Migrator` with the
  app's migrations compiled in. Helpers used by generated migrations (`GrantNewTable`, `EnsureRole`, `AddToSavedMenu`,
  `WithAuditColumns`...) and the startup runner live in the template's `<App>.API/Migrations/MigrationSupport.cs`
  (namespace `NeoApps.Migrations`, shared with the migrator). `versioninfo` is FluentMigrator's history table and is
  excluded from code generation.
- RBAC: `AccessController` (template API) serves `/v1/api/Access/me`, members, roles, the permission grid and the
  saved UI config. Role templates are `permissionmatrix` rows with `user_id IS NULL`, and members get copies.
  Passwords use BCrypt (`Utility/PasswordHasher.cs`).
- `src/NeoApps.Mcp/`: MCP server and CLI (`Cli.cs`). Both use the same services; the generator runs as a child process.
- Build: `dotnet build NeoApps.slnx`. **Test any generator change by generating an app and building it**
  (`./neoapps new TestApp --schema samples/todo-app.sql`, then build the generated `.sln` and run `npm run build`).
- The generator reads templates relative to `CodeGenerator/` and writes apps to `../1_<ticks>/`. Never commit
  `1_*/` or `workspace/` (both are git-ignored).
- MySQL must run with `lower_case_table_names=1`: generated SQL mixes the casing of table names.
- Data rules live in the template's `Utility/DataGuard.cs` (tenancy, SQL choke point: every generated command runs through
  `DataGuard.Apply`/`DataGuard.NonQuery`), `DataGuard.Scopes.cs` (row scopes), `DataGuard.Rules.cs` (lifecycles, immutability, audit, outbox, limits, SLA) and
  `GuardJobs*.cs` (outbox publisher, deadline job). Any new data-access code must execute through them.
- Never commit credentials. Generated apps get a random JWT secret, and S3 keys come from env vars.
