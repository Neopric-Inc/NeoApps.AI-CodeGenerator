# Seeds the ExpertOS sample (samples/expertos.sql + expertos.ui.json) with one case walked to expert review. Usage: seed-expertos.py <admin_user> <admin_password>
import json, sys, urllib.request
B = "http://localhost:5100/v1/api/"
USER, PW = sys.argv[1], sys.argv[2]

def call(method, path, body=None, tok=None):
    req = urllib.request.Request(B + path, method=method, data=json.dumps(body).encode() if body is not None else None,
                                 headers={"Content-Type": "application/json", **({"Authorization": "Bearer " + tok} if tok else {})})
    try:
        with urllib.request.urlopen(req) as r: return r.status, json.loads(r.read() or b"null")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:300]

TOK = call("POST", "Token", {"username": USER, "password": PW})[1]["document"]["accessToken"]
def ctl(t): return t[0].upper() + t[1:]
def upd(table, id_, as_=None, **changes):
    st, r = call("GET", f"{ctl(table)}/{id_}", None, TOK)
    row = r["document"]; row.update(changes)
    st, r = call("PUT", f"{ctl(table)}/{id_}", row, as_ or TOK)
    if st != 200: sys.exit(f"FAIL update {table} {id_} {st} {r} {changes}")
def walk(table, id_, col, *states, as_=None):
    for x in states: upd(table, id_, as_, **{col: x})
def add(table, **row):
    st, r = call("POST", ctl(table), row, TOK)
    if st != 200 or not isinstance(r, dict) or r.get("code") != 1: sys.exit(f"FAIL {table} {st} {r} {row}")
    return r["document"]

ten = add("saas_tenants", tenant_code="NORTHSTAR", name="Northstar Procurement Advisors", legal_name="Northstar Procurement Advisors Inc.",
          primary_email="ops@northstar.example", country="Canada", region="ca", timezone="America/Moncton", deployment_model="shared", status="active")
T = {"tenant_id": ten}

# plans
starter = add("saas_plans", plan_code="STARTER", name="Starter", monthly_price=99, currency="USD", included_seats=3, included_verticals=1, monthly_ai_credits=500, ai_tier="basic", workflow_tier="basic", analytics_tier="basic", sso_enabled=0, dedicated_infra_option=0, is_custom=0, status="active")
growth = add("saas_plans", plan_code="GROWTH", name="Growth", monthly_price=299, currency="USD", included_seats=10, included_verticals=3, monthly_ai_credits=2500, ai_tier="advanced", workflow_tier="advanced", analytics_tier="advanced", sso_enabled=0, dedicated_infra_option=0, is_custom=0, status="active")
add("saas_plans", plan_code="ENTERPRISE", name="Enterprise", currency="USD", included_verticals=6, ai_tier="configurable", workflow_tier="advanced", analytics_tier="advanced", sso_enabled=1, dedicated_infra_option=1, is_custom=1, status="active")
for f, q in [("employee_seats", 10), ("active_verticals", 3), ("ai_credits", 2500), ("customer_portal", 1), ("case_management", 1)]:
    add("saas_plan_features", saas_plan_id=growth, feature_code=f, included_quantity=q, is_enabled=1)
sub = add("saas_subscriptions", **T, saas_plan_id=growth, status="active", billing_cycle="monthly", seats=10)

# platform roles + permission catalog
for code, n in [("PLATFORM_SUPER_ADMIN", "Platform Super Admin"), ("SAAS_OPS_ADMIN", "SaaS Operations Admin"), ("SAAS_BILLING_ADMIN", "SaaS Billing Admin"),
                ("PLATFORM_SECURITY_ADMIN", "Platform Security Admin"), ("PLATFORM_SUPPORT", "Platform Support Agent")]:
    add("platform_roles", role_code=code, name=n)
