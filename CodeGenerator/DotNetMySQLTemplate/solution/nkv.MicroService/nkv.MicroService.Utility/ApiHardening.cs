using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Production defaults for generated APIs (appsettings, all optional):
    ///   "Api":     { "MaxPageSize": 1000 }                 cap for itemsPerPage / LIMIT on every generated query
    ///   "Logging": { "LogSql": false }                      print generated SQL (development only)
    ///   "Cors":    { "AllowedOrigins": "https://app.example.com" }   comma separated; empty = any origin in Development only
    ///   "Swagger": { "Enabled": false }                     Swagger UI outside Development
    /// Errors: database constraint failures become readable 400/409 messages; anything else is a generic 500 with a reference
    /// (the request's correlation id) and the details go to the server log only.
    /// </summary>
    public static class ApiHardening
    {
        public static int MaxPageSize { get; private set; } = 1000;
        public static bool LogSql { get; private set; }

        public static void Configure(IConfiguration c)
        {
            MaxPageSize = Math.Max(1, c.GetValue("Api:MaxPageSize", 1000));
            LogSql = c.GetValue("Logging:LogSql", false);
        }

        public static void Sql(string text) { if (LogSql) Console.WriteLine(text); }

        public static string[] AllowedOrigins(IConfiguration c) =>
            (c["Cors:AllowedOrigins"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static bool IsDevelopment =>
            string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        /// <summary>Status code and safe message for an exception; logs the full exception with the reference.</summary>
        public static (int status, string message) MapError(Exception ex, string reference)
        {
            var my = ex as MySqlException ?? ex.InnerException as MySqlException ?? ex.GetBaseException() as MySqlException;
            (int, string)? known = my?.Number switch
            {
                1062 => (409, "A record with the same value already exists."),
                1451 => (409, "This record is still used by other records."),
                1452 => (409, "A linked record doesn't exist."),
                1048 => (400, "A required value is missing."),
                1406 => (400, "A value is too long."),
                1264 => (400, "A number is out of range."),
                1292 or 1366 => (400, "A value has the wrong format."),
                _ => null,
            };
            if (known is { } k)
            {
                Console.Error.WriteLine($"[{reference}] {k.Item1}: {my!.Number} {my.Message}");
                return k;
            }
            Console.Error.WriteLine($"[{reference}] 500: {ex}");
            return (500, $"Something went wrong. Reference: {reference}");
        }

        /// <summary>Controller helper: <c>catch (Exception ex) { return ApiHardening.Error(this, ex); }</c></summary>
        public static ObjectResult Error(ControllerBase controller, Exception ex)
        {
            var (status, message) = MapError(ex, controller.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"));
            return controller.StatusCode(status, new APIResponse(ResponseCode.ERROR, "Exception", message));
        }

        static readonly Regex SafeCorrelation = new(@"^[A-Za-z0-9._:-]{1,64}$", RegexOptions.Compiled);

        /// <summary>Middleware: correlation id (incoming X-Correlation-Id if safe), security headers, safe 500 for unhandled errors.</summary>
        public static async System.Threading.Tasks.Task Middleware(HttpContext ctx, Func<System.Threading.Tasks.Task> next)
        {
            if (ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) && cid.Count > 0 && SafeCorrelation.IsMatch(cid[0]!))
                ctx.TraceIdentifier = cid[0]!;
            ctx.Response.Headers["X-Correlation-Id"] = ctx.TraceIdentifier;
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            try { await next(); }
            catch (DataGuardException) { throw; }
            catch (Exception ex) when (!ctx.Response.HasStarted && !IsDevelopment)
            {
                var (status, message) = MapError(ex, ctx.TraceIdentifier);
                ctx.Response.StatusCode = status;
                await ctx.Response.WriteAsJsonAsync(new { code = 0, message });
            }
        }

        static readonly Regex LimitPair = new(@"\bLIMIT\s+(@\w+|\d+)\s*,\s*(@\w+|\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex LimitOne = new(@"\bLIMIT\s+(@\w+|\d+)(?!\s*,)(?:\s+OFFSET\s+(@\w+|\d+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Caps the row count of LIMIT clauses (literal or parameter) at MaxPageSize; negative offsets become 0.</summary>
        public static void ClampLimit(MySqlCommand cmd)
        {
            var sql = cmd.CommandText;
            if (sql == null || sql.IndexOf("LIMIT", StringComparison.OrdinalIgnoreCase) < 0) return;
            string Fix(string token, int min, int max)
            {
                if (token.StartsWith("@"))
                {
                    if (cmd.Parameters.Contains(token) && int.TryParse(Convert.ToString(cmd.Parameters[token].Value), out var v))
                        cmd.Parameters[token].Value = Math.Clamp(v, min, max);
                    return token;
                }
                return long.TryParse(token, out var n) ? Math.Clamp(n, min, max).ToString() : token;
            }
            var m = LimitPair.Match(sql);
            if (m.Success)
                cmd.CommandText = sql.Substring(0, m.Index) + $"LIMIT {Fix(m.Groups[1].Value, 0, int.MaxValue)}, {Fix(m.Groups[2].Value, 1, MaxPageSize)}" + sql.Substring(m.Index + m.Length);
            else if ((m = LimitOne.Match(sql)).Success)
                cmd.CommandText = sql.Substring(0, m.Index) + $"LIMIT {Fix(m.Groups[1].Value, 1, MaxPageSize)}" +
                                  (m.Groups[2].Success ? $" OFFSET {Fix(m.Groups[2].Value, 0, int.MaxValue)}" : "") + sql.Substring(m.Index + m.Length);
        }
    }
}
