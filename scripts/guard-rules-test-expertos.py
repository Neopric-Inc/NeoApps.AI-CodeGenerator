"""DataGuard rule tests: lifecycles, evidence requirements, frozen states, append-only + read-only data, audit, outbox,
SLA deadlines + breach job, plan limits, signed file links. Run seed-expertos.py first. Usage: guard-rules-test-expertos.py <admin_user> <admin_password>"""
import json, random, subprocess, sys, time, urllib.parse, urllib.request
B = "http://localhost:5100/v1/api/"
def call(m, p, b=None, t=None, raw=False):
    r = urllib.request.Request(B + p, method=m, data=json.dumps(b).encode() if b is not None else None,
                               headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + t} if t else {})})
    try:
        with urllib.request.urlopen(r) as x: d = x.read(); return x.status, (d if raw else json.loads(d or b"null"))
    except urllib.error.HTTPError as e:
        body = e.read()
        try: return e.code, json.loads(body)
        except Exception: return e.code, body[:200]
def tok(u, p): return call("POST", "Token", {"username": u, "password": p})[1]["document"]["accessToken"]
def doc(r): return r[1].get("document") if isinstance(r[1], dict) else None
def recs(r): d = doc(r); return d.get("records", []) if isinstance(d, dict) else []
def C(t): return t[0].upper() + t[1:]
def get(table, i, t): return doc(call("GET", f"{C(table)}/{i}", t=t))
def put(table, i, t, **ch):
    row = get(table, i, ADM); row.update(ch); return call("PUT", f"{C(table)}/{i}", row, t)
def msg(r): return r[1].get("message") if isinstance(r[1], dict) else r[1]

ADM = tok(sys.argv[1], sys.argv[2]); RUN = str(random.randint(1000, 9999))
roles = {r["role_name"]: r["role_id"] for r in doc(call("GET", "Access/roles", t=ADM))}
def member(u, role, tenant=1):
    call("POST", "Access/members", {"username": u + RUN, "password": "Demo!2026x", "role_id": roles[role], "tenant_id": tenant}, ADM)
    return tok(u + RUN, "Demo!2026x")
EXP, QA, REV, OPS, CUST, FIN = (member("exp", "Expert"), member("qa", "QA Officer"), member("rev", "Reviewer"),
                                member("ops", "Operations Manager"), member("cust", "Customer User"), member("fin", "Finance Officer"))
res = []
def check(name, ok, detail=""):
    ok = bool(ok); res.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{detail}]" if not ok else ""))

