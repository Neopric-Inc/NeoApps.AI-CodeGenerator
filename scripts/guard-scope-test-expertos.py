#!/usr/bin/env python3
"""#75 row scopes on ExpertOS: Customer Users only reach their own customer's rows inside their tenant.
Usage: scope_test.py <admin_user> <admin_password>  (after seed.py)"""
import json, random, sys, urllib.request
B = "http://localhost:5100/v1/api/"
def call(m, p, b=None, t=None):
    r = urllib.request.Request(B + p, method=m, data=json.dumps(b).encode() if b is not None else None,
                               headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + t} if t else {})})
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read() or b"null")
    except urllib.error.HTTPError as e:
        body = e.read()
        try: return e.code, json.loads(body)
        except Exception: return e.code, body[:200]
def tok(u, p): return call("POST", "Token", {"username": u, "password": p})[1]["document"]["accessToken"]
def doc(r): return r[1].get("document") if isinstance(r[1], dict) else None
def recs(r): d = doc(r); return d.get("records", []) if isinstance(d, dict) else []
def add(table, t, **row):
    st, r = call("POST", table, row, t)
    if st != 200: sys.exit(f"setup failed {table}: {st} {r}")
    return doc((st, r))
res = []
def check(name, ok, detail=""):
    ok = bool(ok); res.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{str(detail)[:300]}]" if not ok else ""))

ADM = tok(sys.argv[1], sys.argv[2]); RUN = str(random.randint(1000, 9999))
roles = {r["role_name"]: r["role_id"] for r in doc(call("GET", "Access/roles", t=ADM))}
T = {"tenant_id": 1}
# second customer of the same tenant with its own case, document, task and invoice
custA = 1
custB = add("Customers", ADM, **T, customer_code="C-B" + RUN, name="Bayside Foods", customer_type="business", status="active")
caseA = 1
caseB = add("Cases", ADM, **T, case_number="EXP-2026-B" + RUN, customer_id=custB, vertical_id=5, service_id=1, title="Bayside sourcing", status="draft", priority="normal")
docB = add("Documents", ADM, **T, case_id=caseB, customer_id=custB, document_type_id=1, title="Bayside quote", status="uploaded", classification="confidential")
delB = add("Case_deliverables", ADM, **T, case_id=caseB, title="Bayside report", output_format="pdf", version_number=1, status="draft")
invB = add("Customer_invoices", ADM, **T, customer_id=custB, case_id=caseB, invoice_number="INV-B" + RUN, subtotal=10, tax=0, total=10, currency="USD", status="draft")

def member(u, role, customer=None):
    u = u + RUN
    call("POST", "Access/members", {"username": u, "password": "Scope!2026x", "role_id": roles[role], "tenant_id": 1}, ADM)
    if customer: add("Tenant_users", ADM, **T, login_username=u, first_name=u, last_name="Portal", email=u + "@x.example", user_type="customer", customer_id=customer, status="active")
    return tok(u, "Scope!2026x")
CA = member("dana", "Customer User", custA)
CB = member("bo", "Customer User", custB)
NOCUST = member("nolink", "Customer User")
EXP = member("exp", "Expert")

