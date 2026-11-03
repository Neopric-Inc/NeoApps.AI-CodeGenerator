using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// "validations" from ui.config.json, checked on every insert/update that passes through DataGuard (400 with
    /// field-level messages: {"errors": {"column": ["message"]}}, shown next to the field by the UI):
    ///   "validations": { "customer_invoices": {
    ///       "email":    { "format": "email" },                       // email | url | phone | uuid
    ///       "code":     { "pattern": "^[A-Z0-9-]+$", "message": "Use capitals, digits and dashes" },
    ///       "quantity": { "min": 1, "max": 1000 },  "name": { "minLength": 2, "maxLength": 80 },
    ///       "due_date": { "gte": "issued_at" },                      // gt / gte / lt / lte / ne: another column
    ///       "total":    { "equals": "subtotal + tax - discount_amount", "tolerance": 0.01 },
    ///       "po_number":{ "requiredWhen": { "status": ["issued", "paid"] } },  "notes": { "required": true } } }
    /// On update only the rules touching a changed column are checked, so old rows can still be edited.
    /// </summary>
    public static partial class DataGuard
    {
        sealed class FieldRule
        {
            public string Column, Format, Message, Equals;
            public Regex Pattern;
            public decimal? Min, Max, Tolerance;
            public int? MinLength, MaxLength;
            public bool Required;
            public Dictionary<string, HashSet<string>> RequiredWhen;
            public List<(string op, string other)> Compares = new();
            public HashSet<string> Uses = new(StringComparer.OrdinalIgnoreCase);
        }
        static readonly Dictionary<string, List<FieldRule>> _validations = new(StringComparer.OrdinalIgnoreCase);
        static readonly string[] CompareOps = { "gt", "gte", "lt", "lte", "ne" };

        static void LoadValidations()
        {
            _validations.Clear();
            if (Config["validations"] is not JsonObject all) return;
            foreach (var (table, tnode) in all)
            {
                if (tnode is not JsonObject cols) continue;
                var list = new List<FieldRule>();
                foreach (var (col, rnode) in cols)
                {
                    if (rnode is not JsonObject r) continue;
                    var f = new FieldRule
                    {
                        Column = col, Format = r["format"]?.GetValue<string>()?.ToLowerInvariant(), Message = r["message"]?.GetValue<string>(),
                        Pattern = r["pattern"] is JsonValue p ? new Regex(p.GetValue<string>(), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200)) : null,
                        Min = Dec(r["min"]), Max = Dec(r["max"]), Tolerance = Dec(r["tolerance"]),
                        MinLength = (int?)Dec(r["minLength"]), MaxLength = (int?)Dec(r["maxLength"]),
                        Required = r["required"]?.GetValue<bool>() == true, Equals = r["equals"]?.GetValue<string>(),
                    };
                    f.Uses.Add(col);
                    foreach (var op in CompareOps)
                        if (r[op] is JsonValue o) { f.Compares.Add((op, o.GetValue<string>())); f.Uses.Add(o.GetValue<string>()); }
                    if (r["requiredWhen"] is JsonObject rw)
                    {
                        f.RequiredWhen = new(StringComparer.OrdinalIgnoreCase);
                        foreach (var (k, v) in rw) { f.RequiredWhen[k] = Set(v); f.Uses.Add(k); }
                    }
                    if (f.Equals != null)
                    {
                        Expr.Parse(f.Equals); // fail at startup on a bad expression, not on the first write
                        foreach (Match m in Regex.Matches(f.Equals, @"[A-Za-z_]\w*")) f.Uses.Add(m.Value);
                    }
                    list.Add(f);
                }
                _validations[table] = list;
            }
        }

        static decimal? Dec(JsonNode n) => n is JsonValue v && decimal.TryParse(v.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

        /// <summary>Checks the row (current values merged with the change); throws 400 with every failing field.</summary>
        static void Validate(string table, Dictionary<string, object> row, ICollection<string> changed)
        {
            if (!_validations.TryGetValue(table, out var rules)) return;
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            void Fail(FieldRule f, string msg) { if (!errors.TryGetValue(f.Column, out var l)) errors[f.Column] = l = new(); l.Add(f.Message ?? msg); }
            string Label(string c) => c.Replace('_', ' ');
            foreach (var f in rules)
            {
                if (changed != null && !f.Uses.Any(changed.Contains)) continue;
                if (changed == null && IsNumbered(table, f.Column)) continue; // assigned after the insert
                var v = row.GetValueOrDefault(f.Column);
                var s = Str(v);
                var empty = string.IsNullOrWhiteSpace(s);
                if (empty)
                {
                    if (f.Required) Fail(f, $"{Label(f.Column)} is required");
                    else if (f.RequiredWhen != null && f.RequiredWhen.All(kv => kv.Value.Contains(Str(row.GetValueOrDefault(kv.Key)) ?? "")))
                        Fail(f, $"{Label(f.Column)} is required when {string.Join(" and ", f.RequiredWhen.Select(kv => $"{Label(kv.Key)} is {string.Join(" or ", kv.Value)}"))}");
                    continue;
                }
                switch (f.Format)
                {
                    case "email" when !Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"): Fail(f, "Enter a valid email address"); break;
                    case "url" when !(Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https")): Fail(f, "Enter a valid web address (https://...)"); break;
                    case "phone" when !Regex.IsMatch(s, @"^\+?[0-9 ()./-]{6,20}$"): Fail(f, "Enter a valid phone number"); break;
                    case "uuid" when !Guid.TryParse(s, out _): Fail(f, "Enter a valid id"); break;
                }
                if (f.Pattern != null && !f.Pattern.IsMatch(s)) Fail(f, $"{Label(f.Column)} has the wrong format");
                if (f.MinLength is int mn && s.Length < mn) Fail(f, $"At least {mn} characters");
                if (f.MaxLength is int mx && s.Length > mx) Fail(f, $"At most {mx} characters");
                var num = Num(v);
                if (f.Min is decimal lo && (num == null || num < lo)) Fail(f, $"Must be at least {lo.ToString(CultureInfo.InvariantCulture)}");
                if (f.Max is decimal hi && (num == null || num > hi)) Fail(f, $"Must be at most {hi.ToString(CultureInfo.InvariantCulture)}");
                foreach (var (op, other) in f.Compares)
                {
                    var o = row.GetValueOrDefault(other);
                    if (Str(o) == null) continue;
                    var c = CompareValues(v, o);
                    var ok = op switch { "gt" => c > 0, "gte" => c >= 0, "lt" => c < 0, "lte" => c <= 0, _ => c != 0 };
                    var word = op switch { "gt" => "after / greater than", "gte" => "on or after / at least", "lt" => "before / less than", "lte" => "on or before / at most", _ => "different from" };
                    if (!ok) Fail(f, $"Must be {word} {Label(other)}");
                }
                if (f.Equals != null && num != null)
                {
                    var expected = Expr.Parse(f.Equals).Eval(c => Num(row.GetValueOrDefault(c)) ?? 0m);
                    if (Math.Abs(expected - num.Value) > (f.Tolerance ?? 0.005m))
                        Fail(f, $"Must equal {f.Equals.Replace('_', ' ')} ({Math.Round(expected, 2).ToString(CultureInfo.InvariantCulture)})");
                }
            }
            if (errors.Count > 0)
                throw new DataGuardException("Please check: " + string.Join(", ", errors.Keys), 400) { Errors = errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()) };
        }

        static decimal? Num(object v) => v switch
        {
            null or DBNull => null,
            bool b => b ? 1 : 0,
            IConvertible when v is not string && v is not DateTime => Convert.ToDecimal(v, CultureInfo.InvariantCulture),
            _ => decimal.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null,
        };

        static int CompareValues(object a, object b)
        {
            if (Num(a) is decimal x && Num(b) is decimal y) return x.CompareTo(y);
            if (AsDate(a) is DateTime da && AsDate(b) is DateTime db) return da.CompareTo(db);
            return string.CompareOrdinal(Str(a), Str(b));
        }

        static DateTime? AsDate(object v) => v is DateTime d ? d
            : DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var p) ? p : null;

        /// <summary>Tiny arithmetic expression: numbers, column names, + - * / and parentheses.</summary>
        sealed class Expr
        {
            readonly Func<Func<string, decimal>, decimal> _f;
            Expr(Func<Func<string, decimal>, decimal> f) { _f = f; }
            public decimal Eval(Func<string, decimal> col) => _f(col);
            static readonly Dictionary<string, Expr> Cache = new();

            public static Expr Parse(string text)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(text, out var e)) return e;
                    var toks = Regex.Matches(text, @"\d+(\.\d+)?|[A-Za-z_]\w*|[-+*/()]|\S").Select(m => m.Value).ToList();
                    var i = 0;
                    var f = Sum(toks, ref i);
                    if (i != toks.Count) throw new FormatException($"validations: can't read '{text}' near '{toks[i]}'");
                    return Cache[text] = new Expr(f);
                }
            }
            static Func<Func<string, decimal>, decimal> Sum(List<string> t, ref int i)
            {
                var l = Product(t, ref i);
                while (i < t.Count && (t[i] == "+" || t[i] == "-"))
                {
                    var op = t[i++]; var a = l; var b = Product(t, ref i);
                    l = op == "+" ? c => a(c) + b(c) : c => a(c) - b(c);
                }
                return l;
            }
            static Func<Func<string, decimal>, decimal> Product(List<string> t, ref int i)
            {
                var l = Atom(t, ref i);
                while (i < t.Count && (t[i] == "*" || t[i] == "/"))
                {
                    var op = t[i++]; var a = l; var b = Atom(t, ref i);
                    l = op == "*" ? c => a(c) * b(c) : c => { var d = b(c); return d == 0 ? 0 : a(c) / d; };
                }
                return l;
            }
            static Func<Func<string, decimal>, decimal> Atom(List<string> t, ref int i)
            {
                if (i >= t.Count) throw new FormatException("validations: expression ends early");
                var tok = t[i++];
                if (tok == "-") { var a = Atom(t, ref i); return c => -a(c); }
                if (tok == "(")
                {
                    var inner = Sum(t, ref i);
                    if (i >= t.Count || t[i++] != ")") throw new FormatException("validations: missing )");
                    return inner;
                }
                if (decimal.TryParse(tok, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)) return _ => n;
                if (Regex.IsMatch(tok, @"^[A-Za-z_]\w*$")) return c => c(tok);
                throw new FormatException($"validations: unexpected '{tok}'");
            }
        }
    }
}
