using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace nkv.MicroService.API.Ai
{
    /// <summary>
    /// AI configuration. The API key comes from the ANTHROPIC_API_KEY environment variable (or AI:ApiKey);
    /// behaviour (assistant name, instructions, document tables, tools, workflows) comes from the "ai" section of
    /// ui.config.json shipped with the API, overridden by the copy an admin saved in Admin > Customize UI.
    /// </summary>
    public sealed class AiSettings
    {
        private readonly IConfiguration _configuration;
        private readonly string _contentRoot;
        private readonly string _connectionString;

        public AiSettings(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _contentRoot = env.ContentRootPath;
            _connectionString = configuration.GetConnectionString("MySQLDatabase");
        }

        public string ApiKey => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is { Length: > 0 } k ? k : _configuration["AI:ApiKey"];
        public string BaseUrl => _configuration["AI:BaseUrl"] is { Length: > 0 } u ? u.TrimEnd('/') : "https://api.anthropic.com";
        public string FilesRoot => _configuration["AI:FilesPath"] is { Length: > 0 } p ? p : Path.Combine(_contentRoot, "App_Data", "files");
        public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

        /// <summary>The effective "ai" section for a tenant: file defaults merged with the admin-saved UI config.</summary>
        public JsonObject Config(string tenant)
        {
            var result = new JsonObject();
            var file = Path.Combine(_contentRoot, "ui.config.json");
            if (File.Exists(file) && JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) is JsonObject f)
            {
                if (f["ai"] is JsonObject ai) Merge(result, ai);
                if (f["appName"] != null) result["__appName"] = f["appName"].DeepClone();
                if (f["tagline"] != null) result["__tagline"] = f["tagline"].DeepClone();
            }
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(@"SELECT layout FROM dnd_ui_versions WHERE dnd_ui_type='neoapps-ui-config' AND createdBy=@o AND isActive=1
                                                   ORDER BY dnd_ui_version_id DESC LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@o", tenant == null ? "" : tenant.Length <= 45 ? tenant : tenant.Substring(0, 45));
                if (cmd.ExecuteScalar() is string saved && JsonNode.Parse(saved) is JsonObject s)
                {
                    if (s["ai"] is JsonObject ai) Merge(result, ai);
                    if (s["appName"] != null) result["__appName"] = s["appName"].DeepClone();
                }
            }
            catch { /* saved config is optional */ }
            return result;
        }

        public bool Enabled(JsonObject cfg) => HasKey && (cfg["enabled"]?.GetValue<bool>() ?? true);
        public string Model(JsonObject cfg) => cfg["model"]?.GetValue<string>() ?? _configuration["AI:Model"] ?? "claude-sonnet-5-5";

        private static void Merge(JsonObject target, JsonObject patch)
        {
            foreach (var (k, v) in patch)
            {
                if (v is JsonObject po && target[k] is JsonObject to) Merge(to, po);
                else target[k] = v?.DeepClone();
            }
        }
    }
}
