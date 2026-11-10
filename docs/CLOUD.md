# Deploying to NeoApps Cloud

```bash
./neoapps cloud login                                  # opens the browser: sign up or sign in, pick a plan, approve this computer
./neoapps deploy ClaimKit --target cloud --region us   # first deploy picks the region: us or ca
./neoapps deploy ClaimKit --target cloud               # later deploys reuse it; data stays in that region
```

**No install at all (non-technical users):** add the NeoApps connector in Claude (Desktop or claude.ai): Settings →
Connectors → Add custom connector → `https://cloud.neoapps.ai/mcp`, sign in. Claude then plans, previews, publishes and
changes apps on NeoApps servers (tools `start_here`, `save_app_plan`, `get_app_plan`, `publish_app`, `publish_status`,
`my_apps`, `open_app`, `delete_app`, `billing`); nothing runs on the user's computer.

**From this repo:** in Claude Desktop: `neoapps_deploy_app` with `target: "cloud"` and `region`. Not signed in yet? The deploy opens the same
browser page and waits; the job log shows the link and an 8-character code to compare. CI: `./neoapps cloud login --token`
(or `NEOAPPS_CLOUD_TOKEN`). The app goes live at
`https://<app>.<region>.neoapps.ai`. On the first deploy the app's administrator becomes the customer's own NeoApps login (same username and password);
the result says so in `signIn`. `./neoapps cloud reset-admin <App>` (MCP: `neoapps_cloud_reset_admin`) sets a separate
password if ever needed.

App settings such as AI keys: `./neoapps cloud env set <App> ANTHROPIC_API_KEY` (value taken from your shell, so it stays out of
shell history; `KEY=value` also works) and `./neoapps cloud env unset <App> KEY`. They go straight to the app's `.env` on its host
and the app restarts; the control plane never stores them and they are never shown back.

`./neoapps pack <App> --out app.tar.gz` writes the same bundle to a file without uploading it (used to install the control plane).

## What leaves your machine

`CloudBundle` packs one `.tar.gz` (deleted after upload):

| Entry | Content |
| --- | --- |
| `manifest.json` | app, slug, region, database name, admin username, format version |
| `docker-compose.yml` | api + its own Redis + web; no published ports; memory/CPU limits; `.env` written by the host |
| `db/init.sql` | `CREATE TABLE` for every table, plus roles, permission templates, the admin row, saved UI settings and migration history. No app data. |
| `api/`, `web/` | generated source without `bin`, `obj`, `node_modules`, `build`, `App_Data` |

`appsettings*.json` are stripped of connection strings, the JWT secret, stored passwords and S3 keys.
The token is read from `NEOAPPS_CLOUD_TOKEN` or the OS keychain (service `neoapps-cloud`), never a file.

## API the control plane implements

Base URL: `NEOAPPS_CLOUD_URL` (default `https://cloud.neoapps.ai`). The deploy token is only sent to `/cloud/session`;
every other call carries the session it returns (`Authorization: Bearer <accessToken>`), so the standard jobs API works unchanged.

| Call | Returns |
| --- | --- |
| `POST /v1/api/cloud/device/start {device}` (no auth) | `{userCode, deviceCode, verifyUrl, interval, expiresIn}`: open `verifyUrl` in the browser |
| `POST /v1/api/cloud/device/poll {deviceCode}` (no auth) | 202 while waiting, then 200 `{token}` once (a deploy token for the approving account), 410 when expired or used |
| `GET /v1/api/cloud/connect?code=` | the browser page: sign up (`POST cloud/signup`) or sign in, choose a plan (`POST cloud/billing/checkout`), approve (`POST cloud/device/approve`) |
| `POST /v1/api/cloud/session` with `Authorization: Bearer nac_...` | `{accessToken, expiresAt}`: a one-hour sign-in as the user who created the token |
| `POST /v1/api/cloud/apps {app, slug, region}` | `{slug, region, status}`, or `{checkoutUrl}` when the account has no active plan; 4xx `{message}` if the slug is taken or the region differs |
| `POST /v1/api/cloud/apps/{slug}/upload` | `{uploadUrl, blob}`: a one-time link; the CLI `PUT`s the bundle with `x-ms-blob-type: BlockBlob` |
| `PUT /v1/api/cloud/apps/{slug}/env {set: {KEY: value}, unset: [KEY]}` | `{slug, settings: [names]}`: written to the app's host, app restarted; platform keys refused |
| `POST /v1/api/cloud/apps/{slug}/reset-admin` | `{username, password}`: new password for the app's first admin (shown once), unlocks it and ends its sessions; own apps only |
| `POST /v1/api/Jobs {kind: "cloud.deploy", input: {slug, blob}}` | the job (`job_id`), standard NeoApps jobs API |
| `GET /v1/api/Jobs/{id}/events?after=N&wait=20` | `{job: {status, result, error}, events: [{id, message}]}`; `result` = `{url, adminUsername?, adminPassword?}` |

The `cloud.deploy` job on the host: create the database and user on first deploy and run `db/init.sql`, set a new
admin password, write `.env` (`ConnectionStrings__MySQLDatabase` with `SslMode=Required`, `ConnectionStrings__RabitMQ`
with the app's own vhost, `AppSettings__Secret`, `Cors__AllowedOrigins`), `docker compose up -d --build`, route
`<slug>.<region>.neoapps.ai` to `<slug>-web:80` in Caddy, wait for `/health/ready`, roll back on failure.

Test the client without a control plane: `python3 scripts/cloud-stub.py 8765`, then
`NEOAPPS_CLOUD_URL=http://127.0.0.1:8765 NEOAPPS_CLOUD_TOKEN=nac_test123 ./neoapps deploy <App> --target cloud --region us`.
