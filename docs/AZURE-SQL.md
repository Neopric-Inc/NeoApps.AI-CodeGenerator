# Azure SQL (serverless)

`neoapps new <App> --schema schema.sql --db azuresql` builds the app as usual, then puts its database on Azure SQL
(SQL Server). The connection string comes from `NEOAPPS_AZURESQL` (or `--db-env VAR`) and is never written to a
file. The API reads `Database:Provider=azuresql` from appsettings.json and the connection string from
`ConnectionStrings__MySQLDatabase` in its environment. MySQL stays the default and is unchanged.

Test: `./scripts/azure/test-resources.sh` (throwaway Azure SQL, storage, Service Bus), then
`./scripts/azure/test-azuresql.sh`. After about 75 idle minutes, `./scripts/azure/test-azuresql.sh --resume` tests
waking a paused database.

## Serverless rules the app follows

Azure SQL serverless (General Purpose) pauses after the auto-pause delay (15 minutes minimum, 60 default) only when
there are **no sessions and no user CPU** for the whole delay. While paused only storage is billed. The first login
afterwards fails with **error 40613** while it resumes (usually under a minute).
([auto-pause and resume](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-auto-pause-resume))

- **Waking:** opening a connection retries 40613 and the other transient errors for about 2 minutes (`Db.Connection`).
  Commands are not retried. Startup (migrations, DataGuard) uses the same connections.
- **No idle polling on SQL Server:** e-mail notifications, the outbox publisher and the AI file reader run after the
  writes that need them (and once at startup), not on a timer. On MySQL they keep their timers.
- **Scheduled checks** (`jobs` in ui.config: SLA breaches, escalations) still run every `everySeconds` and keep the
  database awake while the API runs. The API logs this at startup.
- **Connection pool:** idle pooled connections close after a few minutes, well inside the 15-minute minimum delay.

## Serverless services (no Redis, no RabbitMQ)

Set in the API's environment; leave them out and the app works as before (disk, RabbitMQ, Redis).

| Setting | Use |
|---|---|
| `Storage__Provider=azureblob`, `Storage__BlobAccountUrl` (managed identity) or `Storage__BlobConnectionString`, `Storage__Container` | AI documents and job files in Azure Blob (container disks are wiped on restart) |
| `ServiceBus__Namespace` (managed identity) or `ServiceBus__ConnectionString`, `ServiceBus__JobsQueue` | Background jobs on Azure Service Bus |
| `ServiceBus__Outbox` | Data-rule outbox events to a Service Bus queue or topic (topics need the Standard tier) |
| `ConnectionStrings__Redis=` and `ConnectionStrings__RabitMQ=` (empty) | No Redis: permission decisions cached in memory for 60 s per instance, job feeds read the database. No RabbitMQ: the legacy workflow events are dropped |

## For deploying (Container Apps)

- Probe `/health` (no database). `/health/ready` queries the database: as a liveness or readiness probe it keeps the
  database awake.
- A cold start can be container start plus database resume (about a minute): give the startup probe at least 2 minutes.
- Don't enable on serverless app databases: geo-replication or failover groups, long-term backup retention, or a DNS
  alias on the server. These stop auto-pause. Vulnerability-assessment scans, auditing views, Query Store settings,
  tag changes and similar management actions wake a paused database.
- Free tier (100,000 vCore-seconds a month): with `AutoPause` the database stays paused for the rest of the month once
  that is used up, and the free tier has no SLA. Fine for tests; customer apps use a regular (paid) serverless database.
- The NeoApps control plane must not poll app databases; check the app's `/health` instead.
- Scale on the jobs queue (KEDA azure-servicebus rule) so a queued job wakes an app that scaled to zero.

## Not yet on Azure SQL

`evolve`, `preview` and `neoapps test` against Azure SQL; scheduled checks (`jobs`) as an Azure scheduled job; AI
document search ranking (uses a substring match on SQL Server for now).
