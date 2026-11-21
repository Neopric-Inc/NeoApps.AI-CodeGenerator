#!/usr/bin/env python3
"""Smoke test of a generated todo-app API (samples/todo-app): health, sign-in, Access/me, create/list/get/update, a linked
record and delete. Usage: python3 scripts/azure/smoke-test.py <App>. For an azuresql app, NEOAPPS_AZURESQL must be set
(scripts/azure/test-azuresql.sh does that); the API gets it as ConnectionStrings__MySQLDatabase."""
import json, os, subprocess, sys, time, urllib.request, urllib.error, glob

root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
app = sys.argv[1] if len(sys.argv) > 1 else "AzTest"
proj = json.load(open(f"{root}/workspace/{app}/project.json"))
api_dir = os.path.join(proj["BackendPath"], f"{app}.API")
base = "http://127.0.0.1:5199"
env = dict(os.environ, ASPNETCORE_URLS="http://127.0.0.1:5199", ASPNETCORE_ENVIRONMENT="Development", Auth__LoginsPerMinutePerIp="200")
if proj.get("DbProvider") == "azuresql":
    cs = os.environ.get(proj.get("DbEnv") or "NEOAPPS_AZURESQL")
    if not cs: sys.exit(f"Set {proj.get('DbEnv')} first.")
    env["ConnectionStrings__MySQLDatabase"] = cs
log_path = os.path.join(root, "workspace", app, "smoke-api.log")
log = open(log_path, "w")
p = subprocess.Popen(["dotnet", "run", "--no-build", "--no-launch-profile"], cwd=api_dir, env=env, stdout=log, stderr=subprocess.STDOUT)

def req(method, path, body=None, token=None):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(base + path, data=data, method=method, headers={"Content-Type": "application/json"})
    if token: r.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(r, timeout=120) as resp: return resp.status, json.loads(resp.read() or b"null")
    except urllib.error.HTTPError as e:
        t = e.read().decode()[:300]
        return e.code, t

ok = True
def check(name, cond, info=""):
    global ok
    print(("PASS " if cond else "FAIL ") + name + ("" if cond else f"  {info}"))
    ok = ok and cond
try:
    for _ in range(240):
        try:
            if req("GET", "/health")[0] == 200: break
        except Exception: pass
        if p.poll() is not None: break
        time.sleep(1)
    try: s, r = req("GET", "/health")
    except Exception as e: s, r = 0, str(e)
    if s != 200:
        log.flush()
        print("FAIL the API didn't start" + (f" (exit code {p.poll()})" if p.poll() is not None else "") + ". Last lines of its log:")
        print("".join(open(log_path).readlines()[-40:]))
        sys.exit(1)
    check("health", True)
    s, r = req("GET", "/health/ready"); print("ready:", s, r)
    s, r = req("POST", "/v1/api/Token", {"username": proj["AdminUsername"], "password": proj["AdminPassword"]})
    tok = r.get("document", {}).get("accessToken") if isinstance(r, dict) else None
    check("login", s == 200 and tok, r)
    s, r = req("GET", "/v1/api/Access/me", token=tok); check("Access/me", s == 200, r)
    s, r = req("POST", "/v1/api/Todo_lists", {"name": "Smoke list", "color": "blue"}, tok); check("create list", s == 200, r)
    lid = r.get("document") if isinstance(r, dict) else None
    print("  new list id:", lid)
    s, r = req("GET", "/v1/api/Todo_lists?pageNo=1&pageSize=10", token=tok)
    recs = (r.get("document") or {}).get("records", []) if isinstance(r, dict) else []
    check("list lists", s == 200 and any(x.get("name") == "Smoke list" for x in recs), str(r)[:300])
    s, r = req("GET", f"/v1/api/Todo_lists/{lid}", token=tok); check("get one", s == 200, r)
    s, r = req("PUT", f"/v1/api/Todo_lists/{lid}", {"todo_list_id": lid, "name": "Smoke list 2", "color": "red"}, tok); check("update", s == 200, r)
    s, r = req("POST", "/v1/api/Todos", {"todo_list_id": lid, "title": "Smoke todo", "is_done": 0, "priority": "high"}, tok); check("create todo (FK)", s == 200, r)
    s, r = req("GET", "/v1/api/Todos?pageNo=1&pageSize=10", token=tok); check("list todos (with relation)", s == 200, str(r)[:300])
    s, r = req("POST", "/v1/api/Todo_lists/search?pageNo=1&pageSize=10", {"name": "Smoke"}, tok); print("search:", s, str(r)[:200])
    s, r = req("DELETE", f"/v1/api/Todo_lists/{lid}", token=tok); print("delete list (has todo):", s, str(r)[:200])
finally:
    p.terminate()
    try: p.wait(10)
    except Exception: p.kill()
print("ALL OK" if ok else f"SOME FAILED (API log: {log_path})")
sys.exit(0 if ok else 1)