case = 1
# --- lifecycle: allowed steps, roles, evidence
st, r = put("cases", case, EXP, status="delivered")
check("invalid step (expert_review -> delivered) is rejected", st == 409, (st, msg((st, r))))
st, r = put("cases", case, CUST, status="quality_assurance")
check("role not allowed for a step is rejected", st in (401, 403), (st, msg((st, r))))
st, r = put("cases", case, EXP, status="quality_assurance")
check("step needing evidence is blocked until it exists (submitted expert review)", st == 409 and "expert review" in str(msg((st, r))), (st, msg((st, r))))
put("expert_reviews", 1, EXP, status="submitted")
st, r = put("cases", case, EXP, status="quality_assurance")
check("step allowed once evidence exists", st == 200, (st, msg((st, r))))
hist = [(h["old_status"], h["new_status"], h["changed_by_type"]) for h in recs(call("GET", "Case_status_history?itemsPerPage=200", t=ADM)) if h["case_id"] == case]
check("status history written automatically for every step", ("expert_review", "quality_assurance", "human") in hist and len(hist) >= 7, hist)
st, r = put("cases", case, QA, status="final_approval")
check("QA can't approve before a human QA review passes", st == 409, (st, msg((st, r))))
aiqa = doc(call("POST", "Quality_reviews", {"tenant_id": 1, "case_id": case, "reviewer_type": "ai_precheck", "status": "in_progress"}, ADM))
st, r = put("quality_reviews", aiqa, QA, status="passed")
check("an AI pre-check can't be marked passed", st == 409, (st, msg((st, r))))
st, r = call("POST", "Quality_reviews", {"tenant_id": 1, "case_id": case, "reviewer_type": "human", "status": "passed"}, QA)
check("records can't be created already in a later state", st == 409, (st, msg((st, r))))
st, r = put("quality_reviews", 1, QA, status="passed")
check("human QA review can pass", st == 200, (st, msg((st, r))))
st, r = put("cases", case, QA, status="final_approval")
check("case moves to final approval after QA passes", st == 200, (st, msg((st, r))))
st, r = put("cases", case, OPS, status="delivered")
check("delivery blocked without final-deliverable approval", st == 409, (st, msg((st, r))))
appr = doc(call("POST", "Approvals", {"tenant_id": 1, "case_id": case, "approval_type": "final_deliverable", "status": "pending"}, EXP))
st, r = put("approvals", appr, EXP, status="approved")
check("expert can't approve (separation of duties by role)", st in (401, 403), (st, msg((st, r))))
check("reviewer approves", put("approvals", appr, REV, status="approved")[0] == 200)
# --- separation of duties (distinctFrom), negative requirements (forbids), admin bypass (#59)
def person(login): return doc(call("POST", "Tenant_users", {"tenant_id": 1, "login_username": login, "first_name": "T", "last_name": login, "email": login + "@x.test", "user_type": "employee", "status": "active"}, ADM))
rev_person = person("rev" + RUN)
own = doc(call("POST", "Approvals", {"tenant_id": 1, "case_id": case, "approval_type": "scope_change", "status": "pending", "requested_by_id": rev_person}, EXP))
st, r = put("approvals", own, REV, status="approved")
check("the requester can't decide their own approval (distinctFrom)", st == 409 and "someone else" in str(msg((st, r))), (st, msg((st, r))))
st, r = put("approvals", own, QA, status="approved")
check("another allowed role can decide it", st == 200, (st, msg((st, r))))
TADM = member("tadm", "Administrator")
pend = doc(call("POST", "Approvals", {"tenant_id": 1, "case_id": case, "approval_type": "scope_change", "status": "pending"}, EXP))
st, r = put("approvals", pend, TADM, status="approved")
check("a tenant-bound administrator gets no bypass where adminBypass is \"platform\"", st == 403, (st, msg((st, r))))
st, r = put("approvals", pend, ADM, status="approved")
check("the platform administrator still can", st == 200, (st, msg((st, r))))
inv = doc(call("POST", "Customer_invoices", {"tenant_id": 1, "customer_id": 1, "invoice_number": "INV-" + RUN, "currency": "USD", "subtotal": 1, "tax": 0, "total": 1, "status": "draft"}, FIN))
st, r = put("customer_invoices", inv, ADM, status="issued", issued_at="2026-11-03 09:00:00")
check("adminBypass false: the administrator can't issue an invoice", st == 403, (st, msg((st, r))))
st, r = put("customer_invoices", inv, FIN, status="issued", issued_at="2026-11-03 09:00:00")
check("the Finance Officer can", st == 200, (st, msg((st, r))))
rej = doc(call("POST", "Approvals", {"tenant_id": 1, "case_id": case, "approval_type": "final_deliverable", "status": "pending"}, EXP))
put("approvals", rej, REV, status="rejected")
put("case_deliverables", 1, EXP, status="in_review")
check("reviewer approves the deliverable", put("case_deliverables", 1, REV, status="approved")[0] == 200)
st, r = put("cases", case, OPS, status="delivered")
check("delivery blocked while a final-deliverable approval is rejected (forbids)", st == 409 and "rejected" in str(msg((st, r))), (st, msg((st, r))))
call("DELETE", f"Approvals/{rej}", t=ADM)
st, r = put("cases", case, OPS, status="delivered")
row = get("cases", case, ADM)
check("delivery succeeds once QA, approval and deliverable are in place; delivered_at stamped", st == 200 and row["delivered_at"], (st, msg((st, r)), row and row["delivered_at"]))
put("case_deliverables", 1, OPS, status="delivered")
st, r = put("case_deliverables", 1, OPS, title="changed after delivery")
check("delivered deliverable is frozen", st == 409, (st, msg((st, r))))
put("cases", case, OPS, status="closed")
st, r = put("cases", case, ADM, title="edit closed case")
check("closed case is frozen (even for admins)", st == 409, (st, msg((st, r))))
dl = [(d["deadline_type"], bool(d["met_at"])) for d in recs(call("GET", "Case_deadlines?itemsPerPage=200", t=ADM)) if d["case_id"] == case]
check("SLA deadlines opened on entering states and closed on leaving", ("expert_review", True) in dl and ("quality_assurance", True) in dl, dl)

# --- read-only and append-only data
st, r = put("ai_findings", 1, EXP, ai_description="rewritten by human")
check("AI output column is read-only", st == 409, (st, msg((st, r))))
st, r = put("ai_findings", 1, EXP, expert_description="expert version", review_status="edited")
check("expert version can be edited beside it", st == 200, (st, msg((st, r))))
aud = recs(call("GET", "Audit_logs?itemsPerPage=500", t=ADM))
st, r = put("audit_logs", aud[0]["audit_log_id"], ADM, action_code="tampered")
check("audit log is append-only (even for admins)", st == 409, (st, msg((st, r))))
st, r = call("DELETE", f"Case_status_history/{hist and recs(call('GET', 'Case_status_history', t=ADM))[0]['case_status_history_id']}", t=ADM)
check("status history can't be deleted", st == 409, (st, msg((st, r))))
acts = {a["action_code"] for a in aud}
check("audit rows written automatically for creates and updates", {"cases.create", "cases.update", "ai_findings.update"} <= acts, sorted(acts)[:12])
mine = [a for a in recs(call("GET", "Audit_logs?itemsPerPage=500", t=ADM)) if a["action_code"] == "ai_findings.update"]
check("audit row records who and what changed", mine and mine[-1]["actor_ref"].startswith("exp") and "expert_description" in mine[-1]["details"], mine[-1:] )

