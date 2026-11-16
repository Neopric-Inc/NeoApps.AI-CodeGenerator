"""Security checks against a running NeoApps app. Prints PASS/FAIL per check.

Usage: python3 scripts/security-probe.py http://localhost:5000 <admin-username> <admin-password>
Run it against a local or test copy, never production: it creates test members, notes and failed sign-ins.
Written for the QuickNotes sample (tables notes, notebooks); adjust table names for other apps.
Admin credentials are in workspace/<App>/project.json. Sign-in rate limits apply: wait a minute between runs."""
import json, sys, time, urllib.request, urllib.error, urllib.parse

BASE = sys.argv[1].rstrip("/")          # e.g. http://localhost:5000
ADMIN_USER, ADMIN_PW = sys.argv[2], sys.argv[3]
API = BASE + "/v1/api"
results = []

def req(method, path, body=None, token=None, headers=None, raw=None):
    h = {"Content-Type": "application/json"}
    if token: h["Authorization"] = "Bearer " + token
    if headers: h.update(headers)
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    r = urllib.request.Request((API if path.startswith("/") else "") + path if path.startswith("/") else path, data=data, method=method, headers=h)
    try:
        with urllib.request.urlopen(r, timeout=15) as resp:
            return resp.status, dict(resp.headers), resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, dict(e.headers), e.read().decode("utf-8", "replace")

def check(name, ok, detail=""):
    results.append((name, bool(ok), detail))
    print(("PASS " if ok else "FAIL ") + name + (f"  [{detail}]" if detail else ""))

def login(u, p, headers=None):
    s, h, b = req("POST", "/Token", {"username": u, "password": p}, headers=headers)
    try: tok = json.loads(b).get("document", {}).get("accessToken")
    except Exception: tok = None
    return s, h, b, tok

# ---- sign-in
s, h, b, admin = login(ADMIN_USER, ADMIN_PW)
check("Admin can sign in", s == 200 and admin, f"status {s}")
s, h, b, t = login(ADMIN_USER, ADMIN_PW, {"X-NeoApps-Client": "web"})
cookie = h.get("Set-Cookie", "")
check("Web sign-in returns no token in the body", s == 200 and not t, "token present" if t else "")
check("Session cookie is HttpOnly", "httponly" in cookie.lower(), cookie[:80])
check("Session cookie is SameSite", "samesite" in cookie.lower(), "")

# ---- unauthenticated
for path in ["/Notes", "/Notebooks", "/Users", "/Permissionmatrix", "/Access/me"]:
    s, _, _ = req("GET", path)
    check(f"No sign-in -> {path} refused", s in (401, 403), f"status {s}")
s, _, _ = req("GET", "/Notes", token="eyJhbGciOiJub25lIn0.eyJzdWIiOiJ4In0.")
check("Forged unsigned token refused", s in (401, 403), f"status {s}")

# ---- members with roles
s, _, b = req("GET", "/Access/roles", token=admin)
roles = {r.get("role_name") or r.get("name"): r.get("role_id") for r in json.loads(b).get("document", [])} if s == 200 else {}
print("roles:", roles)
pw = "Str0ngPassw0rd!"
for u, role in [("probe_member", "Member"), ("probe_viewer", "Viewer"), ("probe_member2", "Member")]:
    req("POST", "/Access/members", {"username": u, "password": pw, "role_id": roles.get(role)}, token=admin)
_, _, _, member = login("probe_member", pw)
_, _, _, viewer = login("probe_viewer", pw)
_, _, _, member2 = login("probe_member2", pw)
check("Test members can sign in", member and viewer and member2)

s, _, b = req("POST", "/Access/members", {"username": "weakpw", "password": "abc", "role_id": roles.get("Member")}, token=admin)
check("Weak password rejected", s == 400, f"status {s}")

# ---- role permissions on app tables
s, _, b = req("POST", "/Notebooks", {"name": "Admin book", "color": "#000000"}, token=admin)
s, _, b = req("POST", "/Notes", {"title": "Admin note", "status": "active", "is_pinned": 0}, token=admin)
check("Admin can create a note", s in (200, 201), f"status {s}")
s, _, b = req("GET", "/Notes", token=admin)
notes = json.loads(b).get("document", {}).get("records", []) if s == 200 else []
nid = notes[0]["note_id"] if notes else 1

