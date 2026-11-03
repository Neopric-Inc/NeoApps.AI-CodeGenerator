"""#80 numbering on the ExpertOS sample (run seed-expertos.py first). Usage: numbering_test.py <admin> <pw>"""
import json, random, re, subprocess, sys, time, urllib.request
from concurrent.futures import ThreadPoolExecutor
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
YEAR = time.gmtime().tm_year; res = []
def check(name, ok, detail=""): res.append(bool(ok)); print(("PASS " if ok else "FAIL ") + name + ("" if ok else f"  [{detail}]"))
def db(q): return subprocess.run(["mysql", "-uroot", "-pneoapps", "-h127.0.0.1", "-N", "expert_os_db", "-e", q], capture_output=True, text=True).stdout.strip()
def inv(tenant=1, customer=1, **kw):
    body = {"tenant_id": tenant, "customer_id": customer, "subtotal": 100, "tax": 15, "discount_amount": 10, "total": 105, "currency": "USD", "status": "draft"}
    body.update(kw); return call("POST", "Customer_invoices", body, ADM)
def number(i): return call("GET", f"Customer_invoices/{i}", t=ADM)[1]["document"]["invoice_number"]
def seq(n): return int(n.rsplit("-", 1)[1])

case_no = call("GET", "Cases/1", t=ADM)[1]["document"]["case_number"]
check("seeded case got its number from the pattern", re.fullmatch(rf"EXP-{YEAR}-\d{{6}}", case_no or ""), case_no)
st, r = inv(); a = r.get("document")
na = number(a) if st == 200 else None
check("invoice created without a number gets INV-<year>-NNNNN", st == 200 and re.fullmatch(rf"INV-{YEAR}-\d{{5}}", na or ""), (st, r, na))
st, r = inv(invoice_number="HACKED-1"); b = r.get("document")
nb = number(b)
check("a number sent by the client is ignored; the next one is used", nb != "HACKED-1" and seq(nb) == seq(na) + 1, (na, nb))
row = call("GET", f"Customer_invoices/{b}", t=ADM)[1]["document"]
st, r = call("PUT", f"Customer_invoices/{b}", dict(row, invoice_number="INV-0000-1"), ADM)
check("the number is read-only after creation", st == 409, (st, r))
st, r = call("PUT", f"Customer_invoices/{b}", dict(row, notes="edited") if "notes" in row else dict(row, currency="CAD"), ADM)
check("other fields can still be edited", st == 200, (st, r))
st, r = inv(total=1)
check("a rejected insert (validation) ...", st == 400, (st, r))
st, r = inv(); nc = number(r["document"])
check("... consumes no number (gap-free)", seq(nc) == seq(nb) + 1, (nb, nc))
with ThreadPoolExecutor(20) as ex: out = list(ex.map(lambda _: inv(), range(20)))
nums = sorted(seq(number(r[1]["document"])) for r in out if r[0] == 200)
check("20 concurrent inserts: all succeed with distinct, consecutive numbers", len(nums) == 20 and nums == list(range(seq(nc) + 1, seq(nc) + 21)), nums)
# per-tenant sequences
st, r = call("POST", "Saas_tenants", {"tenant_code": "T2X" + str(random.randint(100, 999)), "name": "Second", "legal_name": "Second Inc.", "primary_email": "ops@second.example",
                                      "country": "Canada", "region": "ca", "timezone": "UTC", "deployment_model": "shared", "status": "active"}, ADM)
t2 = r.get("document")
st, r = call("POST", "Customers", {"tenant_id": t2, "customer_code": "C-T2", "name": "T2 customer", "customer_type": "business", "status": "active"}, ADM)
st, r = inv(tenant=t2, customer=r.get("document"))
n2 = number(r["document"]) if st == 200 else None
check("another tenant has its own sequence (starts at 00001)", n2 == f"INV-{YEAR}-00001", (st, r, n2))
# a new sequence continues after numbers already stored (e.g. typed in before numbering was turned on)
db(f"UPDATE customer_invoices SET invoice_number='INV-{YEAR}-00500' WHERE customer_invoice_id={a}")
db("DELETE FROM number_sequences WHERE seq_key LIKE 'customer_invoices.invoice_number|1|%'")
st, r = inv()
check("a fresh sequence continues after the highest stored number", number(r["document"]) == f"INV-{YEAR}-00501", number(r["document"]))
q = call("GET", "Customer_quotes?itemsPerPage=5", t=ADM)[1]["document"]["records"]
check("quotes are numbered too (Q-<year>-NNNN)", q and all(re.fullmatch(rf"Q-{YEAR}-\d{{4}}", x["quote_number"]) for x in q), [x["quote_number"] for x in q])
audit = db(f"SELECT details FROM audit_logs WHERE entity_type='customer_invoices' AND action_code LIKE '%create' ORDER BY audit_log_id DESC LIMIT 1")
check("the audit row records the assigned number, not the placeholder", f"INV-{YEAR}-00501" in audit, audit[:200])
print(f"{sum(res)}/{len(res)} passed")
