#!/usr/bin/env bash
# Usage: ADMIN_U=<admin> ADMIN_P=<password> API=http://localhost:5100/v1/api bash acceptance-test.sh
# Run on a FRESH AuditDesk database (it creates members and changes sample rows).
# AuditDesk acceptance checks: roles, client portal scope, staff scope, locks, phase gate, numbering, append-only,
# allowed references, monthly budgets (incl. concurrent submits), approval limits with escalation, records created by a
# step, refused attempts in the audit log.
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
eb() { echo "{\"client_id\":4,\"engagement_type\":\"Compilation\",\"period_end\":\"2026-06-30\",\"due_date\":\"2026-09-30\",\"phase\":\"$1\",\"partner_id\":1,\"manager_id\":2,\"staff_lead_id\":4,\"revenue_benchmark\":9500000,\"materiality_amount\":95000,\"performance_materiality\":71250,\"fee_amount\":18000}"; }
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "sign-off blocked without Report sign-off" "$(c1)" 0
code -X POST $B/Sign_offs -H "Authorization: Bearer $P" -H "$J" -d '{"engagement_id":4,"phase":"Report","signed_by_id":1,"sign_off_comment":"Report reviewed."}' >/dev/null; check "partner records Report sign-off" "$(c1)" 1
SID=$(python3 -c "import json;print(json.load(open('/tmp/r.json'))['document'])")
code -X PUT $B/Sign_offs/$SID -H "Authorization: Bearer $P" -H "$J" -d '{"engagement_id":4,"phase":"Report","signed_by_id":1,"sign_off_comment":"changed"}' >/dev/null; [ "$(c1)" != "1" ] && check "sign-off can't be edited" blocked blocked || check "sign-off can't be edited" changed blocked
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $M" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "manager can't sign off engagement" "$(c1)" 0
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Signed off')" >/dev/null; check "partner signs off" "$(c1)" 1
code -X PUT $B/Engagements/4 -H "Authorization: Bearer $P" -H "$J" -d "$(eb 'Report')" >/dev/null; check "signed-off engagement is locked" "$(c1)" 0
code -X PUT $B/Engagements/1 -H "Authorization: Bearer $M" -H "$J" -d '{"client_id":1,"engagement_type":"Audit","period_end":"2026-12-31","due_date":"2026-12-01","phase":"Fieldwork","partner_id":1,"manager_id":2,"staff_lead_id":3,"revenue_benchmark":48000000,"materiality_amount":240000,"performance_materiality":180000}' >/dev/null; check "due date before period end refused" "$(c1)" 0
H=$(mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-audit_desk_db} -N -e "SELECT COUNT(*) FROM audit_logs" 2>/dev/null); [ "$H" -gt 0 ] && check "audit log written" yes yes || check "audit log written" "$H rows" ">0"