# --- outbox
ev = recs(call("GET", "Outbox_events?itemsPerPage=500", t=ADM))
types = {e["event_type"] for e in ev}
check("domain events written to the outbox in the same transaction", {"CaseSubmitted", "DocumentUploaded", "QualityReviewCompleted", "DeliverableApproved", "DeliverableDelivered"} <= types, sorted(types))
time.sleep(8)
ev = recs(call("GET", "Outbox_events?itemsPerPage=500", t=ADM))
pub = sum(1 for e in ev if e["status"] == "published")
check("outbox publisher delivered events to RabbitMQ", pub == len(ev) and pub > 0, (pub, len(ev), {e["status"] for e in ev}))
q = subprocess.run(["sudo", "rabbitmqctl", "list_exchanges", "name", "type"], capture_output=True, text=True).stdout
check("RabbitMQ topic exchange declared", "expertos.events\ttopic" in q, q[-200:])

# --- deadline breach job
st, r = call("POST", "Guard/jobs/run", {}, ADM)
dl = [d for d in recs(call("GET", "Case_deadlines?itemsPerPage=200", t=ADM)) if d["deadline_type"] == "customer_response"]
esc = [e for e in recs(call("GET", "Case_escalations?itemsPerPage=200", t=ADM)) if "customer_response" in (e["reason"] or "")]
check("overdue deadline flagged as breached", dl and dl[0]["is_breached"] == 1, dl)
check("escalation opened automatically for the breach", len(esc) == 1 and esc[0]["status"] == "open", esc)
call("POST", "Guard/jobs/run", {}, ADM)
esc = [e for e in recs(call("GET", "Case_escalations?itemsPerPage=200", t=ADM)) if "customer_response" in (e["reason"] or "")]
check("breach job is idempotent (no duplicate escalation)", len(esc) == 1, len(esc))

# --- plan limits (Growth: 10 seats, 2,500 AI credits/month) as a tenant user
OWN = member("own", "Organization Owner")
seats_used = sum(1 for u in recs(call("GET", "Tenant_users?itemsPerPage=500", t=OWN)) if u["user_type"] == "employee")
codes = []
for i in range(10 - seats_used):
    codes.append(call("POST", "Tenant_users", {"tenant_id": 1, "first_name": "Seat", "last_name": str(i), "email": f"s{i}{RUN}@x.example", "user_type": "employee", "status": "active"}, OWN)[0])
st, r = call("POST", "Tenant_users", {"tenant_id": 1, "first_name": "One", "last_name": "Toomany", "email": f"over{RUN}@x.example", "user_type": "employee", "status": "active"}, OWN)
check("employee seats limited by the active plan", all(c == 200 for c in codes) and st == 402, (codes, st, msg((st, r))))
st, r = call("POST", "Tenant_users", {"tenant_id": 1, "first_name": "Portal", "last_name": "User", "email": f"p{RUN}@x.example", "user_type": "customer", "status": "active"}, OWN)
check("limit only counts matching rows (customer portal users are free)", st == 200, (st, msg((st, r))))
st, r = call("POST", "Saas_usage_records", {"tenant_id": 1, "metric_code": "ai_credits", "quantity": 2600, "is_billable": 1}, ADM)
check("platform admin is not limited", st == 200, (st, msg((st, r))))

# --- signed file links (AI file storage)
boundary = "----g" + RUN
body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"table\"\r\n\r\ndocuments\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"recordId\"\r\n\r\n1\r\n"
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"abc_quote.txt\"\r\nContent-Type: text/plain\r\n\r\nABC Industrial quote: Steel Component A $12.50/unit, MOQ 500, 21 days, Net 30.\r\n--{boundary}--\r\n").encode()
req = urllib.request.Request(B + "Ai/files", data=body, method="POST", headers={"Content-Type": f"multipart/form-data; boundary={boundary}", "Authorization": "Bearer " + EXP})
f = json.loads(urllib.request.urlopen(req).read())
fid = f.get("file_id") or f.get("fileId") or (f.get("file") or {}).get("file_id")
link = call("GET", f"Ai/files/{fid}/link", t=EXP)[1]
url = link.get("url", "")
ok = urllib.request.urlopen(url).read()
check("signed link downloads the file without logging in", b"Steel Component A" in ok, ok[:60])
bad = url.replace("sig=", "sig=0")
try: urllib.request.urlopen(bad); code = 200
except urllib.error.HTTPError as e: code = e.code
check("tampered signature is refused", code == 403, code)
exp_url = url.split("?")[0] + "?exp=1&sig=" + url.split("sig=")[1]
try: urllib.request.urlopen(exp_url); code = 200
except urllib.error.HTTPError as e: code = e.code
check("expired link is refused", code == 403, code)
path = subprocess.run(["bash", "-c", "find / -path /proc -prune -o -name '*.txt' -path '*tenant-1*' -newer /tmp/api.log -print 2>/dev/null | head -1"], capture_output=True, text=True).stdout.strip()
check("file stored under the tenant's own folder", "tenant-1" in path, path)

rules = doc(call("GET", "Guard/rules", t=ADM))
print("\nrules in force:", json.dumps({k: rules[k] for k in ("audit", "outbox", "outboxStats")}))
print(f"\n{sum(res)}/{len(res)} passed")
