using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Builds a NeoApps app on top of an existing database (MySQL, PostgreSQL, SQL Server or SQLite).
///
///   map:  reads the source schema and writes workspace/App/import.map.json (source table/column -> app table/column,
///         editable: rename, or "skip": true) and schema.sql that follows the NeoApps rules (snake_case,
///         <singular>_id keys, audit columns, no built-in names). Re-running keeps the names already in the map, so a
///         changed source database becomes a schema change you apply with `neoapps evolve`.
///   copy: copies the rows into the app's MySQL database. Source ids are kept, so relations hold; re-running updates
///         rows by key (insert or update), so it can run again before the switch-over.
///
/// The source connection comes from an environment variable or a SQLite file path. It is never written to a file:
/// the map keeps only the engine, host and database name.
/// </summary>
public static class DbImport
{
    public const string DefaultEnv = "NEOAPPS_SOURCE_DB";
    static readonly string[] AuditCols = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive", "created_by_user", "modified_by_user"];
    static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // ------------------------------------------------------------------ source model

    public sealed record SrcColumn(string Name, string Type, long? Length, int? Precision, int? Scale, bool Nullable, bool Identity);
    public sealed record SrcForeignKey(List<string> Columns, string RefTable, List<string> RefColumns);
    public sealed record SrcTable(string Name, List<SrcColumn> Columns, List<string> PrimaryKey, List<SrcForeignKey> ForeignKeys);