for p in ["cases.read", "cases.create", "cases.assign", "cases.update", "cases.close", "documents.read", "documents.upload", "documents.download",
          "documents.delete", "ai.execute", "ai.review", "ai.configure", "approvals.submit", "approvals.approve", "quality.review", "quality.override",
          "billing.read", "billing.manage", "users.manage", "roles.manage"]:
    add("permission_catalog", permission_code=p, module_code=p.split(".")[0],
        risk_level="high" if p.split(".")[1] in ("delete", "approve", "override", "manage", "close", "configure") else "normal")

# verticals
V = {}
verts = [("INS", "Insurance Claims", "Claims intake, policy review, evidence extraction, claim preparation and adjuster review", ["Claims Intake", "Policy Document Review", "Claims Assessment"], "Insurance privacy, claims handling and licensing requirements"),
         ("TAX", "Tax Preparation", "Tax document intake, income extraction, reconciliation and preparation assistance", ["Tax Document Processing", "Income Reconciliation", "Tax Return Preparation Assistance"], "Taxpayer confidentiality and tax-preparer obligations"),
         ("MED", "Medical Billing", "Billing intake, coding support, claim validation and denial management", ["Billing Validation", "Coding Assistance", "Denial Analysis"], "HIPAA and business associate obligations where applicable"),
         ("IMM", "Immigration Paperwork", "Client onboarding, document checklists, application completeness and package preparation", ["Application Intake", "Evidence Collection", "Application Completeness Review"], "Personal information protection; unauthorized-practice restrictions"),
         ("SCM", "Supply Chain", "Supplier analysis, procurement review, spend analysis and inventory recommendations", ["Supplier Quote Analysis", "Procurement Spend Analysis", "Supplier Performance Review"], "Commercial confidentiality and trade information protection"),
         ("BEN", "Benefits Administration", "Benefits intake, eligibility documentation, application completeness and renewals", ["Benefits Intake", "Eligibility Document Review", "Renewal Management"], "Personal data, financial information and benefits regulations")]
MODS = {}
for code, name, desc, mods, comp in verts:
    vid = add("verticals", vertical_code=code, name=name, description=desc, compliance_notes=comp, status="available")
    V[code] = vid
    add("vertical_versions", vertical_id=vid, version_label="1.0", status="released",
        config_json=json.dumps({"verticalCode": code, "version": "1.0", "enabledModules": [m.lower().replace(" ", "_") for m in mods]}))
    for m in mods:
        MODS[m] = add("vertical_modules", vertical_id=vid, module_code=m.lower().replace(" ", "_"), name=m, status="active")
add("tenant_verticals", **T, vertical_id=V["SCM"], status="active")
add("tenant_module_settings", **T, vertical_module_id=MODS["Supplier Quote Analysis"], is_enabled=1)

# document types (SCM + one per other vertical)
DT = {}
for code, name, fields in [("supplier_quotation", "Supplier quotation", ["supplier", "product", "unit_price", "minimum_order", "lead_time", "payment_terms", "currency"]),
                           ("purchase_order", "Purchase order", ["po_number", "supplier", "lines", "total", "currency"]),
                           ("invoice", "Supplier invoice", ["invoice_number", "supplier", "total", "currency", "due_date"]),
                           ("product_catalog", "Product catalog", []), ("inventory_report", "Inventory report", []),
                           ("sales_report", "Sales report", []), ("delivery_record", "Delivery record", []), ("supplier_contract", "Supplier contract", [])]:
    DT[code] = add("document_types", vertical_id=V["SCM"], type_code=code, name=name, schema_version="1.0", is_platform_defined=1,
                   extraction_schema_json=json.dumps({"fields": fields}))
for v, code, name in [("INS", "claim_form", "Claim form"), ("INS", "insurance_policy", "Insurance policy"), ("TAX", "t4_w2", "Income slip (T4/W-2)"),
                      ("MED", "superbill", "Superbill"), ("MED", "remittance_advice", "Remittance advice / EOB"), ("IMM", "passport", "Passport"),
                      ("BEN", "enrollment_form", "Benefits enrollment form")]:
    add("document_types", vertical_id=V[v], type_code=code, name=name, schema_version="1.0", is_platform_defined=1)

