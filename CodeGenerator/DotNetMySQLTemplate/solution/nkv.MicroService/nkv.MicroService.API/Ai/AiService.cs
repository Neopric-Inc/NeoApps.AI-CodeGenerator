using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Ai
{
    /// <summary>Documents (upload, index, search), the assistant (Q&amp;A with citations over documents and records) and workflows.</summary>
    public sealed class AiService
    {
        private readonly AiSettings _settings;
        private readonly AiData _data;
        private readonly ClaudeClient _claude;
        private readonly IHttpClientFactory _httpFactory;

        public AiService(AiSettings settings, AiData data, ClaudeClient claude, IHttpClientFactory httpFactory)
        {
            _settings = settings;
            _data = data;
            _claude = claude;
            _httpFactory = httpFactory;
        }

        // ================================================================== files

        public JsonObject Upload(AiUser u, string table, string recordId, string fileName, string contentType, Stream content)
        {
            fileName = Path.GetFileName(fileName ?? "file");
            if (DocumentText.Blocked.Contains(Path.GetExtension(fileName)))
                throw new AiException("Programs and scripts can't be uploaded. Any document, spreadsheet, image, e-mail or archive is fine.");
            using var c = _data.Open();
            RequirePerm(c, u, table, p => p.Update || p.Create, "attach files to");
            if (!DataGuard.RecordVisible(c, table, recordId)) throw new AiException("Record not found.");
            var dir = Path.Combine(_settings.FilesRoot, Safe(u.Tenant), DataGuard.StoragePrefix());
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant());
            using (var fs = File.Create(path)) content.CopyTo(fs);

            var dataTenant = (object)DataGuard.Current?.Tenant ?? DBNull.Value;
            var cmd = AiData.Cmd(c, @"INSERT INTO ai_files (owner_name, table_name, record_id, file_name, content_type, size_bytes, status, storage_path, createdBy, tenant_id)
                                     VALUES (@o, @t, @r, @n, @ct, @s, 'indexing', @p, @by, @tn)",
                ("@o", u.Tenant), ("@t", table), ("@r", recordId), ("@n", fileName), ("@ct", contentType), ("@s", new FileInfo(path).Length), ("@p", path), ("@by", u.Username), ("@tn", dataTenant));
            cmd.ExecuteNonQuery();
            var fileId = (int)cmd.LastInsertedId;
            try
            {
                // Text formats are indexed now; scans and images wait for the background OCR worker (AiFileWorker).
                var result = DocumentText.Read(path, fileName);
                var pages = result.Pages;
                foreach (var (text, i) in pages.Select((t, i) => (t, i)))
                {
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    AiData.Cmd(c, "INSERT INTO ai_chunks (file_id, owner_name, page, text, tenant_id) VALUES (@f, @o, @p, @t, @tn)",
                        ("@f", fileId), ("@o", u.Tenant), ("@p", i + 1), ("@t", text), ("@tn", dataTenant)).ExecuteNonQuery();
                }
                var note = result.Status == "needs_ocr"
                    ? (_settings.HasKey ? "Scanned pages or images: reading them with AI in the background." : "Scanned pages or images: they will be read once AI is configured (ANTHROPIC_API_KEY).")
                    : result.Note;
                AiData.Cmd(c, "UPDATE ai_files SET status=@s, pages=@p, error=@e WHERE file_id=@id",
                    ("@s", result.Status), ("@p", pages.Count), ("@e", note), ("@id", fileId)).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AiData.Cmd(c, "UPDATE ai_files SET status='failed', error=@e WHERE file_id=@id", ("@e", ex.Message), ("@id", fileId)).ExecuteNonQuery();
            }
            return ListFiles(u, table, recordId).OfType<JsonObject>().First(f => f["file_id"]!.GetValue<int>() == fileId);
        }

        public JsonArray ListFiles(AiUser u, string table, string recordId)
        {
            using var c = _data.Open();
            RequirePerm(c, u, table, p => p.Read, "read");
            if (!DataGuard.RecordVisible(c, table, recordId)) throw new AiException("Record not found.");
            return AiData.Rows(AiData.Cmd(c, @"SELECT file_id, file_name, content_type, size_bytes, pages, status, error, createdBy, createdAt
                                              FROM ai_files WHERE owner_name=@o AND table_name=@t AND record_id=@r ORDER BY file_id DESC",
                ("@o", u.Tenant), ("@t", table), ("@r", recordId)));
        }

        public (string path, string name, string type) FileContent(AiUser u, int fileId)
        {
            using var c = _data.Open();
            var f = FileRow(c, u, fileId);
            RequirePerm(c, u, f.table, p => p.Read, "read");
            return (f.path, f.name, f.type ?? "application/octet-stream");
        }

        /// <summary>Short-lived signed link (5 minutes) for a file the caller may read.</summary>
        public JsonObject FileLink(AiUser u, int fileId, string baseUrl)
        {
            FileContent(u, fileId); // authorization
            var exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
            return new JsonObject { ["url"] = $"{baseUrl}?exp={exp}&sig={DataGuard.Sign("ai_file:" + fileId, exp)}", ["expires"] = exp };
        }

        /// <summary>File behind a valid signed link (no login needed until it expires).</summary>
        public (string path, string name, string type) SignedFileContent(int fileId, long exp, string sig)
        {
            if (!DataGuard.VerifySignature("ai_file:" + fileId, exp, sig)) throw new AiException("This link has expired or is invalid.");
            using var c = _data.Open();
            using var r = AiData.Cmd(c, "SELECT storage_path, file_name, content_type FROM ai_files WHERE file_id=@id", ("@id", fileId)).ExecuteReader();
            if (!r.Read()) throw new AiException("File not found.");
            return (r.GetString(0), r.GetString(1), r.IsDBNull(2) ? "application/octet-stream" : r.GetString(2));
        }

        public void DeleteFile(AiUser u, int fileId)
        {
            using var c = _data.Open();
            var f = FileRow(c, u, fileId);
            RequirePerm(c, u, f.table, p => p.Update || p.Delete, "remove files from");
            AiData.Cmd(c, "DELETE FROM ai_chunks WHERE file_id=@id", ("@id", fileId)).ExecuteNonQuery();
            AiData.Cmd(c, "DELETE FROM ai_files WHERE file_id=@id", ("@id", fileId)).ExecuteNonQuery();
            try { File.Delete(f.path); } catch { /* already gone */ }
        }

        private static (string table, string record, string path, string name, string type) FileRow(MySqlConnection c, AiUser u, int fileId)
        {
            using var r = AiData.Cmd(c, "SELECT table_name, record_id, storage_path, file_name, content_type FROM ai_files WHERE file_id=@id AND owner_name=@o",
                ("@id", fileId), ("@o", u.Tenant)).ExecuteReader();
            if (!r.Read()) throw new AiException("File not found.");
            return (r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4));
        }

        private void RequirePerm(MySqlConnection c, AiUser u, string table, Func<TablePerm, bool> ok, string verb)
        {
            if (!_data.Permissions(c, u).TryGetValue(table ?? "", out var p) || !ok(p))
                throw new AiException($"You don't have permission to {verb} {table}.");
        }

        private static string Safe(string s) => Regex.Replace(s ?? "tenant", "[^A-Za-z0-9_-]", "_");

        // ================================================================== document search

        private sealed class Source
        {
            public string Id;
            public int FileId;
            public string FileName;
            public int Page;
            public string Table;
            public string RecordId;
            public string RecordLabel;
            public string Text;
        }

        private List<Source> SearchDocuments(MySqlConnection c, AiUser u, string query, string table, string recordId, int limit, Dictionary<string, Source> registry)
        {
            var readable = _data.Permissions(c, u).Where(p => p.Value.Read).Select(p => p.Key).ToList();
            if (readable.Count == 0) return new List<Source>();
            var tableFilter = string.Join(",", readable.Select((_, i) => "@rt" + i));
            var ps = new List<(string, object)> { ("@o", u.Tenant), ("@q", query ?? "") };
            ps.AddRange(readable.Select((t, i) => ("@rt" + i, (object)t)));
            var extra = "";
            if (!string.IsNullOrWhiteSpace(table)) { extra += " AND f.table_name=@t"; ps.Add(("@t", table.ToLowerInvariant())); }
            if (!string.IsNullOrWhiteSpace(recordId)) { extra += " AND f.record_id=@r"; ps.Add(("@r", recordId)); }

            var rows = new List<(int fileId, string name, int page, string table, string record, string text)>();
            void Read(MySqlCommand cmd)
            {
                using var r = cmd.ExecuteReader();
                while (r.Read()) rows.Add((r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetString(3), r.GetString(4), r.GetString(5)));
            }
            Read(AiData.Cmd(c, $@"SELECT k.file_id, f.file_name, k.page, f.table_name, f.record_id, k.text FROM ai_chunks k JOIN ai_files f ON f.file_id=k.file_id
                                  WHERE k.owner_name=@o AND f.table_name IN ({tableFilter}){extra} AND MATCH(k.text) AGAINST (@q IN NATURAL LANGUAGE MODE)
                                  ORDER BY MATCH(k.text) AGAINST (@q IN NATURAL LANGUAGE MODE) DESC LIMIT {Math.Clamp(limit, 1, 12)}", ps.ToArray()));
            if (rows.Count == 0)
            {
                // Short or very common terms are ignored by FULLTEXT; fall back to a substring match on the longest word.
                var word = Regex.Matches(query ?? "", @"[\p{L}\p{N}][\p{L}\p{N}\-]{2,}").Select(m => m.Value).OrderByDescending(w => w.Length).FirstOrDefault();
                if (word != null)
                {
                    ps.Add(("@w", word));
                    Read(AiData.Cmd(c, $@"SELECT k.file_id, f.file_name, k.page, f.table_name, f.record_id, k.text FROM ai_chunks k JOIN ai_files f ON f.file_id=k.file_id
                                          WHERE k.owner_name=@o AND f.table_name IN ({tableFilter}){extra} AND k.text LIKE CONCAT('%', @w, '%') LIMIT {Math.Clamp(limit, 1, 12)}", ps.ToArray()));
                }
            }
            var result = new List<Source>();
            foreach (var row in rows)
            {
                var key = $"{row.fileId}:{row.page}";
                var existing = registry.Values.FirstOrDefault(s => $"{s.FileId}:{s.Page}" == key);
                if (existing == null)
                {
                    existing = new Source
                    {
                        Id = "S" + (registry.Count + 1), FileId = row.fileId, FileName = row.name, Page = row.page, Table = row.table, RecordId = row.record,
                        RecordLabel = _data.RecordLabel(c, u, row.table, row.record), Text = row.text,
                    };
                    registry[existing.Id] = existing;
                }
                result.Add(existing);
            }
            return result;
        }

        // ================================================================== tools

        private static JsonObject Tool(string name, string description, JsonObject properties, params string[] required) => new()
        {
            ["name"] = name,
            ["description"] = description,
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new JsonArray(required.Select(r => (JsonNode)JsonValue.Create(r)).ToArray()),
            },
        };

        private static JsonObject Str(string d) => new() { ["type"] = "string", ["description"] = d };
        private static JsonObject Int(string d) => new() { ["type"] = "integer", ["description"] = d };

        private static readonly JsonObject FiltersSchema = new()
        {
            ["type"] = "array",
            ["description"] = "Conditions, all must match",
            ["items"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["column"] = Str("column name"),
                    ["op"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("eq", "ne", "gt", "gte", "lt", "lte", "contains", "is_null", "not_null") },
                    ["value"] = new JsonObject { ["description"] = "value to compare with (string or number)" },
                },
                ["required"] = new JsonArray("column", "op"),
            },
        };

        private static List<string> EnabledTools(JsonObject cfg) =>
            (cfg["tools"] as JsonArray)?.Select(t => t!.GetValue<string>()).ToList() ?? new List<string> { "documents", "records" };

        private JsonArray Tools(List<string> enabled)
        {
            var tools = new JsonArray();
            if (enabled.Contains("documents"))
                tools.Add(Tool("search_documents", "Full-text search over uploaded documents (protocols, SOPs, contracts...). Returns passages with source ids like S1 to cite.",
                    new JsonObject
                    {
                        ["query"] = Str("keywords or a question; use distinctive terms"),
                        ["table"] = Str("optional: only documents attached to records of this table"),
                        ["record_id"] = Str("optional: only documents attached to this record"),
                        ["limit"] = Int("max passages (default 6)"),
                    }, "query"));
            if (enabled.Contains("records"))
            {
                tools.Add(Tool("list_tables", "Lists the app's tables and columns you may read.", new JsonObject()));
                tools.Add(Tool("query_records", "Reads records from a table with optional filters, sorting and column selection.",
                    new JsonObject
                    {
                        ["table"] = Str("table name"),
                        ["filters"] = FiltersSchema.DeepClone(),
                        ["columns"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
                        ["order_by"] = Str("column to sort by"),
                        ["descending"] = new JsonObject { ["type"] = "boolean" },
                        ["limit"] = Int("max rows (default 25, max 100)"),
                    }, "table"));
                tools.Add(Tool("aggregate_records", "Counts or sums records, optionally grouped by a column.",
                    new JsonObject
                    {
                        ["table"] = Str("table name"),
                        ["metric"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("count", "sum", "avg", "min", "max") },
                        ["column"] = Str("column for sum/avg/min/max"),
                        ["group_by"] = Str("optional column to group by"),
                        ["filters"] = FiltersSchema.DeepClone(),
                    }, "table", "metric"));
            }
            if (enabled.Contains("clinicaltrials_gov"))
                tools.Add(Tool("search_clinical_trials", "Searches ClinicalTrials.gov for studies (default: recruiting).",
                    new JsonObject
                    {
                        ["condition"] = Str("condition or disease, e.g. 'atopic dermatitis'"),
                        ["terms"] = Str("optional extra keywords, e.g. drug or intervention"),
                        ["location"] = Str("optional country/state/city"),
                        ["status"] = Str("RECRUITING (default), NOT_YET_RECRUITING, ACTIVE_NOT_RECRUITING, COMPLETED"),
                        ["limit"] = Int("max studies (default 10, max 25)"),
                    }, "condition"));
            return tools;
        }

        private async Task<string> RunTool(string name, JsonObject input, AiUser u, Dictionary<string, Source> sources, CancellationToken ct)
        {
            using var c = _data.Open();
            switch (name)
            {
                case "search_documents":
                {
                    var found = SearchDocuments(c, u, input["query"]?.GetValue<string>(), input["table"]?.GetValue<string>(),
                        input["record_id"]?.ToString(), input["limit"]?.GetValue<int>() ?? 6, sources);
                    if (found.Count == 0) return "No matching passages in the uploaded documents.";
                    var sb = new StringBuilder();
                    foreach (var s in found)
                        sb.AppendLine($"[{s.Id}] {s.FileName}, page {s.Page} (attached to {s.Table} '{s.RecordLabel}'):\n{Trim(s.Text, 2500)}\n");
                    return sb.ToString();
                }
                case "list_tables": return _data.ListTables(c, u);
                case "query_records": return _data.QueryRecords(c, u, input);
                case "aggregate_records": return _data.Aggregate(c, u, input);
                case "search_clinical_trials": return await SearchTrials(input, ct);
                default: throw new AiException($"Unknown tool {name}");
            }
        }

        private async Task<string> SearchTrials(JsonObject input, CancellationToken ct)
        {
            var q = new List<string>
            {
                "query.cond=" + Uri.EscapeDataString(input["condition"]?.GetValue<string>() ?? ""),
                "filter.overallStatus=" + Uri.EscapeDataString(input["status"]?.GetValue<string>() ?? "RECRUITING"),
                "pageSize=" + Math.Clamp(input["limit"]?.GetValue<int>() ?? 10, 1, 25),
                "fields=NCTId,BriefTitle,OverallStatus,Phase,LeadSponsorName,Condition,EnrollmentCount,StartDate,LocationCountry,InterventionName",
            };
            if (input["terms"]?.GetValue<string>() is { Length: > 0 } terms) q.Add("query.term=" + Uri.EscapeDataString(terms));
            if (input["location"]?.GetValue<string>() is { Length: > 0 } loc) q.Add("query.locn=" + Uri.EscapeDataString(loc));
            var http = _httpFactory.CreateClient("clinicaltrials");
            http.Timeout = TimeSpan.FromSeconds(30);
            string json;
            try { json = await http.GetStringAsync("https://clinicaltrials.gov/api/v2/studies?" + string.Join("&", q), ct); }
            catch (Exception ex) { throw new AiException("ClinicalTrials.gov is unreachable: " + ex.Message); }
            var studies = new JsonArray();
            foreach (var s in JsonNode.Parse(json)?["studies"]?.AsArray() ?? new JsonArray())
            {
                var p = s?["protocolSection"];
                studies.Add(new JsonObject
                {
                    ["nct_number"] = p?["identificationModule"]?["nctId"]?.DeepClone(),
                    ["title"] = p?["identificationModule"]?["briefTitle"]?.DeepClone(),
                    ["status"] = p?["statusModule"]?["overallStatus"]?.DeepClone(),
                    ["start_date"] = p?["statusModule"]?["startDateStruct"]?["date"]?.DeepClone(),
                    ["phases"] = p?["designModule"]?["phases"]?.DeepClone(),
                    ["enrollment"] = p?["designModule"]?["enrollmentInfo"]?["count"]?.DeepClone(),
                    ["sponsor"] = p?["sponsorCollaboratorsModule"]?["leadSponsor"]?["name"]?.DeepClone(),
                    ["conditions"] = p?["conditionsModule"]?["conditions"]?.DeepClone(),
                    ["interventions"] = new JsonArray((p?["armsInterventionsModule"]?["interventions"]?.AsArray() ?? new JsonArray())
                        .Select(i => (JsonNode)JsonValue.Create(i?["name"]?.GetValue<string>())).Take(5).ToArray()),
                    ["countries"] = new JsonArray((p?["contactsLocationsModule"]?["locations"]?.AsArray() ?? new JsonArray())
                        .Select(l => l?["country"]?.GetValue<string>()).Where(x => x != null).Distinct().Take(8).Select(x => (JsonNode)JsonValue.Create(x)).ToArray()),
                });
            }
            return new JsonObject { ["count"] = studies.Count, ["studies"] = studies }.ToJsonString();
        }

        private static string Trim(string s, int n) => s == null ? "" : s.Length <= n ? s : s.Substring(0, n) + "…";

        // ================================================================== assistant

        public async Task<JsonObject> Ask(AiUser u, int? conversationId, string question, string contextTable, string contextRecord, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(question)) throw new AiException("Ask a question.");
            var cfg = _settings.Config(u.Tenant);
            RequireAi(u, cfg);
            using var c = _data.Open();

            int convId;
            if (conversationId is int existing)
            {
                var owner = AiData.Cmd(c, "SELECT COUNT(*) FROM ai_conversations WHERE conversation_id=@id AND owner_name=@o AND user_id=@u",
                    ("@id", existing), ("@o", u.Tenant), ("@u", u.UserId)).ExecuteScalar();
                if (Convert.ToInt64(owner) == 0) throw new AiException("Conversation not found.");
                convId = existing;
            }
            else
            {
                var ins = AiData.Cmd(c, @"INSERT INTO ai_conversations (owner_name, user_id, title, context_table, context_record) VALUES (@o, @u, @t, @ct, @cr)",
                    ("@o", u.Tenant), ("@u", u.UserId), ("@t", Trim(question, 120)), ("@ct", contextTable), ("@cr", contextRecord));
                ins.ExecuteNonQuery();
                convId = (int)ins.LastInsertedId;
            }

            // Recent history (text only) for follow-up questions.
            var messages = new JsonArray();
            foreach (var m in AiData.Rows(AiData.Cmd(c, @"SELECT role, content FROM (SELECT message_id, role, content FROM ai_messages WHERE conversation_id=@id
                                                         ORDER BY message_id DESC LIMIT 10) x ORDER BY message_id", ("@id", convId))))
                messages.Add(new JsonObject { ["role"] = m!["role"]!.GetValue<string>(), ["content"] = m["content"]!.GetValue<string>() });
            var contextNote = contextTable != null && contextRecord != null
                ? $"\n\n(The user is looking at {contextTable} record {contextRecord}: \"{_data.RecordLabel(c, u, contextTable, contextRecord)}\". Prefer documents attached to it when relevant.)"
                : "";
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = question + contextNote });

            var perms = _data.Permissions(c, u);
            var readable = perms.Where(p => p.Value.Read).Select(p => p.Key).ToList();
            var appName = cfg["__appName"]?.GetValue<string>() ?? "this app";
            var system = $@"You are {cfg["assistantName"]?.GetValue<string>() ?? "the assistant"} inside {appName}{(cfg["__tagline"] != null ? $" ({cfg["__tagline"]})" : "")}.
Today is {DateTime.UtcNow:yyyy-MM-dd}. The user is {u.Username}. They can read these tables: {string.Join(", ", readable)}.
{cfg["instructions"]?.GetValue<string>()}

How to answer:
- Use the tools. Search the uploaded documents for anything that documents would contain (procedures, protocol rules, eligibility, budgets, contracts). Query records for questions about the app's data (counts, lists, status, dates).
- Cite every fact taken from a document with its source id in square brackets right after the sentence, e.g. ""Washout is 14 days [S2]."" Only cite ids returned by search_documents.
- If the documents don't contain the answer, say so plainly and don't guess. Say which documents or records would be needed.
- When answering from records, mention the key values (names, numbers, dates) you used.
- Be concise: short paragraphs or bullet lists. Use plain text with simple markdown (bold, bullets). No tables wider than 5 columns.";

            var sources = new Dictionary<string, Source>();
            var enabled = EnabledTools(cfg);
            var (answer, toolsUsed, _) = await _claude.RunAsync(_settings.Model(cfg), system, messages, Tools(enabled), 3000,
                (name, input) => RunTool(name, input, u, sources, ct), ct);

            var cited = Regex.Matches(answer, @"\[(S\d+)\]").Select(m => m.Groups[1].Value).Distinct().Where(sources.ContainsKey).ToList();
            var citations = new JsonArray(cited.Select(id =>
            {
                var s = sources[id];
                return (JsonNode)new JsonObject
                {
                    ["id"] = id, ["file_id"] = s.FileId, ["file_name"] = s.FileName, ["page"] = s.Page,
                    ["table"] = s.Table, ["record_id"] = s.RecordId, ["record_label"] = s.RecordLabel, ["excerpt"] = Trim(s.Text, 600),
                };
            }).ToArray());
            var toolsJson = new JsonArray(toolsUsed.Distinct().Select(t => (JsonNode)JsonValue.Create(t)).ToArray());

            AiData.Cmd(c, "INSERT INTO ai_messages (conversation_id, role, content) VALUES (@c, 'user', @t)", ("@c", convId), ("@t", question)).ExecuteNonQuery();
            AiData.Cmd(c, "INSERT INTO ai_messages (conversation_id, role, content, citations, tools) VALUES (@c, 'assistant', @t, @ci, @to)",
                ("@c", convId), ("@t", answer), ("@ci", citations.ToJsonString()), ("@to", toolsJson.ToJsonString())).ExecuteNonQuery();
            AiData.Cmd(c, "UPDATE ai_conversations SET modifiedAt=NOW() WHERE conversation_id=@c", ("@c", convId)).ExecuteNonQuery();

            return new JsonObject
            {
                ["conversation_id"] = convId,
                ["answer"] = answer,
                ["citations"] = citations,
                ["tools"] = toolsJson,
            };
        }

        public JsonArray Conversations(AiUser u)
        {
            using var c = _data.Open();
            return AiData.Rows(AiData.Cmd(c, @"SELECT conversation_id, title, context_table, context_record, modifiedAt FROM ai_conversations
                                              WHERE owner_name=@o AND user_id=@u ORDER BY modifiedAt DESC LIMIT 50", ("@o", u.Tenant), ("@u", u.UserId)));
        }

        public JsonArray Messages(AiUser u, int conversationId)
        {
            using var c = _data.Open();
            var rows = AiData.Rows(AiData.Cmd(c, @"SELECT m.message_id, m.role, m.content, m.citations, m.tools, m.createdAt FROM ai_messages m
                                                  JOIN ai_conversations v ON v.conversation_id=m.conversation_id
                                                  WHERE m.conversation_id=@id AND v.owner_name=@o AND v.user_id=@u ORDER BY m.message_id",
                ("@id", conversationId), ("@o", u.Tenant), ("@u", u.UserId)));
            foreach (var r in rows.OfType<JsonObject>())
                foreach (var k in new[] { "citations", "tools" })
                    r[k] = r[k] is JsonValue v && v.ToString() is { Length: > 0 } s ? JsonNode.Parse(s) : new JsonArray();
            return rows;
        }

        public void DeleteConversation(AiUser u, int conversationId)
        {
            using var c = _data.Open();
            var n = AiData.Cmd(c, "DELETE FROM ai_conversations WHERE conversation_id=@id AND owner_name=@o AND user_id=@u",
                ("@id", conversationId), ("@o", u.Tenant), ("@u", u.UserId)).ExecuteNonQuery();
            if (n > 0) AiData.Cmd(c, "DELETE FROM ai_messages WHERE conversation_id=@id", ("@id", conversationId)).ExecuteNonQuery();
        }

        // ================================================================== workflows

        /// <summary>
        /// ai.roles limits the assistant and workflows to the listed role names (Administrator always allowed).
        /// Without it every signed-in user may use AI.
        /// </summary>
        private bool RoleAllowed(AiUser u, JsonObject cfg)
        {
            if (cfg["roles"] is not JsonArray roles || roles.Count == 0) return true;
            if (u.RoleId == 1) return true;
            using var c = _data.Open();
            using var cmd = new MySqlCommand("SELECT role_name FROM roles WHERE role_id = @id", c);
            cmd.Parameters.AddWithValue("@id", u.RoleId);
            var name = cmd.ExecuteScalar() as string;
            return name != null && roles.Any(r => string.Equals(r?.GetValue<string>(), name, StringComparison.OrdinalIgnoreCase));
        }

        private void RequireAi(AiUser u, JsonObject cfg)
        {
            if (!RoleAllowed(u, cfg)) throw new AiException("AI tools are not available for your role.");
            if (!_settings.Enabled(cfg)) throw new AiException("AI is not configured. Set ANTHROPIC_API_KEY, or OPENAI_API_KEY + OPENAI_BASE_URL, for the API.");
        }

        public JsonObject Status(AiUser u)
        {
            var cfg = _settings.Config(u.Tenant);
            return new JsonObject
            {
                ["enabled"] = _settings.Enabled(cfg) && RoleAllowed(u, cfg),
                ["files"] = true,
                ["configured"] = _settings.HasKey,
                ["model"] = _settings.Model(cfg),
                ["assistantName"] = cfg["assistantName"]?.DeepClone() ?? "Assistant",
                ["documentTables"] = cfg["documentTables"]?.DeepClone() ?? new JsonArray(),
                ["suggestions"] = cfg["suggestions"]?.DeepClone() ?? new JsonArray(),
                ["workflows"] = new JsonArray(((cfg["workflows"] as JsonArray) ?? new JsonArray()).Select(w => (JsonNode)new JsonObject
                {
                    ["id"] = w?["id"]?.DeepClone(), ["label"] = w?["label"]?.DeepClone(), ["description"] = w?["description"]?.DeepClone(),
                    ["table"] = w?["table"]?.DeepClone(), ["when"] = w?["when"]?.DeepClone(), ["targets"] = w?["targets"]?.DeepClone(),
                }).ToArray()),
            };
        }

        private JsonObject Workflow(JsonObject cfg, string id) =>
            ((cfg["workflows"] as JsonArray) ?? new JsonArray()).OfType<JsonObject>().FirstOrDefault(w => w["id"]?.GetValue<string>() == id)
            ?? throw new AiException($"Unknown workflow '{id}'.");

        /// <summary>The workflow's target tables, as named in information_schema (unknown names are dropped).</summary>
        private List<string> Targets(MySqlConnection c, JsonObject wf)
        {
            var wanted = (wf["targets"] as JsonArray ?? new JsonArray()).Select(t => t!.GetValue<string>().ToLowerInvariant()).ToHashSet();
            return _data.AppTables(c).Where(wanted.Contains).ToList();
        }

        /// <summary>
        /// Quick checks before a workflow runs in the background (AI configured, workflow exists, create permission on its
        /// targets, a record given when it runs on one). Returns the workflow's label.
        /// </summary>
        public string CheckWorkflow(AiUser u, string workflowId, string recordId)
        {
            var cfg = _settings.Config(u.Tenant);
            RequireAi(u, cfg);
            var wf = Workflow(cfg, workflowId);
            using var c = _data.Open();
            var perms = _data.Permissions(c, u);
            var denied = Targets(c, wf).Where(t => !perms.TryGetValue(t, out var p) || !p.Create).ToList();
            if (denied.Count > 0) throw new AiException($"You need create permission on: {string.Join(", ", denied)}.");
            if (wf["table"] != null && string.IsNullOrEmpty(recordId)) throw new AiException("This workflow runs on a record.");
            return wf["label"]?.GetValue<string>() ?? workflowId;
        }

        /// <summary>What a tool call is doing, in words for the job's live feed.</summary>
        private static string DescribeTool(string name, JsonObject input) => name switch
        {
            "search_documents" => $"Searching documents for \"{Trim(input["query"]?.ToString() ?? "", 80)}\"",
            "list_tables" => "Looking at what the app tracks",
            "query_records" => $"Reading {input["table"]?.ToString() ?? "records"}",
            "aggregate_records" => $"Summarising {input["table"]?.ToString() ?? "records"}",
            "search_clinical_trials" => "Searching ClinicalTrials.gov",
            "propose_records" => "Drafting the proposal",
            _ => "Using " + name,
        };

        /// <summary>Asks the model to propose records for the workflow's target tables. Nothing is saved.</summary>
        public async Task<JsonObject> PreviewWorkflow(AiUser u, string workflowId, string recordId, string note, CancellationToken ct, Action<string> progress = null)
        {
            var cfg = _settings.Config(u.Tenant);
            RequireAi(u, cfg);
            var wf = Workflow(cfg, workflowId);
            using var c = _data.Open();
            var perms = _data.Permissions(c, u);
            var targets = Targets(c, wf);
            var denied = targets.Where(t => !perms.TryGetValue(t, out var p) || !p.Create).ToList();
            if (denied.Count > 0) throw new AiException($"You need create permission on: {string.Join(", ", denied)}.");

            var sb = new StringBuilder();
            var sourceTable = wf["table"]?.GetValue<string>();
            JsonObject sourceRecord = null;
            if (sourceTable != null)
            {
                if (string.IsNullOrEmpty(recordId)) throw new AiException("This workflow runs on a record.");
                var res = JsonNode.Parse(_data.QueryRecords(c, u, new JsonObject
                {
                    ["table"] = sourceTable,
                    ["filters"] = new JsonArray(new JsonObject { ["column"] = _data.Columns(c, sourceTable).First(x => x.IsPk).Name, ["op"] = "eq", ["value"] = recordId }),
                    ["limit"] = 1,
                }))!;
                sourceRecord = res["rows"]?.AsArray().FirstOrDefault() as JsonObject ?? throw new AiException("Record not found.");
                sb.AppendLine($"SOURCE RECORD ({sourceTable}): {sourceRecord.ToJsonString()}");
                var text = new StringBuilder();
                using (var r = AiData.Cmd(c, @"SELECT f.file_name, k.page, k.text FROM ai_chunks k JOIN ai_files f ON f.file_id=k.file_id
                                              WHERE f.owner_name=@o AND f.table_name=@t AND f.record_id=@r ORDER BY f.file_id, k.page",
                    ("@o", u.Tenant), ("@t", sourceTable), ("@r", recordId)).ExecuteReader())
                    while (r.Read() && text.Length < 180000) text.AppendLine($"--- {r.GetString(0)} page {r.GetInt32(1)} ---\n{r.GetString(2)}");
                if (text.Length == 0 && wf["requiresDocument"]?.GetValue<bool>() != false)
                    throw new AiException("Upload the document to this record first (Files section), then run the workflow.");
                progress?.Invoke(text.Length == 0 ? $"Read the {sourceTable} record" : $"Read the {sourceTable} record and its documents ({text.Length / 1000}k characters)");
                sb.AppendLine("ATTACHED DOCUMENT TEXT:\n" + text);
            }

            // Values copied from the source record into every new row, e.g. {"study_id": "study_id"}.
            var inherit = new Dictionary<string, object>();
            foreach (var (target, src) in (wf["inherit"] as JsonObject) ?? new JsonObject())
                if (sourceRecord?[src!.GetValue<string>()] is JsonNode v) inherit[target] = v.ToString();

            sb.AppendLine("\nTARGET TABLES (column type; -> foreign key; required):\n" + _data.DescribeTables(c, targets));
            if (inherit.Count > 0) sb.AppendLine("These values are filled in automatically, don't set them: " + string.Join(", ", inherit.Select(kv => $"{kv.Key}={kv.Value}")));
            if (!string.IsNullOrWhiteSpace(note)) sb.AppendLine("\nNOTE FROM THE USER: " + note);

            var propose = Tool("propose_records", "Return the records to create. Use ref ids to link new rows: put \"@<ref>\" as the value of a foreign key to point at another new row.",
                new JsonObject
                {
                    ["summary"] = Str("one or two sentences describing what you found and what will be created"),
                    ["records"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["ref"] = Str("unique id for this new row, e.g. v1"),
                                ["table"] = Str("target table"),
                                ["values"] = new JsonObject { ["type"] = "object", ["description"] = "column -> value; no primary keys or audit columns" },
                            },
                            ["required"] = new JsonArray("ref", "table", "values"),
                        },
                    },
                }, "summary", "records");
            var tools = Tools((wf["tools"] as JsonArray)?.Select(t => t!.GetValue<string>()).ToList() ?? new List<string>());
            tools.Add(propose);
            var system = $@"You run the ""{wf["label"]}"" workflow in {cfg["__appName"]?.GetValue<string>() ?? "the app"}. Today is {DateTime.UtcNow:yyyy-MM-dd}.
{wf["instructions"]?.GetValue<string>()}
Use only the tables listed as targets. Fill required columns. Use values that exist in the source; don't invent facts. Dates as YYYY-MM-DD.
When done, call propose_records exactly once with all rows.";
            var sources = new Dictionary<string, Source>();
            progress?.Invoke($"Asking {_settings.Model(cfg)} to work through: {string.Join(", ", targets)}");
            var (text2, _, proposal) = await _claude.RunAsync(_settings.Model(cfg), system,
                new JsonArray(new JsonObject { ["role"] = "user", ["content"] = sb.ToString() }), tools, 16000,
                (name, input) => { progress?.Invoke(DescribeTool(name, input)); return RunTool(name, input, u, sources, ct); },
                ct, stopOnTool: "propose_records", maxTurns: 12,
                onToolCall: name => { if (name == "propose_records") progress?.Invoke(DescribeTool(name, new JsonObject())); });
            if (proposal == null) throw new AiException("The assistant didn't propose any records: " + Trim(text2, 300));
            if (proposal["records"] is JsonValue rv && rv.TryGetValue<string>(out var rs))
                try { proposal["records"] = JsonNode.Parse(rs) as JsonArray; } catch (System.Text.Json.JsonException) { }
            progress?.Invoke($"Checking {(proposal["records"] as JsonArray)?.Count ?? 0} proposed rows against the tables");

            var cleaned = ValidateProposal(c, targets, proposal["records"] as JsonArray ?? new JsonArray(), inherit, out var warnings);
            return new JsonObject
            {
                ["workflow"] = workflowId,
                ["record_id"] = recordId,
                ["summary"] = proposal["summary"]?.DeepClone(),
                ["records"] = cleaned,
                ["warnings"] = new JsonArray(warnings.Select(w => (JsonNode)JsonValue.Create(w)).ToArray()),
            };
        }

        private JsonArray ValidateProposal(MySqlConnection c, List<string> targets, JsonArray records, Dictionary<string, object> inherit, out List<string> warnings)
        {
            warnings = new List<string>();
            var cols = targets.ToDictionary(t => t, t => _data.Columns(c, t), StringComparer.OrdinalIgnoreCase);
            var refs = new HashSet<string>();
            var output = new JsonArray();
            foreach (var r in records.OfType<JsonObject>())
            {
                var requested = r["table"]?.GetValue<string>()?.ToLowerInvariant();
                var table = targets.FirstOrDefault(t => t == requested);
                if (table == null) { warnings.Add($"Skipped a row for table '{requested}', which isn't a target of this workflow."); continue; }
                var values = new JsonObject();
                // some models send the values object as a JSON string; accept both
                var given = r["values"] as JsonObject ?? (r["values"] is JsonValue sv && sv.TryGetValue<string>(out var vs) ? TryParseObject(vs) : null);
                if (given == null) warnings.Add($"A {table} row came without readable values.");
                var unknown = new List<string>();
                foreach (var (k, v) in given ?? new JsonObject())
                {
                    var col = cols[table].FirstOrDefault(x => string.Equals(x.Name, k, StringComparison.OrdinalIgnoreCase));
                    if (col == null) { unknown.Add(k); continue; }
                    if (col.IsPk || col.AutoIncrement || new[] { "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive" }.Contains(col.Name)) continue;
                    values[col.Name] = v?.DeepClone();
                }
                if (unknown.Count > 0) warnings.Add($"Ignored {table} fields that aren't columns: {string.Join(", ", unknown)}.");
                if (values.Count == 0) warnings.Add($"A {table} row has no content of its own; re-run the workflow.");
                foreach (var (k, v) in inherit)
                    if (cols[table].FirstOrDefault(x => x.Name == k) is { } ic) values[ic.Name] = JsonValue.Create(v.ToString());
                var refId = r["ref"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N")[..6];
                refs.Add(refId);
                output.Add(new JsonObject { ["ref"] = refId, ["table"] = table, ["values"] = values });
            }
            foreach (var row in output.OfType<JsonObject>())
                foreach (var (k, v) in row["values"]!.AsObject())
                    if (v is JsonValue jv && jv.ToString().StartsWith("@") && !refs.Contains(jv.ToString()[1..]))
                        warnings.Add($"{row["table"]}.{k} points at unknown row {jv}.");
            return output;
        }

        private static JsonObject TryParseObject(string s)
        {
            try { return JsonNode.Parse(s) as JsonObject; } catch (System.Text.Json.JsonException) { return null; }
        }

        /// <summary>Creates the (possibly edited) proposed rows in dependency order. All-or-nothing.</summary>
        public JsonObject ApplyWorkflow(AiUser u, string workflowId, string recordId, JsonArray records)
        {
            var cfg = _settings.Config(u.Tenant);
            var wf = Workflow(cfg, workflowId);
            using var c = _data.Open();
            var perms = _data.Permissions(c, u);
            var targets = Targets(c, wf);
            var cleaned = ValidateProposal(c, targets, records ?? new JsonArray(), new Dictionary<string, object>(), out _).OfType<JsonObject>().ToList();
            if (cleaned.Count == 0) throw new AiException("Nothing to create: none of the rows belong to this workflow's tables.");
            foreach (var t in cleaned.Select(r => r["table"]!.GetValue<string>()).Distinct())
                if (!perms.TryGetValue(t, out var p) || !p.Create) throw new AiException($"You need create permission on {t}.");

            var ids = new Dictionary<string, long>();
            var created = new Dictionary<string, int>();
            using var tx = c.BeginTransaction();
            var pending = cleaned.ToList();
            while (pending.Count > 0)
            {
                var ready = pending.FirstOrDefault(r => r["values"]!.AsObject().All(kv => !(kv.Value is JsonValue v && v.ToString().StartsWith("@")) || ids.ContainsKey(kv.Value!.ToString()[1..])))
                    ?? throw new AiException("Rows reference each other in a cycle or point at a missing row.");
                pending.Remove(ready);
                var table = ready["table"]!.GetValue<string>();
                var values = ready["values"]!.AsObject();
                var names = values.Select(kv => kv.Key).ToList();
                var cmd = AiData.Cmd(c, $"INSERT INTO `{table}` ({string.Join(", ", names.Select(n => "`" + n + "`"))}{(names.Count > 0 ? ", " : "")}createdBy, modifiedBy, isActive) " +
                                        $"VALUES ({string.Join(", ", names.Select((_, i) => "@v" + i))}{(names.Count > 0 ? ", " : "")}@o, @o, 1)", ("@o", u.Tenant));
                for (var i = 0; i < names.Count; i++)
                {
                    var v = values[names[i]];
                    object val = v is JsonValue jv && jv.ToString().StartsWith("@") ? ids[jv.ToString()[1..]]
                        : v is JsonValue j2 && j2.TryGetValue<bool>(out var b) ? (b ? 1 : 0)
                        : v == null ? null : v is JsonValue j3 ? j3.ToString() : v.ToJsonString();
                    cmd.Parameters.AddWithValue("@v" + i, val ?? DBNull.Value);
                }
                cmd.Transaction = tx;
                try { DataGuard.NonQuery(cmd); }
                catch (MySqlException ex) { throw new AiException($"Couldn't create a {table} row: {ex.Message}"); }
                ids[ready["ref"]!.GetValue<string>()] = cmd.LastInsertedId;
                created[table] = created.GetValueOrDefault(table) + 1;
            }
            var summary = string.Join(", ", created.Select(kv => $"{kv.Value} {kv.Key}"));
            var log = AiData.Cmd(c, @"INSERT INTO ai_runs (owner_name, user_id, workflow, table_name, record_id, status, summary, created_records)
                                     VALUES (@o, @u, @w, @t, @r, 'applied', @s, @n)",
                ("@o", u.Tenant), ("@u", u.UserId), ("@w", workflowId), ("@t", wf["table"]?.GetValue<string>()), ("@r", recordId), ("@s", summary), ("@n", ids.Count));
            log.Transaction = tx;
            log.ExecuteNonQuery();
            tx.Commit();
            return new JsonObject
            {
                ["created"] = new JsonObject(created.Select(kv => new KeyValuePair<string, JsonNode>(kv.Key, kv.Value))),
                ["summary"] = $"Created {summary}.",
            };
        }
    }
}
