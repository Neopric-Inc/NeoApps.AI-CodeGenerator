using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Type-checks an app's custom screens (workspace/&lt;App&gt;/custom) against the admin UI kit before anything is built,
/// so a broken screen is reported to the person (and their assistant) at preview time instead of failing a publish.
/// The UI is assembled exactly as the generator does (template entry + ReactUiKit/src + custom) in a scratch folder.
/// Packages come from NEOAPPS_UI_DEPS (a folder holding node_modules, prepared in the cloud builder image) or a cache
/// under ~/.neoapps/ui-deps keyed by the template's lockfile. Without node/npm the check is skipped.
/// </summary>
public static class CustomScreensCheck
{
    public sealed record Result(bool Ran, List<string> Errors, string? Note = null);

    public static Result Run(string generatorDir, string customDir)
    {
        if (!Directory.Exists(customDir) || !Directory.EnumerateFiles(customDir, "*.*", SearchOption.AllDirectories)
                .Any(f => f.EndsWith(".ts") || f.EndsWith(".tsx")))
            return new(false, []);
        var template = Path.Combine(generatorDir, "ReactTsTemplate3", "ReactTsProject");
        var kit = Path.Combine(generatorDir, "ReactUiKit", "src");
        if (!Directory.Exists(template) || !Directory.Exists(kit)) return new(false, [], "UI template not found");

        var deps = DependencyDir(template, out var note);
        if (deps == null) return new(false, [], note);

        var scratch = Path.Combine(Path.GetTempPath(), "neoapps-uicheck-" + Guid.NewGuid().ToString("N")[..12]);
        try
        {
            Directory.CreateDirectory(Path.Combine(scratch, "src"));
            File.Copy(Path.Combine(template, "tsconfig.json"), Path.Combine(scratch, "tsconfig.json"));
            foreach (var f in new[] { "index.tsx", "vite-env.d.ts" })
                File.Copy(Path.Combine(template, "src", f), Path.Combine(scratch, "src", f));
            CopyDir(kit, Path.Combine(scratch, "src"));
            var custom = Path.Combine(scratch, "src", "custom");
            if (Directory.Exists(custom)) Directory.Delete(custom, true);
            CopyDir(customDir, custom);
            Directory.CreateSymbolicLink(Path.Combine(scratch, "node_modules"), Path.Combine(deps, "node_modules"));

            var tsc = Path.Combine(deps, "node_modules", "typescript", "bin", "tsc");
            var r = CommandRunner.RunAsync("node", [tsc, "--noEmit", "--pretty", "false", "-p", "."], scratch,
                timeout: TimeSpan.FromMinutes(3)).GetAwaiter().GetResult();
            if (r.Ok) return new(true, []);
            var errors = r.Output.Split('\n')
                .Select(l => l.Trim())
                .Where(l => Regex.IsMatch(l, @"^src/.+\(\d+,\d+\): error TS\d+"))
                .Select(l => l.StartsWith("src/") ? l[4..] : l)
                .Distinct().ToList();
            if (errors.Count == 0) errors.Add("The custom screens do not compile: " + r.Tail(3));
            return new(true, errors);
        }
        finally
        {
            try { Directory.Delete(scratch, true); } catch { /* best effort */ }
        }
    }

    /// <summary>A folder with node_modules for the template's lockfile: NEOAPPS_UI_DEPS, else a cache filled with npm ci.</summary>
    static string? DependencyDir(string template, out string? note)
    {
        note = null;
        var preset = Environment.GetEnvironmentVariable("NEOAPPS_UI_DEPS");
        if (!string.IsNullOrEmpty(preset) && File.Exists(Path.Combine(preset, "node_modules", "typescript", "bin", "tsc")))
            return preset;

        var lockFile = Path.Combine(template, "package-lock.json");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(lockFile)))[..16].ToLowerInvariant();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neoapps", "ui-deps", hash);
        if (File.Exists(Path.Combine(dir, "node_modules", "typescript", "bin", "tsc"))) return dir;

        try
        {
            Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(template, "package.json"), Path.Combine(dir, "package.json"), true);
            File.Copy(lockFile, Path.Combine(dir, "package-lock.json"), true);
            var r = CommandRunner.RunAsync("npm", ["ci", "--ignore-scripts", "--no-audit", "--no-fund"], dir,
                timeout: TimeSpan.FromMinutes(5)).GetAwaiter().GetResult();
            if (r.Ok) return dir;
            note = "Custom screens were not checked: npm ci failed. " + r.Tail(2);
        }
        catch (Exception e)
        {
            note = "Custom screens were not checked: " + e.Message; // e.g. no node/npm on this machine
        }
        return null;
    }

    static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(from, f);
            if (rel.Split(Path.DirectorySeparatorChar).Contains("node_modules")) continue;
            var dest = Path.Combine(to, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(f, dest, true);
        }
    }
}