s, _, _ = req("GET", "/Notes", token=viewer);                     check("Viewer can read notes", s == 200, f"status {s}")
s, _, _ = req("POST", "/Notes", {"title": "v", "status": "active", "is_pinned": 0}, token=viewer); check("Viewer cannot create", s in (401, 403), f"status {s}")
s, _, _ = req("PUT", f"/Notes/{nid}", {"note_id": nid, "title": "hacked", "status": "active", "is_pinned": 0}, token=viewer); check("Viewer cannot edit", s in (401, 403), f"status {s}")
s, _, _ = req("DELETE", f"/Notes/{nid}", token=viewer);           check("Viewer cannot delete", s in (401, 403), f"status {s}")
s, _, _ = req("POST", "/Notes", {"title": "m", "status": "active", "is_pinned": 0}, token=member); check("Member can create", s in (200, 201), f"status {s}")
s, _, _ = req("DELETE", f"/Notes/{nid}", token=member);           check("Member cannot delete (role has no delete)", s in (401, 403), f"status {s}")

s, _, b = req("GET", f"/Notes/{nid}", token=admin)
check("Refused edits and deletes changed nothing", s == 200 and "hacked" not in b and "Admin note" in b, f"status {s}")

# ---- privilege escalation through built-in tables and admin endpoints
for path in ["/Users", "/Roles", "/Permissionmatrix", "/Entities", "/S3bucket", "/Messagequeue"]:
    s, _, b = req("GET", path, token=member)
    leaked = "password" in b.lower() and s == 200
    check(f"Member cannot read {path}", s in (401, 403, 404), f"status {s}" + (" (password field visible)" if leaked else ""))
s, _, b = req("GET", "/Access/me", token=member)
me = json.loads(b).get("document", {}) if s == 200 else {}
my_id = me.get("user_id") or me.get("userId")
s, _, _ = req("POST", "/Permissionmatrix", {"role_id": roles.get("Member"), "entity_id": 1, "permissionValue": 15}, token=member)
check("Member cannot write permissions", s in (401, 403, 404), f"status {s}")
s, _, _ = req("PUT", f"/Users/{my_id or 2}", {"user_id": my_id or 2, "role_id": roles.get("Administrator"), "username": "probe_member"}, token=member)
check("Member cannot make themselves admin via /Users", s in (401, 403, 404), f"status {s}")
s, _, _ = req("PUT", f"/Access/members/{my_id or 2}", {"username": "probe_member", "role_id": roles.get("Administrator")}, token=member)
check("Member cannot change roles via /Access/members", s in (401, 403), f"status {s}")
s, _, _ = req("GET", "/Access/members", token=member)
check("Member cannot list members", s in (401, 403), f"status {s}")
s, _, after = req("GET", "/Access/me", token=member)
check("Member is still not admin afterwards", '"Administrator"' not in after, "")

# ---- record ownership / audit stamping
s, _, b = req("POST", "/Notes", {"title": "forged", "status": "active", "is_pinned": 0, "createdBy": "someone_else", "created_by_user": "someone_else"}, token=member)
s2, _, b2 = req("GET", "/Notes?itemsPerPage=1000", token=admin)
forged = [n for n in json.loads(b2).get("document", {}).get("records", []) if n.get("title") == "forged"]
cbu = forged[0].get("created_by_user") if forged else None
check("Who-created is stamped by the server, not the client", cbu == "probe_member", f"created_by_user={cbu}")

# ---- injection
probes = ["title;DROP TABLE notes", "title' OR '1'='1", "(SELECT SLEEP(3))", "title|ASC,(SELECT 1)"]
for p in probes:
    t0 = time.time(); s, _, b = req("GET", "/Notes?orderBy=" + urllib.parse.quote(p), token=admin); dt = time.time() - t0
    check(f"orderBy injection refused: {p}", s != 500 and dt < 2.5 and "sql" not in b.lower(), f"status {s}, {dt:.1f}s")
for p in ["x' OR '1'='1", "%' UNION SELECT username,password_hash FROM users -- ", "abc\\' ; SELECT SLEEP(3); -- "]:
    t0 = time.time(); s, _, b = req("GET", "/Notes/search?searchKey=" + urllib.parse.quote(p), token=admin); dt = time.time() - t0
    check(f"search injection harmless: {p[:30]}", s != 500 and dt < 2.5 and "password_hash" not in b and "$2" not in b, f"status {s}, {dt:.1f}s")
