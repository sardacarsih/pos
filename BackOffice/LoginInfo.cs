using System;
using System.Collections.Generic;

namespace BackOffice
{
    public static class LoginInfo
    {
        public static string userID;
        public static string role;

        /// <summary>APP_ID yang bisa diakses user yang sedang login (untuk gating modul).</summary>
        public static HashSet<string> AccessibleApps { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
