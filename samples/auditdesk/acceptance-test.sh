#!/usr/bin/env bash
# Usage: ADMIN_U=<admin> ADMIN_P=<password> API=http://localhost:5100/v1/api bash acceptance-test.sh
# Run on a FRESH AuditDesk database (it creates members and changes sample rows).
# AuditDesk acceptance checks: roles, client portal scope, staff scope, locks, phase gate, numbering, append-only.
set -u
B=${API:-http://localhost:5100/v1/api}; J='Content-Type: application/json'
pass=0; fail=0
check() { if [ "$2" = "$3" ]; then echo "PASS $1"; pass=$((pass+1)); else echo "FAIL $1: got '$2' want '$3' :: $(head -c 220 /tmp/r.json)"; fail=$((fail+1)); fi; }
tok() { curl -s -X POST $B/Token -H "$J" -d "{\"username\":\"$1\",\"password\":\"$2\"}" | python3 -c "import json,sys;print(json.load(sys.stdin)['document']['accessToken'])"; }
code() { curl -s -o /tmp/r.json -w "%{http_code}" "$@"; }
c1() { python3 -c "import json;d=json.load(open('/tmp/r.json'));print(d.get('code') if isinstance(d,dict) else d)"; }
n() { python3 -c "import json;d=json.load(open('/tmp/r.json'));doc=d.get('document');print(doc.get('totalRecords',len(doc.get('records',[]))) if isinstance(doc,dict) else 0)"; }
field() { python3 -c "import json,sys;d=json.load(open('/tmp/r.json'));r=(d.get('document') or {}).get('records',[]);print(sorted(set(str(x.get('$1')) for x in r)))"; }
role() { curl -s "$B/Access/roles" -H "Authorization: Bearer $A" | python3 -c "import json,sys;d=json.load(sys.stdin);print([r['role_id'] for r in d['document'] if r['role_name']=='$1'][0])"; }

A=$(tok "$ADMIN_U" "$ADMIN_P")
for m in "priya.shah:Partner" "marcus.lee:Manager" "ana.torres:Staff" "dana.brooks:Client"; do
  u=${m%%:*}; r=${m##*:}; curl -s -X POST $B/Access/members -H "Authorization: Bearer $A" -H "$J" -d "{\"username\":\"$u\",\"password\":\"Passw0rd2026\",\"role_id\":$(role $r),\"tenant_id\":0}" >/dev/null
done
P=$(tok priya.shah Passw0rd2026); M=$(tok marcus.lee Passw0rd2026); S=$(tok ana.torres Passw0rd2026); C=$(tok dana.brooks Passw0rd2026)

code "$B/Engagements?pageNo=1&pageSize=50" -H "Authorization: Bearer $C" >/dev/null; check "client sees only own engagements" "$(field client_id)" "['1']"
check "client can't see materiality" "$(field materiality_amount)" "['None']"
code "$B/Document_requests?pageNo=1&pageSize=50" -H "Authorization: Bearer $C" >/dev/null; check "client sees own requests" "$(n)" 2
code "$B/Findings?pageNo=1&pageSize=50" -H "Authorization: Bearer $C" >/dev/null; check "client can't read findings" "$(c1)" "None"
code "$B/Engagements?pageNo=1&pageSize=50" -H "Authorization: Bearer $S" >/dev/null; check "staff sees only led engagements" "$(field staff_lead_id)" "['3']"

# client uploads, then can't edit an uploaded request
RID=$(mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-audit_desk_db} -N -e "SELECT document_request_id FROM document_requests WHERE request_number='DRQ-2026-00001'" 2>/dev/null)
body() { echo "{\"engagement_id\":1,\"client_contact_id\":1,\"item_requested\":\"December bank statements, all operating accounts\",\"instructions\":\"$1\",\"due_date\":\"2027-01-15\",\"request_status\":\"$2\"$3}"; }
code -X PUT $B/Document_requests/$RID -H "Authorization: Bearer $C" -H "$J" -d "$(body 'PDF statements for every account open at year end.' Uploaded '')" >/dev/null; check "client marks uploaded" "$(c1)" 1
code -X PUT $B/Document_requests/$RID -H "Authorization: Bearer $C" -H "$J" -d "$(body changed Uploaded '')" >/dev/null; check "client can't edit after upload" "$(c1)" 0
code -X PUT $B/Document_requests/$RID -H "Authorization: Bearer $S" -H "$J" -d "$(body 'PDF statements for every account open at year end.' Returned '')" >/dev/null; check "return needs a comment" "$(c1)" 0
code -X PUT $B/Document_requests/$RID -H "Authorization: Bearer $S" -H "$J" -d "$(body 'PDF statements for every account open at year end.' Returned ',"return_comment":"Missing the payroll account."')" >/dev/null; check "staff returns with comment" "$(c1)" 1

# findings: staff drafts (numbered), can't approve; manager approves; staff can't edit approved
code -X POST $B/Findings -H "Authorization: Bearer $S" -H "$J" -d '{"engagement_id":1,"audit_area":"Cash","finding_description":"Unreconciled difference of $4,210 on the operating account.","amount":4210,"severity":"Minor","finding_status":"Draft"}' >/dev/null; check "staff drafts finding" "$(c1)" 1
FID=$(python3 -c "import json;print(json.load(open('/tmp/r.json'))['document'])")
FN=$(mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-audit_desk_db} -N -e "SELECT finding_number FROM findings WHERE finding_id=$FID" 2>/dev/null); check "finding numbered" "$FN" "FND-2026-00002"
fb() { echo "{\"engagement_id\":1,\"audit_area\":\"Cash\",\"finding_description\":\"$1\",\"amount\":4210,\"severity\":\"Minor\",\"finding_status\":\"$2\"}"; }
code -X PUT $B/Findings/$FID -H "Authorization: Bearer $S" -H "$J" -d "$(fb 'Unreconciled difference of $4,210 on the operating account.' Approved)" >/dev/null; check "staff can't approve" "$(c1)" 0
code -X PUT $B/Findings/$FID -H "Authorization: Bearer $M" -H "$J" -d "$(fb 'Unreconciled difference of $4,210 on the operating account.' Approved)" >/dev/null; check "manager approves" "$(c1)" 1
code -X PUT $B/Findings/$FID -H "Authorization: Bearer $S" -H "$J" -d "$(fb 'edited after approval' Approved)" >/dev/null; check "staff can't edit approved finding" "$(c1)" 0

