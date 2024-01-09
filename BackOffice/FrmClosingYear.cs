
using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice
{
    public partial class FrmClosingYear : DevExpress.XtraEditors.XtraForm
    {

        StokOpnameController controller = new();
        List<DTOStockData> persediaan;
        public FrmClosingYear()
        {
            InitializeComponent();

        }
        DateTime p_daritanggal, p_daritanggalr2, p_sampaitanggal, p_tglAngsuran;
        readonly string[] arrayBulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "Nopember", "Desember" };
        int p_bulan, p_tahun, p_remise, p_periode, p_tahun_TOCLOSE, p_periode_TOCLOSE, p_remise_TOCLOSE, p_bulan_TOCLOSE, next_periode;




        private void CreateThisPeriode(int p_periode)
        {
            // Extract the tahun and bulan from next_periode using substr
            string periode_str = p_periode.ToString();
            string tahun_str = periode_str.Substring(0, 4);
            string bulan_str = periode_str.Substring(4);

            int tahun = int.Parse(tahun_str);
            int bulan = int.Parse(bulan_str);
            string namabulan = arrayBulan[bulan - 1];
            string query = "INSERT INTO POS_PERIODE (PERIODE, TAHUN, BULAN, R1DARI,R1SAMPAI,R2DARI,R2SAMPAI,BDARI,BSAMPAI,REMISE1, REMISE2) VALUES " +
                "(:this_periode, :this_tahun, :this_namabulan,:R1DARI,:R1SAMPAI,:R2DARI,:R2SAMPAI,:BDARI,:BSAMPAI, 'T', 'T')";

            // Check if the record already exists
            string checkQuery = "SELECT COUNT(*) FROM POS_PERIODE WHERE PERIODE = :p_periode";
            int count = 0;

            // create a new Oracle connection object
            using OracleConnection conn = new(global.connectionString);
            // open the database connection
            conn.Open();

            // Check if the record already exists
            using (OracleCommand checkCommand = new OracleCommand(checkQuery, conn))
            {
                checkCommand.Parameters.Add(new OracleParameter("p_periode", OracleDbType.Int32) { Value = p_periode });
                count = Convert.ToInt32(checkCommand.ExecuteScalar());
            }

            // Perform the insert only if the record does not exist
            if (count == 0)
            {
                // create a new OracleCommand object for the insert operation
                using (OracleCommand command = new OracleCommand(query, conn))
                {
                    command.Parameters.Add(new OracleParameter("this_periode", OracleDbType.Int32) { Value = p_periode });
                    command.Parameters.Add(new OracleParameter("this_tahun", OracleDbType.Int16) { Value = tahun });
                    command.Parameters.Add(new OracleParameter("this_namabulan", OracleDbType.Varchar2) { Value = namabulan });
                    command.Parameters.Add(new OracleParameter("R1DARI", OracleDbType.Varchar2) { Value = p_daritanggal });
                    command.Parameters.Add(new OracleParameter("R1SAMPAI", OracleDbType.Varchar2) { Value = p_sampaitanggal });
                    command.Parameters.Add(new OracleParameter("R2DARI", OracleDbType.Varchar2) { Value = p_daritanggal });
                    command.Parameters.Add(new OracleParameter("R2SAMPAI", OracleDbType.Varchar2) { Value = p_sampaitanggal });
                    command.Parameters.Add(new OracleParameter("BDARI", OracleDbType.Varchar2) { Value = p_daritanggal });
                    command.Parameters.Add(new OracleParameter("BSAMPAI", OracleDbType.Varchar2) { Value = p_sampaitanggal });

                    command.ExecuteNonQuery();
                }
            }
            else
            {
                string sqlsql = string.Empty;
                if (p_remise == 1)
                {
                    sqlsql = "UPDATE POS_PERIODE SET R1DARI=:1,R1SAMPAI=:2 WHERE PERIODE=:PERIODE";
                }
                else if (p_remise == 2)
                {
                    sqlsql = "UPDATE POS_PERIODE SET R2DARI=:1,R2SAMPAI=:2 WHERE PERIODE=:PERIODE";
                }
                else
                {
                    sqlsql = "UPDATE POS_PERIODE SET BDARI=:1,BSAMPAI=:2 WHERE PERIODE=:PERIODE";

                }
                // create a new OracleCommand object for the insert operation
                using OracleCommand command = new(sqlsql, conn);
                command.Parameters.Add(new OracleParameter("1", OracleDbType.Varchar2) { Value = p_daritanggal.ToString("dd-MMM-yyyy") });
                command.Parameters.Add(new OracleParameter("2", OracleDbType.Varchar2) { Value = p_sampaitanggal.ToString("dd-MMM-yyyy") });
                command.Parameters.Add(new OracleParameter("PERIODE", OracleDbType.Int32) { Value = p_periode });

                command.ExecuteNonQuery();
            }
        }

        private void CreateStatusClosing(int p_remise, int p_periode_TOCLOSE)
        {
            string query = string.Empty;
            if (p_remise == 1)
            {
                query = "UPDATE POS_PERIODE SET REMISE1='Y' WHERE PERIODE=:p_periode_TOCLOSE";
                // create a new Oracle connection object
                using OracleConnection conn = new(global.connectionString);
                // open the database connection
                conn.Open();

                // create a new Oracle command object
                using (OracleCommand cmd = new(query, conn))
                {
                    // bind the PERIODE parameter with the provided value
                    cmd.Parameters.Add("p_periode_TOCLOSE", p_periode_TOCLOSE);

                    // execute the query and retrieve the result
                    cmd.ExecuteNonQuery();

                }
                // close the database connection
                conn.Close();
            }
            else if (p_remise == 2)
            {
                query = "UPDATE POS_PERIODE SET REMISE2='Y' WHERE PERIODE=:p_periode_TOCLOSE";
                // create a new Oracle connection object
                using OracleConnection conn = new(global.connectionString);
                // open the database connection
                conn.Open();

                // create a new Oracle command object
                using (OracleCommand cmd = new(query, conn))
                {
                    // bind the PERIODE parameter with the provided value
                    cmd.Parameters.Add("p_periode_TOCLOSE", p_periode_TOCLOSE);

                    // execute the query and retrieve the result
                    cmd.ExecuteNonQuery();

                }
                // close the database connection
                conn.Close();
            }
            else
            {
                query = "UPDATE POS_PERIODE SET BULANAN='Y' WHERE PERIODE=:p_periode_TOCLOSE";
                // create a new Oracle connection object
                using OracleConnection conn = new(global.connectionString);
                // open the database connection
                conn.Open();

                // create a new Oracle command object
                using (OracleCommand cmd = new(query, conn))
                {
                    // bind the PERIODE parameter with the provided value
                    cmd.Parameters.Add("p_periode_TOCLOSE", p_periode_TOCLOSE);

                    // execute the query and retrieve the result
                    cmd.ExecuteNonQuery();

                }
                // close the database connection
                conn.Close();
            }
        }

        private void CreateNextPeriode(int next_periode)
        {
            // Extract the tahun and bulan from next_periode using substr
            string next_periode_str = next_periode.ToString();
            string tahun_str = next_periode_str.Substring(0, 4);
            string bulan_str = next_periode_str.Substring(4);

            int tahun = int.Parse(tahun_str);
            int bulan = int.Parse(bulan_str);
            string namabulan = arrayBulan[bulan - 1];
            string query = "INSERT INTO POS_PERIODE (PERIODE, TAHUN, BULAN, REMISE1, REMISE2) VALUES (:next_periode, :next_p_tahun, :next_p_namabulan, 'T', 'T')";

            // Check if the record already exists
            string checkQuery = "SELECT COUNT(1) FROM POS_PERIODE WHERE PERIODE = :next_periode";
            int count = 0;

            // create a new Oracle connection object
            using OracleConnection conn = new(global.connectionString);
            // open the database connection
            conn.Open();

            // Check if the record already exists
            using (OracleCommand checkCommand = new OracleCommand(checkQuery, conn))
            {
                checkCommand.Parameters.Add(new OracleParameter("next_periode", OracleDbType.Int32) { Value = next_periode });
                count = Convert.ToInt32(checkCommand.ExecuteScalar());
            }

            // Perform the insert only if the record does not exist
            if (count == 0)
            {
                // create a new OracleCommand object for the insert operation
                using (OracleCommand command = new OracleCommand(query, conn))
                {
                    command.Parameters.Add(new OracleParameter("next_periode", OracleDbType.Int32) { Value = next_periode });
                    command.Parameters.Add(new OracleParameter("next_p_tahun", OracleDbType.Int16) { Value = tahun });
                    command.Parameters.Add(new OracleParameter("next_p_namabulan", OracleDbType.Varchar2) { Value = namabulan });

                    command.ExecuteNonQuery();
                }
            }
            else
            {
                // Handle the case when the record already exists
                Console.WriteLine("The record already exists. Skipping the insert operation.");
            }
        }

        private void sbtutupbuku_Click(object sender, EventArgs e)
        {
            try
            {
                SplashScreenManager.ShowForm(typeof(WaitForm1), true, true);

                if (setahun.Value != 0)
                {
                    p_periode = Convert.ToInt32((setahun.Value + 1).ToString() + 1.ToString("00"));

                    bool isclosed = Tools_Services.GetRemiseStatus(p_periode, 3);
                    if (isclosed)
                    {
                        XtraMessageBox.Show("Periode tahun ini telah di Tutup ", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var startdate = new DateTime((int)setahun.Value, 1, 1);
                    var enddate = new DateTime((int)setahun.Value, 12, 31);
                    persediaan = controller.GetProductStockInfo(startdate, enddate).Where(saldoakhir=> saldoakhir.STOCK_AKHIR!=0).ToList();

                    if (persediaan.Any())
                    {
                        //XtraMessageBox.Show("Proses tutup tahun", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ProsesPindahanSaldo(persediaan, enddate.AddDays(1));
                        //jika diaktifkan saldo akhir dan saldo awal tidak nyambung
                        //update_hpp_nol(enddate.AddDays(1));
                    }
                    else
                    {
                        XtraMessageBox.Show("Periode tahun yang ditutup tidak memiliki transaksi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions specific to your application
                XtraMessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the wait form
                SplashScreenManager.CloseForm(false);
            }

        }

        private void update_hpp_nol(DateTime tanggal)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            using OracleTransaction transaction = connection.BeginTransaction();
            try
            {
                // Your SQL query using the MERGE statement with parameter
                string mergeQuery = @"
                    MERGE INTO POS_STOCK t
                    USING pos_product s
                    ON (t.kode_barang = s.kode_item)
                    WHEN MATCHED THEN
                        UPDATE SET t.hpp = s.beli
                        WHERE t.hpp = 0 AND t.tanggal = :p_tanggal";

                using (OracleCommand command = new OracleCommand(mergeQuery, connection))
                {
                    // Add parameters to the command
                    command.Parameters.Add(new OracleParameter("p_tanggal", tanggal));

                    command.Transaction = transaction;

                    // Execute the command
                    command.ExecuteNonQuery();
                }

                // Commit the transaction
                transaction.Commit();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                //Console.WriteLine($"Error: {ex.Message}");

                // Rollback the transaction in case of an exception
                transaction.Rollback();
            }
        }

        private void ProsesPindahanSaldo(List<DTOStockData> persediaan, DateTime tanggal)
        {
            try
            {
                using OracleConnection conn = new(global.connectionString);
                conn.Open();

                // Check if the record already exists
                int exist = ceksaldoawaltahunstock(tanggal);

                if (exist > 0)
                {
                    // Delete existing records for the specified date
                    string deleteQuery = "DELETE FROM POS_STOCK WHERE TANGGAL = :p_tanggal";

                    using (OracleCommand deleteCommand = new(deleteQuery, conn))
                    {
                        deleteCommand.Parameters.Add(new OracleParameter("p_tanggal", OracleDbType.Date) { Value = tanggal });
                        deleteCommand.ExecuteNonQuery();
                    }
                }

                // Insert new records from the list
                if (persediaan != null && persediaan.Count > 0)
                {
                    // Insert query
                    string insertQuery = "INSERT INTO POS_STOCK (PRODUCT_ID, KODE_BARANG, TANGGAL, QUANTITY, HPP) " +
                                         "VALUES (:ProductID, :KodeBarang, :Tanggal, :Quantity, :HPP)";

                    using OracleTransaction transaction = conn.BeginTransaction();
                    try
                    {
                        foreach (var stockData in persediaan)
                        {
                            using OracleCommand insertCommand = new(insertQuery, conn);
                            // Replace hardcoded ProductID with a dynamic value
                            insertCommand.Parameters.Add(new OracleParameter(":ProductID", 9999999));
                            insertCommand.Parameters.Add(new OracleParameter(":KodeBarang", stockData.KODE_ITEM));
                            insertCommand.Parameters.Add(new OracleParameter(":Tanggal", tanggal));
                            insertCommand.Parameters.Add(new OracleParameter(":Quantity", stockData.STOCK_AKHIR));
                            insertCommand.Parameters.Add(new OracleParameter(":HPP", stockData.TOTAL_COST_AVG ));

                            // Execute the query
                            insertCommand.ExecuteNonQuery();
                        }

                        // Commit the transaction if all operations succeed
                        transaction.Commit();

                        // Display message box for insertion success
                        XtraMessageBox.Show("Proses tutup tahun selesai!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of an error
                        transaction.Rollback();
                        Console.WriteLine($"Error: {ex.Message}");
                        // You may choose to rethrow the exception here for further handling
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log, display an error message, etc.)
                Console.WriteLine($"Error: {ex.Message}");
                // You may choose to rethrow the exception here for further handling
                throw;
            }
        }


        private int ceksaldoawaltahunstock(DateTime tanggal)
        {

            // Check if the record already exists
            string checkQuery = "select count(1) from POS_STOCK WHERE TANGGAL = :p_tanggal";
            int count = 0;

            // create a new Oracle connection object
            using OracleConnection conn = new(global.connectionString);
            // open the database connection
            conn.Open();

            // Check if the record already exists
            using (OracleCommand checkCommand = new(checkQuery, conn))
            {
                checkCommand.Parameters.Add(new OracleParameter("p_tanggal", OracleDbType.Date) { Value = tanggal });
                count = Convert.ToInt32(checkCommand.ExecuteScalar());
            }
            return count;
        }

        private void FrmClosingYear_Load(object sender, EventArgs e)
        {
            setahun.Value = DateTime.Now.Year;
        }
    }
}