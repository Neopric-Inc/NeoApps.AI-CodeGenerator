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
    .dropdown-menu { display: none; position: absolute; top: 100%; left: -14px; min-width: 250px; padding: 8px;
      background: var(--paper); border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 12px 30px rgba(23,33,43,.10); z-index: 20; }
    .dropdown:hover .dropdown-menu, .dropdown:focus-within .dropdown-menu { display: block; }
    .dropdown-menu a { display: block; padding: 8px 10px; border-radius: 8px; color: var(--ink); }
    .dropdown-menu a:hover { background: var(--wash); }
    .nav-links a[aria-current="page"], .dropdown > button[aria-current="page"] { color: var(--ink); font-weight: 650; }
    .includes { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; }
    .include { background: var(--paper); border: 1px solid var(--line); border-radius: 14px; padding: 18px; }
    .include h3 { margin: 0 0 6px; font-size: 1rem; }
    .include p { margin: 0; color: var(--muted); font-size: .9rem; }
    .industry { margin: 0 0 34px; }
    .industry h2 { font-size: 1.5rem; margin: 0 0 14px; }
    .faq details { border-bottom: 1px solid var(--line); padding: 14px 0; }
    .faq summary { cursor: pointer; font-weight: 650; }
    .faq p { color: var(--muted); margin: 10px 0 0; }
    .connect { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
    .connect article { background: var(--paper); border: 1px solid var(--line); border-radius: 16px; padding: 22px; }
    .connect h3 { margin: 0 0 10px; }
    .connect ol { margin: 0; padding-left: 20px; color: var(--muted); }
    .connect li { margin: 6px 0; }
    .price { font-size: 2rem; font-weight: 750; margin: 6px 0; }
    @media (max-width: 900px) { .includes, .connect { grid-template-columns: 1fr 1fr; } }
    @media (max-width: 600px) { .includes, .connect { grid-template-columns: 1fr; } .dropdown { display: none; } }
"""

NAV = [
    ("index.html", "Home", None),
    ("use-cases.html", "Use cases", [
        ("use-cases.html#healthcare", "Healthcare and research"),
        ("use-cases.html#legal", "Legal"),
        ("use-cases.html#professional", "Accounting and professional services"),
        ("use-cases.html#field", "Field operations and compliance"),
        ("use-cases.html#education", "Education"),
        ("use-cases.html#prompt-library", "Prompt library"),
    ]),
    ("pricing.html", "Pricing", None),
    ("developers.html", "Developers", [
        ("developers.html#open-source", "Open source"),
        ("developers.html#local", "Run it locally"),
        ("developers.html#rules", "Data rules"),
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
    items.append('<a class="button small" href="get-started.html">Get started</a>')
    return f"""  <header class="topbar">
    <nav class="nav wrap" aria-label="Main navigation">
      <a class="brand" href="index.html" aria-label="NeoApps.AI home"><span class="brand-mark">N</span>NeoApps.AI</a>
      <div class="nav-links">{''.join(items)}</div>
    </nav>
  </header>"""


FOOTER = f"""  <footer>
    <div class="wrap footer-row">
      <a class="brand" href="index.html"><span class="brand-mark">N</span>NeoApps.AI</a>
      <span>Business apps you own. Built in Claude, ChatGPT or Codex.</span>
      <div class="footer-links"><a href="get-started.html">Get started</a><a href="pricing.html">Pricing</a><a href="developers.html">Developers</a><a href="{GITHUB}">GitHub</a><a href="mailto:info@neopric.com">Contact</a></div>
    </div>
  </footer>"""


def page(file, title, description, body, scripts=("assets/site.js",)):
    js = "".join(f'\n  <script src="{s}"></script>' for s in scripts)
    html = f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="theme-color" content="#f7f8fa">
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
    ]),
]

CLOUD_PROMPT = ("Use the NeoApps connector. I want to build [describe your business app]. Ask me about users, data, approvals and "
                "permissions, then show me the preview and wait for my review. After I approve, publish it, check it's live and give me "
                "the link. Never ask me for passwords, card numbers or API keys in this chat.")

# ---------------------------------------------------------------- Home
home = f"""    <section class="hero">
      <div class="wrap hero-grid">
        <div>
          <span class="eyebrow"><span class="dot"></span> Works in Claude, ChatGPT and Codex</span>
          <h1>Describe your process.<br>Get a working business app.</h1>
          <p>Logins, roles, approvals, audit trail and AI, built from a conversation. Live the same day. Change it any time; your data stays.</p>
          <div class="hero-actions">
            <a class="button" href="get-started.html">Start building</a>
            <a class="button secondary" href="use-cases.html">See use cases</a>
          </div>
          <p class="hero-note">Production apps, not prototypes. Open source. You own the code.</p>
        </div>
        <div class="terminal" aria-label="Example conversation">
          <div class="terminal-head"><span></span><span></span><span></span> claude · neoapps</div>
          <div class="terminal-body">
            <div><span class="prompt">you &gt;</span> Build a client portal for my firm</div>
            <div class="term-muted">Who uses it? What needs approval?</div>
            <div class="term-blue">Preview: 6 screens, 4 roles, 2 approvals</div>
            <div class="term-muted">Approved → publishing…</div>
            <div><span class="prompt">neoapps &gt;</span> Live at yourfirm.us.neoapps.ai</div>
          </div>
        </div>
      </div>
    </section>
""" + section("how-it-works", "How it works", "Four steps. No code.", None,
    '        <div class="steps">'
    '<article class="step"><div class="step-num">01</div><h3>Connect</h3><p>Add NeoApps to Claude, ChatGPT or Codex. One link.</p></article>'
    '<article class="step"><div class="step-num">02</div><h3>Describe</h3><p>Explain your process. Your assistant asks the right questions.</p></article>'
    '<article class="step"><div class="step-num">03</div><h3>Review</h3><p>See every screen, field and permission before anything is built.</p></article>'
    '<article class="step"><div class="step-num">04</div><h3>Publish</h3><p>It goes live. Invite your team and customers.</p></article></div>', white=True) \
+ section("includes", "What you get", "Every app is production grade.", "The parts business software needs, enforced on the server, in every app.", cards(INCLUDES)) \
+ section("industries", "Use cases", "Built for real businesses.", None,
    cards([(name, items[0][1]) for _, name, items in INDUSTRIES], cls="include", grid="includes")
    + '\n        <p style="margin:20px 0 0"><a class="text-link" href="use-cases.html">All use cases and ready-made prompts →</a></p>', white=True) \
+ section("proof", "Proof", "ClaimKit: years of R&D tax-credit expertise, live in four hours.", None,
    '        <div class="video-grid"><div class="video-frame"><iframe src="https://www.youtube-nocookie.com/embed/MD_jAxglA_Y?rel=0" title="ClaimKit walkthrough" loading="lazy" referrerpolicy="strict-origin-when-cross-origin" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe></div>'
    '<div class="video-copy"><p>Our team built ClaimKit, a full client portal for R&amp;D tax-credit consultants, with a coding agent and NeoApps in about four hours. A typical agency build takes five to six months.</p>'
    '<p>The expert still decides. The app does the intake, documents and evidence.</p></div></div>') \
+ """    <section class="section white">
      <div class="wrap">
        <div class="cta-band">
          <div><h2>Your process. Your app.</h2><p>Start free with the open source, or go live on NeoApps Cloud from US$49/month.</p></div>
          <a class="button" href="get-started.html">Start building</a>
        </div>
      </div>
    </section>
"""
page("index.html", "NeoApps.AI: describe your process, get a working business app",
     "Describe your process in Claude, ChatGPT or Codex and get a production business app with logins, approvals, audit trail and AI.", home)

# ---------------------------------------------------------------- Use cases
uc = ""
for i, (anchor, name, items) in enumerate(INDUSTRIES):
    uc += section(anchor, "Use cases" if i == 0 else name, name if i else name, "Each one is a starting point. Your assistant adapts it to how you work." if i == 0 else None,
                  cards(items), white=bool(i % 2))
uc += """    <section class="section white" id="prompt-library">
      <div class="wrap">
        <div class="section-heading">
          <div class="kicker">Prompt library</div>
          <h2>Start from a ready-made prompt.</h2>
          <p>Copy one into Claude, ChatGPT or Codex. Your assistant asks questions and shows a preview before building.</p>
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
gs = section("connect", "Get started", "Connect once. Then just describe your app.", "Pick the assistant you already use.",
    f"""        <div class="connect">
          <article><h3>Claude</h3><ol><li>Open the Claude desktop app: Settings → Connectors → Add custom connector.</li><li>Paste <code>{CLOUD_URL}</code></li><li>Click Connect and sign in to NeoApps. It also works in the Claude mobile app.</li></ol></article>
          <article><h3>ChatGPT</h3><ol><li>Settings → Apps &amp; Connectors → Advanced settings: turn on Developer mode.</li><li>Choose Create and paste <code>{CLOUD_URL}</code></li><li>Sign in to NeoApps when asked.</li></ol></article>
          <article><h3>Codex</h3><ol><li>Run <code>codex mcp add neoapps --url {CLOUD_URL}</code></li><li>Sign in to NeoApps when asked.</li></ol></article>
        </div>
        <p style="margin:18px 0 0;color:var(--muted)">First time: create your NeoApps account and pick a plan. A card is required; promo codes go in at checkout.</p>""", white=True) \
+ section("prompt", "Then", "Paste this and describe your business.", None, f'        <div style="max-width:860px">{prompt_box(CLOUD_PROMPT)}</div>') \
+ section("after", "After it's live", "Invite people. Add AI. Keep improving.", None,
    '        <div class="steps">'
    '<article class="step"><div class="step-num">01</div><h3>Invite people</h3><p>Add your team and customers in Members, with the role they need.</p></article>'
    '<article class="step"><div class="step-num">02</div><h3>Turn on AI</h3><p>Ask your assistant for the AI settings link. Paste your own key there, never in a chat.</p></article>'
    '<article class="step"><div class="step-num">03</div><h3>Change it</h3><p>Tell your assistant what to change. Review, publish. Your data stays.</p></article>'
    '<article class="step"><div class="step-num">04</div><h3>Forgot your login?</h3><p>Go to <a class="text-link" href="https://cloud.neoapps.ai/recover">cloud.neoapps.ai/recover</a>.</p></article></div>', white=True) \
+ section(None, "Prefer to run it yourself?", "Free and open source.", None,
    '        <p><a class="button secondary" href="developers.html">Developer setup →</a></p>')
page("get-started.html", "Get started · NeoApps.AI", "Connect NeoApps to Claude, ChatGPT or Codex and build your first business app.", gs)

# ---------------------------------------------------------------- Pricing
pr = section("plans", "Pricing", "Simple pricing. Your data and code stay yours.", None,
    """        <div class="paths">
          <article class="path-card"><div class="path-label">Open source</div><h3>Run it yourself</h3><div class="price">Free</div><p>Generate and host apps on your own machines. You manage hosting and updates.</p><a class="button secondary" href="developers.html">Developer setup</a></article>
          <article class="path-card featured"><div class="path-label">NeoApps Cloud</div><h3>One app</h3><div class="price">US$49<small>/month</small></div><p>Hosting, database and sign-in included. Built and published from your assistant.</p><a class="button" href="get-started.html">Get started</a></article>
          <article class="path-card"><div class="path-label">NeoApps Cloud</div><h3>Up to three apps</h3><div class="price">US$149<small>/month</small></div><p>Everything in One app, for up to three apps.</p><a class="button secondary" href="get-started.html">Get started</a></article>
        </div>
        <p style="margin:18px 0 0;color:var(--muted)">AI inside your apps runs on your own Anthropic or OpenAI-compatible key, billed by them. Your Claude, ChatGPT or Codex plan is separate. Data is hosted in the US.</p>
        <p style="margin:8px 0 0;color:var(--muted)">Need it built for you? <a class="text-link" href="mailto:info@neopric.com?subject=NeoApps%20custom%20build">Talk to us</a> about custom apps, hosting and support.</p>""", white=True) \
+ section("adopt", "How teams adopt", "Start small. Grow when it sticks.", None,
    '        <ol style="max-width:820px;margin:0;padding-left:20px;line-height:1.9">'
    '<li><b>One team, one workflow.</b> Replace a spreadsheet in a day.</li>'
    '<li><b>Invite the team.</b> Set who sees and changes what.</li>'
    '<li><b>Add the next workflow</b> once the first one sticks.</li></ol>') \
+ section("faq", "FAQ", "Questions people ask.", None, '        <div class="faq" style="max-width:820px">' + "".join(
    f"<details><summary>{q}</summary><p>{a}</p></details>" for q, a in [
        ("Do I need to code?", "No. You describe your business in Claude, ChatGPT or Codex. You review a preview and approve it."),
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
page("pricing.html", "Pricing · NeoApps.AI", "NeoApps Cloud from US$49/month for one app, US$149/month for up to three. Free and open source to self-host.", pr)

# ---------------------------------------------------------------- Developers
dev = section("open-source", "Developers", "Open source. Real stack. No lock-in.",
    "NeoApps turns a MySQL schema and a UI config into a full app. Two years open source, built on 15 years of ERP work.",
    cards([
        (".NET 10 API", "Generated controllers, repositories, JWT auth and Swagger."),
        ("React admin UI", "React 19 and MUI, driven by schema.json and ui.config.json."),
        ("MySQL, Redis, RabbitMQ", "Background jobs, approvals and events, out of the box."),
        ("MCP server and CLI", "Every operation as a tool for Claude, ChatGPT, Codex and others."),
    ]), white=True) \
+ section("local", "Run it locally", "One command to install.", "Needs Docker. Registers the MCP server with Claude Code and Claude Desktop and prints the config for Codex and others.",
    f"""        <div class="prompt-box" style="max-width:860px"><pre class="copy-text">curl -fsSL https://raw.githubusercontent.com/Neopric-Inc/NeoApps.AI-CodeGenerator/main/install.sh | bash</pre><div class="copy-row"><span class="copy-status" aria-live="polite"></span><button class="button secondary small copy-button" type="button">Copy</button></div></div>
        <p style="margin:16px 0 0">Then ask your agent to build an app, or use the CLI: <code>neoapps new MyApp --schema schema.sql --ui ui.config.json --deploy</code>. <a class="text-link" href="{GITHUB}#coding-agents-mcp">Setup guide ↗</a></p>""") \
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
    ]), white=True) \
+ section("build-yourself", "Prompt", "Build and host it yourself.", None,
    '        <div style="max-width:860px">' + prompt_box("I want to build [describe my business app]. Ask me about users, processes, data and permissions. Check the NeoApps schema guidelines and my environment. Show me a plan and wait for my approval. Then generate, build and run the app locally with the NeoApps tools, keep the source in my project, and show me how to host it myself.") + "</div>"
    + f'\n        <p style="margin:16px 0 0"><a class="button secondary" href="{GITHUB}">View on GitHub ↗</a></p>')
page("developers.html", "Developers · NeoApps.AI", "Open-source generator: MySQL schema and UI config to a .NET 10 API and React admin UI, with an MCP server and CLI.", dev)
print("built", [p.name for p in ROOT.glob("*.html")])
