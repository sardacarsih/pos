using System;
using System.Collections.Generic;

namespace BackOffice
{
    public static class LoginInfo
    {
        public static string userID;
        public static string role;

        /// <summary>Role ADMIN selalu memiliki akses penuh ke seluruh modul BackOffice.</summary>
        public static bool HasFullAccess =>
            string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

        /// <summary>USER_ID numerik dari POS_USER.</summary>
        public static int UserId { get; set; }

        /// <summary>Nama lengkap user yang login (POS_USER.FULL_NAME).</summary>
        public static string FullName { get; set; }

        /// <summary>APP_ID yang bisa diakses user yang sedang login (untuk gating modul).</summary>
        public static HashSet<string> AccessibleApps { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
