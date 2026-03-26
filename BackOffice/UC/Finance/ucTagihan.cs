using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Laporan;
using BackOffice.View;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using OfficeOpenXml.Table;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using DevExpress.XtraPrinting.Native;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace BackOffice.UC
{
    public partial class ucTagihan : UserControl
    {
        DataSet _ds, _ds2;
        string JenisLaporan;
        int p_tahun, p_bulan, p_remise, p_periode;

        string[] bulan = PeriodeServices.GetBulan();
        string[] remise = PeriodeServices.GetRemise();

        //Using singleton pattern to create an instance to ucModule3
        private static ucTagihan _instance;
        public static ucTagihan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucTagihan();
                return _instance;
            }
        }
        public ucTagihan()
        {
            InitializeComponent();
        }


        private void ucTagihanPenjualan_Load(object sender, EventArgs e)
        {
            Load_Bulan();
        }
        private void Load_Bulan()
        {

            int currentMonthIndex = DateTime.Now.Month - 1;
            ((RepositoryItemComboBox)bei_bulan.Edit).Items.AddRange(bulan);
            bei_bulan.EditValue = bulan[currentMonthIndex];


            bei_tahun.EditValue = DateTime.Now.Year;



            int currentremiseIndex = 0;
            if (DateTime.Today.Day > 15)
            {
                currentremiseIndex = 1;
            }
            ((RepositoryItemComboBox)bei_remise.Edit).Items.AddRange(remise);
            bei_remise.EditValue = remise[currentremiseIndex];
        }

        private void bei_tahun_EditValueChanged(object? sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }

        private void bei_bulan_EditValueChanged(object sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }

        private void bei_remise_EditValueChanged(object sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }
        DateTime p_daritanggal, p_daritanggalr2, p_sampaitanggal, p_tglAngsuran;

        private void repositoryItemRadioGroup1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            RadioGroup radioGroup = sender as RadioGroup;
            JenisLaporan = radioGroup.EditValue.ToString();
            //XtraMessageBox.Show(JenisLaporan);
        }


        private void Load_Tagihan_Perioe()
        {
            try
            {
                if (bei_bulan.EditValue != null && bei_tahun.EditValue != null && bei_remise.EditValue != null)
                {

                    p_bulan = Array.IndexOf(bulan, bei_bulan.EditValue.ToString()) + 1;
                    p_remise = Array.IndexOf(remise, bei_remise.EditValue.ToString()) + 1;
                    p_tahun = Convert.ToInt16(bei_tahun.EditValue.ToString());
                    p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));
                    periode = "PERIODE : " + bei_bulan.EditValue.ToString() + " " + bei_tahun.EditValue.ToString() + " ( " + bei_remise.EditValue.ToString() + " )";

                    LoadParameterTanggal(p_periode, p_remise);

                    _ds = POS_Services.Tagihan_ALL(p_periode, p_remise);
                    //_ds.WriteXmlSchema("Tagihan_Penjualan.xml");
                    //load detail daftar tagihan pinjaman dan penjualan kredit barang
                    _ds2 = Finance_Services.Tagihan_Pinjaman_dan_Kredit(p_periode, p_remise);
                    //_ds2.WriteXmlSchema("Tagihan_Pinjaman_dan_kredit.xml");
                    //// Sets up a master-detail relationship between data tables.
                    //DataColumn keyColumn = _ds.Tables["rekap_NIK"].Columns["NIK"];
                    //DataColumn foreignKeyColumn = _ds.Tables["tagihan_waserda"].Columns["NIK"];
                    //_ds.Relations.Add("rekap_tagihan_waserda", keyColumn, foreignKeyColumn);

                    //Bind the grid control to the data source
                    gridControl1.DataSource = _ds.Tables["rekap_NIK"];
                    gridControl1.ForceInitialize();
                    gridView1.OptionsBehavior.Editable = false;
                    //Hide the CategoryID column for the master View
                    gridView1.Columns["UNIT_KERJA"].GroupIndex = 0;
                    gridView1.Columns["SISALALU"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["SISALALU"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["SIMPANAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["SIMPANAN"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["KREDIT"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["KREDIT"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["PINJAMAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["PINJAMAN"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["WASERDA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["WASERDA"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["TOTAL"].DisplayFormat.FormatString = "N0";


                    gridView1.OptionsFind.AlwaysVisible = true;

                    gridView1.OptionsView.ShowFooter = true;
                    gridView1.OptionsFind.ShowFindButton = true;


                    // Add summary for "TOTAL" column
                    GridColumn totalColumn = gridView1.Columns["TOTAL"];
                    totalColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                    totalColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        DateTime remisedari, remisesampai, bulanandari, bulanansampai;

        private void LoadParameterTanggal(int p_periode, int p_remise)
        {
            string query = "SELECT r1dari, r1sampai, r2dari, r2sampai, bdari, bsampai FROM pos_periode WHERE periode = :periode";

            using OracleConnection connection = new OracleConnection(global.connectionString);
            connection.Open();

            using OracleCommand command = new OracleCommand(query, connection);
            // Add parameters to prevent SQL injection
            command.Parameters.Add(new OracleParameter(":periode", p_periode));

            using OracleDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (p_remise == 1)
                {
                    // Assuming r1dari and r1sampai are of type string in the database
                    if (DateTime.TryParse(reader["r1dari"].ToString(), out DateTime dariValue)
                        && DateTime.TryParse(reader["r1sampai"].ToString(), out DateTime sampaiValue))
                    {
                        remisedari = dariValue;
                        remisesampai = sampaiValue;
                    }
                    else
                    {
                        // Handle parsing errors
                        XtraMessageBox.Show("Error parsing date values for remise 1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    if (DateTime.TryParse(reader["r2dari"].ToString(), out DateTime r2DariValue)
                        && DateTime.TryParse(reader["r2sampai"].ToString(), out DateTime r2SampaiValue)
                        && DateTime.TryParse(reader["bdari"].ToString(), out DateTime bDariValue)
                        && DateTime.TryParse(reader["bsampai"].ToString(), out DateTime bSampaiValue))
                    {
                        remisedari = r2DariValue;
                        remisesampai = r2SampaiValue;
                        bulanandari = bDariValue;
                        bulanansampai = bSampaiValue;
                    }
                    else
                    {
                        // Handle parsing errors
                        // Handle parsing errors with MessageBox
                        XtraMessageBox.Show("Error parsing date values for remise 2 and Bulanan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            }
        }


        string periode;
        private void BLBICETAK_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            //_ds.WriteXmlSchema("Tagihan_Penjualan.xsd");

            if (JenisLaporan == "Rekap")
            {
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptRekapbyUnitKerja();
                XtraReport report2 = new rptTagihan_Penjualan();

                // Set the data sources and parameters for each report
                report1.DataSource = _ds;
                report1.RequestParameters = true;
                report1.Parameters["PERIODE"].Value = periode;
                report1.CreateDocument();

                // Set other properties for report1 as needed

                report2.DataSource = _ds;
                report2.RequestParameters = true;
                report2.Parameters["PERIODE"].Value = periode;
                report2.CreateDocument();
                // Set other properties for report2 as needed

                report1.Pages.AddRange(report2.Pages);
                report1.PrintingSystem.ContinuousPageNumbering = true;

                ReportPrintTool tool = new(report1);
                tool.ShowPreview();
            }
            else
            {
                // Assuming you are creating an instance of FrmReportParameter in MainForm
                rptparameter reportParameterForm = new()
                {
                    ReportDataSet = _ds,
                    dsPinjamandanKredit = _ds2,
                    remise = p_remise,
                    report_periode = periode,
                    StartPosition = FormStartPosition.CenterScreen
                };
                reportParameterForm.ShowDialog();
            }

        }

        private void gridView1_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            try
            {
                DevExpress.XtraGrid.Views.Grid.GridView view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
                if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
                {
                    int rowHandle = e.HitInfo.RowHandle;
                    //hapus menu jika ada
                    e.Menu.Items.Clear();
                    DXMenuItem kredit = CreateMenuItemkredit(view, rowHandle);
                    DXMenuItem pinjaman = CreateMenuItempinjaman(view, rowHandle);
                    DXMenuItem waserda = CreateMenuItemwaserda(view, rowHandle);


                    kredit.BeginGroup = true;
                    pinjaman.BeginGroup = true;
                    waserda.BeginGroup = true;


                    e.Menu.Items.Add(kredit);
                    e.Menu.Items.Add(pinjaman);
                    e.Menu.Items.Add(waserda);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemkredit(DevExpress.XtraGrid.Views.Grid.GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Kredit Barang", new EventHandler(OnkreditClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[0];
            return checkItem;
        }

        private void OnkreditClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var NIK = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var NAMA = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            var KREDIT = decimal.Parse(gridView1.GetRowCellValue(rowhandle, "KREDIT").ToString());

            if (KREDIT != 0)
            {
                XtraReport report1 = new rptTagihanKreditBarang_nik
                {
                    DataSource = LaporanManager.TagihanKreditBarang(NIK, p_periode),
                    RequestParameters = true
                };
                report1.Parameters["PERIODE"].Value = periode;
                report1.Parameters["NAMA"].Value = NAMA;
                report1.ShowPreview();
            }
            else
            {
                XtraMessageBox.Show("Pelanggan ini tidak memiliki potongan kredit barang", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private DXMenuItem CreateMenuItempinjaman(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Pinjaman", new EventHandler(OnpinjamanClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[1];
            return checkItem;
        }

        private void OnpinjamanClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var NIK = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var NAMA = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            var PINJAMAN = decimal.Parse(gridView1.GetRowCellValue(rowhandle, "PINJAMAN").ToString());
            if (PINJAMAN != 0)
            {
                XtraReport report1 = new rptTagihanPinjaman_nik
                {
                    DataSource = LaporanManager.TagihanPinjaman(NIK, p_periode),
                    RequestParameters = true
                };
                // report1.Parameters["NAMA"].Value = NAMA + " (Periode : " + startdate.ToString("dd-MMM-yyyy") + " - " + enddate.ToString("dd-MMM-yyyy") + " )";
                report1.ShowPreview();
            }
            else
            {
                XtraMessageBox.Show("Anggota ini tidak memiliki potongan Pinjaman Tunai", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private DXMenuItem CreateMenuItemwaserda(DevExpress.XtraGrid.Views.Grid.GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Waserda", new EventHandler(OnwaserdaClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[2];
            return checkItem;
        }

        private void OnwaserdaClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var NIK = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var NAMA = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            var status = gridView1.GetRowCellValue(rowhandle, "STATUS").ToString();
            var waserda = decimal.Parse(gridView1.GetRowCellValue(rowhandle, "WASERDA").ToString());
            DateTime daritanggal, sampaitanggal;

            if (status == "BULANAN")
            {
                daritanggal = bulanandari;
                sampaitanggal = bulanansampai;

            }
            else
            {
                daritanggal = remisedari;
                sampaitanggal = remisesampai;

            }

            if (waserda != 0)
            {

                XtraReport report1 = new rptTagihan_PenjualanDetails_NIK
                {
                    // Set the data sources and parameters for each report
                    DataSource = LaporanManager.PenjualanWaserdaDetail(NIK, daritanggal, sampaitanggal),
                    RequestParameters = true
                };
                report1.Parameters["PERIODE"].Value = periode;
                report1.Parameters["TANGGAL"].Value = daritanggal.ToString("dd-MMM-yyyy") + "to" + sampaitanggal.ToString("dd-MMM-yyyy");
                report1.Parameters["NIK"].Value = NIK + " " + NAMA;
                // Create a ReportPrintTool instance and assign the report to it
                ReportPrintTool printTool = new(report1);

                // Show the print preview dialog
                printTool.ShowPreview();
            }
            else
            {
                XtraMessageBox.Show("Pelanggan ini tidak memiliki potongan waserda", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var p_periode = periode;
            // Create the sample DataSet
            DataSet dataSet = _ds;
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using ExcelPackage package = new();
            // Add worksheets
            ExcelExporter.AddWorksheet(package, dataSet, "Rekap Tagihan", p_periode);
            ExcelExporter.AddWorksheet(package, dataSet, "Tagihan Detail", p_periode);

            Byte[] bin = package.GetAsByteArray();
            string tempPath = Path.GetTempPath();
            string filename = Path.Combine(tempPath, $"Tagihan_{Guid.NewGuid()}.xlsx");
            File.WriteAllBytes(@filename, bin);
            ProcessStartInfo psi = new(@filename)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }

       
        private void bei_bulan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }
    }
}
