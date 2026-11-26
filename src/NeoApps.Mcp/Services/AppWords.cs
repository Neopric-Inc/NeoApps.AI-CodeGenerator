using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NeoApps.Mcp.Services;

/// <summary>
/// The app's own words as the generated UI shows them in English (table and field labels, option labels, menu groups,
/// dashboard cards, AI names and buttons, validation messages, notification titles), and the checks for ui.config "i18n":
/// languages offered by the app and, per language, which of those words still lack a translation. The rules for
/// derived labels mirror ReactUiKit/src/neoapps/config.ts (humanize, singularize) and i18n/index.ts (optionText).
/// </summary>
public static class AppWords
{
    /// <summary>Languages the built-in screens are translated into (ReactUiKit/src/neoapps/i18n/languages.ts).</summary>
    public static readonly string[] BuiltInLanguages =
    [
        "en", "fr", "es", "pt", "de", "it", "nl", "pl", "ro", "cs", "hu", "el", "sv", "da", "nb", "fi", "tr", "ru", "uk",
        "ar", "he", "fa", "ur", "hi", "gu", "mr", "bn", "pa", "ta", "te", "kn", "ml", "zh-CN", "zh-TW", "ja", "ko", "vi",
        "th", "id", "ms", "fil", "sw",
    ];

    static readonly string[] Audit = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive", "created_by_user", "modified_by_user"];
    static readonly Dictionary<string, string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
        { ["id"] = "ID", ["nct"] = "NCT", ["url"] = "URL", ["api"] = "API", ["fte"] = "FTE", ["gcp"] = "GCP", ["icf"] = "ICF", ["sop"] = "SOP", ["soa"] = "SoA" };

    /// <summary>"trial_opportunities" -> "Trial Opportunities" (config.ts humanize).</summary>
    public static string Humanize(string name)
    {
        var s = Regex.Replace(Regex.Replace(name, "_id$", ""), "([a-z0-9])([A-Z])", "$1 $2");
        return string.Join(" ", Regex.Split(s, @"[_\s]+").Where(w => w.Length > 0)
            .Select(w => Acronyms.TryGetValue(w, out var a) ? a : char.ToUpperInvariant(w[0]) + w[1..]));
    }

    public static string Singularize(string label)
    {
        if (Regex.IsMatch(label, "ies$", RegexOptions.IgnoreCase)) return Regex.Replace(label, "ies$", "y", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(label, "(ss|us)$", RegexOptions.IgnoreCase)) return label;
        if (Regex.IsMatch(label, "(ches|shes|xes|ses)$", RegexOptions.IgnoreCase)) return Regex.Replace(label, "es$", "", RegexOptions.IgnoreCase);
        return Regex.Replace(label, "s$", "", RegexOptions.IgnoreCase);
    }

