using BackOffice.Interface;
using BackOffice.Model;
using BackOffice.UC;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using System;
using System.Data;
using System.Linq;

namespace BackOffice.DataLayer
{
    public class KASRepository : IKas
    {

        public List<FinBayarVia> PembayaranVia(string kode  )
        {
            // Define the SQL query
            string query = "SELECT ID, KODE,NAMA,NOMOR_REK, ACKODE FROM FIN_BAYAR_VIA where KATEGORI=:kode order by id";

            try
            {
                // Execute the query using Dapper
                using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
                {
                    dbConnection.Open();

                    IEnumerable<FinBayarVia> result = dbConnection.Query<FinBayarVia>(query,new {kode=kode});

                    // Return the result for further processing
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve payment via list");
                // Handle the exception as needed
                return new List<FinBayarVia>(); // Or throw the exception if appropriate
            }
        }

        public List<FinJenisBayar> JenisBayar()
        {
            // Define the SQL query
            string query = "SELECT ID, NAMA, ACKODE FROM FIN_JENISBAYAR order by id";

            try
            {
                // Execute the query using Dapper
                using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
                {
                    dbConnection.Open();

                    IEnumerable<FinJenisBayar> result = dbConnection.Query<FinJenisBayar>(query);

                    // Return the result for further processing
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve payment type list");
                // Handle the exception as needed
                return new List<FinJenisBayar>(); // Or throw the exception if appropriate
            }
        }

        public List<DTOTransaksiKAS> KAS_Transaksi(string idKas, DateTime startDate, DateTime endDate)
        {
            using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
            {
                dbConnection.Open();

                string query = @"
                        SELECT NOMOR,
                               TANGGAL,
                               KETERANGAN,
                               DEBET,
                               KREDIT,
                               SUM(DEBET - KREDIT) OVER (ORDER BY INOUT, NOMOR) AS SALDO
                        FROM (
                            SELECT 0 AS INOUT,
                                   '-' AS NOMOR,
                                   :StartDate AS TANGGAL,
                                   'SALDO AWAL' AS KETERANGAN,
                                   SALDO AS DEBET,
                                   0 AS KREDIT
                            FROM FIN_KAS_SALDO
                            WHERE IDKAS = :IdKas AND TANGGAL = :StartDate 
                            UNION
                            SELECT INOUT,
                                   NOMOR,
                                   TANGGAL,
                                   KETERANGAN,
                                   DEBET,
                                   KREDIT
                            FROM FIN_KAS_TRANSAKSI
                            WHERE IDKAS = :IdKas AND TANGGAL BETWEEN :StartDate AND :EndDate
                        ) SUB
                        ORDER BY SUB.INOUT, SUB.NOMOR";

                var parameters = new
                {
                    IdKas = idKas,
                    StartDate = startDate,
                    EndDate = endDate
                };

                List<DTOTransaksiKAS> result = dbConnection.Query<DTOTransaksiKAS>(query, parameters).ToList();

                return result;
            }
        }


        public List<DTOTransaksiKAS> Edit_KAS_Transaksi(string p_nomorkas)
        {
            using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
            {
                dbConnection.Open();

                // Assuming DTOTransaksiKAS has a primary key property named Nomor
                string query = @"
                SELECT NOMOR, TANGGAL, KET AS KETERANGAN, DEBET, KREDIT
                FROM FIN_KAS_TRANSAKSI
                WHERE NOMOR = :Nomor";

                var parameters = new
                {
                    Nomor = p_nomorkas
                };

                List<DTOTransaksiKAS> result = dbConnection.Query<DTOTransaksiKAS>(query, parameters).ToList();

                return result;
            }
        }

        public void Delete_KAS(string p_nomorkas)
        {
            using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
            {
                dbConnection.Open();

                // Assuming DTOTransaksiKAS has a primary key property named Nomor
                string query = @"
                DELETE FROM FIN_KAS_TRANSAKSI
                WHERE NOMOR = :Nomor";

                var parameters = new
                {
                    Nomor = p_nomorkas
                };

                dbConnection.Execute(query, parameters, commandType: CommandType.Text);
            }
        }
    }
}
