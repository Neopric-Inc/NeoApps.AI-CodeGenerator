using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;

namespace NeoApps.Mcp.Tools;

/// <summary>Reading, merging and documenting ui.config.json, the file that shapes the generated admin UI.</summary>
internal static class UiConfigTools
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    public static JsonObject? Read(string file) =>
        File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file), documentOptions: Lenient) as JsonObject : null;

    public static string Normalize(string json) => Parse(json).ToJsonString(Pretty);

    /// <summary>RFC 7386-style merge: objects merge recursively, null deletes, everything else replaces.</summary>
    public static string Merge(JsonObject? current, string changes)
    {
        var target = current ?? new JsonObject();
        MergeInto(target, Parse(changes));
        return target.ToJsonString(Pretty);
    }

    private static void MergeInto(JsonObject target, JsonObject patch)
    {
        foreach (var (key, value) in patch.ToList())
        {
            if (value is null) { target.Remove(key); continue; }
            if (value is JsonObject po && target[key] is JsonObject to) { MergeInto(to, po); continue; }
            target[key] = value.DeepClone();
        }
    }

    private static JsonObject Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json, documentOptions: Lenient) as JsonObject
                ?? throw new McpException("ui config must be a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new McpException("Invalid JSON for ui config: " + ex.Message);
        }
    }

    /// <summary>table -> "col:type, col->ref, ..." from the generated public/schema.json, so Claude can plan changes.</summary>
    public static object? SchemaSummary(string? frontendPath)
    {
        var file = frontendPath is null ? null : Path.Combine(frontendPath, "public", "schema.json");
        if (file is null || !File.Exists(file)) return null;
        var schema = JsonNode.Parse(File.ReadAllText(file))!;
        return schema["tables"]!.AsArray().ToDictionary(
            t => t!["name"]!.GetValue<string>(),
            t => string.Join(", ", t!["columns"]!.AsArray().Select(c =>
                c!["name"]!.GetValue<string>() + (c["references"] is JsonObject r ? "->" + r["table"] : ":" + c["columnType"]))));
    }

    public const string Guide = """
        ui.config.json shapes the generated admin UI. Everything is optional: without it the UI infers labels, widgets,
        list columns, display names for related records, child tables and menu icons from the schema.
        Admins can also edit it in the running app (Admin > Customize UI); that saved copy overrides the file.

        {
          "appName": "Research Site OS",
          "tagline": "Studies, documents and startup in one place",
          "theme": { "primary": "#1f6feb", "secondary": "#0ca678" },
          "menu": [                                   // sidebar groups, in order; unlisted tables go under "More"
            { "group": "Studies", "items": ["studies", "visits", "assessments"] },
            { "group": "Documents", "items": ["documents", "source_documents"] }
          ],
          "dashboard": { "cards": [ { "table": "studies", "label": "Active studies" },          // row count
                                    { "table": "studies", "label": "Total budget", "sum": "budget_amount",  // or a total,
                                      "where": { "status": "enrolling" }, "format": "currency", "currencyCode": "USD" } ], // optionally filtered
                         "recentTable": "documents" },
          "roles": {                                  // seeded when the database is created (c=create r=read u=update d=delete)
            "Coordinator": { "*": "r", "documents": "cru", "visits": "cru" },
            "Viewer": { "*": "r" }
          },                                          // default roles if omitted: Member {"*":"cru"}, Viewer {"*":"r"}
          "tables": {
            "studies": {
              "label": "Studies", "singular": "Study", "icon": "science",
              "description": "Trials the site is running or evaluating",
              "displayField": "title",               // shown wherever another table references a study
              "listColumns": ["protocol_number", "title", "phase", "status", "current_enrollment"],
              "formFields": ["sponsor_id", "protocol_number", "title", "phase", "status"],   // form order
              "defaultSort": "start_date|DESC",
              "children": ["visits", "documents"],    // related lists shown on a study's page
              "hidden": false,                       // true hides the table from the menu
              "fields": {
                "status": { "widget": "select", "options": ["startup", "enrolling", "closed"],
                            "colors": { "enrolling": "success", "closed": "default" } },
                "budget_amount": { "label": "Budget", "widget": "currency", "currencyCode": "USD" },
                "nct_number": { "label": "NCT #", "help": "ClinicalTrials.gov identifier", "placeholder": "NCT01234567" },
                "notes": { "hidden": true },          // hidden everywhere; "hideInForm": true hides only in the form
                "current_enrollment": { "readOnly": true, "defaultValue": 0 }
              }
            }
          }
        }

        widgets: text, textarea, number, currency, percent, date, datetime, checkbox, select (needs "options"),
                 reference (foreign keys; automatic), email, url, color.
        colors (for select values): default, primary, secondary, success, warning, error, info.
        icons: table, people, description, event, assignment, science, business, chat, link, bolt, travel_explore,
               receipt, inventory, folder, dashboard, settings, security, palette, check, hospital, calendar, star,
               cart, school, home.
        Audit columns (createdBy, modifiedBy, createdAt, modifiedAt, isActive) and primary keys stay out of forms automatically.
        Select fields render as coloured chips in lists, record pages and the dropdown itself.

        Custom screens (when config isn't enough): put React code in workspace/<App>/custom/index.tsx next to ui.config.json.
        It is copied to the frontend's src/custom/ on every generate and must export
          export const extensions: Extensions = { routes: [{ path, element }], nav: [{ to, icon, label }],
                                                 dashboardTop: Component, recordActions: Component<{ table, record, onChanged }> };
        (types in src/neoapps/extensions.ts; use api/listRecords/createRecord/updateRecord from ../neoapps/api and useApp()
        for me/can()). Example: samples/splitshare/custom.

        Data rules (enforced by the generated API on every query, AI included; see Utility/DataGuard*.cs). All optional:
          "tenancy": { "column": "tenant_id", "tenantTable": "tenants", "sharedWritable": [] }
              Tables with the column are isolated per tenant: reads filtered, the column forced on insert/update from the
              signed token (never the request), foreign keys must point at the same tenant, tables without the column are
              read-only for tenant users. Members get a tenant in Admin > Members (POST /Access/members "tenant_id");
              users without one are platform users. Tenant admins can't edit shared role templates or the UI config.
          "rowScopes": [ { "name": "customer", "roles": ["Customer User"], "column": "customer_id",
                           "userValue": { "table": "contacts", "match": "login_username", "column": "customer_id" } } ]
              Row-level access inside a tenant: users in the roles only reach rows of their own value (here their customer):
              tables with the column, the home table whose primary key is the column (customers), and tables linked to
              those by foreign keys (up to 3 hops, e.g. tasks.case_id -> cases.customer_id). The column is forced on writes,
              foreign keys must stay inside the scope, users without a value see nothing, other roles are unaffected, and
              AI files of out-of-scope records are hidden. "match" is compared with the login name ("matchBy": "user_id" for
              the numeric user id); optional "tables" / "exclude" limit where it applies. Same pattern for "experts only
              see their assigned cases" (column assigned_expert_id, home table = the experts table).
          "lifecycles": { "cases": { "column": "status", "initial": ["draft"], "frozen": ["closed"],
              "transitions": [ { "from": "draft", "to": "submitted", "roles": ["*"], "actors": ["human"] },
                               { "from": "qa", "to": "delivered", "roles": ["Manager"],
                                 "requires": [ { "table": "approvals", "link": "case_id", "where": { "status": "approved" },
                                                 "message": "needs an approval" } ],
                                 "forbids": [ { "table": "qa_items", "link": "case_id", "where": { "result": "failed" },
                                                "message": "QA has failed items" } ],
                                 "distinctFrom": ["requested_by"], "adminBypass": false } ],
              "history": { "table": "case_status_history", "link": "case_id", "from": "old_status", "to": "new_status", "actor": "changed_by_type" },
              "timestamps": { "delivered": "delivered_at" },
              "sla": { "table": "case_deadlines", "link": "case_id", "type": "deadline_type", "due": "due_at", "met": "met_at",
                       "hours": { "expert_review": 24 } } } }
              Only listed steps are allowed (409 otherwise), by the listed roles (Administrator: any listed step) and actors
              ("human" default; add "ai" to let AI workflows do it). "requires": related rows that must exist ("linkFrom":
              column of this row to match instead of its id); "forbids": related rows that must NOT exist. "distinctFrom":
              separation of duties: columns holding the person who may not do this step (a users id or login name, or
              { "column": "requested_by_id", "table": "members", "userColumn": "user_id" } for a member FK); applies to
              administrators too. "adminBypass" (per lifecycle or per step): true (default), "platform" (only platform
              admins, never tenant-bound ones) or false (admins need a listed role). Frozen states lock the record. History rows, timestamps and SLA
              deadline rows are written automatically in the same transaction.
          "validations": { "customer_invoices": {
              "contact_email": { "format": "email" }, "website": { "format": "url" }, "phone": { "format": "phone" },
              "invoice_number": { "pattern": "^INV-[0-9]{4,}$", "message": "Use INV- followed by digits" },
              "quantity": { "min": 1, "max": 1000 }, "name": { "minLength": 2, "maxLength": 80 },
              "due_date": { "gte": "issued_at" },                        // gt / gte / lt / lte / ne: another column
              "total": { "equals": "subtotal + tax - discount_amount", "tolerance": 0.01 },
              "po_number": { "requiredWhen": { "status": ["issued", "paid"] } }, "notes": { "required": true } } }
              Field rules beyond NOT NULL and types, checked by the API on every create/update (400 with
              {"errors": {"column": ["message"]}}, shown next to the field) and by the form before saving ("equals" only
              by the API). "message" overrides the default text. On updates only rules touching a changed column run.
          "numbering": { "customer_invoices": { "column": "invoice_number", "pattern": "INV-{yyyy}-{seq:5}" },
                         "cases": [ { "column": "case_number", "pattern": "C{yy}{mm}-{seq:4}" } ] }
              Numbers assigned by the API on insert, then read-only (the form hides the field; clients may leave it empty).
              Tokens {yyyy} {yy} {mm} {dd} (UTC), {seq} / {seq:N} zero-padded, {tenant}. One gap-free sequence per
              table/column, tenant and period ("reset": yearly with a year in the pattern, monthly with {mm}, daily with
              {dd}, else never; or set it). New sequences continue after the highest number already stored.
          "immutable": { "audit_logs": ["*"], "findings": ["ai_text", "confidence"] }   // "*" = append-only; else read-only columns
          "audit": { "table": "audit_logs", "platformTable": "platform_audit_logs", "exclude": [] }
              One row per create/update/delete (actor_type, actor_ref, action_code, entity_type, entity_ref, details JSON of
              changes, correlation_ref, ip_address, tenant column: whichever the table has).
          "outbox": { "table": "outbox_events", "exchange": "app.events", "maxAttempts": 5,
                      "events": [ { "table": "cases", "on": "update", "when": { "status": "submitted" }, "event": "CaseSubmitted" } ] }
              Event rows (event_type, aggregate_type, aggregate_ref, payload, status...) written in the write's transaction and
              published to RabbitMQ (topic exchange, routing key = event, MessageId for de-duplication, retries, dead letter).
          "jobs": { "everySeconds": 60, "deadlines": [ { "table": "case_deadlines", "due": "due_at", "met": "met_at", "breached": "is_breached",
                    "escalate": { "table": "case_escalations", "values": { "case_id": "{case_id}", "reason": "Missed {deadline_type}" } } } ] }
          "limits": [ { "table": "members", "where": { "kind": "employee" }, "max": { "from": "subscriptions", "column": "seats", "where": { "status": "active" } } },
                      { "table": "usage", "sum": "quantity", "period": "month", "max": { "from": "plans", "column": "credits",
                        "join": { "table": "subscriptions", "on": "plan_id" } } } ]     // 402 when exceeded; platform users exempt
          Files: uploads are stored under tenant-{id}/; GET /Ai/files/{id}/link returns a 5-minute signed download link.
          Verify with ./neoapps test <App> (tenant isolation on every table, append-only tables, lifecycle steps).

        AI assistant (Claude). Optional "ai" section; active when the API runs with ANTHROPIC_API_KEY set:
          "ai": {
            "enabled": true,
            "model": "claude-sonnet-5-5",
            "assistantName": "Site Assistant",
            "instructions": "Who the users are and how to answer (domain rules, tone, what not to do).",
            "documentTables": ["documents"],          // record pages of these tables get a Files panel; uploads are indexed
            "tools": ["documents", "records", "clinicaltrials_gov"],   // what the assistant may use
            "suggestions": ["Which studies are enrolling?"],           // starter questions on the Assistant page
            "roles": ["Coordinator"],                 // optional: only these roles (and Administrator) see the assistant,
                                                      // Ask AI and workflows; others keep the Files panel for uploads
            "workflows": [
              { "id": "soa_to_source", "label": "Build visit schedule",
                "table": "documents", "when": { "document_type": "protocol" },   // button on matching records; omit "table" for a global workflow
                "targets": ["visits", "assessments", "visit_assessments"],     // tables it may create rows in (user needs create permission)
                "inherit": { "study_id": "study_id" },                          // target column <- source record column
                "tools": ["records"],                                           // extra tools while running (optional)
                "description": "Shown in the dialog.",
                "instructions": "Step-by-step: what to read and which rows to create. New rows link with '@<ref>'.",
                "requiresDocument": false }                                       // default true: the record must have an uploaded file;
                                                                                  // set false for workflows that work from records (claims, projects)
            ]
          }
        The assistant answers from uploaded files (with [S1] citations to file and page) and from the app's records,
        always limited to what the signed-in user may read. Workflows run as background jobs (RabbitMQ worker): the dialog
        shows each step live, the job pauses with a proposal the user reviews (and can untick rows), and nothing is created
        until they approve. They can close the dialog and come back under Background jobs. "records" = query/aggregate
        tables; "clinicaltrials_gov" = live ClinicalTrials.gov search. Model enum-like target columns as VARCHAR and
        describe valid values in "instructions".

        Background jobs. Other long-running work (imports, monthly runs, anything that waits for approval) is a C# class
        deriving from JobHandler in workspace/<App>/custom-api/ (copied into <App>.API/Custom/ on every generate,
        "nkv.MicroService" is replaced by the app name). It gets the same queue, live feed, approval/resume, cancel and
        per-user data scoping, and can attach screenshots/videos (ctx.AttachFile). custom-api/packages.json adds NuGet
        packages and browsers ({"packages": {"Microsoft.Playwright": "1.63.0"}, "browsers": ["chromium"]}). Examples:
        samples/custom-api-example/ (approval), samples/custom-api-browser/ (Playwright page check). Show the Background jobs page in
        the menu without AI with: "jobs": { "menu": true }
        """;
}
