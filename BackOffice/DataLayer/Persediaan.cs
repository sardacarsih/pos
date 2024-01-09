using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraRichEdit.Model;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.DataLayer
{
    public class Persediaan : IPersediaan
    {
        public DataTable FillDictionaryBarang_FromDatabase()
        {
            DataTable dataTable = new DataTable();

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                string query = "SELECT kode_item KODE, Productname NAMA FROM pos_product where aktif='Y' order by Productname";

                using (OracleCommand command = new OracleCommand(query, connection))
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    // Load data directly into DataTable
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }

        public List<DTOKartuStok> KartuStokBarang(string kode, DateTime startdate, DateTime enddate)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            // Ensure the connection is open
            dbConnection.Open();

            // Execute the Oracle function using Dapper
            var result = dbConnection.Query<DTOKartuStok>(
                sql: "SELECT * FROM GET_STOCK_CARD_PIPELINE(:p_kode,:p_start_date, :p_end_date)",
                param: new { p_kode= kode, p_start_date = startdate, p_end_date = enddate }
            );

            // Optionally, you can handle the result or perform additional actions

            return result.ToList();
        }
    }
}
