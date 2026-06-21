using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Pos.Shared.Auth
{
    /// <summary>
    /// Akses data login &amp; manajemen user (POS_USER / POS_USER_ACCESS / POS_ROLE / POS_APP).
    /// Connection string di-inject lewat constructor agar repo bisa dipakai
    /// di semua aplikasi (Penjualan / BackOffice).
    /// </summary>
    public sealed class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private OracleConnection Open()
        {
            var con = new OracleConnection(_connectionString);
            con.Open();
            return con;
        }

        // ---------------------------------------------------------------- Login

        /// <summary>
        /// Ambil data login user untuk satu aplikasi. Return null jika user
        /// tidak ada atau tidak punya akses aktif ke aplikasi tersebut.
        /// </summary>
        public LoginResult? Lookup(string username, string appId)
        {
            const string sql = @"
                SELECT u.USER_ID       AS UserId,
                       u.USERNAME      AS Username,
                       u.FULL_NAME     AS FullName,
                       u.PASSWORD_HASH AS PasswordHash,
                       r.ROLE_NAME     AS RoleName,
                       a.APP_ID        AS AppId,
                       u.IS_ACTIVE     AS IsActive,
                       u.IS_LOCKED     AS IsLocked
                FROM   POS_USER u
                JOIN   POS_USER_ACCESS a ON a.USER_ID = u.USER_ID
                                        AND a.APP_ID  = :appId
                                        AND a.IS_ACTIVE = 1
                JOIN   POS_ROLE r        ON r.ROLE_ID = a.ROLE_ID
                WHERE  u.USERNAME = :username";

            using var con = Open();
            return con.QuerySingleOrDefault<LoginResult>(
                sql, new { username = username.ToLowerInvariant(), appId });
        }

        /// <summary>Daftar APP_ID yang bisa diakses user (untuk gating modul).</summary>
        public HashSet<string> GetAccessibleApps(string username)
        {
            const string sql = @"
                SELECT a.APP_ID
                FROM   POS_USER u
                JOIN   POS_USER_ACCESS a ON a.USER_ID = u.USER_ID AND a.IS_ACTIVE = 1
                WHERE  u.USERNAME = :username
                AND    u.IS_ACTIVE = 1
                AND    u.IS_LOCKED = 0";

            using var con = Open();
            return con.Query<string>(sql, new { username = username.ToLowerInvariant() })
                      .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------------------------------- Counter percobaan

        public void ResetFailedAttempts(string username)
        {
            using var con = Open();
            con.Execute(
                "UPDATE POS_USER SET FAILED_ATTEMPTS = 0, UPDATED_AT = SYSTIMESTAMP WHERE USERNAME = :username",
                new { username = username.ToLowerInvariant() });
        }

        /// <summary>Naikkan counter gagal; kunci akun bila mencapai <paramref name="lockThreshold"/>.</summary>
        public void RegisterFailedAttempt(string username, int lockThreshold = 3)
        {
            using var con = Open();
            con.Execute(@"
                UPDATE POS_USER
                SET    FAILED_ATTEMPTS = FAILED_ATTEMPTS + 1,
                       IS_LOCKED = CASE WHEN FAILED_ATTEMPTS + 1 >= :threshold THEN 1 ELSE IS_LOCKED END,
                       UPDATED_AT = SYSTIMESTAMP
                WHERE  USERNAME = :username",
                new { username = username.ToLowerInvariant(), threshold = lockThreshold });
        }

        // ------------------------------------------------ Manajemen user CRUD

        public List<UserAccount> ListUsers()
        {
            const string sql = @"
                SELECT USER_ID         AS UserId,
                       USERNAME        AS Username,
                       FULL_NAME       AS FullName,
                       IS_ACTIVE       AS IsActive,
                       IS_LOCKED       AS IsLocked,
                       FAILED_ATTEMPTS AS FailedAttempts
                FROM   POS_USER
                ORDER  BY USERNAME";

            using var con = Open();
            return con.Query<UserAccount>(sql).ToList();
        }

        public UserAccount? GetUser(int userId)
        {
            const string sql = @"
                SELECT USER_ID         AS UserId,
                       USERNAME        AS Username,
                       FULL_NAME       AS FullName,
                       IS_ACTIVE       AS IsActive,
                       IS_LOCKED       AS IsLocked,
                       FAILED_ATTEMPTS AS FailedAttempts
                FROM   POS_USER
                WHERE  USER_ID = :userId";

            using var con = Open();
            return con.QuerySingleOrDefault<UserAccount>(sql, new { userId });
        }

        public bool UsernameExists(string username)
        {
            using var con = Open();
            return con.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM POS_USER WHERE USERNAME = :username",
                new { username = username.ToLowerInvariant() }) > 0;
        }

        /// <summary>Buat user baru, return USER_ID.</summary>
        public int InsertUser(string username, string? fullName, string passwordHash)
        {
            using var con = Open();
            con.Execute(@"
                INSERT INTO POS_USER (USERNAME, FULL_NAME, PASSWORD_HASH)
                VALUES (:username, :fullName, :passwordHash)",
                new { username = username.ToLowerInvariant(), fullName, passwordHash });

            return con.ExecuteScalar<int>(
                "SELECT USER_ID FROM POS_USER WHERE USERNAME = :username",
                new { username = username.ToLowerInvariant() });
        }

        public void UpdateUser(int userId, string? fullName)
        {
            using var con = Open();
            con.Execute(
                "UPDATE POS_USER SET FULL_NAME = :fullName, UPDATED_AT = SYSTIMESTAMP WHERE USER_ID = :userId",
                new { userId, fullName });
        }

        public void UpdatePassword(int userId, string passwordHash)
        {
            using var con = Open();
            con.Execute(
                "UPDATE POS_USER SET PASSWORD_HASH = :passwordHash, UPDATED_AT = SYSTIMESTAMP WHERE USER_ID = :userId",
                new { userId, passwordHash });
        }

        public void SetActive(int userId, bool isActive)
        {
            using var con = Open();
            con.Execute(
                "UPDATE POS_USER SET IS_ACTIVE = :isActive, UPDATED_AT = SYSTIMESTAMP WHERE USER_ID = :userId",
                new { userId, isActive = isActive ? 1 : 0 });
        }

        /// <summary>Set/lepas kunci akun. Membuka kunci juga mereset counter gagal.</summary>
        public void SetLocked(int userId, bool isLocked)
        {
            using var con = Open();
            con.Execute(@"
                UPDATE POS_USER
                SET    IS_LOCKED = :isLocked,
                       FAILED_ATTEMPTS = CASE WHEN :isLocked = 0 THEN 0 ELSE FAILED_ATTEMPTS END,
                       UPDATED_AT = SYSTIMESTAMP
                WHERE  USER_ID = :userId",
                new { userId, isLocked = isLocked ? 1 : 0 });
        }

        // ---------------------------------------------------- Akses (app+role)

        public List<UserAccessRow> GetUserAccess(int userId)
        {
            const string sql = @"
                SELECT a.USER_ID   AS UserId,
                       a.APP_ID    AS AppId,
                       ap.APP_NAME AS AppName,
                       a.ROLE_ID   AS RoleId,
                       r.ROLE_NAME AS RoleName,
                       a.IS_ACTIVE AS IsActive
                FROM   POS_USER_ACCESS a
                JOIN   POS_APP  ap ON ap.APP_ID  = a.APP_ID
                JOIN   POS_ROLE r  ON r.ROLE_ID  = a.ROLE_ID
                WHERE  a.USER_ID = :userId
                ORDER  BY a.APP_ID";

            using var con = Open();
            return con.Query<UserAccessRow>(sql, new { userId }).ToList();
        }

        /// <summary>Tambah/ubah akses user ke satu app dengan role tertentu.</summary>
        public void UpsertAccess(int userId, string appId, int roleId, bool isActive = true)
        {
            const string sql = @"
                MERGE INTO POS_USER_ACCESS a
                USING (SELECT :userId AS USER_ID, :appId AS APP_ID FROM DUAL) src
                ON (a.USER_ID = src.USER_ID AND a.APP_ID = src.APP_ID)
                WHEN MATCHED THEN
                    UPDATE SET a.ROLE_ID = :roleId, a.IS_ACTIVE = :isActive
                WHEN NOT MATCHED THEN
                    INSERT (USER_ID, APP_ID, ROLE_ID, IS_ACTIVE)
                    VALUES (:userId, :appId, :roleId, :isActive)";

            using var con = Open();
            con.Execute(sql, new { userId, appId, roleId, isActive = isActive ? 1 : 0 });
        }

        public void RemoveAccess(int userId, string appId)
        {
            using var con = Open();
            con.Execute(
                "DELETE FROM POS_USER_ACCESS WHERE USER_ID = :userId AND APP_ID = :appId",
                new { userId, appId });
        }

        // ---------------------------------------------------- Katalog app/role

        public List<AppInfo> ListApps(bool activeOnly = true)
        {
            string sql = @"
                SELECT APP_ID AS AppId, APP_NAME AS AppName, IS_ACTIVE AS IsActive
                FROM   POS_APP";
            if (activeOnly)
            {
                sql += " WHERE IS_ACTIVE = 1";
            }
            sql += " ORDER BY APP_NAME";

            using var con = Open();
            return con.Query<AppInfo>(sql).ToList();
        }

        public List<RoleInfo> ListRoles(bool activeOnly = true)
        {
            string sql = @"
                SELECT ROLE_ID AS RoleId, ROLE_NAME AS RoleName, IS_ACTIVE AS IsActive
                FROM   POS_ROLE";
            if (activeOnly)
            {
                sql += " WHERE IS_ACTIVE = 1";
            }
            sql += " ORDER BY ROLE_NAME";

            using var con = Open();
            return con.Query<RoleInfo>(sql).ToList();
        }
    }
}
