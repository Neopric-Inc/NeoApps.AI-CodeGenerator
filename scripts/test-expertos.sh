#!/bin/bash
# Seeds ExpertOS and runs every data-rule suite against the API on :5100 (start it with Auth__LoginsPerMinutePerIp=1000).
# Usage: ./scripts/test-expertos.sh   (credentials from workspace/ExpertOS/project.json)
cd "$(dirname "$0")/.."
read U P < <(python3 -c "import json;d=json.load(open('workspace/ExpertOS/project.json'));print(d['AdminUsername'], d['AdminPassword'])")
python3 scripts/seed-expertos.py "$U" "$P" | tail -1 || exit 1
for s in guard-rules-test-expertos validation-test-expertos numbering-test-expertos real-user-test guard-scope-test-expertos tenancy-test-expertos; do
  printf '%-28s ' "$s"; python3 scripts/$s.py "$U" "$P" 2>&1 | grep -E '^FAIL|^[0-9]+/[0-9]+ passed' | tail -4
done
NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=${NEOAPPS_MYSQL_PORT:-3306} ./neoapps test ExpertOS 2>&1 | tail -1
echo "separately (need their own API settings): auth-test.py (Auth__MaxFailedLogins=3 Auth__LockoutMinutes=1 Auth__LoginsPerMinutePerIp=30), hardening-test.py (Api__MaxPageSize=2), guard-upgrade-test.py"
