#!/usr/bin/env python3
"""
Generated-app data-rule tests (./neoapps test <App>). Reads workspace/<App>/project.json (API URL, admin login),
the app's schema.sql and ui.config.json, then checks against the running API:

  tenancy    - two tenants, every tenant table: rows of tenant A are invisible to tenant B (list, get), and B can't
               update or delete them; a tenant_id sent by B is replaced with B's own; shared tables are read-only.
  immutable  - append-only tables reject updates.
  lifecycles - a status jump that no transition allows is rejected.
  rowScopes  - per scope: two home records (e.g. two customers) with a linked user each; rows of every covered table
               created for the second are invisible to the first and visible to the second.

Exit code 0 when everything passes. Writes workspace/<App>/guard-test-report.json.
"""
import json, os, random, re, sys, urllib.error, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = sys.argv[1] if len(sys.argv) > 1 else sys.exit("usage: guard-tests.py <App>")
WS = os.path.join(ROOT, "workspace", APP)
project = json.load(open(os.path.join(WS, "project.json")))
API = project.get("ApiBaseUrl") or f"http://localhost:{project.get('ApiPort', 5100)}/v1/api/"
cfg_path = os.path.join(WS, "ui.config.json")
CFG = json.load(open(cfg_path)) if os.path.exists(cfg_path) else {}
SQL = open(project.get("SchemaFile") or os.path.join(WS, "schema.sql")).read()
RUN = str(random.randint(10000, 99999))
AUDIT_COLS = {"createdby", "modifiedby", "createdat", "modifiedat", "isactive"}

# ------------------------------------------------------------------ schema
tables, order = {}, []
for m in re.finditer(r"CREATE\s+TABLE\s+`?(\w+)`?\s*\((.*?)\)\s*;", SQL, re.S | re.I):
    name, body = m.group(1).lower(), m.group(2)
    parts, depth, cur = [], 0, ""
    for ch in body:
        if ch == "(": depth += 1
        if ch == ")": depth -= 1
        if ch == "," and depth == 0: parts.append(cur); cur = ""
        else: cur += ch
    parts.append(cur)
    cols, fks, pk = {}, {}, None
    for p in (x.strip() for x in parts):
        fk = re.match(r"(?:CONSTRAINT\s+\w+\s+)?FOREIGN\s+KEY\s*\(\s*`?(\w+)`?\s*\)\s*REFERENCES\s+`?(\w+)`?", p, re.I)
        if fk: fks[fk.group(1).lower()] = fk.group(2).lower(); continue
        if re.match(r"(UNIQUE|KEY|INDEX|PRIMARY|CONSTRAINT|CHECK)\b", p, re.I): continue
        c = re.match(r"`?(\w+)`?\s+(\w+)(?:\((\d+)(?:,\d+)?\))?(.*)", p, re.S)
        if not c: continue
        rest = c.group(4).upper()
        cols[c.group(1).lower()] = {"type": c.group(2).lower(), "len": int(c.group(3) or 0), "notnull": "NOT NULL" in rest,
                                     "default": "DEFAULT" in rest, "pk": "PRIMARY KEY" in rest}
        if "PRIMARY KEY" in rest: pk = c.group(1).lower()
    tables[name] = {"cols": cols, "fks": fks, "pk": pk}
    order.append(name)

ten = CFG.get("tenancy") or {}
TCOL, TTABLE = ten.get("column", "tenant_id"), ten.get("tenantTable")
TENANT_TABLES = [t for t in order if TCOL in tables[t]["cols"]]
SHARED = [t for t in order if t not in TENANT_TABLES and t != TTABLE]
LIFE = CFG.get("lifecycles") or {}
IMM = {t: v for t, v in (CFG.get("immutable") or {}).items() if "*" in v}
for k in ("audit", "outbox"):
    t = (CFG.get(k) or {}).get("table")
    if t: IMM[t] = ["*"]
OPTIONS = {t: {c: f["options"] for c, f in (spec.get("fields") or {}).items() if isinstance(f, dict) and f.get("options")}
           for t, spec in (CFG.get("tables") or {}).items()}

# ------------------------------------------------------------------ http
def call(m, path, body=None, tok=None):
    req = urllib.request.Request(API + path, method=m, data=json.dumps(body).encode() if body is not None else None,
                                 headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + tok} if tok else {})})
    try:
        with urllib.request.urlopen(req, timeout=30) as r: return r.status, json.loads(r.read() or b"null")
    except urllib.error.HTTPError as e:
        b = e.read()
        try: return e.code, json.loads(b)
        except Exception: return e.code, b.decode(errors="replace")[:200]
