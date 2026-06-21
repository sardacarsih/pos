using Pos.Shared.Auth;

namespace Migrator
{
    /// <summary>
    /// Membuat user Admin default (idempotent) dengan akses ADMIN ke semua aplikasi.
    /// Memakai ulang AuthRepository &amp; PasswordCryptographyPbkdf2 dari Shared/Auth.
    /// </summary>
    public static class AdminSeeder
    {
        public static void Seed(string connectionString, string username, string password)
        {
            username = username.Trim().ToLowerInvariant();
            var repo = new AuthRepository(connectionString);

            if (repo.UsernameExists(username))
            {
                Console.WriteLine($"  [skip ] user '{username}' sudah ada.");
                return;
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
        }
    }
}
