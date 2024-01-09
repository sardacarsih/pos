using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public class RBACManager : IRBACManager
    {
        private OracleConnection connection;
        private readonly string connectionString;
        public RBACManager()
        {
            connectionString = global.connectionString;
            connection = new OracleConnection(connectionString);
            connection.Open();
        }

        public User GetUserByUsername(string username)
        {
            using OracleConnection connection = new(connectionString);
            string query = "SELECT UserId, Username, Password FROM Users WHERE Username = :username";
            return connection.QuerySingleOrDefault<User>(query, new { username });
        }

        public List<Role> GetRolesByUserId(int userId)
        {
            using OracleConnection connection = new(connectionString);
            string query = @"SELECT r.RoleId, r.RoleName
                             FROM Roles r
                             INNER JOIN UserRoleMappings urm ON r.RoleId = urm.RoleId
                             WHERE urm.UserId = :userId";
            return connection.Query<Role>(query, new { userId }).ToList();
        }

        public List<Permission> GetPermissionsByRoleId(int roleId)
        {
            using OracleConnection connection = new(connectionString);
            string query = @"SELECT p.PermissionId, p.PermissionIdParent,p.PermissionName
                             FROM Permissions p
                             INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
                             WHERE rp.RoleId = :roleId";
            return connection.Query<Permission>(query, new { roleId }).ToList();
        }
    }
}
