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
          "dashboard": { "cards": [ { "table": "studies", "label": "Active studies" } ], "recentTable": "documents" },
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
                "budget_amount": { "label": "Budget", "widget": "currency" },
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

        AI assistant (Claude). Optional "ai" section; active when the API runs with ANTHROPIC_API_KEY set:
          "ai": {
            "enabled": true,
            "model": "claude-sonnet-5-5",
            "assistantName": "Site Assistant",
            "instructions": "Who the users are and how to answer (domain rules, tone, what not to do).",
            "documentTables": ["documents"],          // record pages of these tables get a Files panel; uploads are indexed
            "tools": ["documents", "records", "clinicaltrials_gov"],   // what the assistant may use
            "suggestions": ["Which studies are enrolling?"],           // starter questions on the Assistant page
            "workflows": [
              { "id": "soa_to_source", "label": "Build visit schedule",
                "table": "documents", "when": { "document_type": "protocol" },   // button on matching records; omit "table" for a global workflow
                "targets": ["visits", "assessments", "visit_assessments"],     // tables it may create rows in (user needs create permission)
                "inherit": { "study_id": "study_id" },                          // target column <- source record column
                "tools": ["records"],                                           // extra tools while running (optional)
                "description": "Shown in the dialog.",
                "instructions": "Step-by-step: what to read and which rows to create. New rows link with '@<ref>'." }
            ]
          }
        The assistant answers from uploaded files (with [S1] citations to file and page) and from the app's records,
        always limited to what the signed-in user may read. Workflows show a preview that the user reviews (and can
        untick rows) before anything is created. "records" = query/aggregate tables; "clinicaltrials_gov" = live
        ClinicalTrials.gov search. Model enum-like target columns as VARCHAR and describe valid values in "instructions".
        """;
}
