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

        /// <summary>"anthropic" (default) or "openai" for any OpenAI-compatible endpoint (NVIDIA NIM / Nemotron, vLLM, Ollama…).
        /// Set AI_PROVIDER, or just OPENAI_API_KEY + OPENAI_BASE_URL without an Anthropic key.</summary>
        public string Provider
        {
            get
            {
                var p = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? _configuration["AI:Provider"];
                if (!string.IsNullOrWhiteSpace(p)) return p.Trim().ToLowerInvariant() is "openai" or "openai-compatible" or "nvidia" or "nim" ? "openai" : "anthropic";
                return string.IsNullOrWhiteSpace(Env("ANTHROPIC_API_KEY")) && !string.IsNullOrWhiteSpace(Env("OPENAI_API_KEY")) ? "openai" : "anthropic";
            }
        }
        public bool IsOpenAi => Provider == "openai";
        public string ProviderName => IsOpenAi ? "AI endpoint" : "Claude";
        public string ApiKey => IsOpenAi
            ? Env("OPENAI_API_KEY") ?? _configuration["AI:ApiKey"]
            : Env("ANTHROPIC_API_KEY") ?? _configuration["AI:ApiKey"];
        public string BaseUrl => IsOpenAi
            ? (Env("OPENAI_BASE_URL") ?? _configuration["AI:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/')
            : _configuration["AI:BaseUrl"] is { Length: > 0 } u ? u.TrimEnd('/') : "https://api.anthropic.com";
        private static string Env(string k) => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : null;
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
        /// <summary>AI_MODEL (env) wins over ui.config "model", so switching providers needs no config edit.</summary>
        public string Model(JsonObject cfg) => Env("AI_MODEL") ?? cfg["model"]?.GetValue<string>() ?? _configuration["AI:Model"] ?? "claude-sonnet-5-5";
        /// <summary>Model for reading scans and photos. AI_VISION_MODEL lets a text-only main model pair with a vision model.</summary>
        public string VisionModel(JsonObject cfg) => Env("AI_VISION_MODEL") ?? Model(cfg);

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
