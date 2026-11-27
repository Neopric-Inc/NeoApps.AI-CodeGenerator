"""Builds the NeoApps.AI site pages from one shared header/footer.

Run from the repo root: python3 scripts/build_website.py
Edit the page bodies below, then rebuild; the HTML files are what GitHub Pages serves.
"""
from html import escape
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "website"
CLOUD_URL = "https://cloud.neoapps.ai/mcp"
SIGNUP_URL = "https://cloud.neoapps.ai/signup"
GITHUB = "https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator"

EXTRA_CSS = """
    .dropdown { position: relative; }
    .dropdown > button { background: none; border: 0; font: inherit; color: inherit; cursor: pointer; padding: 6px 0; }
    .dropdown-menu { display: none; position: absolute; top: 100%; left: -14px; min-width: 260px; padding: 8px;
      background: #121317; border: 1px solid #2a2d33; border-radius: 12px; box-shadow: 0 24px 60px rgba(0,0,0,.5); z-index: 40; }
    .dropdown:hover .dropdown-menu, .dropdown:focus-within .dropdown-menu { display: block; }
    .dropdown-menu a { display: block; padding: 8px 12px; border-radius: 8px; color: var(--soft); }
    .dropdown-menu .menu-group { display: block; padding: 10px 12px 4px; color: var(--faint); font-size: .72rem; font-weight: 600; text-transform: uppercase; letter-spacing: .08em; }
    .dropdown-menu .menu-sep { display: block; height: 1px; margin: 6px 8px; background: #2a2d33; }
    .dropdown-menu a:hover { background: #1a1c21; color: var(--ink); }
    .nav-links a[aria-current="page"], .dropdown > button[aria-current="page"] { color: var(--ink); }
    .faq details { border-bottom: 1px solid var(--line); padding: 18px 0; }
    .faq summary { cursor: pointer; font-weight: 500; font-size: 1.04rem; }
    .faq p { color: var(--muted); margin: 10px 0 0; }
    .video-frame video { width: 100%; height: 100%; display: block; object-fit: cover; background: #000; }
    .video-copy ul { color: var(--muted); margin: 12px 0 16px; padding-left: 18px; }
    .video-copy li { margin-bottom: 6px; }
    .hero .trust { justify-content: center; margin-top: 32px; }
    .hero h1, .hero .lede { text-wrap: balance; }
    .bridge { margin: -8px 0 28px; color: var(--soft); font-size: 1.02rem; }
"""

