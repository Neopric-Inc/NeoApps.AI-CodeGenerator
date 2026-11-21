using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace NeoApps.Mcp.Services;

/// <summary>
/// Puts a generated app's database on Azure SQL (SQL Server). The generator always builds the app against a scratch MySQL
/// database; this copies that database's exact definition (tables, types, defaults, keys, indexes, foreign keys) and rows
/// (ids kept) to SQL Server as a setup script the app runs on first start. Used by `neoapps new --db azuresql`.
/// </summary>
public static class AzureSqlTarget
{
    public sealed record Col(string Name, string DataType, string ColumnType, bool Nullable, string? Default, bool DefaultIsExpr, bool AutoIncrement,
        long? Length, int? Precision, int? Scale, int? DateTimePrecision);
    public sealed record Index(string Name, bool Unique, List<string> Columns);
    public sealed record Fk(string Name, List<string> Columns, string RefTable, List<string> RefColumns, string DeleteRule);
    public sealed record Table(string Name, List<Col> Columns, List<string> PrimaryKey, List<Index> Indexes, List<Fk> ForeignKeys);

    /// <summary>Reads the MySQL database's tables (the connection's current database).</summary>
    public static List<Table> ReadMySql(string mysqlConnection)
    {
        using var c = new MySqlConnection(mysqlConnection);
        c.Open();
        var tables = new Dictionary<string, Table>(StringComparer.OrdinalIgnoreCase);
        using (var q = new MySqlCommand(@"SELECT c.table_name, c.column_name, c.data_type, c.column_type, c.is_nullable, c.column_default, c.extra,
                c.character_maximum_length, c.numeric_precision, c.numeric_scale, c.datetime_precision
              FROM information_schema.columns c JOIN information_schema.tables t ON t.table_schema = c.table_schema AND t.table_name = c.table_name
              WHERE c.table_schema = DATABASE() AND t.table_type = 'BASE TABLE' ORDER BY c.table_name, c.ordinal_position", c))
        using (var r = q.ExecuteReader())
            while (r.Read())
            {
                var t = r.GetString(0);
                if (!tables.TryGetValue(t, out var table)) tables[t] = table = new Table(t, new(), new(), new(), new());
                var extra = r.IsDBNull(6) ? "" : r.GetString(6).ToLowerInvariant();
                table.Columns.Add(new Col(r.GetString(1), r.GetString(2).ToLowerInvariant(), r.GetString(3).ToLowerInvariant(), r.GetString(4) == "YES",
                    r.IsDBNull(5) ? null : r.GetString(5), extra.Contains("default_generated"), extra.Contains("auto_increment"),
                    r.IsDBNull(7) ? null : System.Convert.ToInt64(r.GetValue(7)), r.IsDBNull(8) ? null : System.Convert.ToInt32(r.GetValue(8)),
                    r.IsDBNull(9) ? null : System.Convert.ToInt32(r.GetValue(9)), r.IsDBNull(10) ? null : System.Convert.ToInt32(r.GetValue(10))));
            }
        using (var q = new MySqlCommand(@"SELECT table_name, index_name, non_unique, column_name FROM information_schema.statistics
              WHERE table_schema = DATABASE() AND index_type = 'BTREE' ORDER BY table_name, index_name, seq_in_index", c))
        using (var r = q.ExecuteReader())
            while (r.Read())
            {
                if (!tables.TryGetValue(r.GetString(0), out var table)) continue;
                var name = r.GetString(1);
                if (name == "PRIMARY") { table.PrimaryKey.Add(r.GetString(3)); continue; }
                var ix = table.Indexes.FirstOrDefault(i => i.Name == name);
                if (ix == null) table.Indexes.Add(ix = new Index(name, System.Convert.ToInt32(r.GetValue(2)) == 0, new()));
                ix.Columns.Add(r.GetString(3));
            }
        using (var q = new MySqlCommand(@"SELECT k.table_name, k.constraint_name, k.column_name, k.referenced_table_name, k.referenced_column_name, rc.delete_rule
              FROM information_schema.key_column_usage k JOIN information_schema.referential_constraints rc
                ON rc.constraint_schema = k.constraint_schema AND rc.constraint_name = k.constraint_name AND rc.table_name = k.table_name
              WHERE k.table_schema = DATABASE() AND k.referenced_table_name IS NOT NULL ORDER BY k.table_name, k.constraint_name, k.ordinal_position", c))
        using (var r = q.ExecuteReader())
            while (r.Read())
            {
                if (!tables.TryGetValue(r.GetString(0), out var table)) continue;
                var name = r.GetString(1);
                var fk = table.ForeignKeys.FirstOrDefault(f => f.Name == name);
                if (fk == null) table.ForeignKeys.Add(fk = new Fk(name, new(), r.GetString(3), new(), r.GetString(5)));
                fk.Columns.Add(r.GetString(2)); fk.RefColumns.Add(r.GetString(4));
            }
        return tables.Values.ToList();
    }

    // ---- types ------------------------------------------------------------------------------------------------------

    public static string SqlType(Col c)
    {
        string N(long? len) => len is > 0 and <= 4000 ? $"({len})" : "(MAX)";
        return c.DataType switch
        {
            "tinyint" when c.ColumnType.StartsWith("tinyint(1)") => "BIT",
            "bit" when c.ColumnType is "bit(1)" => "BIT",
            "bit" => "BIGINT",
            "tinyint" => c.ColumnType.Contains("unsigned") ? "TINYINT" : "SMALLINT", // MySQL tinyint is signed (-128..127)
            "smallint" => c.ColumnType.Contains("unsigned") ? "INT" : "SMALLINT",
            "mediumint" or "int" or "integer" => c.ColumnType.Contains("unsigned") ? "BIGINT" : "INT",
            "bigint" => c.ColumnType.Contains("unsigned") ? "DECIMAL(20,0)" : "BIGINT",
            "decimal" or "numeric" => $"DECIMAL({c.Precision ?? 18},{c.Scale ?? 0})",
            "float" => "REAL",
            "double" or "real" => "FLOAT",
            "varchar" => "NVARCHAR" + N(c.Length),
            "char" => c.Length is > 0 and <= 4000 ? $"NCHAR({c.Length})" : "NVARCHAR(MAX)",
            "tinytext" or "text" or "mediumtext" or "longtext" or "json" => "NVARCHAR(MAX)",
            "enum" or "set" => "NVARCHAR(255)",
            "date" => "DATE",
            "time" => "TIME(0)",
            "datetime" or "timestamp" => $"DATETIME2({Math.Clamp(c.DateTimePrecision ?? 0, 0, 7)})",
            "year" => "SMALLINT",
            "binary" => c.Length is > 0 and <= 8000 ? $"BINARY({c.Length})" : "VARBINARY(MAX)",
            "varbinary" => c.Length is > 0 and <= 8000 ? $"VARBINARY({c.Length})" : "VARBINARY(MAX)",
            "tinyblob" or "blob" or "mediumblob" or "longblob" => "VARBINARY(MAX)",
            "geometry" or "point" or "linestring" or "polygon" => "VARBINARY(MAX)",
            _ => "NVARCHAR(MAX)",
        };
    }

    static string? SqlDefault(Col c)
    {
        if (c.Default == null || c.AutoIncrement) return null;
        var d = c.Default.Trim();
        if (d.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
        if (d.StartsWith("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase) || d.StartsWith("now(", StringComparison.OrdinalIgnoreCase)) return "SYSUTCDATETIME()";
        if (c.DefaultIsExpr) return null; // other expressions: the app always sets the value itself
        if (c.DataType is "tinyint" or "smallint" or "mediumint" or "int" or "integer" or "bigint" or "decimal" or "numeric" or "float" or "double" or "bit")
            return decimal.TryParse(d.Trim('\'').Replace("b'", "").TrimEnd('\''), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n)
                ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        return "N'" + d.Trim('\'').Replace("'", "''") + "'";
    }

    static string Q(string n) => "[" + n.Replace("]", "]]") + "]";

    static bool Indexable(string type) => !type.EndsWith("(MAX)") && type != "NVARCHAR(MAX)";

    static int KeyBytes(string type)
    {
        var m = System.Text.RegularExpressions.Regex.Match(type, @"^N(VAR)?CHAR\((\d+)\)$");
        if (m.Success) return int.Parse(m.Groups[2].Value) * 2;
        m = System.Text.RegularExpressions.Regex.Match(type, @"^(VAR)?BINARY\((\d+)\)$");
        if (m.Success) return int.Parse(m.Groups[2].Value);
        return 8;
    }

    public static string CreateTableSql(Table t, List<string> warnings)
    {
        var sb = new StringBuilder($"CREATE TABLE {Q(t.Name)} (\n");
        var parts = new List<string>();
        foreach (var c in t.Columns)
        {
            var type = SqlType(c);
            var def = SqlDefault(c);
            parts.Add($"  {Q(c.Name)} {type}{(c.AutoIncrement ? " IDENTITY(1,1)" : "")} {(c.Nullable ? "NULL" : "NOT NULL")}" +
                      (def != null ? $" CONSTRAINT {Q($"DF_{t.Name}_{c.Name}")} DEFAULT {def}" : ""));
        }
        if (t.PrimaryKey.Count > 0) parts.Add($"  CONSTRAINT {Q("PK_" + t.Name)} PRIMARY KEY ({string.Join(", ", t.PrimaryKey.Select(Q))})");
        sb.Append(string.Join(",\n", parts)).Append("\n)");
        return sb.ToString();
    }

    public static List<string> IndexSql(Table t, List<string> warnings)
    {
        var list = new List<string>();
        var types = t.Columns.ToDictionary(c => c.Name, SqlType, StringComparer.OrdinalIgnoreCase);
        foreach (var ix in t.Indexes)
        {
            var colTypes = ix.Columns.Select(c => types.GetValueOrDefault(c, "NVARCHAR(MAX)")).ToList();
            if (colTypes.Any(x => !Indexable(x))) { warnings.Add($"{t.Name}: index {ix.Name} skipped (long text column can't be indexed on SQL Server)."); continue; }
            if (colTypes.Sum(KeyBytes) > 1700) { warnings.Add($"{t.Name}: index {ix.Name} skipped (key longer than SQL Server's 1700 bytes)."); continue; }
            // A unique index on nullable columns: SQL Server allows one NULL only, MySQL many; filter NULLs out to match MySQL.
            var nullable = ix.Columns.Where(c => t.Columns.First(x => x.Name.Equals(c, StringComparison.OrdinalIgnoreCase)).Nullable).ToList();
            var filter = ix.Unique && nullable.Count > 0 ? " WHERE " + string.Join(" AND ", nullable.Select(c => Q(c) + " IS NOT NULL")) : "";
            list.Add($"CREATE {(ix.Unique ? "UNIQUE " : "")}INDEX {Q($"IX_{t.Name}_{ix.Name}")} ON {Q(t.Name)} ({string.Join(", ", ix.Columns.Select(Q))}){filter}");
        }
        return list;
    }

    public static string FkSql(Table t, Fk fk, bool cascade) =>
        $"ALTER TABLE {Q(t.Name)} ADD CONSTRAINT {Q($"FK_{t.Name}_{fk.Name}")} FOREIGN KEY ({string.Join(", ", fk.Columns.Select(Q))}) " +
        $"REFERENCES {Q(fk.RefTable)} ({string.Join(", ", fk.RefColumns.Select(Q))})" +
        (cascade ? fk.DeleteRule switch { "CASCADE" => " ON DELETE CASCADE", "SET NULL" => " ON DELETE SET NULL", _ => "" } : "");

    // ---- setup script -------------------------------------------------------------------------------------------

    /// <summary>
    /// The T-SQL that creates every table of the MySQL database on SQL Server and loads its rows (ids kept), then adds
    /// indexes and links. Batches are separated by lines holding only "GO". The generated app runs it itself on first start
    /// (Db.InitializeIfEmpty), with its own identity, so nothing outside the app needs access to its database.
    /// </summary>
    public static string Script(string mysqlConnection, List<string> warnings)
    {
        var tables = ReadMySql(mysqlConnection);
        var sb = new StringBuilder("-- NeoApps: database setup for SQL Server / Azure SQL. Run once on an empty database.\n");
        foreach (var t in tables) sb.Append(CreateTableSql(t, warnings)).Append("\nGO\n");
        using (var my = new MySqlConnection(mysqlConnection))
        {
            my.Open();
            foreach (var t in tables) AppendRows(sb, my, t);
        }
        foreach (var t in tables)
            foreach (var ix in IndexSql(t, warnings)) sb.Append(ix).Append("\nGO\n");
        foreach (var t in tables)
            foreach (var fk in t.ForeignKeys)
                // SQL Server refuses some cascades (several paths, error 1785): keep the link without the cascade then
                sb.Append($"BEGIN TRY {FkSql(t, fk, cascade: true)} END TRY BEGIN CATCH {FkSql(t, fk, cascade: false)} END CATCH\nGO\n");
        return sb.ToString();
    }

    static void AppendRows(StringBuilder sb, MySqlConnection my, Table t)
    {
        using var q = new MySqlCommand($"SELECT {string.Join(", ", t.Columns.Select(c => "`" + c.Name + "`"))} FROM `{t.Name}`", my);
        using var r = q.ExecuteReader();
        var types = t.Columns.Select(c => ClrType(SqlType(c))).ToArray();
        var identity = t.Columns.Any(c => c.AutoIncrement);
        var head = $"INSERT INTO {Q(t.Name)} ({string.Join(", ", t.Columns.Select(c => Q(c.Name)))}) VALUES\n";
        var rows = new List<string>();
        void Flush()
        {
            if (rows.Count == 0) return;
            if (identity) sb.Append($"SET IDENTITY_INSERT {Q(t.Name)} ON;\n");
            sb.Append(head).Append(string.Join(",\n", rows)).Append(";\n");
            if (identity) sb.Append($"SET IDENTITY_INSERT {Q(t.Name)} OFF;\n");
            sb.Append("GO\n");
            rows.Clear();
        }
        while (r.Read())
        {
            var vals = new string[t.Columns.Count];
            for (var i = 0; i < vals.Length; i++) vals[i] = r.IsDBNull(i) ? "NULL" : Literal(ToTarget(r.GetValue(i), types[i]));
            rows.Add("(" + string.Join(", ", vals) + ")");
            if (rows.Count == 500) Flush(); // SQL Server allows up to 1000 rows per VALUES list
        }
        Flush();
    }

    static string Literal(object v) => v switch
    {
        null or DBNull => "NULL",
        bool b => b ? "1" : "0",
        string s => "N'" + s.Replace("'", "''") + "'",
        DateTime d => "'" + d.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture) + "'",
        TimeSpan ts => "'" + ts.ToString(@"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture) + "'",
        byte[] bytes => "0x" + Convert.ToHexString(bytes),
        double dbl => dbl.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        IFormattable n => n.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => "N'" + v.ToString()!.Replace("'", "''") + "'",
    };

    /// <summary>Drops every table of the SQL Server database (a fresh `neoapps new`); the app then sets itself up again.</summary>
    public static int Reset(string sqlServerConnection, Action<string> log)
    {
        using var s = new SqlConnection(sqlServerConnection);
        OpenWithRetry(s, log);
        using var count = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID()", s);
        var n = Convert.ToInt32(count.ExecuteScalar());
        if (n == 0) return 0;
        Exec(s, @"DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
                           N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' FROM sys.foreign_keys WHERE schema_id = SCHEMA_ID();
            EXEC sp_executesql @sql;
            SET @sql = N'';
            SELECT @sql += N'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';' FROM sys.tables WHERE schema_id = SCHEMA_ID();
            EXEC sp_executesql @sql;");
        return n;
    }

    static Type ClrType(string sqlType) => sqlType switch
    {
        "BIT" => typeof(bool),
        "TINYINT" => typeof(byte),
        "SMALLINT" => typeof(short),
        "INT" => typeof(int),
        "BIGINT" => typeof(long),
        "REAL" => typeof(float),
        "FLOAT" => typeof(double),
        "DATE" => typeof(DateTime),
        "TIME(0)" => typeof(TimeSpan),
        _ when sqlType.StartsWith("DECIMAL") => typeof(decimal),
        _ when sqlType.StartsWith("DATETIME2") => typeof(DateTime),
        _ when sqlType.Contains("BINARY") => typeof(byte[]),
        _ => typeof(string),
    };

    static object ToTarget(object v, Type target)
    {
        if (target.IsInstanceOfType(v)) return v;
        if (target == typeof(string)) return v switch
        {
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            byte[] b => Encoding.UTF8.GetString(b),
            _ => System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "",
        };
        if (target == typeof(bool)) return System.Convert.ToInt64(v) != 0;
        if (target == typeof(TimeSpan)) return v is TimeSpan ts ? ts : TimeSpan.Parse(v.ToString()!);
        if (target == typeof(DateTime) && v is MySqlDateTime md) return md.IsValidDateTime ? md.GetDateTime() : DBNull.Value;
        return System.Convert.ChangeType(v, target, System.Globalization.CultureInfo.InvariantCulture);
    }

    static void Exec(SqlConnection s, string sql)
    {
        using var cmd = new SqlCommand(sql, s) { CommandTimeout = 300 };
        cmd.ExecuteNonQuery();
    }

    /// <summary>Azure SQL serverless databases pause when idle; the first connection wakes them and can fail while resuming.</summary>
    public static void OpenWithRetry(SqlConnection s, Action<string> log)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { s.Open(); return; }
            catch (SqlException e) when (attempt < 6 && e.Number is 40613 or 40197 or 40501 or 49918 or 49919 or 49920 or 4060 or -2)
            {
                log($"Azure SQL is waking up (attempt {attempt}), retrying in 15s...");
                Thread.Sleep(15000);
            }
        }
    }
}
