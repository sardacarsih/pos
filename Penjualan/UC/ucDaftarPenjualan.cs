using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSplashScreen;
using Penjualan.BusinessLayer;
using Penjualan.Laporan;
using Penjualan.Model;
using System.Data;
using System.Diagnostics;

namespace Penjualan.UC
{
    public partial class ucDaftarPenjualan : UserControl
    {
        PenjualanController controller = new();
        List<DTODaftarPenjualan> DaftarPenjualan;
        //DTOFakturPenjualanHeader FakturPenjualanHeader { get; set; }
        List<DTOFakturPenjualanDetail> ListItemsPenjualan { get; set; }
        //Using singleton pattern to create an instance to ucModule3
        private static ucDaftarPenjualan? _instance;
        public static ucDaftarPenjualan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucDaftarPenjualan();
                return _instance;
            }
        }
        public ucDaftarPenjualan()
        {
            InitializeComponent();
            
        }

        private void ucDaftarPenjualan_Load(object sender, EventArgs e)
        {
            dateEdit1.EditValue = new DateTime (DateTime.Today.Year, DateTime.Today.Month,1);
            dateEdit2.EditValue= DateTime.Today;
        }

        private void LoadPenjualan()
        {
            IOverlaySplashScreenHandle handle = null;
            handle = SplashScreenManager.ShowOverlayForm(this);
            gridControl1.DataSource = null;
            var date1 = DateTime.Parse(dateEdit1.Text);
            var date2 = DateTime.Parse(dateEdit2.Text);
            //DaftarPenjualan.Clear();
            DaftarPenjualan = controller.GetPenjualan(date1, date2);
            gridControl1.DataSource = DaftarPenjualan;
            gridView1.Columns["STATUS"].GroupIndex = 0;
            gridView1.Columns["UNIT_KERJA"].GroupIndex = 1;
            gridView1.Columns["BRUTO"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["BRUTO"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["POTONGAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["POTONGAN"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["TOTAL"].DisplayFormat.FormatString = "N0";            
            gridView1.BestFitColumns();
            gridView1.ExpandGroupLevel(0);
            SplashScreenManager.CloseOverlayForm(handle);
        }

        private void dateEdit1_EditValueChanged(object sender, EventArgs e)
        {
            if(dateEdit1.EditValue != null && dateEdit2.EditValue != null)
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
           
            var rekap = DaftarPenjualan.GroupBy(p => new {
                Jenis=p.NIK== Global.DefaultCustomerNIK ? "Tunai":"Kredit",
                p.TANGGAL })
                .Select(g => new DTORekapPenjualan
                {
                    Jenis=g.Key.Jenis,
                    Tanggal=g.Key.TANGGAL,
                    Jumlah=g.Sum(p=>p.TOTAL)
                });
            rptRekapJual report = new ()
            {
                DataSource = rekap,
                RequestParameters = true
            };
            report.Print();
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
        private void CetakFaktur(string nofaktur)
        {
            var FakturPenjualanHeader = DaftarPenjualan.Where(x => x.NO_TRANSAKSI == nofaktur).FirstOrDefault();
            List<DTODaftarBarang> ListItemsPenjualan = controller.GetDaftarBarang(nofaktur);

            rptFakturJual report = new()
            {
                DataSource = ListItemsPenjualan
            };

            // Set the default paper height
            int defaultPageHeight = 1169;
            report.PageHeight = defaultPageHeight;

            // Calculate the paper height based on the number of records
            if (ListItemsPenjualan.Count > 40)
            {
                float paperHeight = CalculatePaperHeight(ListItemsPenjualan.Count);

                // Set the calculated paper height with an explicit cast to int
                report.PageHeight = (int)paperHeight;
            }

            // Set other properties
            report.PageWidth = 297;
            report.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            report.PaperName = "Roll Paper 76 x 297 mm";

            // Set other parameters
            report.Parameters["Nama_Toko"].Value = "KOPKAR - KUSUMA LESTARI";
            report.Parameters["NoFaktur"].Value = FakturPenjualanHeader.NO_TRANSAKSI;
            report.Parameters["Pelanggan"].Value = FakturPenjualanHeader.NIK + ", " + FakturPenjualanHeader.NAMA_PELANGGAN.ToUpper() + " , " + FakturPenjualanHeader.UNIT_KERJA;
            report.Parameters["Jenis_Bayar"].Value = "KREDIT";
            report.Parameters["Waktu"].Value = FakturPenjualanHeader.TANGGAL.ToString("dd-MMM-yy");
            report.Parameters["Tenor"].Value = FakturPenjualanHeader.TENOR;
            report.Parameters["Bruto"].Value = FakturPenjualanHeader.BRUTO;
            report.Parameters["Potongan"].Value = FakturPenjualanHeader.POTONGAN;
            report.Parameters["Total"].Value = FakturPenjualanHeader.TOTAL;

            report.ShowPrintMarginsWarning = false;
            ReportPrintTool tool = new(report);
            tool.Print();
        }

        private float CalculatePaperHeight(int numberOfRecords)
        {
            // Adjust the paper height based on the number of records
            // You might need to fine-tune these values based on your actual requirements
            float defaultPageHeight = 1169.0f; // Base paper height
            float heightPerRecord = 0.5f; // Height per record

            float calculatedHeight = defaultPageHeight + (numberOfRecords * heightPerRecord);

            return calculatedHeight;
        }




        private void gridView1_PopupMenuShowing(object sender, DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            try
            {
                GridView view = sender as GridView;
                if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
                {
                    int rowHandle = e.HitInfo.RowHandle;
                    //hapus menu jika ada
                    e.Menu.Items.Clear();

                    DXMenuItem cetak = CreateMenuItemCetak(view, rowHandle);
                    DXMenuItem ubah = CreateMenuItemUbah(view, rowHandle);
                    DXMenuItem DetailBarang = CreateMenuItemDetail(view, rowHandle);


                    cetak.BeginGroup = true;
                    ubah.BeginGroup = true;
                    DetailBarang.BeginGroup = true;
                    e.Menu.Items.Add(cetak);
                    e.Menu.Items.Add(ubah);
                    e.Menu.Items.Add(DetailBarang);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemDetail(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Detail Nota", new EventHandler(OnDetailClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[1];
            return checkItem;
        }

        private void OnDetailClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nik = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var Nama = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();         
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            var Tanggal = Convert.ToDateTime(gridView1.GetRowCellValue(rowhandle, "TANGGAL"));
            var Bruto = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "BRUTO").ToString());
            var Potongan = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "POTONGAN").ToString());
            var Total = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "TOTAL").ToString());
            var Tenor = Convert.ToInt16(gridView1.GetRowCellValue(rowhandle, "TENOR").ToString());

            DTOFakturPenjualanHeader PenjualanHeader = new()
            {
                NO_TRANSAKSI = Nomor,
                TANGGAL = Tanggal,
                NIK = Nik,
                NAMA_PELANGGAN = Nama,
                BRUTO = Bruto,
                POTONGAN = Potongan,
                TOTAL = Total,
                TENOR = Tenor
            };

            //List<DTODaftarBarang> itemPenjualanData = controller.GetDaftarBarang(nofaktur);
            List<DTODaftarBarang> itemPenjualanData = controller.GetDaftarBarang(Nomor);


            // Tampilkan form untuk memilih produk secara manual
            using frmViewFakturPenjualan viewForm = new();
            viewForm.StartPosition = FormStartPosition.CenterScreen;
            viewForm.FakturPenjualanHeader = PenjualanHeader;
            viewForm.ListItemsPenjualan = itemPenjualanData;
            viewForm.ShowDialog();
        }

        private DXMenuItem CreateMenuItemUbah(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Ubah", new EventHandler(OnUbahClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[1];
            return checkItem;
        }

        private void OnUbahClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nik = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var Nama = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            if (Nik == Global.DefaultCustomerNIK)
            {
                XtraMessageBox.Show("Penjualan Tunai tidak dapat diubah", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            var Tanggal = Convert.ToDateTime(gridView1.GetRowCellValue(rowhandle, "TANGGAL"));
            //var Kasir = gridView1.GetRowCellValue(rowhandle, "KASIR").ToString();
            var Bruto = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "BRUTO").ToString());
            var Potongan = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "POTONGAN").ToString());
            var Total = Convert.ToDecimal(gridView1.GetRowCellValue(rowhandle, "TOTAL").ToString());
            var Tenor = Convert.ToInt16(gridView1.GetRowCellValue(rowhandle, "TENOR").ToString());
            UbahFaktur(Nomor, Tanggal, Nik,Nama, Bruto, Potongan, Total, Tenor);
        }

        private void UbahFaktur(string? nomor, DateTime tanggal, string? nik, string? nama, decimal bruto, decimal potongan, decimal total, short tenor)
        {
            DTOFakturPenjualanHeader PenjualanHeader = new()
            {
                NO_TRANSAKSI = nomor,
                TANGGAL = tanggal,
                NIK = nik,
                NAMA_PELANGGAN= nama,
                BRUTO = bruto,
                POTONGAN = potongan,
                TOTAL = total,
                TENOR = tenor
            };

            //List<DTODaftarBarang> itemPenjualanData = controller.GetDaftarBarang(nofaktur);
            List<DTODaftarBarang> itemPenjualanData = controller.GetDaftarBarang(nomor);


            // Tampilkan form untuk memilih produk secara manual
            using frmEditFaktur UbahForm = new();
            UbahForm.StartPosition = FormStartPosition.CenterScreen;
            UbahForm.FakturPenjualanHeader = PenjualanHeader;
            UbahForm.ListItemsPenjualan = itemPenjualanData;

            if (UbahForm.ShowDialog() == DialogResult.OK)
            {
                LoadPenjualan();
            }
        }

        private DXMenuItem CreateMenuItemCetak(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Cetak Ulang", new EventHandler(OnCetakClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[0];
            return checkItem;
        }

        private void OnCetakClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            CetakFaktur(Nomor);
        }

        private void sbrefresh_Click(object sender, EventArgs e)
        {
            LoadPenjualan();
        }

        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            OnDetailClick(null, EventArgs.Empty);
        }
    }
}
