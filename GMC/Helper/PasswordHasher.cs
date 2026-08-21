using System.Security.Cryptography;
using System.Text;

namespace GMC.Helper
{
    /// <summary>
    /// PBKDF2-SHA256 password hasher with salt + iteration count baked into
    /// the output string.  Hashes are produced in the form:
    ///
    ///     PBKDF2$&lt;iterations&gt;$&lt;base64-salt&gt;$&lt;base64-hash&gt;
    ///
    /// The verify method accepts both plaintext (legacy users — exact match)
    /// and PBKDF2-formatted hashes, so this can be rolled out gradually:
    ///
    /// 1. New accounts created via UserRegistration are stored as hashes.
    /// 2. Existing accounts continue to authenticate against their plaintext.
    /// 3. On successful plaintext login, the BL may opportunistically
    ///    re-write the row to a hash to migrate the user forward.
    ///
    /// To switch this on, call <see cref="Hash"/> from UserRegistrationDAL and
    /// add a call to <see cref="Verify"/> inside LoginDAL.ValidateUser*.
    /// </summary>
    public static class PasswordHasher
    {
        private const int    SaltSize       = 16;
        private const int    HashSize       = 32;
        private const int    DefaultIters   = 100_000;
        private const string Prefix         = "PBKDF2";

        public static string Hash(string password, int iterations = DefaultIters)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iterations,
                HashAlgorithmName.SHA256, HashSize);
            return $"{Prefix}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary> True if <paramref name="password"/> matches the stored value. </summary>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;

            // Legacy plaintext fallback (constant-time compare).
            if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(password),
                    Encoding.UTF8.GetBytes(stored));

            var parts = stored.Split('$');
            if (parts.Length != 4) return false;
            if (!int.TryParse(parts[1], out var iters)) return false;
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] hash = Convert.FromBase64String(parts[3]);

            byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iters,
                HashAlgorithmName.SHA256, hash.Length);
            return CryptographicOperations.FixedTimeEquals(candidate, hash);
        }

        /// <summary> True if a stored value is already PBKDF2-formatted. </summary>
        public static bool IsHashed(string stored) =>
            !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
    }
}