# AI platform
prov = add("ai_providers", provider_code="anthropic", name="Anthropic", status="active", data_policy="No training on customer data")
model = add("ai_models", ai_provider_id=prov, model_code="claude-sonnet-5-5", name="Claude Sonnet 5.5", context_window=200000, timeout_seconds=120, max_retries=2, status="active")
AG = {}; AGID = {}
for code, name, scope, desc in [("INTAKE", "Intake Agent", "shared", "Understands customer requests"),
                                ("CLASSIFICATION", "Classification Agent", "shared_plus_vertical_rules", "Identifies service and case type"),
                                ("DOCUMENT_INTELLIGENCE", "Document Intelligence Agent", "shared", "Extracts document information"),
                                ("VALIDATION", "Validation Agent", "shared_plus_vertical_rules", "Checks completeness and consistency"),
                                ("RESEARCH", "Research Agent", "shared", "Retrieves approved reference information"),
                                ("ANALYSIS", "Analysis Agent", "specialized", "Industry-specific analysis"),
                                ("SUPPLIER_RISK", "Supplier Risk Agent", "specialized", "Identifies supplier-related concerns; separates facts from inferred risks"),
                                ("RECOMMENDATION", "Recommendation Agent", "specialized", "Generates proposed actions"),
                                ("QUALITY_ASSURANCE", "QA Agent", "shared_plus_vertical_rules", "Checks deliverable quality; never approves its own work"),
                                ("REPORTING", "Reporting Agent", "shared", "Generates final reports from validated data and templates"),
                                ("COMMUNICATION", "Communication Agent", "shared", "Prepares customer communications for human approval")]:
    a = add("ai_agents", agent_code=code, name=name, description=desc, agent_scope=scope, status="active")
    AGID[code] = a
    AG[code] = add("ai_agent_versions", ai_agent_id=a, version_label="1.0", ai_model_id=model, status="released",
                   tool_permissions="read:case,read:documents(authorized refs only); write:proposals only")
for code in ["INTAKE", "DOCUMENT_INTELLIGENCE", "ANALYSIS", "SUPPLIER_RISK", "RECOMMENDATION", "QUALITY_ASSURANCE", "REPORTING"]:
    add("vertical_agent_profiles", vertical_id=V["SCM"], ai_agent_id=AGID[code], ai_agent_version_id=AG[code], role_in_workflow=code.lower())
aiwf = add("ai_workflows", vertical_id=V["SCM"], workflow_code="supplier_quote_analysis_v1", name="Supplier Quote Analysis pipeline")
aiwfv = add("ai_workflow_versions", ai_workflow_id=aiwf, version_label="1.0", status="released",
            steps_json=json.dumps(["intake", "document_intelligence", ["procurement_analysis", "supplier_risk"], "recommendation", "quality_assurance", "human_review", "report_generation"]))
add("tenant_agent_configs", **T, ai_agent_version_id=AG["ANALYSIS"], vertical_id=V["SCM"], ai_model_id=model, is_enabled=1, monthly_token_limit=2000000)
add("tenant_model_permissions", **T, ai_model_id=model, is_allowed=1, max_tokens_per_run=50000, data_handling_policy="no_training")
add("ai_escalation_rules", **T, rule_code="LOW_CONFIDENCE", condition_text="Any finding confidence < 0.6 or schema validation failed", action_code="escalate_to_human", is_enabled=1)

# workflow engine: standard case lifecycle
wfd = add("wf_definitions", vertical_id=V["SCM"], workflow_code="standard_case_lifecycle", name="Standard case lifecycle", status="active")
wfv = add("wf_versions", wf_definition_id=wfd, version_label="1.0", status="published")
ST = {}
stages = [("draft", "Draft", "human"), ("submitted", "Submitted", "system"), ("intake_review", "Intake review", "human"),
          ("information_required", "Information required", "human"), ("processing", "Processing", "system"), ("ai_analysis", "AI analysis", "ai"),
          ("expert_review", "Expert review", "human"), ("rework", "Rework", "human"), ("quality_assurance", "Quality assurance", "human"),
          ("final_approval", "Final approval", "approval"), ("delivered", "Delivered", "human"), ("closed", "Closed", "system"), ("cancelled", "Cancelled", "system")]
