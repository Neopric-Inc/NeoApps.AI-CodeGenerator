#!/usr/bin/env python3
"""Cross-tenant isolation tests for the DataGuard generator feature. Run seed-expertos.py first. Usage: tenancy-test-expertos.py <admin_user> <admin_password>"""
import json, sys, urllib.request, urllib.parse
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
def records(r): d = doc(r); return d.get("records", []) if isinstance(d, dict) else []

ADM = tok(sys.argv[1], sys.argv[2])
import random; RUN = str(random.randint(1000, 9999))
roles = {r["role_name"]: r["role_id"] for r in doc(call("GET", "Access/roles", t=ADM))}

# second tenant with its own customer and case (created by the platform admin)
t2 = doc(call("POST", "Saas_tenants", {"tenant_code": "HARBOR" + RUN, "name": "Harbor Claims Services", "deployment_model": "shared", "status": "active"}, ADM))
cust2 = doc(call("POST", "Customers", {"tenant_id": t2, "customer_code": "H-" + RUN, "name": "Bayview Logistics", "customer_type": "business", "status": "active"}, ADM))
sv = doc(call("POST", "Services", {"tenant_id": t2, "vertical_id": 1, "service_code": "CLM" + RUN, "name": "Claims intake", "status": "active", "requires_human_review": 1, "requires_qa": 1}, ADM))
case2 = doc(call("POST", "Cases", {"tenant_id": t2, "case_number": "EXP-2026-1" + RUN, "customer_id": cust2, "vertical_id": 5, "service_id": sv,
                                    "title": "Harbor case", "status": "draft", "priority": "normal"}, ADM))
N2 = doc(call("GET", f"Cases/{case2}", t=ADM))["case_number"]  # assigned by the API when "numbering" is on
print(f"tenant 2 = {t2}, customer {cust2}, case {case2}")

def member(u, role, tenant):
    call("POST", "Access/members", {"username": u, "password": "Demo!2026x", "role_id": roles[role], "tenant_id": tenant}, ADM)
    return tok(u, "Demo!2026x")
carol = member("carol.t1" + RUN, "Expert", 1)            # tenant 1 expert
bob = member("bob.t2" + RUN, "Organization Owner", t2)   # tenant 2 owner (full CRUD on tenant data)
alice = member("alice.t1admin" + RUN, "Administrator", 1)  # tenant 1 administrator

results = []
def check(name, ok, detail=""):
    results.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{detail}]" if detail and not ok else ""))

me = doc(call("GET", "Access/me", t=bob))
check("token carries tenant from server mapping", me["tenant_id"] == t2, me)
c1 = [r["case_number"] for r in records(call("GET", "Cases", t=carol))]
check("tenant 1 user lists only tenant 1 cases", c1 and all(not n.startswith("EXP-2026-1") for n in c1), c1)
c2 = [r["case_number"] for r in records(call("GET", "Cases", t=bob))]
check("tenant 2 user lists only tenant 2 cases", c2 == [N2], c2)
st, r = call("GET", "Cases/1", t=bob)
check("tenant 2 cannot read tenant 1 case by id", doc((st, r)) in (None, {}) or st == 404, r)
st, r = call("GET", "Cases/search?searchKey=a", t=bob)
check("search can't leak across tenants (OR precedence)", all(x["tenant_id"] == t2 for x in records((st, r))), r)
st, r = call("POST", "Cases/filter?andOr=OR", [{"ColumnName": "case_number", "ColumnCondition": 1, "ColumnValue": "%EXP%"}, {"ColumnName": "title", "ColumnCondition": 1, "ColumnValue": "%"}], bob)
check("filter with OR stays in tenant", all(x["tenant_id"] == t2 for x in records((st, r))), r)
st, r = call("POST", "Cases/filter?andOr=AND", [{"ColumnName": "1=1 OR title", "ColumnCondition": 1, "ColumnValue": "x"}], bob)
check("filter column injection rejected", st in (400, 403), (st, r))
st, r = call("POST", "Cases/filter?andOr=AND", [{"ColumnName": "title", "ColumnCondition": 1, "ColumnValue": "x' OR '1'='1"}], bob)
check("filter value injection is just data", st == 200 and len(records((st, r))) == 0, (st, r))
st, r = call("GET", "Cases?orderBy=" + urllib.parse.quote("case_id;DROP TABLE x"), t=bob)
check("orderBy injection ignored (only safe identifiers reach SQL)", st != 200 or all(x["tenant_id"] == t2 for x in records((st, r))), (st, r))

body = {"tenant_id": 1, "case_number": "EXP-2026-2" + RUN, "customer_id": cust2, "vertical_id": 5, "service_id": sv, "title": "spoofed tenant", "status": "draft", "priority": "normal"}
st, r = call("POST", "Cases", body, bob)
new_id = doc((st, r))
row = doc(call("GET", f"Cases/{new_id}", t=ADM)) if st == 200 else None
check("tenant_id in request body is ignored (forced to caller's tenant)", st == 200 and row and row["tenant_id"] == t2, (st, r, row))
st, r = call("POST", "Cases", dict(body, case_number="EXP-2026-3" + RUN, customer_id=1), bob)
check("foreign key to another tenant's row is blocked", st == 403, (st, r))
upd = dict(body, case_number="EXP-2026-000001", title="HACKED", customer_id=1, tenant_id=1)
st, r = call("PUT", "Cases/1", upd, bob)
row1 = doc(call("GET", "Cases/1", t=ADM))
check("tenant 2 cannot update tenant 1 case", row1["title"] != "HACKED", (st, r))
st, r = call("DELETE", "Cases/1", t=bob)
check("tenant 2 cannot delete tenant 1 case", doc(call("GET", "Cases/1", t=ADM)) is not None, (st, r))
st, r = call("POST", "Verticals", {"vertical_code": "X" + RUN, "name": "rogue", "status": "available"}, alice)
check("shared catalog tables are read-only for tenant users", st == 403, (st, r))
tn = [x["saas_tenant_id"] for x in records(call("GET", "Saas_tenants", t=bob))]
check("tenant user sees only its own tenant record", tn == [t2], tn)
plat = [r["case_number"] for r in records(call("GET", "Cases", t=ADM))]
check("platform admin sees all tenants", N2 in plat and len(plat) > len(c2), plat)
mem = [m["username"] for m in doc(call("GET", "Access/members", t=alice))]
check("tenant administrator lists only its tenant's members", "bob.t2" + RUN not in mem and "carol.t1" + RUN in mem, mem)
st, _ = call("PUT", f"Access/roles/{roles['Expert']}/permissions", [], alice)
check("tenant administrator cannot change shared role templates", st == 403, st)
st, r = call("POST", "Access/members", {"username": "mallory" + RUN, "password": "Demo!2026x", "role_id": roles["Administrator"], "tenant_id": t2}, alice)
mal = tok("mallory" + RUN, "Demo!2026x")
check("tenant admin can't place a member in another tenant", doc(call("GET", "Access/me", t=mal))["tenant_id"] == 1, r)
st, r = call("GET", "Ai/files?table=cases&recordId=1", t=bob)
check("AI files of another tenant's record are hidden", st in (400, 403, 404), (st, r))
print(f"\n{sum(results)}/{len(results)} passed")
