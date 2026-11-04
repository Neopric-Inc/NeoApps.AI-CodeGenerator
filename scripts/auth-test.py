#!/usr/bin/env python3
"""#78 login protection. API started with Auth__MaxFailedLogins=3 Auth__LockoutMinutes=1 Auth__LoginsPerMinutePerIp=30.
Usage: auth_test.py <admin_user> <admin_password>"""
import base64, json, random, sys, time, urllib.request
B = "http://localhost:5100/v1/api/"
def call(m, p, b=None, t=None):
    r = urllib.request.Request(B + p, method=m, data=json.dumps(b).encode() if b is not None else None,
                               headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + t} if t else {})})
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read() or b"null")
    except urllib.error.HTTPError as e:
        body = e.read()
        try: return e.code, json.loads(body)
        except Exception: return e.code, body.decode(errors="replace")[:200]
def login(u, p): return call("POST", "Token", {"username": u, "password": p})
def tok(u, p): return login(u, p)[1]["document"]["accessToken"]
def msg(r): return r[1].get("message") if isinstance(r[1], dict) else r[1]
res = []
def check(name, ok, detail=""):
    ok = bool(ok); res.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{str(detail)[:250]}]" if not ok else ""))

ADMIN, PW = sys.argv[1], sys.argv[2]; RUN = str(random.randint(1000, 9999))
check("generated admin password is long (16+)", len(PW) >= 16, len(PW))
ADM = tok(ADMIN, PW)
roles = {r["role_name"]: r["role_id"] for r in call("GET", "Access/roles", t=ADM)[1]["document"]}
def add_member(u, pw, role="Expert"): return call("POST", "Access/members", {"username": u, "password": pw, "role_id": roles[role], "tenant_id": 1}, ADM)

# password policy
for pw, why in [("Short1", "too short"), ("onlyletterspassword", "no digits"), (f"ann{RUN}x99zz", "contains username"), ("password123", "common")]:
    st, r = add_member(f"ann{RUN}", pw)
    check(f"weak password refused ({why})", st == 400, (st, msg((st, r))))
st, r = add_member(f"ann{RUN}", "Strong!Pass42")
check("strong password accepted", st == 200, (st, msg((st, r))))
ids = {m["username"]: m["user_id"] for m in call("GET", "Access/members", t=ADM)[1]["document"]}
ann = ids[f"ann{RUN}"]

# token lifetime
t = tok(f"ann{RUN}", "Strong!Pass42")
p = json.loads(base64.urlsafe_b64decode(t.split(".")[1] + "=="))
check("access token lasts 8 hours (Auth:TokenMinutes), not 7 days", abs((p["exp"] - p["nbf"]) - 480 * 60) < 120, (p["exp"] - p["nbf"]) / 3600)
check("token carries a version claim", "neoapps:tv" in p, list(p))

# lockout
for _ in range(3): login(f"ann{RUN}", "wrong-password-1")
st, r = login(f"ann{RUN}", "Strong!Pass42")
check("account locked after 3 failures, even with the right password", st == 429 and "Try again" in str(msg((st, r))), (st, msg((st, r))))
for _ in range(3): login(f"ghost{RUN}", "whatever-123")
st2, r2 = login(f"ghost{RUN}", "whatever-123")
check("unknown usernames get the same lockout (no account discovery)", st2 == 429 and msg((st2, r2)) == msg((st, r)), (st2, msg((st2, r2))))
check("admin can clear a lockout", call("POST", f"Access/members/{ann}/unlock", {}, ADM)[0] == 200 and login(f"ann{RUN}", "Strong!Pass42")[0] == 200)

# revocation
t = tok(f"ann{RUN}", "Strong!Pass42")
check("token works before sign-out", call("GET", "Access/me", t=t)[0] == 200)
call("POST", "Access/logout", {}, t)
check("sign-out ends the session (same token refused)", call("GET", "Cases", t=t)[0] == 401)
t = tok(f"ann{RUN}", "Strong!Pass42")
st, r = call("PUT", f"Access/members/{ann}", {"role_id": roles["Expert"], "password": "Another!Pass77"}, ADM)
check("admin password reset ends existing sessions", st == 200 and call("GET", "Cases", t=t)[0] == 401, (st, msg((st, r))))
t = tok(f"ann{RUN}", "Another!Pass77")
call("PUT", f"Access/members/{ann}", {"role_id": roles["Reviewer"]}, ADM)
check("role change ends existing sessions", call("GET", "Cases", t=t)[0] == 401)
t = tok(f"ann{RUN}", "Another!Pass77")
call("PUT", f"Access/members/{ann}", {"role_id": roles["Reviewer"], "isActive": 0}, ADM)
check("deactivation ends existing sessions and blocks sign-in", call("GET", "Cases", t=t)[0] == 401 and login(f"ann{RUN}", "Another!Pass77")[0] == 401)
call("PUT", f"Access/members/{ann}", {"role_id": roles["Reviewer"], "isActive": 1}, ADM)
t = tok(f"ann{RUN}", "Another!Pass77")
st, r = call("PUT", "Access/me/password", {"current_password": "nope-nope-1", "new_password": "Third!Pass99"}, t)
check("own password change needs the current password", st == 400, (st, msg((st, r))))
st, r = call("PUT", "Access/me/password", {"current_password": "Another!Pass77", "new_password": "Third!Pass99"}, t)
check("own password change works and ends other sessions", st == 200 and call("GET", "Cases", t=t)[0] == 401 and login(f"ann{RUN}", "Third!Pass99")[0] == 200, (st, msg((st, r))))
st, r = call("POST", f"Access/members/{ann}/sign-out", {}, ADM)
check("admin can sign a member out everywhere", st == 200)

# rate limit per IP (30/min in this run): distinct usernames so the lockout doesn't kick in
codes = [login(f"rl{RUN}_{i}", "x-123-rate")[0] for i in range(40)]
check("sign-in rate limit per IP returns 429", codes.count(429) >= 5 and codes[0] == 401, codes)
print(f"\n{sum(res)}/{len(res)} passed")
