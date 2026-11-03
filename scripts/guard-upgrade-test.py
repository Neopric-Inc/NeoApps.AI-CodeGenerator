#!/usr/bin/env python3
# Usage: guard-upgrade-test.py before <admin> <password>   (tenancy off; then enable tenancy and restart the API)
#        guard-upgrade-test.py after  <admin> <password>
"""#54: enabling tenancy on an existing app. Phase 'before' (tenancy off): seed, add members, attach an AI file, save tokens.
Phase 'after' (tenancy on, API restarted): unassigned members are denied (also with old tokens), owner admin is platform,
AI files are tagged with their record's tenant, bulk assignment restores access, user_tenants never becomes an app table."""
import json, subprocess, sys, urllib.request
B = "http://localhost:5100/v1/api/"
STATE = "/tmp/upgrade_state.json"
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
def sql(q): return subprocess.run(["mysql", "-h127.0.0.1", "-uroot", "-pneoapps", "-N", "expert_os_db", "-e", q], capture_output=True, text=True).stdout.strip()
phase, admin, pw = sys.argv[1], sys.argv[2], sys.argv[3]
ADM = tok(admin, pw)
if phase == "before":
    roles = {r["role_name"]: r["role_id"] for r in doc(call("GET", "Access/roles", t=ADM))}
    for u, role in [("legacy.expert", "Expert"), ("legacy.customer", "Customer User")]:
        call("POST", "Access/members", {"username": u, "password": "Legacy!2026", "role_id": roles[role]}, ADM)
    exp = tok("legacy.expert", "Legacy!2026")
    st, r = call("GET", "Cases", t=exp)
    print("before: expert sees cases:", st, len((doc((st, r)) or {}).get("records", [])))
    boundary = "----up"
    body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"table\"\r\n\r\ndocuments\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"recordId\"\r\n\r\n1\r\n"
            f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"legacy.txt\"\r\nContent-Type: text/plain\r\n\r\nLegacy quote text for tenant 1.\r\n--{boundary}--\r\n").encode()
    req = urllib.request.Request(B + "Ai/files", data=body, method="POST", headers={"Content-Type": f"multipart/form-data; boundary={boundary}", "Authorization": "Bearer " + exp})
    print("before: AI file uploaded:", json.loads(urllib.request.urlopen(req).read()).get("status"))
    print("before: ai_files tenant_id:", sql("SELECT IFNULL(tenant_id,'NULL') FROM ai_files"))
    json.dump({"exp_token": exp}, open(STATE, "w"))
    sys.exit(0)

res = []
def check(name, ok, detail=""):
    ok = bool(ok); res.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{detail}]" if not ok else ""))
state = json.load(open(STATE))
log = open("/tmp/api.log").read()
check("startup warns about members without a tenant", "member(s) have no tenant" in log, log[-300:])
st, r = call("GET", "Cases", t=state["exp_token"])
check("token issued before tenancy no longer reads data", st == 403, (st, r))
exp = tok("legacy.expert", "Legacy!2026")
st, r = call("GET", "Cases", t=exp)
check("unassigned member is denied, not a platform user", st == 403 and "assigned" in str(r), (st, r))
me = doc(call("GET", "Access/me", t=exp))
check("/Access/me reports the member as unassigned", me["tenant_assigned"] is False and me["is_platform"] is False, me and {k: me[k] for k in ("tenant_id", "tenant_assigned", "is_platform")})
st, r = call("GET", "Cases", t=ADM)
check("workspace owner (Administrator) stays a platform user", st == 200 and len(doc((st, r))["records"]) >= 1, (st,))
check("existing AI file tagged with its record's tenant", sql("SELECT tenant_id FROM ai_files") == "1" and sql("SELECT COUNT(*) FROM ai_chunks WHERE tenant_id IS NULL") == "0",
      (sql("SELECT IFNULL(tenant_id,'NULL') FROM ai_files"), sql("SELECT COUNT(*) FROM ai_chunks WHERE tenant_id IS NULL")))
mem = {m["username"]: m for m in doc(call("GET", "Access/members", t=ADM))}
check("members list flags unassigned members", mem["legacy.expert"]["tenant_assigned"] is False and mem["legacy.customer"]["tenant_assigned"] is False, mem.get("legacy.expert"))
st, r = call("POST", "Access/members/assign-tenant", {"tenant_id": 1}, ADM)
check("bulk assignment of all unassigned members", st == 200 and len(doc((st, r))["assigned"]) == 2, (st, r))
st, r = call("GET", "Cases", t=exp)
check("assigned member regains access at once (same token)", st == 200 and all(c["tenant_id"] == 1 for c in doc((st, r))["records"]), (st,))
st, r = call("POST", "Access/members/assign-tenant", {"tenant_id": 999}, ADM)
check("assigning to an unknown tenant is refused", st == 400, (st, r))
check("user_tenants is not an app table (no entity, no controller)", sql("SELECT COUNT(*) FROM entities WHERE entity_name='user_tenants'") == "0", "")
print(f"\n{sum(res)}/{len(res)} passed")
