using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// "numbering" from ui.config.json: numbers assigned by the API on insert and read-only afterwards.
    ///   "numbering": { "customer_invoices": { "column": "invoice_number", "pattern": "INV-{yyyy}-{seq:5}" },
    ///                  "cases": [ { "column": "case_number", "pattern": "C{yy}{mm}-{seq:4}", "reset": "monthly" } ] }
    /// Tokens: {yyyy} {yy} {mm} {dd} (UTC date of creation), {seq} / {seq:N} (zero-padded to N digits), {tenant}.
    /// One sequence per table/column, tenant (tenant tables) and period: "reset" is yearly when the pattern has a year,
    /// monthly with a month, otherwise never (override with "reset": "never" | "yearly" | "monthly" | "daily").
    /// Gap-free and safe under concurrency: the counter row in number_sequences is incremented in the same transaction as
    /// the insert (it stays locked until commit, and a failed insert rolls the number back). A new sequence continues
    /// after the highest number already stored with the same prefix, so hand-typed numbers are not reused.
    /// </summary>
    public static partial class DataGuard
    {
        sealed class Numbering { public string Table, Column, Pattern, Reset; }
        static readonly Dictionary<string, List<Numbering>> _numbering = new(StringComparer.OrdinalIgnoreCase);
        static readonly Regex Token = new(@"\{(yyyy|yy|mm|dd|tenant|seq(?::(\d+))?)\}", RegexOptions.IgnoreCase);

        static void LoadNumbering(MySqlConnection c)
        {
            _numbering.Clear();
            if (Config["numbering"] is not JsonObject all) return;
            using (var cmd = new MySqlCommand(@"CREATE TABLE IF NOT EXISTS number_sequences (seq_key VARCHAR(191) NOT NULL PRIMARY KEY,
                next_value BIGINT NOT NULL, updatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)", c)) cmd.ExecuteNonQuery();
            foreach (var (table, node) in all)
                foreach (var o in (node is JsonArray a ? a.OfType<JsonObject>() : new[] { node as JsonObject }).Where(x => x != null))
                {
                    var n = new Numbering { Table = table, Column = o["column"]!.GetValue<string>(), Pattern = o["pattern"]?.GetValue<string>() ?? "{seq}" };
                    if (!Regex.IsMatch(n.Pattern, @"\{seq(:\d+)?\}", RegexOptions.IgnoreCase))
                        throw new InvalidOperationException($"numbering {table}.{n.Column}: the pattern needs {{seq}}");
                    var p = n.Pattern.ToLowerInvariant();
                    n.Reset = o["reset"]?.GetValue<string>()?.ToLowerInvariant()
                              ?? (p.Contains("{dd}") ? "daily" : p.Contains("{mm}") ? "monthly" : p.Contains("{yy") ? "yearly" : "never");
                    if (!_numbering.TryGetValue(table, out var list)) _numbering[table] = list = new();
                    list.Add(n);
                    // numbers can't be edited once assigned
                    if (!_immutable.TryGetValue(table, out var im)) _immutable[table] = im = new(StringComparer.OrdinalIgnoreCase);
                    if (!im.Contains("*")) im.Add(n.Column);
                }
        }

        static bool IsNumbered(string table, string column) =>
            _numbering.TryGetValue(table, out var l) && l.Any(n => n.Column.Equals(column, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Binds a temporary unique value to the numbered columns of an INSERT and, after it succeeds (same transaction),
        /// takes the next number and writes it to the new row.
        /// </summary>
        static void AssignNumbers(MySqlCommand cmd, Caller caller, string table, List<string> cols, List<string> exprs, Dictionary<string, object> values)
        {
            if (!_numbering.TryGetValue(table, out var list) || !PrimaryKeys.TryGetValue(table, out var pk)) return;
            var now = DateTime.UtcNow;
            foreach (var n in list)
            {
                var i = cols.IndexOf(n.Column.ToLowerInvariant());
                if (i >= 0 && i < exprs.Count && exprs[i].Trim().StartsWith("@"))
                    Bind(cmd, exprs[i].Trim(), "#" + Guid.NewGuid().ToString("N").Substring(0, 12)); // placeholder, replaced below
                var tenant = TenantTables.Contains(table) ? Str(values.GetValueOrDefault(TenantColumn)) ?? Str(caller?.Tenant) : null;
                After(cmd, (c, _) =>
                {
                    var number = NextNumber(c, n, tenant, now);
                    using var u = new MySqlCommand($"UPDATE `{table}` SET `{n.Column}` = @v WHERE `{pk}` = @id", c.Connection, c.Transaction);
                    u.Parameters.AddWithValue("@v", number); u.Parameters.AddWithValue("@id", c.LastInsertedId);
                    u.ExecuteNonQuery();
                    values[n.Column] = number; // audit / outbox rows written after this see the real number
                });
            }
        }

        static string Render(string pattern, DateTime d, string tenant, long? seq) =>
            Token.Replace(pattern, m => m.Groups[1].Value.ToLowerInvariant() switch
            {
                "yyyy" => d.ToString("yyyy", CultureInfo.InvariantCulture),
                "yy" => d.ToString("yy", CultureInfo.InvariantCulture),
                "mm" => d.ToString("MM", CultureInfo.InvariantCulture),
                "dd" => d.ToString("dd", CultureInfo.InvariantCulture),
                "tenant" => tenant ?? "",
                _ => seq == null ? "\u0000" : seq.Value.ToString(m.Groups[2].Success ? new string('0', int.Parse(m.Groups[2].Value)) : "0", CultureInfo.InvariantCulture),
            });

        static string NextNumber(MySqlCommand c, Numbering n, string tenant, DateTime now)
        {
            var period = n.Reset switch { "daily" => now.ToString("yyyyMMdd"), "monthly" => now.ToString("yyyyMM"), "yearly" => now.ToString("yyyy"), _ => "" };
            var key = $"{n.Table}.{n.Column}|{tenant}|{period}".ToLowerInvariant();
            // first use of this sequence: continue after the highest number already stored with the same prefix
            var shape = Render(n.Pattern, now, tenant, null);
            var cut = shape.IndexOf('\u0000');
            var prefix = shape.Substring(0, cut);
            var suffix = shape.Substring(cut + 1);
            long start = 1;
            using (var exists = new MySqlCommand("SELECT COUNT(*) FROM number_sequences WHERE seq_key = @k", c.Connection, c.Transaction))
            {
                exists.Parameters.AddWithValue("@k", key);
                if (Convert.ToInt64(exists.ExecuteScalar()) == 0)
                {
                    var tenantCond = TenantTables.Contains(n.Table) && tenant != null ? $" AND `{TenantColumn}` = @t" : "";
                    using var max = new MySqlCommand($@"SELECT `{n.Column}` FROM `{n.Table}` WHERE `{n.Column}` LIKE @p{tenantCond}", c.Connection, c.Transaction);
                    max.Parameters.AddWithValue("@p", prefix.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%");
                    if (tenantCond != "") max.Parameters.AddWithValue("@t", tenant);
                    using var r = max.ExecuteReader();
                    while (r.Read())
                    {
                        var s = r.IsDBNull(0) ? "" : r.GetString(0);
                        if (s.Length <= prefix.Length + suffix.Length || !s.EndsWith(suffix, StringComparison.Ordinal)) continue;
                        if (long.TryParse(s.Substring(prefix.Length, s.Length - prefix.Length - suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v >= start)
                            start = v + 1;
                    }
                }
            }
            // locks the counter row until the insert's transaction ends: concurrent inserts wait, rollbacks leave no gap
            using (var inc = new MySqlCommand(@"INSERT INTO number_sequences (seq_key, next_value) VALUES (@k, @s + 1)
                    ON DUPLICATE KEY UPDATE next_value = next_value + 1, updatedAt = UTC_TIMESTAMP()", c.Connection, c.Transaction))
            {
                inc.Parameters.AddWithValue("@k", key); inc.Parameters.AddWithValue("@s", start);
                inc.ExecuteNonQuery();
            }
            using var get = new MySqlCommand("SELECT next_value - 1 FROM number_sequences WHERE seq_key = @k", c.Connection, c.Transaction);
            get.Parameters.AddWithValue("@k", key);
            return Render(n.Pattern, now, tenant, Convert.ToInt64(get.ExecuteScalar()));
        }
    }
}
