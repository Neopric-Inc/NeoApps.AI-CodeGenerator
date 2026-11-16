"""Builds the NeoApps.AI site pages from one shared header/footer.

Run from the repo root: python3 scripts/build_website.py
Edit the page bodies below, then rebuild; the HTML files are what GitHub Pages serves.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "website"
CLOUD_URL = "https://cloud.neoapps.ai/mcp"
GITHUB = "https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator"

EXTRA_CSS = """
    .dropdown { position: relative; }
    .dropdown > button { background: none; border: 0; font: inherit; color: inherit; cursor: pointer; padding: 6px 0; }
    .dropdown-menu { display: none; position: absolute; top: 100%; left: -14px; min-width: 260px; padding: 8px;
      background: #121317; border: 1px solid #2a2d33; border-radius: 12px; box-shadow: 0 24px 60px rgba(0,0,0,.5); z-index: 40; }
    .dropdown:hover .dropdown-menu, .dropdown:focus-within .dropdown-menu { display: block; }
    .dropdown-menu a { display: block; padding: 8px 12px; border-radius: 8px; color: var(--soft); }
    .dropdown-menu a:hover { background: #1a1c21; color: var(--ink); }
    .nav-links a[aria-current="page"], .dropdown > button[aria-current="page"] { color: var(--ink); }
    .faq details { border-bottom: 1px solid var(--line); padding: 18px 0; }
    .faq summary { cursor: pointer; font-weight: 500; font-size: 1.04rem; }
    .faq p { color: var(--muted); margin: 10px 0 0; }
    @media (max-width: 640px) { .dropdown { display: none; } }
"""

NAV = [
    ("index.html", "Home", None),
    ("use-cases.html", "Use cases", [
        ("use-cases.html#healthcare", "Healthcare and research"),
        ("use-cases.html#legal", "Legal"),
        ("use-cases.html#professional", "Accounting and professional services"),
        ("use-cases.html#field", "Field operations and compliance"),
        ("use-cases.html#education", "Education"),
        ("use-cases.html#insurance", "Insurance"),
        ("use-cases.html#property", "Property and real estate"),
        ("use-cases.html#public", "Nonprofit and government"),
        ("use-cases.html#manufacturing", "Manufacturing and quality"),
        ("use-cases.html#prompt-library", "Prompt library"),
    ]),
    ("pricing.html", "Pricing", None),
    ("developers.html", "Developers", [
        ("developers.html#open-source", "Open source"),
        ("developers.html#local", "Run it locally"),
        ("developers.html#rules", "Data rules"),
        ("developers.html#extend", "Extend it"),
        ("security.html", "Security"),
        (GITHUB, "GitHub ↗"),
    ]),
]


def header(current):
    items = []
    for href, label, sub in NAV:
        cur = ' aria-current="page"' if href == current else ""
        if sub:
            links = "".join(f'<a href="{h}">{l}</a>' for h, l in sub)
            items.append(f'<div class="dropdown"><button type="button"{cur} onclick="location.href=\'{href}\'">{label} ▾</button>'
                         f'<div class="dropdown-menu">{links}</div></div>')
        else:
            items.append(f'<a href="{href}"{cur}>{label}</a>')
    items.append('<a class="button small" href="get-started.html">Connect assistant</a>')
    return f"""  <header class="topbar">
    <nav class="nav wrap" aria-label="Main navigation">
      <a class="brand" href="index.html" aria-label="NeoApps.AI home"><span class="brand-mark">N</span>NeoApps.AI</a>
      <div class="nav-links">{''.join(items)}</div>
    </nav>
  </header>"""


FOOTER = f"""  <footer>
    <div class="wrap footer-row">
      <a class="brand" href="index.html"><span class="brand-mark">N</span>NeoApps.AI</a>
      <span>Custom software, built by your AI, owned by you.</span>
      <div class="footer-links"><a href="get-started.html">Connect your AI assistant</a><a href="pricing.html">Pricing</a><a href="developers.html">Developers</a><a href="security.html">Security</a><a href="{GITHUB}">GitHub</a><a href="mailto:info@neopric.com">Contact</a></div>
    </div>
  </footer>"""


def page(file, title, description, body, scripts=("assets/site.js",)):
    js = "".join(f'\n  <script src="{s}"></script>' for s in scripts)
    html = f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="theme-color" content="#0b0c0e">
  <link rel="icon" href="favicon.ico" sizes="any">
  <link rel="icon" href="favicon.svg" type="image/svg+xml">
  <link rel="apple-touch-icon" href="apple-touch-icon.png">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap">
  <meta name="description" content="{description}">
  <title>{title}</title>
  <link rel="stylesheet" href="assets/site.css">
  <style>{EXTRA_CSS}  </style>
</head>
<body>
{header(file)}
  <main id="top">
{body}
  </main>
{FOOTER}{js}
</body>
</html>
"""
    (ROOT / file).write_text(html)


def prompt_box(text):
    return (f'<div class="prompt-box"><pre class="copy-text">{text}</pre><div class="copy-row"><span class="copy-status" aria-live="polite"></span>'
            f'<button class="button secondary small copy-button" type="button">Copy prompt</button></div></div>')


def section(id_, kicker, heading, intro, inner, white=False):
    cls = "section white" if white else "section"
    idattr = f' id="{id_}"' if id_ else ""
    intro_html = f"<p>{intro}</p>" if intro else ""
    return f"""    <section class="{cls}"{idattr}>
      <div class="wrap">
        <div class="section-heading">
          <div class="kicker">{kicker}</div>
          <h2>{heading}</h2>
          {intro_html}
        </div>
{inner}
      </div>
    </section>
"""


def cards(items, cls="include", grid="includes"):
    return f'        <div class="{grid}">' + "".join(f'<article class="{cls}"><h3>{h}</h3><p>{p}</p></article>' for h, p in items) + "</div>"


INCLUDES = [
    ("Sign-in and roles", "Your team and your customers each get the right access."),
    ("People see only their own", "Customers see their records. Staff see their work."),
    ("Approvals that lock", "Submitted means submitted. Only approvers change it."),
    ("Audit trail", "Who changed what, and when. Every time."),
    ("Automatic numbers", "INV-2026-00001, by the rules you set."),
    ("Dashboard", "The numbers you run the business on."),
    ("AI that drafts", "It reads documents and proposes. People approve."),
    ("Changes keep your data", "Add a field or a step. Nothing is lost."),
]

INDUSTRIES = [
    ("healthcare", "Healthcare and research", [
        ("Patient intake portal", "Clinics, physiotherapy and clinical research intake, with forms, documents and consent."),
        ("Mental health intake and monitoring", "Private practice and campus counselling: intake, check-ins, follow-ups."),
        ("Research data collection", "Study participants, visits, structured data and review status."),
    ]),
    ("legal", "Legal", [
        ("Legal intake and case portal", "Personal injury, medical assessments and legal aid: intake, documents, deadlines."),
        ("Contract review and approvals", "Requests, versions and sign-off, with AI answers about clauses."),
        ("Matter and deadline tracker", "Matters, tasks, court dates and who's on each."),
    ]),
    ("professional", "Accounting and professional services", [
        ("Audit engagements", "Client engagements from planning to sign-off, a client document portal, AI-drafted findings."),
        ("Requests and approvals", "Purchases, time off, IT and expenses. Submit, approve, lock, report."),
        ("R&D tax credit claims (SR&ED)", "Collect project evidence, timesheets and expenses; prepare claims for review."),
        ("Grant applications", "Applications, reviewers, scoring and decisions."),
        ("Personal finance", "Client financial health for advisors and credit unions."),
    ]),
    ("field", "Field operations and compliance", [
        ("Field service", "Dispatch, work orders, technicians, equipment, inventory and invoices."),
        ("Field inspections", "Quality checks, maintenance and construction safety, with photos and sign-off."),
        ("Engineering assessments", "Design reviews, asset inspections and safety audits."),
        ("Compliance audits", "Inspections, evidence, findings and corrective actions."),
        ("Vendor qualification", "Supplier onboarding, documents and periodic reviews."),
    ]),
    ("education", "Education", [
        ("Student intake and support", "Admissions, wellbeing, accessibility and support cases."),
        ("Course and instructor scheduling", "Courses, sections, rooms and instructors."),
        ("Scholarship and bursary applications", "Apply, review, score and award."),
    ]),
    ("insurance", "Insurance", [
        ("Claims intake", "Customer portal, documents, adjuster review and payout approval. Demo available on request."),
        ("Policy endorsements", "Change requests with review and approval."),
        ("Broker submissions", "Quotes, documents and status in one place."),
    ]),
    ("property", "Property and real estate", [
        ("Maintenance requests", "Tenant portal, work orders and vendor assignment."),
        ("Lease tracking", "Units, tenants, renewals and rent changes."),
        ("Unit inspections", "Move-in and move-out checklists with photos."),
    ]),
    ("public", "Nonprofit and government", [
        ("Case management", "Clients, services and follow-ups, with privacy by role."),
        ("Volunteer coordination", "People, shifts and hours."),
        ("Permit applications", "Submit, review, approve and issue a permit number."),
    ]),
    ("manufacturing", "Manufacturing and quality", [
        ("Non-conformance and corrective action", "Defect, root cause, fix and verification."),
        ("Equipment maintenance", "Assets, schedules and work logs."),
        ("Supplier quality", "Audits, scores and corrective actions."),
    ]),
]

REQ_PROMPT = "Build me a Requests app. Employees submit purchase, equipment, travel and time-off requests. Managers approve or reject them with a note. Employees only see their own requests. Keep a history of every step, and notify the manager when something is submitted and the employee when it's decided."

CLOUD_PROMPT = ("Use the NeoApps connector. I want to build [describe your business app]. Ask me about users, data, approvals and "
                "permissions, then show me the preview and wait for my review. After I approve, publish it, check it's live and give me "
                "the link. Never ask me for passwords, card numbers or API keys in this chat.")


# ---------------------------------------------------------------- Illustrations (HTML/CSS mockups, sample data)
CHECK = '<svg viewBox="0 0 16 16" aria-hidden="true"><path d="M3.5 8.5l3 3 6-7" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>'
MOCK_REQUEST = """<div class="mock" role="img" aria-label="Illustration: a request moving from Draft to Submitted to Approved, with who did each step">
  <div class="mock-bar"><span>REQ-2026-00012</span><span class="pill ok">Approved</span></div>
  <div class="mock-body">
    <div class="mock-title">Standing desk for the ops room</div>
    <div class="mock-kv"><span>Requested by</span><b>Emma Clarke</b></div>
    <div class="mock-kv"><span>Amount</span><b>$640.00</b></div>
    <div class="mock-steps"><span class="pill">Draft</span><i></i><span class="pill info">Submitted</span><i></i><span class="pill ok">Approved</span></div>
    <ul class="mock-log"><li><b>Emma</b> submitted<em>9:14 AM</em></li><li><b>Marcus</b> approved: “Order from the usual supplier.”<em>10:02 AM</em></li></ul>
  </div>
</div>"""
MOCK_SIGNOFF = """<div class="mock" role="img" aria-label="Illustration: a partner re-entering their password to sign off an audit engagement">
  <div class="mock-bar"><span>ENG-2026-00001</span><span class="pill info">Report</span></div>
  <div class="mock-body">
    <div class="mock-title">Sign this step</div>
    <div class="mock-quote">I approve this engagement for release and confirm the report is complete.</div>
    <div class="mock-field"><span>Your password</span><b>••••••••••</b></div>
    <div class="mock-actions"><span class="mock-btn ghost">Cancel</span><span class="mock-btn">Sign</span></div>
    <p class="mock-foot">Signer, time and statement go in the audit log.</p>
  </div>
</div>"""
MOCK_INSPECTION = """<div class="mock" role="img" aria-label="Illustration: a site inspection checklist with photos and a supervisor sign-off">
  <div class="mock-bar"><span>INS-2026-00031 · Dock 4</span><span class="pill warn">In review</span></div>
  <div class="mock-body">
    <ul class="mock-check"><li class="done">""" + CHECK + """Guard rails secured</li><li class="done">""" + CHECK + """Fire exits clear</li><li class="fail"><span>!</span>Forklift log missing</li></ul>
    <div class="mock-photos"><span></span><span></span><span></span></div>
    <div class="mock-kv"><span>Corrective action</span><b>Assigned to Ana · due Fri</b></div>
  </div>
</div>"""
MOCK_CONNECT = """<div class="mock wide" role="img" aria-label="Illustration: an assistant's add-connector form with the NeoApps link pasted in">
  <div class="mock-bar"><span>Add custom connector</span><span class="mock-x">×</span></div>
  <div class="mock-body">
    <div class="mock-field"><span>Name</span><b>NeoApps</b></div>
    <div class="mock-field focus"><span>URL</span><b>""" + CLOUD_URL + """</b><em class="mock-callout">Paste this</em></div>
    <div class="mock-actions"><span class="mock-btn ghost">Cancel</span><span class="mock-btn">Add</span></div>
    <p class="mock-foot">Then sign in to NeoApps when asked. Your assistant's screen looks similar.</p>
  </div>
</div>"""
ARCH = """<div class="arch" role="img" aria-label="Diagram: your assistant talks to the NeoApps connector, which builds and publishes your app (API, web app, database). On NeoApps Cloud the builder and app run on our servers; self-hosted, everything runs in your environment.">
  <div class="arch-col"><div class="arch-label">Where you work</div>
    <div class="arch-box"><b>Your assistant</b><span>Claude, ChatGPT, Codex or Gemini</span></div></div>
  <div class="arch-arrow" aria-hidden="true"><span>MCP</span></div>
  <div class="arch-zone"><div class="arch-label">NeoApps Cloud, or your environment when self-hosted</div>
    <div class="arch-row">
      <div class="arch-box"><b>NeoApps connector</b><span>Plan, preview, publish</span></div>
      <div class="arch-arrow" aria-hidden="true"><span>builds</span></div>
      <div class="arch-box"><b>Generator</b><span>Schema + UI config → code</span></div>
      <div class="arch-arrow" aria-hidden="true"><span>deploys</span></div>
      <div class="arch-stack"><div class="arch-box accent"><b>Your app</b><span>.NET 10 API · React UI</span></div>
        <div class="arch-box"><b>Data</b><span>MySQL · Redis · RabbitMQ</span></div></div>
    </div></div>
</div>"""

COMPARE_TABLE = '''        <div class="compare-wrap"><table class="compare">
          <thead><tr><th scope="col"><span class="sr-only">Question</span></th><th scope="col">Our cloud</th><th scope="col" class="featured">Your cloud</th><th scope="col">Your machine</th></tr></thead>
          <tbody>
            <tr><th scope="row">Who runs it</th><td>NeoApps, on Microsoft Azure in the US</td><td class="featured">Your Azure, AWS or Google Cloud account. We set it up and hand it over.</td><td>You, on your own servers</td></tr>
            <tr><th scope="row">Updates</th><td>We keep it running and up to date</td><td class="featured">Your team, with our handover</td><td>You</td></tr>
            <tr><th scope="row">Your app's code</th><td>Yes, anytime. Ask your assistant for your app's plan and build it with the open-source generator.</td><td class="featured">Yours</td><td>Yours</td></tr>
            <tr><th scope="row">Your records</th><td>Export any list to CSV</td><td class="featured">In your own database</td><td>In your own database</td></tr>
            <tr><th scope="row">Price</th><td>14 days free, then from US$49/month</td><td class="featured">One-time, from US$10,000</td><td>Free, open source</td></tr>
            <tr><th scope="row"></th><td><a class="text-link" href="get-started.html">Connect your AI assistant</a></td><td class="featured"><a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20deploy%20in%20our%20cloud">Talk to us</a></td><td><a class="text-link" href="developers.html">Developer setup</a></td></tr>
          </tbody>
        </table></div>'''

# ---------------------------------------------------------------- Home
home = f"""    <section class="hero">
      <div class="wrap">
        <a class="eyebrow" href="#how-it-works"><span class="dot"></span> Open-Source Execution Engine for Frontier AI</a>
        <h1>Frontier AI thinks. NeoApps compiles it into governed production software.</h1>
        <p class="lede">Connect Claude, ChatGPT, Codex, or Gemini to NeoApps. Turn your product specs, research, and workflows into full-stack apps with server-enforced roles, audit trails, and zero vendor lock-in.</p>
        <div class="hero-actions">
          <a class="button" href="get-started.html">Start Building Free</a>
          <a class="button secondary" href="developers.html#local">Install the NeoApps CLI</a>
        </div>
        <p class="fine">14-day cloud trial • No card needed</p>
      </div>
    </section>
    <section class="works" aria-label="Works with">
      <div class="wrap">
        <p class="label">Works natively with:</p>
        <div class="works-row"><span>Claude</span><span>Claude Desktop (MCP)</span><span>ChatGPT</span><span>OpenAI Codex</span><span>Google Gemini</span></div>
      </div>
    </section>
    <section class="section" id="positioning">
      <div class="wrap">
        <div class="section-heading">
          <h2>AI writes code fast. NeoApps makes sure it holds up in production.</h2>
          <p>Raw AI-generated apps break down when real users, permissions, and auditors show up. NeoApps separates AI reasoning from deterministic execution.</p>
        </div>
        <div class="tiles"><article class="tile"><h3>Raw Vibe Coding (Other AI Builders)</h3><ul class="plain-list"><li><strong>Fragile Security</strong> Permissions hidden in UI buttons that anyone can bypass via API.</li><li><strong>Black-Box Lock-In</strong> Proprietary hosting and tangled generated code you can&#x27;t easily maintain.</li><li><strong>Context Reset</strong> Re-explaining business rules every time you tweak a screen.</li></ul></article><article class="tile"><h3>Governed Compilation (NeoApps.AI)</h3><ul class="plain-list"><li><strong>Server-Enforced Rules</strong> Roles, record locks, and password sign-offs enforced at the database and API layer.</li><li><strong>100% Open-Source Core</strong> Inspect the plan, export clean code, or self-host on your own cloud anytime.</li><li><strong>Deterministic Blueprints</strong> Your AI generates a structured specification; NeoApps compiles it reliably every time.</li></ul></article></div>
      </div>
    </section>
    <section class="section" id="how-it-works">
      <div class="wrap">
        <div class="section-heading">
          <h2>From AI conversation to live deployment in four steps.</h2>
        </div>
        <div class="walk">
          <article class="walk-step"><div class="step-num">01</div><h3>Plan in Your Favorite AI</h3><p>Brainstorm your product, SaaS portal, or internal workflow in Claude, ChatGPT, or Gemini. NeoApps plugs directly into your assistant via MCP and custom actions.</p></article>
          <article class="walk-step"><div class="step-num">02</div><h3>Review the Deterministic Plan</h3><p>Before a single line of infrastructure spins up, inspect a human-readable blueprint of your data models, user roles, state transitions, and notification triggers.</p><div class="shot"><img src="assets/img/req-plan.webp" width="1390" height="380" loading="lazy" alt="Sample app plan with roles and workflow steps"></div></article>
          <article class="walk-step"><div class="step-num">03</div><h3>Compile &amp; Deploy Anywhere</h3><p>NeoApps compiles your plan into a production-ready application with built-in authentication, server-side RBAC, and responsive web/mobile views.</p><div class="shot"><img src="assets/img/req-approve.webp" width="1600" height="1000" loading="lazy" alt="Generated app showing the approval workflow"></div></article>
          <article class="walk-step"><div class="step-num">04</div><h3>Audit &amp; Iterate Safely</h3><p>Every record change, approval, and sign-off is logged automatically. Ask your AI to add new features without breaking existing security rules.</p><div class="shot"><img src="assets/img/req-history.webp" width="1600" height="1000" loading="lazy" alt="Approval history in the generated app"></div></article>
        </div>
      </div>
    </section>
    <section class="section" id="industries">
      <div class="wrap">
        <div class="section-heading">
          <h2>Built for external products and mission-critical operations.</h2>
          <p>Whether you are launching a multi-tenant client portal or replacing high-risk spreadsheets, NeoApps enforces your rules out of the box.</p>
        </div>
        <div class="tiles"><article class="tile"><h3>Customer-Facing &amp; Commercial Apps</h3><ul class="plain-list"><li><strong>Client &amp; Partner Portals</strong> Give customers secure access to their own records, invoices, and project milestones.</li><li><strong>Vertical SaaS MVPs</strong> Build B2B tools with multi-tenant data isolation.</li><li><strong>Service Marketplaces &amp; Intake</strong> Automate onboarding, document collection, and status tracking on desktop and mobile.</li></ul></article><article class="tile"><h3>Regulated &amp; Audited Operations</h3><ul class="plain-list"><li><strong>Healthcare &amp; Research</strong> Patient intake and clinical study coordination deployed inside your own cloud.</li><li><strong>Legal &amp; Accounting</strong> Engagement portals, evidence tracking, and partner sign-offs with password re-entry.</li><li><strong>Field Ops &amp; Manufacturing</strong> Non-conformance reports, corrective actions, and mobile checklists.</li></ul></article></div>
        <p><a class="text-link" href="use-cases.html">Explore use cases and starter prompts →</a></p>
      </div>
    </section>
    <section class="section" id="own">
      <div class="wrap">
        <div class="section-heading">
          <h2>Run it where your data must live. Leave whenever you want.</h2>
          <p>Deploy on our managed US Azure cloud, or run the open-source engine in your own environment.</p>
        </div>
        <div id="rules" class="tiles"><article class="tile"><h3>Security and deployment</h3><ul class="plain-list"><li><strong>Zero-Trust Architecture</strong> Deploy in your own AWS, Azure, or Google Cloud account and apply your organization’s security controls.</li><li><strong>Password-Verified Sign-Offs</strong> Critical actions require password re-entry—admins can&#x27;t skip it, and the AI can&#x27;t forge it.</li><li><strong>Zero Model Training</strong> NeoApps never uses your schemas, business logic, or customer records to train AI models. Your AI provider’s terms apply.</li></ul></article></div>
        <p class="fine"><a class="text-link" href="security.html">Review security controls</a> · <a class="text-link" href="pricing.html#faq">Read the compliance FAQ</a></p>
      </div>
    </section>
    <section class="section" id="plans">
      <div class="wrap">
        <div class="section-heading">
          <h2>Start open-source. Scale with full governance.</h2>
        </div>
        <div class="paths">
          <article class="path-card"><h3>Community / Self-Hosted (Free Forever)</h3><ul class="plain-list"><li>Full open-source generator &amp; CLI</li><li>Run locally or on your own servers</li><li>Unlimited apps and developers</li><li>Community GitHub support</li></ul></article>
          <article class="path-card"><h3>Cloud Starter ($49/month)</h3><ul class="plain-list"><li>Hosted on NeoApps managed US Azure cloud</li><li>14-day free trial, no credit card required</li><li>Built-in auth, audit logs, and CSV exports</li></ul></article>
          <article class="path-card"><h3>Team &amp; Growth ($299/month)</h3><p><strong>Proposed plan · not available today</strong></p><ul class="plain-list"><li>Custom domains &amp; white-label branding</li><li>Staging and production environments</li><li>SSO / SAML integration &amp; priority support</li><li>Higher user seats and API rate limits</li></ul></article>
          <article class="path-card"><h3>Enterprise Private Cloud (From $10,000 one-time setup)</h3><ul class="plain-list"><li>Deployed and configured in your AWS, Azure, or GCP account</li><li>Full source code and infrastructure handover</li></ul></article>
        </div>
        <p><a class="text-link" href="pricing.html">Compare current plans →</a></p>
      </div>
    </section>
""" \
+ section("proof", "Proof", "ClaimKit: years of R&amp;D tax-credit expertise, live in four hours.", None,
    '        <div class="video-grid"><div class="video-frame"><iframe src="https://www.youtube-nocookie.com/embed/MD_jAxglA_Y?rel=0" title="ClaimKit walkthrough" loading="lazy" referrerpolicy="strict-origin-when-cross-origin" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe></div>'
    '<div class="video-copy"><p>Our team built ClaimKit, a full client portal for R&amp;D tax-credit consultants, with a coding agent and NeoApps in about four hours. A typical agency build takes five to six months.</p>'
    '<p>The expert still decides. The app does the intake, documents and evidence.</p></div></div>') \
+ f"""    <section class="cta">
      <div class="wrap">
        <h2>Stop babysitting AI-generated code. Start compiling real software.</h2>
        <p>Connect your AI assistant and ship your first governed app today.</p>
        <div class="hero-actions">
          <a class="button" href="get-started.html">Start Free on Cloud</a>
          <a class="button secondary" href="{GITHUB}">Explore GitHub Repo</a>
        </div>
      </div>
    </section>
"""
page("index.html", 'NeoApps.AI · Governed production software',
     'Connect Claude, ChatGPT, Codex, or Gemini to NeoApps. Turn your product specs, research, and workflows into full-stack apps with server-enforced roles, audit trails, and zero vendor lock-in.', home)

# ---------------------------------------------------------------- Use cases
FEATURED = [
    (MOCK_REQUEST, "Requests and approvals", "Employees ask. Managers decide. Every step is on record.", "#professional"),
    (MOCK_SIGNOFF, "Audit engagements", "Partners sign off with their password. The statement is logged.", "#professional"),
    (MOCK_INSPECTION, "Field inspections", "Checklists, photos, failed items become corrective actions.", "#field"),
]
uc = section("app-types", "Use cases", "Start with the kind of app you need.", "Choose a customer portal or an internal workflow, then try a sample prompt.",
    '''        <div class="tiles">
          <article class="tile"><h3>Customer portals</h3><p>Clients submit documents and check progress. Give each client access to their own records.</p><a class="text-link" href="index.html#proof">See the ClaimKit walkthrough →</a> <a class="text-link" href="get-started.html#portal-prompt">Get the portal prompt →</a></article>
          <article class="tile"><h3>Internal workflows</h3><p>Employees submit requests, managers approve them, and each decision is recorded.</p><a class="text-link" href="index.html#how-it-works">See the Requests walkthrough →</a></article>
        </div>''') + section("featured", "Examples", "Apps teams run every day.", "See how teams handle requests, sign-offs and inspections.",
    '        <div class="featured-grid">' + "".join(
        f'<article class="featured-card">{m}<h3>{t}</h3><p>{d}</p><a class="text-link" href="{h}">See related apps →</a></article>' for m, t, d, h in FEATURED)
    + '</div>\n        <p style="margin:20px 0 0"><a class="text-link" href="#prompt-library">Start from a ready-made prompt →</a></p>')
for i, (anchor, name, items) in enumerate(INDUSTRIES):
    uc += section(anchor, "Industry", name, None, cards(items), white=bool(i % 2))
uc += """    <section class="section white" id="prompt-library">
      <div class="wrap">
        <div class="section-heading">
          <div class="kicker">Prompt library</div>
          <h2>Start from a ready-made prompt.</h2>
          <p>Copy one into Claude, ChatGPT, Codex or Gemini. Your assistant asks questions and shows a preview before building.</p>
        </div>
        <div class="library-tools" role="search">
          <input id="prompt-search" type="search" placeholder="Search apps, industries or workflows…" aria-label="Search prompt library">
          <select id="prompt-category" aria-label="Filter prompt category">
            <option value="all">All categories</option>
            <option value="catalog">NeoApps app catalog</option>
            <option value="industry">Industry scenarios</option>
            <option value="repo">Repository app ideas</option>
            <option value="agent">Advanced agents</option>
            <option value="platform">Platform and SaaS ideas</option>
            <option value="studio">Agent Studio and R&amp;D</option>
            <option value="samples">Repository sample apps</option>
          </select>
        </div>
        <div class="library-meta"><span id="prompt-count" aria-live="polite"></span><span>Copy any prompt; build instructions are included.</span></div>
        <div class="prompt-library-grid" id="prompt-grid"></div>
      </div>
    </section>
"""
page("use-cases.html", "Use cases · NeoApps.AI", "Business apps for healthcare, legal, accounting, field operations and education, with ready-made prompts.", uc,
     scripts=("assets/prompts.js", "assets/site.js"))

# ---------------------------------------------------------------- Get started
def copy_inline(text):
    return (f'<span class="copy-inline"><code class="copy-text">{text}</code>'
            f'<button class="copy-button" type="button" aria-label="Copy {text}">Copy</button><span class="copy-status sr-only" aria-live="polite"></span></span>')


PORTAL_PROMPT = CLOUD_PROMPT.replace("[describe your business app]", "a client portal where clients upload documents and see their own records. Staff review submissions, record approval decisions and notify the client when the status changes")

gs = section("connect", "Get started", "Connect your assistant and preview your first app.", "Connect your assistant, review the app plan, then approve it to publish.",
    f"""        <p class="needs"><strong>You'll need:</strong> a NeoApps account. Confirm your email and you get 14 days free, no card. Pick a plan when you're ready (promo codes go in at checkout). Connecting opens a NeoApps page where you sign up or sign in.</p>
        <div class="connect-visual"><div class="connect-copy"><h3>Choose your assistant</h3><p>Paste the NeoApps link where your assistant adds connectors or apps, then sign in. That's the whole setup.</p></div>""" + MOCK_CONNECT + f"""</div>
        <div class="connect">
          <details name="assistant-setup"><summary>Claude</summary><ol><li>Open the Claude desktop app: Settings → Connectors → Add custom connector.</li><li>Paste {copy_inline(CLOUD_URL)}</li><li>Click Connect and sign in to NeoApps. It also works in the Claude mobile app.</li></ol></details>
          <details name="assistant-setup"><summary>ChatGPT</summary><ol><li>Settings → Apps &amp; Connectors → Advanced settings: turn on Developer mode.</li><li>Choose Create and paste {copy_inline(CLOUD_URL)}</li><li>Sign in to NeoApps when asked.</li></ol></details>
          <details name="assistant-setup"><summary>Codex</summary><ol><li>Run {copy_inline("codex mcp add neoapps --url " + CLOUD_URL)}</li><li>Sign in to NeoApps when asked.</li></ol></details>
          <details name="assistant-setup"><summary>Gemini</summary><ol><li>Open gemini.google.com: Connectors → + Custom. Paste {copy_inline(CLOUD_URL)} and leave Client ID empty.</li><li>Agree, then sign in to NeoApps and click Allow.</li><li>In a chat, type <code>@NeoApps Cloud</code> and describe your app.</li></ol></details>
        </div>
        <div class="gemini-strip">
          <h3>Gemini, start to finish</h3>
          <p>A note-taking app, built and published from a Gemini chat in about 5 minutes.</p>
          <div class="shots-row">
            <figure><div class="shot"><img src="assets/img/gemini-add.webp" width="448" height="380" loading="lazy" alt="Gemini's Connect to an MCP server dialog with the NeoApps link pasted in"></div><figcaption>1. Paste the NeoApps link.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/gemini-allow.webp" width="342" height="227" loading="lazy" alt="The NeoApps page asking to allow Google to build apps"></div><figcaption>2. Sign in to NeoApps and allow.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/gemini-chat-plan.webp" width="624" height="522" loading="lazy" alt="Gemini chat asking to let it save the QuickNotes app plan to NeoApps Cloud"></div><figcaption>3. Ask with @NeoApps Cloud.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/quicknotes-list.webp" width="800" height="370" loading="lazy" alt="The live QuickNotes app showing a notes list"></div><figcaption>4. Your app is live.</figcaption></figure>
          </div>
        </div>
""", white=True) \
+ section("prompt", "Then", "Paste this and describe your business.", None, f'        <div style="max-width:860px">{prompt_box(CLOUD_PROMPT)}</div>') \
+ section("portal-prompt", "Starter prompt", "Try a client portal.", "Copy this prompt into your connected assistant to preview a portal with client and staff roles.", f'        <div style="max-width:860px">{prompt_box(PORTAL_PROMPT)}</div>') \
+ section("after", "After it's live", "Invite people. Add AI. Keep improving.", None,
    '        <div class="steps">'
    '<article class="step"><div class="step-num">01</div><h3>Invite people</h3><p>Add your team and customers in Members, with the role they need.</p></article>'
    '<article class="step"><div class="step-num">02</div><h3>Turn on AI</h3><p>Ask your assistant for the AI settings link. Paste your own key there, never in a chat.</p></article>'
    '<article class="step"><div class="step-num">03</div><h3>Change it</h3><p>Tell your assistant what to change. Review, publish. Your data stays.</p></article>'
    '<article class="step"><div class="step-num">04</div><h3>Forgot your login?</h3><p>Go to <a class="text-link" href="https://cloud.neoapps.ai/recover">cloud.neoapps.ai/recover</a>.</p></article></div>', white=True) \
+ section(None, "Prefer to run it yourself?", "Free and open source.", None,
    '        <p><a class="button secondary" href="developers.html">Developer setup →</a></p>')
page("get-started.html", "Get started · NeoApps.AI", "Connect NeoApps to Claude, ChatGPT, Codex or Gemini and build your first business app.", gs)

# ---------------------------------------------------------------- Pricing
pr = section("plans", "Pricing", "Choose where your app runs.", None,
    """        <div class="paths">
          <article class="path-card"><div class="path-label">Private cloud</div><h3>Deploy in your cloud</h3><div class="price">From US$10,000</div><p>One-time setup in your AWS, Azure or Google Cloud account, with handover to your team.</p><a class="button secondary" href="mailto:info@neopric.com?subject=NeoApps%20private%20cloud">Discuss private cloud setup</a></article>
          <article class="path-card"><div class="path-label">Open source</div><h3>Run it yourself</h3><div class="price">Free</div><p>Generate and host apps on your own machines. You manage hosting and updates.</p><a class="button secondary" href="developers.html">Developer setup</a></article>
          <article class="path-card featured"><div class="path-label">NeoApps Cloud</div><h3>One app</h3><div class="price">US$49<small>/month</small></div><p>Hosting, database and sign-in included. Built and published from your assistant. Try it free for 14 days, no card.</p><a class="button" href="get-started.html">Connect your AI assistant</a></article>
          <article class="path-card"><div class="path-label">NeoApps Cloud</div><h3>Up to three apps</h3><div class="price">US$149<small>/month</small></div><p>Everything in One app, for up to three apps.</p><a class="button secondary" href="get-started.html">Connect your AI assistant</a></article>
        </div>
        <p style="margin:18px 0 0;color:var(--muted)">AI inside your apps runs on your own Anthropic or OpenAI-compatible key, billed by them. Your Claude, ChatGPT, Codex or Gemini plan is separate. Data is hosted in the US.</p>
        <p style="margin:8px 0 0;color:var(--muted)">Need it built for you? <a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20custom%20build">Talk to us</a> about custom apps, hosting and support.</p>""", white=True) \
+ section("compare", "Where it runs", "Who runs it, who updates it, what you keep.", None, COMPARE_TABLE) \
+ section("adopt", "How teams adopt", "Start small. Grow when it sticks.", None,
    '        <ol style="max-width:820px;margin:0;padding-left:20px;line-height:1.9">'
    '<li><b>One team, one workflow.</b> Replace a spreadsheet in a day.</li>'
    '<li><b>Invite the team.</b> Set who sees and changes what.</li>'
    '<li><b>Add the next workflow</b> once the first one sticks.</li></ol>') \
+ section("faq", "FAQ", "Questions people ask.", None, '        <div class="faq" style="max-width:820px">' + "".join(
    f"<details><summary>{q}</summary><p>{a}</p></details>" for q, a in [
        ("Is there a free trial?", "Yes. 14 days, no card. Confirm your email, then build and publish one app. Pick a plan to keep it running; if you don't, it pauses and is kept for 30 days."),
        ("Do I need to code?", "No. You describe your business in Claude, ChatGPT, Codex or Gemini. You review a preview and approve it."),
        ("Is it a prototype or a real app?", "A real app: sign-in, roles, data rules enforced on the server, and an audit trail. Teams run their business on it."),
        ("How fast is it live?", "Usually the same day. Describe it, approve the preview, publish."),
        ("Do I lose data when I change the app?", "No. Ask your assistant for the change and your data stays. If a change would delete something, you're asked first."),
        ("Who sees what?", "You decide by role. Customers can see only their own records. The server enforces it, not just the screen."),
        ("Where is my data?", "Microsoft Azure, United States. Encrypted in transit (HTTPS) and at rest."),
        ("Is it secure enough for regulated work?", "Role-based access enforced on the server, an audit trail, encryption and US hosting are in place today. SOC 2 and HIPAA are not certified yet. Need them now? Run it in your own cloud (below)."),
        ("Does AI train on my data?", "No. AI is off until you turn it on. Then it runs on your own AI provider and key, under your provider's terms. We never train on your data."),
        ("What does AI cost?", "Nothing from us. You pay your AI provider for what you use."),
        ("Who owns my data?", "You do. Want everything in your own hands? Build and run the app on your own machine. It's open source: the code, data and servers are all yours."),
        ("Can you deploy it in our cloud?", "Yes. We set it up in your own Azure, AWS or Google Cloud account, inside the security and compliance controls you already have, and hand it over. One-time service, starting at US$10,000. <a class=\"text-link\" href=\"mailto:info@neopric.com?subject=NeoApps%20deploy%20in%20our%20cloud\">Contact us</a>"),
        ("Can I bring my spreadsheet?", "Yes. Give it to your assistant and it loads the rows when it builds your app."),
        ("Does it work on phones?", "Yes. Open the app's link and tap Add to Home Screen. On desktop, click Install."),
        ("Can I use my own domain?", "Not yet. Your app gets a neoapps.ai address for now."),
        ("What can't it do?", "It's for business records and workflows. It isn't for games, design tools or heavy number-crunching."),
        ("What if I cancel?", "Your apps stay available for 30 days so you can take your data with you. Then they're closed."),
    ]) + "</div>")
page("pricing.html", "Pricing · NeoApps.AI", "NeoApps Cloud: 14 days free, no card. Then US$49/month for one app, US$149/month for up to three. Free and open source to self-host.", pr)

# ---------------------------------------------------------------- Developers
dev = section("open-source", "Developers", "Review the app plan. Generate source you can run.",
    "NeoApps turns a MySQL schema and a UI config into a full app. Start with AuditDesk to see how roles and workflows are defined before you generate the app.",
    f'<p><a class="button secondary" href="#local">Try the AuditDesk example</a> <a class="text-link" href="{GITHUB}/tree/main/samples/auditdesk">Browse the sample files ↗</a></p>' + cards([
        (".NET 10 API", "Generated controllers, repositories, JWT auth and Swagger."),
        ("React admin UI", "React 19 and MUI, driven by schema.json and ui.config.json."),
        ("MySQL, Redis, RabbitMQ", "Background jobs, approvals and events, out of the box."),
        ("MCP server and CLI", "Every operation as a tool for Claude, ChatGPT, Codex, Gemini and others."),
    ]), white=True) \
+ section("how", "How it fits together", "Your assistant talks. NeoApps builds. Your app runs.", "The same pieces run on NeoApps Cloud or in your own environment.", "        " + ARCH) \
+ section("local", "Run it locally", "One command to install.", "Requires Git, the .NET 10 SDK and Docker. The installer registers the MCP server with Claude Code and Claude Desktop and prints the config for Codex and others.",
    f"""        <div class="prompt-box" style="max-width:860px"><pre class="copy-text">curl -fsSL https://raw.githubusercontent.com/Neopric-Inc/NeoApps.AI-CodeGenerator/main/install.sh | bash</pre><div class="copy-row"><span class="copy-status" aria-live="polite"></span><button class="button secondary small copy-button" type="button">Copy</button></div></div>
        <p style="margin:16px 0 0">From a clone of the repository, preview the sample with <code>neoapps preview AuditDesk --schema samples/auditdesk/auditdesk.sql --ui samples/auditdesk/auditdesk.ui.json</code>. Review the plan, then run <code>neoapps new AuditDesk --schema samples/auditdesk/auditdesk.sql --ui samples/auditdesk/auditdesk.ui.json --deploy</code> to generate and run it. <a class="text-link" href="{GITHUB}#coding-agents-mcp">Setup guide ↗</a></p>""") \
+ section("rules", "Data rules", "Business rules are config, not code.", "The API enforces them on every query, for people, AI and integrations alike.",
    cards([
        ("Tenancy and row scopes", "Customer portals: each customer sees only their own rows."),
        ("Lifecycles", "Status steps, who may take them, and per-role edit locks."),
        ("Validations and numbering", "Formats, ranges, date order, totals; INV-2026-00001."),
        ("Audit, outbox, limits", "Change history, domain events and plan limits."),
        ("Migrations keep data", "Schema changes become reviewed FluentMigrator migrations."),
        ("Background jobs", "Long work and approvals on a RabbitMQ worker."),
        ("AI workflows", "Document Q&amp;A, ask-your-data and AI-drafted records with approval."),
        ("Local models", "Any OpenAI-compatible endpoint, e.g. Ollama or vLLM."),
        ("Signatures", "Steps that need the person's password again; signer, time and statement logged."),
        ("Notifications", "Bell and email from rules: who to tell, when a record moves."),
        ("Installable", "Every app installs on phones and desktops."),
        ("CSV import and export", "Any list. Imports are checked against every rule first, then saved all or nothing."),
    ]), white=True) \
+ section("extend", "Extend it", "When config isn't enough, write code.", f'Your code survives every regenerate. Start from a working app in <a class="text-link" href="{GITHUB}/tree/main/samples">samples/ ↗</a>.',
    cards([
        ("Custom screens", "Your own React pages, dashboard widgets and record buttons. Works on NeoApps Cloud too."),
        ("Custom server code", "C# jobs and endpoints, any NuGet package. Self-hosted for now."),
        ("Preview first", "One page with the data model, every screen and who can do what, before anything is built."),
        ("Test the rules", "<code>neoapps test</code> proves each role sees only its own rows."),
    ])
    + '\n        <p style="margin:24px 0 0"><b>Not built in yet:</b> calendar view, bulk entry grid, PDFs, SMS and calendar sync. Calendar, charts and PDFs work today as custom screens, on NeoApps Cloud too; SMS and calendar sync as custom server code, self-hosted.</p>') \
+ section("build-yourself", "Prompt", "Build and host it yourself.", None,
    '        <div style="max-width:860px">' + prompt_box("I want to build [describe my business app]. Ask me about users, processes, data and permissions. Check the NeoApps schema guidelines and my environment. Show me a plan and wait for my approval. Then generate, build and run the app locally with the NeoApps tools, keep the source in my project, and show me how to host it myself.") + "</div>"
    + f'\n        <p style="margin:16px 0 0"><a class="button secondary" href="{GITHUB}">View on GitHub ↗</a></p>')
page("developers.html", "Developers · NeoApps.AI", "Open-source generator: MySQL schema and UI config to a .NET 10 API and React admin UI, with an MCP server and CLI.", dev)


# ---------------------------------------------------------------- Security
SEC_ROWS = [
    ("Someone opens another person's records by changing an id",
     "Every query passes one checkpoint (DataGuard) that applies roles, tenants and row rules. A request for a record you may not see is refused."),
    ("Keys and passwords end up in the browser",
     "The web app never holds the sign-in token: it lives in an HttpOnly cookie. AI keys are typed on a NeoApps page and stay on the server. Each app gets its own random signing secret."),
    ("No limits on sign-in, no checks on input",
     "Sign-in is rate-limited per address, with lockout and password rules. Field rules (formats, ranges, totals) are checked on the server, not only in the form."),
    ("SQL injection and cross-site scripting",
     "Values are always parameters; column names must be plain names or the request is refused. The API only answers in JSON and React escapes what it shows."),
    ("Code that calls packages that don't exist",
     "The AI writes a plan, not code. Custom screens can only import a short list of pinned, vetted packages."),
]
SEC_TABLE = '        <div class="compare-wrap"><table class="compare"><thead><tr><th scope="col">Where AI-built apps usually break</th><th scope="col">What NeoApps does</th></tr></thead><tbody>' + "".join(
    f"<tr><th scope=\"row\">{a}</th><td>{b}</td></tr>" for a, b in SEC_ROWS) + "</tbody></table></div>\n"

sec = section(None, "Security", "AI designs it. Tested code builds it. You can check every line.",
    "Your assistant writes a plan: tables, roles and rules. The same open-source generator turns every plan into code, so the parts that usually go wrong are written once and tested.",
    SEC_TABLE) \
+ section("tested", "What we tested", "54 checks on a real app. All pass.",
    "The 16 Nov 2026 test run covered a generated QuickNotes app running in production mode.",
    """        <div class="includes">
          <article class="include"><h3>Access</h3><p>Viewers can't write. Members can't delete, read user or permission tables, or make themselves admin. Signed-out sessions stop working.</p></article>
          <article class="include"><h3>Injection</h3><p>Attacks through sort order, search, filters and ids are refused or treated as plain text. Tables stay intact.</p></article>
          <article class="include"><h3>Sign-in</h3><p>Weak passwords refused. Repeated wrong passwords are blocked. Forged tokens refused. Cookies are HttpOnly and SameSite.</p></article>
          <article class="include"><h3>Packages and code</h3><p>No known-vulnerable NuGet or npm packages. A static scan with the open Semgrep rules, then a manual review of what it flagged.</p></article>
        </div>
        <h3 style="margin:32px 0 8px">What we found and fixed</h3>
        <ul class="plain-list">
          <li>Search could show deleted records.</li>
          <li>The built-in users endpoint showed password hashes to admins.</li>
          <li>A logging library had a known moderate issue. Updated.</li>
          <li>Cloud app pages were missing some browser security headers.</li>
        </ul>
        <p style="color:var(--muted)">See the <a class="text-link" href="https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/scripts/security-probe.py">test script</a> for the checks behind these results.</p>
""", white=True) \
+ section("careful", "Where to be careful", "What we can't check for you.", None,
    """        <ul class="plain-list">
          <li>Code you add yourself (custom screens) is yours to review.</li>
          <li>Roles do what you set. Give people the least access they need.</li>
          <li>AI features run on your own key. Keep it on the AI settings page, never in a chat.</li>
        </ul>
        <p>Found a problem? Email <a class="text-link" href="mailto:info@neopric.com?subject=Security">info@neopric.com</a> with \"Security\" in the subject.</p>
""")
page("security.html", "Security · NeoApps.AI", "How NeoApps closes the holes AI-built apps usually have, and the checks we ran to prove it.", sec)
print("built", [p.name for p in ROOT.glob("*.html")])
