using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;

namespace BackOffice.UC
{
    public partial class ucBarangRusak : UserControl
    {
        private BindingList<TransactionBarangRusak> transactionDataList;
        StokOpnameController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucBarangRusak? _instance;
        public static ucBarangRusak Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucBarangRusak();
                return _instance;
            }
        }

        public ucBarangRusak()
        {
            InitializeComponent();
            transactionDataList = new BindingList<TransactionBarangRusak>();
            gridControl1.DataSource = transactionDataList;
        }
        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var startdate = new DateTime(Convert.ToDateTime(detanggal.Text).Year, 1, 1);
                var enddate = Convert.ToDateTime(detanggal.Text);
                string kodeitem = string.Empty;
                string barcode = barcodeTextBox.Text;
                if (string.IsNullOrEmpty(barcode)) { return; }
                DTOProductInfo productInfo = POS_Services.RetrieveProductInfo(barcode);

                if (productInfo.ProductId != 0)
                {
                    int productid = Convert.ToInt32(productInfo.ProductId);
                    kodeitem = productInfo.KodeItem.ToString();
                    string productname = productInfo.ProductName.ToString();
                    string satuan = productInfo.Satuan.ToString();
                    decimal hpp = Convert.ToDecimal(productInfo.Hpp);

                    //var exist_so = controller.CheckStockOpname(kodeitem, enddate);
                    //if (exist_so)
                    //{
                    //    XtraMessageBox.Show("Item Barang rusak sudah ada pada tanggal diatasnya\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //    return;
                    //}

                    var existingProduct = transactionDataList.FirstOrDefault(p => p.Barcode == barcode);
                    if (existingProduct != null)
                    {
                        XtraMessageBox.Show("Item Barang sudah ada pada Daftar\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtnamabarang.Text = string.Empty;
                        barcodeTextBox.Focus();
                        return;
                    }
                    txtItemBarang.Text = kodeitem;
                    txtnamabarang.Text = productname;
                    txtsatuan.Text = satuan;
                    txthpp.Text = hpp.ToString();


                }
                else
                {
                    // Tampilkan form untuk memilih produk secara manual
                    using ProductForm productForm = new();
                    productForm.StartPosition = FormStartPosition.CenterScreen;
                    productForm.SetSearchPanelValue(barcodeTextBox.Text);
                    if (productForm.ShowDialog() == DialogResult.OK)
                    {
                        productid = productForm.ProductId;
                        kodeitem = productForm.Kode_Item;
                        string barcodefromx = productForm.Barcode;
                        string productname = productForm.ProductName;
                        string satuan = productForm.Satuan;
                        decimal hpp = productForm.Hpp != null ? productForm.Hpp : 0m;
                       

                        var exist_so = controller.CheckStockOpname(kodeitem, enddate);
                        if (exist_so)
                        {
                            XtraMessageBox.Show("Stock Opname Item Barang sudah ada pada tanggal yang sama atau diatasnya\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        var existingProduct = transactionDataList.FirstOrDefault(p => p.ProductId == productid);
                        if (existingProduct != null)
                        {
                            XtraMessageBox.Show("Item Barang sudah ada pada Daftar\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtnamabarang.Text = string.Empty;
                            barcodeTextBox.Focus();
                            return;
                        }

                        barcodeTextBox.Text = barcodefromx;
                        txtItemBarang.Text = kodeitem;
                        txtnamabarang.Text = productname;
                        txtsatuan.Text = satuan;
                        txthpp.Text = hpp.ToString();
                    }

                    txtqtyfisik.Focus();
                }
            }
            else if (e.KeyCode == Keys.F2)
            {
                // Set focus to the GridView control
                gridView1.Focus();

                // Get the last row index
                int lastIndex = gridView1.RowCount - 1;

                // Start editing the Qty column on the last row
                gridView1.FocusedRowHandle = lastIndex;
                gridView1.FocusedColumn = gridView1.Columns["Qty"];
                gridView1.ShowEditor();
            }
            else if (e.KeyCode == Keys.F5)
            {
                blbisimpan.PerformClick();
            }
        }

        private void NewTransaction()
        {
            detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");

            txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text));
            transactionDataList.Clear();           
            txtqtyfisik.Text = "0";
            txthpp.Text = "0";
            txtketerangan.Text = "Rusak";
            barcodeTextBox.Focus();
        }

        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'STOCK_OPNAME' AND nomor LIKE 'SO-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand selectCommand = new(selectQuery, connection);
            string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

            // Buat nomor transaksi baru untuk tahun saat ini
            string newTransactionNumber;
            if (string.IsNullOrEmpty(lastTransactionNumber))
            {
                newTransactionNumber = $"SO-{currentYear.ToString("D2")}-000001"; // Jika belum ada nomor transaksi sebelumnya


            }
            else
            {
                int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 6));
                int newNumber = lastNumber + 1;
                newTransactionNumber = $"SO-{currentYear.ToString("D2")}-{newNumber.ToString("D6")}"; // Format nomor transaksi dengan leading zero
            }

            return newTransactionNumber;
        }

        private void gridView1_RowCountChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < gridView1.RowCount; i++)
            {

                gridView1.SetRowCellValue(i, gridView1.Columns["No"], i + 1);
            }
        }

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            try
            {

                // Check if the changed cell is in the "Qty" column
                if (e.Column.FieldName == "Qty")
                {
                    if (e.RowHandle >= 0)
                    {
                        // Get the current row
                        TransactionDataBeli data = (TransactionDataBeli)gridView1.GetRow(e.RowHandle);

                        // Update the Total property of the TransactionData object
                        data.UpdateTotal();

                        // Call the PostEditor method to update the GridView
                        gridView1.PostEditor();
                        // Calculate and update the total price
                        //HitungTotalHarga();
                        gridView1.RefreshData();
                    }
                }
                else if (e.Column.FieldName == "Total")
                {
                    object cellValue = e.Value;

                    if (cellValue != null)
                    {
                        if (!decimal.TryParse(cellValue.ToString(), out decimal total))
                        {
                            gridView1.SetColumnError(e.Column, "Input hanya angka.");
                        }
                        else if (total <= 0)
                        {
                            gridView1.SetColumnError(e.Column, "Total harus positif.");
                        }
                        else
                        {
                            gridView1.SetColumnError(e.Column, null); // Clear error if validation passes
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                barcodeTextBox.Focus();
            }

        }
        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                // Close the editor and exit the edit mode
                gridView1.CloseEditor();
                gridView1.UpdateCurrentRow();
                // Set focus to the GridView control
                barcodeTextBox.Focus();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Delete)
            {
                // Get the selected row from the GridView
                GridView view = gridControl1.FocusedView as GridView;
                int selectedRowHandle = view.FocusedRowHandle;

                // Get the underlying item from the list based on the selected row
                if (selectedRowHandle >= 0 && selectedRowHandle < transactionDataList.Count)
                {
                    TransactionBarangRusak selectedItem = transactionDataList[selectedRowHandle];

                    // Show a confirmation dialog before deleting
                    DialogResult result = MessageBox.Show("Apakah anda yakin akan menghapus baris ini?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Remove the item from the list
                        transactionDataList.RemoveAt(selectedRowHandle);
                    }
                }
                //HitungTotalHarga();
            }
        }

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            if (gridView1.FocusedColumn.FieldName == "Qty" && e.Value != null)
            {
                string qtyString = e.Value.ToString();

                if (qtyString.Length > 5)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty ERROR.";
                    return;
                }

                if (!decimal.TryParse(qtyString, out decimal qty))
                {
                    e.Valid = false;
                    e.ErrorText = "Input hanya angka.";
                    return;
                }

                if (qty < 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty tidak boleh negatif.";
                }
            }
            else if (gridView1.FocusedColumn.FieldName == "Total" && e.Value != null)
            {
                if (!decimal.TryParse(e.Value.ToString(), out decimal jumlah))
                {
                    e.Valid = false;
                    e.ErrorText = "Input hanya angka.";
                    return;
                }

                if (jumlah <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Jumlah harus positif.";
                }
            }
        }

        private void ucPenjualan_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.End)
            {
                bbibayar.PerformClick();
            }
            if (e.KeyCode == Keys.Escape)
            {
                bbibatal.PerformClick();
            }
        }
        private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (transactionDataList.Count == 0) return;
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            NewTransaction();
        }

        private void ucPenjualan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                // Your F5 key press logic here
                // For example, you can refresh the user control or perform any desired action
                blbisimpan.PerformClick();
            }
        }
        int productid;


        private void txtqty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }

        }

        private void txtharga_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }


        private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var tahun = Convert.ToDateTime(detanggal.Text).Year;
            var bulan = Convert.ToDateTime(detanggal.Text).Month;
            var tgl = Convert.ToDateTime(detanggal.Text).Day;
            var periode = Convert.ToInt32(tahun.ToString() + bulan.ToString("00"));

            if (transactionDataList.Count == 0) return;


            // Assuming you have a list of TransactionStockOpname objects
            List<TransactionBarangRusak> barangrusakList = transactionDataList.ToList();

            controller.Insert_BarangRusak(barangrusakList);
            controller.UpdateTransactionNumber(txtnotransaksi.Text);

            NewTransaction();
        }
        private void detanggal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void lookUpEditSupplier_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }

        }

        private void txtpotongan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void txtqtyfisik_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }


        private void ucStockOpname_Load(object sender, EventArgs e)
        {
            NewTransaction();
            gridView1.Columns["Nomor_SO"].Visible = false;
            gridView1.Columns["Tanggal"].Visible = false;
            gridView1.Columns["ProductId"].Visible = false;
            gridView1.Columns["Barcode"].Visible = false;
            gridView1.Columns["No"].Width = 30;
            gridView1.Columns["Kode_Item"].Width = 50;
            gridView1.Columns["ProductName"].Width = 200;
            gridView1.Columns["Keterangan"].Width = 150;

            gridView1.Columns["Hpp"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Hpp"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["Total"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Total"].DisplayFormat.FormatString = "N0";

            gridView1.Columns["No"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Kode_Item"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["ProductName"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Satuan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["QtyFisik"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Hpp"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Total"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Keterangan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;

            gridView1.BeginUpdate();
            foreach (GridColumn column in gridView1.VisibleColumns.Cast<GridColumn>())
            {
                if (column.FieldName == "QtyFisik")
                    column.OptionsColumn.ReadOnly = false;
                else
                    column.OptionsColumn.ReadOnly = true;
            }
            gridView1.EndUpdate();
        }

       

        private void txtketerangan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var newProduct = new TransactionBarangRusak
                {
                    Nomor_SO = txtnotransaksi.Text,
                    Tanggal = Convert.ToDateTime(detanggal.Text),
                    ProductId = productid,
                    Barcode = barcodeTextBox.Text,
                    Kode_Item = txtItemBarang.Text,
                    ProductName = txtnamabarang.Text,
                    Satuan = txtsatuan.Text,
                    QtyFisik = decimal.Parse(txtqtyfisik.Text),
                    Hpp = decimal.Parse(txthpp.Text),
                    Keterangan=txtketerangan.Text
                };
                transactionDataList.Add(newProduct);

                productid = 0;
                barcodeTextBox.Text = string.Empty;
                txtItemBarang.Text = string.Empty;
                txtnamabarang.Text = string.Empty;
                txtsatuan.Text = string.Empty;
                txtqtyfisik.Text = "0";
                txthpp.Text = "0";
                barcodeTextBox.Focus();
            }
        }
    }
}
