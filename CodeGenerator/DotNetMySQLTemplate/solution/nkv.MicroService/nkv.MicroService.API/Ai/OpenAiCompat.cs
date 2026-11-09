using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.API.Ai
{
    /// <summary>
    /// Translates the Anthropic Messages request/response shape used throughout the app to and from the OpenAI
    /// Chat Completions shape, so any OpenAI-compatible endpoint (NVIDIA NIM / Nemotron, vLLM, Ollama, OpenRouter…)
    /// can be used without touching the callers.
    /// </summary>
    public static class OpenAiCompat
    {
        public static JsonObject ToOpenAi(JsonObject a)
        {
            var msgs = new JsonArray();
            var system = a["system"] switch
            {
                JsonValue v => v.GetValue<string>(),
                JsonArray arr => string.Join("\n", arr.Select(b => b?["text"]?.GetValue<string>()).Where(t => t != null)),
                _ => null,
            };
            if (!string.IsNullOrWhiteSpace(system)) msgs.Add(new JsonObject { ["role"] = "system", ["content"] = system });

            foreach (var m in a["messages"]?.AsArray() ?? new JsonArray())
            {
                var role = m!["role"]!.GetValue<string>();
                var content = m["content"];
                if (content is JsonValue s) { msgs.Add(new JsonObject { ["role"] = role, ["content"] = s.GetValue<string>() }); continue; }
                var blocks = content?.AsArray() ?? new JsonArray();
                if (role == "assistant")
                {
                    var text = string.Join("\n", blocks.Where(b => T(b) == "text").Select(b => b!["text"]!.GetValue<string>()));
                    var calls = new JsonArray(blocks.Where(b => T(b) == "tool_use").Select(b => (JsonNode)new JsonObject
                    {
                        ["id"] = b!["id"]!.GetValue<string>(),
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = b["name"]!.GetValue<string>(), ["arguments"] = (b["input"] ?? new JsonObject()).ToJsonString() },
                    }).ToArray());
                    var am = new JsonObject { ["role"] = "assistant", ["content"] = text.Length > 0 ? text : null };
                    if (calls.Count > 0) am["tool_calls"] = calls;
                    msgs.Add(am);
                    continue;
                }
                // user turn: tool results become "tool" messages, the rest becomes one user message with parts
                foreach (var r in blocks.Where(b => T(b) == "tool_result"))
                {
                    var rc = r!["content"] switch
                    {
                        JsonValue v => v.GetValue<string>(),
                        JsonArray arr => string.Join("\n", arr.Select(b => b?["text"]?.GetValue<string>()).Where(t => t != null)),
                        _ => "",
                    };
                    if (r["is_error"]?.GetValue<bool>() == true && !rc.StartsWith("Error")) rc = "Error: " + rc;
                    msgs.Add(new JsonObject { ["role"] = "tool", ["tool_call_id"] = r["tool_use_id"]!.GetValue<string>(), ["content"] = rc });
                }
                var parts = new JsonArray();
                foreach (var b in blocks)
                {
                    switch (T(b))
                    {
                        case "text": parts.Add(new JsonObject { ["type"] = "text", ["text"] = b!["text"]!.GetValue<string>() }); break;
                        case "image":
                            var src = b!["source"]!;
                            parts.Add(new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject {
                                ["url"] = $"data:{src["media_type"]!.GetValue<string>()};base64,{src["data"]!.GetValue<string>()}" } });
                            break;
                        case "document":
                            // OpenAI-compatible servers generally can't take PDFs; DocumentOcr sends page images instead.
                            parts.Add(new JsonObject { ["type"] = "text", ["text"] = "[a PDF was attached but this model endpoint cannot read PDFs]" });
                            break;
                    }
                }
                if (parts.Count > 0)
                    msgs.Add(new JsonObject { ["role"] = "user", ["content"] = parts.Count == 1 && T(parts[0]) == "text" ? parts[0]!["text"]!.DeepClone() : parts });
            }

            var o = new JsonObject { ["model"] = a["model"]!.DeepClone(), ["messages"] = msgs };
            if (a["max_tokens"] != null) o["max_tokens"] = a["max_tokens"]!.DeepClone();
            if (a["temperature"] != null) o["temperature"] = a["temperature"]!.DeepClone();
            if (a["tools"] is JsonArray tools && tools.Count > 0)
            {
                o["tools"] = new JsonArray(tools.Select(t => (JsonNode)new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = t!["name"]!.GetValue<string>(),
                        ["description"] = t["description"]?.GetValue<string>() ?? "",
                        ["parameters"] = (t["input_schema"] ?? new JsonObject { ["type"] = "object" }).DeepClone(),
                    },
                }).ToArray());
                var tc = a["tool_choice"];
                var kind = tc?["type"]?.GetValue<string>();
                if (kind == "tool") o["tool_choice"] = new JsonObject { ["type"] = "function", ["function"] = new JsonObject { ["name"] = tc!["name"]!.GetValue<string>() } };
                else if (kind == "any") o["tool_choice"] = "required";
                else if (kind == "none") o["tool_choice"] = "none";
            }
            return o;
        }

        /// <param name="forcedTool">When the request forced a tool and the model answered in plain JSON text instead, treat that text as the tool input.</param>
        public static JsonObject FromOpenAi(JsonObject r, string forcedTool)
        {
            var choice = r["choices"]?.AsArray().FirstOrDefault() ?? throw new AiException("The AI endpoint returned no choices.");
            var msg = choice!["message"]!;
            var content = new JsonArray();
            var text = StripThinking(msg["content"]?.GetValue<string>() ?? "");
            var calls = msg["tool_calls"]?.AsArray() ?? new JsonArray();
            if (text.Length > 0 && !(calls.Count == 0 && forcedTool != null && TryJson(text) != null)) content.Add(new JsonObject { ["type"] = "text", ["text"] = text });
            foreach (var c in calls)
            {
                var f = c!["function"]!;
                content.Add(new JsonObject
                {
                    ["type"] = "tool_use",
                    ["id"] = c["id"]?.GetValue<string>() is { Length: > 0 } id ? id : "call_" + Guid.NewGuid().ToString("N")[..12],
                    ["name"] = f["name"]!.GetValue<string>(),
                    ["input"] = TryJson(f["arguments"]?.GetValue<string>()) ?? new JsonObject(),
                });
            }
            if (calls.Count == 0 && forcedTool != null && TryJson(text) is JsonObject asJson)
                content.Add(new JsonObject { ["type"] = "tool_use", ["id"] = "call_" + Guid.NewGuid().ToString("N")[..12], ["name"] = forcedTool, ["input"] = asJson });

            var hasTool = content.Any(b => T(b) == "tool_use");
            var finish = choice["finish_reason"]?.GetValue<string>();
            return new JsonObject
            {
                ["content"] = content,
                ["stop_reason"] = hasTool ? "tool_use" : finish == "length" ? "max_tokens" : "end_turn",
                ["usage"] = new JsonObject
                {
                    ["input_tokens"] = r["usage"]?["prompt_tokens"]?.DeepClone() ?? 0,
                    ["output_tokens"] = r["usage"]?["completion_tokens"]?.DeepClone() ?? 0,
                },
            };
        }

        private static string T(JsonNode b) => b?["type"]?.GetValue<string>();

        /// <summary>Reasoning models (Nemotron, DeepSeek, Qwen…) may put their chain of thought in &lt;think&gt; tags.</summary>
        private static string StripThinking(string s) => Regex.Replace(s, @"<think>[\s\S]*?</think>", "").Trim();

        private static JsonObject TryJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = Regex.Replace(s.Trim(), @"^```(?:json)?\s*|\s*```$", "");
            try { return JsonNode.Parse(t) as JsonObject; } catch (JsonException) { return null; }
        }
    }
}
