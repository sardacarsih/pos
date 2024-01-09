
using BackOffice.DataLayer;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucUnitKerja : UserControl
    {
       
        string KODE,NAMA,SW_POT_SHU;
        //Using singleton pattern to create an instance to ucModule3
        private static ucUnitKerja _instance;
        public static ucUnitKerja Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucUnitKerja();
                return _instance;
            }
        }
        public ucUnitKerja()
        {
            InitializeComponent();
        }


        private static DataTable UNITKERJA()
        {
            string query = "SELECT KODE,NAMA,SW_POT_SHU FROM FIN_UNITKERJA";
            using OracleConnection connection = new(global.connectionString);
            using OracleCommand _command = new(query, connection)
            {
                CommandType = CommandType.Text
            };
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            OracleDataReader dr;
            dr = _command.ExecuteReader();
            DataTable _dt = new();
            _dt.Load(dr);
            dr.Close();
            connection.Close();
            return _dt;
        }

        public int InsertUNITKERJA(string kode,string unitkerja,string pot_shu)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"INSERT INTO FIN_UNITKERJA (KODE,NAMA,SW_POT_SHU) VALUES (:kode,:unitkerja,:potshu)";

            using OracleCommand command = new(mergeSql, connection);
            command.Parameters.Add("kode", OracleDbType.Varchar2).Value = kode;
            command.Parameters.Add("unitkerja", OracleDbType.Varchar2).Value = unitkerja;
            command.Parameters.Add("potshu", OracleDbType.Varchar2).Value = pot_shu;


            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected;
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string potshu = "T";
            if (checkEdit1.Checked == true) { potshu = "Y"; }
            if(string.IsNullOrEmpty(txtunitkerja.Text)) { return; }
            var kodemax = GetNextFormattedKode();

            int result = InsertUNITKERJA(kodemax,txtunitkerja.Text.ToUpper(), potshu);

            if (result>0)
            {
                MessageBox.Show("unit kerja inserted .");
                Load_UNITKERJA();
            }
            else
            {
                MessageBox.Show("Failed to insert unit kerja.");
            }
            txtkode.Text = string.Empty;
            txtunitkerja.Text = string.Empty;
            checkEdit1.Checked = false;

        }

        static string GetNextFormattedKode()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = "SELECT MAX(TO_NUMBER(KODE)) FROM FIN_UNITKERJA";

            using OracleCommand command = new(query, connection);
            object result = command.ExecuteScalar();

            if (result != DBNull.Value)
            {
                int maxKode = Convert.ToInt32(result);
                int nextKode = maxKode + 1;
                string formattedNextKode = nextKode.ToString("D2"); // Format as two-digit string

                return formattedNextKode;
            }
            else
            {
                return "01"; // Default value if no data found
            }
        }

        private void Load_UNITKERJA()
        {
            var UK = UNITKERJA();
            gridControl1.DataSource = UK;
            gridView1.BestFitColumns();
        }

        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
            if (gridView1.SelectedRowsCount > 0)
            {
                DataRow row = gridView1.GetDataRow(gridView1.FocusedRowHandle);
                if (row != null)
                {
                    KODE = row["KODE"].ToString();
                    NAMA = row["NAMA"].ToString();
                    SW_POT_SHU = row["SW_POT_SHU"].ToString();
                }
            }
            txtkode.Text = KODE;
            txtunitkerja.Text = NAMA;
            if (SW_POT_SHU == "Y")
            {
                checkEdit1.Checked = true;
            }
            else
            {
                checkEdit1.Checked = false;
            }
            barLargeButtonItem1.Enabled = false; 
            barLargeButtonItem2.Enabled=true;
            barLargeButtonItem3.Enabled = true;
        }

        private void barLargeButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var pot_shu = "T";
            if (checkEdit1.Checked == true)
            {
                pot_shu = "Y";
            }
            if (string.IsNullOrEmpty(txtunitkerja.Text)) { return; }
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"UPDATE FIN_UNITKERJA SET NAMA=:nama,SW_POT_SHU=:pot_shu WHERE KODE=:kode";

            using OracleCommand command = new(mergeSql, connection);
            command.Parameters.Add("nama", OracleDbType.Varchar2).Value = txtunitkerja.Text.ToUpper();
            command.Parameters.Add("pot_shu", OracleDbType.Varchar2).Value = pot_shu;
            command.Parameters.Add("kode", OracleDbType.Varchar2).Value = KODE;

            int rowsAffected = command.ExecuteNonQuery();
            Load_UNITKERJA();
            txtkode.Text = string.Empty;
            txtunitkerja.Text = string.Empty;
            
            checkEdit1.Checked = false;
            barLargeButtonItem1.Enabled = true;
            barLargeButtonItem2.Enabled = false;
            barLargeButtonItem3.Enabled = false;
        }

        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(txtunitkerja.Text)) { return; }
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"delete FIN_UNITKERJA WHERE KODE=:kode";

            using OracleCommand command = new(mergeSql, connection);          
            command.Parameters.Add("kode", OracleDbType.Varchar2).Value = KODE;

            int rowsAffected = command.ExecuteNonQuery();
            Load_UNITKERJA();
            txtkode.Text= string.Empty;
            txtunitkerja.Text = string.Empty;
            checkEdit1.Checked = false;
            barLargeButtonItem1.Enabled = true;
            barLargeButtonItem2.Enabled = false;
            barLargeButtonItem3.Enabled = false;
        }

        private void ucUnitKerja_Load(object sender, EventArgs e)
        {
            Load_UNITKERJA();
        }
    }
}
