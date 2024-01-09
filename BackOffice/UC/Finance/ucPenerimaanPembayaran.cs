using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucPenerimaanPembayaran : UserControl
    {
        MasterDataController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucPenerimaanPembayaran _instance;

        public static List<DTOPenerimaanPembayaran> pembayaranList = new();

        string[] bulan = PeriodeServices.GetBulan();
        string[] remise = PeriodeServices.GetRemise();

        public static ucPenerimaanPembayaran Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPenerimaanPembayaran();
                return _instance;
            }
        }
        public ucPenerimaanPembayaran()
        {
            InitializeComponent();
            SetSidePanelMaximumHeight();
        }

        private void SetSidePanelMaximumHeight()
        {
            // Get the screen width and height
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Set the width and maximum height of SidePanel2 equal to the screen width and height
            // Calculate the maximum height based on the screen resolution
            int maxWidth = (int)(screenWidth * 1);
            int maxHeight = (int)(screenHeight * 1); // Adjust the factor as per your requirement

            //sidePanel2.Width = screenWidth;
            sidePanel2.MaximumSize = new Size(maxWidth, maxHeight);
        }

        private void ucPenerimaanPembayaran_Load(object sender, EventArgs e)
        {
            load_unitkerja_byTransaksi();

            int currentMonthIndex = DateTime.Now.Month - 1;
           comboBoxEdit_bulan.Properties.Items.AddRange(bulan);
            comboBoxEdit_bulan.SelectedIndex= currentMonthIndex;

            spinEdit_tahun.Value = DateTime.Now.Year;
            spinEdit_tahun.Properties.MaxValue = DateTime.Now.Year;

            comboBoxEdit_remise.Properties.Items.AddRange(remise);

            int currentremiseIndex = 0;
            if (DateTime.Today.Day > 15)
            {
                currentremiseIndex = 1;
            }
            comboBoxEdit_remise.SelectedIndex= currentremiseIndex;
           
        }



        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            
            if (e.Column.FieldName == "BAYAR")
            {
                    DTOPenerimaanPembayaran item = (DTOPenerimaanPembayaran)gridView1.GetRow(e.RowHandle);
                    decimal tagihan = item.TAGIHAN;
                    decimal bayar = Convert.ToDecimal(e.Value);
                    decimal sisa = tagihan - bayar;

                    gridView1.SetRowCellValue(e.RowHandle, "SISA", sisa);

                    item.BAYAR = bayar;
                    item.SISA = sisa;

                    comboBoxEdit_bulan.Enabled = false;
                    comboBoxEdit_remise.Enabled = false;
                    spinEdit_tahun.Enabled = false;
                    searchLookUpEdit_unitkerja.Enabled = false;

                    sbsimpan.Enabled = true;
                    sbbatal.Enabled = true;
            }
        }

        private void Load_Penerimaan_Pembayaran()
        {
           
            if( comboBoxEdit_bulan.EditValue!= null && spinEdit_tahun.EditValue!=null && comboBoxEdit_remise.EditValue != null && searchLookUpEdit_unitkerja.EditValue!=null)
            {
               
                int p_bulan =  comboBoxEdit_bulan.SelectedIndex+1;
                int p_remise = comboBoxEdit_remise.SelectedIndex+1;
                var p_tahun = Convert.ToInt16(spinEdit_tahun.EditValue);
                var p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));
                string ukerja   = searchLookUpEdit_unitkerja.EditValue.ToString();
              
              
                    pembayaranList = Load_Pembayaran(p_periode, p_remise, ukerja);
                    if(pembayaranList.Count> 0 ) 
                    {
                        gridControl1.DataSource = pembayaranList;
                        gridView1.RefreshData(); // Refresh the grid view's data source
                                                 // Assuming you have defined the grid columns for "tagihan," "bayar," and "sisa" in gridView1

                    // Hide the "Periode" column
                    gridView1.Columns["PERIODE"].Visible = false;

                    // Hide the "Remise" column
                    gridView1.Columns["REMISE"].Visible = false;

                    // Hide the "Unit Kerja" column
                    gridView1.Columns["UNIT_KERJA"].Visible = false;

                    // Hide the "Is Modified" column
                    gridView1.Columns["IsModified"].Visible = false;

                    // Specify the display format for the "tagihan" column
                    gridView1.Columns["TAGIHAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["TAGIHAN"].DisplayFormat.FormatString = "N2";
                    gridView1.Columns["TAGIHAN"].Summary.Clear();
                    gridView1.Columns["TAGIHAN"].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "TAGIHAN", "{0:N2}");
                    // Specify the display format for the "bayar" column
                    gridView1.Columns["BAYAR"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["BAYAR"].DisplayFormat.FormatString = "N2";
                    gridView1.Columns["BAYAR"].Summary.Clear();
                    gridView1.Columns["BAYAR"].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "BAYAR", "{0:N2}");

                    // Specify the display format for the "sisa" column
                    gridView1.Columns["SISA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["SISA"].DisplayFormat.FormatString = "N2";
                    gridView1.Columns["SISA"].Summary.Clear();
                    gridView1.Columns["SISA"].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "SISA", "{0:N2}");

                    // Disable editing for specific columns
                    gridView1.Columns["NIK"].OptionsColumn.AllowEdit = false;
                    gridView1.Columns["NAMA_PELANGGAN"].OptionsColumn.AllowEdit = false;
                    gridView1.Columns["TAGIHAN"].OptionsColumn.AllowEdit = false;
                    gridView1.Columns["SISA"].OptionsColumn.AllowEdit = false;

                    // Sort ascending by "NAMA_PELANGGAN" column
                    gridView1.ClearSorting(); // Clear any existing sorting
                    gridView1.Columns["NAMA_PELANGGAN"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;


                }
                else
                    {
                        gridControl1.DataSource = null;
                    XtraMessageBox.Show("Tidak terdapat transaksi pembayaran", "Info");
                    }

            }

            sbsimpan.Enabled= false;
            sbbatal.Enabled= false;

        }
        private static bool IsModified(List<DTOPenerimaanPembayaran> pembayaranList)
        {
            return pembayaranList.Any(item => item.IsModified);
        }

        private static void DiscardChanges(List<DTOPenerimaanPembayaran> pembayaranList)
        {
            foreach (DTOPenerimaanPembayaran item in pembayaranList)
            {
                item.IsModified = false;
            }
        }

        static List<DTOPenerimaanPembayaran> Load_Pembayaran(int p_periode, int p_remise, string p_unitkerja)
        {
            pembayaranList.Clear();
            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                string query = "SELECT b.*, a.nama_pelanggan, a.unit_kerja " +
                               "FROM fin_terima_pembayaran b " +
                               "JOIN fin_anggota a ON a.nik = b.nik " +
                               "WHERE b.periode = :p AND b.remise = :r AND a.UNIT_KERJA = :u";

                using (OracleCommand command = new OracleCommand(query, connection))
                {
                    command.Parameters.Add(new OracleParameter("p", p_periode));
                    command.Parameters.Add(new OracleParameter("r", p_remise));
                    command.Parameters.Add(new OracleParameter("u", p_unitkerja));

                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DTOPenerimaanPembayaran item = new DTOPenerimaanPembayaran()
                            {
                                PERIODE = reader.GetInt32(reader.GetOrdinal("periode")),
                                REMISE = reader.GetInt32(reader.GetOrdinal("remise")),
                                NIK = reader.GetString(reader.GetOrdinal("nik")),
                                NAMA_PELANGGAN = reader.GetString(reader.GetOrdinal("nama_pelanggan")),
                                UNIT_KERJA = reader.GetString(reader.GetOrdinal("unit_kerja")),
                                TAGIHAN = reader.GetDecimal(reader.GetOrdinal("tagihan")),
                                BAYAR = reader.GetDecimal(reader.GetOrdinal("bayar")),
                                SISA = reader.GetDecimal(reader.GetOrdinal("sisa"))
                            };

                            pembayaranList.Add(item);
                        }
                    }
                }

                connection.Close();
            }

            // Reset IsModified flag after loading new data
            foreach (DTOPenerimaanPembayaran item in pembayaranList)
            {
                item.IsModified = false;
            }

            return pembayaranList; // Return the populated pembayaranList
        }



        private void BLBICETAK_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
           var periode= "PERIODE : "+comboBoxEdit_bulan.Text+" " + spinEdit_tahun.Value.ToString() + " ( "+comboBoxEdit_remise.Text + " )";

            //_ds.WriteXmlSchema("Tagihan_Penjualan.xsd");

          
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptRekapbyUnitKerja();
                XtraReport report2 = new rptTagihan_Penjualan();

                // Set the data sources and parameters for each report
                report1.DataSource = pembayaranList;
                report1.RequestParameters = true;
                report1.Parameters["PERIODE"].Value = periode;
                report1.CreateDocument();

                // Set other properties for report1 as needed

                report2.DataSource = pembayaranList;
                report2.RequestParameters = true;
                report2.Parameters["PERIODE"].Value = periode;
                report2.CreateDocument();
                // Set other properties for report2 as needed

                report1.Pages.AddRange(report2.Pages);
                report1.PrintingSystem.ContinuousPageNumbering = true;

                ReportPrintTool tool = new ReportPrintTool(report1);
                tool.ShowPreview();
        }

        private void comboBoxEdit_bulan_SelectedIndexChanged(object sender, EventArgs e)
        {
            load_unitkerja_byTransaksi();
            Load_Penerimaan_Pembayaran();
        }

        private void load_unitkerja_byTransaksi()
        {
            if (comboBoxEdit_bulan.EditValue != null && spinEdit_tahun.EditValue != null && comboBoxEdit_remise.EditValue != null)
            {

                int p_bulan = comboBoxEdit_bulan.SelectedIndex + 1;
                int p_remise = comboBoxEdit_remise.SelectedIndex + 1;
                var p_tahun = Convert.ToInt16(spinEdit_tahun.EditValue);
                var p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));

                var unitkerja = controller.GetUnitKerjaFromTransaksi(p_periode, p_remise);
                searchLookUpEdit_unitkerja.Properties.DataSource = unitkerja;
                searchLookUpEdit_unitkerja.Properties.ValueMember = "KODE";
                searchLookUpEdit_unitkerja.Properties.DisplayMember = "NAMA";
            }
        }


        private void spinEdit_tahun_EditValueChanged(object sender, EventArgs e)
        {
            load_unitkerja_byTransaksi();
            Load_Penerimaan_Pembayaran();
        }

        private void comboBoxEdit_remise_SelectedIndexChanged(object sender, EventArgs e)
        {
            load_unitkerja_byTransaksi();
            Load_Penerimaan_Pembayaran();
        }

        private void searchLookUpEdit_unitkerja_EditValueChanged(object sender, EventArgs e)
        {
          

            Load_Penerimaan_Pembayaran();
        }

        private void sbsimpan_Click(object sender, EventArgs e)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string updateQuery = "UPDATE fin_terima_pembayaran SET bayar = :bayar, sisa = :sisa WHERE periode = :periode AND remise = :remise AND nik = :nik";

            foreach (DTOPenerimaanPembayaran item in pembayaranList)
            {
                if (item.IsModified)
                {
                    using (OracleCommand command = new OracleCommand(updateQuery, connection))
                    {
                        command.Parameters.Add("bayar", OracleDbType.Decimal).Value = item.BAYAR;
                        command.Parameters.Add("sisa", OracleDbType.Decimal).Value = item.SISA;
                        command.Parameters.Add("periode", OracleDbType.Int32).Value = item.PERIODE;
                        command.Parameters.Add("remise", OracleDbType.Int32).Value = item.REMISE;
                        command.Parameters.Add("nik", OracleDbType.Varchar2).Value = item.NIK;

                        command.ExecuteNonQuery();
                    }
                    // Reset the IsModified flag after saving the changes
                    item.IsModified = false;
                }

            }

            connection.Close();
            comboBoxEdit_bulan.Enabled = true;
            comboBoxEdit_remise.Enabled = true;
            spinEdit_tahun.Enabled = true;
            searchLookUpEdit_unitkerja.Enabled = true;
            sbsimpan.Enabled = false;
            sbbatal.Enabled = false;
            XtraMessageBox.Show("Data berhasil disimpan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void sbbatal_Click(object sender, EventArgs e)
        {
            if (pembayaranList.Any(item => item.IsModified))
            {
                DialogResult result = XtraMessageBox.Show("Perubahan Data Dibatalkan. Anda yakin akan membatalkan perubahan data?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    return ; // Return the existing pembayaranList without reloading the data
                }
                else
                {
                    Load_Penerimaan_Pembayaran();

                    comboBoxEdit_bulan.Enabled = true;
                    comboBoxEdit_remise.Enabled = true;
                    spinEdit_tahun.Enabled = true;
                    searchLookUpEdit_unitkerja.Enabled = true; 
                }
            }
           
        }

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            int tahun = (int)spinEdit_tahun.Value;
            int bulan = comboBoxEdit_bulan.SelectedIndex + 1;
            int p_periode = Convert.ToInt32(tahun.ToString() + bulan.ToString("00"));
            int p_remise = comboBoxEdit_remise.SelectedIndex + 1;
            bool isclosed = Tools_Services.GetRemiseStatus(p_periode, p_remise);

            if (isclosed)
            {
                XtraMessageBox.Show("Data Pembayaran tidak dapat diubah, Periode telah di Tutup ", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Valid = false; // Set Valid property to false to prevent value change
                gridView1.CancelUpdateCurrentRow(); // Cancel the row update
                return;
            }
            else
            {
                decimal bayarValue;

                if (!decimal.TryParse(e.Value.ToString(), out bayarValue))
                {
                    e.Valid = false;
                    e.ErrorText = "input hanya angka.";
                    return;
                }

                bayarValue = Convert.ToDecimal(e.Value);
                decimal tagihanValue = Convert.ToDecimal(gridView1.GetRowCellValue(gridView1.FocusedRowHandle, "TAGIHAN"));

                if (bayarValue < 0)
                {
                    e.Valid = false;
                    e.ErrorText = "NILAI BAYAR TIDAK BOLEH NEGATIVE.";
                }
                else if (bayarValue > tagihanValue)
                {
                    e.Valid = false;
                    e.ErrorText = "NILAI BAYAR TIDAK BOLEH LEBIH BESAR DARI TAGIHAN.";
                }
            }
        }

    }
}
