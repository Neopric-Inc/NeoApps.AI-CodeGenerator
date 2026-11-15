using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MySqlConnector;
using nkv.MicroService.API.Ai;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// "data.import": imports rows (parsed from a CSV in the browser) into one table. Every row goes through DataGuard
    /// exactly like a normal create (scopes, tenancy, validations, lifecycles, numbering, audit), as "&lt;user&gt; via import".
    /// First a trial run inside a transaction that is rolled back, so nothing is saved and no numbers are used; problems are
    /// reported per row. If every row is fine the job waits for approval, then imports all rows in one transaction.
    /// Input: {"table": "customers", "rows": [{"name": "Acme", ...}], "key": "email"?}. With "key", rows whose key value
    /// already exists are skipped, so running the same file twice adds nothing.
    /// </summary>
    public sealed class ImportJob : JobHandler
    {
        public const int MaxRows = 5000;
        private static readonly HashSet<string> NotWritable = new(StringComparer.OrdinalIgnoreCase)
        {
            "createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive", DataGuard.CreatedByUser, DataGuard.ModifiedByUser,
        };

        private readonly AiData _data;

        public ImportJob(AiData data) => _data = data;

        public override string Kind => "data.import";

        private static AiUser AsUser(JobUser u) => new(u.UserId, u.Username, u.RoleId, u.Owner);

        public override string Validate(JobUser user, JsonObject input)
        {
            using var c = _data.Open();
            var (table, rows, _, _) = Read(c, user, input);
            return $"Import {rows.Count} row{(rows.Count == 1 ? "" : "s")} into {table}";
        }

        /// <summary>Checks the input and the person's create permission; returns the table, rows, writable columns and key.</summary>
        private (string Table, List<JsonObject> Rows, Dictionary<string, ColumnInfo> Columns, string Key) Read(MySqlConnection c, JobUser user, JsonObject input)
        {
            var table = input["table"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(table) || !_data.AppTables(c).Contains(table)) throw new JobException("Say which table to import into (input.table).");
            if (!_data.Permissions(c, AsUser(user)).TryGetValue(table, out var p) || !p.Create)
                throw new JobException($"You need permission to create {table} records to import them.");
            var rows = (input["rows"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();
            if (rows.Count == 0) throw new JobException("The file has no rows to import.");
            if (rows.Count > MaxRows) throw new JobException($"Import up to {MaxRows} rows at a time; this file has {rows.Count}.");
            var columns = _data.Columns(c, table).Where(col => !NotWritable.Contains(col.Name) && !(col.IsPk && col.AutoIncrement))
                .ToDictionary(col => col.Name, StringComparer.OrdinalIgnoreCase);
            var unknown = rows.SelectMany(r => r.Select(kv => kv.Key)).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(k => !columns.ContainsKey(k)).ToList();
            if (unknown.Count > 0) throw new JobException($"These columns can't be imported into {table}: {string.Join(", ", unknown)}.");
            var key = input["key"]?.ToString();
            if (!string.IsNullOrEmpty(key) && !columns.ContainsKey(key)) throw new JobException($"Key column '{key}' isn't a column of {table}.");
            return (table, rows, columns, string.IsNullOrEmpty(key) ? null : columns[key].Name);
        }

        public override Task RunAsync(JobContext ctx)
        {
            using var c = _data.Open();
            var (table, rows, columns, key) = Read(c, ctx.User, ctx.Input);
            ctx.Progress($"Checking {rows.Count} rows against the app's rules (nothing is saved yet)");
            var outcome = Insert(c, ctx, table, rows, columns, key, commit: false);
            if (outcome.Errors.Count > 0)
            {
                var csv = new StringBuilder("row,problem\r\n");
                foreach (var e in outcome.Errors) csv.Append(e.Row).Append(",\"").Append(e.Message.Replace("\"", "\"\"")).Append("\"\r\n");
                ctx.AttachFile("import-problems.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()), "Rows that couldn't be imported");
                ctx.Complete(new JsonObject
                {
                    ["table"] = table, ["imported"] = 0, ["ready"] = outcome.Ready, ["skipped"] = outcome.Skipped,
                    ["errors"] = new JsonArray(outcome.Errors.Take(200).Select(e => (JsonNode)new JsonObject { ["row"] = e.Row, ["message"] = e.Message }).ToArray()),
                    ["errorCount"] = outcome.Errors.Count,
                }, $"{outcome.Errors.Count} row{(outcome.Errors.Count == 1 ? " has" : "s have")} problems. Nothing was imported; fix the file and try again.");
                return Task.CompletedTask;
            }
            var summary = new JsonObject { ["table"] = table, ["ready"] = outcome.Ready, ["skipped"] = outcome.Skipped };
            if (outcome.Ready == 0) { ctx.Complete(summary, $"Nothing to import: all {outcome.Skipped} rows already exist."); return Task.CompletedTask; }
            ctx.AwaitApproval(summary, $"{outcome.Ready} row{(outcome.Ready == 1 ? " is" : "s are")} ready to import" +
                (outcome.Skipped > 0 ? $", {outcome.Skipped} already exist{(outcome.Skipped == 1 ? "s" : "")} and will be skipped" : "") + $". Approve to import {(outcome.Ready == 1 ? "it" : "them")}.");
            return Task.CompletedTask;
        }

        public override Task ResumeAsync(JobContext ctx, JsonObject decision)
        {
            using var c = _data.Open();
            var (table, rows, columns, key) = Read(c, ctx.User, ctx.Input);
            ctx.Progress($"Importing {rows.Count} rows");
            var outcome = Insert(c, ctx, table, rows, columns, key, commit: true);
            if (outcome.Errors.Count > 0) // data changed since the check: nothing was saved
                throw new JobException($"Row {outcome.Errors[0].Row}: {outcome.Errors[0].Message} Nothing was imported.");
            ctx.Complete(new JsonObject { ["table"] = table, ["imported"] = outcome.Ready, ["skipped"] = outcome.Skipped },
                $"Imported {outcome.Ready} row{(outcome.Ready == 1 ? "" : "s")}" + (outcome.Skipped > 0 ? $", skipped {outcome.Skipped} that already existed." : "."));
            return Task.CompletedTask;
        }

        private sealed record Outcome(int Ready, int Skipped, List<(int Row, string Message)> Errors);

        /// <summary>
        /// Inserts every row in one transaction through DataGuard. Trial (commit false): each row in its own savepoint so all
        /// problems are found, then everything is rolled back. Real run: any failure rolls back the whole file.
        /// </summary>
        private static Outcome Insert(MySqlConnection c, JobContext ctx, string table, List<JsonObject> rows,
            Dictionary<string, ColumnInfo> columns, string key, bool commit)
        {
            var errors = new List<(int, string)>();
            int ready = 0, skipped = 0;
            var seenKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var tx = c.BeginTransaction();
            try
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    var rowNo = i + 2; // spreadsheet row: header is row 1
                    var values = rows[i].Where(kv => kv.Value != null && kv.Value.ToString() != "")
                        .ToDictionary(kv => columns[kv.Key].Name, kv => Convert(columns[kv.Key], kv.Value!.ToString()));
                    if (key != null && values.TryGetValue(key, out var kv0) && kv0 != null)
                    {
                        var k = kv0.ToString()!;
                        if (seenKeys.TryGetValue(k, out var first)) { errors.Add((rowNo, $"Same {key} '{k}' as row {first}.")); continue; }
                        seenKeys[k] = rowNo;
                        if (Exists(c, tx, table, key, kv0)) { skipped++; continue; }
                    }
                    if (!commit) Exec(c, tx, "SAVEPOINT import_row");
                    try
                    {
                        var names = values.Keys.ToList();
                        using var cmd = new MySqlCommand(
                            $"INSERT INTO `{table}` ({string.Join(", ", names.Select(n => "`" + n + "`"))}{(names.Count > 0 ? ", " : "")}createdBy, modifiedBy, isActive) " +
                            $"VALUES ({string.Join(", ", names.Select((_, j) => "@v" + j))}{(names.Count > 0 ? ", " : "")}@o, @o, 1)", c, tx);
                        for (var j = 0; j < names.Count; j++) cmd.Parameters.AddWithValue("@v" + j, values[names[j]] ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@o", ctx.User.Owner);
                        DataGuard.NonQuery(cmd);
                        ready++;
                    }
                    catch (Exception ex) when (ex is DataGuardException || ex is MySqlException || ex is FormatException)
                    {
                        if (commit) throw new ImportRowException(rowNo, Friendly(ex));
                        Exec(c, tx, "ROLLBACK TO SAVEPOINT import_row");
                        errors.Add((rowNo, Friendly(ex)));
                    }
                    if (rowNo % 250 == 0) ctx.Progress($"{(commit ? "Imported" : "Checked")} {i + 1} of {rows.Count} rows", (i + 1) * 100 / rows.Count);
                }
                if (commit) tx.Commit(); else tx.Rollback();
            }
            catch (ImportRowException e)
            {
                tx.Rollback();
                errors.Add((e.Row, e.Message));
            }
            return new Outcome(ready, skipped, errors);
        }

        private sealed class ImportRowException : Exception
        {
            public int Row { get; }
            public ImportRowException(int row, string message) : base(message) => Row = row;
        }

        private static void Exec(MySqlConnection c, MySqlTransaction tx, string sql)
        {
            using var cmd = new MySqlCommand(sql, c, tx);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Whether a visible, active row already has this key value (scoped like any read by this person).</summary>
        private static bool Exists(MySqlConnection c, MySqlTransaction tx, string table, string key, object value)
        {
            using var cmd = new MySqlCommand($"SELECT 1 FROM `{table}` WHERE `{key}` = @k AND isActive = 1 LIMIT 1", c, tx);
            cmd.Parameters.AddWithValue("@k", value);
            return DataGuard.Apply(cmd).ExecuteScalar() != null;
        }

        /// <summary>CSV text to a database value for the column's type. Unreadable numbers and dates throw FormatException.</summary>
        private static object Convert(ColumnInfo col, string text)
        {
            var s = text.Trim();
            if (s.StartsWith("'") && s.Length > 1 && "=+-@".Contains(s[1])) s = s[1..]; // the formula guard Export adds
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            switch (col.DataType)
            {
                case "tinyint" when col.ColumnType.StartsWith("tinyint(1)"):
                    var b = s.ToLowerInvariant();
                    if (b is "1" or "true" or "yes" or "y") return 1;
                    if (b is "0" or "false" or "no" or "n") return 0;
                    throw new FormatException($"{col.Name}: '{s}' isn't yes/no.");
                case "int": case "bigint": case "smallint": case "mediumint": case "tinyint":
                    return long.TryParse(s, System.Globalization.NumberStyles.Integer, ci, out var n) ? n : throw new FormatException($"{col.Name}: '{s}' isn't a whole number.");
                case "decimal": case "double": case "float":
                    var cleaned = s.Replace(",", "").Replace("$", "").Replace("€", "").Replace("£", "").Trim();
                    return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Number, ci, out var d) ? d : throw new FormatException($"{col.Name}: '{s}' isn't a number.");
                case "date":
                    return DateTime.TryParse(s, ci, System.Globalization.DateTimeStyles.None, out var dt) ? dt.ToString("yyyy-MM-dd")
                        : throw new FormatException($"{col.Name}: '{s}' isn't a date (use YYYY-MM-DD).");
                case "datetime": case "timestamp":
                    return DateTime.TryParse(s, ci, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var t)
                        ? t.ToString("yyyy-MM-dd HH:mm:ss") : throw new FormatException($"{col.Name}: '{s}' isn't a date and time.");
                default:
                    return s;
            }
        }

        private static string Friendly(Exception ex) => ex switch
        {
            MySqlException { Number: 1452 } => "It points at a related record that doesn't exist.",
            MySqlException { Number: 1062 } => "A record with the same unique value already exists.",
            MySqlException { Number: 1364 } m => System.Text.RegularExpressions.Regex.Replace(m.Message, "^Field '(.+)' doesn't have a default value$", "Required field '$1' is empty."),
            MySqlException { Number: 1048 } m => m.Message.Replace("Column", "Required field").Replace("cannot be null", "is empty."),
            MySqlException { Number: 1406 } => "A value is too long for its field.",
            _ => ex.Message,
        };
    }
}
