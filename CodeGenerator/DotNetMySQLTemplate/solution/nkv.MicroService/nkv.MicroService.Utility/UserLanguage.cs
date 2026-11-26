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
            using (var t = Db.Command(Db.CreateTableIfMissing("user_preferences", $@"user_id INT NOT NULL PRIMARY KEY, language VARCHAR(20) NULL,
                    saved_views {Db.LongTextType} NULL, modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP"), c)) t.ExecuteNonQuery();
            // tables made before saved views: add the column
            try { using var probe = Db.Command("SELECT saved_views FROM user_preferences WHERE 1 = 0", c); probe.ExecuteNonQuery(); }
            catch (DbException) { using var add = Db.Command($"ALTER TABLE user_preferences ADD saved_views {Db.LongTextType} NULL", c); add.ExecuteNonQuery(); }
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

    /// <summary>
    /// Each user's saved list views (filters, search and sort per table), one JSON document in user_preferences.saved_views.
    /// The UI owns the format; the API only stores it (up to 256 KB).
    /// </summary>
    public static class UserViews
    {
        public const int MaxLength = 256 * 1024;

        public static string Get(DbConnection c, int userId)
        {
            UserLanguage.Ensure(c);
            using var q = Db.Command("SELECT saved_views FROM user_preferences WHERE user_id = @u", c);
            q.Parameters.AddWithValue("@u", userId);
            return q.ExecuteScalar() is string s && s.Length > 0 ? s : null;
        }

        public static void Set(DbConnection c, int userId, string json)
        {
            UserLanguage.Ensure(c);
            // only saved_views and modifiedAt change: the language is kept
            using var q = Db.Command(Db.Upsert("user_preferences", new[] { "user_id", "saved_views", "modifiedAt" }, new[] { "user_id" }), c);
            q.Parameters.AddWithValue("@user_id", userId);
            q.Parameters.AddWithValue("@saved_views", (object)json ?? DBNull.Value);
            q.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
            Db.Execute(q);
        }
    }
}
