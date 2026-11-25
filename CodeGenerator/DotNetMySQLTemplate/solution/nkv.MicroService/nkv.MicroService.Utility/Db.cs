using Microsoft.Data.SqlClient;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace nkv.MicroService.Utility
{
    public enum DbKind { MySql, SqlServer }

    /// <summary>
    /// The one place that knows which database the app runs on (Database:Provider = mysql | azuresql).
    /// Everything that differs between MySQL and SQL Server (Azure SQL) goes through here; on MySQL every helper returns
    /// exactly the SQL the app used before.
    /// </summary>
    public static class Db
    {
        public static DbKind Kind { get; private set; } = DbKind.MySql;
        public static bool IsSqlServer => Kind == DbKind.SqlServer;

        public static void Configure(string provider) =>
            Kind = (provider ?? "").Trim().ToLowerInvariant() switch
            {
                "azuresql" or "sqlserver" or "mssql" => DbKind.SqlServer,
                "" or "mysql" => DbKind.MySql,
                var p => throw new ArgumentException($"Unknown Database:Provider '{p}'. Use mysql or azuresql."),
            };

        public static DbConnection Connection(string connectionString) =>
            IsSqlServer ? new SqlConnection(connectionString) { RetryLogicProvider = ResumeRetry } : new MySqlConnection(connectionString);

        /// <summary>
        /// Azure SQL serverless pauses when idle; the first login after that fails with 40613 while it resumes (about a
        /// minute). Opening a connection retries those transient errors for up to ~2 minutes. Commands are not retried.
        /// </summary>
        static readonly SqlRetryLogicBaseProvider ResumeRetry = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
        {
            NumberOfTries = 7,
            DeltaTime = TimeSpan.FromSeconds(4),
            MaxTimeInterval = TimeSpan.FromSeconds(30),
            TransientErrors = new[] { 40613, 40197, 40501, 49918, 49919, 49920, 4060, 4221, 10928, 10929, 40540, 233, 64, 10053, 10054, 10060 },
        });

        /// <summary>
        /// Background work that should run after writes instead of polling. A serverless database pauses only when no
        /// session or query touches it for the auto-pause delay, so a loop that checks the database every few seconds
        /// keeps it (and its bill) running all day. On MySQL the old timers stay.
        /// </summary>
        public static bool PauseFriendly => IsSqlServer;

        /// <summary>The database name in a connection string (used to name queues and channels per app).</summary>
        public static string DatabaseName(string connectionString) =>
            IsSqlServer ? new SqlConnectionStringBuilder(connectionString).InitialCatalog : new MySqlConnectionStringBuilder(connectionString).Database;

        public static DbConnection Open(string connectionString)
        {
            var c = Connection(connectionString);
            c.Open();
            return c;
        }

        public static DbCommand Command(string sql, DbConnection connection, DbTransaction transaction = null)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (transaction != null) cmd.Transaction = transaction;
            return cmd;
        }

        public static DbCommand Command(string sql, DbConnection connection, params (string, object)[] ps)
        {
            var cmd = Command(sql, connection);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
            return cmd;
        }

        // ---- SQL pieces ---------------------------------------------------------------------------------------------

        /// <summary>Quoted identifier: `name` on MySQL, [name] on SQL Server.</summary>
        public static string Q(string name) => IsSqlServer ? "[" + name.Replace("]", "]]") + "]" : "`" + name.Replace("`", "``") + "`";

        /// <summary>Page clause after ORDER BY. SQL Server requires an ORDER BY before it.</summary>
        public static string Page(string offset, string limit) =>
            IsSqlServer ? $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY" : $"LIMIT {offset}, {limit}";

        /// <summary>First n rows, after ORDER BY (SQL Server requires one; use "ORDER BY (SELECT NULL)" when order doesn't matter).</summary>
        public static string Limit(object n) => IsSqlServer ? $"OFFSET 0 ROWS FETCH NEXT {n} ROWS ONLY" : $"LIMIT {n}";

        /// <summary>"TOP n " after SELECT on SQL Server; pair with LimitEnd(n) at the end of the statement for MySQL.</summary>
        public static string Top(object n) => IsSqlServer ? $"TOP {n} " : "";
        public static string LimitEnd(object n) => IsSqlServer ? "" : $" LIMIT {n}";
        public static string MonthStart => IsSqlServer ? "DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1)" : "DATE_FORMAT(UTC_TIMESTAMP(), '%Y-%m-01')";

        /// <summary>a equals b, where NULL equals NULL (MySQL's &lt;=&gt;).</summary>
        public static string NullSafeEq(string a, string b) => IsSqlServer ? $"({a} = {b} OR ({a} IS NULL AND {b} IS NULL))" : $"{a} <=> {b}";

        public static string Now => IsSqlServer ? "SYSDATETIME()" : "NOW()";
        public static string UtcNow => IsSqlServer ? "SYSUTCDATETIME()" : "UTC_TIMESTAMP()";
        public static string UtcNowMs => IsSqlServer ? "SYSUTCDATETIME()" : "UTC_TIMESTAMP(3)";
        public static string IfNull => IsSqlServer ? "ISNULL" : "IFNULL";
        public static string TextType => IsSqlServer ? "NVARCHAR(MAX)" : "TEXT";
        public static string LongTextType => IsSqlServer ? "NVARCHAR(MAX)" : "MEDIUMTEXT";
        public static string AutoId => IsSqlServer ? "INT IDENTITY(1,1) PRIMARY KEY" : "INT AUTO_INCREMENT PRIMARY KEY";
        public static string BigAutoId => IsSqlServer ? "BIGINT IDENTITY(1,1) PRIMARY KEY" : "BIGINT AUTO_INCREMENT PRIMARY KEY";
        public static string DateTimeType => IsSqlServer ? "DATETIME2" : "DATETIME";
        public static string BoolType => IsSqlServer ? "BIT" : "TINYINT(1)";
        public static string AsText(string expr) => IsSqlServer ? $"CAST({expr} AS NVARCHAR(100))" : $"CAST({expr} AS CHAR)";
        public static string AddSeconds(string expr, string seconds) => IsSqlServer ? $"DATEADD(SECOND, {seconds}, {expr})" : $"DATE_ADD({expr}, INTERVAL {seconds} SECOND)";

        /// <summary>
        /// CREATE TABLE only when missing. `body` is the column list (without the outer parentheses) written in MySQL syntax;
        /// on SQL Server it is translated with Ddl(). Only for the platform's own fixed table definitions.
        /// </summary>
        public static string CreateTableIfMissing(string table, string body) =>
            IsSqlServer ? $"IF OBJECT_ID(N'{table}', N'U') IS NULL CREATE TABLE {Q(table)} ({Ddl(body, table)})"
                        : $"CREATE TABLE IF NOT EXISTS {Q(table)} ({body})";

        /// <summary>Translates the platform's MySQL column definitions to SQL Server (types, identity, inline indexes).
        /// Types are only rewritten right after a column name, so a column called "text" or "json" keeps its name.</summary>
        public static string Ddl(string body, string table = "t")
        {
            if (!IsSqlServer) return body;
            var R = (Func<string, string, string, string>)((s, p, r) => System.Text.RegularExpressions.Regex.Replace(s, p, r, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            var s = body;
            s = R(s, @"\bENGINE\s*=\s*\w+", "");
            s = R(s, @",\s*FULLTEXT\s+KEY\s+\w+\s*\([^)]*\)", "");            // full-text indexes are created separately on SQL Server
            s = R(s, @"\bUNIQUE\s+KEY\s+(\w+)\s*\(", $"CONSTRAINT {table}_$1 UNIQUE (");
            s = R(s, @"(,\s*)KEY\s+(\w+)\s*\(", "$1INDEX $2 (");
            s = R(s, @"\bAUTO_INCREMENT\b", "IDENTITY(1,1)");
            s = R(s, @"\bON\s+UPDATE\s+CURRENT_TIMESTAMP(\(\d\))?", "");
            s = R(s, @"\bCURRENT_TIMESTAMP\(\d\)", "SYSDATETIME()");
            s = R(s, @"(?<=\b\w+\s+)TINYINT\(1\)", "BIT");
            s = R(s, @"(?<=\b\w+\s+)(MEDIUMTEXT|LONGTEXT|TEXT|JSON)\b", "NVARCHAR(MAX)");
            s = R(s, @"(?<=\b\w+\s+)VARCHAR\(", "NVARCHAR(");
            s = R(s, @"(?<=\b\w+\s+)DATETIME\b", "DATETIME2");
            s = R(s, @"(?<=\b\w+\s+)DOUBLE\b", "FLOAT");
            s = R(s, @"(?<=\b\w+\s+)(LONGBLOB|MEDIUMBLOB|BLOB)\b", "VARBINARY(MAX)");
            return s;
        }

        /// <summary>Insert, or update the given columns when the key already exists. keyCols must form a primary/unique key.</summary>
        public static string Upsert(string table, IList<string> cols, IList<string> keyCols, IList<string> updateCols = null)
        {
            updateCols ??= cols.Where(c => !keyCols.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            if (!IsSqlServer)
                return $"INSERT INTO {Q(table)} ({string.Join(", ", cols.Select(Q))}) VALUES ({string.Join(", ", cols.Select(c => "@" + c))})" +
                       (updateCols.Count > 0 ? $" ON DUPLICATE KEY UPDATE {string.Join(", ", updateCols.Select(c => $"{Q(c)} = VALUES({Q(c)})"))}"
                                             : $" ON DUPLICATE KEY UPDATE {Q(keyCols[0])} = {Q(keyCols[0])}");
            return $"MERGE {Q(table)} WITH (HOLDLOCK) AS t USING (SELECT {string.Join(", ", cols.Select(c => $"@{c} AS {Q(c)}"))}) AS s " +
                   $"ON {string.Join(" AND ", keyCols.Select(k => $"t.{Q(k)} = s.{Q(k)}"))} " +
                   (updateCols.Count > 0 ? $"WHEN MATCHED THEN UPDATE SET {string.Join(", ", updateCols.Select(c => $"{Q(c)} = s.{Q(c)}"))} " : "") +
                   $"WHEN NOT MATCHED THEN INSERT ({string.Join(", ", cols.Select(Q))}) VALUES ({string.Join(", ", cols.Select(c => "s." + Q(c)))});";
        }

        // ---- Inserted ids -------------------------------------------------------------------------------------------

        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DbCommand, System.Runtime.CompilerServices.StrongBox<long>> _ids = new();
        static readonly System.Text.RegularExpressions.Regex InsertStart = new(@"^\s*INSERT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Runs a write. On SQL Server an INSERT also fetches SCOPE_IDENTITY() in the same batch, so Db.LastInsertedId(cmd)
        /// works afterwards on both databases. Returns the rows affected.
        /// </summary>
        public static int Execute(DbCommand cmd)
        {
            if (!IsSqlServer || !InsertStart.IsMatch(cmd.CommandText)) return cmd.ExecuteNonQuery();
            var sql = cmd.CommandText.TrimEnd().TrimEnd(';');
            cmd.CommandText = sql + "; SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            try
            {
                long id = 0; int affected;
                using (var r = cmd.ExecuteReader())
                {
                    do { while (r.Read()) if (!r.IsDBNull(0)) id = r.GetInt64(0); } while (r.NextResult());
                    affected = r.RecordsAffected;
                }
                _ids.AddOrUpdate(cmd, new System.Runtime.CompilerServices.StrongBox<long>(id));
                return affected;
            }
            finally { cmd.CommandText = sql; }
        }

        /// <summary>Runs an INSERT and returns the new auto id (0 when the table has none).</summary>
        public static long InsertReturningId(DbCommand cmd) { Execute(cmd); return LastInsertedId(cmd); }

        /// <summary>Id of the last INSERT run on this command (run it with Db.Execute or DataGuard.NonQuery on SQL Server).</summary>
        public static long LastInsertedId(DbCommand cmd) =>
            cmd is MySqlCommand m ? m.LastInsertedId : _ids.TryGetValue(cmd, out var b) ? b.Value : 0;

        // ---- Errors -------------------------------------------------------------------------------------------------

        public enum Error { Other, Duplicate, ForeignKey, TooLong, Required, BadValue }

        public static Error Classify(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is MySqlException m)
                    return m.Number switch { 1062 => Error.Duplicate, 1451 or 1452 => Error.ForeignKey, 1406 => Error.TooLong, 1048 or 1364 => Error.Required, 1366 or 1292 => Error.BadValue, _ => Error.Other };
                if (e is SqlException s)
                    return s.Number switch { 2627 or 2601 => Error.Duplicate, 547 => Error.ForeignKey, 2628 or 8152 => Error.TooLong, 515 => Error.Required, 245 or 8114 or 241 or 242 => Error.BadValue, _ => Error.Other };
            }
            return Error.Other;
        }

        public static bool IsDbError(Exception ex) => ex is MySqlException || ex is SqlException || ex.InnerException is MySqlException || ex.InnerException is SqlException;

        // ---- Schema (information_schema differs between the two) ----------------------------------------------------

        static string SchemaFilter => IsSqlServer ? "table_schema = SCHEMA_NAME()" : "table_schema = DATABASE()";

        public static bool TableExists(DbConnection c, string table, DbTransaction tx = null) =>
            Scalar(c, $"SELECT COUNT(*) FROM information_schema.tables WHERE {SchemaFilter} AND table_name = @t", tx, ("@t", table)) > 0;

        public static bool ColumnExists(DbConnection c, string table, string column, DbTransaction tx = null) =>
            Scalar(c, $"SELECT COUNT(*) FROM information_schema.columns WHERE {SchemaFilter} AND table_name = @t AND column_name = @c", tx, ("@t", table), ("@c", column)) > 0;

        public static List<string> Tables(DbConnection c) =>
            Rows(c, $"SELECT table_name FROM information_schema.tables WHERE {SchemaFilter} AND table_type = 'BASE TABLE' ORDER BY table_name", r => r.GetString(0));

        public sealed record Column(string Table, string Name, string DataType, string ColumnType, bool Nullable, bool HasDefault, bool IsPk, bool AutoIncrement, int Position);

        /// <summary>Columns of the app's tables (or of one table), in table/position order. Names keep their stored case.</summary>
        public static List<Column> Columns(DbConnection c, string table = null)
        {
            var only = table == null ? "" : (IsSqlServer ? " AND o.name = @t" : " AND c.table_name = @t");
            // SQL Server: the catalog views, not information_schema (whose per-column primary-key lookup took over 30
            // seconds on a small serverless Azure SQL database and stopped the app from starting)
            var sql = IsSqlServer
                ? $@"SELECT o.name, col.name, ty.name,
                        ty.name + CASE WHEN ty.name IN ('varchar','char','varbinary','binary') THEN '(' + CASE WHEN col.max_length = -1 THEN 'max' ELSE CAST(col.max_length AS VARCHAR(10)) END + ')'
                                       WHEN ty.name IN ('nvarchar','nchar') THEN '(' + CASE WHEN col.max_length = -1 THEN 'max' ELSE CAST(col.max_length / 2 AS VARCHAR(10)) END + ')'
                                       WHEN ty.name IN ('decimal','numeric') THEN '(' + CAST(col.precision AS VARCHAR(10)) + ',' + CAST(col.scale AS VARCHAR(10)) + ')' ELSE '' END,
                        CASE WHEN col.is_nullable = 1 THEN 1 ELSE 0 END, CASE WHEN col.default_object_id <> 0 THEN 1 ELSE 0 END,
                        CASE WHEN EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                                          WHERE i.object_id = o.object_id AND i.is_primary_key = 1 AND ic.column_id = col.column_id) THEN 1 ELSE 0 END,
                        CASE WHEN col.is_identity = 1 THEN 1 ELSE 0 END,
                        ROW_NUMBER() OVER (PARTITION BY o.object_id ORDER BY col.column_id)
                   FROM sys.objects o JOIN sys.columns col ON col.object_id = o.object_id JOIN sys.types ty ON ty.user_type_id = col.user_type_id
                   WHERE o.type IN ('U','V') AND o.schema_id = SCHEMA_ID(){only} ORDER BY o.name, col.column_id"
                : $@"SELECT c.table_name, c.column_name, c.data_type, c.column_type, CASE WHEN c.is_nullable = 'YES' THEN 1 ELSE 0 END,
                        CASE WHEN c.column_default IS NULL THEN 0 ELSE 1 END, CASE WHEN c.column_key = 'PRI' THEN 1 ELSE 0 END,
                        CASE WHEN c.extra LIKE '%auto_increment%' THEN 1 ELSE 0 END, c.ordinal_position
                   FROM information_schema.columns c WHERE c.table_schema = DATABASE(){only} ORDER BY c.table_name, c.ordinal_position";
            return Rows(c, sql, r => new Column(r.GetString(0), r.GetString(1), r.GetString(2).ToLowerInvariant(), r.GetString(3).ToLowerInvariant(),
                Convert.ToInt32(r.GetValue(4)) == 1, Convert.ToInt32(r.GetValue(5)) == 1, Convert.ToInt32(r.GetValue(6)) == 1,
                !r.IsDBNull(7) && Convert.ToInt32(r.GetValue(7)) == 1, Convert.ToInt32(r.GetValue(8))), table == null ? Array.Empty<(string, object)>() : new[] { ("@t", (object)table) });
        }

        public sealed record ForeignKey(string Table, string Column, string RefTable, string RefColumn);

        public static List<ForeignKey> ForeignKeys(DbConnection c, string table = null)
        {
            var sql = IsSqlServer
                ? @"SELECT tp.name, cp.name, tr.name, cr.name FROM sys.foreign_key_columns fkc
                      JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id JOIN sys.columns cp ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
                      JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id JOIN sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
                    WHERE tp.schema_id = SCHEMA_ID()" + (table == null ? "" : " AND tp.name = @t")
                : @"SELECT table_name, column_name, referenced_table_name, referenced_column_name FROM information_schema.key_column_usage
                    WHERE table_schema = DATABASE() AND referenced_table_name IS NOT NULL" + (table == null ? "" : " AND table_name = @t");
            return Rows(c, sql, r => new ForeignKey(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3)),
                table == null ? Array.Empty<(string, object)>() : new[] { ("@t", (object)table) });
        }

        /// <summary>Adds a nullable column when missing (used for columns the platform adds to app tables).</summary>
        public static void AddColumnIfMissing(DbConnection c, string table, string column, string type)
        {
            if (ColumnExists(c, table, column)) return;
            using var a = Command($"ALTER TABLE {Q(table)} ADD {(IsSqlServer ? "" : "COLUMN ")}{Q(column)} {type}", c);
            a.ExecuteNonQuery();
        }

        // ---- first start on SQL Server ---------------------------------------------------------------------------------

        /// <summary>
        /// SQL Server only: when the database has no tables yet, runs the app's setup script (db/init.azuresql.sql, written by
        /// the generator: tables, starting rows, indexes, links) in one transaction. Several instances starting at once take
        /// turns (sp_getapplock). Then, when Bootstrap:AdminUsername / Bootstrap:AdminPasswordHash are set (first deploy on
        /// NeoApps Cloud), the first administrator gets that username and BCrypt hash. Returns true when it set up the database.
        /// </summary>
        public static bool InitializeIfEmpty(string connectionString, string scriptPath, string adminUsername, string adminPasswordHash, Action<string> log)
        {
            if (!IsSqlServer || string.IsNullOrEmpty(scriptPath) || !System.IO.File.Exists(scriptPath)) return false;
            using var c = Open(connectionString);
            using (var lockCmd = Command("DECLARE @r INT; EXEC @r = sp_getapplock @Resource = 'neoapps_init', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 300000; SELECT @r", c))
                if (Convert.ToInt32(lockCmd.ExecuteScalar()) < 0) throw new InvalidOperationException("Timed out waiting for another instance to set up the database.");
            try
            {
                if (Tables(c).Count > 0) return false;
                log?.Invoke("Database is empty: running db/init.azuresql.sql");
                var batches = System.Text.RegularExpressions.Regex.Split(System.IO.File.ReadAllText(scriptPath), @"^\s*GO\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                using var tx = c.BeginTransaction();
                foreach (var b in batches)
                {
                    if (string.IsNullOrWhiteSpace(b)) continue;
                    using var cmd = Command(b, c, tx);
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
                if (!string.IsNullOrWhiteSpace(adminUsername) && !string.IsNullOrWhiteSpace(adminPasswordHash))
                {
                    using var admin = Command(@"UPDATE users SET username = @u, password_hash = @h, isActive = 1
                        WHERE user_id = (SELECT MIN(user_id) FROM users WHERE role_id = 1)", c, tx);
                    admin.Parameters.AddWithValue("@u", adminUsername.Trim());
                    admin.Parameters.AddWithValue("@h", adminPasswordHash.Trim());
                    admin.ExecuteNonQuery();
                }
                tx.Commit();
                log?.Invoke($"Database set up ({batches.Length} batches).");
                return true;
            }
            finally
            {
                using var release = Command("EXEC sp_releaseapplock @Resource = 'neoapps_init', @LockOwner = 'Session'", c);
                release.ExecuteNonQuery();
            }
        }

        // ---- small helpers ------------------------------------------------------------------------------------------

        public static long Scalar(DbConnection c, string sql, DbTransaction tx, params (string, object)[] ps)
        {
            using var q = Command(sql, c, tx);
            foreach (var (n, v) in ps) q.Parameters.AddWithValue(n, v);
            var o = q.ExecuteScalar();
            return o == null || o is DBNull ? 0 : Convert.ToInt64(o);
        }

        public static long Scalar(DbConnection c, string sql, params (string, object)[] ps) => Scalar(c, sql, null, ps);

        public static int Exec(DbConnection c, string sql, params (string, object)[] ps)
        {
            using var q = Command(sql, c);
            foreach (var (n, v) in ps) q.Parameters.AddWithValue(n, v);
            return q.ExecuteNonQuery();
        }

        public static List<T> Rows<T>(DbConnection c, string sql, Func<DbDataReader, T> map, params (string, object)[] ps)
        {
            using var q = Command(sql, c);
            foreach (var (n, v) in ps) q.Parameters.AddWithValue(n, v);
            using var r = q.ExecuteReader();
            var list = new List<T>();
            while (r.Read()) list.Add(map(r));
            return list;
        }
    }

    /// <summary>Lets code written against MySqlConnector's API (AddWithValue, reader.GetX("column")) run on any ADO.NET provider.
    /// MySqlConnector's own instance methods still win where the static type is a MySql type.</summary>
    public static class DbExtensions
    {
        public static DbParameter AddWithValue(this DbParameterCollection ps, string name, object value)
        {
            if (ps is MySqlParameterCollection m) return m.AddWithValue(name, value);
            if (ps is SqlParameterCollection s) return s.AddWithValue(name, value ?? DBNull.Value);
            throw new NotSupportedException(ps.GetType().Name);
        }

        public static string GetString(this DbDataReader r, string name) => Convert.ToString(r.GetValue(r.GetOrdinal(name)), System.Globalization.CultureInfo.InvariantCulture);
        public static int GetInt32(this DbDataReader r, string name) => Convert.ToInt32(r.GetValue(r.GetOrdinal(name)));
        public static long GetInt64(this DbDataReader r, string name) => Convert.ToInt64(r.GetValue(r.GetOrdinal(name)));
        public static short GetInt16(this DbDataReader r, string name) => Convert.ToInt16(r.GetValue(r.GetOrdinal(name)));
        public static sbyte GetSByte(this DbDataReader r, string name) => Convert.ToSByte(r.GetValue(r.GetOrdinal(name)));
        public static byte GetByte(this DbDataReader r, string name) => Convert.ToByte(r.GetValue(r.GetOrdinal(name)));
        public static bool GetBoolean(this DbDataReader r, string name) => Convert.ToBoolean(r.GetValue(r.GetOrdinal(name)));
        public static decimal GetDecimal(this DbDataReader r, string name) => Convert.ToDecimal(r.GetValue(r.GetOrdinal(name)));
        public static double GetDouble(this DbDataReader r, string name) => Convert.ToDouble(r.GetValue(r.GetOrdinal(name)));
        public static float GetFloat(this DbDataReader r, string name) => Convert.ToSingle(r.GetValue(r.GetOrdinal(name)));
        public static DateTime GetDateTime(this DbDataReader r, string name) => Convert.ToDateTime(r.GetValue(r.GetOrdinal(name)));
        public static Guid GetGuid(this DbDataReader r, string name) => r.GetGuid(r.GetOrdinal(name));
        public static object GetValue(this DbDataReader r, string name) => r.GetValue(r.GetOrdinal(name));
        public static bool IsDBNull(this DbDataReader r, string name) => r.IsDBNull(r.GetOrdinal(name));
    }

    /// <summary>A one-shot runner: Wake() runs the action once after a short delay (several wakes before it runs = one run).</summary>
    public sealed class WakeTimer
    {
        readonly System.Threading.Timer _timer;
        int _scheduled;
        public WakeTimer(Action action) =>
            _timer = new System.Threading.Timer(_ => { System.Threading.Interlocked.Exchange(ref _scheduled, 0); action(); }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        public void Wake(TimeSpan? after = null)
        {
            if (System.Threading.Interlocked.Exchange(ref _scheduled, 1) == 0)
                _timer.Change(after ?? TimeSpan.FromSeconds(2), System.Threading.Timeout.InfiniteTimeSpan);
        }
    }
}
