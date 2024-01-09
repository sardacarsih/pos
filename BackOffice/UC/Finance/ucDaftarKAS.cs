using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Laporan;
using BackOffice.Model;
using BackOffice.View;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;

namespace BackOffice.UC
{
    public partial class ucDaftarKAS : UserControl
    {
        string[] bulan = PeriodeServices.GetBulan();

        KASController controller = new();

        //Using singleton pattern to create an instance to ucModule3
        private static ucDaftarKAS _instance;
        public static ucDaftarKAS Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucDaftarKAS();
                return _instance;
            }
        }
        public ucDaftarKAS()
        {
            InitializeComponent();

        }

        private void Load_Bulan()
        {
            int currentMonthIndex = DateTime.Now.Month - 1;
            ((RepositoryItemComboBox)bei_bulan.Edit).Items.AddRange(bulan);
            bei_bulan.EditValue = bulan[currentMonthIndex];
            bei_tahun.EditValue = DateTime.Now.Year;

        }

        private void bei_tahun_EditValueChanged(object? sender, EventArgs e)
        {
            Load_Daftar_Transaksi_KAS();
        }

        private void bei_bulan_EditValueChanged(object sender, EventArgs e)
        {
            Load_Daftar_Transaksi_KAS();
        }

        private void bei_remise_EditValueChanged(object sender, EventArgs e)
        {
            Load_Daftar_Transaksi_KAS();
        }
        DateTime p_daritanggal, p_daritanggalr2, p_sampaitanggal, p_tglAngsuran;

        private void gridView1_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            try
            {
                GridView view = sender as GridView;
                if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
                {
                    int rowHandle = e.HitInfo.RowHandle;
                    //hapus menu jika ada
                    e.Menu.Items.Clear();
                    DXMenuItem ubah = CreateMenuItemUbah(view, rowHandle);


                    ubah.BeginGroup = true;
                    e.Menu.Items.Add(ubah);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemUbah(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Ubah", new EventHandler(OnUbahClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[0];
            return checkItem;
        }

        private void OnUbahClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nik = gridView1.GetRowCellValue(rowhandle, "NIK").ToString();
            var Nama = gridView1.GetRowCellValue(rowhandle, "NAMA_PELANGGAN").ToString();
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            var Tanggal = Convert.ToDateTime(gridView1.GetRowCellValue(rowhandle, "TANGGAL"));
            var Tenor = Convert.ToInt16(gridView1.GetRowCellValue(rowhandle, "TENOR").ToString());
            UbahFakturPinjaman(Nomor, Tanggal, Nik, Nama, Tenor);
        }

        private void UbahFakturPinjaman(string? nomor, DateTime tanggal, string? nik, string? nama, short tenor)
        {
            DTOPinjaman FakturPinjamanHeader = new()
            {
                NO_TRANSAKSI = nomor,
                TANGGAL = tanggal,
                NIK = nik,
                NAMA_PELANGGAN = nama,
                TENOR = tenor - 1
            };
            List<DTOPinjamanDetail> AngsuranPinjaman = Finance_Services.GetDaftarAngsuran(nomor);
            // Tampilkan form untuk memilih produk secara manual
            using frmEditFakturPinjaman UbahForm = new();
            UbahForm.StartPosition = FormStartPosition.CenterScreen;
            UbahForm.FakturPinjamanHeader = FakturPinjamanHeader;
            UbahForm.ListPinjamanDetail = AngsuranPinjaman;

            if (UbahForm.ShowDialog() == DialogResult.OK)
            {
                Load_Daftar_Transaksi_KAS();
            }
        }

        List<DTOTransaksiKAS> daftarTransaksiKAS = new();
        private void Load_Daftar_Transaksi_KAS()
        {

            if (bei_bulan.EditValue != null && bei_tahun.EditValue != null)
            {

                int p_bulan = Array.IndexOf(bulan, bei_bulan.EditValue.ToString()) + 1;
                var p_tahun = Convert.ToInt16(bei_tahun.EditValue.ToString());
                var p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));

                DateTime startDate = new DateTime(p_tahun, p_bulan, 1);
                DateTime endDate=new DateTime(p_tahun,p_bulan,DateTime.DaysInMonth(startDate.Year, startDate.Month));
                // Panggil metode DaftarPinjaman dengan bulan dan tahun yang diinginkan
                daftarTransaksiKAS = controller.KAS_Transaksi("SP",startDate,endDate);
                if(daftarTransaksiKAS != null)
                {
                    gridControl1.DataSource = daftarTransaksiKAS;

                    //RepositoryItemCheckEdit edit = new()
                    //{
                    //    ValueUnchecked = "T",
                    //    ValueChecked = "Y"
                    //};



                    //MASTER
                    //gridView1.Columns["PIUTANG"].VisibleIndex = -1;
                    //gridView1.Columns["SISAWAKTU"].VisibleIndex = -1;
                    gridView1.Columns["DEBET"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["DEBET"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["KREDIT"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["KREDIT"].DisplayFormat.FormatString = "N0";
                    gridView1.Columns["SALDO"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    gridView1.Columns["SALDO"].DisplayFormat.FormatString = "N0";
                    //gridView1.Columns["ISLUNAS"].ColumnEdit = edit;
                    //gridView1.OptionsDetail.EnableMasterViewMode = true;

                }


            }

        }

        private void BLBICETAK_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var periode = "PERIODE : " + bei_bulan.EditValue.ToString() + " " + bei_tahun.EditValue.ToString();
            //REPORT DESIGN FROM OBJECT
            XtraReport report1 = new rptPinjamanTunai
            {
                DataSource = daftarTransaksiKAS,
                RequestParameters = true
            };
            report1.Parameters["PERIODE"].Value = periode;
            report1.ShowPreview();
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Tampilkan form untuk memilih produk secara manual
            using frmKAS TransaksiKAS = new();
            TransaksiKAS.StartPosition = FormStartPosition.CenterScreen;
            TransaksiKAS.form_actions = "Simpan";
            TransaksiKAS.form_title = "Tambah Transaksi KAS";
            TransaksiKAS.productid = 0;

            if (TransaksiKAS.ShowDialog() == DialogResult.OK)
            {
                //LoadMasterBarang(isaktif);
                //string targetKodeItem = MasterBarang.Kode_Item;

                //// Assuming your data source contains a mapping from kode_item to productid
                //int targetProductID = GetProductIDFromKodeItem(targetKodeItem, gridView1);

                //int rowIndex = -1;

                //for (int i = 0; i < gridView1.RowCount; i++)
                //{
                //    int productID = Convert.ToInt32(gridView1.GetRowCellValue(i, "PRODUCTID"));

                //    if (productID == targetProductID)
                //    {
                //        rowIndex = i;
                //        break;
                //    }
                //}

                //if (rowIndex != -1)
                //{
                //    gridView1.FocusedRowHandle = rowIndex;
                //    gridView1.SelectRow(rowIndex);
                //}
            }
        }

        private void ucDaftarKAS_Load(object sender, EventArgs e)
        {
            Load_Bulan();
        }
    }
}
