using System;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice.DataLayer
{
    public class FinSettingsDataAccess
    {
        public int GetMaxAngsuran()
        {
            int maxAngsuran = 0;

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                string checkExistenceQuery = "SELECT COUNT(*) FROM FIN_SETTINGS WHERE CONFIG = 'MAXANGSURAN'";
                string insertQuery = "INSERT INTO FIN_SETTINGS (CONFIG, JUMLAH) VALUES ('MAXANGSURAN', 12)";

                using (OracleCommand checkExistenceCommand = new OracleCommand(checkExistenceQuery, connection))
                {
                    connection.Open();

                    int rowCount = Convert.ToInt32(checkExistenceCommand.ExecuteScalar());

                    if (rowCount == 0)
                    {
                        using (OracleCommand insertCommand = new OracleCommand(insertQuery, connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            maxAngsuran = 12;
                        }
                    }
                    else
                    {
                        string selectQuery = "SELECT JUMLAH FROM FIN_SETTINGS WHERE CONFIG = 'MAXANGSURAN'";

                        using (OracleCommand selectCommand = new OracleCommand(selectQuery, connection))
                        {
                            OracleDataReader reader = selectCommand.ExecuteReader();

                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    maxAngsuran = Convert.ToInt32(reader["JUMLAH"]);
                                }
                            }

                            reader.Close();
                        }
                    }
                }
            }

            return maxAngsuran;
        }

        public int GetSimpananWajib()
        {
            int simpananWajib = 0;

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                string checkExistenceQuery = "SELECT COUNT(*) FROM FIN_SETTINGS WHERE CONFIG = 'SIMPANAN_WAJIB'";
                string insertQuery = "INSERT INTO FIN_SETTINGS (CONFIG, JUMLAH) VALUES ('SIMPANAN_WAJIB', 50000)";

                using (OracleCommand checkExistenceCommand = new OracleCommand(checkExistenceQuery, connection))
                {
                    connection.Open();

                    int rowCount = Convert.ToInt32(checkExistenceCommand.ExecuteScalar());

                    if (rowCount == 0)
                    {
                        using (OracleCommand insertCommand = new OracleCommand(insertQuery, connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            simpananWajib = 50000;
                        }
                    }
                    else
                    {
                        string selectQuery = "SELECT JUMLAH FROM FIN_SETTINGS WHERE CONFIG = 'SIMPANAN_WAJIB'";

                        using (OracleCommand selectCommand = new OracleCommand(selectQuery, connection))
                        {
                            OracleDataReader reader = selectCommand.ExecuteReader();

                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    simpananWajib = Convert.ToInt32(reader["JUMLAH"]);
                                }
                            }

                            reader.Close();
                        }
                    }
                }
            }

            return simpananWajib;
        }
        public double GetBungaEfektif()
        {
            double bungaEfektif = 0;

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                string checkExistenceQuery = "SELECT COUNT(*) FROM FIN_SETTINGS WHERE CONFIG = 'BUNGA_EFEKTIF'";
                string insertQuery = "INSERT INTO FIN_SETTINGS (CONFIG, JUMLAH) VALUES ('BUNGA_EFEKTIF', 1.6)";

                using (OracleCommand checkExistenceCommand = new OracleCommand(checkExistenceQuery, connection))
                {
                    connection.Open();

                    int rowCount = Convert.ToInt32(checkExistenceCommand.ExecuteScalar());

                    if (rowCount == 0)
                    {
                        using (OracleCommand insertCommand = new OracleCommand(insertQuery, connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            bungaEfektif = 1.6; // Set the default value as 1.6
                        }
                    }
                    else
                    {
                        string selectQuery = "SELECT JUMLAH FROM FIN_SETTINGS WHERE CONFIG = 'BUNGA_EFEKTIF'";

                        using (OracleCommand selectCommand = new OracleCommand(selectQuery, connection))
                        {
                            OracleDataReader reader = selectCommand.ExecuteReader();

                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    bungaEfektif = Convert.ToDouble(reader["JUMLAH"]);
                                }
                            }

                            reader.Close();
                        }
                    }
                }
            }

            return bungaEfektif;
        }
    }
}