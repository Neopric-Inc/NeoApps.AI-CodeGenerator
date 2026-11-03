"""Demo data for ConstructOS: python3 seed.py <admin-username> <admin-password> [api-base]
(default http://localhost:5100/v1/api/). Loads a demo job across all modules, adds one login per role
(password Test#12345) and checks that each role is allowed / denied as configured."""
import json, sys, urllib.request, urllib.error

B = sys.argv[3] if len(sys.argv) > 3 else "http://localhost:5100/v1/api/"

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

T = login(sys.argv[1], sys.argv[2])
fails = []

def mk(table, **v):
    v.setdefault("createdBy", "x"); v.setdefault("modifiedBy", "x")
    s, r = call("POST", table.capitalize(), v, T)
    if s != 200 or not isinstance(r, dict) or r.get("code") != 1:
        fails.append((table, s, r)); return None
    return r["document"]

# Directory
cc = {c: mk("cost_codes", code=c, title=t, csi_division=c[:2], cost_type="subcontract") for c, t in
      [("03-300", "Cast-in-Place Concrete"), ("09-200", "Drywall & Framing"), ("23-000", "HVAC"), ("26-000", "Electrical")]}
owner = mk("companies", name="Harbourview Health Partners", company_type="owner", prequal_status="approved")
arch = mk("companies", name="Lindqvist Architects", company_type="architect", prequal_status="approved")
subs = {n: mk("companies", name=n, company_type="subcontractor", trades=t, prequal_status=p, insurance_expiry=e)
        for n, t, p, e in [("Atlantic Concrete Ltd", "Concrete", "approved", "2027-05-01"),
                           ("Maritime Drywall Inc", "Drywall, ACT", "approved", "2026-10-15"),
                           ("Fundy Interiors", "Drywall, ACT", "conditional", "2027-02-01"),
                           ("Tidewater Mechanical", "HVAC, Plumbing", "approved", "2027-03-31")]}
pm = mk("contacts", company_id=owner, full_name="Dana Leblanc", title="Owner's Rep")

# Project
p = mk("projects", project_number="24-118", name="Harbourview Medical Office Building", owner_company_id=owner,
       architect_company_id=arch, address="200 Main St, Moncton NB", project_type="healthcare", delivery_method="cm_at_risk",
       phase="construction", contract_value=18450000, retainage_pct=10, start_date="2026-04-06", substantial_completion="2027-08-30")
mk("project_team", project_id=p, contact_id=pm, project_role="owner_rep", is_primary=1)
specs = mk("documents", project_id=p, title="Project Manual Vol 1 (Div 01-14)", document_type="specifications", discipline="general", revision="IFC")

# Precon
ss = mk("spec_sections", project_id=p, document_id=specs, section_number="09 21 16", title="Gypsum Board Assemblies", csi_division="09")
pk = mk("trade_packages", project_id=p, cost_code_id=cc["09-200"], package_number="BP-09", name="Drywall & Acoustical Ceilings",
        budget_amount=1240000, bid_due_date="2026-02-20", status="leveling")
si1 = mk("scope_items", trade_package_id=pk, spec_section_id=ss, item_no=1, description="Metal stud framing at all interior partitions", scope_category="included")
si2 = mk("scope_items", trade_package_id=pk, spec_section_id=ss, item_no=2, description="Firestopping at own penetrations", scope_category="included")
for s in ("Maritime Drywall Inc", "Fundy Interiors"):
    mk("bid_invitations", trade_package_id=pk, company_id=subs[s], invited_date="2026-01-28", response="submitted")
b1 = mk("bids", trade_package_id=pk, company_id=subs["Maritime Drywall Inc"], base_amount=1187500, status="leveling")
b2 = mk("bids", trade_package_id=pk, company_id=subs["Fundy Interiors"], base_amount=1129000, status="leveling", is_low_bid=1)
mk("bid_line_items", bid_id=b2, scope_item_id=si1, description="Framing", amount=612000, inclusion="included")
mk("scope_gaps", bid_id=b2, scope_item_id=si2, gap_type="excluded", description="Fundy excludes firestopping (proposal p.2)", plug_amount=38500, resolution="open")

# Field
mk("schedule_activities", project_id=p, trade_package_id=pk, activity_code="A1240", name="L2 interior framing", planned_start="2026-11-02",
   planned_finish="2026-12-11", percent_complete=35, is_critical=1, status="in_progress")
dl = mk("daily_logs", project_id=p, log_date="2026-11-02", weather="Overcast", temp_high=9, temp_low=2,
        work_performed="Maritime Drywall 8 men framing L2 east. Tidewater 4 setting RTU curbs. Ceiling grid damaged at L1 corridor.")
mk("daily_log_crews", daily_log_id=dl, company_id=subs["Maritime Drywall Inc"], workers=8, hours=64, work_area="L2 east")
rfi = mk("rfis", project_id=p, rfi_number="RFI-042", subject="Rated wall type at L2 corridor", question="A-501 shows 1-hr, life safety plan shows 2-hr. Which governs?",
         spec_section_id=ss, assigned_company_id=arch, date_sent="2026-10-28", date_due="2026-11-04", cost_impact="yes", status="open")
sub = mk("submittals", project_id=p, spec_section_id=ss, company_id=subs["Maritime Drywall Inc"], submittal_number="09 21 16-001",
         title="Gypsum board product data", submittal_type="product_data", status="with_architect")
