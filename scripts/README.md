# Scripts

| Script | What it does |
|---|---|
| `sandbox-deps.sh` | Installs/starts MySQL, Redis and RabbitMQ without Docker (cloud sessions, CI). |
| `setup.sh` | Runs `../install.sh` against this clone: builds the CLI, puts `neoapps` on PATH, registers the MCP server with Claude Code / Claude Desktop. |
| `expertos-model.py <dir>` | Generates the ExpertOS sample (135 tables, every data-rule section) into `<dir>`. `samples/expertos.*` are its output. |
| `regen-expertos.sh` | Regenerates and builds ExpertOS from `workspace/ExpertOS`, prints the admin login and the run command. |
| `seed-expertos.py` | Seeds ExpertOS with a tenant, customers and one case walked through its lifecycle. |
| `test-expertos.sh` | Seeds and runs every ExpertOS suite below plus `./neoapps test`. |
| `guard-tests.py` | Generic suite behind `./neoapps test <App>`: tenancy per table, row scopes, append-only tables, lifecycle steps. |
| `guard-rules-test-expertos.py` | Lifecycles, evidence/forbids, separation of duties, admin bypass, frozen states, audit, outbox, SLA, limits, signed links. |
| `validation-test-expertos.py` | Field validations (#79). |
| `numbering-test-expertos.py` | Automatic numbering, including 20 concurrent inserts (#80). |
| `real-user-test.py` | `created_by_user` / `modified_by_user` (#60). |
| `guard-scope-test-expertos.py` | Row scopes: customers and experts see only their rows (#75). |
| `tenancy-test-expertos.py` | Cross-tenant isolation (#53). |
| `guard-upgrade-test.py` | Enabling tenancy on an existing database (#54). |
| `auth-test.py` | Sign-in protection (#78). Start the API with `Auth__MaxFailedLogins=3 Auth__LockoutMinutes=1 Auth__LoginsPerMinutePerIp=30`. |
| `hardening-test.py` | Production hardening (#77). The page-cap check needs `Api__MaxPageSize=2`. |

Typical loop after a generator change: `./scripts/regen-expertos.sh`, start the API (printed), `./scripts/test-expertos.sh`.
