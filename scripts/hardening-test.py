#!/usr/bin/env python3
"""#77 production hardening checks against a generated API started in Production with Api__MaxPageSize=2.
Usage: hardening_test.py <admin_user> <admin_password> <appsettings.json path>"""
import json, sys, urllib.request
B = "http://localhost:5100"
def req(m, path, body=None, tok=None, headers=None):
    h = {"Content-Type": "application/json", **({"Authorization": "Bearer " + tok} if tok else {}), **(headers or {})}
    r = urllib.request.Request(B + path, method=m, data=json.dumps(body).encode() if body is not None else None, headers=h)
    try:
        with urllib.request.urlopen(r) as x: return x.status, dict(x.headers), x.read()
    except urllib.error.HTTPError as e: return e.code, dict(e.headers), e.read()
def j(b):
    try: return json.loads(b or b"null")
    except Exception: return None
res = []
def check(name, ok, detail=""):
    ok = bool(ok); res.append(ok); print(("PASS " if ok else "FAIL ") + name + (f"  [{str(detail)[:300]}]" if not ok else ""))

st, h, b = req("POST", "/v1/api/Token", {"username": sys.argv[1], "password": sys.argv[2]})
T = j(b)["document"]["accessToken"]
A = "/v1/api/"
# CORS
st, h, b = req("OPTIONS", A + "Cases", headers={"Origin": "http://evil.example", "Access-Control-Request-Method": "GET"})
check("CORS: unknown website gets no access", "Access-Control-Allow-Origin" not in h, h.get("Access-Control-Allow-Origin"))
st, h, b = req("OPTIONS", A + "Cases", headers={"Origin": "http://localhost:3100", "Access-Control-Request-Method": "GET"})
check("CORS: the generated UI's origin is allowed", h.get("Access-Control-Allow-Origin") == "http://localhost:3100", h)
# errors
st, h, b = req("POST", A + "Cases/filter?andOr=AND", [{"ColumnName": "no_such_column", "ColumnCondition": 1, "ColumnValue": "x"}], T)
body = j(b) or {}
check("unexpected error: generic 500 with a reference, no SQL details", st == 500 and "Reference" in json.dumps(body) and "no_such_column" not in json.dumps(body) and "Unknown column" not in json.dumps(body), (st, body))
check("reference matches the X-Correlation-Id header", h.get("X-Correlation-Id", "?") in json.dumps(body), (h.get("X-Correlation-Id"), body))
st, h, b = req("POST", A + "Ai_models", {"ai_provider_id": 99999, "model_code": "x", "name": "x", "status": "active", "timeout_seconds": 1, "max_retries": 0}, T)
check("constraint error: readable 409 instead of the database message", st == 409 and "doesn't exist" in json.dumps(j(b)) and "foreign key" not in json.dumps(j(b)).lower(), (st, j(b)))
st, h, b = req("GET", A + "Cases", None, T, headers={"X-Correlation-Id": "trace-abc-123"})
check("incoming correlation id is kept", h.get("X-Correlation-Id") == "trace-abc-123", h.get("X-Correlation-Id"))
check("security headers set", h.get("X-Content-Type-Options") == "nosniff" and h.get("X-Frame-Options") == "DENY", h)
# page cap (Api__MaxPageSize=2 for this run)
st, h, b = req("GET", A + "Audit_logs?itemsPerPage=100000", None, T)
n = len((j(b) or {}).get("document", {}).get("records", []))
check("page size capped by Api:MaxPageSize", n == 2, n)
st, h, b = req("GET", A + "Audit_logs/search?searchKey=a&itemsPerPage=100000", None, T)
check("search capped too", len((j(b) or {}).get("document", {}).get("records", [])) <= 2)
# swagger + dev pages off in Production
st, h, b = req("GET", "/swagger/index.html")
check("Swagger off in Production by default", st == 404, st)
# health
st, h, b = req("GET", "/health/ready")
check("/health/ready checks MySQL, Redis and RabbitMQ", st == 200 and j(b)["checks"] == {"mysql": "ok", "redis": "ok", "rabbitmq": "ok"}, (st, j(b)))
# logs / config
log = open("/tmp/api.log").read()
check("SQL isn't printed to the log", "SQL Command" not in log and "newcommandText" not in log, log[-200:])
cfg = open(sys.argv[3]).read()
check("no default credentials in appsettings", "admin123" not in cfg and '"DefaultTokenPassword": ""' in cfg)
print(f"\n{sum(res)}/{len(res)} passed")
