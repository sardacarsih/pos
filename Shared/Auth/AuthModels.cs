using System.Collections.Generic;

namespace Pos.Shared.Auth
{
    /// <summary>Baris user untuk layar manajemen user.</summary>
    public sealed class UserAccount
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public int FailedAttempts { get; set; }
    }

    /// <summary>Aplikasi (POS_APP).</summary>
    public sealed class AppInfo
    {
        public string AppId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>Role (POS_ROLE).</summary>
    public sealed class RoleInfo
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>Akses satu user ke satu app dengan role (POS_USER_ACCESS).</summary>
    public sealed class UserAccessRow
    {
        public int UserId { get; set; }
        public string AppId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
