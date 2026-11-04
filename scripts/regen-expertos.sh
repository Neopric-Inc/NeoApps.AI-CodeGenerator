#!/bin/bash
# Regenerates ExpertOS from workspace/ExpertOS (API only by default; FRONTEND="" TEMPLATE="..." to change), builds it
# and prints the new admin credentials. Without Docker run `sudo ./scripts/sandbox-deps.sh` first.
#   ./scripts/expertos-model.py workspace/ExpertOS   (or: mkdir -p workspace/ExpertOS && cp samples/expertos.sql workspace/ExpertOS/schema.sql && cp samples/expertos.ui.json workspace/ExpertOS/ui.config.json)
#   ./scripts/regen-expertos.sh
set -e
cd "$(dirname "$0")/.."
pkill -f 'net10.0/ExpertOS[.]API' || true
rm -rf 1_*
export NEOAPPS_SKIP_INFRA=${NEOAPPS_SKIP_INFRA:-true} NEOAPPS_MYSQL_PORT=${NEOAPPS_MYSQL_PORT:-3306}
UI=${1:-workspace/ExpertOS/ui.config.json}
./neoapps new ExpertOS --schema workspace/ExpertOS/schema.sql --ui $UI ${FRONTEND---no-frontend} ${TEMPLATE} > /tmp/gen.log 2>&1 || { tail -20 /tmp/gen.log; exit 1; }
python3 -c "import json;d=json.load(open('workspace/ExpertOS/project.json'));print('admin:', d['AdminUsername'], d['AdminPassword'])"
G=$(ls -d 1_*/ExpertOS/DotNet_Output/solution/ExpertOS)
cp $UI $G/ExpertOS.API/ui.config.json
(cd $G && dotnet build ExpertOS.sln -v q 2>&1 | grep -E " error |Build succeeded")
echo "run:  cd $G/ExpertOS.API && Auth__LoginsPerMinutePerIp=1000 ASPNETCORE_URLS=http://0.0.0.0:5100 dotnet run --no-build --no-launch-profile"
