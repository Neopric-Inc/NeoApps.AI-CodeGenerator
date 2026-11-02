using System.Text.RegularExpressions;
using NeoApps.Mcp.Tools;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Planning workspace for an app before (and between) generations: workspace/&lt;App&gt;/
///   requirements.md   living requirements doc (written with the user)
///   schema.sql        planned schema
///   ui.config.json    planned screens, roles, AI
///   preview.html      visual of the plan (entities, screens, roles, AI, issues, what changed)
///   history/vN/       versions the user approved; previews are compared against the latest one
/// </summary>
public static class Requirements
{
    public static object Preview(ProjectStore store, string name, string? sqlSchema = null, string? uiConfig = null, bool saveVersion = false, string? copyTo = null)
    {
        name = ProjectStore.ValidateName(name);
        var dir = store.ProjectDir(name);
        Directory.CreateDirectory(dir);
        var schemaFile = Path.Combine(dir, "schema.sql");
        var uiFile = store.UiConfigFile(name);
        if (!string.IsNullOrWhiteSpace(sqlSchema)) File.WriteAllText(schemaFile, sqlSchema);
        if (!string.IsNullOrWhiteSpace(uiConfig)) File.WriteAllText(uiFile, UiConfigTools.Normalize(uiConfig));
        if (!File.Exists(schemaFile)) throw new InvalidOperationException($"No schema yet: write {schemaFile} (or pass the schema).");

        var history = Path.Combine(dir, "history");
        var latest = LatestVersion(history);
        string? Prev(string file) => latest == 0 ? null : File.Exists(Path.Combine(history, $"v{latest}", file)) ? File.ReadAllText(Path.Combine(history, $"v{latest}", file)) : null;

        var ui = File.Exists(uiFile) ? File.ReadAllText(uiFile) : null;
        var result = AppPreview.Build(File.ReadAllText(schemaFile), ui, Prev("schema.sql"), Prev("ui.config.json"), title: null);
        var html = result.Html.Replace("<title>App preview · plan</title>", $"<title>{name} · plan</title>").Replace("<h1>App preview</h1>", $"<h1>{name}</h1>");
        var previewFile = Path.Combine(dir, "preview.html");
        File.WriteAllText(previewFile, html);
        if (copyTo != null) File.WriteAllText(Path.GetFullPath(copyTo), html);

        int? saved = null;
        if (saveVersion)
        {
            saved = latest + 1;
            var vdir = Path.Combine(history, $"v{saved}");
            Directory.CreateDirectory(vdir);
            foreach (var f in new[] { schemaFile, uiFile, Path.Combine(dir, "requirements.md"), previewFile })
                if (File.Exists(f)) File.Copy(f, Path.Combine(vdir, Path.GetFileName(f)), overwrite: true);
        }

        return new
        {
            app = name,
            previewFile,
            comparedWith = latest == 0 ? null : $"v{latest}",
            savedVersion = saved == null ? null : $"v{saved}",
            requirementsFile = File.Exists(Path.Combine(dir, "requirements.md")) ? Path.Combine(dir, "requirements.md") : null,
            tables = result.Tables,
            relations = result.Relations,
            issues = result.Warnings,
            changes = result.Changes,
            next = result.Warnings.Count > 0
                ? "Fix the issues, then preview again."
                : saveVersion
                    ? $"Generate with: neoapps new {name} --schema {schemaFile} --ui {uiFile}"
                    : "Show the preview to the user. When they approve, preview again with saveVersion/--save, then generate.",
        };
    }

    private static int LatestVersion(string history) =>
        Directory.Exists(history)
            ? Directory.GetDirectories(history).Select(d => Regex.Match(Path.GetFileName(d), @"^v(\d+)$")).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).DefaultIfEmpty(0).Max()
            : 0;
}
