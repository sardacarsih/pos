
using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using DevExpress.CodeParser;
using DevExpress.Xpo.DB.Helpers;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
using System.Media;

namespace BackOffice
{
    public partial class FrmClosing : DevExpress.XtraEditors.XtraForm
    {
        TutupBukuController controller = new();
        public FrmClosing()
        {
            InitializeComponent();
           
        }
        DateTime p_daritanggal, p_daritanggalr2, p_sampaitanggal, p_tglAngsuran;
        readonly string[] arrayBulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "Nopember", "Desember" };
        int p_bulan, p_tahun,p_remise,p_periode, p_tahun_TOCLOSE, p_periode_TOCLOSE, p_remise_TOCLOSE, p_bulan_TOCLOSE, next_periode;

        private void sbtutupbuku_Click(object sender, EventArgs e)
        {
            int remise = comboBoxEdit_Remise.SelectedIndex + 1;
            bool isclosed = Tools_Services.GetRemiseStatus(p_periode, remise);
            if (isclosed)
            {
                XtraMessageBox.Show("Periode telah di Tutup ", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            p_daritanggal = Convert.ToDateTime(dedaritgl.Text);
                p_sampaitanggal= Convert.ToDateTime(desampaitgl.Text);

            if (MIN_R1DARI.Year != 0000 || MIN_R2DARI.Year != 0000|| MIN_BDARI.Year != 0000) // menghandle nilai minimal belum ada
            {

                if (p_sampaitanggal <= p_daritanggal)
                {
                    XtraMessageBox.Show("Cek Pilihan Tanggal ?", "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                    return;
                }
                if (remise == 1)
                {
                    if (p_daritanggal <= LAST_R2SAMPAI)
                    {
                        XtraMessageBox.Show("Untuk mencegah tagihan Remise 1 double,Tanggal Awal tidak boleh <= tanggal tutup buku periode sebelumnya..!!!\n" + LAST_R2SAMPAI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (MIN_R1DARI.Year != 0001) // menghandle nilai minimal belum ada
                    {
                        if (p_daritanggal != MIN_R1DARI)
                        {
                            XtraMessageBox.Show("Tanggal tutup buku Remise 1 harus dumulai dari tanggal..!!!\n" + MIN_R1DARI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    
                }
                else if (remise == 2)
                {
                    if (p_daritanggal <= LAST_R1SAMPAI)
                    {
                        XtraMessageBox.Show("Untuk mencegah tagihan Remise 2 double,Tanggal Awal tidak boleh <= tanggal tutup buku periode sebelumnya..!!!\n" + LAST_R1SAMPAI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (p_daritanggal != MIN_R2DARI)
                    {
                        XtraMessageBox.Show("Tanggal tutup buku Remise 2 harus dumulai dari tanggal..!!!\n" + MIN_R2DARI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    if (p_daritanggal <= LAST_BSAMPAI)
                    {
                        XtraMessageBox.Show("Untuk mencegah tagihan Bulanan double,Tanggal Awal tidak boleh <= tanggal tutup buku periode sebelumnya..!!!\n" + LAST_BSAMPAI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (MIN_R1DARI.Year != 0001) // menghandle nilai minimal belum ada
                    {
                        if (p_daritanggal != MIN_BDARI)
                        {
                            XtraMessageBox.Show("Tanggal tutup buku Bulanan harus dumulai dari tanggal..!!!\n" + MIN_BDARI.ToString("dd-MMM-yyyy"), "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                        

                }
            }
           

            using var handle = SplashScreenManager.ShowOverlayForm(this);
            try
            {
                Stopwatch watch = new();
                watch.Start();
                POS_Services.Tutup_Buku(p_periode, p_remise, p_tglAngsuran, p_daritanggal, p_sampaitanggal);
                CreateThisPeriode(p_periode);
                CreateNextPeriode(next_periode);
                CreateStatusClosing(p_remise, p_periode_TOCLOSE);
                watch.Stop();
                TimeSpan timeSpan = watch.Elapsed;
                string waktuproses = string.Format("Waktu Proses : {0}h {1}m {2}s {3}ms", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);

                XtraMessageBox.Show("Proses Tutup buku  telah Selesai " + waktuproses, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OracleException ex)
            {
                if (ex.Number == 20001)
                {
                    XtraMessageBox.Show("Proses tidak dapat dilanjutkan, Data Pembayaran telah dilakukan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (ex.Message.Contains("FIN_TAGIHAN_WASERDA_PK"))
                {
                    var dupnik = controller.GetDuplicateNiks(p_periode, p_remise, p_daritanggal, p_daritanggalr2, p_sampaitanggal);
                    XtraMessageBox.Show("Proses tidak dapat dilanjutkan, Double NIK Tagihan \n"+ string.Join(Environment.NewLine, dupnik.ToList()) +"\n,Perbaiki Transaksi Penjualan ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void cmbbulan_SelectedIndexChanged(object sender, EventArgs e)
        {
            Update_Periode();

        }
        private void comboBoxEdit_Remise_SelectedIndexChanged(object sender, EventArgs e)
        {
           
            Update_Periode();
           
        }
        DateTime  LAST_R1SAMPAI,  LAST_R2SAMPAI,  LAST_BSAMPAI, MIN_R1DARI, MIN_R2DARI, MIN_BDARI;
        private void LoadL_lastDate_closing(int p_bulan,int p_tahun)
        {
            int tahun,bulan,lastperide,sameperiode;
            sameperiode = int.Parse(p_tahun.ToString() + p_bulan.ToString("00"));
            if (p_bulan==1)
            {
                tahun = p_tahun - 1;
                bulan = 12;
                lastperide=int.Parse(tahun.ToString() + bulan.ToString("00"));
                

            }
            else
            {
                tahun = p_tahun;
                bulan = p_bulan-1;
                lastperide = int.Parse(tahun.ToString() + bulan.ToString("00"));
            }
            if(p_remise==1 || p_remise == 3)
            {
                string selectQuery = "SELECT   R2SAMPAI,  BSAMPAI FROM POS_PERIODE WHERE PERIODE = :p_periode ORDER BY periode";

                using (OracleConnection connection = new OracleConnection(global.connectionString))
                {
                    connection.Open();

                    using (OracleCommand _command = new OracleCommand(selectQuery, connection))
                    {
                        _command.Parameters.Add(":p_periode", OracleDbType.Int32).Value = lastperide;

                        using (OracleDataReader reader = _command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (DateTime.TryParse(reader["R2SAMPAI"].ToString(), out DateTime sampaiValueR2) &&
                                    DateTime.TryParse(reader["BSAMPAI"].ToString(), out DateTime sampaiValueB))
                                {
                                    // Access data using reader.GetValue(index) or reader["columnName"]                                  
                                    LAST_R2SAMPAI = sampaiValueR2;
                                    LAST_BSAMPAI = sampaiValueB;
                                }

                            }
                        }
                    }
                }
                MIN_R1DARI = LAST_R2SAMPAI.AddDays(1);
                MIN_BDARI= LAST_BSAMPAI.AddDays(1);
            }
            else if(p_remise==2)
            {
                string selectQuery = "SELECT  R1SAMPAI FROM POS_PERIODE WHERE PERIODE = :p_periode ORDER BY periode";

                using (OracleConnection connection = new OracleConnection(global.connectionString))
                {
                    connection.Open();

                    using (OracleCommand _command = new OracleCommand(selectQuery, connection))
                    {
                        _command.Parameters.Add(":p_periode", OracleDbType.Int32).Value = sameperiode;

                        using (OracleDataReader reader = _command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (DateTime.TryParse(reader["R1SAMPAI"].ToString(), out DateTime sampaiValueR1) )
                                {
                                    // Access data using reader.GetValue(index) or reader["columnName"]
                                    LAST_R1SAMPAI = sampaiValueR1;
                                }

                            }
                        }
                    }
                }
                MIN_R2DARI= LAST_R1SAMPAI.AddDays(1);    

            }                

        }

        private void setahun_EditValueChanged(object sender, EventArgs e)
        {
            Update_Periode();
        }

        private void Update_Periode()
        {
            if (cmbbulan.EditValue != null && comboBoxEdit_Remise.EditValue != null && setahun.Value != 0)
            {
                // Check if cmbbulan, comboBoxEdit_Remise, and setahun have non-null values

                // Convert setahun.Value to an integer and assign it to p_tahun
                p_tahun = Convert.ToInt32(setahun.Value);

                // Get the selected index of cmbbulan (assumed as a combo box) and add 1 to get p_bulan
                p_bulan = cmbbulan.SelectedIndex + 1;

                

                // Combine p_tahun and p_bulan in "yyyymm" format, convert it to an integer to get p_periode
                p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));

                // Get the selected index of comboBoxEdit_Remise (assumed as a combo box) and add 1 to get p_remise
                p_remise = comboBoxEdit_Remise.SelectedIndex + 1;

                // Get the last day of the selected month and year using the DateTime.DaysInMonth method
                var lastDayOfMonth = DateTime.DaysInMonth(p_tahun, p_bulan);

                if (p_remise == 1)
                {
                    // Set p_daritanggal to the first day of the selected month and year
                    p_daritanggal = new DateTime(p_tahun, p_bulan, 1);

                    // Set p_sampaitanggal to the 15th day of the selected month and year
                    p_sampaitanggal = new DateTime(p_tahun, p_bulan, 15);
                }
                else if(p_remise == 2) 
                {
                    // Set p_daritanggal to the first day of the selected month and year
                    p_daritanggal = new DateTime(p_tahun, p_bulan, 16);

                  
                    // Set p_sampaitanggal to the last day of the selected month and year
                    p_sampaitanggal = new DateTime(p_tahun, p_bulan, lastDayOfMonth);
                }
                else
                {
                    // Set p_daritanggal to the first day of the selected month and year
                    p_daritanggal = new DateTime(p_tahun, p_bulan, 1);


                    // Set p_sampaitanggal to the last day of the selected month and year
                    p_sampaitanggal = new DateTime(p_tahun, p_bulan, lastDayOfMonth);

                    // Set p_tglAngsuran to 1 day after p_sampaitanggal
                    p_tglAngsuran = p_sampaitanggal;

                }

                dedaritgl.Text= p_daritanggal.ToString("dd-MMM-yyyy");
                desampaitgl.Text= p_sampaitanggal.ToString("dd-MMM-yyyy");

                if (p_bulan == 1)
                {
                    // Calculate new variables based on the previous year if the selected month is January

                    // Subtract 1 from p_tahun to get the previous year
                    p_tahun_TOCLOSE = p_tahun - 1;

                    // Set p_bulan_TOCLOSE to 12 (December)
                    p_bulan_TOCLOSE = 12;

                    // Combine p_tahun_TOCLOSE and p_bulan_TOCLOSE in "yyyymm" format to get p_periode_TOCLOSE
                    p_periode_TOCLOSE = Convert.ToInt32(p_tahun_TOCLOSE.ToString() + p_bulan_TOCLOSE.ToString("00"));

                    // Assign p_remise to p_remise_TOCLOSE
                    p_remise_TOCLOSE = p_remise;
                }
                else
                {
                    // Calculate new variables based on the current year and the previous month if the selected month is not January

                    // Set p_tahun_TOCLOSE to p_tahun
                    p_tahun_TOCLOSE = p_tahun;

                    // Subtract 1 from p_bulan to get the previous month
                    p_bulan_TOCLOSE = p_bulan - 1;

                    // Combine p_tahun_TOCLOSE and p_bulan_TOCLOSE in "yyyymm" format to get p_periode_TOCLOSE
                    p_periode_TOCLOSE = Convert.ToInt32(p_tahun_TOCLOSE.ToString() + p_bulan_TOCLOSE.ToString("00"));

                    // Assign p_remise to p_remise_TOCLOSE
                    p_remise_TOCLOSE = p_remise;
                }

                // Calculate the next period based on the selected month and year
                if (p_bulan == 12)
                {
                    // If the selected month is December, set the next period to January of the next year
                    next_periode = Convert.ToInt32((p_tahun + 1).ToString() + "01");
                }
                else
                {
                    // Otherwise, set the next period to the next month of the same year
                    next_periode = Convert.ToInt32(p_tahun.ToString() + (p_bulan + 1).ToString("00"));
                }

                if (p_bulan != 0 && p_tahun != 0)
                {
                    LoadL_lastDate_closing(p_bulan, p_tahun);
                }
            }
        }







        private void FrmClosing_Load(object sender, EventArgs e)
        {
            

            cmbbulan.Properties.Items.AddRange(arrayBulan);

            comboBoxEdit_Remise.Properties.Items.AddRange(new []{ "Remise I", "Remise II", "Bulanan" });

            cmbbulan.SelectedIndex = DateTime.Today.Month- 1;
            comboBoxEdit_Remise.SelectedIndex = 0;
            setahun.Value = DateTime.Today.Year;
            Update_Periode();
        }

       

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
                command.Parameters.Add(new OracleParameter("1", OracleDbType.Varchar2) { Value = p_daritanggal.ToString("dd-MMM-yyyy")});
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
            string namabulan = arrayBulan[bulan-1];
            string query = "INSERT INTO POS_PERIODE (PERIODE, TAHUN, BULAN, REMISE1, REMISE2) VALUES (:next_periode, :next_p_tahun, :next_p_namabulan, 'T', 'T')";

            // Check if the record already exists
            string checkQuery = "SELECT COUNT(*) FROM POS_PERIODE WHERE PERIODE = :next_periode";
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

    }
}