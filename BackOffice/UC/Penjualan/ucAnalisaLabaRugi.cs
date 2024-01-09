using BackOffice.Model;
using DevExpress.Utils.Menu;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraSplashScreen;
using System.Diagnostics;

namespace BackOffice.UC
{
    public partial class ucAnalisaLabaRugi : UserControl
    {
        PenjualanController controller = new();
        List<DTOLabaRugiAnalisa> DaftarPenjualan;
        //Using singleton pattern to create an instance to ucModule3
        private static ucAnalisaLabaRugi? _instance;
        public static ucAnalisaLabaRugi Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucAnalisaLabaRugi();
                return _instance;
            }
        }
        public ucAnalisaLabaRugi()
        {
            InitializeComponent();

        }


        public void LoadPenjualan()
        {
            IOverlaySplashScreenHandle handle = null;
            handle = SplashScreenManager.ShowOverlayForm(this);
            gridControl1.DataSource = null;
            var date1 = DateTime.Parse(dateEdit1.Text);
            var date2 = DateTime.Parse(dateEdit2.Text);
            //DaftarPenjualan.Clear();
            DaftarPenjualan = controller.AnalisaLabaRugi(date1, date2);
            gridControl1.DataSource = DaftarPenjualan;
            gridView1.Columns["NO_TRANSAKSI"].GroupIndex = 0;
            gridView1.Columns["TANGGAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView1.Columns["TANGGAL"].DisplayFormat.FormatString = "dd-MMM-yyyy";
            gridView1.Columns["PENJUALAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PENJUALAN"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["HPP"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["HPP"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["LABA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["LABA"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["PERSEN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PERSEN"].DisplayFormat.FormatString = "P0"; // "P0" format string represents percentage with no decimal places
            gridView1.BestFitColumns();
            gridView1.ExpandGroupLevel(0);
            SplashScreenManager.CloseOverlayForm(handle);
        }

        private void dateEdit1_EditValueChanged(object sender, EventArgs e)
        {
            if (dateEdit1.EditValue != null && dateEdit2.EditValue != null)
            {
                var date1 = DateTime.Parse(dateEdit1.Text);
                var date2 = DateTime.Parse(dateEdit2.Text);
                if (date1 > date2) { gridControl1.DataSource = null; return; }
                LoadPenjualan();

            }

        }

        private void dateEdit2_EditValueChanged(object sender, EventArgs e)
        {
            if (dateEdit1.EditValue != null && dateEdit2.EditValue != null)
            {
                var date1 = DateTime.Parse(dateEdit1.Text);
                var date2 = DateTime.Parse(dateEdit2.Text);
                if (date1 > date2) { gridControl1.DataSource = null; return; }
                LoadPenjualan();
            }

        }

        private void sbcetak_Click(object sender, EventArgs e)
        {
            var periode = "Tanggal : " + DateTime.Parse(dateEdit1.Text).ToString("dd-MMM-yyyy") + " sampai " + DateTime.Parse(dateEdit2.Text).ToString("dd-MMM-yyyy");

            //// Create instances of the reports you want to merge
            //XtraReport report1 = new rptPenjualanRekap();
            //XtraReport report2 = new rptPenjualan();

            //// Set the data sources and parameters for each report
            //report1.DataSource = REKAPKHT;
            //report1.RequestParameters = true;
            //report1.Parameters["HEADER"].Value = "REKAP TAGIHAN WASERDA KHT";
            //report1.Parameters["PERIODE"].Value = periode;
            //report1.CreateDocument();


            //// Set other properties for report1 as needed

            //report2.DataSource = KHT;
            //report2.RequestParameters = true;
            //report2.Parameters["HEADER"].Value = "DETAIL TAGIHAN WASERDA KHT";
            //report2.Parameters["PERIODE"].Value = periode;
            //report2.CreateDocument();
            //// Set other properties for report2 as needed

            //report1.Pages.AddRange(report2.Pages);
            //report1.PrintingSystem.ContinuousPageNumbering = true;

            //ReportPrintTool tool = new(report1);
            //tool.ShowPreview();




        }

        private void sbexport_Click(object sender, EventArgs e)
        {
            IOverlaySplashScreenHandle handle = null;
            //try
            //{

            handle = SplashScreenManager.ShowOverlayForm(this);

            XlsxExportOptionsEx xlsxOptions = new()
            {
                ShowGridLines = true,
                ExportType = DevExpress.Export.ExportType.Default,    // ExportType
                TextExportMode = TextExportMode.Value,
                RawDataMode = true
            };
            string fileName = "Penjualan.xlsx";
            gridControl1.ExportToXlsx(@fileName, xlsxOptions);
            SplashScreenManager.CloseOverlayForm(handle);
            ProcessStartInfo psi = new ProcessStartInfo(@fileName)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
            //}
            //catch
            //{

            //}
        }

        private void sbrefresh_Click(object sender, EventArgs e)
        {
            LoadPenjualan();
        }

        private void ucAnalisaLabaRugi_Load(object sender, EventArgs e)
        {
            dateEdit1.EditValue = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dateEdit2.EditValue = DateTime.Today;
        }
    }
}