s, _, b = req("GET", "/Notes/1%20OR%201=1", token=admin)
check("Path id injection refused", s in (400, 404), f"status {s}")
s, _, _ = req("POST", "/Notes", {"title": "a'); DROP TABLE notes; --", "status": "active", "is_pinned": 0}, token=admin)
s2, _, _ = req("GET", "/Notes", token=admin)
check("Quote in a field is stored as text, table intact", s in (200, 201) and s2 == 200, f"{s}/{s2}")

# ---- XSS: API returns JSON, never HTML
s, h, b = req("POST", "/Notes", {"title": "<script>alert(1)</script>", "body": "<img src=x onerror=alert(1)>", "status": "active", "is_pinned": 0}, token=admin)
s, h, b = req("GET", "/Notes", token=admin)
check("API answers as JSON (not HTML)", "application/json" in h.get("Content-Type", ""), h.get("Content-Type", ""))
check("nosniff header set", h.get("X-Content-Type-Options", "").lower() == "nosniff")

# ---- errors, docs, CORS, paging
s, _, b = req("POST", "/Notes", raw=b"{not json", token=admin)
check("Bad input gives no stack trace", "   at " not in b and "Exception" not in b, f"status {s}")
s, _, b = req("GET", "/Notes/abc", token=admin)
check("Bad id gives no stack trace", "   at " not in b and "MySql" not in b, f"status {s}")
s, _, _ = req("GET", BASE + "/swagger/index.html")
check("API docs (Swagger) off in production", s == 404, f"status {s}")
s, h, _ = req("GET", "/Notes", token=admin, headers={"Origin": "https://evil.example"})
check("Unknown website origin not allowed (CORS)", h.get("Access-Control-Allow-Origin") not in ("*", "https://evil.example"), h.get("Access-Control-Allow-Origin", "none"))
s, _, b = req("GET", "/Notes?itemsPerPage=1000000", token=admin)
check("Huge page size handled", s in (200, 400), f"status {s}")

# ---- sign-out and disabled sessions
s, _, _ = req("POST", f"/Access/members/{my_id}/sign-out", token=admin)
time.sleep(31)
s, _, _ = req("GET", "/Notes", token=member)
check("Signed-out member's old token stops working", s == 401, f"status {s}")

# ---- brute force
codes = []
for i in range(12):
    s, _, _, _ = login("probe_member2", "wrong-password-%d" % i); codes.append(s)
s, _, _, t = login("probe_member2", pw)
check("Repeated wrong passwords lock the account or rate-limit", (429 in codes) or (s != 200), f"codes {sorted(set(codes))}, then right password -> {s}")

passed = sum(1 for _, ok, _ in results if ok)
print(f"\n{passed}/{len(results)} passed")

# ---- regressions for fixes found in this audit
s, _, b, admin = login(ADMIN_USER, ADMIN_PW)
s, _, b = req("POST", "/Notes", {"title": "zzgone plan", "body": "zzgone body", "status": "active", "is_pinned": 0}, token=admin)
s, _, b = req("GET", "/Notes/search?searchKey=zzgone", token=admin)
gid = json.loads(b)["document"]["records"][0]["note_id"]
req("DELETE", f"/Notes/{gid}", token=admin)
s, _, b = req("GET", "/Notes/search?searchKey=zzgone", token=admin)
doc = json.loads(b).get("document") if s == 200 else None
check("Deleted records stay out of search", s == 200 and not (doc and doc.get("records")), f"status {s}, {b[:80]}")
s, _, b = req("GET", "/Users", token=admin)
check("No password hashes in /Users, even for admins", s == 200 and "$2" not in b, f"status {s}")
s, _, b = req("GET", "/Users", token=admin)
u = [x for x in json.loads(b)["document"]["records"] if x["username"] == "probe_viewer"][0]
u = {k: u[k] for k in ("user_id", "username", "role_id", "isActive")}
s, _, b = req("PUT", f"/Users/{u['user_id']}", u, token=admin)
s2, _, _, t2 = login("probe_viewer", pw)
check("Editing a user without a password keeps their password", s == 200 and t2, f"update {s}, sign-in {s2}")
passed = sum(1 for _, ok, _ in results if ok)
print(f"\n{passed}/{len(results)} passed (with regressions)")
s, _, b = req("POST", "/Users", {"username": "weak_via_users", "password_hash": "abc", "role_id": 2, "isActive": 1}, token=admin)
check("Password rules also apply to /Users", s == 400, f"status {s}")
passed = sum(1 for _, ok, _ in results if ok)
print(f"\n{passed}/{len(results)} passed (final)")
sys.exit(0 if passed == len(results) else 1)
