using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Penjualan.BusinessLayer;
using Penjualan.DataLayer;
using Penjualan.Model;
using System.ComponentModel;

namespace Penjualan.UC
{
    public partial class ucPenjualan : UserControl
    {
        bool ispending = false;

        private DateTime _bulananDari, _bulananSampai;
        private DateTime _remise1Dari, _remise1Sampai;
        private DateTime _remise2Dari, _remise2Sampai;


        private BindingList<TransactionData> transactionDataList;
        PendingController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static readonly Lazy<ucPenjualan> _instance = new(() => new ucPenjualan());
        public static ucPenjualan Instance => _instance.Value;

        public ucPenjualan()
        {
            InitializeComponent(); 
            transactionDataList = new BindingList<TransactionData>();
            gridControl1.DataSource = transactionDataList;

        }
        DateTime startdate, enddate;
        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string barcode = barcodeTextBox.Text;
                    if (string.IsNullOrEmpty(barcode)) { return; }
                    DTOProductInfo productInfo = POS_Services.RetrieveProductInfo(barcode);                    

                    if (productInfo.ProductId != 0)
                    {
                        if (!AddProductToTransaction(
                            Convert.ToInt32(productInfo.ProductId), barcode,
                            productInfo.KodeItem, productInfo.ProductName,
                            productInfo.Satuan, Convert.ToDecimal(productInfo.Price),
                            Convert.ToDecimal(productInfo.Hpp)))
                            return;
                    }
                    else
                    {
                        using ProductForm productForm = new();
                        productForm.StartPosition = FormStartPosition.CenterScreen;
                        productForm.SetSearchPanelValue(barcodeTextBox.Text);
                        if (productForm.ShowDialog() == DialogResult.OK)
                        {
                            if (!AddProductToTransaction(
                                productForm.ProductId, productForm.Barcode,
                                productForm.Kode_Item, productForm.ProductName,
                                productForm.Satuan, productForm.Price, productForm.Hpp))
                                return;
                        }
                    }
                    HitungTotalHarga();
                    barcodeTextBox.Text = string.Empty;
                    barcodeTextBox.Focus();
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
                    blbibayar.PerformClick();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool AddProductToTransaction(int productid, string barcode, string kodeitem, string productname, string satuan, decimal price, decimal hpp)
        {
            if (LoginInfo.Penjualan_Control_Qty)
            {
                var qty_sistem = POS_Services.GetStocItem(kodeitem, startdate, enddate);
                if (qty_sistem < 1)
                {
                    XtraMessageBox.Show("Barang belum dapat dijual, tidak terdapat stock pada system\nKode Barang : " + kodeitem + "\nNama Barang : " + productname + "\nStok System : " + qty_sistem, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
                }
            }

            if (hpp == 0)
            {
                XtraMessageBox.Show("Barang belum dapat dijual Hpp belum ditentukan\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            if (price <= hpp)
            {
                XtraMessageBox.Show("Harga jual <= dari harga beli\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            var existingProduct = transactionDataList.FirstOrDefault(p => p.ProductId == productid);
            if (existingProduct != null)
            {
                existingProduct.Qty++;
                existingProduct.UpdateTotal();
                HitungNilaiPotongan();
                gridView1.RefreshData();
            }
            else
            {
                var newProduct = new TransactionData
                {
                    ProductId = productid,
                    Barcode = barcode,
                    Kode_Item = kodeitem,
                    ProductName = productname,
                    Satuan = satuan,
                    Hpp = hpp,
                    Price = price,
                    Qty = 1
                };
                newProduct.UpdateTotal();
                transactionDataList.Add(newProduct);
            }
            return true;
        }






        decimal total;
        private void HitungTotalHarga()
        {
            total = transactionDataList.Sum(p => p.Total);
            txttotalpenjualan.Text = "Total: " + total.ToString("C");
        }

        private void ucPenjualan_Load(object sender, EventArgs e)
        {
            NewTransaction();
            // Assuming gridView1 is your GridView instance
            gridView1.Columns["No"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["ProductName"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Satuan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Qty"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Price"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Bruto"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Potongan"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridView1.Columns["Total"].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            int tahun=DateTime.Now.Year;
            int bulan = DateTime.Now.Month;
            int periode = (tahun * 100) + bulan;
            GetTanggalByPeriode(periode);
            CheckPeriodeStatus();
        }

        private void CheckPeriodeStatus()
        {
            var tgl = DateTime.Today;
            var periode = Convert.ToInt32(tgl.Year.ToString() + tgl.Month.ToString("00"));
            int remise = tgl.Day <= 15 ? 1 : 2;

            bool tutup = Tools_Services.GetRemiseStatus(periode, remise);
            MaxPeriodeFinder periodemax = new();
            var maxperiode = periodemax.GetMaxPeriode();

            if (tutup || periode > maxperiode)
            {
                blbibayar.Enabled = false;
                barcodeTextBox.Enabled = false;
                string alasan = tutup ? "Periode telah ditutup" : "Periode belum tersedia";
                XtraMessageBox.Show($"{alasan}, transaksi tidak dapat dilakukan.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GetTanggalByPeriode(int periode)
        {
            var dates = POS_Services.GetTanggalByPeriode(periode);
            if (dates != null)
            {
                _remise1Dari = dates.Remise1Dari;
                _remise1Sampai = dates.Remise1Sampai;
                _remise2Dari = dates.Remise2Dari;
                _remise2Sampai = dates.Remise2Sampai;
                _bulananDari = dates.BulananDari;
                _bulananSampai = dates.BulananSampai;
            }
        }



        private void NewTransaction()
        {
            try
            {
                detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");

                startdate = new(DateTime.Parse(detanggal.Text).Year, 1, 1);
                enddate = DateTime.Parse(detanggal.Text);
                txtjam.Text = DateTime.Now.ToString("HH:mm:ss");


                txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text));
                transactionDataList.Clear();

                txttotalpenjualan.Text = "";
                barcodeTextBox.Focus();
                ispending = false;
            }
             catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string GenerateTransactionNumber(DateTime date)
        {
            return POS_Services.GenerateTransactionNumber(date);
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

                if (e.Column.FieldName == "Qty")
                {
                    if (e.RowHandle >= 0)
                    {
                        TransactionData data = (TransactionData)gridView1.GetRow(e.RowHandle);

                        //jika setting control qty penjualan
                        if (LoginInfo.Penjualan_Control_Qty)
                        {
                            // Get the system stock for the item
                            decimal qtySistem = POS_Services.GetStocItem(data.Kode_Item, startdate, enddate);

                            // Check if the changed quantity exceeds the system stock
                            if (data.Qty > qtySistem)
                            {
                                // Optionally, you can display a message to the user or take other actions.
                                MessageBox.Show("Input Qty Melebihi Stock System! Qty akan diset sesuai nilai maksimal Stock di sistem\n" + "\nKode Barang: " + data.Kode_Item + "\nNama Barang: " + data.ProductName + "\nStok System: " + qtySistem, "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                // Reset the quantity to the maximum allowed value (qtySistem)
                                data.Qty = qtySistem;
                            }
                        }
                        

                        // Update the Total property of the TransactionData object
                        data.UpdateTotal();

                        // Call the PostEditor method to update the GridView
                        gridView1.PostEditor();

                        // Calculate the discount for the current row
                        HitungNilaiPotongan();

                        // Calculate and update the total price
                        HitungTotalHarga();

                        gridView1.RefreshData();
                    }
                }

            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                barcodeTextBox.Focus();
            }

        }

        private void HitungNilaiPotongan()
        {
            int focusedRowHandle = gridView1.FocusedRowHandle;
            if (focusedRowHandle < 0)
                return;

            decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(focusedRowHandle, "Qty"));
            TransactionData data = transactionDataList[focusedRowHandle];

            var potongan = POS_Services.GetPotonganByKodeItem(data.Kode_Item);

            decimal discount = 0;
            if (potongan != null && potongan.MinQty > 0 && qty >= potongan.MinQty)
            {
                discount = qty * potongan.Potongan;
            }

            data.Potongan = discount;
            transactionDataList.ResetItem(focusedRowHandle);
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
                    TransactionData selectedItem = transactionDataList[selectedRowHandle];

                    // Show a confirmation dialog before deleting
                    DialogResult result = MessageBox.Show("Apakah anda yakin akan menghapus baris ini?\nNama Barang :"+ selectedItem.ProductName, "Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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

                if (qty <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty harus lebih dari 0.";
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
        private List<DTOFakturPenjualanDetail> GetItemPenjualanData()
        {
            List<DTOFakturPenjualanDetail> ListItemsPenjualan = new();

            for (int i = 0; i < transactionDataList.Count; i++)
            {
                TransactionData data = transactionDataList[i];
                DTOFakturPenjualanDetail detail = new()
                {
                    BARIS = i + 1,
                    PRODUCT_ID = data.ProductId,
                    KODE_BARANG = data.Kode_Item,
                    BARCODE = data.Barcode,
                    NAMA_BARANG = data.ProductName,
                    SATUAN = data.Satuan,
                    JUMLAH_BARANG = data.Qty,
                    HPP = data.Hpp,
                    HARGA_BARANG = data.Price,
                    BRUTO = data.Qty * data.Price,
                    POTONGAN = data.Potongan,
                    TOTAL_HARGA = data.Total
                };
                ListItemsPenjualan.Add(detail);
            }

            return ListItemsPenjualan;
        }


        private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if(transactionDataList.Count== 0) return;
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
                blbibayar.PerformClick();
            }
        }

        private void blbibayar_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var tahun = Convert.ToDateTime(detanggal.Text).Year;
            var bulan = Convert.ToDateTime(detanggal.Text).Month;
            var tgl = Convert.ToDateTime(detanggal.Text).Day;
            var periode = Convert.ToInt32(tahun.ToString() + bulan.ToString("00"));
            int remise;
            if (tgl <= 15)
            {
                remise = 1;
            }
            else
            {
                remise = 2;
            }
            bool tutup = Tools_Services.GetRemiseStatus(periode, remise);
            MaxPeriodeFinder periodemax = new();
            var maxperiode = periodemax.GetMaxPeriode();
            if (tutup)
            {
                XtraMessageBox.Show("Periode telah ditutup,transaksi tidak dapat dilakukan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (periode > maxperiode)
            {
                XtraMessageBox.Show("Input transaksi dibatasi pada periode yang valid", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            //if (maxperiode - periode >= 2)
            //{
            //    XtraMessageBox.Show("Input transaksi mundur ditolak", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            if (total < 0)
            {
                XtraMessageBox.Show("Total Transaksi Minus", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (transactionDataList.Count == 0) return;
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            //// Get the total amount due from the main form
            var Bruto = transactionDataList.Sum(p => p.Qty * p.Price);
            var Potongan = transactionDataList.Sum(p => p.Potongan);
            var Total = transactionDataList.Sum(p => p.Total);

            DTOFakturPenjualanHeader PenjualanHeader = new()
            {
                NO_TRANSAKSI = txtnotransaksi.Text,
                TANGGAL = Convert.ToDateTime(detanggal.Text),
                JAM = txtjam.Text,
                KASIR = txtkasir.Text,
                BRUTO = Bruto,
                POTONGAN = Potongan,
                TOTAL = Total
            };


            List<DTOFakturPenjualanDetail> itemPenjualanData = GetItemPenjualanData();


            // Tampilkan form untuk memilih produk secara manual
            using PaymentForm paymentForm = new();
            paymentForm.StartPosition = FormStartPosition.CenterScreen;
            // Set the TotalAmount and Items properties
            paymentForm.PendingFaktur = ispending;
            paymentForm.FakturPenjualanHeader = PenjualanHeader;
            paymentForm.ListItemsPenjualan = itemPenjualanData;

            // Set properti tambahan yang dilempar ke form
            paymentForm.BulananDari = _bulananDari;
            paymentForm.BulananSampai = _bulananSampai;
            paymentForm.Remise1Dari = _remise1Dari;
            paymentForm.Remise1Sampai = _remise1Sampai;
            paymentForm.Remise2Dari = _remise2Dari;
            paymentForm.Remise2Sampai = _remise2Sampai;

            if (paymentForm.ShowDialog() == DialogResult.OK)
            {
                NewTransaction();
            }
        }

        private void blbipending_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (transactionDataList.Count == 0)
            {
                return;
            }
            DTOFakturPending FakturPenjualanHeader = new()
            {
                NO_TRANSAKSI = txtnotransaksi.Text,
                TANGGAL = Convert.ToDateTime(detanggal.Text),
                JAM = txtjam.Text,
                KASIR = txtkasir.Text
            };
            //delete if exist
            controller.DeletePendingFaktur(FakturPenjualanHeader.NO_TRANSAKSI);
            //insert
            List<DTODaftarBarangPending> ListItemsPenjualan = GetItemPenjualanDataPending();
            controller.InsertFaktur_Pending(FakturPenjualanHeader, ListItemsPenjualan);
            // Transaction number already reserved atomically in GenerateTransactionNumber
            NewTransaction();
        }

        private List<DTODaftarBarangPending> GetItemPenjualanDataPending()
        {
            List<DTODaftarBarangPending> ListItemsPenjualan = new();

            for (int i = 0; i < transactionDataList.Count; i++)
            {
                TransactionData data = transactionDataList[i];
                DTODaftarBarangPending detail = new()
                {
                    BARIS = i + 1,
                    PRODUCT_ID = data.ProductId,
                    KODE_BARANG = data.Kode_Item,
                    BARCODE = data.Barcode,
                    NAMA_BARANG = data.ProductName,
                    SATUAN = data.Satuan,
                    JUMLAH_BARANG = data.Qty,
                    HPP = data.Hpp,
                    HARGA_BARANG = data.Price,
                    BRUTO = data.Qty * data.Price,
                    POTONGAN = data.Potongan,
                    TOTAL_HARGA = data.Total
                };
                ListItemsPenjualan.Add(detail);
            }

            return ListItemsPenjualan;
        }

        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (transactionDataList.Count > 0)
            {
                DialogResult result = XtraMessageBox.Show("Anda akan mengganti data Penjualan dari Faktur Pending ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    return; // Return and do nothing if user chooses "No"
                }
                transactionDataList.Clear();
                HitungTotalHarga();
                LoadDataFakturPending();
            }
            else
            {
                LoadDataFakturPending();
            }
        }

        private void LoadDataFakturPending()
        {
            // Tampilkan form untuk memilih produk secara manual
            using frmPending PendingForm = new();
            PendingForm.StartPosition = FormStartPosition.CenterScreen;
            if (PendingForm.ShowDialog() == DialogResult.OK)
            {
                string notransaksi = PendingForm.No_Transaksi;
                DateTime tanggal = PendingForm.Tanggal;
                string jam = PendingForm.Jam;

                txtnotransaksi.Text = notransaksi;
                detanggal.Text = tanggal.ToString("dd-MMM-yyyy");
                txtjam.Text = jam;

                var itempending = controller.GetDaftarBarangPending(notransaksi).ToList();               
                foreach (var item in itempending)
                {
                    var newProduct = new TransactionData
                    {
                        ProductId = item.PRODUCT_ID,
                        Barcode = item.BARCODE,
                        Kode_Item = item.KODE_BARANG,
                        ProductName = item.NAMA_BARANG,
                        Satuan = item.SATUAN,
                        Hpp = item.HPP,
                        Price = item.HARGA_BARANG,
                        Qty = item.JUMLAH_BARANG
                    };

                    newProduct.UpdateTotal();
                    transactionDataList.Add(newProduct);
                }
                // Calculate and update the total price
                HitungTotalHarga();
                gridView1.RefreshData();
                ispending = true;
            }
        }
    }
}