for i, (code, name, typ) in enumerate(stages):
    ST[code] = add("wf_stages", wf_version_id=wfv, stage_code=code, name=name, stage_type=typ, sort_order=i + 1,
                   is_terminal=1 if code in ("closed", "cancelled") else 0, sla_hours={"intake_review": 8, "ai_analysis": 2, "expert_review": 24, "quality_assurance": 8}.get(code))
for a, b, actor in [("draft", "submitted", "human"), ("submitted", "intake_review", "system"), ("intake_review", "information_required", "human"),
                    ("information_required", "intake_review", "human"), ("intake_review", "processing", "human"), ("processing", "ai_analysis", "system"),
                    ("ai_analysis", "expert_review", "ai"), ("expert_review", "rework", "human"), ("rework", "ai_analysis", "human"),
                    ("expert_review", "quality_assurance", "human"), ("quality_assurance", "rework", "human"), ("quality_assurance", "final_approval", "human"),
                    ("final_approval", "delivered", "human"), ("delivered", "closed", "human"), ("draft", "cancelled", "human"), ("submitted", "cancelled", "human")]:
    perm = {"delivered": "approvals.approve", "final_approval": "quality.review", "closed": "cases.close"}.get(b, "cases.update")
    add("wf_transitions", wf_version_id=wfv, from_stage_id=ST[a], to_stage_id=ST[b], transition_code=f"{a}_to_{b}", allowed_actor=actor, required_permission=perm)
add("wf_actions", wf_stage_id=ST["ai_analysis"], run_on="enter", action_type="start_ai_workflow", action_config_json=json.dumps({"workflow": "supplier_quote_analysis_v1"}))
add("wf_actions", wf_stage_id=ST["information_required"], run_on="enter", action_type="send_notification", action_config_json=json.dumps({"template": "MISSING_DOCUMENTS"}))
add("wf_assignments", wf_stage_id=ST["expert_review"], assignment_strategy="skill_match", skill_code="procurement")
add("wf_sla_rules", wf_version_id=wfv, wf_stage_id=ST["expert_review"], sla_hours=24, warning_hours=18, escalate_to_role_code="operations_manager")

# report template + services (all four SCM services)
tpl = add("report_templates", vertical_id=V["SCM"], template_code="SQA_REPORT_V1", name="Supplier comparison report", output_format="pdf", version_label="1.0", status="active",
          sections_json=json.dumps(["Executive summary", "Customer objective", "Documents reviewed", "Supplier comparison", "Price comparison", "Commercial terms",
                                    "Delivery comparison", "Risk assessment", "Recommended next steps", "Expert review and limitations"]))
cat = add("service_categories", **T, vertical_id=V["SCM"], name="Procurement & sourcing")
SV = {}
for code, name, price, ptype, days, status in [("SQA", "Supplier Quote Analysis", 149, "fixed", 3, "active"), ("PSA", "Procurement Spend Analysis", 299, "volume", 5, "draft"),
                                               ("SPA", "Supplier Performance Assessment", 249, "fixed", 5, "draft"), ("IOA", "Inventory Optimization Assessment", 399, "complexity", 7, "draft")]:
    sid = add("services", **T, vertical_id=V["SCM"], service_category_id=cat, service_code=code, name=name, estimated_turnaround_days=days, requires_human_review=1, requires_qa=1, status=status)
    SV[code] = sid
    add("service_pricing", **T, service_id=sid, pricing_type=ptype, base_price=price, currency="USD")
