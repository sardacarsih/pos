using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class Permission
    {
        public int PermissionId { get; set; }
        public int PermissionIdParent { get; set; }
        public string PermissionName { get; set; }
    }

    public class UserRoleMapping
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class RolePermissionMapping
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }

}
