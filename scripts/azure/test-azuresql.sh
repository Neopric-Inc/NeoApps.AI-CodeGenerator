#!/usr/bin/env bash
# End-to-end test of `neoapps new --db azuresql`: generates the todo sample on the Azure SQL database made by
# test-resources.sh, builds it, starts the API against Azure SQL and runs the smoke test. Run from the repo folder, with
# Docker Desktop open (the generator uses a scratch MySQL, Redis and RabbitMQ there).
#   ./scripts/azure/test-azuresql.sh            # resets the test database each run
set -euo pipefail
cd "$(dirname "$0")/../.."
ENV_FILE="${ENV_FILE:-$HOME/secrets/neoapps-azure-test.env}"
[ -f "$ENV_FILE" ] || { echo "No $ENV_FILE: run ./scripts/azure/test-resources.sh first."; exit 1; }
set -a; . "$ENV_FILE"; set +a
APP="${APP:-AzSqlTest}"

echo "== Generate $APP on Azure SQL"
./neoapps new "$APP" --schema samples/todo-app/todo-app.sql --db azuresql --no-frontend

BACKEND=$(python3 -c "import json;print(json.load(open('workspace/$APP/project.json'))['BackendPath'])")
echo "== Build"
(cd "$BACKEND" && dotnet build "$APP.sln" -v q -nologo | tail -3)

echo "== Run the API against Azure SQL and test it"
python3 scripts/azure/smoke-test.py "$APP"
