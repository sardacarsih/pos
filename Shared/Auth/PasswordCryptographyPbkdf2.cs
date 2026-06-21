using System;
using System.Security.Cryptography;

namespace Pos.Shared.Auth
{
    /// <summary>
    /// Hashing &amp; verifikasi password berbasis PBKDF2 (best practice OWASP).
    ///
    /// Format storage (self-describing, mendukung naik iterasi tanpa merusak hash lama):
    ///   PBKDF2-SHA256$&lt;iterations&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;
    ///
    /// Default: PBKDF2/SHA256, 600.000 iterasi, salt 16 byte, derived key 32 byte.
    /// </summary>
    public sealed class PasswordCryptographyPbkdf2
    {
        private const string Prefix = "PBKDF2-SHA256";
        private const int SaltSize = 16;   // 128-bit
        private const int HashSize = 32;   // 256-bit
        private const int Iterations = 600_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        /// <summary>Buat hash ter-encode untuk disimpan ke kolom PASSWORD_HASH.</summary>
        public string GetHashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

            return string.Join('$',
                Prefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        /// <summary>
        /// True jika <paramref name="password"/> cocok dengan <paramref name="storedHash"/>.
        /// Parameter (iterasi/salt) dibaca dari string tersimpan sehingga hash yang
        /// dibuat dengan iterasi lebih rendah tetap bisa diverifikasi.
        /// </summary>
        public bool IsValidPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

            // Perbandingan constant-time (anti timing attack).
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        /// <summary>
        /// True jika hash perlu di-rehash (mis. dibuat dengan iterasi di bawah standar
        /// saat ini). Panggil setelah login sukses untuk upgrade transparan.
        /// </summary>
        public bool NeedsRehash(string storedHash)
        {
            string[] parts = storedHash?.Split('$') ?? Array.Empty<string>();
            if (parts.Length != 4 || parts[0] != Prefix)
            {
                return true;
            }
            return !int.TryParse(parts[1], out int iterations) || iterations < Iterations;
        }
    }
}
