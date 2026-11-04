"""#79 validations on the ExpertOS sample (run seed-expertos.py first). Usage: validation_test.py <admin> <pw>"""
import json, random, sys, urllib.request
B = "http://localhost:5100/v1/api/"
def call(m, p, b=None, t=None):
    r = urllib.request.Request(B + p, method=m, data=json.dumps(b).encode() if b is not None else None,
                               headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + t} if t else {})})
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read() or b"null")
    except urllib.error.HTTPError as e:
        try: return e.code, json.loads(e.read())
        except Exception: return e.code, None
ADM = call("POST", "Token", {"username": sys.argv[1], "password": sys.argv[2]})[1]["document"]["accessToken"]
RUN = str(random.randint(1000, 9999)); res = []
def check(name, ok, detail=""): res.append(bool(ok)); print(("PASS " if ok else "FAIL ") + name + ("" if ok else f"  [{detail}]"))
def errs(r): return (r[1] or {}).get("errors") or {}
def cust(**kw):
    body = {"tenant_id": 1, "customer_code": "C-" + RUN, "name": "Acme " + RUN, "customer_type": "business", "status": "active"}
    body.update(kw); return call("POST", "Customers", body, ADM)
r = cust(billing_email="not-an-email", customer_code="bad code!")
check("bad email and pattern -> 400 with both fields", r[0] == 400 and {"billing_email", "customer_code"} <= set(errs(r)), r)
check("custom message is returned", "Use 2-20 capitals" in str(errs(r).get("customer_code")), errs(r))
check("default message for email", "valid email" in str(errs(r).get("billing_email")), errs(r))
r = cust(billing_email="ap@acme.example"); cid = r[1].get("document") if r[0] == 200 else None
check("valid customer is created", r[0] == 200 and cid, r)
def inv(**kw):
    body = {"tenant_id": 1, "customer_id": 1, "invoice_number": "INV-" + RUN + str(random.randint(0, 999)), "subtotal": 100, "tax": 15, "discount_amount": 10, "total": 105, "currency": "USD", "status": "draft"}
    body.update(kw); return call("POST", "Customer_invoices", body, ADM)
r = inv(total=999)
check("computed check: total must equal subtotal + tax - discount", r[0] == 400 and "105" in str(errs(r).get("total")), r)
r = inv(discount_amount=500, total=-385)
check("compare: discount can't exceed subtotal", r[0] == 400 and "discount_amount" in errs(r), r)
r = inv(subtotal=-5, total=0)
check("min: subtotal >= 0", r[0] == 400 and "subtotal" in errs(r), r)
r = inv(currency="usd")
check("pattern: currency code", r[0] == 400 and "currency" in errs(r), r)
r = inv(issued_at="2026-11-10 00:00:00", due_date="2026-11-01")
check("date order: due date before issue date is rejected", r[0] == 400 and "due_date" in errs(r), r)
r = inv(); iid = r[1].get("document") if r[0] == 200 else None
check("valid invoice is created", r[0] == 200 and iid, r)
row = call("GET", f"Customer_invoices/{iid}", t=ADM)[1]["document"]
row2 = dict(row, notes=None) if "notes" in row else dict(row); row2["total"] = 1
r = call("PUT", f"Customer_invoices/{iid}", row2, ADM)
check("update is validated too", r[0] == 400 and "total" in errs(r), r)
call("POST", "Access/members", {"username": "fin" + RUN, "password": "Demo!2026x", "role_id": {x["role_name"]: x["role_id"] for x in call("GET", "Access/roles", t=ADM)[1]["document"]}["Finance Officer"], "tenant_id": 1}, ADM)
FIN = call("POST", "Token", {"username": "fin" + RUN, "password": "Demo!2026x"})[1]["document"]["accessToken"]
r = call("PUT", f"Customer_invoices/{iid}", dict(row, status="issued"), FIN)
check("requiredWhen: issuing needs issued_at", r[0] == 400 and "issued_at" in errs(r), r)
r = call("PUT", f"Customer_invoices/{iid}", dict(row, status="issued", issued_at="2026-11-03 09:00:00"), FIN)
check("issuing with issued_at works", r[0] == 200, r)
# rules only for changed columns: an old row that breaks a rule can still be edited elsewhere
import subprocess
subprocess.run(["mysql", "-uroot", "-pneoapps", "-h127.0.0.1", "expert_os_db", "-e", f"UPDATE customers SET billing_email='legacy' WHERE customer_id={cid}"], capture_output=True)
row = call("GET", f"Customers/{cid}", t=ADM)[1]["document"]
r = call("PUT", f"Customers/{cid}", dict(row, notes="edited"), ADM)
check("old row with a bad value can still be edited (only changed columns are checked)", r[0] == 200, r)
print(f"{sum(res)}/{len(res)} passed")
