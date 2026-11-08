using Pos.Shared.Auth;

namespace Migrator
{
    /// <summary>
    /// Membuat user Admin default (idempotent) dengan akses ADMIN ke semua aplikasi.
    /// Memakai ulang AuthRepository &amp; PasswordCryptographyPbkdf2 dari Shared/Auth.
    /// </summary>
    public static class AdminSeeder
    {
        /// <returns>true bila user baru dibuat, false bila sudah ada.</returns>
        public static bool Seed(string connectionString, string username, string password)
        {
            username = username.Trim().ToLowerInvariant();
            var repo = new AuthRepository(connectionString);

            if (repo.UsernameExists(username))
            {
                Console.WriteLine($"  [skip ] user '{username}' sudah ada.");
                return false;
            }

            RoleInfo adminRole = repo.ListRoles()
                .FirstOrDefault(r => r.RoleName.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Role 'ADMIN' tidak ada - jalankan migrasi terlebih dahulu.");

            var crypto = new PasswordCryptographyPbkdf2();
            int userId = repo.InsertUser(username, "Administrator", crypto.GetHashPassword(password));

            List<AppInfo> apps = repo.ListApps();
            foreach (AppInfo app in apps)
            {
                repo.UpsertAccess(userId, app.AppId, adminRole.RoleId);
            }

            Console.WriteLine($"  [done ] user admin '{username}' dibuat, akses ADMIN ke {apps.Count} aplikasi.");
            return true;
        }

        /// <summary>
        /// Reset password user yang sudah ada (PBKDF2 yang sama dengan BackOffice) sekaligus
        /// mengaktifkan dan membuka kunci akun. Role dan akses tidak diubah.
        /// </summary>
        /// <returns>false bila user tidak ditemukan.</returns>
        public static bool Reset(string connectionString, string username, string password)
        {
            username = username.Trim().ToLowerInvariant();
            var repo = new AuthRepository(connectionString);

            UserAccount? user = repo.ListUsers()
                .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                Console.WriteLine($"  [fail ] user '{username}' tidak ditemukan.");
                return false;
            }

            repo.UpdatePassword(user.UserId, new PasswordCryptographyPbkdf2().GetHashPassword(password));
            repo.SetActive(user.UserId, true);
            repo.SetLocked(user.UserId, false); // juga mereset counter gagal login
            Console.WriteLine($"  [done ] password '{username}' direset; akun diaktifkan dan dibuka kuncinya.");
            return true;
        }
    }
}