sqa = SV["SQA"]
svv = add("service_versions", **T, service_id=sqa, version_label="1.0", wf_version_id=wfv, ai_workflow_version_id=aiwfv, status="active")
Q = {}
for i, (fc, q, ft) in enumerate([("objective", "What do you want to achieve with this analysis?", "textarea"), ("products", "Which products or parts are in scope?", "textarea"),
                                 ("annual_volume", "Expected annual volume", "text"), ("target_currency", "Currency for comparison", "text")]):
    Q[fc] = add("service_questions", **T, service_id=sqa, field_code=fc, question_text=q, field_type=ft, is_required=1, sort_order=i + 1)
add("service_document_requirements", **T, service_id=sqa, document_type_id=DT["supplier_quotation"], is_required=1, min_count=2)
add("service_deliverable_templates", **T, service_id=sqa, report_template_id=tpl, is_default=1)
chk = add("quality_checklists", **T, service_id=sqa, vertical_id=V["SCM"], name="Supplier Quote Analysis QA", version_label="1.0")
QI = []
for i, (code, text, cat_) in enumerate([("all_quotes", "All quotations have been processed", "completeness"), ("specs", "Product specifications are comparable", "missing_evidence"),
                                        ("currency", "Currency differences are normalized", "calculation_error"), ("freight_tax", "Taxes and freight are considered", "completeness"),
                                        ("moq", "Minimum order quantities are checked", "completeness"), ("lead_time", "Delivery times are compared", "completeness"),
                                        ("payment", "Payment terms are reviewed", "completeness"), ("calcs", "Calculations are verified", "calculation_error"),
                                        ("evidence", "Recommendations have supporting evidence", "missing_evidence"), ("approval", "Human expert approval is recorded", "compliance")]):
    QI.append(add("quality_checklist_items", **T, quality_checklist_id=chk, item_code=code, item_text=text, issue_category=cat_, is_required=1, sort_order=i + 1))

# organization, roles, people
add("organizations", **T, legal_name="Northstar Procurement Advisors Inc.", industry="Supply chain consulting", company_size="10-49", country="Canada", timezone="America/Moncton", default_currency="USD", status="active")
add("organization_branding", **T, portal_title="Northstar Client Portal", primary_color="#1d4ed8", secondary_color="#0f766e", email_from_name="Northstar")
R = {}
for code, n in [("ORG_OWNER", "Organization Owner"), ("ORG_ADMIN", "Organization Admin"), ("OPS_MANAGER", "Operations Manager"), ("TEAM_LEADER", "Team Leader"),
                ("EXPERT", "Expert"), ("REVIEWER", "Reviewer"), ("QA_OFFICER", "QA Officer"), ("FINANCE", "Finance Officer"), ("CUSTOMER", "Customer User"), ("AUDITOR", "Read-only Auditor")]:
    R[code] = add("tenant_roles", **T, role_code=code, role_name=n, is_system=1)
cust = add("customers", **T, customer_code="C-0001", name="Northfield Components Ltd", customer_type="business", industry="manufacturing", company_size="50-200", country="Canada", billing_email="ap@northfield.example", status="active")
add("customer_contacts", **T, customer_id=cust, full_name="Dana Reyes", email="dana@northfield.example", job_title="Purchasing Manager", is_primary=1)
add("customer_consents", **T, customer_id=cust, consent_type="ai_processing", granted=1)
add("customer_consents", **T, customer_id=cust, consent_type="model_training", granted=0)
expert = add("tenant_users", **T, first_name="Priya", last_name="Shah", email="priya@northstar.example", job_title="Supply chain expert", user_type="employee", status="active")
qa = add("tenant_users", **T, first_name="Marc", last_name="Leblanc", email="marc@northstar.example", job_title="QA officer", user_type="employee", status="active")
add("tenant_user_roles", **T, tenant_user_id=expert, tenant_role_id=R["EXPERT"])
add("tenant_user_roles", **T, tenant_user_id=qa, tenant_role_id=R["QA_OFFICER"])
add("user_skills", **T, tenant_user_id=expert, skill_code="procurement", proficiency="expert")
team = add("teams", **T, team_name="Sourcing desk", team_lead_id=expert)
add("team_members", **T, team_id=team, tenant_user_id=expert, member_role="leader")
add("notification_templates", **T, template_code="MISSING_DOCUMENTS", channel="email", subject="We need a few more documents", body="Hi {{contact}}, to continue case {{case_number}} please upload: {{items}}", status="active")
add("tax_rates", **T, region="NB, Canada", name="HST", rate_percent=15)
add("data_retention_policies", **T, entity_type="documents", retention_days=730, policy_action="archive")

