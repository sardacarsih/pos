using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.Model;
using BackOffice.View;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System.ComponentModel;

namespace BackOffice.UC
{
    public partial class ucPembelianEdit : UserControl
    {
        List<DTOSupplier> supplier;
        private BindingList<TransactionDataBeli> transactionDataList;
        PembelianController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucPembelianEdit? _instance;
        public static ucPembelianEdit Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPembelianEdit();
                return _instance;
            }
        }

        public ucPembelianEdit()
        {
            InitializeComponent(); 
            transactionDataList = new BindingList<TransactionDataBeli>();
            gridControl1.DataSource = transactionDataList;

        }
        public void SetPembelian(DTOFakturPembelian_Header PembelianHeader)
        {
            Load_Supplier();
            // Set the data from PenjualanHeader to the controls in ucPenjualanEdit
            // Example:
            txtnotransaksi.Text = PembelianHeader.NO_TRANSAKSI;
            detanggal.EditValue = Convert.ToDateTime(PembelianHeader.TANGGAL.ToString("dd-MMM-yyyy"));
            txttermin.Text = PembelianHeader.TERMIN.ToString();

            // Set the default value for searchLookUpEdit1
            int index = supplier.FindIndex(item => item.KODE == PembelianHeader.SUPPLIER_ID);
            if (index != -1)
            {
                searchLookUpEditSupplier.EditValue = supplier[index].KODE;
            }

            // Set other controls as needed
        }
        public void SetItemPembelianDetail(List<DTODaftarBarang> itemPembelianData)
        {
            // Process and display the itemPenjualanData in ucPenjualanEdit
            // Example:
            foreach (var item in itemPembelianData)
            {
                var newProduct = new TransactionDataBeli
                {
                    ProductId = item.PRODUCTID,
                    Barcode = item.BARCODE,
                    Kode_Item = item.KODE_BARANG,
                    ProductName = item.NAMA_BARANG,
                    Satuan = item.SATUAN,
                    Hpp = item.HPP,
                    Price = item.HARGA_BARANG,
                    Qty = item.JUMLAH_BARANG,
                    Bruto = item.BRUTO,
                    Potongan = item.POTONGAN,
                    Total = item.TOTAL_HARGA
                };
                //newProduct.UpdateTotal();
                transactionDataList.Add(newProduct);
            }
            gridControl1.DataSource = transactionDataList;
            barcodeTextBox.Focus();
        }
        private void Load_Supplier()
        {
            supplier = controller.GetSuppliers();
            searchLookUpEditSupplier.Properties.DataSource = supplier;
            searchLookUpEditSupplier.Properties.ValueMember = "KODE";
            searchLookUpEditSupplier.Properties.DisplayMember = "NAMA";
        }

        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string barcode = barcodeTextBox.Text;
                if (string.IsNullOrEmpty(barcode)) { return; }
                DTOProductInfo productInfo = POS_Services.RetrieveProductInfo(barcode);

                if (productInfo.ProductId != 0)
                {
                    int productid = Convert.ToInt32(productInfo.ProductId);
                    string kodeitem = productInfo.KodeItem.ToString();
                    string productname = productInfo.ProductName.ToString();
                    string satuan = productInfo.Satuan.ToString();
                    decimal price = Convert.ToDecimal(productInfo.Price);
                    decimal hpp = Convert.ToDecimal(productInfo.Hpp);
                    txtItemBarang.Text = kodeitem;
                    txtnamabarang.Text = productname;
                    txtsatuan.Text=satuan;
                    texthpplama.Text = hpp.ToString();
                    texthargajual.Text = price.ToString();
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
                        string kodeitem = productForm.Kode_Item;
                        string barcodefromx = productForm.Barcode;
                        string productname = productForm.ProductName;
                        string satuan = productForm.Satuan;
                        decimal hpp = productForm.Hpp != null ? productForm.Hpp : 0m;
                        decimal hjual = productForm.Price != null ? productForm.Price : 0m;
                        
                       
                           barcodeTextBox.Text = barcodefromx;
                            txtItemBarang.Text = kodeitem;
                            txtnamabarang.Text = productname;
                            txtsatuan.Text = satuan;
                        texthpplama.Text = hpp.ToString();
                        texthargajual.Text = hjual.ToString();
                    }
                   
                }
                txtqty.Focus();
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
        decimal total;
        private void HitungTotalHarga()
        {
            total = transactionDataList.Sum(p => p.Total);
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
                        HitungTotalHarga();
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
                    TransactionDataBeli selectedItem = transactionDataList[selectedRowHandle];

                    // Show a confirmation dialog before deleting
                    DialogResult result = MessageBox.Show("Apakah anda yakin akan menghapus baris ini?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Remove the item from the list
                        transactionDataList.RemoveAt(selectedRowHandle);
                    }
                }
                HitungTotalHarga();
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

        private void ucPembelianEdit_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
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
            if(transactionDataList.Count== 0) return;
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
        }

        private void ucPembelianEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                // Your F5 key press logic here
                // For example, you can refresh the user control or perform any desired action
                blbisimpan.PerformClick();
            }
        }
        int productid;
        private void Subtotal_calc()
        {
            var qty = decimal.Parse(txtqty.Text);
            var harga = decimal.Parse(txthargabeli.Text);
            var potongan = decimal.Parse(txtpotongan.Text);
            var total = (qty * harga) - potongan;
            txttotal.Text = total.ToString();
        }

        private void txtqty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                Subtotal_calc();
            }

        }

        private void txtharga_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                Subtotal_calc();
            }
        }


        private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var tahun = Convert.ToDateTime(detanggal.Text).Year;
            var bulan = Convert.ToDateTime(detanggal.Text).Month;
            var tgl = Convert.ToDateTime(detanggal.Text).Day;
            var periode = Convert.ToInt32(tahun.ToString() + bulan.ToString("00"));   

            if (transactionDataList.Count == 0) return;
            if (searchLookUpEditSupplier.EditValue ==null)
            {
                XtraMessageBox.Show("Supplier harus diisi", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (total < 0)
            {
                XtraMessageBox.Show("Total Transaksi Minus", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //// Get the total amount due from the main form
            var Bruto = transactionDataList.Sum(p => p.Qty * p.Hpp);
            var Potongan = transactionDataList.Sum(p => p.Potongan);
            var Total = transactionDataList.Sum(p => p.Total);

            DTOFakturPembelian_Header PembelianHeader = new()
            {
                NO_TRANSAKSI = txtnotransaksi.Text,
                TANGGAL = Convert.ToDateTime(detanggal.Text),
                SUPPLIER_ID = searchLookUpEditSupplier.EditValue.ToString(),
                BRUTO = Bruto,
                POTONGAN = Potongan,
                TOTAL = Total,
                TERMIN=int.Parse(txttermin.Text),
                USERID = txtuser.Text
            };


            List<DTOFakturPembelianDetail> itemPembelianData = GetItemPembelianData();

            //menghapus faktur pembelian,detail pembelian dan daftar angsuran penjualan kredit
            controller.HapusPembelian(PembelianHeader.NO_TRANSAKSI);

            //insert pembelian dan set hpp dan harga jual pada product
            controller.InsertFaktur_Pembelian(PembelianHeader, itemPembelianData);

            XtraMessageBox.Show("Faktur Pembelian telah diupdate", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private List<DTOFakturPembelianDetail> GetItemPembelianData()
        {
            List<DTOFakturPembelianDetail> ListItemsPembelian = new();

            for (int i = 0; i < transactionDataList.Count; i++)
            {
                TransactionDataBeli data = transactionDataList[i];
                DTOFakturPembelianDetail detail = new()
                {
                    BARIS = i + 1,
                    PRODUCT_ID = data.ProductId,
                    KODE_BARANG = data.Kode_Item,
                    NAMA_BARANG = data.ProductName,
                    SATUAN = data.Satuan,
                    QUANTITY = data.Qty,
                    HARGA_BELI = data.Hpp,
                    HARGA_JUAL = data.Price,
                    BRUTO = data.Qty * data.Hpp,
                    POTONGAN = data.Potongan,
                    TOTAL = data.Total
                };
                ListItemsPembelian.Add(detail);
            }
            return ListItemsPembelian;
        }
        private void detanggal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void searchLookUpEditSupplier_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }

        }

        private void texthargajual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (decimal.Parse(txtqty.Text) == 0 || decimal.Parse(txtqty.Text) < 0 || string.IsNullOrEmpty(txtItemBarang.Text))
                {
                    XtraMessageBox.Show("Qty dan harga harus diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (decimal.Parse(texthargajual.Text) <= decimal.Parse(txthargabeli.Text))
                {
                    XtraMessageBox.Show("Harga Jual <=  Harga Pokok Pembelian", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var newProduct = new TransactionDataBeli
                {
                    ProductId = productid,
                    Barcode = barcodeTextBox.Text,
                    Kode_Item = txtItemBarang.Text,
                    ProductName = txtnamabarang.Text,
                    Satuan = txtsatuan.Text,
                    Qty = decimal.Parse(txtqty.Text),
                    Hpp=decimal.Parse(txthargabeli.Text),
                    Price = decimal.Parse(texthargajual.Text),
                    Potongan = decimal.Parse(txtpotongan.Text),
                };
                newProduct.UpdateTotal();
                transactionDataList.Add(newProduct);
                HitungTotalHarga();
                productid = 0;
                barcodeTextBox.Text = string.Empty;
                txtItemBarang.Text = string.Empty;
                txtnamabarang.Text = string.Empty;
                txtsatuan.Text = string.Empty;
                txtqty.Text = "0";
                txthargabeli.Text = "0";
                txtpotongan.Text = "0";
                texthpplama.Text = "0";
                texthargajual.Text = "0";
                barcodeTextBox.Focus();
            }
        }

        private void txtpotongan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                Subtotal_calc();
            }
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Find the parent form of the user control
            frmEditFakturPembelian parentForm = (frmEditFakturPembelian)FindForm();

            // Check if the parent form was found and if it is a valid form
            if (parentForm != null && parentForm is Form)
            {
                // Close the parent form
                parentForm.Close();
            }
        }

        private void ucPembelianEdit_Load(object sender, EventArgs e)
        {
            gridView1.Columns["No"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["ProductName"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Satuan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Qty"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Hpp"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Bruto"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Potongan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Total"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            txtqty.Text = "0";
            txthargabeli.Text = "0";
            txtpotongan.Text = "0";
            texthpplama.Text = "0";
            texthargajual.Text = "0";
            barcodeTextBox.Focus();
        }
    }
}
