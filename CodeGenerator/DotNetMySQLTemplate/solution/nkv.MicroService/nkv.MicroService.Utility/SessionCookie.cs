using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Browser sessions: the access token lives in an HttpOnly cookie, so page scripts (and anyone with dev tools open on a
    /// shared machine) can't read or copy it. API clients still send "Authorization: Bearer ...".
    /// Writes authenticated by the cookie must carry the X-NeoApps-Client header (a custom header forces a CORS preflight,
    /// so other sites can't forge requests).
    /// </summary>
    public static class SessionCookie
    {
        public const string Name = "neoapps_session";
        public const string ClientHeader = "X-NeoApps-Client";
        static string _sameSite = "Strict";
        static string _secure = "auto";

        public static void Init(IConfiguration c)
        {
            _sameSite = c.GetValue("Auth:CookieSameSite", "Strict");
            _secure = c.GetValue("Auth:CookieSecure", "auto");
        }

        /// <summary>True when the caller is the generated web UI (it never needs the token in the response body).</summary>
        public static bool IsBrowserClient(HttpRequest r) => r.Headers.ContainsKey(ClientHeader);

        static bool Secure(HttpRequest r) => _secure.Equals("true", StringComparison.OrdinalIgnoreCase)
            || (_secure.Equals("auto", StringComparison.OrdinalIgnoreCase)
                && (r.IsHttps || string.Equals(r.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase)));

        static CookieOptions Options(HttpRequest r, DateTimeOffset? expires) => new CookieOptions
        {
            HttpOnly = true,
            Secure = Secure(r),
            SameSite = Enum.TryParse<SameSiteMode>(_sameSite, true, out var m) ? m : SameSiteMode.Strict,
            Path = "/",
            Expires = expires,
            IsEssential = true,
        };

        public static void Set(HttpResponse res, string token, DateTime validTo) =>
            res.Cookies.Append(Name, token, Options(res.HttpContext.Request, new DateTimeOffset(DateTime.SpecifyKind(validTo, DateTimeKind.Utc))));

        public static void Clear(HttpResponse res) => res.Cookies.Delete(Name, Options(res.HttpContext.Request, null));

        /// <summary>The caller's raw token (bearer header, else session cookie) for code that passes it on.</summary>
        public static string RequestToken(HttpRequest r)
        {
            var h = r.Headers.Authorization.ToString();
            if (h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var t = h.Substring(7).Trim();
                if (t.Length > 0 && t != "null" && t != "undefined") return t;
            }
            return r.Cookies.TryGetValue(Name, out var c) && !string.IsNullOrEmpty(c) ? c : null;
        }

        /// <summary>Token for JwtBearer: an explicit bearer header wins; otherwise the session cookie (reads always, writes only with the client header).</summary>
        public static string TokenFor(HttpRequest r)
        {
            var h = r.Headers.Authorization.ToString();
            if (h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var t = h.Substring(7).Trim();
                if (t.Length > 0 && t != "null" && t != "undefined") return null; // let JwtBearer read the header
                r.Headers.Remove("Authorization");
            }
            if (!r.Cookies.TryGetValue(Name, out var cookie) || string.IsNullOrEmpty(cookie)) return null;
            var safe = HttpMethods.IsGet(r.Method) || HttpMethods.IsHead(r.Method) || HttpMethods.IsOptions(r.Method);
            return safe || IsBrowserClient(r) ? cookie : null;
        }
    }
}
