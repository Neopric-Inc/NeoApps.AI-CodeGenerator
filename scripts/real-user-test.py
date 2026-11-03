"""#60: created_by_user / modified_by_user record the real person; createdBy stays the owner. Usage: real_user_test.py <admin> <pw> [table]"""
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
def tok(u, p): return call("POST", "Token", {"username": u, "password": p})[1]["document"]["accessToken"]
ADM = tok(sys.argv[1], sys.argv[2]); RUN = str(random.randint(1000, 9999)); ADMIN = sys.argv[1]
roles = {r["role_name"]: r["role_id"] for r in call("GET", "Access/roles", t=ADM)[1]["document"]}
res = []
def check(name, ok, detail=""): res.append(bool(ok)); print(("PASS " if ok else "FAIL ") + name + ("" if ok else f"  [{detail}]"))
def member(u, role):
    body = {"username": u + RUN, "password": "Demo!2026x", "role_id": roles[role]}
    if len(sys.argv) <= 3: body["tenant_id"] = 1
    call("POST", "Access/members", body, ADM); return u + RUN, tok(u + RUN, "Demo!2026x")
table = sys.argv[3] if len(sys.argv) > 3 else "Customers"
if table == "Customers":
    A, TA = member("ops", "Operations Manager"); Bn, TB = member("ops2", "Operations Manager")
    body = {"tenant_id": 1, "customer_code": "C" + RUN, "name": "Acme " + RUN, "customer_type": "business", "status": "active", "created_by_user": "spoof", "modified_by_user": "spoof"}
else:
    A, TA = member("ua", "Administrator"); Bn, TB = member("ub", "Administrator")
    body = {"title": "t" + RUN, "created_by_user": "spoof", "modified_by_user": "spoof"}
st, r = call("POST", table, body, TA)
rid = r.get("document") if isinstance(r, dict) else None
check("member A creates a record", st == 200 and rid, (st, r))
row = call("GET", f"{table}/{rid}", t=ADM)[1]["document"]
check("created_by_user / modified_by_user = A, not the client's value", row.get("created_by_user") == A and row.get("modified_by_user") == A, row)
check("createdBy still holds the workspace owner (scoping unchanged)", row.get("createdBy") == ADMIN, row.get("createdBy"))
row2 = dict(row); row2.update({"created_by_user": "spoof", "modified_by_user": "spoof"})
row2[[k for k in row2 if k in ("name", "title")][0]] += " (edited)"
st, r = call("PUT", f"{table}/{rid}", row2, TB)
row = call("GET", f"{table}/{rid}", t=ADM)[1]["document"]
check("member B edits: modified_by_user = B, created_by_user stays A", st == 200 and row.get("created_by_user") == A and row.get("modified_by_user") == Bn, (st, row))
st, r = call("GET", f"{table}?itemsPerPage=500", t=TB)
check("both members still see it (owner scoping intact)", any(x.get(list(x)[0]) == rid for x in r["document"]["records"]) if st == 200 else False, st)
print(f"{sum(res)}/{len(res)} passed")
