using BackOffice.Laporan;
using BackOffice.Model;
using BackOffice.View;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSplashScreen;
using System.Data;
using System.Diagnostics;

namespace BackOffice.UC
{
    public partial class ucDaftarPenjualan : UserControl
    {
        PenjualanController controller = new();
        List<DTODaftarPenjualan> DaftarPenjualan;
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

        public void LoadPenjualan()
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

            // Enable the footer to show summaries
            gridView1.OptionsView.ShowFooter = true;

            gridView1.GroupSummary.Clear();
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "BRUTO", null, "(BRUTO = {0:N0})"));
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "POTONGAN", null, "(POTONGAN = {0:N0})"));
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "TOTAL", null, "(TOTAL = {0:N0})"));

            // Add subtotals for "PENJUALAN," "HPP," and "LABA" columns within each group
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "BRUTO", gridView1.Columns["BRUTO"], "Subtotal: {0:N0}");
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "POTONGAN", gridView1.Columns["POTONGAN"], "Subtotal: {0:N0}");
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "TOTAL", gridView1.Columns["TOTAL"], "Subtotal: {0:N0}");

            // Add summary for "PENJUALAN" column
            GridColumn penjualanColumn = gridView1.Columns["TOTAL"];
            penjualanColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            penjualanColumn.SummaryItem.DisplayFormat = "Total Penjualan: {0:N0}";
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
            var periode = "Tanggal : " + DateTime.Parse(dateEdit1.Text).ToString("dd-MMM-yyyy") + " sampai " + DateTime.Parse(dateEdit2.Text).ToString("dd-MMM-yyyy");
            if (radioGroup1.SelectedIndex == 0)
            {
                var rekap = DaftarPenjualan.GroupBy(p => new {
                    Jenis = p.NIK == DataLayer.global.DefaultCustomerNIK ? "Tunai" : "Kredit",
                    p.TANGGAL
                })
                .Select(g => new DTORekapPenjualan
                {
                    Jenis = g.Key.Jenis,
                    Tanggal = g.Key.TANGGAL,
                    Jumlah = g.Sum(p => p.TOTAL)
                });
                rptRekapJual report = new()
                {
                    DataSource = rekap,
                    RequestParameters = true
                };
                report.ShowPreview();
            }
            else if (radioGroup1.SelectedIndex == 1)
            {

                var REKAPKHT = DaftarPenjualan.Where(p => p.STATUS == "KHT")
                    .GroupBy(g=> new { g.UNIT_KERJA, g.NIK,g.NAMA_PELANGGAN})
                    .Select(group=>new DTORekapPenjualanByNik
                    {
                        UNIT_KERJA = group.Key.UNIT_KERJA,
                        NIK = group.Key.NIK,
                        NAMA_PELANGGAN = group.Key.NAMA_PELANGGAN,
                        TOTAL = group.Sum(x=>x.TOTAL)
                    });
                var KHT = DaftarPenjualan.Where(p => p.STATUS == "KHT");
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptPenjualanRekap();
                XtraReport report2 = new rptPenjualan();

                // Set the data sources and parameters for each report
                report1.DataSource = REKAPKHT;
                report1.RequestParameters = true;
                report1.Parameters["HEADER"].Value = "REKAP TAGIHAN WASERDA KHT";
                report1.Parameters["PERIODE"].Value = periode;
                report1.CreateDocument();


                // Set other properties for report1 as needed

                report2.DataSource = KHT;
                report2.RequestParameters = true;
                report2.Parameters["HEADER"].Value = "DETAIL TAGIHAN WASERDA KHT";
                report2.Parameters["PERIODE"].Value = periode;
                report2.CreateDocument();
                // Set other properties for report2 as needed

                report1.Pages.AddRange(report2.Pages);
                report1.PrintingSystem.ContinuousPageNumbering = true;

                ReportPrintTool tool = new(report1);
                tool.ShowPreview();

            }
            else if (radioGroup1.SelectedIndex == 2)
            {
                var REKAPBULANAN = DaftarPenjualan.Where(p => p.STATUS == "BULANAN" && p.NIK != DataLayer.global.DefaultCustomerNIK)
                    .GroupBy(g => new { g.UNIT_KERJA, g.NIK, g.NAMA_PELANGGAN })
                    .Select(group => new DTORekapPenjualanByNik
                    {
                        UNIT_KERJA = group.Key.UNIT_KERJA,
                        NIK = group.Key.NIK,
                        NAMA_PELANGGAN = group.Key.NAMA_PELANGGAN,
                        TOTAL = group.Sum(x => x.TOTAL)
                    });
                var BULANAN = DaftarPenjualan.Where(p => p.STATUS == "BULANAN" && p.NIK != DataLayer.global.DefaultCustomerNIK);
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptPenjualanRekap();
                XtraReport report2 = new rptPenjualan();

                // Set the data sources and parameters for each report
                report1.DataSource = REKAPBULANAN;
                report1.RequestParameters = true;
                report1.Parameters["HEADER"].Value = "REKAP TAGIHAN WASERDA BULANAN";
                report1.Parameters["PERIODE"].Value = periode;
                report1.CreateDocument();


                // Set other properties for report1 as needed

                report2.DataSource = BULANAN;
                report2.RequestParameters = true;
                report2.Parameters["HEADER"].Value = "DETAIL TAGIHAN WASERDA BULANAN";
                report2.Parameters["PERIODE"].Value = periode;
                report2.CreateDocument();
                // Set other properties for report2 as needed

                report1.Pages.AddRange(report2.Pages);
                report1.PrintingSystem.ContinuousPageNumbering = true;

                ReportPrintTool tool = new(report1);
                tool.ShowPreview();

            }


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
                    DXMenuItem hapus = CreateMenuItemHapus(view, rowHandle);
                    DXMenuItem DetailBarang = CreateMenuItemDetail(view, rowHandle);


                    cetak.BeginGroup = true;
                    ubah.BeginGroup = true;
                    hapus.BeginGroup = true;
                    DetailBarang.BeginGroup = true;
                    e.Menu.Items.Add(cetak);
                    e.Menu.Items.Add(ubah);
                    e.Menu.Items.Add(hapus);
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

        private DXMenuItem CreateMenuItemHapus(GridView view, int rowHandle)
        {
            DXMenuItem checkItem = new("Hapus", new EventHandler(OnHapusClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[2];
            return checkItem;
        }

        private void OnHapusClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            
            var Nama = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            // Show a confirmation dialog before deleting
            DialogResult result = MessageBox.Show("Anda yakin akan menghapus faktur penjualan ?\nnomor : "+ Nomor +"\n" +
                "atas nama :"+ Nama+" ", "Hapus Faktur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Remove the item from the list
                controller.HapusFakturPenjualan(Nomor);
                LoadPenjualan();

            }

        }

        private DXMenuItem CreateMenuItemUbah(GridView view, int rowHandle)
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
            if (Nik == DataLayer.global.DefaultCustomerNIK)
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
            UbahFaktur(Nomor, Tanggal, Nik, Nama, Bruto, Potongan, Total, Tenor);
        }

        private void UbahFaktur(string? nomor, DateTime tanggal, string? nik, string? nama, decimal bruto, decimal potongan, decimal total, short tenor)
        {
            try
            {
                DTOFakturPenjualanHeader PenjualanHeader = new()
                {
                    NO_TRANSAKSI = nomor,
                    TANGGAL = tanggal,
                    NIK = nik,
                    NAMA_PELANGGAN = nama,
                    BRUTO = bruto,
                    POTONGAN = potongan,
                    TOTAL = total,
                    TENOR = tenor
                };


                List<DTODaftarBarang> itemPenjualanData = controller.GetDaftarBarang(nomor);


                // Tampilkan form untuk memilih produk secara manual
                //using frmEditFaktur UbahForm = new();
                using frmEditFakturPenjualan UbahForm = new();
                // Create an instance of ucPenjualanEdit and assign it to ucPenjualanEditInstance
                UbahForm.ucPenjualanEditInstance = new ucPenjualanEdit();

                //UbahForm.StartPosition = FormStartPosition.CenterScreen;
                // Assuming you have the data available
                UbahForm.ucPenjualanEditInstance.SetPenjualanHeader(PenjualanHeader);
                UbahForm.ucPenjualanEditInstance.SetItemPenjualanData(itemPenjualanData);
                if (UbahForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LoadPenjualan();

                        string targetProductID = nomor; // Replace with the desired product ID
                        int rowIndex = -1;

                        // Expand all group rows
                        gridView1.ExpandAllGroups();

                        for (int i = 0; i < gridView1.RowCount; i++)
                        {
                            object cellValue = gridView1.GetRowCellValue(i, "NO_TRANSAKSI");
                            if (cellValue != null)
                            {
                                string productID = cellValue.ToString();
                                // Now you can use the productID variable safely.

                               // string productID = gridView1.GetRowCellValue(i, "NO_TRANSAKSI").ToString();

                                if (productID == targetProductID)
                                {
                                    rowIndex = i;
                                    break;
                                }
                            }
                            
                        }

                        if (rowIndex != -1)
                        {
                            // Collapse all group rows
                            gridView1.CollapseAllGroups();
                            // Expand the group row at the specified rowIndex
                            gridView1.ExpandGroupRow(rowIndex);
                            // Set the focused row to the selected row
                            gridView1.FocusedRowHandle = rowIndex;
                            gridView1.SelectRow(rowIndex);
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private DXMenuItem CreateMenuItemCetak(GridView view, int rowHandle)
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

        private void sbrefresh_Click(object sender, EventArgs e)
        {
            LoadPenjualan();
        }
    }
}
