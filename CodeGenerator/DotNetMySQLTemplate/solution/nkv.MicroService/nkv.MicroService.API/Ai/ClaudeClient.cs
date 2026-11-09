using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace nkv.MicroService.API.Ai
{
    /// <summary>Minimal Anthropic Messages API client with a tool-use loop. With AI_PROVIDER=openai the same calls go to an OpenAI-compatible endpoint (see OpenAiCompat).</summary>
    public sealed class ClaudeClient
    {
        private readonly HttpClient _http;
        private readonly AiSettings _settings;

        public ClaudeClient(HttpClient http, AiSettings settings)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromMinutes(5);
            _settings = settings;
        }

        private const int MaxAttempts = 4;

        /// <summary>
        /// One Messages API call. Temporary failures (connection drops, timeouts, 429 rate limits, 5xx and 529 "overloaded")
        /// are retried up to <see cref="MaxAttempts"/> times with backoff, honouring retry-after.
        /// </summary>
        public async Task<JsonObject> CreateAsync(JsonObject body, CancellationToken ct)
        {
            var forcedTool = body["tool_choice"]?["type"]?.GetValue<string>() == "tool" ? body["tool_choice"]!["name"]!.GetValue<string>() : null;
            var payload = (_settings.IsOpenAi ? OpenAiCompat.ToOpenAi(body) : body).ToJsonString();
            for (var attempt = 1; ; attempt++)
            {
                TimeSpan? wait = null;
                try
                {
                    var openAi = _settings.IsOpenAi;
                    using var req = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl + (openAi ? "/chat/completions" : "/v1/messages"))
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                    };
                    if (openAi) req.Headers.Add("Authorization", "Bearer " + _settings.ApiKey);
                    else { req.Headers.Add("x-api-key", _settings.ApiKey); req.Headers.Add("anthropic-version", "2023-06-01"); }
                    using var res = await _http.SendAsync(req, ct);
                    var text = await res.Content.ReadAsStringAsync(ct);
                    if (res.IsSuccessStatusCode && openAi) return OpenAiCompat.FromOpenAi(JsonNode.Parse(text)!.AsObject(), forcedTool);
                    if (res.IsSuccessStatusCode) return JsonNode.Parse(text)!.AsObject();
                    var code = (int)res.StatusCode;
                    string msg;
                    try { var e = JsonNode.Parse(text)?["error"]; msg = (e is JsonValue ev ? ev.ToString() : e?["message"]?.GetValue<string>()) ?? text; } catch { msg = text; }
                    if (attempt >= MaxAttempts || !(code == 429 || code == 408 || code >= 500))
                        throw new AiException($"{_settings.ProviderName} API error ({code}): {msg}");
                    wait = res.Headers.RetryAfter?.Delta;
                }
                catch (HttpRequestException ex) when (attempt < MaxAttempts) { _ = ex; }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < MaxAttempts) { /* request timeout */ }
                catch (HttpRequestException ex) { throw new AiException($"Couldn't reach the {_settings.ProviderName} API: " + ex.Message); }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new AiException($"The {_settings.ProviderName} API didn't answer in time."); }
                var backoff = wait ?? TimeSpan.FromSeconds(Math.Min(30, 2 * Math.Pow(2, attempt - 1)));
                await Task.Delay(backoff > TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : backoff, ct);
            }
        }

        /// <summary>
        /// Runs the model until it stops asking for tools. <paramref name="runTool"/> executes a tool call and returns
        /// the text result. Returns the final assistant text plus the names of tools used.
        /// </summary>
        public async Task<(string text, List<string> toolsUsed, JsonObject lastToolInput)> RunAsync(
            string model, string system, JsonArray messages, JsonArray tools, int maxTokens,
            Func<string, JsonObject, Task<string>> runTool, CancellationToken ct, string stopOnTool = null, int maxTurns = 10,
            Action<string> onToolCall = null)
        {
            var used = new List<string>();
            for (var turn = 0; turn < maxTurns; turn++)
            {
                var body = new JsonObject
                {
                    ["model"] = model,
                    ["max_tokens"] = maxTokens,
                    ["system"] = system,
                    ["messages"] = messages.DeepClone(),
                };
                if (tools != null && tools.Count > 0) body["tools"] = tools.DeepClone();
                var res = await CreateAsync(body, ct);
                var content = res["content"]!.AsArray();
                messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

                var calls = content.Where(b => b?["type"]?.GetValue<string>() == "tool_use").Select(b => b!.AsObject()).ToList();
                if (calls.Count == 0 || res["stop_reason"]?.GetValue<string>() != "tool_use")
                {
                    var text = string.Join("\n", content.Where(b => b?["type"]?.GetValue<string>() == "text").Select(b => b!["text"]!.GetValue<string>()));
                    return (text.Trim(), used, null);
                }

                var results = new JsonArray();
                foreach (var call in calls)
                {
                    var name = call["name"]!.GetValue<string>();
                    var input = call["input"] as JsonObject ?? new JsonObject();
                    used.Add(name);
                    if (name == stopOnTool) { onToolCall?.Invoke(name); return ("", used, input); }
                    string output;
                    var isError = false;
                    try { output = await runTool(name, input); }
                    catch (AiException ex) { output = "Error: " + ex.Message; isError = true; }
                    results.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = call["id"]!.GetValue<string>(),
                        ["content"] = output.Length > 60000 ? output.Substring(0, 60000) + "\n[truncated]" : output,
                        ["is_error"] = isError,
                    });
                }
                messages.Add(new JsonObject { ["role"] = "user", ["content"] = results });
            }
            throw new AiException("The assistant needed too many steps; try a more specific question.");
        }
    }

    public sealed class AiException : Exception
    {
        public AiException(string message) : base(message) { }
    }
}
