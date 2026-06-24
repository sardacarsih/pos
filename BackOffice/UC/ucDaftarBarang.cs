using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using BackOffice.View;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace BackOffice.UC
{
    public partial class ucDaftarBarang : DevExpress.XtraEditors.XtraUserControl
    {
        MasterDataController controller = new();
        List<DTOPRODUCTS> DaftarBarang;
        List<DTODiskon> DaftarDiskon;
        string isaktif = "Y";
        //Using singleton pattern to create an instance to ucModule3
        private static ucDaftarBarang _instance;
        public static ucDaftarBarang Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucDaftarBarang();
                return _instance;
            }
        }
        public ucDaftarBarang()
        {
            InitializeComponent();
        }
        private void ucDaftarBarang_Load(object sender, EventArgs e)
        {
            LoadMasterBarang("Y");
        }

        private void LoadMasterBarang(string isaktif)
        {
            DaftarBarang = controller.GetBarang(isaktif);
            DaftarDiskon = controller.GetDiskon();
            gridControl1.DataSource = DaftarBarang;
            gridView1.Columns[0].Visible = false;
            gridView1.Columns["KATEGORI"].Visible = false;
            gridView1.Columns["KATEGORI_ID"].Visible = false;
            gridView1.Columns["BARCODE"].Width = 100;
            gridView1.Columns["PRODUCTNAME"].Width = 300;
            gridView1.Columns["PRODUCTNAME"].Caption = "NAMA BARANG";
            gridView1.Columns["BELI"].Caption = "HARGA BELI";
            gridView1.Columns["PRICE"].Caption = "HARGA JUAL";
            gridView1.Columns["PRICE"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PRICE"].DisplayFormat.FormatString = "n0";
            gridView1.Columns["BELI"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["BELI"].DisplayFormat.FormatString = "n0";
            gridView1.Columns["MARGIN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["MARGIN"].DisplayFormat.FormatString = "n0";
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Tampilkan form untuk memilih produk secara manual
            using frmMasterBarang MasterBarang = new();
            MasterBarang.StartPosition = FormStartPosition.CenterScreen;
            MasterBarang.form_actions = "Simpan";
            MasterBarang.form_title = "Tambah Barang";
            MasterBarang.productid = 0;

            if (MasterBarang.ShowDialog() == DialogResult.OK)
            {
                LoadMasterBarang(isaktif);
                string targetKodeItem = MasterBarang.Kode_Item;

                // Assuming your data source contains a mapping from kode_item to productid
                int targetProductID = GetProductIDFromKodeItem(targetKodeItem, gridView1);

                int rowIndex = -1;

                for (int i = 0; i < gridView1.RowCount; i++)
                {
                    int productID = Convert.ToInt32(gridView1.GetRowCellValue(i, "PRODUCTID"));

                    if (productID == targetProductID)
                    {
                        rowIndex = i;
                        break;
                    }
                }

                if (rowIndex != -1)
                {
                    gridView1.FocusedRowHandle = rowIndex;
                    gridView1.SelectRow(rowIndex);
                }
            }

        }
        private int GetProductIDFromKodeItem(string kodeitem, GridView gridView)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                string currentKodeItem = gridView.GetRowCellValue(i, "KODE_ITEM").ToString();
                if (currentKodeItem == kodeitem)
                {
                    int productID = Convert.ToInt32(gridView.GetRowCellValue(i, "PRODUCTID"));
                    return productID;
                }
            }
            return -1; // Return -1 if the kodeitem is not found
        }

        private void gridView1_Click(object sender, EventArgs e)
        {
            LoadDiskon();
            PilihItemBarang();
        }
        int productid;
        string barcode, kode_item, productname, satuan, kategori;
        decimal price, beli;
        char aktif;

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtminqty.Text) && string.IsNullOrEmpty(txtpotrp.Text))
            {
                return;
            }
            MergeDiskonBarang(kode_item, int.Parse(txtminqty.Text), decimal.Parse(txtpotrp.Text, CultureInfo.InvariantCulture));
            DaftarDiskon = controller.GetDiskon();
            LoadDiskon();
            txtminqty.Text = string.Empty;
            txtpotrp.Text = string.Empty;
            XtraMessageBox.Show("Update Diskon Barang selesai ", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public int MergeDiskonBarang(string kode_item, int min_qty, decimal diskon_rp)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string mergeQuery = @"
                MERGE INTO POS_POTONGANBERDASARKANQTY tgt
                USING (SELECT :kode_item AS kodeitem,
                              :min_qty AS minqty,
                              :diskon_rp AS diskonrp
                       FROM dual) src
                ON (tgt.KODE_ITEM = src.kodeitem)
                WHEN MATCHED THEN
                    UPDATE SET tgt.MINQTY = src.minqty,
                               tgt.POTONGAN = src.diskonrp
                WHEN NOT MATCHED THEN
                    INSERT (tgt.KODE_ITEM,tgt.MINQTY, tgt.POTONGAN)
                    VALUES (src.kodeitem, src.minqty,src.diskonrp)";

            using OracleCommand command = new(mergeQuery, connection);
            // Set parameter values
            //command.Parameters.Add(":kode_item", OracleDbType.Varchar2).Value = kode_item;
            //command.Parameters.Add(":min_qty", OracleDbType.Int32).Value = min_qty;
            //command.Parameters.Add(":diskon_rp", OracleDbType.Decimal).Value = diskon_rp;

            // Set parameter values
            command.Parameters.Add(new OracleParameter(":kode_item", OracleDbType.Varchar2)).Value = kode_item;
            command.Parameters.Add(new OracleParameter(":min_qty", OracleDbType.Int32)).Value = min_qty;
            command.Parameters.Add(new OracleParameter(":diskon_rp", OracleDbType.Decimal)).Value = diskon_rp;

            // Execute the MERGE statement
            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected;
        }

        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (productid == 0) { return; }

                // Show a confirmation dialog before deleting
                DialogResult result = XtraMessageBox.Show("Nama Barang : " + productname + "\nAnda yakin akan menghapus barang ini?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    controller.DeleteProduct(productid);
                    LoadMasterBarang(isaktif);
                    productid = 0;
                }


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("child record found"))
                {
                    XtraMessageBox.Show("Barang ini tidak dapat dihapus, karena sudah ada transaksi", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                else
                {
                    XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }


        }
        string filter_itembarang;
        private void PilihItemBarang()
        {
            try
            {
                if (gridView1.SelectedRowsCount > 0)
                {
                    int selectedIndex = gridView1.GetSelectedRows()[0];
                    int selectedHandle = gridView1.GetVisibleRowHandle(selectedIndex);
                    DTOPRODUCTS selectedItem = gridView1.GetRow(selectedHandle) as DTOPRODUCTS;

                    if (selectedItem != null)
                    {
                        // Rest of the code remains the same
                        productid = selectedItem.PRODUCTID;
                        barcode = selectedItem.BARCODE;
                        kode_item = selectedItem.KODE_ITEM;
                        productname = selectedItem.PRODUCTNAME;
                        kategori = selectedItem.KATEGORI_ID;
                        satuan = selectedItem.SATUAN;
                        price = selectedItem.PRICE;
                        beli = selectedItem.BELI;
                        aktif= selectedItem.AKTIF;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any general exception that might occur
                // Log the exception or display an error message
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDiskon()
        {
            try
            {
                filter_itembarang = gridView1.GetRowCellValue(gridView1.FocusedRowHandle, "KODE_ITEM")?.ToString();

                if (!string.IsNullOrEmpty(filter_itembarang))
                {
                    var diskon = DaftarDiskon.Where(x => x.KODE_ITEM == filter_itembarang);
                    gridControl2.DataSource = diskon;
                    gridView2.Columns[0].Visible = false;
                }
            }
            catch (Exception ex)
            {
                // Handle any general exception that might occur
                // Log the exception or display an error message
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gridView1_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                LoadDiskon();
                PilihItemBarang();
            }
            catch (Exception ex)
            {
                // Handle any general exception that might occur
                // Log the exception or display an error message
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void blbiubahbarang_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // var editbarang = FilteredBarangAktif.FirstOrDefault();
            // Tampilkan form untuk memilih produk secara manual
            using frmMasterBarang EditMasterBarang = new();
            EditMasterBarang.StartPosition = FormStartPosition.CenterScreen;
            EditMasterBarang.form_actions = "Update";
            EditMasterBarang.form_title = "Update Barang";

            EditMasterBarang.productid = productid;
            EditMasterBarang.kategori = kategori;
            EditMasterBarang.barcode = barcode;
            EditMasterBarang.kode_item = kode_item;
            EditMasterBarang.productname = productname;
            EditMasterBarang.satuan = satuan;
            EditMasterBarang.price = price;
            EditMasterBarang.beli = beli;
            EditMasterBarang.aktif = aktif;
            if (EditMasterBarang.ShowDialog() == DialogResult.OK)
            {
                LoadMasterBarang(isaktif);

                int targetProductID = productid; // Replace with the desired product ID
                int rowIndex = -1;

                for (int i = 0; i < gridView1.RowCount; i++)
                {
                    int productID = Convert.ToInt32(gridView1.GetRowCellValue(i, "PRODUCTID"));

                    if (productID == targetProductID)
                    {
                        rowIndex = i;
                        break;
                    }
                }

                if (rowIndex != -1)
                {
                    gridView1.FocusedRowHandle = rowIndex;
                    gridView1.SelectRow(rowIndex);
                }

            }
        }

        private void barToggleSwitchItem1_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if(barToggleSwitchItem1.Checked)
            {
                isaktif = "T";
            }
            else
            {
                isaktif = "Y";
            }
            LoadMasterBarang(isaktif);
        }
    }
}
