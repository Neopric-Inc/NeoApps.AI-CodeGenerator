using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace nkv.MicroService.API.Ai
{
    public sealed record AiUser(int UserId, string Username, int RoleId, string Tenant)
    {
        public bool IsAdmin => RoleId == 1;
    }

    public sealed record TablePerm(bool Read, bool Create, bool Update, bool Delete);

    public sealed record ColumnInfo(string Name, string DataType, string ColumnType, bool Nullable, bool HasDefault, bool IsPk, bool AutoIncrement, string RefTable, string RefColumn);

    /// <summary>Database access for the AI module: its own tables, permissions, schema and safe, tenant-scoped data tools.</summary>
    public sealed class AiData
    {
        private static readonly HashSet<string> System = new(StringComparer.OrdinalIgnoreCase)
        {
            "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders", "dnd_ui_versions",
            "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds", "workflow_runs",
            "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions",
            "ai_files", "ai_chunks", "ai_conversations", "ai_messages", "ai_runs",
        };
        private static readonly string[] Audit = { "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive" };
        private static bool _schemaReady;
        private static readonly object SchemaLock = new();
        private readonly string _cs;

        public AiData(IConfiguration configuration) => _cs = configuration.GetConnectionString("MySQLDatabase");

        public MySqlConnection Open()
        {
            var c = new MySqlConnection(_cs);
            c.Open();
            EnsureSchema(c);
            return c;
        }

        private static void EnsureSchema(MySqlConnection c)
        {
            if (_schemaReady) return;
            lock (SchemaLock)
            {
                if (_schemaReady) return;
                foreach (var sql in new[]
                {
                    @"CREATE TABLE IF NOT EXISTS ai_files (file_id INT AUTO_INCREMENT PRIMARY KEY, owner_name VARCHAR(255) NOT NULL,
                        table_name VARCHAR(64) NOT NULL, record_id VARCHAR(64) NOT NULL, file_name VARCHAR(255) NOT NULL, content_type VARCHAR(150),
                        size_bytes BIGINT, pages INT, status VARCHAR(20) NOT NULL, error TEXT, storage_path VARCHAR(500) NOT NULL,
                        createdBy VARCHAR(255), createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, KEY ix_files_record (owner_name, table_name, record_id))",
                    @"CREATE TABLE IF NOT EXISTS ai_chunks (chunk_id INT AUTO_INCREMENT PRIMARY KEY, file_id INT NOT NULL, owner_name VARCHAR(255) NOT NULL,
                        page INT NOT NULL, text MEDIUMTEXT NOT NULL, KEY ix_chunks_file (file_id), FULLTEXT KEY ft_chunks_text (text)) ENGINE=InnoDB",
                    @"CREATE TABLE IF NOT EXISTS ai_conversations (conversation_id INT AUTO_INCREMENT PRIMARY KEY, owner_name VARCHAR(255) NOT NULL,
                        user_id INT NOT NULL, title VARCHAR(255) NOT NULL, context_table VARCHAR(64), context_record VARCHAR(64),
                        createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, KEY ix_conv_user (owner_name, user_id))",
                    @"CREATE TABLE IF NOT EXISTS ai_messages (message_id INT AUTO_INCREMENT PRIMARY KEY, conversation_id INT NOT NULL, role VARCHAR(20) NOT NULL,
                        content MEDIUMTEXT NOT NULL, citations JSON NULL, tools JSON NULL, createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, KEY ix_msg_conv (conversation_id))",
                    @"CREATE TABLE IF NOT EXISTS ai_runs (run_id INT AUTO_INCREMENT PRIMARY KEY, owner_name VARCHAR(255) NOT NULL, user_id INT NOT NULL,
                        workflow VARCHAR(100) NOT NULL, table_name VARCHAR(64), record_id VARCHAR(64), status VARCHAR(20) NOT NULL, summary TEXT,
                        created_records INT NOT NULL DEFAULT 0, createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)",
                })
                {
                    using var cmd = new MySqlCommand(sql, c);
                    cmd.ExecuteNonQuery();
                }
                _schemaReady = true;
            }
        }

        public static MySqlCommand Cmd(MySqlConnection c, string sql, params (string, object)[] ps)
        {
            var cmd = new MySqlCommand(sql, c);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            return cmd;
        }

        // ------------------------------------------------------------------ permissions & schema

        public Dictionary<string, TablePerm> Permissions(MySqlConnection c, AiUser u)
        {
            var perms = new Dictionary<string, TablePerm>(StringComparer.OrdinalIgnoreCase);
            if (u.IsAdmin)
            {
                foreach (var t in AppTables(c)) perms[t] = new TablePerm(true, true, true, true);
                return perms;
            }
            using var r = Cmd(c, @"SELECT e.entity_name, p.can_read, p.can_write, p.can_update, p.can_delete FROM permissionmatrix p
                                   JOIN entities e ON e.entity_id=p.entity_id WHERE p.user_id=@u AND p.isActive=1", ("@u", u.UserId)).ExecuteReader();
            while (r.Read())
                perms[r.GetString(0).ToLowerInvariant()] = new TablePerm(F(r, 1), F(r, 2), F(r, 3), F(r, 4));
            return perms;
        }

        private static bool F(MySqlDataReader r, int i) => !r.IsDBNull(i) && Convert.ToInt32(r.GetValue(i)) == 1;

        public List<string> AppTables(MySqlConnection c)
        {
            var list = new List<string>();
            using var r = Cmd(c, "SELECT table_name FROM information_schema.tables WHERE table_schema=DATABASE() AND table_type='BASE TABLE' ORDER BY table_name").ExecuteReader();
            while (r.Read()) { var t = r.GetString(0).ToLowerInvariant(); if (!System.Contains(t)) list.Add(t); }
            return list;
        }

        public List<ColumnInfo> Columns(MySqlConnection c, string table)
        {
            var fks = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            using (var r = Cmd(c, @"SELECT column_name, referenced_table_name, referenced_column_name FROM information_schema.key_column_usage
                                    WHERE table_schema=DATABASE() AND table_name=@t AND referenced_table_name IS NOT NULL", ("@t", table)).ExecuteReader())
                while (r.Read()) fks[r.GetString(0)] = (r.GetString(1).ToLowerInvariant(), r.GetString(2));
            var cols = new List<ColumnInfo>();
            using (var r = Cmd(c, @"SELECT column_name, data_type, column_type, is_nullable, column_default, column_key, extra FROM information_schema.columns
                                    WHERE table_schema=DATABASE() AND table_name=@t ORDER BY ordinal_position", ("@t", table)).ExecuteReader())
                while (r.Read())
                {
                    fks.TryGetValue(r.GetString(0), out var fk);
                    cols.Add(new ColumnInfo(r.GetString(0), r.GetString(1).ToLowerInvariant(), r.GetString(2).ToLowerInvariant(), r.GetString(3) == "YES",
                        !r.IsDBNull(4), r.GetString(5) == "PRI", r.GetString(6).Contains("auto_increment"), fk.Item1, fk.Item2));
                }
            return cols;
        }

        public string DescribeTables(MySqlConnection c, IEnumerable<string> tables)
        {
            var sb = new StringBuilder();
            foreach (var t in tables)
            {
                var cols = Columns(c, t).Where(x => !Audit.Contains(x.Name));
                sb.Append(t).Append(": ").AppendLine(string.Join(", ", cols.Select(x =>
                    x.Name + " " + x.ColumnType + (x.IsPk ? " PK" : "") + (x.RefTable != null ? $" -> {x.RefTable}.{x.RefColumn}" : "") + (!x.Nullable && !x.HasDefault && !x.IsPk ? " required" : ""))));
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ read-only data tools

        private static readonly Regex Ident = new("^[A-Za-z_][A-Za-z0-9_]{0,63}$");

        private string CheckTable(MySqlConnection c, AiUser u, string table)
        {
            table = (table ?? "").Trim().ToLowerInvariant();
            if (!Ident.IsMatch(table) || !AppTables(c).Contains(table)) throw new AiException($"Unknown table '{table}'. Call list_tables.");
            if (!Permissions(c, u).TryGetValue(table, out var p) || !p.Read) throw new AiException($"You don't have read access to '{table}'.");
            return table;
        }

        private static string CheckColumn(List<ColumnInfo> cols, string col)
        {
            var match = cols.FirstOrDefault(x => string.Equals(x.Name, col, StringComparison.OrdinalIgnoreCase));
            return match?.Name ?? throw new AiException($"Unknown column '{col}'. Available: {string.Join(", ", cols.Select(x => x.Name))}");
        }

        private static string Where(List<ColumnInfo> cols, JsonArray filters, List<(string, object)> ps)
        {
            var parts = new List<string> { "t.createdBy=@tenant", "t.isActive=1" };
            var i = 0;
            foreach (var f in filters ?? new JsonArray())
            {
                var col = CheckColumn(cols, f?["column"]?.GetValue<string>());
                var op = (f?["op"]?.GetValue<string>() ?? "eq").ToLowerInvariant();
                var p = "@f" + i++;
                var val = f?["value"] is JsonValue v ? v.ToString() : f?["value"]?.ToJsonString();
                string sql = op switch
                {
                    "eq" => $"t.`{col}` = {p}", "ne" => $"t.`{col}` <> {p}", "gt" => $"t.`{col}` > {p}", "gte" => $"t.`{col}` >= {p}",
                    "lt" => $"t.`{col}` < {p}", "lte" => $"t.`{col}` <= {p}", "contains" => $"t.`{col}` LIKE CONCAT('%', {p}, '%')",
                    "is_null" => $"t.`{col}` IS NULL", "not_null" => $"t.`{col}` IS NOT NULL",
                    _ => throw new AiException($"Unknown op '{op}'. Use eq, ne, gt, gte, lt, lte, contains, is_null, not_null."),
                };
                if (op != "is_null" && op != "not_null") ps.Add((p, val));
                parts.Add(sql);
            }
            return string.Join(" AND ", parts);
        }

        public string ListTables(MySqlConnection c, AiUser u)
        {
            var perms = Permissions(c, u);
            var readable = AppTables(c).Where(t => perms.TryGetValue(t, out var p) && p.Read).ToList();
            return "Tables you can read (column type, PK, -> foreign key):\n" + DescribeTables(c, readable);
        }

        public string QueryRecords(MySqlConnection c, AiUser u, JsonObject input)
        {
            var table = CheckTable(c, u, input["table"]?.GetValue<string>());
            var cols = Columns(c, table);
            var ps = new List<(string, object)> { ("@tenant", u.Tenant) };
            var where = Where(cols, input["filters"] as JsonArray, ps);
            var select = input["columns"] is JsonArray sc && sc.Count > 0
                ? string.Join(", ", sc.Select(x => "t.`" + CheckColumn(cols, x!.GetValue<string>()) + "`"))
                : string.Join(", ", cols.Where(x => !Audit.Contains(x.Name) || x.Name == "createdAt").Select(x => "t.`" + x.Name + "`"));
            var order = input["order_by"]?.GetValue<string>() is { Length: > 0 } ob ? $" ORDER BY t.`{CheckColumn(cols, ob)}` {(input["descending"]?.GetValue<bool>() == true ? "DESC" : "ASC")}" : "";
            var limit = Math.Clamp(input["limit"]?.GetValue<int>() ?? 25, 1, 100);
            var count = Convert.ToInt64(Cmd(c, $"SELECT COUNT(*) FROM `{table}` t WHERE {where}", ps.ToArray()).ExecuteScalar());
            var rows = Rows(Cmd(c, $"SELECT {select} FROM `{table}` t WHERE {where}{order} LIMIT {limit}", ps.ToArray()));
            return new JsonObject { ["table"] = table, ["total_matching"] = count, ["returned"] = rows.Count, ["rows"] = rows }.ToJsonString();
        }

        public string Aggregate(MySqlConnection c, AiUser u, JsonObject input)
        {
            var table = CheckTable(c, u, input["table"]?.GetValue<string>());
            var cols = Columns(c, table);
            var metric = (input["metric"]?.GetValue<string>() ?? "count").ToLowerInvariant();
            if (!new[] { "count", "sum", "avg", "min", "max" }.Contains(metric)) throw new AiException("metric must be count, sum, avg, min or max");
            var expr = metric == "count" ? "COUNT(*)" : $"{metric.ToUpperInvariant()}(t.`{CheckColumn(cols, input["column"]?.GetValue<string>())}`)";
            var ps = new List<(string, object)> { ("@tenant", u.Tenant) };
            var where = Where(cols, input["filters"] as JsonArray, ps);
            var group = input["group_by"]?.GetValue<string>() is { Length: > 0 } g ? CheckColumn(cols, g) : null;
            var sql = group == null
                ? $"SELECT {expr} AS value FROM `{table}` t WHERE {where}"
                : $"SELECT t.`{group}` AS `{group}`, {expr} AS value FROM `{table}` t WHERE {where} GROUP BY t.`{group}` ORDER BY value DESC LIMIT 50";
            return new JsonObject { ["table"] = table, ["metric"] = metric, ["rows"] = Rows(Cmd(c, sql, ps.ToArray())) }.ToJsonString();
        }

        public static JsonArray Rows(MySqlCommand cmd)
        {
            var arr = new JsonArray();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var o = new JsonObject();
                for (var i = 0; i < r.FieldCount; i++)
                {
                    var v = r.IsDBNull(i) ? null : r.GetValue(i);
                    o[r.GetName(i)] = v switch
                    {
                        null => null,
                        DateTime d => d.ToString(d.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        decimal m => JsonValue.Create(m), double db => JsonValue.Create(db), float fl => JsonValue.Create(fl),
                        long l => JsonValue.Create(l), int n => JsonValue.Create(n), sbyte sb => JsonValue.Create((int)sb), bool b => JsonValue.Create(b),
                        _ => JsonValue.Create(v.ToString()),
                    };
                }
                arr.Add(o);
            }
            return arr;
        }

        /// <summary>A short label for a record (its name/title-like column), used in citations.</summary>
        public string RecordLabel(MySqlConnection c, string table, string recordId)
        {
            try
            {
                var cols = Columns(c, table);
                var pk = cols.FirstOrDefault(x => x.IsPk)?.Name;
                var label = new[] { "title", "name", "full_name", "protocol_number", "visit_name" }.Select(n => cols.FirstOrDefault(x => x.Name == n)?.Name).FirstOrDefault(n => n != null)
                    ?? cols.FirstOrDefault(x => x.DataType == "varchar" && !Audit.Contains(x.Name))?.Name;
                if (pk == null || label == null) return $"{table} #{recordId}";
                return Cmd(c, $"SELECT `{label}` FROM `{table}` WHERE `{pk}`=@id", ("@id", recordId)).ExecuteScalar()?.ToString() ?? $"{table} #{recordId}";
            }
            catch { return $"{table} #{recordId}"; }
        }
    }
}
