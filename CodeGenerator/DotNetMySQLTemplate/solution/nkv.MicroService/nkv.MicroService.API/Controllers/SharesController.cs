using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Share links, like Google Forms: a member makes a link to a form for one of the app's tables (new responses) or to
    /// one record (e.g. "update your delivery address"), limited to the fields they choose.
    ///   access "anyone"  : anyone with the link, no sign-in
    ///   access "members" : only people signed in to this app
    /// Links can expire, stop after N responses, be closed and reopened, and be regenerated (the old link stops working).
    /// A link acts with its creator's rights at the moment it is used (so removing their permission or account stops it),
    /// but only on the chosen table, record and fields; every write runs through DataGuard like any other, shown as
    /// "&lt;creator&gt; via share link". The link is "/f/&lt;id&gt;.&lt;code&gt;": the code is an HMAC of a random nonce with the
    /// app's secret, so the database alone can't produce working links. ui.config.json "sharing": false turns it off.
    /// </summary>
    // answers change with every action (closed, used, expired): never cached, or a browser keeps showing an old one
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ApiController]
    public class SharesController : ControllerBase
    {
        const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Shares";
        const string PublicRoute = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/public/share";
        static readonly HashSet<string> NeverShared = new(StringComparer.OrdinalIgnoreCase)
            { "createdby", "modifiedby", "created_by_user", "modified_by_user", "isactive", "createdat", "modifiedat" };

        readonly byte[] _secret;
        public SharesController(IConfiguration config)
        {
            var s = config["AppSettings:Secret"];
            _secret = string.IsNullOrEmpty(s) ? null : Encoding.UTF8.GetBytes(s);
        }

        bool Enabled => _secret != null && DataGuard.Config["sharing"]?.GetValueKind() != JsonValueKind.False;
        static ActionResult Gone(string message, int status = 404) => new ObjectResult(new { message }) { StatusCode = status };

        static DbConnection Open()
        {
            var c = Db.Connection(DataGuard.ConnectionString);
            c.Open();
            Ensure(c);
            return c;
        }

        static bool _ensured;
        static void Ensure(DbConnection c)
        {
            if (_ensured) return;
            using (var cmd = Db.Command(Db.CreateTableIfMissing("share_links", $@"share_id {Db.AutoId},
                nonce VARCHAR(64) NOT NULL, table_name VARCHAR(64) NOT NULL, kind VARCHAR(10) NOT NULL, record_id VARCHAR(64) NULL,
                fields {Db.TextType} NOT NULL, fixed_values {Db.TextType} NULL, access VARCHAR(10) NOT NULL, title VARCHAR(200) NULL,
                expires_at DATETIME NULL, max_responses INT NULL, responses INT NOT NULL DEFAULT 0, is_open {Db.BoolType} NOT NULL DEFAULT 1,
                owner_name VARCHAR(255) NOT NULL, user_id INT NOT NULL, role_id INT NOT NULL, created_by_user VARCHAR(255) NOT NULL,
                createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, last_used_at DATETIME NULL,
                show_fields {Db.TextType} NULL, allow_files {Db.BoolType} NOT NULL DEFAULT 0"), c)) Db.Execute(cmd);
            // tables made before these options: add the columns
            foreach (var (col, def) in new[] { ("show_fields", $"{Db.TextType} NULL"), ("allow_files", $"{Db.BoolType} NOT NULL DEFAULT 0") })
            {
                try { using var probe = Db.Command($"SELECT {col} FROM share_links WHERE 1 = 0", c); probe.ExecuteNonQuery(); }
                catch (DbException) { using var add = Db.Command($"ALTER TABLE share_links ADD {col} {def}", c); add.ExecuteNonQuery(); }
            }
            _ensured = true;
        }

        // ------------------------------------------------------------------ tokens

        string Code(long id, string nonce)
        {
            using var h = new HMACSHA256(_secret);
            return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes($"share:{id}:{nonce}"))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        string Url(long id, string nonce) => $"/f/{id}.{Code(id, nonce)}";
        static string NewNonce() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

        sealed class Link
        {
            public long Id; public string Nonce, Table, Kind, RecordId, Access, Title, Owner, CreatedBy;
            public List<string> Fields; public JsonObject Fixed; public JsonObject Show; public bool AllowFiles;
            public DateTime? ExpiresAt; public int? MaxResponses; public int Responses; public bool IsOpen; public int UserId, RoleId;
        }

        static Link ReadLink(DbDataReader r) => new()
        {
            Id = Convert.ToInt64(r["share_id"]), Nonce = (string)r["nonce"], Table = (string)r["table_name"], Kind = (string)r["kind"],
            RecordId = r["record_id"] as string, Access = (string)r["access"], Title = r["title"] as string,
            Fields = JsonSerializer.Deserialize<List<string>>((string)r["fields"]) ?? new(),
            Fixed = r["fixed_values"] is string fx && fx.Length > 0 ? JsonNode.Parse(fx) as JsonObject : null,
            ExpiresAt = r["expires_at"] is DateTime e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null,
            MaxResponses = r["max_responses"] is DBNull ? null : Convert.ToInt32(r["max_responses"]),
            Responses = Convert.ToInt32(r["responses"]), IsOpen = Convert.ToInt32(r["is_open"]) == 1,
            Owner = (string)r["owner_name"], UserId = Convert.ToInt32(r["user_id"]), RoleId = Convert.ToInt32(r["role_id"]),
            CreatedBy = (string)r["created_by_user"],
            Show = r["show_fields"] is string sf && sf.Length > 0 ? JsonNode.Parse(sf) as JsonObject : null,
            AllowFiles = Convert.ToInt32(r["allow_files"]) == 1,
        };

        static Link Load(DbConnection c, long id)
        {
            using var q = Db.Command("SELECT * FROM share_links WHERE share_id = @id", c);
            q.Parameters.AddWithValue("@id", id);
            using var r = q.ExecuteReader();
            return r.Read() ? ReadLink(r) : null;
        }

        /// <summary>The link a token names, or null when the token is malformed or its code doesn't match.</summary>
        Link FromToken(DbConnection c, string token)
        {
            var parts = (token ?? "").Split('.');
            if (parts.Length != 2 || parts[1].Length > 64 || !long.TryParse(parts[0], out var id) || id <= 0) return null;
            var link = Load(c, id);
            if (link == null) return null;
            var want = Encoding.ASCII.GetBytes(Code(link.Id, link.Nonce));
            var got = Encoding.ASCII.GetBytes(parts[1]);
            return want.Length == got.Length && CryptographicOperations.FixedTimeEquals(want, got) ? link : null;
        }

        // ------------------------------------------------------------------ rights

        /// <summary>Role permission of a member on a table (r/c/u letters).</summary>
        static string Rights(DbConnection c, int userId, int roleId, string table)
        {
            using var q = Db.Command(@"SELECT p.can_read, p.can_write, p.can_update FROM permissionmatrix p JOIN entities e ON e.entity_id = p.entity_id
                WHERE LOWER(e.entity_name) = @t AND p.role_id = @r AND p.user_id = @u", c);
            q.Parameters.AddWithValue("@t", table.ToLowerInvariant());
            q.Parameters.AddWithValue("@r", roleId);
            q.Parameters.AddWithValue("@u", userId);
            using var r = q.ExecuteReader();
            if (!r.Read()) return "";
            return (Convert.ToInt32(r.GetValue(0)) == 1 ? "r" : "") + (Convert.ToInt32(r.GetValue(1)) == 1 ? "c" : "") + (Convert.ToInt32(r.GetValue(2)) == 1 ? "u" : "");
        }

        static bool IsActiveUser(DbConnection c, int userId)
        {
            using var q = Db.Command("SELECT isActive FROM users WHERE user_id = @u", c);
            q.Parameters.AddWithValue("@u", userId);
            var v = q.ExecuteScalar();
            return v != null && v != DBNull.Value && Convert.ToInt32(v) == 1;
        }

        /// <summary>The caller a link acts as: its creator, at the moment of use, labelled "via share link".</summary>
        static DataGuard.Caller CreatorCaller(Link link, string member, Microsoft.AspNetCore.Http.HttpContext http)
        {
            var caller = new DataGuard.Caller
            {
                User = link.CreatedBy, UserId = link.UserId, RoleId = link.RoleId, Owner = link.Owner,
                Via = member == null ? "share link" : $"share link ({member})",
                Ip = http.Connection?.RemoteIpAddress?.ToString(), CorrelationId = http.TraceIdentifier,
            };
            if (DataGuard.TenancyEnabled)
            {
                var (tenant, unassigned) = DataGuard.ResolveTenant(link.UserId, link.RoleId);
                caller.Tenant = tenant; caller.Unassigned = unassigned;
            }
            return caller;
        }

        /// <summary>Stored names of the fields a link may expose: real columns, not the key, audit, tenant, hidden or (unless fixed) reference columns.</summary>
        static List<string> CleanFields(string table, IEnumerable<string> names, bool allowReferences)
        {
            var pk = DataGuard.PrimaryKeys.GetValueOrDefault(table);
            var fks = DataGuard.ForeignKeys.TryGetValue(table, out var l) ? l.Select(x => x.Column).ToHashSet(StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
            var outList = new List<string>();
            foreach (var n in names ?? Enumerable.Empty<string>())
            {
                var col = DataGuard.TryKnownColumn(table, n);
                if (col == null) throw new DataGuardException($"Unknown field: {n}", 400);
                if (string.Equals(col, pk, StringComparison.OrdinalIgnoreCase) || NeverShared.Contains(col) || DataGuard.IsHidden(table, col)
                    || (DataGuard.TenancyEnabled && string.Equals(col, DataGuard.TenantColumn, StringComparison.OrdinalIgnoreCase)))
                    throw new DataGuardException($"{n} can't be shared.", 400);
                if (!allowReferences && fks.Contains(col)) throw new DataGuardException($"{n} links to another table: set a fixed value for it instead.", 400);
                if (!outList.Contains(col, StringComparer.OrdinalIgnoreCase)) outList.Add(col);
            }
            return outList;
        }

        static object Value(JsonNode v) => v switch
        {
            null => null,
            JsonValue jv when jv.TryGetValue<bool>(out var b) => b ? 1 : 0,
            JsonValue jv when jv.TryGetValue<long>(out var n) => n,
            JsonValue jv when jv.TryGetValue<decimal>(out var d) => d,
            JsonValue jv when jv.TryGetValue<string>(out var s) => s.Length > 20000 ? throw new DataGuardException("A value is too long.", 400) : s,
            _ => throw new DataGuardException("Only plain values can be sent.", 400),
        };

        // ------------------------------------------------------------------ members: manage links

        public sealed class ShareRequest
        {
            public string table { get; set; }
            public string recordId { get; set; }          // set: a link to this one record (update); empty: a form (new responses)
            public List<string> fields { get; set; }
            public JsonObject fixedValues { get; set; }   // form links: values every response gets (e.g. which list it goes to)
            public string access { get; set; }            // "anyone" (default) or "members"
            public string title { get; set; }
            public DateTime? expiresAt { get; set; }
            public int? maxResponses { get; set; }
            /// <summary>Form links: details of the record a fixed reference points at, shown above the form (with its files),
            /// e.g. {"job_id": ["title", "description", "location"]}.</summary>
            public Dictionary<string, List<string>> showFields { get; set; }
            /// <summary>Form links: people may attach files (a resume): PDF, Word, text or images, up to 10 MB, 3 per response.</summary>
            public bool allowFiles { get; set; }
        }

        DataGuard.Caller Me => DataGuard.Current;
        bool IsAdmin => Me?.RoleId == 1;

        object Describe(Link l) => new
        {
            id = l.Id, table = l.Table, kind = l.Kind, recordId = l.RecordId, fields = l.Fields, fixedValues = l.Fixed, access = l.Access,
            title = l.Title, showFields = l.Show, allowFiles = l.AllowFiles, expiresAt = l.ExpiresAt, maxResponses = l.MaxResponses, responses = l.Responses, isOpen = l.IsOpen,
            createdBy = l.CreatedBy, url = Url(l.Id, l.Nonce),
        };

        [Authorize, HttpGet, Route(Route)]
        public ActionResult List(string table, string recordId = null)
        {
            if (!Enabled) return Gone("Sharing is turned off for this app.");
            var t = DataGuard.KnownTable(table);
            if (t == null || Me == null) return Gone("Unknown table.");
            using var c = Open();
            using var q = Db.Command($@"SELECT * FROM share_links WHERE owner_name = @o AND table_name = @t
                AND {(recordId == null ? "record_id IS NULL" : "record_id = @rid")}{(IsAdmin ? "" : " AND user_id = @u")} ORDER BY share_id DESC", c);
            q.Parameters.AddWithValue("@o", Me.Owner ?? "");
            q.Parameters.AddWithValue("@t", t);
            q.Parameters.AddWithValue("@u", Me.UserId);
            if (recordId != null) q.Parameters.AddWithValue("@rid", recordId);
            var items = new List<object>();
            using (var r = q.ExecuteReader()) while (r.Read()) items.Add(Describe(ReadLink(r)));
            return Ok(new { items });
        }

        [Authorize, HttpPost, Route(Route)]
        public ActionResult Create([FromBody] ShareRequest req)
        {
            if (!Enabled) return Gone("Sharing is turned off for this app.");
            var t = DataGuard.KnownTable(req?.table);
            if (t == null || Me == null || Me.Unassigned) return Gone("Unknown table.");
            var kind = string.IsNullOrWhiteSpace(req.recordId) ? "form" : "record";
            var access = req.access == "members" ? "members" : "anyone";
            using var c = Open();
            var rights = Rights(c, Me.UserId, Me.RoleId, t);
            if (!rights.Contains(kind == "form" ? 'c' : 'u')) return Gone(kind == "form" ? "You can't add records here, so you can't share a form for it." : "You can't edit this record, so you can't share it.", 403);
            var fields = CleanFields(t, req.fields, allowReferences: false);
            if (fields.Count == 0 || fields.Count > 60) return BadRequest(new { message = "Choose the fields to share." });
            JsonObject fixedValues = null;
            if (kind == "form" && req.fixedValues is { Count: > 0 })
            {
                fixedValues = new JsonObject();
                var keys = CleanFields(t, req.fixedValues.Select(kv => kv.Key), allowReferences: true);
                foreach (var (k, v) in req.fixedValues)
                {
                    var col = keys.First(x => x.Equals(DataGuard.TryKnownColumn(t, k), StringComparison.OrdinalIgnoreCase));
                    Value(v);
                    fixedValues[col] = v?.DeepClone();
                    fields.RemoveAll(f => f.Equals(col, StringComparison.OrdinalIgnoreCase));
                }
            }
            JsonObject show = null;
            if (kind == "form" && req.showFields is { Count: > 0 })
            {
                show = new JsonObject();
                foreach (var (refCol, wanted) in req.showFields)
                {
                    var col = DataGuard.TryKnownColumn(t, refCol);
                    var fk = col == null ? default : (DataGuard.ForeignKeys.TryGetValue(t, out var fks) ? fks.FirstOrDefault(x => x.Column.Equals(col, StringComparison.OrdinalIgnoreCase)) : default);
                    if (fk.Column == null || fixedValues?[col] == null)
                        return BadRequest(new { message = $"{refCol}: details can only be shown for a link field with a fixed value." });
                    var target = DataGuard.KnownTable(fk.RefTable);
                    if (target == null) return BadRequest(new { message = $"{refCol}: unknown linked table." });
                    var cols = new JsonArray();
                    foreach (var n in CleanFields(target, wanted, allowReferences: true)) cols.Add(n);
                    if (cols.Count == 0 || cols.Count > 30) return BadRequest(new { message = "Choose up to 30 details to show." });
                    // the creator must be able to see that record now
                    using var _ = DataGuard.RunAs(Me);
                    using var q = Db.Command($"SELECT COUNT(*) FROM {Db.Q(target)} WHERE {Db.Q(DataGuard.PrimaryKeys[target])} = @id", c);
                    q.Parameters.AddWithValue("@id", Value(fixedValues[col]));
                    if (Convert.ToInt32(DataGuard.Apply(q).ExecuteScalar()) == 0) return Gone("The linked record wasn't found.");
                    show[col] = new JsonObject { ["table"] = target, ["fields"] = cols };
                }
            }
            if (kind == "record")
            {
                // the creator must be able to see the record now
                using var _ = DataGuard.RunAs(Me);
                using var q = Db.Command($"SELECT COUNT(*) FROM {Db.Q(t)} WHERE {Db.Q(DataGuard.PrimaryKeys[t])} = @id", c);
                q.Parameters.AddWithValue("@id", req.recordId);
                if (Convert.ToInt32(DataGuard.Apply(q).ExecuteScalar()) == 0) return Gone("Record not found.");
            }
            if (req.expiresAt is { } exp && exp.ToUniversalTime() <= DateTime.UtcNow) return BadRequest(new { message = "The expiry date is in the past." });
            if (req.maxResponses is < 1) return BadRequest(new { message = "Maximum responses must be at least 1." });
            var nonce = NewNonce();
            using var ins = Db.Command(@"INSERT INTO share_links (nonce, table_name, kind, record_id, fields, fixed_values, access, title, expires_at, max_responses,
                owner_name, user_id, role_id, created_by_user, show_fields, allow_files) VALUES (@n, @t, @k, @rid, @f, @fx, @a, @ti, @e, @m, @o, @u, @r, @cb, @sf, @af)", c);
            ins.Parameters.AddWithValue("@sf", show == null ? DBNull.Value : show.ToJsonString());
            ins.Parameters.AddWithValue("@af", kind == "form" && req.allowFiles ? 1 : 0);
            ins.Parameters.AddWithValue("@n", nonce);
            ins.Parameters.AddWithValue("@t", t);
            ins.Parameters.AddWithValue("@k", kind);
            ins.Parameters.AddWithValue("@rid", kind == "record" ? req.recordId : DBNull.Value);
            ins.Parameters.AddWithValue("@f", JsonSerializer.Serialize(fields));
            ins.Parameters.AddWithValue("@fx", fixedValues == null ? DBNull.Value : fixedValues.ToJsonString());
            ins.Parameters.AddWithValue("@a", access);
            ins.Parameters.AddWithValue("@ti", string.IsNullOrWhiteSpace(req.title) ? DBNull.Value : req.title.Trim()[..Math.Min(200, req.title.Trim().Length)]);
            ins.Parameters.AddWithValue("@e", req.expiresAt is { } e2 ? e2.ToUniversalTime() : DBNull.Value);
            ins.Parameters.AddWithValue("@m", (object)req.maxResponses ?? DBNull.Value);
            ins.Parameters.AddWithValue("@o", Me.Owner ?? "");
            ins.Parameters.AddWithValue("@u", Me.UserId);
            ins.Parameters.AddWithValue("@r", Me.RoleId);
            ins.Parameters.AddWithValue("@cb", Me.User ?? "");
            var id = Db.InsertReturningId(ins);
            return Ok(Describe(Load(c, id)));
        }

        Link Mine(DbConnection c, long id)
        {
            var l = Load(c, id);
            return l != null && Me != null && l.Owner == Me.Owner && (IsAdmin || l.UserId == Me.UserId) ? l : null;
        }

        public sealed class ShareUpdate { public bool? isOpen { get; set; } public DateTime? expiresAt { get; set; } public bool clearExpiry { get; set; } public int? maxResponses { get; set; } public bool clearMax { get; set; } public string title { get; set; } }

        [Authorize, HttpPut, Route(Route + "/{id:long}")]
        public ActionResult Update(long id, [FromBody] ShareUpdate u)
        {
            if (!Enabled) return Gone("Sharing is turned off for this app.");
            using var c = Open();
            var l = Mine(c, id);
            if (l == null || u == null) return Gone("Link not found.");
            using var q = Db.Command(@"UPDATE share_links SET is_open = @o, expires_at = @e, max_responses = @m, title = @ti WHERE share_id = @id", c);
            q.Parameters.AddWithValue("@o", (u.isOpen ?? l.IsOpen) ? 1 : 0);
            q.Parameters.AddWithValue("@e", u.clearExpiry ? DBNull.Value : u.expiresAt is { } e ? e.ToUniversalTime() : (object)l.ExpiresAt ?? DBNull.Value);
            q.Parameters.AddWithValue("@m", u.clearMax ? DBNull.Value : (object)(u.maxResponses is >= 1 ? u.maxResponses : l.MaxResponses) ?? DBNull.Value);
            q.Parameters.AddWithValue("@ti", (object)(u.title ?? l.Title) ?? DBNull.Value);
            q.Parameters.AddWithValue("@id", id);
            q.ExecuteNonQuery();
            return Ok(Describe(Load(c, id)));
        }

        /// <summary>New link; the old one stops working at once.</summary>
        [Authorize, HttpPost, Route(Route + "/{id:long}/regenerate")]
        public ActionResult Regenerate(long id)
        {
            if (!Enabled) return Gone("Sharing is turned off for this app.");
            using var c = Open();
            if (Mine(c, id) == null) return Gone("Link not found.");
            using var q = Db.Command("UPDATE share_links SET nonce = @n WHERE share_id = @id", c);
            q.Parameters.AddWithValue("@n", NewNonce());
            q.Parameters.AddWithValue("@id", id);
            q.ExecuteNonQuery();
            return Ok(Describe(Load(c, id)));
        }

        [Authorize, HttpDelete, Route(Route + "/{id:long}")]
        public ActionResult Delete(long id)
        {
            if (!Enabled) return Gone("Sharing is turned off for this app.");
            using var c = Open();
            if (Mine(c, id) == null) return Gone("Link not found.");
            using var q = Db.Command("DELETE FROM share_links WHERE share_id = @id", c);
            q.Parameters.AddWithValue("@id", id);
            q.ExecuteNonQuery();
            return Ok(new { message = "Link deleted." });
        }

        // ------------------------------------------------------------------ files on a response

        static readonly HashSet<string> UploadTypes = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt", ".png", ".jpg", ".jpeg" };
        const long MaxUploadBytes = 10 * 1024 * 1024;
        const int MaxUploadsPerResponse = 3;

        string UploadSig(Link link, string recordId, long until)
        {
            using var h = new HMACSHA256(_secret);
            return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes($"share-upload:{link.Id}:{link.Nonce}:{recordId}:{until}"))).ToLowerInvariant();
        }

        public sealed class UploadForm { public Microsoft.AspNetCore.Http.IFormFile file { get; set; } public string key { get; set; } }

        /// <summary>
        /// Attaches a file to the response just sent through this link. The key (from the submit answer) names that one
        /// record and works for 30 minutes; PDF, Word, text or images, up to 10 MB, 3 per response. Stored and indexed like
        /// files added in the app, so the record page and the AI see it.
        /// </summary>
        [AllowAnonymous, EnableRateLimiting("public"), HttpPost, Route(PublicRoute + "/{token}/files"), RequestSizeLimit(12_000_000)]
        public ActionResult Upload(string token, [FromForm] UploadForm form)
        {
            using var c = Open();
            var problem = Usable(c, token, out var link, out var caller, forUpload: true);
            if (problem != null) return problem;
            if (!link.AllowFiles || link.Kind != "form") return Gone("Files can't be added here.");
            var parts = (form?.key ?? "").Split('.');
            if (parts.Length != 3 || !long.TryParse(parts[0], out _) || !long.TryParse(parts[1], out var until) || until < DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(parts[2]), Encoding.ASCII.GetBytes(UploadSig(link, parts[0], until))))
                return Gone("This upload has expired. Send the form again to attach files.", 410);
            var recordId = parts[0];
            var f = form.file;
            if (f == null || f.Length == 0) return BadRequest(new { message = "Choose a file." });
            if (f.Length > MaxUploadBytes) return BadRequest(new { message = "Files can be up to 10 MB." });
            var ext = System.IO.Path.GetExtension(f.FileName ?? "");
            if (!UploadTypes.Contains(ext)) return BadRequest(new { message = "Attach a PDF, Word, text or image file." });
            using (var count = Db.Command("SELECT COUNT(*) FROM ai_files WHERE owner_name = @o AND table_name = @t AND record_id = @r", c))
            {
                count.Parameters.AddWithValue("@o", link.Owner);
                count.Parameters.AddWithValue("@t", link.Table.ToLowerInvariant());
                count.Parameters.AddWithValue("@r", recordId);
                if (Convert.ToInt32(count.ExecuteScalar()) >= MaxUploadsPerResponse) return BadRequest(new { message = $"Up to {MaxUploadsPerResponse} files per response." });
            }
            var ai = HttpContext.RequestServices.GetService(typeof(nkv.MicroService.API.Ai.AiService)) as nkv.MicroService.API.Ai.AiService;
            if (ai == null) return Gone("Files can't be added here.");
            using (DataGuard.RunAs(caller))
            using (var s = f.OpenReadStream())
            {
                try
                {
                    ai.Upload(new nkv.MicroService.API.Ai.AiUser(link.UserId, $"{link.CreatedBy} via share link", link.RoleId, link.Owner),
                        link.Table.ToLowerInvariant(), recordId, f.FileName, f.ContentType, s);
                }
                catch (nkv.MicroService.API.Ai.AiException ex) { return BadRequest(new { message = ex.Message }); }
            }
            return Ok(new { message = "File attached." });
        }

        // ------------------------------------------------------------------ the link itself (no sign-in for "anyone")

        /// <summary>Checks a link is usable now. Returns an error result, or null and the caller to act as.</summary>
        ActionResult Usable(DbConnection c, string token, out Link link, out DataGuard.Caller caller, bool forUpload = false)
        {
            caller = null;
            link = Enabled ? FromToken(c, token) : null;
            if (link == null) return Gone("This link isn't available. Ask the person who sent it for a new one.");
            if (!link.IsOpen) return Gone("This form is closed.", 410);
            if (link.ExpiresAt is { } e && e <= DateTime.UtcNow) return Gone("This link has expired.", 410);
            if (!forUpload && link.Kind == "form" && link.MaxResponses is { } m && link.Responses >= m) return Gone("This form isn't taking more responses.", 410);
            string member = null;
            if (link.Access == "members")
            {
                if (User?.Identity?.IsAuthenticated != true || User.FindFirst(ClaimTypes.Actor)?.Value != link.Owner)
                    return Gone("Sign in to this app to open this link.", 401);
                member = User.FindFirst(ClaimTypes.Name)?.Value;
            }
            // the creator's rights now: account active, table permission still granted
            var rights = IsActiveUser(c, link.UserId) ? Rights(c, link.UserId, link.RoleId, link.Table) : "";
            if (!rights.Contains(link.Kind == "form" ? 'c' : 'u')) return Gone("This link isn't available. Ask the person who sent it for a new one.");
            if (DataGuard.KnownTable(link.Table) is not { } table || !DataGuard.PrimaryKeys.ContainsKey(table)) return Gone("This link isn't available.");
            link.Table = table;
            caller = CreatorCaller(link, member, HttpContext);
            if (caller.Unassigned) return Gone("This link isn't available.");
            return null;
        }

        [AllowAnonymous, EnableRateLimiting("public"), HttpGet, Route(PublicRoute + "/{token}")]
        public ActionResult OpenLink(string token)
        {
            using var c = Open();
            var problem = Usable(c, token, out var link, out var caller);
            if (problem != null) return problem;
            Dictionary<string, object> values = null;
            if (link.Kind == "record")
            {
                using var _ = DataGuard.RunAs(caller);
                using var q = Db.Command($"SELECT {string.Join(", ", link.Fields.Select(Db.Q))} FROM {Db.Q(link.Table)} WHERE {Db.Q(DataGuard.PrimaryKeys[link.Table])} = @id", c);
                q.Parameters.AddWithValue("@id", link.RecordId);
                using var r = DataGuard.Apply(q).ExecuteReader();
                if (!r.Read()) return Gone("This link isn't available.");
                values = new();
                for (var i = 0; i < r.FieldCount; i++) values[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            }
            return Ok(new { title = link.Title, table = link.Table, kind = link.Kind, fields = link.Fields, values, access = link.Access,
                context = Context(c, link, caller), allowFiles = link.AllowFiles });
        }

        /// <summary>The records a form link shows above the form: only the chosen fields, read as the creator, plus their files.</summary>
        List<object> Context(DbConnection c, Link link, DataGuard.Caller caller)
        {
            var list = new List<object>();
            if (link.Show == null || link.Fixed == null) return list;
            using var _ = DataGuard.RunAs(caller);
            foreach (var (col, node) in link.Show)
            {
                if (node is not JsonObject o || DataGuard.KnownTable(o["table"]?.ToString()) is not { } target || link.Fixed[col] is not { } idNode) continue;
                var fields = (o["fields"] as JsonArray ?? new JsonArray()).Select(x => DataGuard.TryKnownColumn(target, x?.ToString())).Where(x => x != null && !DataGuard.IsHidden(target, x)).ToList();
                if (fields.Count == 0 || !DataGuard.PrimaryKeys.TryGetValue(target, out var pk)) continue;
                var id = Value(idNode);
                using var q = Db.Command($"SELECT {string.Join(", ", fields.Select(Db.Q))} FROM {Db.Q(target)} WHERE {Db.Q(pk)} = @id", c);
                q.Parameters.AddWithValue("@id", id);
                var values = new Dictionary<string, object>();
                using (var r = DataGuard.Apply(q).ExecuteReader())
                {
                    if (!r.Read()) continue;
                    for (var i = 0; i < r.FieldCount; i++) values[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                }
                list.Add(new { table = target, fields, values, files = ContextFiles(c, link, target, Convert.ToString(id)) });
            }
            return list;
        }

        static List<object> ContextFiles(DbConnection c, Link link, string table, string recordId)
        {
            var files = new List<object>();
            try
            {
                using var q = Db.Command("SELECT file_id, file_name, size_bytes FROM ai_files WHERE owner_name = @o AND table_name = @t AND record_id = @r ORDER BY file_id", c);
                q.Parameters.AddWithValue("@o", link.Owner);
                q.Parameters.AddWithValue("@t", table.ToLowerInvariant());
                q.Parameters.AddWithValue("@r", recordId);
                using var r = q.ExecuteReader();
                while (r.Read()) files.Add(new { id = Convert.ToInt64(r.GetValue(0)), name = r.GetString(1), size = r.IsDBNull(2) ? 0 : Convert.ToInt64(r.GetValue(2)) });
            }
            catch (DbException) { /* no files module */ }
            return files;
        }

        /// <summary>Downloads a file of a record the link shows (e.g. the job description), through a 5-minute signed link.</summary>
        [AllowAnonymous, EnableRateLimiting("public"), HttpGet, Route(PublicRoute + "/{token}/file/{fileId:long}")]
        public ActionResult ContextFile(string token, long fileId)
        {
            using var c = Open();
            var problem = Usable(c, token, out var link, out var caller);
            if (problem != null) return problem;
            if (link.Show == null || link.Fixed == null) return Gone("File not found.");
            foreach (var (col, node) in link.Show)
            {
                if (node is not JsonObject o || DataGuard.KnownTable(o["table"]?.ToString()) is not { } target || link.Fixed[col] is not { } idNode) continue;
                var recordId = Convert.ToString(Value(idNode));
                using (DataGuard.RunAs(caller))
                {
                    using var vis = Db.Command($"SELECT COUNT(*) FROM {Db.Q(target)} WHERE {Db.Q(DataGuard.PrimaryKeys[target])} = @id", c);
                    vis.Parameters.AddWithValue("@id", recordId);
                    if (Convert.ToInt32(DataGuard.Apply(vis).ExecuteScalar()) == 0) continue;
                }
                using var q = Db.Command("SELECT COUNT(*) FROM ai_files WHERE file_id = @f AND owner_name = @o AND table_name = @t AND record_id = @r", c);
                q.Parameters.AddWithValue("@f", fileId);
                q.Parameters.AddWithValue("@o", link.Owner);
                q.Parameters.AddWithValue("@t", target.ToLowerInvariant());
                q.Parameters.AddWithValue("@r", recordId);
                if (Convert.ToInt32(q.ExecuteScalar()) == 0) continue;
                var exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
                return Redirect($"/{APIEndpoint.Version}/{APIEndpoint.APIRoute}/Ai/files/{fileId}/signed?exp={exp}&sig={DataGuard.Sign("ai_file:" + fileId, exp)}");
            }
            return Gone("File not found.");
        }

        [AllowAnonymous, EnableRateLimiting("public"), HttpPost, Route(PublicRoute + "/{token}")]
        public ActionResult Submit(string token, [FromBody] JsonObject body)
        {
            if (body == null || body.Count > 100) return BadRequest(new { message = "Nothing to save." });
            // honeypot: a hidden "website" box people never see; bots fill it in
            if (body["website"] is JsonValue hp && hp.TryGetValue<string>(out var hps) && hps.Length > 0) return Ok(new { message = "Thank you, your response was sent." });
            using var c = Open();
            if (User?.Identity?.IsAuthenticated == true && !SessionCookie.IsBrowserClient(Request) && !Request.Headers.ContainsKey("Authorization"))
                return Gone("Not available.", 403); // cookie writes need the client header
            var problem = Usable(c, token, out var link, out var caller);
            if (problem != null) return problem;
            var table = link.Table;
            var cols = new List<string>(); var values = new List<object>();
            foreach (var f in link.Fields)
            {
                var col = DataGuard.TryKnownColumn(table, f);
                if (col == null) continue;
                var sent = body.FirstOrDefault(kv => kv.Key.Equals(f, StringComparison.OrdinalIgnoreCase));
                if (sent.Key == null) continue;
                cols.Add(col); values.Add(Value(sent.Value));
            }
            if (link.Fixed != null)
                foreach (var (k, v) in link.Fixed)
                    if (DataGuard.TryKnownColumn(table, k) is { } col) { cols.Add(col); values.Add(Value(v)); }
            if (cols.Count == 0) return BadRequest(new { message = "Nothing to save." });

            using var _ = DataGuard.RunAs(caller);
            if (link.Kind == "form")
            {
                // count the response first, atomically, so "max responses" holds under load
                using (var take = Db.Command("UPDATE share_links SET responses = responses + 1, last_used_at = " + Db.UtcNow +
                    " WHERE share_id = @id AND (max_responses IS NULL OR responses < max_responses)", c))
                {
                    take.Parameters.AddWithValue("@id", link.Id);
                    if (take.ExecuteNonQuery() == 0) return Gone("This form isn't taking more responses.", 410);
                }
                foreach (var audit in new[] { "createdBy", "modifiedBy" })
                    if (DataGuard.TryKnownColumn(table, audit) is { } ac && !cols.Contains(ac, StringComparer.OrdinalIgnoreCase)) { cols.Add(ac); values.Add(link.Owner); }
                using var cmd = c.CreateCommand();
                cmd.CommandText = $"INSERT INTO {Db.Q(table)} ({string.Join(", ", cols.Select(Db.Q))}) VALUES ({string.Join(", ", cols.Select((_, i) => "@p" + i))})";
                for (var i = 0; i < values.Count; i++) cmd.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
                try
                {
                    DataGuard.NonQuery(cmd);
                }
                catch (Exception ex) when (ex is DbException || ex is DataGuardException)
                {
                    using (var back = Db.Command("UPDATE share_links SET responses = responses - 1 WHERE share_id = @id AND responses > 0", c))
                    {
                        back.Parameters.AddWithValue("@id", link.Id);
                        back.ExecuteNonQuery();
                    }
                    if (ex is DataGuardException) throw; // validation messages per field, shown on the form
                    return BadRequest(new { message = "Couldn't send. Please fill in every required field and check the values." });
                }
                if (!link.AllowFiles) return Ok(new { message = "Thank you, your response was sent." });
                var newId = Convert.ToString(Db.LastInsertedId(cmd));
                var until = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
                return Ok(new { message = "Thank you, your response was sent.", uploadKey = $"{newId}.{until}.{UploadSig(link, newId, until)}" });
            }
            else
            {
                if (DataGuard.TryKnownColumn(table, "modifiedBy") is { } mb && !cols.Contains(mb, StringComparer.OrdinalIgnoreCase)) { cols.Add(mb); values.Add(link.Owner); }
                using var cmd = c.CreateCommand();
                cmd.CommandText = $"UPDATE {Db.Q(table)} SET {string.Join(", ", cols.Select((x, i) => $"{Db.Q(x)} = @p{i}"))} WHERE {Db.Q(DataGuard.PrimaryKeys[table])} = @id";
                for (var i = 0; i < values.Count; i++) cmd.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", link.RecordId);
                try
                {
                    if (DataGuard.NonQuery(cmd) == 0) return Gone("This link isn't available.");
                }
                catch (DbException)
                {
                    return BadRequest(new { message = "Couldn't save. Please check the values." });
                }
                using (var used = Db.Command("UPDATE share_links SET responses = responses + 1, last_used_at = " + Db.UtcNow + " WHERE share_id = @id", c))
                {
                    used.Parameters.AddWithValue("@id", link.Id);
                    used.ExecuteNonQuery();
                }
                return Ok(new { message = "Saved. Thank you." });
            }
        }
    }
}