# demo case
case = add("cases", **T, public_id="5b7c1e0a-3f7e-4c1d-9a63-2f1e8c0d4a11", case_number="EXP-2026-000001", customer_id=cust, vertical_id=V["SCM"], service_id=sqa, service_version_id=svv,
           title="Steel Component A sourcing", customer_objective="Reduce unit cost for Steel Component A without hurting delivery", status="draft", priority="normal", channel="portal",
           owner_user_id=expert, assigned_team_id=team, human_minutes=25, ai_cost=2.85)
for fc, v in [("objective", "Reduce unit cost"), ("products", "Steel Component A"), ("annual_volume", "12000"), ("target_currency", "USD")]:
    add("case_intake_responses", **T, case_id=case, service_question_id=Q[fc], response_value=v)
add("case_assignments", **T, case_id=case, tenant_user_id=expert, assignment_role="expert", is_current=1)
add("case_deadlines", **T, case_id=case, deadline_type="delivery", due_at="2026-11-06T17:00:00", is_breached=0)
add("case_deadlines", **T, case_id=case, deadline_type="customer_response", due_at="2026-11-01T12:00:00", is_breached=0)  # already overdue: the SLA job will escalate it
walk("cases", case, "status", "submitted", "intake_review", "processing", "ai_analysis", "expert_review")  # history, SLA deadlines, audit & events are automatic
wfi = add("wf_instances", **T, case_id=case, wf_version_id=wfv, current_stage_id=ST["expert_review"], status="running")
add("wf_stage_instances", **T, wf_instance_id=wfi, wf_stage_id=ST["expert_review"], assigned_to_id=expert, status="active")
d1 = add("documents", **T, case_id=case, customer_id=cust, document_type_id=DT["supplier_quotation"], title="ABC Industrial quotation", original_filename="abc_quote.pdf",
         storage_key=f"tenant-{ten}/cases/case-{case}/original/abc_quote.pdf", mime_type="application/pdf", classification="confidential", status="validated")
d2 = add("documents", **T, case_id=case, customer_id=cust, document_type_id=DT["supplier_quotation"], title="Bolt & Beam quotation", original_filename="boltbeam_quote.xlsx",
         storage_key=f"tenant-{ten}/cases/case-{case}/original/boltbeam_quote.xlsx", mime_type="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", classification="confidential", status="extracted")
ex1 = add("document_extractions", **T, document_id=d1, document_type_id=DT["supplier_quotation"], schema_version="1.0", confidence=0.95, status="unverified", validated_by_id=expert,
          extracted_json=json.dumps({"supplier": "ABC Industrial", "product": "Steel Component A", "unit_price": 12.50, "minimum_order": 500, "lead_time": "21 days", "payment_terms": "Net 30", "currency": "USD"}))
for fcode, val, conf, pg in [("supplier", "ABC Industrial", 0.99, 1), ("product", "Steel Component A", 0.97, 1), ("unit_price", "$12.50", 0.96, 2), ("minimum_order", "500", 0.95, 2),
                             ("lead_time", "21 days", 0.93, 2), ("payment_terms", "Net 30", 0.94, 3), ("currency", "USD", 0.99, 2)]:
    f = add("document_extracted_fields", **T, document_extraction_id=ex1, field_code=fcode, field_value=val, confidence=conf, is_verified=1)
    add("document_evidence", **T, document_extraction_id=ex1, document_extracted_field_id=f, page_number=pg)