NAV = [
    ("index.html", "Home", None),
    ("use-cases.html", "Use cases", [
        ("#group", "Client and regulated portals"),
        ("use-cases.html#professional", "Accounting and professional services"),
        ("use-cases.html#legal", "Legal"),
        ("use-cases.html#healthcare", "Healthcare and research"),
        ("use-cases.html#insurance", "Insurance"),
        ("#group", "Field and physical operations"),
        ("use-cases.html#field", "Field operations and compliance"),
        ("use-cases.html#manufacturing", "Manufacturing and quality"),
        ("use-cases.html#property", "Property and real estate"),
        ("#group", "Data and live operations"),
        ("use-cases.html#live", "Live boards, maps and 3D floor plans"),
        ("use-cases.html#education", "Education"),
        ("use-cases.html#public", "Nonprofit and government"),
        ("#sep", ""),
        ("use-cases.html#prompt-library", "Prompt library"),
    ]),
    ("pricing.html", "Pricing", None),
    ("docs.html", "Docs", None),
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
    mobile = []
    for href, label, sub in NAV:
        cur = ' aria-current="page"' if href == current else ""
        mobile.append(f'<a href="{href}"{cur}>{label}</a>')
        if sub:
            links = "".join('<span class="menu-group">' + l + '</span>' if h == "#group" else '<span class="menu-sep"></span>' if h == "#sep"
                            else f'<a href="{h}">{l}</a>' for h, l in sub)
            items.append(f'<div class="dropdown"><button type="button"{cur} onclick="location.href=\'{href}\'">{label} ▾</button>'
                         f'<div class="dropdown-menu">{links}</div></div>')
        else:
            items.append(f'<a href="{href}"{cur}>{label}</a>')
    connect_current = ' aria-current="page"' if current == "get-started.html" else ""
    items.append(f'<a class="button small" href="{SIGNUP_URL}">Start free</a>')
    security_current = ' aria-current="page"' if current == "security.html" else ""
    mobile.append(f'<a href="security.html"{security_current}>Security</a>')
    mobile.append(f'<a href="get-started.html"{connect_current}>Setup guide</a>')
    mobile.append(f'<a href="{SIGNUP_URL}">Start free</a>')
    return f"""  <header class="topbar">
    <nav class="nav wrap" aria-label="Main navigation">
      <a class="brand" href="index.html" aria-label="NeoApps.AI home"><span class="brand-mark">N</span>NeoApps.AI</a>
      <div class="nav-links">{''.join(items)}</div>
      <details class="mobile-nav">
        <summary>Menu</summary>
        <div class="mobile-nav-links">{''.join(mobile)}</div>
      </details>
    </nav>
  </header>"""


FOOTER = f"""  <footer>
    <div class="wrap footer-row">
      <a class="brand" href="index.html"><span class="brand-mark">N</span>NeoApps.AI</a>
      <span>Custom software, built by your AI, owned by you.</span>
      <div class="footer-links"><a href="get-started.html">Assistant setup guide</a><a href="pricing.html">Pricing</a><a href="docs.html">Docs</a><a href="developers.html">Developers</a><a href="security.html">Security</a><a href="llms.txt">For agents</a><a href="{GITHUB}">GitHub</a><a href="mailto:info@neopric.com">Contact</a></div>
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


def prompt_box(text, label="Copy prompt"):
    return (f'<div class="prompt-box"><pre class="copy-text">{text}</pre><div class="copy-row"><span class="copy-status" aria-live="polite"></span>'
            f'<button class="button secondary small copy-button" type="button">{label}</button></div></div>')


def section(id_, kicker, heading, intro, inner, white=False, first=False):
    cls = "section white" if white else "section"
    idattr = f' id="{id_}"' if id_ else ""
    kicker_html = f'          <div class="kicker">{kicker}</div>\n' if kicker else ""
    intro_html = f"          <p>{intro}</p>\n" if intro else ""
    tag = "h1" if first else "h2"
    return f"""    <section class="{cls}"{idattr}>
      <div class="wrap">
        <div class="section-heading">
{kicker_html}          <{tag}>{heading}</{tag}>
{intro_html}        </div>
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
    ("Share a form", "Like Google Forms: applications with a resume, requests, sign-ups. Anyone with the link, or members only."),
    ("Invite by email", "People join from the email with no password, and sign in later with a code."),
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
        ("Hiring and internships", "Post a job, share an apply link with resume upload, get an email for every application, let AI score candidates."),
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
        ("Workshop and class bookings", "A public page of upcoming sessions, paid bookings by card, and instructor applications."),
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
MOCK_PORTAL = """<div class="mock" role="img" aria-label="Illustration: a client portal showing the documents requested and the current status">
  <div class="mock-bar"><span>Client portal</span><span class="pill info">In review</span></div>
  <div class="mock-body">
    <div class="mock-title">Document request</div>
    <div class="mock-kv"><span>Engagement letter</span><b>Received</b></div>
    <div class="mock-kv"><span>Supporting documents</span><b>In review</b></div>
    <p class="mock-foot">Your contact will update the status after reviewing your documents.</p>
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
ARCH = """<div class="arch arch-row" role="img" aria-label="Claude, Cursor or Codex drafts a MySQL schema and JSON app configuration. NeoApps validates the plan and generates a .NET 10 API with server-side permissions and audit logging, a MySQL database, and a React web app.">
  <div class="arch-box"><b>LLM / MCP client</b><span>Claude, Cursor, Codex</span></div>
  <div class="arch-arrow" aria-hidden="true"></div>
  <div class="arch-box"><b>Declarative app plan</b><span>MySQL schema + JSON configuration</span></div>
  <div class="arch-arrow" aria-hidden="true"></div>
  <div class="arch-box"><b>NeoApps compiler</b><span>Validate, preview, generate</span></div>
  <div class="arch-arrow" aria-hidden="true"></div>
  <div class="arch-box accent"><b>Generated app</b><span>.NET 10 API · server RBAC · audit log · MySQL · React PWA</span></div>
</div>"""

COMPARE_TABLE = '''        <div class="compare-wrap" role="region" aria-label="Hosting comparison table" tabindex="0"><table class="compare">
          <thead><tr><th scope="col">What to compare</th><th scope="col">NeoApps Cloud</th><th scope="col" class="featured">Private cloud setup</th><th scope="col">Self-hosted</th></tr></thead>
          <tbody>
            <tr><th scope="row">Who runs it</th><td>NeoApps, on Microsoft Azure</td><td class="featured">Your Azure, AWS or Google Cloud account. We set it up and hand it over.</td><td>You, on your own servers</td></tr>
            <tr><th scope="row">Who handles hosting updates</th><td>NeoApps</td><td class="featured">Your team after handover</td><td>Your team</td></tr>
            <tr><th scope="row">Your app's code</th><td>Ask your assistant for the app plan. Use the open-source generator to build the code from it.</td><td class="featured">Your team receives the source code at handover.</td><td>Your team generates and keeps the source code.</td></tr>
            <tr><th scope="row">Your records</th><td>Export any list to CSV</td><td class="featured">In your own database</td><td>In your own database</td></tr>
            <tr><th scope="row">Price</th><td>Free for up to 2 apps, then from US$15/month. 1% of payments if you sell in your app.</td><td class="featured">One-time setup from US$10,000. You pay your cloud provider.</td><td>Free generator. You cover your infrastructure and maintenance.</td></tr>
            <tr><th scope="row">Get started</th><td><a class="text-link" href="https://cloud.neoapps.ai/signup">Start free</a></td><td class="featured"><a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20deploy%20in%20our%20cloud">Talk to us</a></td><td><a class="text-link" href="developers.html#local">Open the local setup guide</a></td></tr>
          </tbody>
        </table></div>'''

# ---------------------------------------------------------------- Live apps (Starfall Arena trailer)
TRAILER = ('<div class="video-frame"><video controls muted playsinline preload="none" poster="assets/img/starfall-poster.jpg" '
           'aria-label="Starfall Arena trailer: ships fly and fight in a 3D asteroid field">'
           '<source src="assets/video/starfall-trailer.mp4" type="video/mp4">'
           '<a href="assets/video/starfall-trailer.mp4">Download the Starfall Arena trailer (MP4)</a></video></div>')

LIVE_IDEA = ("Help me plan a multiplayer space game for my team. Players sign in, pick a ship and colour, and join a match with a "
             "code. Everyone in the match sees each other fly and fire in real time, with sound and a scoreboard.")


LIVE_IDEAS = [
    ("Live dispatch map", "Jobs and technicians on a map; pins move as people update their status.",
     "Build a field service dispatch app with a live map: jobs and technicians as coloured markers on the built-in map, status changes and technician moves shown to every dispatcher instantly, technicians update from their phone."),
    ("Warehouse 3D view", "Racks and machines in 3D, red, amber or green from their records.",
     "Build a warehouse and equipment tracker with a 3D view: racks drawn in 3D, coloured by the worst status of their equipment, orbit and zoom, click a rack to open its records."),
    ("Classroom quiz", "Students answer from their phones and see a live leaderboard.",
     "Build a classroom quiz game: teachers run a quiz with a join code, students answer from their phones with just a nickname, everyone sees a live leaderboard, final scores are saved."),
    ("Team whiteboard", "Sticky notes and arrows with everyone's cursor, saved automatically.",
     "Build a shared whiteboard: sticky notes, text and arrows, every member sees the others' cursors and changes live, boards save automatically."),
    ("Quote builder with PDF", "Totals update as you go; download a branded quote PDF.",
     "Build a quoting app: customers, products and numbered quotes with line items, totals calculated from saved prices, and a download button for a branded PDF quote."),
    ("Live operations dashboard", "Charts that update the moment an order or ticket changes.",
     "Build an operations dashboard for orders and support tickets with charts (orders per day, revenue this week, open tickets by priority) that refresh for everyone within a second when anything changes."),
]

# ---------------------------------------------------------------- Home
FEATURED = [
    ("client-portal", MOCK_PORTAL, "Client portal",
     "Clients submit documents and see the status of their own requests. Staff can review submissions in one place.",
     "Help me plan a client portal. Clients should upload documents and see only their own requests. "
     "Staff should review submissions and update their status.", "index.html#proof", "See the ClaimKit walkthrough"),
    ("approval-tracker", MOCK_REQUEST, "Internal approval tracker",
     "Employees submit requests. Managers record decisions and employees can see the outcome.",
     "Help me plan an internal approval tracker. Employees should submit purchase requests and see their own requests. "
     "Managers should approve or reject requests and be able to leave a note.", "#professional", "See related workflows"),
    ("field-inspection", MOCK_INSPECTION, "Field inspection checklist",
     "Inspectors record results and photos on site. Supervisors can assign and follow up on corrective actions.",
     "Help me plan a field inspection app. Inspectors should complete checklists, attach photos, and flag issues. "
     "Supervisors should assign corrective actions and track completion.", "#field", "See related field apps"),
]


home_examples = "".join(
    f"""          <article class="featured-card">
            {mockup}
            <div class="featured-content">
              <h3>{title}</h3>
              <p>{description}</p>
              <a class="text-link" href="use-cases.html#{anchor}">View {title.lower()} example →</a>
            </div>
          </article>"""
    for anchor, mockup, title, description, *_ in FEATURED
)

home = """    <section class="hero">
      <div class="wrap">
        <h1>Build, run, and share web apps as easily as a cloud&nbsp;doc.</h1>
        <p class="lede">Describe your idea to your AI assistant, launch in minutes, and sell directly from your app.</p>
        <div class="hero-actions">
          <a class="button" href="https://cloud.neoapps.ai/signup">Start free</a>
          <a class="button secondary" href="#industries">See example apps</a>
        </div>
        <p class="fine">Free for up to 2 apps. No card needed.</p>
        <ul class="trust" aria-label="What IT teams get">
          <li>Open-source runtime</li><li>Run it in your own cloud</li><li>Access by role and by record</li><li>Audit history, refused attempts included</li><li>Uses the AI plan you already have</li>
        </ul>
      </div>
    </section>
    <section class="works" aria-label="Assistant setup options">
      <div class="wrap">
        <p class="label">Setup guides for</p>
        <div class="works-row"><span>Claude</span><span>ChatGPT</span><span>Codex</span><span>Gemini</span></div>
      </div>
    </section>
""" + section("industries", "", "What you can build", "Start with a workflow your team already knows.",
    f"""        <p class="bridge">Different workflows, same guardrails: role-based access, approval history and your choice of cloud, all from a plan you review before launch.</p>
        <div class="featured-grid">
{home_examples}
        </div>
        <p><a class="text-link" href="use-cases.html#prompt-library">Browse all starter prompts →</a></p>""") \
+ section("showcase", "Built in ChatGPT", "A 3D apartment map, from one conversation", None,
    '        <div class="video-grid"><div class="video-frame"><video controls muted playsinline preload="none" poster="assets/img/apartment-facility-map-poster.jpg" style="object-fit:contain" aria-label="Apartment Facility Map, built in ChatGPT with NeoApps: the prompt, then each floor as a furnished 3D cutaway, an exploded view of the building, and one apartment isolated with its appliances and open tickets"><source src="assets/video/apartment-facility-map.mp4" type="video/mp4"><a href="assets/video/apartment-facility-map.mp4">Download the Apartment Facility Map video (MP4)</a></video></div>'
    '<div class="video-copy"><ul><li>Describe the app in ChatGPT with the NeoApps connector: buildings, units, tickets and who can do what.</li>'
    '<li>Review the plan, publish, and use it: furnished floor plans, an exploded building view, and a side panel for each unit.</li>'
    '<li>The same roles and rules apply on every screen, enforced by the NeoApps runtime.</li></ul>'
    '<p><a class="text-link" href="use-cases.html#live">More live boards, maps and 3D →</a></p></div></div>') + """
    <section class="section" id="how-it-works">
      <div class="wrap">
        <div class="section-heading">
          <h2>From your first idea to a working app</h2>
        </div>
        <div class="walk">
          <article class="walk-step"><div class="step-num">01</div><h3>Describe your workflow</h3><p>Tell your assistant what people need to do and what information the app should keep.</p></article>
          <article class="walk-step"><div class="step-num">02</div><h3>Review the plan before anything runs</h3><p>Every screen, role, field and approval rule is listed in a plan you can read and change. The AI writes the plan, not the security code, and nothing is built until you approve it.</p><div class="shot"><img src="assets/img/req-plan.webp" width="1390" height="380" loading="lazy" alt="Sample app plan with roles and workflow steps"></div></article>
          <article class="walk-step"><div class="step-num">03</div><h3>Publish after approval</h3><p>Ask your assistant to publish the approved plan to NeoApps Cloud. Open the app link when it’s ready. You can also run the app yourself.</p><div class="shot"><img src="assets/img/req-approve.webp" width="1600" height="1000" loading="lazy" alt="Generated app showing the approval workflow"></div></article>
          <article class="walk-step"><div class="step-num">04</div><h3>Improve the app with your team</h3><p>Use the app with your team. Review its activity and ask your assistant to plan changes as your workflow evolves.</p><div class="shot"><img src="assets/img/req-history.webp" width="1600" height="1000" loading="lazy" alt="Approval history in the generated app"></div></article>
        </div>
      </div>
    </section>
    <section class="section" id="positioning">
      <div class="wrap">
        <div class="section-heading">
          <h2>Rules the runtime enforces, not the AI</h2>
          <p>Sign-in, access and approval rules come from the plan you approved. The NeoApps runtime applies them on every request, the same way every time, including requests made through the app's AI assistant.</p>
        </div>
        <div id="rules" class="includes">
          <article class="include"><h3>Access by role and by record</h3><p>Clients see only their own records. Staff see what their role allows. Hidden fields stay hidden in the API, not just on screen.</p></article>
          <article class="include"><h3>Approvals with limits</h3><p>Status steps, approval limits, escalation and separation of duties are checked by the server, so a manager can't approve above their limit.</p></article>
          <article class="include"><h3>Audit history</h3><p>Turn on the audit log to record every change and every refused attempt: who, when, and which rule applied.</p></article>
          <article class="include"><h3>Open source and inspectable</h3><p>The generator and the runtime that enforces these rules are open source on GitHub. Read the code, run the tests, host it yourself.</p></article>
        </div>
        <p><a class="text-link" href="security.html">Review security controls →</a></p>
      </div>
    </section>
""" + section("own", "", "Run it in our cloud or yours", "Use NeoApps Cloud, arrange setup in your own cloud account, or run the open-source generator on your own infrastructure. You keep the plan and the generated code either way.",
    '        <p><a class="text-link" href="pricing.html#compare">Compare hosting options →</a></p>') \
+ section("proof", "Customer portal example", "ClaimKit for R&amp;D tax-credit consultants", None,
    '        <div class="video-grid"><div class="video-frame"><iframe src="https://www.youtube-nocookie.com/embed/MD_jAxglA_Y?rel=0" title="ClaimKit walkthrough" loading="lazy" referrerpolicy="strict-origin-when-cross-origin" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe></div>'
    '<div class="video-copy"><p>Our team built ClaimKit with a coding agent and NeoApps. The portal collects documents and tracks evidence so consultants can review client submissions.</p></div></div>') \
+ section("live-demo", "See the rules at work", "An approval the rules refused, and the audit trail that shows it", None,
    '        <div class="video-grid"><div class="video-frame"><video controls muted playsinline preload="none" poster="assets/img/audit-trail-poster.jpg" aria-label="AuditDesk demo: a manager\'s approval above his limit is refused, he escalates, a partner approves, and the audit log shows every step"><source src="assets/video/audit-trail-demo.mp4" type="video/mp4"><a href="assets/video/audit-trail-demo.mp4">Download the AuditDesk demo (MP4)</a></video></div>'
    '<div class="video-copy"><ul><li>A staff auditor logs a $9,000 finding.</li><li>The manager\'s approval limit is $5,000, so the app refuses his approval. He escalates it instead.</li><li>A partner approves it. The audit log shows each step, including the refused attempt and the rule behind it.</li></ul>'
    '<p>Recorded in AuditDesk, an open-source sample app. Its rules are a few lines of reviewed configuration, not code an AI wrote.</p>'
    f'<p><a class="text-link" href="{GITHUB}/tree/main/samples/auditdesk">See AuditDesk on GitHub →</a></p>'
    '<p><a class="text-link" href="use-cases.html#live">Live boards, maps and 3D floor plans →</a></p></div></div>') \
+ section("plans", "", "Free plan and pricing", "Start free with up to 2 apps. See pricing for plans and app limits.",
    '        <p><a class="text-link" href="pricing.html">See NeoApps pricing →</a></p>') \
+ section("faq", "FAQ", "Before you build your first app", None,
    """        <div class="faq">
          <details><summary>Do I need to know how to code?</summary><p>For the Cloud connector, describe the app in your own words and review the plan in your assistant. The <a class="text-link" href="developers.html#local">local setup</a> is available if you prefer to run the tools yourself.</p></details>
          <details><summary>Which assistants can I use?</summary><p>See the <a class="text-link" href="get-started.html#connect">assistant setup guides</a> for supported connection paths and workspace requirements.</p></details>
          <details><summary>What will I see before I publish?</summary><p>The plan describes what the app tracks, its screens, and what each person can do. Ask your assistant to revise anything that needs changing.</p></details>
          <details><summary>Does the AI decide who can see our data?</summary><p>No. The AI proposes a plan; you review its roles and rules before anything is built. The open-source runtime then enforces those rules on every request. They aren't generated as code that could differ from app to app.</p></details>
          <details><summary>Do we need another AI subscription?</summary><p>No. You build with the assistant your company already uses, such as Claude, ChatGPT, Codex or Gemini. AI features inside an app use your own provider key when you turn them on.</p></details>
          <details><summary>What if I get stuck?</summary><p>See the <a class="text-link" href="docs.html#troubleshooting">troubleshooting guide</a> for help with connecting, signing in, and publishing.</p></details>
        </div>""") + """
    <section class="cta">
      <div class="wrap">
        <h2>Plan your first app</h2>
        <p>Connect your assistant and describe a workflow you want to improve.</p>
        <div class="hero-actions">
          <a class="button" href="https://cloud.neoapps.ai/signup">Start free</a>
          <a class="button secondary" href="docs.html#first-app">Read the first-app guide</a>
        </div>
      </div>
    </section>
"""
page("index.html", "NeoApps.AI · Build, run, and share web apps as easily as a cloud doc",
     "Build, run, and share web apps as easily as a cloud doc. Describe your idea to your AI assistant, launch in minutes, and sell directly from your app. Roles, approvals and audit history are enforced by an open-source runtime.", home)

# ---------------------------------------------------------------- Use cases
def starter_card(anchor, mockup, title, description, idea, href, link_label):
    prompt = "Use the NeoApps connector. " + idea + " Show me the screens and access rules before building."
    return f"""          <article id="{anchor}" class="featured-card">
            {mockup}
            <div class="featured-content">
              <h3>{title}</h3>
              <p>{description}</p>
              <a class="text-link" href="{href}">{link_label} →</a>
              <details>
                <summary>Starter prompt</summary>
                {prompt_box(escape(prompt), f"Copy {title.lower()} prompt")}
              </details>
            </div>
          </article>"""


uc = section("app-types", "Use cases", "Start with the kind of app you need.", "Choose a customer portal or an internal workflow, then try a sample prompt.",
    '''        <div class="tiles">
          <article class="tile"><h3>Customer portals</h3><p>Clients submit documents and check progress. Give each client access to their own records.</p><a class="text-link" href="index.html#proof">See the ClaimKit walkthrough →</a> <a class="text-link" href="get-started.html#portal-prompt">Get the portal prompt →</a></article>
          <article class="tile"><h3>Internal workflows</h3><p>Employees submit requests, managers approve them, and each decision is recorded.</p><a class="text-link" href="index.html#how-it-works">See the Requests walkthrough →</a></article>
          <article class="tile"><h3>Live operations boards</h3><p>Dispatch maps, 3D floor plans and dashboards that update the moment a record changes, with the same access rules as the rest of the app.</p><a class="text-link" href="#live">See live operations ideas →</a></article>
        </div>''', first=True) + section("featured", "Examples", "Three workflows to start with.", "These sample screens show how a NeoApps app can work. Choose an example and copy its prompt to plan your own app.",
    '        <div class="featured-grid">\n' + "\n".join(starter_card(*example) for example in FEATURED)
    + '\n        </div>\n        <p style="margin:20px 0 0"><a class="button" href="' + SIGNUP_URL + '">Start free</a> <a class="text-link" href="#prompt-library">Browse more starter prompts →</a></p>')
for i, (anchor, name, items) in enumerate(INDUSTRIES):
    uc += section(anchor, "Industry", name, None, cards(items), white=bool(i % 2))
FACILITY_PROMPT = ("Build a real-time 3D apartment building facility map. Include multiple buildings, floors, apartment units and shared "
    "zones (lobby, laundry room, boiler room) drawn on an interactive 3D floor plan. Colour each unit by its worst open issue: red for a "
    "safety hold (gas leak, fire hazard), orange for an urgent repair (no hot water, broken A/C), yellow for routine maintenance (leaky "
    "faucet), green for all clear. Clicking a unit opens a side panel with its appliances and open tickets. Colour changes must appear "
    "for everyone within a second. Roles: Tenant (own unit only), Handyman (updates repairs), Safety Inspector (the only role that can "
    "clear a safety hold), Property Manager (full admin). Show me the screens and access rules before building.")
LIVE_OPS = [
    ("Document triage queue", "Several reviewers work one queue. Each file shows who is reviewing it right now, an SLA countdown, and status changes the client portal sees instantly.",
     "Build a document triage queue for client KYC and tax documents: several staff review incoming files at once, each file shows who is reviewing it right now so two people don't pick the same one, an SLA countdown per file, and status changes appear instantly in the client portal. Only reviewers see the queue; clients see only their own files."),
    ("Quality hold escalation board", "A flagged defect alerts quality engineers at once, tracks sign-offs across shifts, and keeps the release locked until every required role approves.",
     "Build a quality hold board for a production floor: an operator flags a defect, quality engineers are alerted instantly, each required role (QA, production lead, plant manager) signs off in order across shifts, and the lot stays on hold until all have approved. Keep a full audit history."),
    ("Facility floor plan", "Click a room, warehouse zone or hospital wing on a 2D or 3D plan to see its equipment status, open maintenance tickets and safety holds.",
     "Build a facility map: buildings with rooms and zones drawn on a 3D floor plan, each coloured by its worst open maintenance ticket or safety hold, click a room to see its equipment and tickets, updates appear for everyone within a second."),
]


def idea_tiles(ideas):
    return "".join(
        '<article class="tile"><h3>' + escape(t) + '</h3><p>' + escape(d) + '</p><details><summary>Starter prompt</summary>'
        + prompt_box(escape("Use the NeoApps connector. " + q + " Show me the screens and access rules before building."), "Copy prompt") + '</details></article>'
        for t, d, q in ideas)


uc += section("live", "Live boards, maps and 3D", "Operations views that update as the work happens.",
    "When a record changes, every screen that shows it updates within a second. Live views use the same sign-in, roles and audit history as the rest of the app.",
    '        <div class="video-grid" style="margin-bottom:36px"><div class="video-frame"><video controls muted playsinline preload="none" poster="assets/img/apartment-facility-map-poster.jpg" style="object-fit:contain" aria-label="Apartment Facility Map, built in ChatGPT with NeoApps: the prompt, then each floor as a furnished 3D cutaway, an exploded view of the building, and one apartment isolated with its appliances and open tickets"><source src="assets/video/apartment-facility-map.mp4" type="video/mp4"><a href="assets/video/apartment-facility-map.mp4">Download the Apartment Facility Map video (MP4)</a></video></div>'
    '<div class="video-copy"><h3>Apartment Facility Map, built in ChatGPT</h3><ul>'
    '<li>Each floor is a furnished 3D cutaway, every unit coloured by its worst open issue.</li>'
    '<li>Switch buildings and floors, or explode the building to see every floor at once.</li>'
    '<li>Click a unit for its appliances and open tickets; isolate it in 3D and orbit around it.</li>'
    '<li>Tenants, handymen, safety inspectors and managers each see and do only what their role allows.</li></ul>'
    '<details><summary>Starter prompt</summary>' + prompt_box(escape("Use the NeoApps connector. " + FACILITY_PROMPT), "Copy prompt") + '</details>'
    f'<p><a class="text-link" href="{GITHUB}/tree/main/samples/facility-map">See a similar open-source sample on GitHub →</a></p></div></div>\n'
    '        <div class="tiles">' + idea_tiles(LIVE_OPS) + '</div>\n'
    '        <h3 style="margin-top:36px">More ideas you can build today</h3><div class="tiles">' + idea_tiles(LIVE_IDEAS) + '</div>\n'
    '        <p class="fine" style="margin-top:20px">The same live engine also runs a 3D multiplayer game: <a class="text-link" href="assets/video/starfall-trailer.mp4">watch the Starfall Arena trailer (MP4)</a>.</p>')
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
            <option value="live">Live, 3D, maps and charts</option>
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

gs = section("connect", "", "Plan your first app with NeoApps", "Connect the assistant you already use, check the app plan, and decide when to publish.",
    f"""        <p class="needs"><strong>You'll need a NeoApps account.</strong> Connecting opens NeoApps Cloud so you can sign up or sign in. See <a class="text-link" href="pricing.html#faq">free plan and plan details</a>.</p>
        <div class="connect-visual"><div class="connect-copy"><h2>Choose your assistant</h2><p>Add NeoApps from the assistant’s settings. If you use a work account, you may need an administrator to enable connectors or custom apps.</p></div>""" + MOCK_CONNECT + f"""</div>
        <div class="connect">
          <details name="assistant-setup"><summary>Claude</summary><ol><li>Open the Claude desktop app: Settings → Connectors → Add custom connector.</li><li>Paste {copy_inline(CLOUD_URL)}</li><li>Click Connect and sign in to NeoApps. It also works in the Claude mobile app.</li></ol></details>
          <details name="assistant-setup"><summary>ChatGPT</summary><ol><li>Settings → Apps &amp; Connectors → Advanced settings: turn on Developer mode.</li><li>Choose Create and paste {copy_inline(CLOUD_URL)}</li><li>Sign in to NeoApps when asked.</li></ol></details>
          <details name="assistant-setup"><summary>Codex</summary><ol><li>Run {copy_inline("codex mcp add neoapps --url " + CLOUD_URL)}</li><li>Sign in to NeoApps when asked.</li></ol></details>
          <details name="assistant-setup"><summary>Gemini</summary><ol><li>Open gemini.google.com: Connectors → + Custom. Paste {copy_inline(CLOUD_URL)} and leave Client ID empty.</li><li>Agree, then sign in to NeoApps and click Allow.</li><li>In a chat, type <code>@NeoApps Cloud</code> and describe your app.</li></ol></details>
        </div>
        <p class="fine">Can’t find the setup option? <a class="text-link" href="docs.html#troubleshooting">Check the connection and sign-in guide</a>.</p>
        <p><a class="text-link" href="#check-connection">Already connected? Check your connection →</a></p>
        <div class="gemini-strip">
          <h2>A first app in Gemini</h2>
          <p>This example shows how the QuickNotes app went from setup to a live link.</p>
          <div class="shots-row">
            <figure><div class="shot"><img src="assets/img/gemini-add.webp" width="448" height="380" loading="lazy" alt="Gemini's Connect to an MCP server dialog with the NeoApps link pasted in"></div><figcaption>1. Paste the NeoApps link.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/gemini-allow.webp" width="342" height="227" loading="lazy" alt="The NeoApps page asking to allow Google to build apps"></div><figcaption>2. Sign in to NeoApps and allow.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/gemini-chat-plan.webp" width="624" height="522" loading="lazy" alt="Gemini chat asking to let it save the QuickNotes app plan to NeoApps Cloud"></div><figcaption>3. Ask with @NeoApps Cloud.</figcaption></figure>
            <figure><div class="shot"><img src="assets/img/quicknotes-list.webp" width="800" height="370" loading="lazy" alt="The live QuickNotes app showing a notes list"></div><figcaption>4. Your app is live.</figcaption></figure>
          </div>
        </div>
""", white=True, first=True) \
+ section("check-connection", "", "Check your connection", "Open a chat with NeoApps enabled and ask your assistant to show your NeoApps apps. An empty list is expected for a new account.",
    '        <p><a class="text-link" href="docs.html#troubleshooting">Get help signing in or connecting →</a></p>') \
+ section("prompt", "", "Describe the app you want", 'Start with a task your team repeats. If you’re still choosing an idea, <a class="text-link" href="use-cases.html#featured">try one of the examples</a>.',
    f'        <div style="max-width:860px">{prompt_box(escape(CLOUD_PROMPT))}</div>') \
+ section("portal-prompt", "Starter prompt", "Try a client portal.", "Copy this prompt into your connected assistant to preview a portal with client and staff roles.", f'        <div style="max-width:860px">{prompt_box(PORTAL_PROMPT)}</div>') \
+ section("review-and-publish", "", "Review, approve, and open your app",
    "Check the proposed screens, records, and access rules. Ask for changes if you need them. When the plan is ready, approve it and ask your assistant to publish and share the link.",
    '        <p>Open the link and follow the sign-in instructions. Try the workflow with sample records before you invite your team.</p>'
    '        <p><a class="text-link" href="docs.html#first-app">Read the first-app guide →</a></p>') \
+ section("after", "After it's live", "Invite people. Add AI. Keep improving.", None,
    '        <div class="steps">'
    '<article class="step"><div class="step-num">01</div><h3>Invite people</h3><p>Members → Invite link: type their emails and pick a role. Each person gets a link, joins with no password, and later signs in with a code sent to their email.</p></article>'
    '<article class="step"><div class="step-num">02</div><h3>Turn on AI</h3><p>Ask your assistant for the AI settings link. Paste your own key there, never in a chat.</p></article>'
    '<article class="step"><div class="step-num">03</div><h3>Share a form</h3><p>On any list, Share form gives you a link people outside the app can fill in, with file uploads like a resume. Close it or replace the link any time.</p></article>'
    '<article class="step"><div class="step-num">04</div><h3>Change it</h3><p>Tell your assistant what to change. Review, publish. Your data stays.</p></article>'
    '<article class="step"><div class="step-num">05</div><h3>Forgot your login?</h3><p>Go to <a class="text-link" href="https://cloud.neoapps.ai/recover">cloud.neoapps.ai/recover</a>.</p></article></div>', white=True) \
+ section(None, "Prefer to run it yourself?", "Free and open source.", None,
    '        <p><a class="button secondary" href="developers.html">Developer setup →</a></p>')
page("get-started.html", "Get started · NeoApps.AI", "Connect NeoApps to Claude, ChatGPT, Codex or Gemini and build your first business app.", gs)

# ---------------------------------------------------------------- Pricing
pr = section("plans", "Pricing", "Choose where your app runs.", "Use NeoApps Cloud for managed hosting, run the open-source generator yourself, or have NeoApps set it up in your own cloud.",
    """        <div class="paths">
          <article class="path-card"><div class="path-label">NeoApps Cloud</div><h3>Free</h3><div class="price">US$0</div><p>Up to 2 apps, each with its own live link, database and sign-in. No card, no end date.</p><a class="button secondary" href="https://cloud.neoapps.ai/signup">Start free</a></article>
          <article class="path-card featured"><div class="path-label">NeoApps Cloud</div><h3>Starter</h3><div class="price">US$15<small>/month</small></div><p>Up to 3 apps. Hosting, database, sign-in and backups included. Built and published from your assistant.</p><a class="button" href="https://cloud.neoapps.ai/signup">Choose Starter</a></article>
          <article class="path-card"><div class="path-label">NeoApps Cloud</div><h3>Business</h3><div class="price">US$99<small>/month</small></div><p>Up to 30 apps, everything in Starter, and help from a person when you need it.</p><a class="button secondary" href="https://cloud.neoapps.ai/signup">Choose Business</a></article>
          <article class="path-card"><div class="path-label">Open source</div><h3>Run it yourself</h3><div class="price">Free</div><p>Generate and host apps on your own machines. You manage hosting and updates.</p><a class="button secondary" href="developers.html">Developer setup</a></article>
          <article class="path-card"><div class="path-label">Private cloud</div><h3>Deploy in your cloud</h3><div class="price">From US$10,000</div><p>One-time setup in your AWS, Azure or Google Cloud account, with handover to your team.</p><a class="button secondary" href="mailto:info@neopric.com?subject=NeoApps%20private%20cloud">Discuss private cloud setup</a></article>
        </div>
        <p style="margin:18px 0 0">Selling something in your app? If your customers pay through your app, we keep 1% of each payment, on top of Stripe's own fee. Selling nothing costs nothing extra.</p>
        <p><a class="text-link" href="#compare">Compare hosting options →</a></p>
        <p style="margin:18px 0 0;color:var(--muted)">Your assistant plan is separate. Optional AI features inside an app use your own Anthropic or OpenAI-compatible key, and your AI provider bills you for that usage.</p>
        <p style="margin:8px 0 0;color:var(--muted)">Need it built for you? <a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20custom%20build">Talk to us</a> about custom apps, hosting and support.</p>""", white=True, first=True) \