def doc(r): return r[1].get("document") if isinstance(r[1], dict) else None
def ctl(t): return t[0].upper() + t[1:]
def login(u, p):
    st, r = call("POST", "Token", {"username": u, "password": p})
    if st != 200: sys.exit(f"login failed for {u}: {st} {r}")
    return r["document"]["accessToken"]

ADMIN = login(project["AdminUsername"], project["AdminPassword"])

# ------------------------------------------------------------------ row factory
def value(table, col, spec, unique):
    t, n = spec["type"], spec["len"] or 255
    life = LIFE.get(table)
    if life and life.get("column", "status") == col and life.get("initial"): return life["initial"][0]
    if col in OPTIONS.get(table, {}): return OPTIONS[table][col][0]
    if t in ("int", "bigint", "smallint", "mediumint"): return 1
    if t in ("decimal", "float", "double"): return 1
    if t == "tinyint": return 0
    if t == "date": return "2026-01-01"
    if t in ("datetime", "timestamp"): return "2026-01-01T00:00:00"
    if t == "json": return "{}"
    if t in ("text", "mediumtext", "longtext"): return "test"
    s = f"t{unique}{RUN}"
    return s[:n] if n else s

class Factory:
    def __init__(self, tok, tenant):
        self.tok, self.tenant, self.made, self.errors = tok, tenant, {}, {}
        self.link_to = set()   # tables whose foreign keys are filled even when optional (row-scope chains)
    def row(self, table, unique=None):
        unique = unique or str(random.randint(100, 999999))
        info, body = tables[table], {}
        for col, spec in info["cols"].items():
            if spec["pk"] or col in AUDIT_COLS: continue
            if col == TCOL: body[col] = self.tenant; continue
            ref = info["fks"].get(col)
            if ref:
                if not spec["notnull"] and ref not in self.link_to: continue
                parent = self.tenant if ref == TTABLE else self.ensure(ref)
                if parent is None: return None
                body[col] = parent; continue
            if not spec["notnull"] and spec["type"] not in ("varchar", "char") and col != getattr(self, "scope_col", None): continue
            body[col] = value(table, col, spec, unique)
        return body
    def ensure(self, table):
        """Id of a row of `table` usable by this tenant (created once)."""
        if table in self.made: return self.made[table]
        if table not in TENANT_TABLES:  # shared table: any existing row, else created by the platform admin
            st, r = call("GET", f"{ctl(table)}?itemsPerPage=1", tok=ADMIN)
            recs = (doc((st, r)) or {}).get("records") or []
            if recs: self.made[table] = recs[0][tables[table]["pk"]]; return self.made[table]
            body = Factory(ADMIN, None).row(table)
            st, r = call("POST", ctl(table), body, ADMIN)
            self.made[table] = doc((st, r)) if st == 200 else None
            return self.made[table]
        self.made[table] = None  # cycle guard
        body = self.row(table)
        if body is None: self.errors[table] = "parent could not be created"; return None
        st, r = call("POST", ctl(table), body, self.tok)
        if st == 200 and isinstance(doc((st, r)), int): self.made[table] = doc((st, r))
        else: self.errors[table] = f"{st} {r.get('message') if isinstance(r, dict) else r}"[:160]
        return self.made[table]

# ------------------------------------------------------------------ setup: two tenants with an admin each
report = {"app": APP, "checks": [], "skipped": {}}
def check(cat, name, ok, detail=""):
    report["checks"].append({"category": cat, "check": name, "ok": bool(ok), "detail": str(detail)[:300]})
    if not ok: print(f"FAIL [{cat}] {name}  {str(detail)[:200]}")

if not TTABLE or not TENANT_TABLES:
    print("No tenancy configured (ui.config.json \"tenancy\"); only immutability and lifecycle checks run.")
