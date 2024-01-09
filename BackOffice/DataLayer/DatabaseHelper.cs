using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public static class global
    {

      public static readonly string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ERP)));User Id=ADMIN_ERP;Password=ADMIN_ERPboss;";
    //public static readonly string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.10.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=kopkar;Password=kopkarboss;";

    }
}
