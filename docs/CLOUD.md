# Deploying to NeoApps Cloud

```bash
./neoapps cloud login                                  # paste a deploy token (nac_...) from https://cloud.neoapps.ai
./neoapps deploy ClaimKit --target cloud --region us   # first deploy picks the region: us or ca
./neoapps deploy ClaimKit --target cloud               # later deploys reuse it; data stays in that region
```

In Claude Desktop: `neoapps_deploy_app` with `target: "cloud"` and `region`. The app goes live at
`https://<app>.<region>.neoapps.ai`. The first deploy returns the admin login once.

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

All calls carry `Authorization: Bearer nac_...`. Base URL: `NEOAPPS_CLOUD_URL` (default `https://cloud.neoapps.ai`).

| Call | Returns |
| --- | --- |
| `POST /v1/api/cloud/apps {app, slug, region}` | `{slug, region, status}`, or `{checkoutUrl}` when the account has no active plan; 4xx `{message}` if the slug is taken or the region differs |
| `POST /v1/api/cloud/apps/{slug}/upload` | `{uploadUrl, blob}`: a one-time link; the CLI `PUT`s the bundle with `x-ms-blob-type: BlockBlob` |
| `POST /v1/api/Jobs {kind: "cloud.deploy", input: {slug, blob}}` | the job (`job_id`), standard NeoApps jobs API |
| `GET /v1/api/Jobs/{id}/events?after=N&wait=20` | `{job: {status, result, error}, events: [{id, message}]}`; `result` = `{url, adminUsername?, adminPassword?}` |

The `cloud.deploy` job on the host: create the database and user on first deploy and run `db/init.sql`, set a new
admin password, write `.env` (`ConnectionStrings__MySQLDatabase` with `SslMode=Required`, `ConnectionStrings__RabitMQ`
with the app's own vhost, `AppSettings__Secret`, `Cors__AllowedOrigins`), `docker compose up -d --build`, route
`<slug>.<region>.neoapps.ai` to `<slug>-web:80` in Caddy, wait for `/health/ready`, roll back on failure.

Test the client without a control plane: `python3 scripts/cloud-stub.py 8765`, then
`NEOAPPS_CLOUD_URL=http://127.0.0.1:8765 NEOAPPS_CLOUD_TOKEN=nac_test123 ./neoapps deploy <App> --target cloud --region us`.
