
using BackOffice.DataLayer;
using DevExpress.Utils.About;
using DevExpress.XtraCharts.Designer.Native;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackOffice.UC
{
    public partial class ucSatuan : UserControl
    {
        int ID;
        string satuan;
        //Using singleton pattern to create an instance to ucModule3
        private static ucSatuan _instance;
        public static ucSatuan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucSatuan();
                return _instance;
            }
        }
        public ucSatuan()
        {
            InitializeComponent();
        }


        private static DataTable Satuan()
        {
            string query = "SELECT * FROM POS_SATUAN";
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

        public int InsertSatuan(string satuanValue)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"INSERT INTO POS_SATUAN (SATUAN) VALUES (:satuan)";

            using OracleCommand command = new(mergeSql, connection);
            command.Parameters.Add("satuan", OracleDbType.Varchar2).Value = satuanValue;

            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected;
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if(string.IsNullOrEmpty(TXTSATUAN.Text)) { return; }
            int result = InsertSatuan(TXTSATUAN.Text.ToUpper());

            if (result>0)
            {
                MessageBox.Show("Satuan inserted or Update successfully.");
                Load_Satuan();
            }
            else
            {
                MessageBox.Show("Failed to insert satuan.");
            }
            TXTSATUAN.Text = string.Empty;
        }

        private void ucSatuan_Load(object sender, EventArgs e)
        {
            Load_Satuan();
        }

        private void Load_Satuan()
        {
            var sat = Satuan();
            gridControl1.DataSource = sat;
            gridView1.Columns["ID"].Visible = false;
            gridView1.BestFitColumns();
        }

        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
            if (gridView1.SelectedRowsCount > 0)
            {
                DataRow row = gridView1.GetDataRow(gridView1.FocusedRowHandle);
                if (row != null)
                {
                    ID = Convert.ToInt32(row["ID"]);
                    satuan = row["SATUAN"].ToString();
                }
            }
            TXTSATUAN.Text = satuan;
            barLargeButtonItem1.Enabled = false; 
            barLargeButtonItem2.Enabled=true;
            barLargeButtonItem3.Enabled = true;
        }

        private void barLargeButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(TXTSATUAN.Text)) { return; }
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"UPDATE POS_SATUAN SET SATUAN=:satuan WHERE ID=:id";

            using OracleCommand command = new(mergeSql, connection);
            command.Parameters.Add("satuan", OracleDbType.Varchar2).Value = TXTSATUAN.Text.ToUpper();
            command.Parameters.Add("id", OracleDbType.Int32).Value = ID;

            int rowsAffected = command.ExecuteNonQuery();
            Load_Satuan();
            TXTSATUAN.Text = string.Empty;
            barLargeButtonItem1.Enabled = true;
            barLargeButtonItem2.Enabled = false;
            barLargeButtonItem3.Enabled = false;
        }

        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(TXTSATUAN.Text)) { return; }
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeSql = @"delete POS_SATUAN WHERE ID=:id";

            using OracleCommand command = new(mergeSql, connection);          
            command.Parameters.Add("id", OracleDbType.Int32).Value = ID;

            int rowsAffected = command.ExecuteNonQuery();
            Load_Satuan();
            TXTSATUAN.Text = string.Empty;
            barLargeButtonItem1.Enabled = true;
            barLargeButtonItem2.Enabled = false;
            barLargeButtonItem3.Enabled = false;
        }
    }
}
