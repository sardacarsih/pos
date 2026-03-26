using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;

namespace BackOffice.UC
{
    public partial class ucStockOpname : UserControl
    {
        private BindingList<TransactionStockOpname> transactionDataList;
        StokOpnameController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucStockOpname? _instance;
        public static ucStockOpname Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucStockOpname();
                return _instance;
            }
        }

        public ucStockOpname()
        {
            InitializeComponent();
            transactionDataList = new BindingList<TransactionStockOpname>();
            gridControl1.DataSource = transactionDataList;
        }
        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!DateTime.TryParse(detanggal.Text, out DateTime selectedDate))
                {
                    XtraMessageBox.Show("Tanggal tidak valid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var startdate = new DateTime(selectedDate.Year, 1, 1);
                var enddate = selectedDate;
                string kodeitem = string.Empty;
                string barcode = barcodeTextBox.Text.Trim();

                if (string.IsNullOrEmpty(barcode))
                {
                    XtraMessageBox.Show("Barcode tidak boleh kosong.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DTOProductInfo productInfo;
                try
                {
                    productInfo = POS_Services.RetrieveProductInfo(barcode);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Terjadi kesalahan saat mengambil informasi produk: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (productInfo?.ProductId != 0)
                {
                    productid = productInfo.ProductId;
                    kodeitem = productInfo.KodeItem; 

                    txtItemBarang.Text = kodeitem;
                    txtnamabarang.Text = productInfo.ProductName;
                    txtsatuan.Text = productInfo.Satuan;
                    txthpp.Text = productInfo.Hpp.ToString("F2");
                }
                else
                {
                    using ProductForm productForm = new()
                    {
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    productForm.SetSearchPanelValue(barcode);

                    if (productForm.ShowDialog() == DialogResult.OK)
                    {
                        productid = productForm.ProductId;
                        kodeitem = productForm.Kode_Item; 
                        barcodeTextBox.Text = productForm.Barcode;
                        txtItemBarang.Text = kodeitem;
                        txtnamabarang.Text = productForm.ProductName;
                        txtsatuan.Text = productForm.Satuan;
                        txthpp.Text = productForm.Hpp.ToString("F2");
                    }
                }

                try
                {
                    var qty_sistem = controller.GetStockData(kodeitem, startdate, enddate);
                    txtqtysystem.Text = qty_sistem?.StockAkhir.ToString("F2") ?? "0";
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Terjadi kesalahan saat mengambil data stok: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtqtysystem.Text = "0";
                }

                txtqtyfisik.Focus();
            }
            else if (e.KeyCode == Keys.F2)
            {
                gridView1.Focus();

                int lastIndex = gridView1.RowCount - 1;
                if (lastIndex >= 0)
                {
                    gridView1.FocusedRowHandle = lastIndex;
                    gridView1.FocusedColumn = gridView1.Columns["QtyFisik"];
                    gridView1.ShowEditor();
                }
            }
            else if (e.KeyCode == Keys.F5)
            {
                blbisimpan.PerformClick();
            }
        }


        private void NewTransaction()
        {
            detanggal.Text = DateTime.Today.ToString("yyyy-MM-dd");

            if (DateTime.TryParse(detanggal.Text, out DateTime parsedDate))
            {
                txtnotransaksi.Text = GenerateTransactionNumber(parsedDate);
            }
            transactionDataList.Clear();
            barcodeTextBox.Focus();
            txtqtysystem.Text = "0";
            txtqtyfisik.Text = "0";
            txthpp.Text = "0";
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
                if (e.Column.FieldName == "QtyFisik")
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
                            gridView1.SetColumnError(e.Column, "qty harus positif.");
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
                    TransactionStockOpname selectedItem = transactionDataList[selectedRowHandle];

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
            if (gridView1.FocusedColumn.FieldName == "QtyFisik" && e.Value != null)
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

        private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (transactionDataList.Count == 0) return;
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            isButtonClickHandled = false;
            detanggal.Enabled = true;
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
            isButtonClickHandled = false;
            if (transactionDataList.Count == 0) return;


            // Assuming you have a list of TransactionStockOpname objects
            List<TransactionStockOpname> stockOpnameList = transactionDataList.ToList();

            controller.Insert_StockOpname(stockOpnameList);
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
                if (decimal.TryParse(txtqtysystem.Text, out decimal qtySystem) &&
                    decimal.TryParse(txtqtyfisik.Text, out decimal qtyFisik) &&
                    decimal.TryParse(txthpp.Text, out decimal hpp))
                {
                    // Cek apakah ProductId sudah ada di transactionDataList
                    var existingProduct = transactionDataList.FirstOrDefault(p => p.ProductId == productid);

                    if (existingProduct != null)
                    {
                        // Jika ProductId sudah ada, perbarui QtyFisik
                        existingProduct.QtyFisik += qtyFisik;
                    }
                    else
                    {
                        // Jika ProductId belum ada, tambahkan produk baru
                        var newProduct = new TransactionStockOpname
                        {
                            Nomor_SO = txtnotransaksi.Text,
                            Tanggal = DateTime.TryParse(detanggal.Text, out DateTime tanggal) ? tanggal : DateTime.Now,
                            ProductId = productid,
                            Barcode = barcodeTextBox.Text,
                            Kode_Item = txtItemBarang.Text,
                            ProductName = txtnamabarang.Text,
                            Satuan = txtsatuan.Text,
                            QtySystem = qtySystem,
                            QtyFisik = qtyFisik,
                            Hpp = hpp,
                        };
                        transactionDataList.Add(newProduct);
                    }

                    ResetInputFields();
                }
                else
                {
                    MessageBox.Show("Input tidak valid. Pastikan semua data angka terisi dengan benar.", "Kesalahan Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ResetInputFields()
        {
            productid = 0;
            barcodeTextBox.Text = string.Empty;
            txtItemBarang.Text = string.Empty;
            txtnamabarang.Text = string.Empty;
            txtsatuan.Text = string.Empty;
            txtqtysystem.Text = "0";
            txtqtyfisik.Text = "0";
            txthpp.Text = "0";
            barcodeTextBox.Focus();
        }



        private void ucStockOpname_Load(object sender, EventArgs e)
        {
            NewTransaction();
            gridView1.Columns["Nomor_SO"].Visible = false;
            gridView1.Columns["Tanggal"].Visible = false;
            gridView1.Columns["ProductId"].Visible = false;
            gridView1.Columns["Barcode"].Visible = false;
            gridView1.Columns["No"].Width = 50;
            gridView1.Columns["ProductName"].Width = 300;

            gridView1.Columns["No"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Kode_Item"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["ProductName"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Satuan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["QtySystem"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["QtyFisik"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Selisih"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Hpp"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Total"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Hpp"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Hpp"].DisplayFormat.FormatString = "n0";
            gridView1.Columns["Total"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Total"].DisplayFormat.FormatString = "n0";


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

        private object oldDetanggalValue;
        private void detanggal_EditValueChanging(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {
            //// Save the old value before the change
            //oldDetanggalValue = detanggal.EditValue;

            //if (transactionDataList.Count > 0)
            //{
            //    e.Cancel = true; // Cancelling the change
            //    XtraMessageBox.Show("Tanggal tidak dapat diubah, Transaksi sudah ada", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //    // Optionally, you can revert to the old value
            //    detanggal.EditValue = oldDetanggalValue;
            //}
        }

        private void detanggal_EditValueChanged(object sender, EventArgs e)
        {
            if (DateTime.TryParse(detanggal.Text, out DateTime selectedDate))
            {
                txtnotransaksi.Text = GenerateTransactionNumber(selectedDate);
            }
        }
        List<DTOStockData> persediaan;
        private bool isButtonClickHandled = false; // Add this variable at the class level
        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            // Check if the button click has already been handled
            if (isButtonClickHandled)
            {
                XtraMessageBox.Show("Data Import untuk saldo system minus namun tidak memiliki saldo fisik sudah diproses", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                detanggal.Enabled = false;
                return;
            }

            SplashScreenManager.ShowForm(typeof(WaitForm1), true, true);

            try
            {

                if (DateTime.Parse(detanggal.Text).ToString("dd-MM") == "31-12")
                {
                    // Set the flag to indicate that the button click has been handled
                    isButtonClickHandled = true;
                    var startdate = new DateTime(DateTime.Parse(detanggal.Text).Year, 1, 1);
                    var enddate = new DateTime(DateTime.Parse(detanggal.Text).Year, 12, 31);
                    persediaan = controller.GetProductStockInfo(startdate, enddate);
                    var saldominus = persediaan.Where(saldo => saldo.STOCK_AKHIR < 0).ToList();

                    if (saldominus.Any())
                    {
                        foreach (var stockopname in saldominus)
                        {
                            var newProduct = new TransactionStockOpname
                            {
                                Nomor_SO = txtnotransaksi.Text,
                                Tanggal = Convert.ToDateTime(detanggal.Text),
                                ProductId = 0,
                                Barcode = "",
                                Kode_Item = stockopname.KODE_ITEM,
                                ProductName = stockopname.PRODUCTNAME,
                                Satuan = stockopname.SATUAN,
                                QtySystem = stockopname.STOCK_AKHIR,
                                QtyFisik = 0,
                                Hpp = stockopname.TOTAL_COST_AVG,
                            };
                            transactionDataList.Add(newProduct);
                        }
                    }
                    else
                    {
                        XtraMessageBox.Show("Data Saldo Minus tidak ditemukan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions specific to your application
                XtraMessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the wait form
                SplashScreenManager.CloseForm(false);
            }

        }

        private void barLargeButtonItem4_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            // Check if the button click has already been handled
            if (isButtonClickHandled)
            {
                XtraMessageBox.Show("Data Import untuk saldo system plus namun tidak memiliki saldo fisik sudah diproses", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                detanggal.Enabled = false;
                return;
            }

            SplashScreenManager.ShowForm(typeof(WaitForm1), true, true);

            try
            {

                if (DateTime.Parse(detanggal.Text).ToString("dd-MM") == "31-12")
                {
                    // Set the flag to indicate that the button click has been handled
                    isButtonClickHandled = true;
                    var startdate = new DateTime(DateTime.Parse(detanggal.Text).Year, 1, 1);
                    var enddate = new DateTime(DateTime.Parse(detanggal.Text).Year, 12, 31);
                    persediaan = controller.GetProductStockInfo(startdate, enddate);
                    var saldominus = persediaan.Where(saldo => saldo.STOCK_OPNAME == 0).ToList();

                    if (saldominus.Any())
                    {
                        foreach (var stockopname in saldominus)
                        {
                            var newProduct = new TransactionStockOpname
                            {
                                Nomor_SO = txtnotransaksi.Text,
                                Tanggal = Convert.ToDateTime(detanggal.Text),
                                ProductId = 0,
                                Barcode = "",
                                Kode_Item = stockopname.KODE_ITEM,
                                ProductName = stockopname.PRODUCTNAME,
                                Satuan = stockopname.SATUAN,
                                QtySystem = stockopname.STOCK_AKHIR,
                                QtyFisik = 0,
                                Hpp = stockopname.TOTAL_COST_AVG,
                            };
                            transactionDataList.Add(newProduct);
                        }
                    }
                    else
                    {
                        XtraMessageBox.Show("Semua Barang Sudah dilakukan Stock Opname", "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions specific to your application
                XtraMessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the wait form
                SplashScreenManager.CloseForm(false);
            }
        }
         
    }
}
