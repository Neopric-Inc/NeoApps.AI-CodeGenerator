#!/usr/bin/env python3
"""Stand-in for cloud.neoapps.ai (docs/CLOUD.md): checks the bundle the CLI sends and applies db/init.sql to a fresh local database.
Usage: python3 scripts/cloud-stub.py 8765   (token nac_test123; PAID=0 to get a checkout link instead)"""
import json, os, re, subprocess, tarfile, threading, sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
TOKEN = "nac_test123"; PORT = int(sys.argv[1]); PAID = os.environ.get("PAID", "1") == "1"
jobs = {}; uploads = {}; issued = set()
SLUG = re.compile(r"^[a-z0-9][a-z0-9-]{0,29}$")
import tempfile; WORK = tempfile.mkdtemp(prefix="neoapps-cloud-stub-")
def run_job(jid, slug, blob):
    ev = jobs[jid]["events"]
    def log(m): ev.append({"id": len(ev) + 1, "level": "info", "message": m})
    try:
        d = os.path.join(WORK, "extract", slug); subprocess.run(["rm", "-rf", d]); os.makedirs(d)
        with tarfile.open(uploads[blob]) as t: t.extractall(d, filter="data")
        m = json.load(open(f"{d}/manifest.json")); log(f"manifest ok: {m['app']} region={m['region']} web={m['hasWeb']}")
        names = os.listdir(d); assert {"api", "docker-compose.yml", "db"} <= set(names), names
        for root, dirs, files in os.walk(d):
            assert not ({"bin", "obj", "node_modules"} & set(dirs)), root
            for f in files:
                if f.startswith("appsettings") and f.endswith(".json"):
                    j = json.load(open(os.path.join(root, f)))
                    assert not j.get("AppSettings", {}).get("Secret"), "secret leaked in " + f
                    assert not j.get("ConnectionStrings", {}).get("MySQLDatabase"), "conn leaked in " + f
        log("no secrets in appsettings, no build folders")
        db = f"cloud_{slug.replace('-', '_')}"
        sql = f"DROP DATABASE IF EXISTS {db}; CREATE DATABASE {db};"
        subprocess.run(["mysql", "-uroot", "-pneoapps", "-h127.0.0.1", "-e", sql], check=True, capture_output=True)
        r = subprocess.run(["mysql", "-uroot", "-pneoapps", "-h127.0.0.1", db], stdin=open(f"{d}/db/init.sql"), capture_output=True, text=True)
        assert r.returncode == 0, r.stderr[-500:]
        out = subprocess.run(["mysql", "-uroot", "-pneoapps", "-h127.0.0.1", "-N", "-e",
            f"SELECT (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='{db}'), (SELECT COUNT(*) FROM {db}.users), (SELECT COUNT(*) FROM {db}.roles)"],
            capture_output=True, text=True).stdout.split()
        log(f"database created: {out[0]} tables, {out[1]} user, {out[2]} roles")
        jobs[jid].update(status="completed", result={"url": f"https://{slug}.{m['region']}.neoapps.ai", "adminUsername": m["adminUsername"], "adminPassword": "Fresh-Pass-123"})
    except Exception as e:
        log("FAILED: " + str(e)); jobs[jid].update(status="failed", error=str(e))
class H(BaseHTTPRequestHandler):
    def log_message(self, *a): pass
    def send(self, code, obj):
        b = json.dumps(obj).encode(); self.send_response(code); self.send_header("Content-Type", "application/json"); self.end_headers(); self.wfile.write(b)
    def auth(self):
        if self.headers.get("Authorization") != f"Bearer {TOKEN}": self.send(401, {"message": "bad token"}); return False
        return True
    def body(self):
        n = int(self.headers.get("Content-Length") or 0); return self.rfile.read(n)
    def do_POST(self):
        if not self.auth(): return
        b = self.body(); p = self.path
        if p == "/v1/api/cloud/apps":
            j = json.loads(b)
            if not PAID: return self.send(200, {"slug": j["slug"], "checkoutUrl": "https://checkout.stripe.com/test"})
            return self.send(200, {"slug": j["slug"], "region": j["region"], "status": "new"})
        if p.endswith("/upload"):
            slug = p.split("/")[-2]
            if not SLUG.match(slug): return self.send(400, {"message": "invalid slug"})
            blob = f"{slug}-1.tar.gz"; issued.add(blob)
            return self.send(200, {"uploadUrl": f"http://127.0.0.1:{PORT}/blob/{blob}", "blob": blob})
        if p == "/v1/api/Jobs":
            j = json.loads(b); jid = f"job{len(jobs) + 1}"
            slug, blob = str(j["input"].get("slug", "")), str(j["input"].get("blob", ""))
            if not SLUG.match(slug) or blob not in uploads: return self.send(400, {"message": "unknown slug or upload"})
            jobs[jid] = {"status": "running", "events": [], "result": None, "error": None}
            threading.Thread(target=run_job, args=(jid, slug, blob)).start()
            return self.send(200, {"job_id": jid, "status": "queued"})
        self.send(404, {"message": "no route"})
    def do_PUT(self):
        # only names this stub handed out are written, so the path can't leave WORK
        blob = self.path.split("/")[-1]; data = self.body()
        if blob not in issued: return self.send(400, {"message": "unknown upload"})
        path = os.path.join(WORK, blob)
        assert self.headers.get("x-ms-blob-type") == "BlockBlob"
        open(path, "wb").write(data); uploads[blob] = path; self.send_response(201); self.end_headers()
    def do_GET(self):
        if not self.auth(): return
        jid = self.path.split("/")[4]; after = int(self.path.split("after=")[1].split("&")[0])
        import time; time.sleep(0.3); j = jobs[jid]
        self.send(200, {"job": {"job_id": jid, "status": j["status"], "result": j["result"], "error": j["error"]}, "events": [e for e in j["events"] if e["id"] > after]})
ThreadingHTTPServer(("127.0.0.1", PORT), H).serve_forever()
