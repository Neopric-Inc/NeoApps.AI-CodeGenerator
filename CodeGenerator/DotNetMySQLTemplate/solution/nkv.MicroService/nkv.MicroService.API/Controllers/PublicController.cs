using System;
using System.Collections.Generic;
using System.Linq;
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
    /// For visitors without sign-in (a shop, a public form): only the tables and actions the "Public" role in
    /// ui.config.json grants (e.g. "roles": {"Public": {"products": "r", "orders": "c"}}). Every query runs through
    /// DataGuard as the Public role: its hidden fields, read filters, validations and numbering apply. Rate-limited per
    /// visitor. Nothing is public unless that role exists; apps with tenancy have no public access.
    /// Custom public endpoints follow the same pattern: route under v1/api/public/..., [AllowAnonymous],
    /// [EnableRateLimiting("public")], and every query inside using (DataGuard.RunAs(DataGuard.PublicCaller(HttpContext))).
    /// </summary>
    [ApiController, AllowAnonymous, EnableRateLimiting("public")]
    [Route(APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/public")]
    public sealed class PublicController : ControllerBase
    {
        static readonly HashSet<string> NeverShown = new(StringComparer.OrdinalIgnoreCase)
            { "createdby", "modifiedby", "created_by_user", "modified_by_user", "isactive" };
        readonly string _connectionString;
        public PublicController(IConfiguration config) => _connectionString = config.GetConnectionString("MySQLDatabase");

        ActionResult NotAvailable() => NotFound(new { message = "This page isn't available." });

        /// <summary>Rows of a table the Public role may read (newest first, 50 per page, up to 200).</summary>
        [HttpGet("{table}")]
        public ActionResult List(string table, int page = 1, int pageSize = 50)
        {
            table = (table ?? "").ToLowerInvariant();
            if (!DataGuard.PublicCan(table, 'r') || !DataGuard.PrimaryKeys.TryGetValue(table, out var pk)) return NotAvailable();
            pageSize = Math.Clamp(pageSize, 1, 200); page = Math.Max(1, page);
            using var _ = DataGuard.RunAs(DataGuard.PublicCaller(HttpContext));
            using var c = Db.Open(_connectionString);
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {Db.Q(table)} WHERE {Db.Q(DataGuard.KnownColumn(table, "isActive"))} = 1 ORDER BY {Db.Q(pk)} DESC " +
                Db.Page(((page - 1) * pageSize).ToString(), pageSize.ToString());
            return Ok(new { records = Read(table, DataGuard.Apply(cmd)) });
        }

        [HttpGet("{table}/{id:int}")]
        public ActionResult One(string table, int id)
        {
            table = (table ?? "").ToLowerInvariant();
            if (!DataGuard.PublicCan(table, 'r') || !DataGuard.PrimaryKeys.TryGetValue(table, out var pk)) return NotAvailable();
            using var _ = DataGuard.RunAs(DataGuard.PublicCaller(HttpContext));
            using var c = Db.Open(_connectionString);
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {Db.Q(table)} WHERE {Db.Q(pk)} = @id AND {Db.Q(DataGuard.KnownColumn(table, "isActive"))} = 1";
            cmd.Parameters.AddWithValue("@id", id);
            var rows = Read(table, DataGuard.Apply(cmd));
            return rows.Count == 0 ? NotAvailable() : Ok(rows[0]);
        }

        /// <summary>Creates a row in a table the Public role may create in (a form, an order). Returns its id.</summary>
        [HttpPost("{table}")]
        public ActionResult Create(string table, [FromBody] JsonObject body)
        {
            table = (table ?? "").ToLowerInvariant();
            if (!DataGuard.PublicCan(table, 'c') || !DataGuard.PrimaryKeys.TryGetValue(table, out var pk)) return NotAvailable();
            if (body == null || body.Count == 0 || body.Count > 100) return BadRequest(new { message = "Nothing to save." });
            using var _ = DataGuard.RunAs(DataGuard.PublicCaller(HttpContext));
            var cols = new List<string>(); var values = new List<object>();
            foreach (var (name, value) in body)
            {
                var col = DataGuard.KnownColumn(table, name); // unknown names refused (400)
                if (string.Equals(col, pk, StringComparison.OrdinalIgnoreCase) || NeverShown.Contains(col) || DataGuard.IsHidden(table, col)) continue;
                cols.Add(col); values.Add(Value(value));
            }
            if (cols.Count == 0) return BadRequest(new { message = "Nothing to save." });
            // rows belong to the workspace owner, like every row the app writes
            var owner = DataGuard.Current?.Owner;
            foreach (var audit in new[] { "createdBy", "modifiedBy" })
                if (owner != null && DataGuard.TryKnownColumn(table, audit) is { } ac) { cols.Add(ac); values.Add(owner); }
            using var c = Db.Open(_connectionString);
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"INSERT INTO {Db.Q(table)} ({string.Join(", ", cols.Select(Db.Q))}) VALUES ({string.Join(", ", cols.Select((_, i) => "@p" + i))})";
            for (var i = 0; i < values.Count; i++) cmd.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
            DataGuard.Apply(cmd);
            var id = Db.InsertReturningId(cmd);
            return Ok(new { id, message = "Saved." });
        }

        static List<Dictionary<string, object>> Read(string table, System.Data.Common.DbCommand cmd)
        {
            var rows = new List<Dictionary<string, object>>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < r.FieldCount; i++)
                {
                    var name = r.GetName(i);
                    if (NeverShown.Contains(name) || DataGuard.IsHidden(table, name)) continue;
                    row[name] = r.IsDBNull(i) ? null : r.GetValue(i);
                }
                rows.Add(row);
            }
            return rows;
        }

        static object Value(JsonNode v) => v switch
        {
            null => null,
            JsonValue j when j.TryGetValue<bool>(out var b) => b ? 1 : 0,
            JsonValue j when j.TryGetValue<long>(out var l) => l,
            JsonValue j when j.TryGetValue<decimal>(out var d) => d,
            JsonValue j when j.TryGetValue<string>(out var s) => s.Length > 4000 ? s[..4000] : s,
            _ => v.ToJsonString(),
        };
    }
}
