"""Checks a ConstructOS database after the v1 -> v2 (equipment tracking) migration: old data still readable, new tables
and columns work, existing members got permissions on the new tables from their roles, the new Equipment Manager role
works, and a menu saved in Admin > Customize UI gained the new screens.
Usage: verify_upgrade.py <admin-username> <admin-password> [api-base]   (after seed.py on v1, then evolve)"""
import json, sys, urllib.request, urllib.error

B = sys.argv[3] if len(sys.argv) > 3 else "http://localhost:5100/v1/api/"
ok = True

def call(method, path, body=None, tok=None):
    req = urllib.request.Request(B + path, method=method, data=json.dumps(body).encode() if body is not None else None,
                                 headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + tok} if tok else {})})
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.loads(r.read() or b"null")
    except urllib.error.HTTPError as e:
        return e.code, (e.read() or b"").decode()[:200]

def login(u, p):
    return call("POST", "Token", {"username": u, "password": p})[1]["document"]["accessToken"]

def check(label, cond, detail=""):
    global ok
    ok &= bool(cond)
    print(("PASS " if cond else "FAIL ") + label + (f"  ({detail})" if detail and not cond else ""))

def first(table, tok):
    return call("GET", f"{table}?pageNo=1&pageSize=1", tok=tok)[1]["document"]["records"][0]

A = login(sys.argv[1], sys.argv[2])
au = {"createdBy": "x", "modifiedBy": "x"}

# Old data is still there and readable through the regenerated API.
proj = first("Projects", A)
check("existing project readable", proj["name"] == "Harbourview Medical Office Building")
dl = first("Daily_logs", A)
check("existing daily log readable, new column present", "equipment_notes" in dl and dl["equipment_notes"] is None)
s, r = call("GET", "Pay_app_lines?pageNo=1&pageSize=5", tok=A)
check("existing pay app line kept", r["document"]["records"][0]["flag"] == "48.7% billed vs 35% schedule progress")

# New tables work end to end.
s, r = call("POST", "Equipment", {"project_id": proj["project_id"], "equipment_number": "EQ-014", "name": "Genie S-65 boom lift",
                                  "equipment_type": "lift", "ownership": "rented", "daily_rate": 425, "on_site_date": "2026-10-20",
                                  "status": "on_site", **au}, A)
check("create equipment", s == 200 and r.get("code") == 1, r)
eq = r["document"] if s == 200 else None
s, r = call("POST", "Equipment_logs", {"daily_log_id": dl["daily_log_id"], "equipment_id": eq, "hours_used": 6.5, "idle_hours": 1.5,
                                       "operator_name": "J. Cormier", **au}, A)
check("create equipment usage linked to existing daily log", s == 200 and r.get("code") == 1, r)
s, r = call("GET", "Equipment_logs?pageNo=1&pageSize=5", tok=A)
check("list equipment usage", s == 200 and r["document"]["records"][0]["operator_name"] == "J. Cormier", r)

# Changed columns on existing tables.
dl2 = {k: v for k, v in dl.items() if k not in ("createdAt", "modifiedAt")}
dl2["equipment_notes"] = "Boom lift EQ-014 ran 6.5h"
dl2["log_date"] = "2026-11-02"   # the list endpoint returns display dates; write ISO like the UI does
s, r = call("PUT", f"Daily_logs/{dl['daily_log_id']}", dl2, A)
check("update existing daily log with new column", s == 200 and first("Daily_logs", A)["equipment_notes"] == "Boom lift EQ-014 ran 6.5h", r)
s, r = call("POST", "Punch_items", {"project_id": proj["project_id"], "location": "L2 east", "description": "x" * 1500, "status": "open", **au}, A)
check("punch description longer than the old 1000-char limit", s == 200 and r.get("code") == 1, r)
comp = first("Companies", A)
s, r = call("POST", "Inspections", {"project_id": proj["project_id"], "inspection_type": "Lift cert", "company_id": comp["company_id"], "result": "scheduled", **au}, A)
check("inspection with new company relation", s == 200 and r.get("code") == 1, r)

# Existing members got permissions for the new tables from their role's grants.
eqbody = {"project_id": proj["project_id"], "equipment_number": "EQ-T", "name": "t", "status": "on_site", "equipment_type": "heavy", "ownership": "owned", **au}
for user, create_expected, read_expected in [("super1", 200, 200), ("acct1", 401, 200), ("sub1", 401, 401), ("owner1", 401, 200)]:
    t = login(user, "Test#12345")
    sc = call("POST", "Equipment", eqbody, t)[0]
    sr = call("GET", "Equipment?pageNo=1&pageSize=1", tok=t)[0]
    check(f"existing member {user}: create equipment {sc} (want {create_expected}), read {sr} (want {read_expected})", sc == create_expected and sr == read_expected)

# The new role works for a new member.
s, roles = call("GET", "Access/roles", tok=A)
rid = {x["role_name"]: x["role_id"] for x in roles["document"]}
check("role Equipment Manager exists", "Equipment Manager" in rid)
call("POST", "Access/members", {"username": "eqmgr1", "password": "Test#12345", "role_id": rid["Equipment Manager"]}, A)
t = login("eqmgr1", "Test#12345")
check("Equipment Manager can create equipment", call("POST", "Equipment", eqbody, t)[0] == 200)
check("Equipment Manager can create a purchase order",
      call("POST", "Purchase_orders", {"project_id": proj["project_id"], "company_id": comp["company_id"], "po_number": "PO-EQ", "status": "draft", **au}, t)[0] == 200)
check("Equipment Manager cannot create RFIs",
      call("POST", "Rfis", {"project_id": proj["project_id"], "rfi_number": "R", "subject": "s", "question": "q", "status": "draft", **au}, t)[0] == 401)
me = call("GET", "Access/me", tok=t)[1]["document"]["permissions"]
check("Equipment Manager /me: equipment cru, rfis read-only",
      me["equipment"] == {"read": True, "create": True, "update": True, "delete": False} and me["rfis"]["read"] and not me["rfis"]["create"], me.get("equipment"))

# A menu saved in Admin > Customize UI (if any) gained the new screens under Field.
cfg = call("GET", "Access/ui-config", tok=A)[1].get("document")
if cfg and cfg.get("menu"):
    field = next((g for g in cfg["menu"] if g["group"] == "Field"), {"items": []})
    check("saved menu has equipment + usage under Field", "equipment" in field["items"] and "equipment_logs" in field["items"], field["items"])
else:
    print("skip saved-menu check (no menu saved in Admin > Customize UI)")

print("ALL PASSED" if ok else "SOME CHECKS FAILED")
sys.exit(0 if ok else 1)
