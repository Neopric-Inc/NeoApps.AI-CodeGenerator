namespace nkv.MicroService.Utility
{
    /// <summary>BCrypt password hashing. Accepts legacy plain-text rows so older databases keep working.</summary>
    public static class PasswordHasher
    {
        public static bool IsHash(string value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith("$2") && value.Length >= 59;

        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 11);

        /// <summary>Hashes the value unless it is already a BCrypt hash.</summary>
        public static string EnsureHashed(string value) =>
            string.IsNullOrEmpty(value) || IsHash(value) ? value : Hash(value);

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;
            if (!IsHash(stored)) return password == stored; // legacy plain-text row
            try { return BCrypt.Net.BCrypt.Verify(password, stored); }
            catch { return false; }
        }
    }
}