mk("submittal_reviews", submittal_id=sub, spec_requirement="Type X 5/8in at rated assemblies (2.2.A)", submitted_value="Type X 5/8in", compliance="complies")
mk("punch_items", project_id=p, company_id=subs["Maritime Drywall Inc"], location="L1 corridor", description="Replace damaged ceiling grid", priority="high", status="open")
mk("safety_incidents", project_id=p, incident_date="2026-10-30", incident_type="near_miss", severity="moderate", description="Unsecured guardrail at L3 slab edge", status="open")
mt = mk("meetings", project_id=p, meeting_type="oac", meeting_date="2026-10-29", minutes="Architect to answer RFI-042 by Nov 4.")
mk("action_items", meeting_id=mt, company_id=arch, description="Answer RFI-042 rated wall conflict", due_date="2026-11-04", status="open")
mk("time_entries", project_id=p, contact_id=pm, cost_code_id=cc["03-300"], work_date="2026-11-02", regular_hours=8, approval="submitted")
mk("inspections", project_id=p, inspection_type="Above-ceiling L1", agency="City of Moncton", scheduled_date="2026-11-12", result="scheduled")

# Financials
mk("budget_lines", project_id=p, cost_code_id=cc["09-200"], description="Drywall & ACT", original_budget=1240000, committed_cost=1167500)
sc = mk("subcontracts", project_id=p, trade_package_id=pk, company_id=subs["Fundy Interiors"], contract_number="SC-09-01",
        title="Drywall & ACT", original_amount=1167500, status="executed", insurance_verified=1)
sov = mk("subcontract_items", subcontract_id=sc, cost_code_id=cc["09-200"], description="Framing", scheduled_value=612000)
mk("purchase_orders", project_id=p, company_id=subs["Tidewater Mechanical"], cost_code_id=cc["23-000"], po_number="PO-0311", amount=86400, status="issued")
ce = mk("change_events", project_id=p, rfi_id=rfi, event_number="CE-017", title="Upgrade L2 corridor to 2-hr", origin="design_error", estimated_cost=42000, status="pricing")
co = mk("change_orders", project_id=p, change_event_id=ce, co_number="PCO-CE-017", co_type="prime", title="2-hr corridor upgrade", amount=44100, status="draft")
mk("change_order_lines", change_order_id=co, cost_code_id=cc["09-200"], description="Add layer Type X both sides", cost_type="subcontract", amount=42000)
pa = mk("pay_applications", project_id=p, subcontract_id=sc, app_number=3, pay_direction="sub_to_gc", period_to="2026-10-31", scheduled_value=1167500,
        work_completed=298000, retainage_held=29800, amount_due=118400, status="under_review")
mk("pay_app_lines", pay_application_id=pa, subcontract_item_id=sov, description="Framing", scheduled_value=612000, previous_completed=180000,
   this_period=118000, percent_complete=48.7, flag="48.7% billed vs 35% schedule progress")
mk("lien_waivers", pay_application_id=pa, company_id=subs["Fundy Interiors"], waiver_type="conditional_progress", amount=118400, status="requested")
mk("invoices", project_id=p, company_id=subs["Tidewater Mechanical"], cost_code_id=cc["23-000"], invoice_number="TM-5521", amount=21600, status="received")

# Closeout
mk("closeout_items", project_id=p, spec_section_id=ss, item_type="attic_stock", description="2% gypsum board attic stock", status="required")
w = mk("warranties", project_id=p, company_id=subs["Tidewater Mechanical"], system_name="RTU-1..4", manufacturer="Trane", duration_months=24)
mk("warranty_claims", warranty_id=w, reported_date="2026-11-01", issue="RTU-2 compressor noise (test entry)", status="reported")

# Reads, joins
s, r = call("GET", "Pay_app_lines?pageNo=1&pageSize=5", tok=T)
print("list pay_app_lines:", s, (r.get("document") or {}).get("records", [{}])[0].get("flag") if isinstance(r, dict) else r)

# Roles
s, roles = call("GET", "Access/roles", tok=T)
rid = {x["role_name"]: x["role_id"] for x in roles["document"]}
print("roles:", list(rid))
checks = []
VALID = {
  "Daily_logs": {"project_id": p, "log_date": "2026-11-03"},
  "Subcontracts": {"project_id": p, "company_id": subs["Tidewater Mechanical"], "contract_number": "SC-X", "title": "x", "status": "draft"},
  "Invoices": {"project_id": p, "company_id": subs["Tidewater Mechanical"], "invoice_number": "X-1", "status": "received"},
  "Rfis": {"project_id": p, "rfi_number": "RFI-X", "subject": "x", "question": "x", "status": "draft"},
  "Bids": {"trade_package_id": pk, "company_id": subs["Fundy Interiors"], "status": "received"},
  "Budget_lines": {"project_id": p, "cost_code_id": cc["09-200"]},
  "Projects": {"project_number": "X", "name": "x", "phase": "bidding"},
}
for role, user, allowed, denied in [
    ("Superintendent", "super1", "Daily_logs", "Subcontracts"),
    ("Accounting", "acct1", "Invoices", "Rfis"),
    ("Subcontractor", "sub1", "Bids", "Budget_lines"),
    ("Owner Rep", "owner1", None, "Projects")]:
    call("POST", "Access/members", {"username": user, "password": "Test#12345", "role_id": rid[role]}, T)
    ut = login(user, "Test#12345")
    au = {"createdBy": "x", "modifiedBy": "x"}
    sa = call("POST", allowed, {**VALID[allowed], **au}, ut)[0] if allowed else None
    sd = call("POST", denied, {**VALID[denied], **au}, ut)[0]
    checks.append((role, allowed, sa, denied, sd))
for c in checks:
    print(f"{c[0]:15} create {c[1]}: {c[2]}   create {c[3]}: {c[4]}")
s, r = call("GET", "Budget_lines?pageNo=1&pageSize=1", tok=login("sub1", "Test#12345"))
print("Subcontractor read Budget_lines:", s)
print("FAILURES:", fails if fails else "none")
