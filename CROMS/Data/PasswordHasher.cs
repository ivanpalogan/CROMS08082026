using System;
using System.Security.Cryptography;

namespace CROMS.Data
{
    /// <summary>
    /// Password hashing for CROMS user accounts. Uses PBKDF2 (Rfc2898DeriveBytes) with
    /// HMAC-SHA256, a per-password random salt, and 100,000 iterations — all built into
    /// .NET Framework, so no extra NuGet package is required. Passwords are NEVER stored
    /// in plain text; only the derived hash is kept.
    ///
    /// Stored format (fits users.password_hash VARCHAR(255)):
    ///   "&lt;iterations&gt;.&lt;base64 salt&gt;.&lt;base64 hash&gt;"
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;   // 128-bit salt
        private const int KeySize = 32;    // 256-bit derived key

        /// <summary>Hashes a plaintext password into the stored string form.</summary>
        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            byte[] key = Derive(password, salt, Iterations);
            return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(key);
        }

        /// <summary>True if the password matches the stored hash. Never throws.</summary>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            string[] parts = stored.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations)) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch { return false; }

            byte[] actual = Derive(password, salt, iterations);
            return FixedTimeEquals(actual, expected);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password ?? "", salt, iterations, HashAlgorithmName.SHA256))
                return pbkdf2.GetBytes(KeySize);
        }

        /// <summary>Length-constant comparison, so verification time can't leak the hash.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
