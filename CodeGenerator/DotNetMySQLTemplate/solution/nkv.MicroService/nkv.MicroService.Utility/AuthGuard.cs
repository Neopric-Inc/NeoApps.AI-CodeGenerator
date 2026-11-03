using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Login protection for generated APIs (appsettings "Auth", all optional):
    ///   "MaxFailedLogins": 5, "LockoutMinutes": 15   lock a username after repeated failures (unknown usernames too)
    ///   "LoginsPerMinutePerIp": 20                   rate limit on POST /Token (429)
    ///   "MinPasswordLength": 10                      plus letters and digits, not the username, not a common password
    ///   "TokenMinutes": 480                          access token lifetime
    /// Revocation: every token carries the user's token version; changing the password, role or active flag, signing out,
    /// or an admin "sign out everywhere" bumps it and all existing tokens stop working (checked on every request, 30s cache).
    /// </summary>
    public static class AuthGuard
    {
        public const string VersionClaim = "neoapps:tv";
        static string _cs;
        public static int MaxFailed { get; private set; } = 5;
        public static int LockoutMinutes { get; private set; } = 15;
        public static int LoginsPerMinutePerIp { get; private set; } = 20;
        public static int MinPasswordLength { get; private set; } = 10;
        public static int TokenMinutes { get; private set; } = 480;
        static readonly ConcurrentDictionary<int, (int version, bool active, DateTime at)> _cache = new();

        public static void Init(IConfiguration c, string connectionString)
        {
            _cs = connectionString;
            MaxFailed = Math.Max(1, c.GetValue("Auth:MaxFailedLogins", 5));
            LockoutMinutes = Math.Max(1, c.GetValue("Auth:LockoutMinutes", 15));
            LoginsPerMinutePerIp = Math.Max(1, c.GetValue("Auth:LoginsPerMinutePerIp", 20));
            MinPasswordLength = Math.Max(8, c.GetValue("Auth:MinPasswordLength", 10));
            // legacy AppSettings:TokenValidityDay is ignored in favour of a short lifetime
            TokenMinutes = Math.Clamp(c.GetValue("Auth:TokenMinutes", 480), 5, 60 * 24 * 30);
            using var conn = Open();
            foreach (var sql in new[]
            {
                @"CREATE TABLE IF NOT EXISTS login_attempts (username VARCHAR(255) NOT NULL PRIMARY KEY, failed INT NOT NULL DEFAULT 0,
                    first_failed_at DATETIME NULL, locked_until DATETIME NULL)",
                @"CREATE TABLE IF NOT EXISTS user_security (user_id INT NOT NULL PRIMARY KEY, token_version INT NOT NULL DEFAULT 1,
                    password_changed_at DATETIME NULL)",
            })
            { using var cmd = new MySqlCommand(sql, conn); cmd.ExecuteNonQuery(); }
        }

        static MySqlConnection Open() { var c = new MySqlConnection(_cs); c.Open(); return c; }
        static string Key(string username) => (username ?? "").Trim().ToLowerInvariant();

        /// <summary>Minutes left on a lockout for this username, or 0.</summary>
        public static int LockedMinutes(string username)
        {
            using var c = Open();
            using var q = new MySqlCommand("SELECT TIMESTAMPDIFF(SECOND, UTC_TIMESTAMP(), locked_until) FROM login_attempts WHERE username = @u AND locked_until > UTC_TIMESTAMP()", c);
            q.Parameters.AddWithValue("@u", Key(username));
            var v = q.ExecuteScalar();
            return v == null || v is DBNull ? 0 : (int)Math.Ceiling(Convert.ToDouble(v) / 60.0);
        }

        /// <summary>Counts a failed login (window = lockout period); locks the username when the limit is reached.</summary>
        public static void RecordFailure(string username)
        {
            using var c = Open();
            using var q = new MySqlCommand(@"INSERT INTO login_attempts (username, failed, first_failed_at, locked_until)
                VALUES (@u, 1, UTC_TIMESTAMP(), IF(1 >= @max, UTC_TIMESTAMP() + INTERVAL @m MINUTE, NULL))
                ON DUPLICATE KEY UPDATE
                  failed = IF(first_failed_at IS NULL OR first_failed_at < UTC_TIMESTAMP() - INTERVAL @m MINUTE, 1, failed + 1),
                  first_failed_at = IF(first_failed_at IS NULL OR first_failed_at < UTC_TIMESTAMP() - INTERVAL @m MINUTE, UTC_TIMESTAMP(), first_failed_at),
                  locked_until = IF(failed >= @max, UTC_TIMESTAMP() + INTERVAL @m MINUTE, locked_until)", c); // MySQL applies these in order: failed is already updated
            q.Parameters.AddWithValue("@u", Key(username));
            q.Parameters.AddWithValue("@m", LockoutMinutes);
            q.Parameters.AddWithValue("@max", MaxFailed);
            q.ExecuteNonQuery();
        }

        public static void RecordSuccess(string username)
        {
            using var c = Open();
            using var q = new MySqlCommand("DELETE FROM login_attempts WHERE username = @u", c);
            q.Parameters.AddWithValue("@u", Key(username));
            q.ExecuteNonQuery();
        }

        /// <summary>Clears a lockout (administrators: Admin > Members).</summary>
        public static void Unlock(string username) => RecordSuccess(username);

        public static int TokenVersion(int userId)
        {
            using var c = Open();
            using var q = new MySqlCommand("SELECT token_version FROM user_security WHERE user_id = @u", c);
            q.Parameters.AddWithValue("@u", userId);
            var v = q.ExecuteScalar();
            return v == null || v is DBNull ? 1 : Convert.ToInt32(v);
        }

        /// <summary>Ends every existing session of the user.</summary>
        public static void RevokeTokens(int userId, bool passwordChanged = false)
        {
            using var c = Open();
            using var q = new MySqlCommand($@"INSERT INTO user_security (user_id, token_version{(passwordChanged ? ", password_changed_at" : "")})
                VALUES (@u, 2{(passwordChanged ? ", UTC_TIMESTAMP()" : "")})
                ON DUPLICATE KEY UPDATE token_version = token_version + 1{(passwordChanged ? ", password_changed_at = UTC_TIMESTAMP()" : "")}", c);
            q.Parameters.AddWithValue("@u", userId);
            q.ExecuteNonQuery();
            _cache.TryRemove(userId, out _);
        }

        /// <summary>True when the token's version is current and the account is still active.</summary>
        public static bool TokenIsCurrent(int userId, int version)
        {
            if (_cs == null) return true;
            if (!_cache.TryGetValue(userId, out var e) || (DateTime.UtcNow - e.at).TotalSeconds > 30)
            {
                using var c = Open();
                using var q = new MySqlCommand(@"SELECT COALESCE(s.token_version, 1), u.isActive FROM users u
                    LEFT JOIN user_security s ON s.user_id = u.user_id WHERE u.user_id = @u", c);
                q.Parameters.AddWithValue("@u", userId);
                using var r = q.ExecuteReader();
                e = r.Read() ? (Convert.ToInt32(r.GetValue(0)), Convert.ToInt32(r.GetValue(1)) == 1, DateTime.UtcNow) : (0, false, DateTime.UtcNow);
                _cache[userId] = e;
            }
            return e.active && e.version == version;
        }

        static readonly string[] Common = { "password", "123456", "qwerty", "letmein", "welcome", "admin", "changeme", "iloveyou", "abc123", "password1" };

        /// <summary>Null when the password is acceptable, otherwise the reason.</summary>
        public static string PasswordProblem(string password, string username)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength) return $"Passwords need at least {MinPasswordLength} characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "Passwords need both letters and digits.";
            if (!string.IsNullOrEmpty(username) && password.Contains(username.Trim(), StringComparison.OrdinalIgnoreCase)) return "Passwords can't contain the username.";
            var lower = password.ToLowerInvariant();
            if (Common.Any(w => lower.StartsWith(w) && lower.Length <= w.Length + 4)) return "That password is too common.";
            return null;
        }
    }
}
