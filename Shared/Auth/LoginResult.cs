namespace Pos.Shared.Auth
{
    /// <summary>
    /// Hasil lookup login untuk satu user pada satu aplikasi (APP_ID).
    /// Dipetakan oleh Dapper dari join POS_USER + POS_USER_ACCESS + POS_ROLE.
    /// </summary>
    public sealed class LoginResult
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
    }
}
