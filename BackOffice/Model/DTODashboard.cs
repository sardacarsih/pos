using BackOffice.DataLayer;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.Model
{
    public class SalesSummary
    {
        public string NAMA_BARANG { get; set; }
        public int QTY { get; set; }
        public decimal JUMLAH { get; set; }
    }
    public class DTODashboard
    {
        public List<SalesSummary> GetTop20SalesSummary(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
            {
                dbConnection.Open();

                string sqlQuery = @"SELECT NAMA_BARANG, 
                                   SUM(JUMLAH_BARANG) AS QTY, 
                                   SUM(TOTAL_HARGA) AS JUMLAH
                            FROM POS_PENJUALAN_DETAIL D
                            JOIN POS_PENJUALAN M ON M.NO_TRANSAKSI=D.NO_TRANSAKSI
                            WHERE TANGGAL BETWEEN :startDate AND :endDate
                            GROUP BY NAMA_BARANG
                            ORDER BY JUMLAH DESC
                            FETCH FIRST 10 ROWS ONLY";

                var salesList = dbConnection.Query<SalesSummary>(sqlQuery, new { startDate, endDate }).ToList();

                return salesList;
            }
        }

    }
}
