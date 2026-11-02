using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NoCodeAppGenerator.Infrastructure;

/// <summary>
/// Loads generator parameters. Sources, in priority order:
/// <list type="number">
/// <item><c>--config &lt;file.json&gt;</c> — a flat JSON object (preferred; used by the MCP server).</item>
/// <item><c>PARAMETER</c> env var as JSON.</item>
/// <item><c>PARAMETER</c> env var in the legacy <c>{key:value,key:value}</c> format.</item>
/// </list>
/// Keys are the legacy ones (project_id, server, uid, username, password, databaseName, script, ...).
/// </summary>
public static partial class GeneratorOptions
{
    /// <summary>Keys Main() reads unconditionally; missing ones default to null.</summary>
    private static readonly string[] KnownKeys =
    [
        "project_id", "server", "uid", "username", "password", "databaseName", "script",
        "statusOfGeneration", "projectName", "DBexists", "port", "rabbitMQConn", "redisConn",
        "apiflowurl", "fronttemplateurl", "Technology_Frontend", "Backend_technology",
        "buttonClicked", "projectType", "swgurl", "noderedurl",
    ];

    private static readonly Dictionary<string, string> Defaults = new(StringComparer.Ordinal)
    {
        ["project_id"] = "1",
        ["server"] = "localhost",
        ["uid"] = "1",
        ["username"] = "root",
        ["password"] = "",
        ["port"] = "3306",
        ["DBexists"] = "No",
        ["buttonClicked"] = "generate",
        ["rabbitMQConn"] = "amqp://guest:guest@localhost:5672/",
        ["redisConn"] = "localhost:6379",
    };

    public static Dictionary<string, string> Load(string[] args, out string source)
    {
        Dictionary<string, string> values;
        var configIndex = Array.FindIndex(args, a => a is "--config" or "-c");
        if (configIndex >= 0 && configIndex + 1 < args.Length)
        {
            source = Path.GetFullPath(args[configIndex + 1]);
            values = FromJson(File.ReadAllText(source));
            // Relative script paths in a config file are relative to that file.
            foreach (var key in new[] { "script", "uiConfig" })
            {
                if (values.TryGetValue(key, out var path) && path is not null
                    && !Uri.TryCreate(path, UriKind.Absolute, out _) && !Path.IsPathRooted(path))
                    values[key] = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, path));
            }
        }
        else
        {
            var parameter = Environment.GetEnvironmentVariable("PARAMETER");
            if (string.IsNullOrWhiteSpace(parameter))
                throw new ArgumentException("No parameters supplied. Use --config <file.json> or set PARAMETER.");
            source = "PARAMETER";
            values = parameter.TrimStart().StartsWith("{\"", StringComparison.Ordinal)
                ? FromJson(parameter)
                : FromLegacy(parameter);
        }

        // Defaults only fill keys that were not supplied at all; an explicit null/"" stays null.
        foreach (var (k, v) in Defaults)
            values.TryAdd(k, v);
        values["password"] ??= "";
        foreach (var k in KnownKeys)
            values.TryAdd(k, null);

        // Normalise flags so the legacy string comparisons ("YES", "generate") keep working.
        if (values["DBexists"] is { } dbExists)
            values["DBexists"] = dbExists.Equals("yes", StringComparison.OrdinalIgnoreCase) || dbExists == "true" ? "YES" : "NO";
        return values;
    }

    /// <summary>Redacted copy, safe for logging.</summary>
    public static string Describe(IReadOnlyDictionary<string, string> values) =>
        string.Join(Environment.NewLine, values.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv =>
            $"  {kv.Key}: {(kv.Key.Contains("password", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(kv.Value) ? "****" : kv.Value)}"));

    private static Dictionary<string, string> FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            result[p.Name] = p.Value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => NullIfEmpty(p.Value.GetString()),
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                _ => p.Value.GetRawText(),
            };
        }
        return result;
    }

    private static Dictionary<string, string> FromLegacy(string parameter)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in LegacyPair().Matches(parameter.Trim().TrimStart('{').TrimEnd('}')))
        {
            var value = match.Groups[2].Value.Trim().Trim('\'', '"');
            result[match.Groups[1].Value] = value is "null" ? null : NullIfEmpty(value);
        }
        return result;
    }

    private static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

    [GeneratedRegex(@"\b(\w+)\s*:\s*(.*?)(?=,\w+\s*:|$)")]
    private static partial Regex LegacyPair();
}
