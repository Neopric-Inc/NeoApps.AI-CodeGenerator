#!/usr/bin/env bash
# Usage: ADMIN_U=<admin> ADMIN_P=<password> API=http://localhost:5100/v1/api bash acceptance-test.sh
# Run on a FRESH FacilityMap database (it creates members and changes tickets). Needs node 22+ (WebSocket) for the live check.
# Checks: tenant sees only their unit, roles on tickets, only a Safety Inspector clears a safety hold, severity can't be
# downgraded, numbering, audit of refusals, and the live room hearing a ticket change within a second.
set -u
B=${API:-http://localhost:5100/v1/api}; J='Content-Type: application/json'
pass=0; fail=0
check() { if [ "$2" = "$3" ]; then echo "PASS $1"; pass=$((pass+1)); else echo "FAIL $1: got '$2' want '$3' :: $(head -c 200 /tmp/fm-r.json 2>/dev/null)"; fail=$((fail+1)); fi; }
tok() { curl -s -X POST $B/Token -H "$J" -d "{\"username\":\"$1\",\"password\":\"$2\"}" | python3 -c "import json,sys;print(json.load(sys.stdin)['document']['accessToken'])"; }
code() { curl -s -o /tmp/fm-r.json -w "%{http_code}" "$@"; }
c1() { python3 -c "import json;d=json.load(open('/tmp/fm-r.json'));print(d.get('code') if isinstance(d,dict) else d)"; }
msg() { python3 -c "import json;print(json.load(open('/tmp/fm-r.json')).get('message',''))"; }
labels() { python3 -c "import json;d=json.load(open('/tmp/fm-r.json'));print(sorted(str(r.get('$1')) for r in (d.get('document') or {}).get('records',[])))"; }
q() { mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-facility_map_db} -N -e "$1" 2>/dev/null; }
role() { curl -s "$B/Access/roles" -H "Authorization: Bearer $A" | python3 -c "import json,sys;print([r['role_id'] for r in json.load(sys.stdin)['document'] if r['role_name']=='$1'][0])"; }
tk() { q "SELECT ticket_id FROM tickets WHERE title='$1'"; }
body() { q "SELECT JSON_OBJECT('ticket_id',ticket_id,'ticket_number',ticket_number,'unit_id',unit_id,'appliance_id',appliance_id,'title',title,'details',details,'severity','$2','ticket_status','$3','work_notes',work_notes) FROM tickets WHERE ticket_id=$1"; }