roles = {r["role_name"]: r["role_id"] for r in doc(call("GET", "Access/roles", tok=ADMIN)) or []}
users = {}
if TTABLE:
    for label in ("a", "b"):
        body = Factory(ADMIN, None).row(TTABLE, unique=label)
        st, r = call("POST", ctl(TTABLE), body, ADMIN)
        if st != 200: sys.exit(f"could not create tenant: {st} {r}")
        tid = doc((st, r)); uname = f"guardtest_{label}{RUN}"
        call("POST", "Access/members", {"username": uname, "password": "GuardTest!2026", "role_id": roles.get("Administrator", 1), "tenant_id": tid}, ADMIN)
        users[label] = (tid, login(uname, "GuardTest!2026"))
    (ta, tokA), (tb, tokB) = users["a"], users["b"]
    A, Bf = Factory(tokA, ta), Factory(tokB, tb)

    # ------------------------------------------------------------------ tenancy checks per table
    for t in TENANT_TABLES:
        rid = A.ensure(t)
        if rid is None: report["skipped"][t] = A.errors.get(t, "not created"); continue
        pk = tables[t]["pk"]
        st, r = call("GET", f"{ctl(t)}?itemsPerPage=1000", tok=tokB)
        ids = [x[pk] for x in ((doc((st, r)) or {}).get("records") or [])]
        check("tenancy", f"{t}: list hides other tenant", rid not in ids, ids[:5])
        st, r = call("GET", f"{ctl(t)}/{rid}", tok=tokB)
        check("tenancy", f"{t}: get by id hides other tenant", not doc((st, r)), (st, r if st != 200 else doc((st, r))))
        before = doc(call("GET", f"{ctl(t)}/{rid}", tok=ADMIN))
        txt = next((c for c, s in tables[t]["cols"].items() if s["type"] == "varchar" and c not in AUDIT_COLS
                    and c not in OPTIONS.get(t, {}) and c != (LIFE.get(t) or {}).get("column")), None)
        if before and txt:
            changed = dict(before); changed[txt] = "tampered"
            call("PUT", f"{ctl(t)}/{rid}", changed, tokB)
            after = doc(call("GET", f"{ctl(t)}/{rid}", tok=ADMIN))
            check("tenancy", f"{t}: other tenant can't update", after and after.get(txt) == before.get(txt), (before.get(txt), after and after.get(txt)))
        call("DELETE", f"{ctl(t)}/{rid}", tok=tokB)
        check("tenancy", f"{t}: other tenant can't delete", doc(call("GET", f"{ctl(t)}/{rid}", tok=ADMIN)) is not None)
        if t not in IMM:
            body = Bf.row(t)
            if body is not None:
                body[TCOL] = ta
                st, r = call("POST", ctl(t), body, tokB)
                if st == 200:
                    row = doc(call("GET", f"{ctl(t)}/{doc((st, r))}", tok=ADMIN))
                    check("tenancy", f"{t}: tenant id in request is replaced", row and row.get(TCOL) == tb, row and row.get(TCOL))
                else:
                    report["skipped"][t + " (spoof)"] = f"{st} {r.get('message') if isinstance(r, dict) else r}"[:160]

    # ------------------------------------------------------------------ shared tables read-only for tenants
    for t in SHARED:
        body = Factory(ADMIN, None).row(t)
        if body is None: continue
        st, r = call("POST", ctl(t), body, tokA)
        check("tenancy", f"{t}: shared table is read-only for tenant users", st == 403, (st, r if st != 200 else "created"))

# ------------------------------------------------------------------ row scopes (inside tenant A)
def scope_tables(sc):
    col = sc["column"].lower()
    home = next((t for t in order if tables[t]["pk"] == col), None)
    only, excl = set(sc.get("tables") or order), set(sc.get("exclude") or [])
    ok = lambda t: t in only and t not in excl
    direct = {t for t in order if ok(t) and col in tables[t]["cols"] and t != home}
    reached = set(direct) | ({home} if home else set())
    linked = set()
    for _ in range(3):
        new = {t for t in order if ok(t) and t not in reached and any(r in reached for r in tables[t]["fks"].values())}
        if not new: break
        linked |= new; reached |= new
    return home, direct, linked