# engagement 4 (Report): partner can't sign off without a Report sign-off; sign-off is append-only
eb() { echo "{\"client_id\":4,\"engagement_type\":\"Compilation\",\"period_end\":\"2026-06-30\",\"due_date\":\"2026-09-30\",\"phase\":\"$1\",\"partner_id\":1,\"manager_id\":2,\"staff_lead_id\":4,\"revenue_benchmark\":9500000,\"materiality_amount\":95000,\"performance_materiality\":71250}"; }
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "sign-off blocked without Report sign-off" "$(c1)" 0
code -X POST $B/Sign_offs -H "Authorization: Bearer $P" -H "$J" -d '{"engagement_id":4,"phase":"Report","signed_by_id":1,"sign_off_comment":"Report reviewed."}' >/dev/null; check "partner records Report sign-off" "$(c1)" 1
SID=$(python3 -c "import json;print(json.load(open('/tmp/r.json'))['document'])")
code -X PUT $B/Sign_offs/$SID -H "Authorization: Bearer $P" -H "$J" -d '{"engagement_id":4,"phase":"Report","signed_by_id":1,"sign_off_comment":"changed"}' >/dev/null; [ "$(c1)" != "1" ] && check "sign-off can't be edited" blocked blocked || check "sign-off can't be edited" changed blocked
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $M" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "manager can't sign off engagement" "$(c1)" 0
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "partner signs off" "$(c1)" 1
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Report')" >/dev/null; check "signed-off engagement is locked" "$(c1)" 0
code -X PUT $B/Engagements/1 -H "Authorization: Bearer $M" -H "$J" -d '{"client_id":1,"engagement_type":"Audit","period_end":"2026-12-31","due_date":"2026-12-01","phase":"Fieldwork","partner_id":1,"manager_id":2,"staff_lead_id":3,"revenue_benchmark":48000000,"materiality_amount":240000,"performance_materiality":180000}' >/dev/null; check "due date before period end refused" "$(c1)" 0
H=$(mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-audit_desk_db} -N -e "SELECT COUNT(*) FROM audit_logs" 2>/dev/null); [ "$H" -gt 0 ] && check "audit log written" yes yes || check "audit log written" "$H rows" ">0"
echo "passed $pass, failed $fail"
