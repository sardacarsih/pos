using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IRBACManager
    {
        User GetUserByUsername(string username);
        List<Role> GetRolesByUserId(int userId);
        List<Permission> GetPermissionsByRoleId(int roleId);
    }
}