    public sealed record Source(string Engine, string ConnectionString, string Host, string Database, string? Schema)
    {
        /// <summary>
        /// mysql://user:pass@host:3306/db, postgres://user:pass@host:5432/db, sqlserver://user:pass@host:1433/db or
        /// sqlite:///path/file.db (or just a .db/.sqlite path). Query parameters pass through (?sslmode=require,
        /// ?TrustServerCertificate=true); ?schema=name picks the Postgres/SQL Server schema (default public/dbo).
        /// </summary>
        public static Source Parse(string value)
        {
            value = value.Trim();
            if (!value.Contains("://") && (File.Exists(value) || Regex.IsMatch(value, @"\.(db|sqlite3?|s3db)$", RegexOptions.IgnoreCase)))
                value = "sqlite://" + Path.GetFullPath(value);
            var m = Regex.Match(value, "^([a-z0-9]+)://(.*)$", RegexOptions.IgnoreCase);
            if (!m.Success) throw new ArgumentException("The source must look like postgres://user:password@host:5432/database, mysql://…, sqlserver://… or a SQLite file path.");
            var engine = m.Groups[1].Value.ToLowerInvariant() switch
            {
                "mysql" or "mariadb" => "mysql",
                "postgres" or "postgresql" or "pg" => "postgres",
                "sqlserver" or "mssql" => "sqlserver",
                "sqlite" or "file" => "sqlite",
                var e => throw new ArgumentException($"Unsupported source '{e}'. Use mysql, postgres, sqlserver or sqlite."),
            };
            if (engine == "sqlite")
            {
                var path = m.Groups[2].Value;
                if (!File.Exists(path)) throw new ArgumentException($"SQLite file not found: {path}");
                return new Source(engine, $"Data Source={path};Mode=ReadOnly", "file", Path.GetFileName(path), null);
            }
            var uri = new Uri(value);
            var user = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
            var pass = uri.UserInfo.Contains(':') ? Uri.UnescapeDataString(uri.UserInfo[(uri.UserInfo.IndexOf(':') + 1)..]) : "";
            var db = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
            if (db.Length == 0) throw new ArgumentException("Add the database name to the source address (…/database).");
            var extra = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2)).ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : "", StringComparer.OrdinalIgnoreCase);
            extra.Remove("schema", out var schema);
            DbConnectionStringBuilder b = engine switch
            {
                "mysql" => new MySqlConnectionStringBuilder { Server = uri.Host, Port = (uint)(uri.Port > 0 ? uri.Port : 3306), UserID = user, Password = pass, Database = db, AllowZeroDateTime = true, ConvertZeroDateTime = true },
                "postgres" => new Npgsql.NpgsqlConnectionStringBuilder { Host = uri.Host, Port = uri.Port > 0 ? uri.Port : 5432, Username = user, Password = pass, Database = db },
                _ => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder { DataSource = uri.Port > 0 ? $"{uri.Host},{uri.Port}" : uri.Host, UserID = user, Password = pass, InitialCatalog = db },
            };
            foreach (var (k, v) in extra) b[k] = v;
            return new Source(engine, b.ConnectionString, uri.Host, db, string.IsNullOrEmpty(schema) ? (engine == "postgres" ? "public" : engine == "sqlserver" ? "dbo" : null) : schema);
        }

        public DbConnection Open()
        {
            DbConnection c = Engine switch
            {
                "mysql" => new MySqlConnection(ConnectionString),
                "postgres" => new Npgsql.NpgsqlConnection(ConnectionString),
                "sqlserver" => new Microsoft.Data.SqlClient.SqlConnection(ConnectionString),
                _ => new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString),
            };
            c.Open();
            return c;
        }

        /// <summary>Quotes an identifier for this engine.</summary>
        public string Q(string name) => Engine switch
        {
            "mysql" => "`" + name.Replace("`", "``") + "`",
            "sqlserver" => "[" + name.Replace("]", "]]") + "]",
            _ => "\"" + name.Replace("\"", "\"\"") + "\"",
        };

        public string QTable(string table) => Schema != null && Engine is "postgres" or "sqlserver" ? Q(Schema) + "." + Q(table) : Q(table);
    }

    /// <summary>A MySQL connection string from mysql://user:pass@host:3306/db?SslMode=Required (the app database to copy into).</summary>
    public static string TargetConnection(string envName)
    {
        var v = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(v)) throw new ArgumentException($"{envName} is not set.");
        var t = Source.Parse(v);
        if (t.Engine != "mysql") throw new ArgumentException($"{envName} must be a mysql:// address (the app database).");
        return t.ConnectionString;
    }

    /// <summary>The source address: the named environment variable, or a SQLite path given directly.</summary>
    public static Source ResolveSource(string? from, string? fromEnv)
    {
        if (!string.IsNullOrWhiteSpace(from)) return Source.Parse(from);
        var name = string.IsNullOrWhiteSpace(fromEnv) ? DefaultEnv : fromEnv;
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v))
            throw new ArgumentException($"Set {name} to the source database address (e.g. export {name}='postgres://user:password@host:5432/db'), or pass --from <file.sqlite>.");
        return Source.Parse(v);
    }

    // ------------------------------------------------------------------ reading the source schema

    public static List<SrcTable> ReadSchema(Source s)
    {
        using var c = s.Open();
        return s.Engine switch
        {
            "sqlite" => ReadSqlite(c),
            "mysql" => ReadInformationSchema(c, s, "DATABASE()"),
            _ => ReadInformationSchema(c, s, "@schema"),
        };
    }

    static List<Dictionary<string, object?>> Rows(DbConnection c, string sql, params (string, object)[] ps)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in ps) { var p = cmd.CreateParameter(); p.ParameterName = n; p.Value = v; cmd.Parameters.Add(p); }
        using var r = cmd.ExecuteReader();
        var list = new List<Dictionary<string, object?>>();
        while (r.Read())
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            list.Add(d);
        }
        return list;
    }

    static string Str(object? o) => Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture) ?? "";
    static long? Num(object? o) => o == null || o is DBNull ? null : Convert.ToInt64(o);

    static List<SrcTable> ReadInformationSchema(DbConnection c, Source s, string schemaExpr)
    {
        (string, object)[] ps = schemaExpr == "@schema" ? [("@schema", s.Schema!)] : [];
        var tables = Rows(c, $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = {schemaExpr} AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME", ps)
            .Select(r => Str(r["TABLE_NAME"])).ToList();
        var identity = s.Engine switch
        {
            "mysql" => "CASE WHEN EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END",
            "postgres" => "CASE WHEN IS_IDENTITY = 'YES' OR COLUMN_DEFAULT LIKE 'nextval(%' THEN 1 ELSE 0 END",
            _ => "COLUMNPROPERTY(OBJECT_ID(QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME)), COLUMN_NAME, 'IsIdentity')",
        };
        var mysqlType = s.Engine == "mysql" ? ", COLUMN_TYPE" : "";
        var cols = Rows(c, $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE,
                {identity} AS IS_IDENT{mysqlType}
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {schemaExpr} ORDER BY TABLE_NAME, ORDINAL_POSITION", ps);
        var pks = Rows(c, $@"SELECT k.TABLE_NAME, k.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k ON k.CONSTRAINT_NAME = t.CONSTRAINT_NAME AND k.TABLE_SCHEMA = t.TABLE_SCHEMA AND k.TABLE_NAME = t.TABLE_NAME
            WHERE t.CONSTRAINT_TYPE = 'PRIMARY KEY' AND t.TABLE_SCHEMA = {schemaExpr} ORDER BY k.TABLE_NAME, k.ORDINAL_POSITION", ps);
        // foreign keys: one row per column pair, in order
        var fkSql = s.Engine switch
        {
            "mysql" => @"SELECT TABLE_NAME AS t, CONSTRAINT_NAME AS n, COLUMN_NAME AS c, REFERENCED_TABLE_NAME AS rt, REFERENCED_COLUMN_NAME AS rc
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION",
            "postgres" => @"SELECT cl.relname AS t, co.conname AS n, a.attname AS c, rcl.relname AS rt, ra.attname AS rc
                FROM pg_constraint co
                JOIN pg_class cl ON cl.oid = co.conrelid JOIN pg_namespace ns ON ns.oid = cl.relnamespace
                JOIN pg_class rcl ON rcl.oid = co.confrelid
                CROSS JOIN LATERAL unnest(co.conkey, co.confkey) WITH ORDINALITY AS k(col, rcol, ord)
                JOIN pg_attribute a ON a.attrelid = co.conrelid AND a.attnum = k.col
                JOIN pg_attribute ra ON ra.attrelid = co.confrelid AND ra.attnum = k.rcol
                WHERE co.contype = 'f' AND ns.nspname = @schema ORDER BY t, n, k.ord",
            _ => @"SELECT OBJECT_NAME(f.parent_object_id) AS t, f.name AS n, COL_NAME(fc.parent_object_id, fc.parent_column_id) AS c,
                    OBJECT_NAME(f.referenced_object_id) AS rt, COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS rc
                FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = f.object_id
                WHERE OBJECT_SCHEMA_NAME(f.parent_object_id) = @schema ORDER BY t, n, fc.constraint_column_id",
        };
        var fks = Rows(c, fkSql, ps);

        return tables.Select(t =>
        {
            var columns = cols.Where(r => Str(r["TABLE_NAME"]) == t).Select(r =>
            {
                var type = Str(r["DATA_TYPE"]).ToLowerInvariant();
                // MySQL: tinyint(1) is a boolean; enum/set keep their declaration
                if (s.Engine == "mysql" && r.TryGetValue("COLUMN_TYPE", out var ct) && Str(ct).ToLowerInvariant() is var full && (full.StartsWith("tinyint(1)") || full.StartsWith("enum") || full.StartsWith("set")))
                    type = full.StartsWith("tinyint(1)") ? "boolean" : "enum";
                return new SrcColumn(Str(r["COLUMN_NAME"]), type, Num(r["CHARACTER_MAXIMUM_LENGTH"]), (int?)Num(r["NUMERIC_PRECISION"]), (int?)Num(r["NUMERIC_SCALE"]),
                    Str(r["IS_NULLABLE"]).Equals("YES", StringComparison.OrdinalIgnoreCase), Num(r["IS_IDENT"]) == 1);
            }).ToList();
            var pk = pks.Where(r => Str(r["TABLE_NAME"]) == t).Select(r => Str(r["COLUMN_NAME"])).ToList();
            var fk = fks.Where(r => Str(r["t"]) == t).GroupBy(r => Str(r["n"]))
                .Select(g => new SrcForeignKey(g.Select(r => Str(r["c"])).ToList(), Str(g.First()["rt"]), g.Select(r => Str(r["rc"])).ToList())).ToList();
            return new SrcTable(t, columns, pk, fk);
        }).ToList();
    }

    static List<SrcTable> ReadSqlite(DbConnection c)
    {
        var tables = Rows(c, "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name").Select(r => Str(r["name"])).ToList();
        return tables.Select(t =>
        {
            var q = "\"" + t.Replace("\"", "\"\"") + "\"";
            var info = Rows(c, $"PRAGMA table_info({q})");
            var columns = info.Select(r =>
            {
                var decl = Str(r["type"]).ToLowerInvariant();
                var m = Regex.Match(decl, @"^\s*([a-z ]+?)\s*(?:\((\d+)(?:\s*,\s*(\d+))?\))?\s*$");
                var type = m.Success ? m.Groups[1].Value.Trim() : decl;
                long? len = m.Success && m.Groups[2].Success ? long.Parse(m.Groups[2].Value) : null;
                int? scale = m.Success && m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : null;
                if (type.Length == 0) type = "text";
                var isNum = type.Contains("dec") || type.Contains("numeric");
                return new SrcColumn(Str(r["name"]), type, isNum ? null : len, isNum ? (int?)len : null, scale, Num(r["notnull"]) == 0 && Num(r["pk"]) == 0, false);
            }).ToList();
            var pk = info.Where(r => Num(r["pk"]) > 0).OrderBy(r => Num(r["pk"])).Select(r => Str(r["name"])).ToList();
            // an INTEGER PRIMARY KEY is SQLite's rowid: it numbers itself
            if (pk.Count == 1 && columns.First(x => x.Name == pk[0]).Type == "integer")
                columns = columns.Select(x => x.Name == pk[0] ? x with { Identity = true } : x).ToList();
            var fk = Rows(c, $"PRAGMA foreign_key_list({q})").GroupBy(r => Num(r["id"]))
                .Select(g => new SrcForeignKey(g.OrderBy(r => Num(r["seq"])).Select(r => Str(r["from"])).ToList(), Str(g.First()["table"]),
                    g.OrderBy(r => Num(r["seq"])).Select(r => Str(r["to"])).ToList())).ToList();
            return new SrcTable(t, columns, pk, fk);
        }).ToList();
    }

    // ------------------------------------------------------------------ names and types

    public static string Snake(string name)
    {
        var s = Regex.Replace(name.Trim(), @"([a-z0-9])([A-Z])", "$1_$2");
        s = Regex.Replace(s, @"([A-Z]+)([A-Z][a-z])", "$1_$2");
        s = Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
        if (s.Length == 0) s = "col";
        if (char.IsDigit(s[0])) s = "c_" + s;
        return s.Length > 60 ? s[..60] : s;
    }

    public static string Singular(string table)
    {
        var parts = table.Split('_');
        var w = parts[^1];
        if (w.EndsWith("ies") && w.Length > 3) w = w[..^3] + "y";
        else if (Regex.IsMatch(w, "(ss|x|ch|sh)es$")) w = w[..^2];
        else if (w.EndsWith("s") && !w.EndsWith("ss") && !w.EndsWith("us") && !w.EndsWith("is") && w.Length > 1) w = w[..^1];
        parts[^1] = w;
        return string.Join('_', parts);
    }

    static string TableName(string source)
    {
        var t = Snake(source);
        return t switch
        {
            "users" or "user" => "members",
            "roles" or "role" => "member_roles",
            _ when AppPreview.Reserved.Contains(t) => "app_" + t,
            _ => t,
        };
    }

    static string ColumnName(string source)
    {
        var c = Snake(source);
        if (AuditCols.Any(a => a.Equals(c, StringComparison.OrdinalIgnoreCase) || Snake(a) == c)) return "source_" + c;
        if (AppPreview.SqlWords.Contains(c) || AppPreview.CSharpWords.Contains(c)) return c + "_value";
        return c;
    }

    /// <summary>MySQL type for a source column, or null when the column can't be shown in an app (binary data).</summary>
    public static string? MySqlType(SrcColumn c)
    {
        var t = c.Type;
        string Text(long? n) => n is null or <= 0 ? (t.Contains("max") || n == -1 ? "LONGTEXT" : "TEXT") : n <= 1000 ? $"VARCHAR({n})" : n <= 65535 ? "TEXT" : "LONGTEXT";
        string Dec() => c.Precision is > 0 and <= 65 ? $"DECIMAL({c.Precision},{Math.Min(c.Scale ?? 0, Math.Min(30, c.Precision.Value))})" : "DECIMAL(30,10)";
        if (t is "boolean" or "bool" or "bit") return "TINYINT(1)";
        if (t is "bigint" or "int8" or "bigserial" or "unsigned big int") return "BIGINT";
        if (t is "int" or "integer" or "int4" or "int2" or "smallint" or "mediumint" or "tinyint" or "serial" or "smallserial" or "year") return "INT";
        if (t is "decimal" or "numeric" or "dec") return Dec();
        if (t is "money" or "smallmoney") return "DECIMAL(19,4)";
        if (t is "real" or "float" or "float4" or "float8" or "double" or "double precision") return "DOUBLE";
        if (t is "date") return "DATE";
        if (t.StartsWith("timestamp") || t is "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset") return "DATETIME";
        if (t.StartsWith("time")) return "VARCHAR(20)";
        if (t is "uuid" or "uniqueidentifier") return "CHAR(36)";
        if (t is "json" or "jsonb") return "JSON";
        if (t is "enum" or "set") return "VARCHAR(100)";
        if (t is "bytea" or "blob" or "binary" or "varbinary" or "image" or "longblob" or "mediumblob" or "tinyblob" or "rowversion" or "timestamp_rowversion") return null;
        if (t.Contains("char") || t.Contains("text") || t is "clob" or "xml" or "citext" or "string" or "name") return Text(c.Length);
        if (t.Contains("int")) return "INT";
        return "TEXT";
    }

    static bool IsInteger(SrcColumn c) => MySqlType(c) is "INT" or "BIGINT";

    // ------------------------------------------------------------------ map

    /// <summary>
    /// Builds (or refreshes) the import map from the source schema. Names already in <paramref name="existing"/> are kept,
    /// so edits survive and a changed source becomes an ordinary schema change.
    /// </summary>
    public static JsonObject BuildMap(Source s, List<SrcTable> src, JsonObject? existing, List<string> warnings)
    {
        var old = (existing?["tables"] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(t => t["source"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
        var tables = new JsonArray();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var keyOf = new Dictionary<string, (string Mode, string Target, List<string> SourceKey)>(StringComparer.OrdinalIgnoreCase);

        // first pass: table names and keys
        foreach (var t in src)
        {
            old.TryGetValue(t.Name, out var prev);
            var name = prev?["target"]?.GetValue<string>() ?? TableName(t.Name);
            while (!used.Add(name)) name += "_2";
            targetOf[t.Name] = name;
            var keyName = Singular(name) + "_id";
            var mode = t.PrimaryKey.Count == 1 && IsInteger(t.Columns.First(c => c.Name == t.PrimaryKey[0])) ? "id" : t.PrimaryKey.Count > 0 ? "surrogate" : "none";
            keyOf[t.Name] = (mode, prev?["key"]?["target"]?.GetValue<string>() ?? keyName, t.PrimaryKey);
        }

        foreach (var t in src)
        {
            old.TryGetValue(t.Name, out var prev);
            var prevCols = (prev?["columns"] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(c => c["source"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
            var (mode, keyTarget, sourceKey) = keyOf[t.Name];
            var colNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { keyTarget };
            var columns = new JsonArray();
            foreach (var c in t.Columns)
            {
                prevCols.TryGetValue(c.Name, out var pc);
                var type = MySqlType(c);
                string target;
                if (mode == "id" && c.Name == sourceKey[0]) target = keyTarget;
                else
                {
                    target = pc?["target"]?.GetValue<string>() ?? ColumnName(c.Name);
                    if (target.Equals(keyTarget, StringComparison.OrdinalIgnoreCase)) target = "source_" + target;
                    while (!colNames.Add(target)) target += "_2";
                }
                var col = new JsonObject { ["source"] = c.Name, ["target"] = target, ["type"] = pc?["type"]?.GetValue<string>() ?? type ?? "(binary: skipped)", ["sourceType"] = c.Type };
                if (!c.Nullable) col["required"] = true;
                if (type == null || pc?["skip"]?.GetValue<bool>() == true) col["skip"] = true;
                if (type == null && pc == null) warnings.Add($"{t.Name}.{c.Name}: binary data ({c.Type}) is skipped; apps keep files as attachments instead.");
                columns.Add(col);
            }
            var fks = new JsonArray();
            foreach (var fk in t.ForeignKeys)
            {
                if (!targetOf.ContainsKey(fk.RefTable)) { warnings.Add($"{t.Name}: relation to {fk.RefTable} skipped (that table isn't in this database/schema)."); continue; }
                var refKey = keyOf[fk.RefTable];
                if (fk.Columns.Count != 1 || refKey.Mode != "id" || !refKey.SourceKey.SequenceEqual(fk.RefColumns, StringComparer.OrdinalIgnoreCase))
                {
                    warnings.Add($"{t.Name}({string.Join(", ", fk.Columns)}) -> {fk.RefTable}: kept as plain values, not a link (only single whole-number keys become links).");
                    continue;
                }
                var col = columns.OfType<JsonObject>().First(x => x["source"]!.GetValue<string>().Equals(fk.Columns[0], StringComparison.OrdinalIgnoreCase));
                if (!IsInteger(t.Columns.First(x => x.Name.Equals(fk.Columns[0], StringComparison.OrdinalIgnoreCase)))) continue;
                fks.Add(new JsonObject { ["column"] = col["target"]!.GetValue<string>(), ["refTable"] = targetOf[fk.RefTable], ["refColumn"] = refKey.Target });
            }
            if (mode == "surrogate") warnings.Add($"{t.Name}: its key ({string.Join(", ", sourceKey)}) isn't a single whole number, so the app adds {keyTarget} and keeps the old key unique (re-runs update by it).");
            if (mode == "none") warnings.Add($"{t.Name}: no primary key; each copy replaces all its rows.");
            var key = new JsonObject { ["mode"] = mode, ["target"] = keyTarget, ["source"] = new JsonArray(sourceKey.Select(k => (JsonNode)JsonValue.Create(k)!).ToArray()) };
            var table = new JsonObject { ["source"] = t.Name, ["target"] = targetOf[t.Name], ["key"] = key, ["columns"] = columns, ["foreignKeys"] = fks };
            if (prev?["skip"]?.GetValue<bool>() == true) table["skip"] = true;
            tables.Add(table);
        }
        foreach (var gone in old.Keys.Where(k => !src.Any(t => t.Name.Equals(k, StringComparison.OrdinalIgnoreCase))))
            warnings.Add($"{gone}: no longer in the source; removed from the map (the app's table stays until you drop it).");

        var active = tables.OfType<JsonObject>().Count(t => t["skip"]?.GetValue<bool>() != true);
        if (active > 15) warnings.Add($"{active} tables: apps work best with 3-15. Set \"skip\": true on tables the app doesn't need.");
        return new JsonObject
        {
            ["source"] = new JsonObject { ["engine"] = s.Engine, ["host"] = s.Host, ["database"] = s.Database, ["schema"] = s.Schema },
            ["mappedAt"] = DateTime.UtcNow.ToString("u"),
            ["tables"] = tables,
        };
    }

    static readonly Regex SafeName = new("^[a-z][a-z0-9_]{0,63}$");
    static readonly Regex SafeType = new(@"^[A-Z]+(\(\d+(,\d+)?\))?$");

    /// <summary>The map can be edited by hand: app names and types must be plain before they go into SQL.</summary>
    public static void Validate(JsonObject map)
    {
        foreach (var t in Active(map))
        {
            var names = new[] { G(t["target"]), G(t["key"]?["target"]) }.Concat(ActiveColumns(t).Select(c => G(c["target"])))
                .Concat((t["foreignKeys"] as JsonArray ?? []).OfType<JsonObject>().SelectMany(f => new[] { G(f["column"]), G(f["refTable"]), G(f["refColumn"]) }));
            if (names.FirstOrDefault(n => !SafeName.IsMatch(n)) is { } bad)
                throw new InvalidOperationException($"import.map.json: '{bad}' in {G(t["source"])} isn't a valid name (lowercase letters, digits and _, starting with a letter).");
            if (ActiveColumns(t).FirstOrDefault(c => !SafeType.IsMatch(G(c["type"]))) is { } badType)
                throw new InvalidOperationException($"import.map.json: {G(t["source"])}.{G(badType["source"])} has type '{G(badType["type"])}'; use a MySQL type like VARCHAR(100), INT or DECIMAL(10,2), or set \"skip\": true.");
        }
    }

    static IEnumerable<JsonObject> Active(JsonObject map) => (map["tables"] as JsonArray ?? []).OfType<JsonObject>().Where(t => t["skip"]?.GetValue<bool>() != true);
    static IEnumerable<JsonObject> ActiveColumns(JsonObject t) => (t["columns"] as JsonArray ?? []).OfType<JsonObject>().Where(c => c["skip"]?.GetValue<bool>() != true);
    static string G(JsonNode? n) => n?.GetValue<string>() ?? "";

    /// <summary>Tables ordered so each comes after the tables it links to (self-links and cycles keep source order).</summary>
    static List<JsonObject> Ordered(JsonObject map)
    {
        var list = Active(map).ToList();
        var names = list.Select(t => G(t["target"])).ToHashSet();
        var done = new List<JsonObject>(); var seen = new HashSet<string>();
        void Visit(JsonObject t, HashSet<string> path)
        {
            var n = G(t["target"]);
            if (seen.Contains(n) || !path.Add(n)) return;
            foreach (var fk in (t["foreignKeys"] as JsonArray ?? []).OfType<JsonObject>())
                if (names.Contains(G(fk["refTable"])) && G(fk["refTable"]) != n) Visit(list.First(x => G(x["target"]) == G(fk["refTable"])), path);
            if (seen.Add(n)) done.Add(t);
        }
        foreach (var t in list) Visit(t, new HashSet<string>());
        return done;
    }

    /// <summary>schema.sql for the app, following the NeoApps rules.</summary>
    public static string SchemaSql(JsonObject map)
    {
        Validate(map);
        var sb = new StringBuilder();
        sb.AppendLine($"-- Generated by `neoapps import-db` from {G(map["source"]?["engine"])} database '{G(map["source"]?["database"])}'. Edit import.map.json and re-run instead of editing this file.");
        var active = Active(map).Select(t => G(t["target"])).ToHashSet();
        foreach (var t in Ordered(map))
        {
            var key = t["key"]!; var keyTarget = G(key["target"]); var mode = G(key["mode"]);
            var lines = new List<string> { $"  {keyTarget} INT AUTO_INCREMENT PRIMARY KEY" };
            var keyCol = mode == "id" ? ActiveColumns(t).FirstOrDefault(c => G(c["target"]) == keyTarget) : null;
            if (keyCol != null && G(keyCol["type"]) == "BIGINT") lines[0] = $"  {keyTarget} BIGINT AUTO_INCREMENT PRIMARY KEY";
            foreach (var c in ActiveColumns(t).Where(c => G(c["target"]) != keyTarget))
                lines.Add($"  {G(c["target"])} {G(c["type"])}{(c["required"]?.GetValue<bool>() == true ? " NOT NULL" : "")}");
            lines.Add("  createdBy VARCHAR(255) NOT NULL");
            lines.Add("  modifiedBy VARCHAR(255) NOT NULL");
            lines.Add("  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            lines.Add("  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            lines.Add("  isActive TINYINT(1) NOT NULL DEFAULT '1'");
            if (mode == "surrogate")
            {
                var uk = KeyTargets(t);
                if (uk.Count > 0) lines.Add($"  UNIQUE KEY uq_{G(t["target"])}_source ({string.Join(", ", uk)})");
            }
            foreach (var fk in (t["foreignKeys"] as JsonArray ?? []).OfType<JsonObject>().Where(f => active.Contains(G(f["refTable"]))))
                lines.Add($"  FOREIGN KEY ({G(fk["column"])}) REFERENCES {G(fk["refTable"])}({G(fk["refColumn"])})");
            sb.AppendLine().AppendLine($"CREATE TABLE {G(t["target"])} (").AppendLine(string.Join(",\n", lines)).AppendLine(");");
        }
        return sb.ToString();
    }

    /// <summary>App column names of the source key (surrogate tables).</summary>
    static List<string> KeyTargets(JsonObject t) => (t["key"]!["source"] as JsonArray ?? []).Select(k => G(k))
        .Select(k => ActiveColumns(t).FirstOrDefault(c => G(c["source"]).Equals(k, StringComparison.OrdinalIgnoreCase)))
        .Where(c => c != null && !G(c!["type"]).Contains("TEXT") && G(c!["type"]) != "JSON").Select(c => G(c!["target"])).ToList();

    /// <summary>A starter ui.config.json: plain-language labels and the field that names each record.</summary>
    public static JsonObject StarterUi(string app, JsonObject map)
    {
        string Title(string s) => string.Join(' ', s.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        var tables = new JsonObject();
        foreach (var t in Active(map))
        {
            var target = G(t["target"]);
            var cols = ActiveColumns(t).ToList();
            // the field that names a record: a name or title; otherwise leave it to the app (it shows the number)
            var display = new[] { "name", "title", "full_name", "label", "email" }
                .Select(n => cols.FirstOrDefault(c => G(c["target"]) == n || G(c["target"]).EndsWith("_" + n)))
                .FirstOrDefault(c => c != null && G(c!["type"]).StartsWith("VARCHAR"));
            var entry = new JsonObject { ["label"] = Title(target.EndsWith("s") ? target : target + "s"), ["singular"] = Title(Singular(target)) };
            if (display != null) entry["displayField"] = G(display["target"]);
            var listCols = cols.Where(c => G(c["target"]) != G(t["key"]!["target"]) && !G(c["type"]).Contains("TEXT") && G(c["type"]) != "JSON").Take(6).Select(c => (JsonNode)JsonValue.Create(G(c["target"]))!).ToArray();
            if (listCols.Length > 0) entry["listColumns"] = new JsonArray(listCols);
            tables[target] = entry;
        }
        return new JsonObject { ["appName"] = Regex.Replace(app, "([a-z])([A-Z])", "$1 $2"), ["tables"] = tables };
    }

    // ------------------------------------------------------------------ copy

    public sealed record TableCount(string Table, long Source, long App);

    /// <summary>
    /// Copies every mapped table into the app database. Ids are kept, so links hold; existing rows are updated by key.
    /// Rows belong to the workspace owner (rows are owner-scoped); created_by_user says "import".
    /// </summary>
    public static List<TableCount> Copy(Source s, JsonObject map, string targetConnection, string? owner, Action<string> log, CancellationToken ct, int batch = 500)
    {
        Validate(map);
        using var src = s.Open();
        using var dst = new MySqlConnection(targetConnection);
        dst.Open();
        // rows belong to the app's workspace owner: the owner_name the first member signs in with
        if (Scalar(dst, @"SELECT p.owner_name FROM permissionmatrix p JOIN users u ON u.user_id = p.user_id
                WHERE p.owner_name IS NOT NULL AND p.owner_name <> '' ORDER BY u.user_id LIMIT 1") is string o && o.Length > 0) owner = o;
        if (string.IsNullOrEmpty(owner)) throw new InvalidOperationException("Couldn't find the app's owner in the target database.");
        Exec(dst, "SET SESSION foreign_key_checks = 0");
        var result = new List<TableCount>();
        var appColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in Rows(dst, "SELECT TABLE_NAME, COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()"))
            (appColumns.TryGetValue(Str(r["TABLE_NAME"]), out var set) ? set : appColumns[Str(r["TABLE_NAME"])] = new(StringComparer.OrdinalIgnoreCase)).Add(Str(r["COLUMN_NAME"]));
        try
        {
            foreach (var t in Ordered(map))
            {
                ct.ThrowIfCancellationRequested();
                var target = G(t["target"]);
                if (!appColumns.TryGetValue(target, out var have))
                    throw new InvalidOperationException($"The app has no table '{target}'. Generate (neoapps new) or evolve the app with the import's schema.sql first.");
                var cols = ActiveColumns(t).ToList();
                var missing = cols.Where(c => !have.Contains(G(c["target"]))).Select(c => G(c["target"])).ToList();
                if (missing.Count > 0) throw new InvalidOperationException($"{target} is missing {string.Join(", ", missing)}. Apply the latest schema.sql with `neoapps evolve` first.");
                var mode = G(t["key"]!["mode"]);
                if (mode == "none") Exec(dst, $"DELETE FROM `{target}`");

                var names = cols.Select(c => G(c["target"])).ToList();
                var stamp = new List<string> { "createdBy", "modifiedBy" };
                if (have.Contains("created_by_user")) stamp.Add("created_by_user");
                if (have.Contains("modified_by_user")) stamp.Add("modified_by_user");
                var all = names.Concat(stamp).ToList();
                var updates = names.Where(n => n != G(t["key"]!["target"])).Select(n => $"`{n}` = VALUES(`{n}`)").Append("modifiedAt = CURRENT_TIMESTAMP").Append("isActive = 1");

                var cmd = src.CreateCommand();
                cmd.CommandText = $"SELECT {string.Join(", ", cols.Select(c => s.Q(G(c["source"]))))} FROM {s.QTable(G(t["source"]))}";
                var reader = cmd.ExecuteReader();
                var rows = new List<object?[]>();
                batch = Math.Max(1, Math.Min(batch, 60_000 / all.Count)); // MySQL allows 65,535 parameters per statement
                long copied = 0;
                void Flush()
                {
                    if (rows.Count == 0) return;
                    using var ins = dst.CreateCommand();
                    var sb = new StringBuilder($"INSERT INTO `{target}` ({string.Join(", ", all.Select(n => $"`{n}`"))}) VALUES ");
                    for (var i = 0; i < rows.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append('(').Append(string.Join(", ", all.Select((_, j) => $"@p{i}_{j}"))).Append(')');
                        for (var j = 0; j < names.Count; j++) ins.Parameters.AddWithValue($"@p{i}_{j}", rows[i][j]);
                        ins.Parameters.AddWithValue($"@p{i}_{names.Count}", owner);
                        ins.Parameters.AddWithValue($"@p{i}_{names.Count + 1}", owner);
                        for (var j = names.Count + 2; j < all.Count; j++) ins.Parameters.AddWithValue($"@p{i}_{j}", "import");
                    }
                    if (mode != "none") sb.Append(" ON DUPLICATE KEY UPDATE ").Append(string.Join(", ", updates));
                    ins.CommandText = sb.ToString();
                    ins.CommandTimeout = 300;
                    ins.ExecuteNonQuery();
                    copied += rows.Count;
                    rows.Clear();
                }
                while (reader.Read())
                {
                    var row = new object?[cols.Count];
                    for (var i = 0; i < cols.Count; i++) row[i] = Value(reader, i, G(cols[i]["type"]));
                    rows.Add(row);
                    if (rows.Count >= batch) Flush();
                }
                Flush();
                reader.Dispose(); cmd.Dispose(); // one open command per connection (Postgres, SQL Server)
                long srcCount;
                using (var cnt = src.CreateCommand()) { cnt.CommandText = $"SELECT COUNT(*) FROM {s.QTable(G(t["source"]))}"; srcCount = Convert.ToInt64(cnt.ExecuteScalar()); }
                var appCount = Convert.ToInt64(Scalar(dst, $"SELECT COUNT(*) FROM `{target}`"));
                log($"{target}: {copied} row(s) copied ({srcCount} in source, {appCount} in app)");
                result.Add(new TableCount(target, srcCount, appCount));
            }
            // links that point nowhere (the source had them too, or a table was skipped)
            foreach (var t in Active(map))
                foreach (var fk in (t["foreignKeys"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    var n = Convert.ToInt64(Scalar(dst, $@"SELECT COUNT(*) FROM `{G(t["target"])}` c LEFT JOIN `{G(fk["refTable"])}` p ON p.`{G(fk["refColumn"])}` = c.`{G(fk["column"])}`
                        WHERE c.`{G(fk["column"])}` IS NOT NULL AND p.`{G(fk["refColumn"])}` IS NULL"));
                    if (n > 0) log($"warning: {G(t["target"])}.{G(fk["column"])}: {n} row(s) point to a missing {G(fk["refTable"])}");
                }
        }
        finally { Exec(dst, "SET SESSION foreign_key_checks = 1"); }
        return result;
    }

    static object? Value(DbDataReader r, int i, string type)
    {
        if (r.IsDBNull(i)) return DBNull.Value;
        var v = r.GetValue(i);
        return v switch
        {
            DateTimeOffset d => d.UtcDateTime,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            TimeOnly t => t.ToString("HH:mm:ss.FFFFFFF"),
            TimeSpan t => t.ToString(),
            Guid g => g.ToString(),
            bool b when type.StartsWith("TINYINT") => b ? 1 : 0,
            bool b => b ? "true" : "false",
            string str when type == "DATETIME" && DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt) => dt,
            Array a when v is not byte[] => JsonSerializer.Serialize(a), // Postgres arrays
            _ when type == "JSON" && v is not string => JsonSerializer.Serialize(v),
            _ => v,
        };
    }

    static void Exec(MySqlConnection c, string sql) { using var cmd = new MySqlCommand(sql, c); cmd.ExecuteNonQuery(); }
    static object? Scalar(MySqlConnection c, string sql) { using var cmd = new MySqlCommand(sql, c); return cmd.ExecuteScalar(); }

    public static string MapJson(JsonObject map) => map.ToJsonString(Pretty);
}