+ section("compare", "", "Compare hosting options", "See what NeoApps manages, what your team manages, and how you keep your code and records.", COMPARE_TABLE) \
+ section("adopt", "How teams adopt", "Start small. Grow when it sticks.", None,
    '        <ol style="max-width:820px;margin:0;padding-left:20px;line-height:1.9">'
    '<li><b>One team, one workflow.</b> Replace a spreadsheet in a day.</li>'
    '<li><b>Invite the team.</b> Set who sees and changes what.</li>'
    '<li><b>Add the next workflow</b> once the first one sticks.</li></ol>') \
+ section("faq", "FAQ", "Questions people ask.", None, '        <div class="faq" style="max-width:820px">' + "".join(
    f"<details><summary>{q}</summary><p>{a}</p></details>" for q, a in [
        ("What do I get for free?", "Confirm your email and build up to 2 apps, each with its own live link. No card and no end date."),
        ("What if I need more apps?", "Choose Starter (3 apps, US$15/month) or Business (30 apps, US$99/month). Your apps and data stay as they are."),
        ("Do you take a cut when I sell?", "Only if your customers pay through your app: we keep 1% of each payment, on top of Stripe\'s own fee. Selling nothing costs nothing extra."),
        ("Do I need to code?", "No. You describe your business in Claude, ChatGPT, Codex or Gemini. You review a preview and approve it."),
        ("Is it a prototype or a real app?", "A real app: sign-in, roles, data rules enforced on the server, and an audit trail. Teams run their business on it."),
        ("How do I get a live link?", 'Approve your preview, then ask your assistant to publish it to NeoApps Cloud. Once the app is live, open the link from your assistant. <a class="text-link" href="get-started.html">See the first-app steps</a>.'),
        ("Do I lose data when I change the app?", "No. Ask your assistant for the change and your data stays. If a change would delete something, you're asked first."),
        ("Who sees what?", "You decide by role. Customers can see only their own records. The server enforces it, not just the screen."),
        ("Where is my data?", 'NeoApps Cloud uses Microsoft Azure and encrypts data in transit and at rest. If your organization needs a specific region, <a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20hosting%20region">ask about hosting options</a> before publishing.'),
        ("What controls can my team review?", 'NeoApps offers server-enforced role permissions, an audit trail and encryption. <a class="text-link" href="security.html">Review the security controls</a> alongside your organization’s requirements.'),
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
page("pricing.html", "Pricing · NeoApps.AI", "NeoApps Cloud: free for up to 2 apps, no card. Starter US$15/month for 3 apps, Business US$99/month for 30. If you sell in your app, we keep 1% of each payment. Free and open source to self-host.", pr)

# ---------------------------------------------------------------- Developers
INSTALL_COMMAND = "curl -fsSL https://raw.githubusercontent.com/Neopric-Inc/NeoApps.AI-CodeGenerator/main/install.sh | bash"
SHELL_CONTINUATION = ' \\\n'
AUDITDESK_INPUTS = SHELL_CONTINUATION.join((
    '  --schema "$HOME/.neoapps/samples/auditdesk/auditdesk.sql"',
    '  --ui "$HOME/.neoapps/samples/auditdesk/auditdesk.ui.json"',
))
PREVIEW_COMMAND = "neoapps preview AuditDesk" + SHELL_CONTINUATION + AUDITDESK_INPUTS
BUILD_COMMAND = "neoapps new AuditDesk" + SHELL_CONTINUATION + AUDITDESK_INPUTS + " --deploy"

CLAUDE_DESKTOP_CONFIG = """{
  "mcpServers": {
    "neoapps": {
      "command": "/absolute/path/to/dotnet",
      "args": ["/Users/you/.neoapps/src/NeoApps.Mcp/bin/Release/net10.0/neoapps-mcp.dll"],
      "env": {
        "NEOAPPS_HOME": "/Users/you/.neoapps",
        "DOTNET_ROOT": "/absolute/path/to/dotnet-directory",
        "PATH": "/absolute/path/to/dotnet-directory:/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin"
      }
    }
  }
}"""

dev = f"""    <section class="hero" id="open-source">
      <div class="wrap">
        <h1>Deterministic compilation for probabilistic AI.</h1>
        <p class="lede">Stop debugging hallucinated auth middleware and leaky SQL queries. Let LLMs generate declarative app plans while NeoApps compiles the server-enforced backend, RBAC, and UI.</p>
        <div class="hero-actions">
          <a class="button secondary" href="{GITHUB}#readme">Read the technical docs</a>
          <a class="button secondary" href="{GITHUB}">Star on GitHub</a>
          <a class="button secondary" href="#setup-paths">Choose a setup</a>
        </div>
      </div>
    </section>
""" + section("setup-paths", "Setup", "Choose where to build.",
    "Start with your assistant on NeoApps Cloud or use the open-source tooling on your own machine.",
    f"""        <div class="tiles">
          <article class="tile">
            <h3>Connect to NeoApps Cloud</h3>
            <p>Use the hosted connector in your assistant to plan and publish a managed app.</p>
            <a class="text-link" href="get-started.html#connect">See setup by assistant →</a>
          </article>
          <article class="tile">
            <h3>Run the generator locally</h3>
            <p>Use the CLI directly or let a coding agent call the local MCP server over standard input and output (stdio).</p>
            <a class="text-link" href="#local">Run the local quickstart</a> · <a class="text-link" href="#mcp">Connect a local agent →</a>
          </article>
        </div>
        <p><a class="text-link" href="llms.txt">Read the agent index</a> · <a class="text-link" href="{GITHUB}/blob/main/AGENTS.md">Read repository agent instructions</a> · <a class="text-link" href="{GITHUB}/blob/main/docs/CLOUD.md">Read Cloud integration details</a></p>""") + f"""    <section class="section" id="local">
      <div class="wrap">
        <div class="section-heading">
          <h2>Run your first app locally</h2>
          <p>Requires Git, the .NET 10 SDK, and Docker. AuditDesk uses sample data. <a class="text-link" href="{GITHUB}/tree/main/samples/auditdesk">Browse its schema and configuration</a>.</p>
        </div>
        <div class="includes">
          <article class="include">
            <h3>Install the CLI</h3>
            <p>The installer puts <code>neoapps</code> in <code>~/.local/bin</code>. If needed, add it to your shell’s <code>PATH</code> as directed by the installer.</p>
            {prompt_box(escape(INSTALL_COMMAND), "Copy install command")}
          </article>
          <article class="include">
            <h3>Preview the app plan</h3>
            <p>Open the preview path printed by the CLI to review the schema, screens, roles, and workflow rules.</p>
            {prompt_box(escape(PREVIEW_COMMAND), "Copy preview command")}
          </article>
          <article class="include">
            <h3>Generate and run AuditDesk</h3>
            <p>Once you’ve reviewed the plan, run the app in Docker. The CLI prints the local app URL and sign-in details.</p>
            {prompt_box(escape(BUILD_COMMAND), "Copy build command")}
          </article>
        </div>
      </div>
    </section>
""" + section("how", "Architecture", "How the compiler works",
    "The assistant proposes a plan. The open-source compiler turns the approved schema and configuration into an application.",
    "        " + ARCH + '\n        <div id="rules" class="compiler-rules">' + cards([
        ("Plan-First Execution", "The assistant drafts the schema and configuration. The preview checks the plan and shows the data model, role permissions, and allowed state transitions."),
        ("Zero-Trust API Layer", "The generated API enforces role permissions, tenant and row scopes, and configured lifecycle rules on the server. The UI follows the same rules."),
        ("Append-Only Audit Trail", "When auditing is configured, tracked changes record the actor, time, action, and changed fields. Password-verified sign-offs can record who signed, when, and the statement they signed."),
    ]) + "</div>") \
+ section("mcp", "Local agent integration", "Connect a local MCP client.",
    "Add the local NeoApps MCP server to a supported client, such as Claude Desktop or Cursor. Your coding agent can inspect schemas, preview permission rules, and plan updates to existing apps.",
    f"""        <p>The installer registers NeoApps in Claude Desktop when it’s installed. Restart Claude Desktop after installation. For manual setup, merge the <code>neoapps</code> entry into <code>mcpServers</code> in <code>claude_desktop_config.json</code>. Use the absolute paths printed by the installer in place of the example paths.</p>
        {prompt_box(escape(CLAUDE_DESKTOP_CONFIG), "Copy MCP configuration")}
        <p><a class="text-link" href="{GITHUB}#coding-agents-mcp">Read the setup guide for Cursor and other MCP clients</a> · <a class="text-link" href="get-started.html">Connect to NeoApps Cloud</a></p>""") \
+ section("extend", "Extensibility", "No walled gardens. Extend everything with standard code.",
    "Keep custom React and C# code alongside your app plan so the generator can include it in the next build.",
    cards([
        ("Custom Screens and Integrations", "Extend the React UI with custom pages and actions. On self-hosted deployments, add C# jobs or endpoints and use configured outbox events to drive integrations such as webhooks."),
        ("Direct Database Access", "When you self-host, connect existing BI tools, ETL pipelines, or SQL clients to your MySQL instance with credentials you manage."),
        ("Eject Anytime", "Keep the schema and UI configuration in your repository. Use the open-source generator to rebuild and run the app in your own environment."),
    ]) + f'\n        <p id="build-yourself"><a class="text-link" href="{GITHUB}/tree/main/samples">Explore app samples and extension code</a></p>') \
+ section("faq", "Developer FAQ", "Building, updating, and versioning apps",
    "How to work with a generated application over time.",
    '        <div class="faq" style="max-width:820px">' + "".join(
        f"<details><summary>{question}</summary><p>{answer}</p></details>" for question, answer in [
            ("What stack does NeoApps generate?", "A .NET 10 Web API with role-based permissions and Swagger, a React 19 and Material UI frontend, and a MySQL database. The local Docker setup also provides Redis and RabbitMQ for background jobs."),
            ("How do schema migrations work when my AI updates an app?", f"Run <code>neoapps evolve AuditDesk --plan</code> to compare the proposed schema with the live database and inspect the generated FluentMigrator migration. NeoApps backs up the database before applying changes. Drops require explicit opt-in, and renames need a mapping. <a class='text-link' href='{GITHUB}#changing-a-running-app-migrations'>Read the migration guide</a>."),
            ("Can I version-control my app plans in Git?", "Yes. Commit the SQL schema, UI configuration, and any migrations or custom code to your own repository. Review changes in pull requests and run approved builds and migrations in your CI/CD pipeline. Store deployment credentials in the pipeline’s secret store."),
        ]) + "</div>")
page("developers.html", "Developers · NeoApps.AI",
     "Use NeoApps to preview declarative app plans and generate a .NET 10 API, MySQL database and React UI. Start with the CLI or connect an MCP client.", dev)


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
    ("Shared links and invites that anyone can guess or reuse",
     "Form, record and invite links are signed with the app's own secret and can be closed, limited, given an end date or replaced. Email invites work once, and sign-in codes expire in 10 minutes after 5 tries."),
    ("Code that calls packages that don't exist",
     "The AI writes a plan, not code. Custom screens can only import a short list of pinned, vetted packages."),
]
SEC_TABLE = '        <div class="compare-wrap"><table class="compare"><thead><tr><th scope="col">Where AI-built apps usually break</th><th scope="col">What NeoApps does</th></tr></thead><tbody>' + "".join(
    f"<tr><th scope=\"row\">{a}</th><td>{b}</td></tr>" for a, b in SEC_ROWS) + "</tbody></table></div>\n"

sec = section(None, "Security", "How NeoApps protects access to your apps.",
    "Your assistant writes a plan: tables, roles and rules. The same open-source generator turns every plan into code, so the parts that usually go wrong are written once and tested.",
    SEC_TABLE, first=True) \
+ section("access", "Access and permissions", "Who can see and change your records?",
    "Review access in the app plan before publishing and assign each person the right role when you invite them.",
    cards([
        ("Roles inside your app", "Roles decide who can view, add, change, or delete records. For example, a client portal can give each client access to their own requests and let staff review submissions."),
        ("Access from your assistant", "When you connect an assistant, review the permissions shown on the NeoApps Cloud sign-in screen. Those permissions cover what the assistant can do in your Cloud account. Each published app has its own roles for its users."),
    ]) + '\n        <p><a class="text-link" href="get-started.html#connect">See how assistant setup works →</a></p>') \
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

# ---------------------------------------------------------------- Docs
first_app_guide = """
    <section class="hero">
      <div class="wrap">
        <h1>Build your first app with NeoApps</h1>
        <p class="lede">Start with a workflow you know. This guide takes you from describing it in your assistant to opening the app your team can use.</p>
        <div class="hero-actions">
          <a class="button" href="#first-app">Follow the guide</a>
          <a class="button secondary" href="#troubleshooting">Find help</a>
        </div>
      </div>
    </section>
""" + section("first-app", "First app", "From idea to app link",
    "Try the client portal with sample records. Once you’ve checked who can see and change each record, you can start working with real documents.",
    cards([
        ("Choose an example", 'Start with a <a class="text-link" href="get-started.html#portal-prompt">client portal</a> or <a class="text-link" href="use-cases.html#featured">browse examples</a>. Write down what people need to submit, who reviews it, and what they need to see.'),
        ("Connect your assistant", 'Choose your assistant on <a class="text-link" href="get-started.html#connect">Get Started</a> and follow its connection steps. In a new chat, ask your assistant to use NeoApps to show your apps. An empty list is expected when you’re new.'),
        ("Review the plan", "Paste the starter prompt into the connected assistant. Check the proposed screens, the information they collect, and what clients and staff can do. Ask for changes and review the revised plan before approving it."),
        ("Publish and open the app", "Ask your assistant to publish the approved plan and share the app link when it is ready. Open the link, follow the sign-in instructions, and test a submission and review with sample data."),
    ]), white=True) \
+ section("troubleshooting", "Troubleshooting", "What to do when you get stuck", None,
    """        <div class="faq">
          <details><summary>I can’t find the connector settings</summary><p>Check the steps for your assistant. A workspace administrator may need to enable connectors or give you permission to add one. If you’re working in a local coding environment, see <a class="text-link" href="developers.html#local">the local quickstart</a>.</p></details>
          <details><summary>NeoApps asks me to sign in again</summary><p>Reconnect NeoApps through your assistant’s settings and use the account that owns your app. Then reopen the conversation and ask the assistant to check for the existing app or plan.</p></details>
          <details><summary>The preview needs changes</summary><p>Tell the assistant which screen, field, or access rule should change. Ask it to show the revised plan before you approve publishing.</p></details>
          <details><summary>Publishing failed or I don’t have a link yet</summary><p>Ask your assistant to check the status of the same publish attempt. If it failed, ask what needs to change. After you address the reported issue, ask the assistant to retry the existing app.</p></details>
        </div>""") \
+ section("agent-docs", "For agents", "Give your coding agent the setup guide",
    'The <a class="text-link" href="llms.txt">agent index</a> links to the Cloud connector workflow, repository instructions, and a local sample.',
    "        " + prompt_box(
        "Use the NeoApps guide at https://neoapps.ai/llms.txt to help me plan a business app. Ask who will use it and what they need to do. Show me the proposed screens and access rules before I decide whether to publish."
    ))
page("docs.html", "First app guide · NeoApps.AI",
     "Choose an example, connect an assistant, review your plan, and publish your first NeoApps application.", first_app_guide)

print("built", [p.name for p in ROOT.glob("*.html")])