A=$(tok "$ADMIN_U" "$ADMIN_P")
for m in "jordan.lee:Tenant" "sam.rivera:Handyman" "dana.kim:Safety Inspector" "pat.morgan:Property Manager"; do
  u=${m%%:*}; r=${m#*:}; curl -s -X POST $B/Access/members -H "Authorization: Bearer $A" -H "$J" -d "{\"username\":\"$u\",\"password\":\"Passw0rd2026\",\"role_id\":$(role "$r"),\"tenant_id\":0}" >/dev/null
done
T=$(tok jordan.lee Passw0rd2026); H=$(tok sam.rivera Passw0rd2026); I=$(tok dana.kim Passw0rd2026); P=$(tok pat.morgan Passw0rd2026)

# tenant: own unit only (Maple Court 302), but every floor to draw the building
code "$B/Units?page=1&itemsPerPage=100" -H "Authorization: Bearer $T" >/dev/null; check "tenant sees only their unit" "$(labels unit_label)" "['302']"
code "$B/Tickets?page=1&itemsPerPage=100" -H "Authorization: Bearer $T" >/dev/null; check "tenant sees only their tickets" "$(labels title)" "['Gas smell near the stove']"
code "$B/Appliances?page=1&itemsPerPage=100" -H "Authorization: Bearer $T" >/dev/null; check "tenant sees only their appliances" "$(python3 -c "import json;print(len(json.load(open('/tmp/fm-r.json'))['document']['records']))")" 4
GAS=$(tk 'Gas smell near the stove'); AC=$(tk 'A/C not cooling'); FAUCET=$(tk 'Leaky kitchen faucet')
code -X PUT $B/Tickets/$GAS -H "Authorization: Bearer $T" -H "$J" -d "$(body $GAS 'Safety Hold' Resolved)" >/dev/null; check "tenant can't change tickets" "$([ "$(c1)" != 1 ] && q "SELECT ticket_status FROM tickets WHERE ticket_id=$GAS")" Open
code -X POST $B/Tickets -H "Authorization: Bearer $T" -H "$J" -d "{\"unit_id\":$(q "SELECT unit_id FROM tickets WHERE ticket_id=$AC"),\"title\":\"Not my unit\",\"severity\":\"Routine Maintenance\",\"ticket_status\":\"Open\"}" >/dev/null
check "tenant's ticket always lands on their own unit" "$(q "SELECT u.unit_label FROM tickets t JOIN units u ON u.unit_id=t.unit_id WHERE t.title='Not my unit'")" 302
code -X POST $B/Tickets -H "Authorization: Bearer $T" -H "$J" -d "{\"unit_id\":10,\"title\":\"Bathroom fan noisy\",\"severity\":\"Routine Maintenance\",\"ticket_status\":\"Open\"}" >/dev/null; check "tenant reports an issue" "$(c1)" 1
check "new ticket numbered" "$(q "SELECT ticket_number FROM tickets WHERE title='Bathroom fan noisy'")" "TKT-$(date +%Y)-00002"

# handyman: works on repairs, can't clear a safety hold or downgrade it
code -X PUT $B/Tickets/$AC -H "Authorization: Bearer $H" -H "$J" -d "$(body $AC 'Urgent Repair' 'In progress')" >/dev/null; check "handyman starts a repair" "$(c1)" 1
code -X PUT $B/Tickets/$AC -H "Authorization: Bearer $H" -H "$J" -d "$(body $AC 'Urgent Repair' Resolved)" >/dev/null; check "handyman resolves an urgent repair" "$(c1)" 1
code -X PUT $B/Tickets/$GAS -H "Authorization: Bearer $H" -H "$J" -d "$(body $GAS 'Safety Hold' Resolved)" >/dev/null; check "handyman can't clear a safety hold" "$(c1)" 0
check "the refusal names the inspector" "$(msg | grep -c 'Safety Inspector')" 1
code -X PUT $B/Tickets/$GAS -H "Authorization: Bearer $H" -H "$J" -d "$(body $GAS 'Routine Maintenance' Resolved)" >/dev/null
check "downgrading the severity doesn't get around it" "$(q "SELECT CONCAT(severity,'/',ticket_status) FROM tickets WHERE ticket_id=$GAS")" "Safety Hold/Open"
code -X POST $B/Units -H "Authorization: Bearer $H" -H "$J" -d '{"floor_id":1,"unit_label":"X","unit_kind":"Apartment"}' >/dev/null; check "handyman can't add units" "$(c1)" "None"

# property manager: full admin, but still not the safety hold
code -X PUT $B/Tickets/$GAS -H "Authorization: Bearer $P" -H "$J" -d "$(body $GAS 'Safety Hold' Resolved)" >/dev/null; check "manager can't clear a safety hold" "$(c1)" 0

# live: a listener in room "facility" hears the inspector's change within a second
node -e '
const [url, tokenWatcher, tokenInspector, api, id, body] = process.argv.slice(1);
const ws = new WebSocket(url, { headers: { Authorization: "Bearer " + tokenWatcher } });
let sent = 0;
ws.onmessage = async (ev) => {
  const m = JSON.parse(ev.data);
  if (m.t === "welcome") { sent = Date.now();
    await fetch(api + "/Tickets/" + id, { method: "PUT", headers: { "Content-Type": "application/json", Authorization: "Bearer " + tokenInspector }, body }); }
  if (m.t === "change" && m.table === "tickets" && String(m.id) === id) { console.log("LIVE " + (Date.now() - sent)); process.exit(0); }
};
setTimeout(() => { console.log("LIVE timeout"); process.exit(1); }, 5000);
' "$(echo $B | sed 's#^http#ws#')/live/facility" "$P" "$I" "$B" "$GAS" "$(body $GAS 'Safety Hold' Resolved)" > /tmp/fm-live.txt 2>&1
LIVE_MS=$(sed -n 's/^LIVE \([0-9]*\)$/\1/p' /tmp/fm-live.txt)
check "inspector clears the safety hold" "$(q "SELECT ticket_status FROM tickets WHERE ticket_id=$GAS")" Resolved
check "live room hears the change within 1 s" "$([ -n "$LIVE_MS" ] && [ "$LIVE_MS" -lt 1000 ] && echo yes || echo "no ($(cat /tmp/fm-live.txt))")" yes
echo "  (change reached the listener in ${LIVE_MS:-?} ms, including the save)"

# audit
check "refused clearance is in the audit log" "$(q "SELECT COUNT(*) > 0 FROM audit_logs WHERE action_code='tickets.denied' AND actor_ref='sam.rivera'")" 1
check "clearance is in the audit log" "$(q "SELECT COUNT(*) FROM audit_logs WHERE action_code='tickets.update' AND actor_ref='dana.kim' AND entity_ref='$GAS'")" 1
echo "passed $pass, failed $fail"
