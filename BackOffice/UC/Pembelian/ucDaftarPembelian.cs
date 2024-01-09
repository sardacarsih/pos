using BackOffice.BussinessLayer;
using BackOffice.Controller;
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
    public partial class ucDaftarPembelian : UserControl
    {
        List<DTOFakturPembelian_Header> daftarPembelian = new();
        string[] bulan = PeriodeServices.GetBulan();
        PembelianController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucDaftarPembelian _instance;
        public static ucDaftarPembelian Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucDaftarPembelian();
                return _instance;
            }
        }
        public ucDaftarPembelian()
        {
            InitializeComponent();
            
        }


        private void ucDaftarPembelian_Load(object sender, EventArgs e)
        {
            Load_Bulan();
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
            Load_Daftar_Pembelian();
        }

        private void bei_bulan_EditValueChanged(object sender, EventArgs e)
        {
            Load_Daftar_Pembelian();
        }      

        private void bei_remise_EditValueChanged(object sender, EventArgs e)
        {
            Load_Daftar_Pembelian();
        }
      

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
                    DXMenuItem hapus = CreateMenuItemHapus(view, rowHandle);
                    DXMenuItem cetak = CreateMenuItemCetak(view, rowHandle);


                    ubah.BeginGroup = true;
                    e.Menu.Items.Add(ubah);
                    hapus.BeginGroup = true;
                    e.Menu.Items.Add(hapus);
                    cetak.BeginGroup = true;
                    e.Menu.Items.Add(cetak);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemCetak(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Cetak", new EventHandler(OnCetakClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[2];
            return checkItem;
        }

        private void OnCetakClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            //var periode = "PERIODE : " + bei_bulan.EditValue.ToString() + " " + bei_tahun.EditValue.ToString();
            //REPORT DESIGN FROM OBJECT
            XtraReport report1 = new rptPembelianDetail
            {
                DataSource = daftarPembelian.Where(no=>no.NO_TRANSAKSI== Nomor).ToList(),
                RequestParameters = true
            };
            //report1.Parameters["PERIODE"].Value = periode;
            report1.ShowPreview();
        }

        private DXMenuItem CreateMenuItemHapus(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Hapus", new EventHandler(OnHapusClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[1];
            return checkItem;
        }

        private void OnHapusClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            // Show a confirmation dialog before deleting
            DialogResult result = XtraMessageBox.Show("Anda yakin akan menghapus faktur Pembelian ?\nnomor : " + Nomor + "\n" 
            , "Hapus Faktur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Remove the item from the list
                controller.HapusPembelian(Nomor);
                Load_Daftar_Pembelian();
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
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            var Tanggal = Convert.ToDateTime(gridView1.GetRowCellValue(rowhandle, "TANGGAL"));
            var SupplierID = gridView1.GetRowCellValue(rowhandle, "SUPPLIER_ID").ToString();
            var Nama = gridView1.GetRowCellValue(rowhandle, "NAMA_SUPPLIER").ToString();           
            var Termin = Convert.ToInt16(gridView1.GetRowCellValue(rowhandle, "TERMIN").ToString());
            UbahFakturPembelian(Nomor, Tanggal, SupplierID, Nama, Termin);
        }

        private void UbahFakturPembelian(string? nomor, DateTime tanggal, string? supplierID, string? nama, short termin)
        {
            try
            {
                DTOFakturPembelian_Header PembeliannHeader = new()
                {
                    NO_TRANSAKSI = nomor,
                    TANGGAL = tanggal,
                    SUPPLIER_ID = supplierID,
                    NAMA_SUPPLIER = nama,
                    BRUTO = 0,
                    POTONGAN = 0,
                    TOTAL = 0,
                };


                List<DTODaftarBarang> itemPembelianData = controller.GetDaftarPembelianBarang(nomor);


                // Tampilkan form untuk memilih produk secara manual
                //using frmEditFaktur UbahForm = new();
                using frmEditFakturPembelian UbahForm = new();
                // Create an instance of ucPenjualanEdit and assign it to ucPenjualanEditInstance
                UbahForm.ucPembelianEditInstance = new ucPembelianEdit();

                //UbahForm.StartPosition = FormStartPosition.CenterScreen;
                // Assuming you have the data available
                UbahForm.ucPembelianEditInstance.SetPembelian(PembeliannHeader);
                UbahForm.ucPembelianEditInstance.SetItemPembelianDetail(itemPembelianData);
                if (UbahForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Load_Daftar_Pembelian();

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

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Load_Daftar_Pembelian();
        }
      
        public void Load_Daftar_Pembelian()
        {
           
            if( bei_bulan.EditValue!= null && bei_tahun.EditValue!=null )
            {
               
                int p_bulan =  Array.IndexOf(bulan, bei_bulan.EditValue.ToString())+1;
                var p_tahun = Convert.ToInt16(bei_tahun.EditValue.ToString());
                var p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));



                // Panggil metode DaftarPinjaman dengan bulan dan tahun yang diinginkan
                daftarPembelian = controller.DaftarPembelian(p_bulan, p_tahun);
               gridControl1.DataSource= daftarPembelian;


                ////MASTER
                gridView1.Columns["PURCHASE_ID"].VisibleIndex = -1;
                gridView1.Columns["USERID"].VisibleIndex = -1;
                gridView1.Columns["BRUTO"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["BRUTO"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["POTONGAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["POTONGAN"].DisplayFormat.FormatString = "N2";
                gridView1.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["TOTAL"].DisplayFormat.FormatString = "N2";
                gridView1.OptionsDetail.EnableMasterViewMode = true;

                //DETAIL
                GridView detailGridView = new (gridControl1);
                gridControl1.LevelTree.Nodes.Add("Details", detailGridView);
                // Assuming _list is the data source for the detail grid
                detailGridView.PopulateColumns(new List<DTOFakturPembelianDetail>());
                detailGridView.OptionsBehavior.Editable = false;
                detailGridView.OptionsView.ShowGroupPanel = false;              


                //// Sort the grid view by the TANGGAL column in ascending order
                //detailGridView.Columns["ANGSURANKE"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
                detailGridView.Columns["PURCHASE_ID"].VisibleIndex = -1;
                detailGridView.Columns["NO_TRANSAKSI"].VisibleIndex = -1;
                detailGridView.Columns["HARGA_JUAL"].VisibleIndex = -1;
                detailGridView.Columns["PRODUCT_ID"].VisibleIndex = -1;               
                detailGridView.Columns["BARIS"].Width = 40;
                detailGridView.Columns["NAMA_BARANG"].Width = 200;
                detailGridView.Columns["HARGA_BELI"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                detailGridView.Columns["HARGA_BELI"].DisplayFormat.FormatString = "N2";
                detailGridView.Columns["BRUTO"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                detailGridView.Columns["BRUTO"].DisplayFormat.FormatString = "N2";
                detailGridView.Columns["POTONGAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                detailGridView.Columns["POTONGAN"].DisplayFormat.FormatString = "N2";
                detailGridView.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                detailGridView.Columns["TOTAL"].DisplayFormat.FormatString = "N2";
               
                //detailGridView.Columns["ISTAGIH"].ColumnEdit = edit;

            }

        }

        private void BLBICETAK_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
           var periode= "PERIODE : "+bei_bulan.EditValue.ToString()+" " + bei_tahun.EditValue.ToString()  ;
            //REPORT DESIGN FROM OBJECT
            XtraReport report1 = new rptPembelianRekap
            {
                DataSource = daftarPembelian,
                RequestParameters = true
            };
            report1.Parameters["PERIODE"].Value = periode;
            report1.ShowPreview();
        }
    }
}