if users and CFG.get("rowScopes"):
    for sc in CFG["rowScopes"]:
        home, direct, linked = scope_tables(sc)
        uv = sc.get("userValue") or {}
        if not home or not uv.get("table") or not sc.get("roles"):
            report["skipped"]["rowScope " + sc.get("name", "?")] = "needs a home table, userValue and roles"; continue
        homes, toks = [], []
        for k in (1, 2):
            st, r = call("POST", ctl(home), A.row(home, unique=f"h{k}"), tokA)
            if st != 200: report["skipped"]["rowScope " + sc["name"]] = f"home row: {st} {r}"; break
            hid = doc((st, r)); homes.append(hid)
            uname = f"guardscope{k}_{RUN}"
            call("POST", "Access/members", {"username": uname, "password": "GuardTest!2026", "role_id": roles.get(sc["roles"][0]), "tenant_id": ta}, ADMIN)
            link = A.row(uv["table"], unique=f"l{k}") or {}
            link[uv["match"]] = uname; link[uv["column"]] = hid
            st, r = call("POST", ctl(uv["table"]), link, tokA)
            if st != 200: report["skipped"]["rowScope " + sc["name"]] = f"user link: {st} {r}"; break
            toks.append(login(uname, "GuardTest!2026"))
        if len(toks) < 2: continue
        F2 = Factory(tokA, ta); F2.made[home] = homes[1]
        F2.link_to = {home} | direct | linked; F2.scope_col = sc["column"].lower()
        for t in sorted(direct | linked):
            if t == uv["table"]: continue
            rid = F2.ensure(t)
            if rid is None: report["skipped"][f"{t} (scope {sc['name']})"] = F2.errors.get(t, "not created"); continue
            pk = tables[t]["pk"]
            st1, r1 = call("GET", f"{ctl(t)}?itemsPerPage=1000", tok=toks[0])
            if st1 != 200: report["skipped"][f"{t} (scope {sc['name']})"] = f"role can't read ({st1})"; continue
            check("rowscope", f"{t}: other {sc['name']}'s row hidden from list", rid not in [x[pk] for x in ((doc((st1, r1)) or {}).get("records") or [])])
            check("rowscope", f"{t}: other {sc['name']}'s row hidden by id", not doc(call("GET", f"{ctl(t)}/{rid}", tok=toks[0])))
            check("rowscope", f"{t}: own {sc['name']} sees the row", doc(call("GET", f"{ctl(t)}/{rid}", tok=toks[1])) is not None)
        st, r = call("GET", f"{ctl(home)}?itemsPerPage=1000", tok=toks[0])
        if st == 200:
            check("rowscope", f"{home}: scoped user sees only its own record", [x[tables[home]['pk']] for x in doc((st, r))["records"]] == [homes[0]])

# ------------------------------------------------------------------ append-only
writer = users["a"][1] if users else ADMIN
fac = A if users else Factory(ADMIN, None)
for t in IMM:
    if t not in tables: continue
    rid = fac.ensure(t) if t in TENANT_TABLES or not users else None
    if rid is None:
        st, r = call("GET", f"{ctl(t)}?itemsPerPage=1", tok=ADMIN)
        recs = (doc((st, r)) or {}).get("records") or []
        rid = recs[0][tables[t]["pk"]] if recs else None
    if rid is None: report["skipped"][t + " (append-only)"] = "no row to test"; continue
    row = doc(call("GET", f"{ctl(t)}/{rid}", tok=ADMIN))
    st, r = call("PUT", f"{ctl(t)}/{rid}", row, ADMIN)
    check("immutable", f"{t}: append-only rejects updates", st == 409, (st, r))

# ------------------------------------------------------------------ lifecycles
for t, lc in LIFE.items():
    if t not in tables or not lc.get("initial"): continue
    col, init = lc.get("column", "status"), lc["initial"][0]
    states = set()
    for tr in lc.get("transitions", []):
        for k in ("from", "to"):
            v = tr.get(k); states.update(v if isinstance(v, list) else [v])
    reachable = set()
    for tr in lc.get("transitions", []):
        fr = tr.get("from"); fr = fr if isinstance(fr, list) else [fr]
        if init in fr or "*" in fr:
            to = tr.get("to"); reachable.update(to if isinstance(to, list) else [to])
    jump = sorted(s for s in states - reachable - {init, "*", None})
    if not jump: continue
    rid = fac.ensure(t) if users or t not in TENANT_TABLES else None
    if rid is None: report["skipped"][t + " (lifecycle)"] = fac.errors.get(t, "not created"); continue
    row = doc(call("GET", f"{ctl(t)}/{rid}", tok=ADMIN))
    if not row or row.get(col) != init: report["skipped"][t + " (lifecycle)"] = f"row not in {init}"; continue
    row[col] = jump[-1]
    st, r = call("PUT", f"{ctl(t)}/{rid}", row, ADMIN)
    check("lifecycle", f"{t}: {init} -> {jump[-1]} rejected (not an allowed step)", st == 409, (st, r))

# ------------------------------------------------------------------ report
cats = {}
for c in report["checks"]:
    k = cats.setdefault(c["category"], [0, 0]); k[0] += c["ok"]; k[1] += 1
print("\n" + "\n".join(f"{k:10} {v[0]}/{v[1]} passed" for k, v in cats.items()))
if report["skipped"]:
    print(f"skipped {len(report['skipped'])}: " + "; ".join(f"{k} ({v})" for k, v in list(report["skipped"].items())[:8]))
json.dump(report, open(os.path.join(WS, "guard-test-report.json"), "w"), indent=2)
failed = sum(not c["ok"] for c in report["checks"])
print(f"TOTAL {len(report['checks']) - failed}/{len(report['checks'])} passed")
sys.exit(1 if failed else 0)