# ---- attribute rules: allowed references, budgets, approval limits, records created by a step, refusals audited
q() { mysql ${MYSQL_ARGS:--uroot -h127.0.0.1} ${DB:-audit_desk_db} -N -e "$1" 2>/dev/null; }
msg() { python3 -c "import json;print(json.load(open('/tmp/r.json')).get('message',''))"; }
newid() { python3 -c "import json;print(json.load(open('/tmp/r.json'))['document'])"; }
M_=$(date +%Y-%m)
# Ana is assigned to engagement 1 only (she also leads engagement 3)
code "$B/Access/choices/time_entries/engagement_id" -H "Authorization: Bearer $S" >/dev/null
check "picker offers only assigned engagements" "$(python3 -c "import json;d=json.load(open('/tmp/r.json'))['document'];print(d['restricted'],sorted(d['ids']))")" "True ['1']"
te() { echo "{\"engagement_id\":$1,\"staff_member_id\":3,\"work_date\":\"$M_-10\",\"hours\":$2,\"work_description\":\"$3\",\"entry_status\":\"${4:-Draft}\"${5:-}}"; }
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 3 2 'Planning memo')" >/dev/null; check "time on an unassigned engagement refused" "$(c1)" 0
check "refusal message has no table names" "$(msg | grep -c '_')" 0
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 6 'Cash testing')" >/dev/null; check "time on an assigned engagement" "$(c1)" 1; T1=$(newid)
# monthly hours cap: Ana has 10 h a month
code -X PUT $B/Time_entries/$T1 -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 6 'Cash testing' Submitted ",\"time_entry_id\":$T1")" >/dev/null; check "submit 6 h under the 10 h cap" "$(c1)" 1
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 5 'Inventory count')" >/dev/null; T2=$(newid)
code -X PUT $B/Time_entries/$T2 -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 5 'Inventory count' Submitted ",\"time_entry_id\":$T2")" >/dev/null; check "submit 5 h more (11 h) refused" "$(c1)" 0
check "refused entry stays Draft" "$(q "SELECT entry_status FROM time_entries WHERE time_entry_id=$T2")" Draft
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 4 'Payroll walkthrough')" >/dev/null; T3=$(newid)
code -X PUT $B/Time_entries/$T3 -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 4 'Payroll walkthrough' Submitted ",\"time_entry_id\":$T3")" >/dev/null; check "submit 4 h (exactly 10 h) allowed" "$(c1)" 1
# two submissions at the same moment with room for only one
q "UPDATE staff_members SET monthly_hours_cap = 12 WHERE staff_member_id = 3"
sleep 31  # the profile cache lasts 30 s; the cap itself is read under the lock
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 2 'Race A')" >/dev/null; RA=$(newid)
code -X POST $B/Time_entries -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 2 'Race B')" >/dev/null; RB=$(newid)
curl -s -o /tmp/ra.json -X PUT $B/Time_entries/$RA -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 2 'Race A' Submitted ",\"time_entry_id\":$RA")" &
curl -s -o /tmp/rb.json -X PUT $B/Time_entries/$RB -H "Authorization: Bearer $S" -H "$J" -d "$(te 1 2 'Race B' Submitted ",\"time_entry_id\":$RB")" &
wait
check "concurrent submissions: exactly one fits" "$(q "SELECT COUNT(*) FROM time_entries WHERE time_entry_id IN ($RA,$RB) AND entry_status='Submitted'")" 1
check "month total never above the cap" "$(q "SELECT SUM(hours) <= 12 FROM time_entries WHERE staff_member_id=3 AND entry_status IN ('Submitted','Approved')")" 1
# approval limit: Marcus may approve up to 5,000; above that he escalates and a partner approves
fnd() { echo "{\"engagement_id\":1,\"audit_area\":\"Revenue\",\"finding_description\":\"$1\",\"amount\":$2,\"severity\":\"Major\",\"finding_status\":\"$3\"}"; }
code -X POST $B/Findings -H "Authorization: Bearer $S" -H "$J" -d "$(fnd 'Cut-off error on December shipments.' 9000 Draft)" >/dev/null; F2=$(newid)
code -X PUT $B/Findings/$F2 -H "Authorization: Bearer $M" -H "$J" -d "$(fnd 'Cut-off error on December shipments.' 9000 Approved)" >/dev/null; check "manager can't approve above his limit" "$(c1)" 0
check "the refusal explains the limit" "$(msg | grep -c 'approval limit')" 1
code -X PUT $B/Findings/$F2 -H "Authorization: Bearer $M" -H "$J" -d "$(fnd 'Cut-off error on December shipments.' 9000 Escalated)" >/dev/null; check "manager escalates it" "$(c1)" 1
code -X PUT $B/Findings/$F2 -H "Authorization: Bearer $P" -H "$J" -d "$(fnd 'Cut-off error on December shipments.' 9000 Approved)" >/dev/null; check "partner approves the escalated finding" "$(c1)" 1
code -X POST $B/Findings -H "Authorization: Bearer $S" -H "$J" -d "$(fnd 'Small rounding difference.' 100 Draft)" >/dev/null; F3=$(newid)
code -X PUT $B/Findings/$F3 -H "Authorization: Bearer $M" -H "$J" -d "$(fnd 'Small rounding difference.' 100 Escalated)" >/dev/null; check "within the limit it can't be escalated" "$(c1)" 0
# the partner's sign-off of engagement 4 (above) created its invoice, once, numbered, with the fee
check "sign-off created one invoice" "$(q "SELECT CONCAT(COUNT(*),' ',MIN(invoice_number),' ',MIN(amount)) FROM invoices WHERE engagement_id=4")" "1 INV-$(date +%Y)-00001 18000.00"
code "$B/Invoices?pageNo=1&pageSize=50" -H "Authorization: Bearer $C" >/dev/null; check "client doesn't see another client's invoice" "$(n)" 0
code "$B/Invoices?pageNo=1&pageSize=50" -H "Authorization: Bearer $P" >/dev/null; check "partner sees the invoice" "$(n)" 1
# refusals and the person behind each step are recorded
check "refused approval is in the audit log" "$(q "SELECT COUNT(*) > 0 FROM audit_logs WHERE action_code='findings.denied' AND actor_ref='marcus.lee' AND details LIKE '%approval limit%'")" 1
check "refused time entry is in the audit log" "$(q "SELECT COUNT(*) > 0 FROM audit_logs WHERE action_code='time_entries.denied' AND actor_ref='ana.torres'")" 1
check "phase history names who signed off" "$(q "SELECT changed_by FROM engagement_phase_history WHERE engagement_id=4 AND new_phase='Signed off'")" priya.shah
echo "passed $pass, failed $fail"