ids = lambda r, k: [x[k] for x in recs(r)]
check("customer A lists only its own cases", ids(call("GET", "Cases", t=CA), "customer_id") and set(ids(call("GET", "Cases", t=CA), "customer_id")) == {custA}, ids(call("GET", "Cases", t=CA), "customer_id"))
check("customer B lists only its own cases", ids(call("GET", "Cases", t=CB), "case_id") == [caseB], ids(call("GET", "Cases", t=CB), "case_id"))
st, r = call("GET", f"Cases/{caseB}", t=CA)
check("customer A can't open customer B's case by id", not doc((st, r)), (st, r))
check("search stays inside the customer", all(x["customer_id"] == custA for x in recs(call("GET", "Cases/search?searchKey=a", t=CA))))
check("documents (direct column) scoped", docB not in ids(call("GET", "Documents?itemsPerPage=500", t=CA), "document_id"))
check("deliverables (linked only through cases) scoped", delB not in ids(call("GET", "Case_deliverables?itemsPerPage=500", t=CA), "case_deliverable_id") and delB in ids(call("GET", "Case_deliverables?itemsPerPage=500", t=CB), "case_deliverable_id"), (ids(call("GET", "Case_deliverables", t=CA), "case_id"), ids(call("GET", "Case_deliverables", t=CB), "case_id")))
check("invoices scoped", invB not in ids(call("GET", "Customer_invoices?itemsPerPage=500", t=CA), "customer_invoice_id"))
check("home table: customer sees only its own customer record", ids(call("GET", "Customers?itemsPerPage=500", t=CA), "customer_id") == [custA], ids(call("GET", "Customers", t=CA), "customer_id"))
check("unrelated catalog tables stay readable (services)", len(recs(call("GET", "Services", t=CA))) >= 1)
st, r = call("POST", "Cases", {**T, "case_number": "EXP-2026-X" + RUN, "customer_id": custB, "vertical_id": 5, "service_id": 1, "title": "spoof", "status": "draft", "priority": "normal"}, CA)
row = doc(call("GET", f"Cases/{doc((st, r))}", t=ADM)) if st == 200 else None
check("customer_id sent by the customer is replaced with their own", st == 200 and row and row["customer_id"] == custA, (st, r, row and row["customer_id"]))
st, r = call("POST", "Case_messages", {**T, "case_id": caseB, "sender_type": "customer", "message_text": "hi"}, CA)
check("can't attach a message to another customer's case", st == 403, (st, r))
full = doc(call("GET", f"Cases/{caseB}", t=ADM)); full["title"] = "HACKED"
call("PUT", f"Cases/{caseB}", full, CA)
check("can't update another customer's case", doc(call("GET", f"Cases/{caseB}", t=ADM))["title"] == "Bayside sourcing")
call("DELETE", f"Documents/{docB}", t=CA)
check("can't delete another customer's document", doc(call("GET", f"Documents/{docB}", t=ADM)) is not None)
st, r = call("POST", "Customers", {**T, "customer_code": "Z" + RUN, "name": "new co", "customer_type": "business", "status": "active"}, CA)
check("scoped user can't create new home records (customers)", st in (401, 403), (st, r))
check("customer user without a link sees nothing", len(recs(call("GET", "Cases", t=NOCUST))) == 0 and len(recs(call("GET", "Customers", t=NOCUST))) == 0)
st, r = call("POST", "Cases", {**T, "case_number": "EXP-2026-N" + RUN, "customer_id": custA, "vertical_id": 5, "service_id": 1, "title": "x", "status": "draft", "priority": "normal"}, NOCUST)
check("customer user without a link can't create", st == 403, (st, r))
check("other roles are unaffected (expert sees both customers)", {custA, custB} <= set(ids(call("GET", "Cases?itemsPerPage=500", t=EXP), "customer_id")))
# AI files: attach to B's document as admin, then customer A can't list or search it
boundary = "----s" + RUN
body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"table\"\r\n\r\ndocuments\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"recordId\"\r\n\r\n{docB}\r\n"
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"bayside.txt\"\r\nContent-Type: text/plain\r\n\r\nBayside secret pricing 9.99 per unit.\r\n--{boundary}--\r\n").encode()
req = urllib.request.Request(B + "Ai/files", data=body, method="POST", headers={"Content-Type": f"multipart/form-data; boundary={boundary}", "Authorization": "Bearer " + ADM})
fid = json.loads(urllib.request.urlopen(req).read()).get("file_id")
st, r = call("GET", f"Ai/files?table=documents&recordId={docB}", t=CA)
check("AI files of another customer's record are hidden", st in (400, 403, 404) or not r, (st, r))
st, r = call("GET", f"Ai/files/{fid}/link", t=CA)
check("no signed link for another customer's file", st != 200, (st, r))
import subprocess
sql = lambda q: subprocess.run(["mysql", "-h127.0.0.1", "-uroot", "-pneoapps", "-N", "expert_os_db", "-e", q], capture_output=True, text=True).stdout.strip()
print(f"\n{sum(res)}/{len(res)} passed")