upd("document_extractions", ex1, status="validated")
ex2 = add("document_extractions", **T, document_id=d2, document_type_id=DT["supplier_quotation"], schema_version="1.0", confidence=0.88, status="unverified")
f = add("document_extracted_fields", **T, document_extraction_id=ex2, field_code="unit_price", field_value="11.80", normalized_value="11.80", confidence=0.88, is_verified=0)
add("document_evidence", **T, document_extraction_id=ex2, document_extracted_field_id=f, sheet_name="Quote", cell_reference="D7")
s1 = add("scm_suppliers", **T, name="ABC Industrial", country="USA")
s2 = add("scm_suppliers", **T, name="Bolt & Beam Supply", country="Mexico")
add("scm_quote_lines", **T, case_id=case, document_id=d1, document_extraction_id=ex1, scm_supplier_id=s1, product_name="Steel Component A", unit_price=12.50, currency="USD",
    minimum_order_qty=500, lead_time_days=21, payment_terms="Net 30", freight_included=0, source_reference="p.2 table 1", extraction_confidence=0.95, validation_status="unverified")
walk("scm_quote_lines", 1, "validation_status", "validated")
add("scm_quote_lines", **T, case_id=case, document_id=d2, document_extraction_id=ex2, scm_supplier_id=s2, product_name="Steel Component A", unit_price=11.80, currency="USD",
    minimum_order_qty=1000, lead_time_days=35, payment_terms="50% upfront, balance on delivery", freight_included=0, source_reference="Quote!D7", extraction_confidence=0.88, validation_status="unverified")
wr = add("ai_workflow_runs", **T, case_id=case, ai_workflow_version_id=aiwfv, status="awaiting_human", correlation_ref="corr-0001")
ar = add("ai_agent_runs", **T, ai_workflow_run_id=wr, case_id=case, ai_agent_version_id=AG["ANALYSIS"], ai_model_id=model, status="completed", attempt=1,
         input_refs_json=json.dumps([{"type": "document_extraction", "id": ex1}, {"type": "document_extraction", "id": ex2}]), input_tokens=18400, output_tokens=2100, cost=0.087)
add("ai_agent_outputs", **T, ai_agent_run_id=ar, output_schema_version="1.0", schema_valid=1, confidence=0.9, needs_human_review=1, output_json=json.dumps({"status": "completed", "findings": 3}))
add("ai_usage_logs", **T, case_id=case, ai_agent_run_id=ar, ai_model_id=model, input_tokens=18400, output_tokens=2100, cost=0.087)
fi1 = add("ai_findings", **T, case_id=case, ai_agent_run_id=ar, finding_code="PRICE_VARIANCE", finding_type="price_variance", severity="medium", title="Price difference",
          ai_description="Supplier A quotes $12.50/unit, Supplier B $11.80/unit: $0.70 potential unit difference, subject to equivalent specifications and commercial terms.",
          evidence_json=json.dumps([{"documentId": d1, "page": 2, "field": "unit_price"}, {"documentId": d2, "sheet": "Quote", "cell": "D7"}]), fact_basis="verified", confidence=0.9, review_status="pending")
fi2 = add("ai_findings", **T, case_id=case, ai_agent_run_id=ar, finding_type="lead_time", severity="medium", title="Delivery time", ai_description="Supplier A: 21 days. Supplier B: 35 days.", fact_basis="verified", confidence=0.92, review_status="pending")
upd("ai_findings", fi2, review_status="accepted")
fi3 = add("ai_findings", **T, case_id=case, ai_agent_run_id=ar, finding_type="payment_terms", severity="high", title="Payment terms",
          ai_description="Supplier A: Net 30. Supplier B: 50% upfront, balance on delivery.", fact_basis="verified", confidence=0.94, review_status="pending")
