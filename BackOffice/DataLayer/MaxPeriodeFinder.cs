using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public class MaxPeriodeFinder
    {
            public  int GetMaxPeriode()
            {
            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                string sqlQuery = "SELECT MAX(periode) FROM pos_periode";

                using (OracleCommand command = new OracleCommand(sqlQuery, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            return reader.GetInt32(0);
                        }
                    }
                }
            }

            return -1; // Return a default value or handle the case when there are no records in the table

        }
    }
}
