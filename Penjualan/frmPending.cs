using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Penjualan.BusinessLayer;
using Penjualan.Model;

namespace Penjualan
{
    public partial class frmPending : DevExpress.XtraEditors.XtraForm
    {
        
        PendingController controller = new();
        private string nomor;
        private DateTime tanggal;
        private string jam;

        public string No_Transaksi
        {
            get { return nomor; }
        }
        public DateTime Tanggal
        {
            get { return tanggal; }
        }
        public string Jam
        {
            get { return jam; }
        }

        public frmPending()
        {
            InitializeComponent();
        }

        private void frmPending_Load(object sender, EventArgs e)
        {
            LoadDaftarPending();
        }

        private void LoadDaftarPending()
        {
            var DaftarPenjualan = controller.GetPenjualanPending();
            gridControl1.DataSource = DaftarPenjualan;
            gridView1.OptionsDetail.EnableMasterViewMode = true;
            //DETAIL
            GridView detailGridView = new(gridControl1);
            gridControl1.LevelTree.Nodes.Add("Details", detailGridView);
            // Assuming _list is the data source for the detail grid
            detailGridView.PopulateColumns(new List<DTODaftarBarangPending>());
            detailGridView.OptionsBehavior.Editable = false;
            detailGridView.OptionsView.ShowGroupPanel = false;
            detailGridView.Columns["NO_TRANSAKSI"].Visible = false;
            detailGridView.Columns["PRODUCT_ID"].Visible = false;
            detailGridView.Columns["KODE_BARANG"].Visible = false;
            detailGridView.Columns["BARCODE"].Visible = false;
            detailGridView.Columns["HPP"].Visible = false;
            detailGridView.Columns["NAMA_BARANG"].Width = 300;
        }

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Get_Pending_List();
            }
        }

        private void Get_Pending_List()
        {
            if (gridView1.SelectedRowsCount > 0)
            {
                int selectedIndex = gridView1.GetSelectedRows()[0];
                int selectedHandle = gridView1.GetVisibleRowHandle(selectedIndex);
                DTOFakturPending selectedItem = gridView1.GetRow(selectedHandle) as DTOFakturPending;

                // Rest of the code remains the same
                nomor = selectedItem.NO_TRANSAKSI;
                tanggal = selectedItem.TANGGAL;
                jam = selectedItem.JAM;
                this.DialogResult = DialogResult.OK;
            }
        }

        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            Get_Pending_List();
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

                    DXMenuItem hapus = CreateMenuItemHapus(view, rowHandle);


                    hapus.BeginGroup = true;
                    e.Menu.Items.Add(hapus);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemHapus(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Hapus", new EventHandler(OnHapusClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[0];
            return checkItem;
        }

        private void OnHapusClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var Nomor = gridView1.GetRowCellValue(rowhandle, "NO_TRANSAKSI").ToString();
            controller.DeletePendingFaktur(Nomor);
            LoadDaftarPending();
        }

        private void frmPending_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)27) // Escape key
            {
                this.Close(); // Close the form
            }
        }
    }
}