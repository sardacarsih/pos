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
            => PembelianHelper.UpdateRowNumbers(gridView1);

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
            => PembelianHelper.HandleCellValueChanged(gridView1, e, transactionDataList, HitungTotalHarga, barcodeTextBox);

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
            => PembelianHelper.HandleGridKeyDown(e, gridControl1, gridView1, transactionDataList, HitungTotalHarga, barcodeTextBox);

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
            => PembelianHelper.ValidateEditor(gridView1, e);

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
            => PembelianHelper.SubtotalCalc(txtqty, txthargabeli, txtpotongan, txttotal);

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
                TERMIN = int.TryParse(txttermin.Text, out int termin) ? termin : 0,
                USERID = txtuser.Text
            };


            List<DTOFakturPembelianDetail> itemPembelianData = GetItemPembelianData();

            //update faktur pembelian (delete+insert dalam satu transaction)
            controller.UpdateFaktur_Pembelian(PembelianHeader, itemPembelianData);

            XtraMessageBox.Show("Faktur Pembelian telah diupdate", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private List<DTOFakturPembelianDetail> GetItemPembelianData()
            => PembelianHelper.GetItemPembelianData(transactionDataList);
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
                var newProduct = PembelianHelper.ValidateAndCreateProduct(
                    ref productid, barcodeTextBox, txtItemBarang, txtnamabarang,
                    txtsatuan, txtqty, txthargabeli, texthargajual, txtpotongan);
                if (newProduct == null) return;

                transactionDataList.Add(newProduct);
                HitungTotalHarga();
                PembelianHelper.ClearInputFields(barcodeTextBox, txtItemBarang, txtnamabarang,
                    txtsatuan, txtqty, txthargabeli, txtpotongan, texthpplama, texthargajual);
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
            PembelianHelper.DisableColumnSorting(gridView1);
            txtqty.Text = "0";
            txthargabeli.Text = "0";
            txtpotongan.Text = "0";
            texthpplama.Text = "0";
            texthargajual.Text = "0";
            barcodeTextBox.Focus();
        }
    }
}