    /// <summary>"in_progress" -> "In progress" (i18n optionText).</summary>
    public static string OptionText(string v)
    {
        if (!Regex.IsMatch(v, "^[a-z0-9]+(?:[_-][a-z0-9]+)*$")) return v;
        var s = Regex.Replace(v, "[_-]+", " ");
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    static bool True(JsonNode? n) => n is JsonValue v && v.TryGetValue<bool>(out var b) && b;
    static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;

    /// <summary>Every English text the app shows that comes from its schema and ui.config, in display order, without duplicates.</summary>
    public static List<string> Collect(List<AppPreview.Table> tables, JsonObject? ui)
    {
        var words = new List<string>();
        var seen = new HashSet<string>();
        void Add(string? s) { if (!string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s, "[A-Za-z]") && seen.Add(s)) words.Add(s); }
        var cfgTables = ui?["tables"] as JsonObject;
        JsonObject? Cfg(string t) => cfgTables?[t] as JsonObject;
        string TableLabel(string t) => Str(Cfg(t)?["label"]) ?? Humanize(t);
        string RefLabel(string t) => Str(Cfg(t)?["singular"]) ?? Singularize(TableLabel(t));

        Add(Str(ui?["appName"])); Add(Str(ui?["tagline"]));
        foreach (var g in ui?["menu"] as JsonArray ?? []) Add(Str(g?["group"]));
        foreach (var c in ui?["dashboard"]?["cards"] as JsonArray ?? []) Add(Str(c?["label"]));
        foreach (var t in tables)
        {
            var cfg = Cfg(t.Name);
            if (True(cfg?["hidden"])) continue;
            var label = TableLabel(t.Name);
            Add(label); Add(Str(cfg?["singular"]) ?? Singularize(label)); Add(Str(cfg?["description"]));
            foreach (var c in t.Columns.Where(c => !Audit.Contains(c.Name) && !c.Pk))
            {
                var fc = cfg?["fields"]?[c.Name] as JsonObject;
                if (True(fc?["hidden"])) continue;
                var fk = t.ForeignKeys.FirstOrDefault(f => f.Column == c.Name);
                Add(Str(fc?["label"]) ?? (fk != null ? RefLabel(fk.RefTable) : Humanize(c.Name)));
                Add(Str(fc?["help"])); Add(Str(fc?["placeholder"]));
                var labels = fc?["optionLabels"] as JsonObject;
                var options = (fc?["options"] as JsonArray)?.Select(o => o?.ToString() ?? "").ToList()
                              ?? EnumValues(c.Type);
                foreach (var o in options.Where(o => o.Length > 0)) Add(Str(labels?[o]) ?? OptionText(o));
            }
        }
        if (ui?["ai"] is JsonObject ai)
        {
            Add(Str(ai["assistantName"]));
            foreach (var s in ai["suggestions"] as JsonArray ?? []) Add(Str(s));
            foreach (var wf in ai["workflows"] as JsonArray ?? []) { Add(Str(wf?["label"])); Add(Str(wf?["description"])); }
        }
        foreach (var (_, cols) in ui?["validations"] as JsonObject ?? [])
            foreach (var (_, rule) in cols as JsonObject ?? []) Add(Str(rule?["message"]));
        foreach (var n in ui?["notifications"] as JsonArray ?? []) Add(Str(n?["title"]));
        foreach (var r in (ui?["roles"] as JsonObject ?? []).Select(kv => kv.Key)) Add(r);
        return words;
    }

    static List<string> EnumValues(string type)
    {
        var m = Regex.Match(type, @"^enum\((.*)\)$", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Split(',').Select(s => s.Trim().Trim('\'')).ToList() : [];
    }

    /// <summary>Problems with ui.config "i18n": unknown languages and app words without a translation, per language.</summary>
    public static void Check(List<AppPreview.Table> tables, JsonObject? ui, List<string> w)
    {
        if (ui?["i18n"] is not JsonObject i18n) return;
        var langs = (i18n["languages"] as JsonArray)?.Select(l => l?.ToString() ?? "").ToList() ?? [];
        var def = Str(i18n["default"]) ?? "en";
        foreach (var l in langs.Append(def).Distinct())
            if (!BuiltInLanguages.Contains(l))
                w.Add($"i18n: '{l}' is not a built-in language ({string.Join(", ", BuiltInLanguages)}); its screens would stay in English.");
        if (langs.Count > 0 && !langs.Contains(def)) w.Add($"i18n: the default language '{def}' is not in i18n.languages.");
        var translations = i18n["translations"] as JsonObject ?? [];
        var words = Collect(tables, ui);
        var targets = (langs.Count > 0 ? langs : translations.Select(kv => kv.Key).ToList()).Where(l => l != "en").ToList();
        foreach (var l in targets)
        {
            var have = translations[l] as JsonObject;
            var missing = words.Where(x => have?[x] == null).ToList();
            if (missing.Count > 0)
                w.Add($"i18n: {missing.Count} of {words.Count} app words have no {l} translation (add them to i18n.translations.{l}): " +
                      string.Join(" | ", missing.Take(60)) + (missing.Count > 60 ? " | …" : ""));
        }
    }
}
