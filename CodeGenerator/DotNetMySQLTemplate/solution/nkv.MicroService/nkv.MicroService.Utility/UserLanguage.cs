using System;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// The language each user chose in the app (the UI's language picker), kept in the built-in user_preferences table.
    /// The UI uses it on every device; the API uses it for emails and messages to that user.
    /// </summary>
    public static class UserLanguage
    {
        private static bool _ready;
        private static readonly Regex Code = new(@"^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})?$");

        public static bool IsValid(string code) => !string.IsNullOrWhiteSpace(code) && Code.IsMatch(code.Trim());

        public static void Ensure(DbConnection c)
        {
            if (_ready) return;
            using (var t = Db.Command(Db.CreateTableIfMissing("user_preferences", @"user_id INT NOT NULL PRIMARY KEY, language VARCHAR(20) NULL,
                    modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP"), c)) t.ExecuteNonQuery();
            _ready = true;
        }

        /// <summary>The user's language code ("fr", "zh-CN"), or null when they haven't chosen one.</summary>
        public static string Get(DbConnection c, int userId, DbTransaction tx = null)
        {
            Ensure(c);
            using var q = Db.Command("SELECT language FROM user_preferences WHERE user_id = @u", c, tx);
            q.Parameters.AddWithValue("@u", userId);
            return q.ExecuteScalar() is string s && s.Length > 0 ? s : null;
        }

        public static void Set(DbConnection c, int userId, string language)
        {
            Ensure(c);
            var code = language?.Trim();
            using var q = Db.Command(Db.Upsert("user_preferences", new[] { "user_id", "language", "modifiedAt" }, new[] { "user_id" }), c);
            q.Parameters.AddWithValue("@user_id", userId);
            q.Parameters.AddWithValue("@language", string.IsNullOrEmpty(code) ? DBNull.Value : code);
            q.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
            Db.Execute(q);
        }
    }
}
