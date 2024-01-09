using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Controller
{
    public class RBACController
    {
        private readonly IRBACManager repository;

        public RBACController(IRBACManager repository)
        {
            this.repository = repository;
        }

        //public bool CheckPermission(string username, string permissionName)
        //{
        //    User user = repository.GetUserByUsername(username);
        //    if (user != null)
        //    {
        //        List<Role> roles = repository.GetRolesByUserId(user.UserId);
        //        foreach (Role role in roles)
        //        {
        //            List<Permission> permissions = repository.GetPermissionsByRoleId(role.RoleId);
        //            if (permissions.Any(p => p.PermissionName == permissionName))
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        public bool CheckPermission(string username, string permissionName)
        {
            User user = repository.GetUserByUsername(username);
            if (user != null)
            {
                List<Role> roles = repository.GetRolesByUserId(user.UserId);
                foreach (Role role in roles)
                {
                    List<Permission> permissions = repository.GetPermissionsByRoleId(role.RoleId);
                    if (permissions.Any(p => p.PermissionName == permissionName ))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }

}