upd("ai_findings", fi3, review_status="edited", expert_description="Supplier B's 50% prepayment ties up ~$70.8k of working capital on a 12,000-unit order.")
rec = add("ai_recommendations", **T, case_id=case, ai_finding_id=fi1, recommendation_code="REQUEST_REVISED_QUOTE",
          ai_recommendation="Investigate Supplier A's price and request a revised quotation. Supplier B's lower price may be offset by longer delivery and less favorable cash-flow terms.",
          rationale="Price gap vs lead-time and cash-flow terms", estimated_value=8400, requires_human_approval=1, review_status="pending")
add("ai_recommendation_sources", **T, ai_recommendation_id=rec, document_id=d1, citation_note="ABC quote p.2")
rev = add("expert_reviews", **T, case_id=case, reviewer_id=expert, review_round=1, status="in_progress")
add("expert_review_decisions", **T, expert_review_id=rev, ai_finding_id=fi2, decision="accept")
add("expert_review_decisions", **T, expert_review_id=rev, ai_finding_id=fi3, decision="edit", comments="Quantified cash-flow impact")
add("ai_feedback", **T, ai_agent_run_id=ar, ai_finding_id=fi3, reviewer_id=expert, feedback_type="partially_correct", comments="Missed working-capital impact")
appr = add("approvals", **T, case_id=case, approval_type="expert_findings", requested_by_id=expert, approver_id=qa, status="pending")
add("approval_history", **T, approval_id=appr, action_code="requested", actor_id=expert)
qr = add("quality_reviews", **T, case_id=case, quality_checklist_id=chk, reviewer_id=qa, reviewer_type="human", status="in_progress")
for i, it in enumerate(QI[:3]):
    add("quality_review_items", **T, quality_review_id=qr, quality_checklist_item_id=it, result=["pass", "fail", "pass"][i], issue_category="missing_evidence" if i == 1 else None,
        comments="Bolt & Beam spec sheet missing" if i == 1 else None)
add("case_deliverables", **T, case_id=case, report_template_id=tpl, title="Supplier Quote Analysis - EXP-2026-000001", output_format="pdf", version_number=1, status="draft",
    executive_summary="Two quotations reviewed for Steel Component A. Bolt & Beam is $0.70/unit cheaper but has a 14-day longer lead time and requires 50% prepayment.")
q = add("customer_quotes", **T, case_id=case, customer_id=cust, quote_number="Q-2026-0001", subtotal=149, tax=22.35, total=171.35, currency="USD", status="draft")
walk("customer_quotes", q, "status", "sent", "accepted")
inv = add("customer_invoices", **T, customer_id=cust, case_id=case, customer_quote_id=q, invoice_number="INV-2026-0001", subtotal=149, tax=22.35, total=171.35, currency="USD", status="draft", due_date="2026-11-30")
# invoices are issued by Finance only (adminBypass false in the lifecycle)
roles = {r["role_name"]: r["role_id"] for r in call("GET", "Access/roles", None, TOK)[1]["document"]}
call("POST", "Access/members", {"username": "seedfinance", "password": "Seed!2026finance", "role_id": roles["Finance Officer"], "tenant_id": ten}, TOK)
FIN = call("POST", "Token", {"username": "seedfinance", "password": "Seed!2026finance"})[1]["document"]["accessToken"]
upd("customer_invoices", inv, FIN, status="issued", issued_at="2026-11-03 09:00:00")
add("customer_invoice_items", **T, customer_invoice_id=inv, description="Supplier Quote Analysis - EXP-2026-000001", quantity=1, unit_price=149, amount=149)
add("notifications", **T, tenant_user_id=expert, notification_type="expert_assigned", channel="in_app", title="New case assigned: EXP-2026-000001", status="sent")
add("saas_usage_records", **T, saas_subscription_id=sub, metric_code="ai_credits", quantity=12, is_billable=1)
for code, val in [("cases_received", 1), ("ai_cost_per_case", 2.85), ("human_minutes_per_case", 25), ("human_correction_rate", 0.33)]:
    add("analytics_daily_metrics", **T, metric_date="2026-11-02", metric_code=code, metric_value=val)
print("seeded OK; tenant", ten, "case", case)
