using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using System.ComponentModel;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucPenjualan : UserControl
    {
        bool ispending = false;
        bool controlQtySaldo = false;
        DateTime startdate, enddate;
        List<DTOPRODUCTS> Barang;
        private BindingList<TransactionDataJual> transactionDataList;
        PendingController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucPenjualan? _instance;
        public static ucPenjualan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPenjualan();
                return _instance;
            }
        }

        public ucPenjualan()
        {
            InitializeComponent(); 
            transactionDataList = new BindingList<TransactionDataJual>();
            gridControl1.DataSource = transactionDataList;

        }
        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                
                string barcode = barcodeTextBox.Text;

                if (string.IsNullOrEmpty(barcode)) { return; }
                    //int JUMLAH=int.Parse(barcode.Substring(0, barcode.Length-1));
                    var scantransaksi = Barang.Where(t => t.BARCODE == barcode).FirstOrDefault();
                if (scantransaksi != null)
                {
                    if (!AddProductToTransaction(
                        Convert.ToInt32(scantransaksi.PRODUCTID), barcode,
                        scantransaksi.KODE_ITEM, scantransaksi.PRODUCTNAME,
                        scantransaksi.SATUAN, Convert.ToDecimal(scantransaksi.PRICE),
                        Convert.ToDecimal(scantransaksi.BELI)))
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
        private bool AddProductToTransaction(int productid, string barcode, string kodeitem, string productname, string satuan, decimal price, decimal hpp)
        {
            if (controlQtySaldo)
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
                var newProduct = new TransactionDataJual
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
            controlQtySaldo = POS_Services.GetSettingKontrol_qty_Saldo();
            Load_RefreshData();
            NewTransaction();
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

        private void Load_RefreshData()
        {
            Barang = DaftarBarang();
        }

        private static List<DTOPRODUCTS> DaftarBarang()
         {
            string query = "SELECT * FROM pos_product WHERE AKTIF='Y' ORDER BY productname";

            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            List<DTOPRODUCTS> productList = connection.Query<DTOPRODUCTS>(query).ToList();

            return productList;
        }


    private void NewTransaction()
        {
            detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");

            startdate = new(DateTime.Parse(detanggal.Text).Year, 1, 1);
            enddate = DateTime.Parse(detanggal.Text);

            txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text));
            transactionDataList.Clear();

            txttotalpenjualan.Text ="";
            barcodeTextBox.Focus();
            ispending=false;
        }

        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'PENJUALAN' AND nomor LIKE 'W-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand selectCommand = new(selectQuery, connection);
            string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

            // Buat nomor transaksi baru untuk tahun saat ini
            string newTransactionNumber;
            if (string.IsNullOrEmpty(lastTransactionNumber))
            {
                newTransactionNumber = $"W-{currentYear.ToString("D2")}-000001"; // Jika belum ada nomor transaksi sebelumnya


            }
            else
            {
                int lastNumber = lastTransactionNumber.Length >= 6
                    ? int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 6))
                    : 0;
                int newNumber = lastNumber + 1;
                newTransactionNumber = $"W-{currentYear.ToString("D2")}-{newNumber.ToString("D6")}";
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
                        // Calculate the discount for the current row
                        HitungNilaiPotongan();
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
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update quantity in sales grid");
            }
            finally
            {
                barcodeTextBox.Focus();
            }

        }

        private void HitungNilaiPotongan()
        {
            // Get the index of the focused row
            int focusedRowHandle = gridView1.FocusedRowHandle;
            if (focusedRowHandle < 0)
                return;

            // Get the value of the "Qty" column for the current row
            decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(focusedRowHandle, "Qty"));

            // Get the current TransactionData object from the transactionDataList
            TransactionDataJual data = transactionDataList[focusedRowHandle];

            // Query the database for discounts based on quantity
            string kodebrg = data.Kode_Item;
            string query = "SELECT MINQTY, POTONGAN FROM POS_POTONGANBERDASARKANQTY WHERE KODE_ITEM = :kodebrg";

            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand command = new(query, connection);
            command.Parameters.Add("kodebrg", OracleDbType.Varchar2).Value = kodebrg;
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                decimal MINQTY = reader.GetDecimal(0);
                decimal POTONGAN = reader.GetDecimal(1);

                decimal discount;
                if (MINQTY <= 0 || qty < MINQTY)
                {
                    discount = 0;
                }
                else
                {
                    discount = qty * POTONGAN;
                }

                // Update the TransactionData object with the calculated discount
                data.Potongan = discount;

                // Update the corresponding row in the transactionDataList
                transactionDataList.ResetItem(focusedRowHandle);
            }
            else
            {
                // No discount applies, set the discount value to 0
                data.Potongan = 0;

                // Update the corresponding row in the transactionDataList
                transactionDataList.ResetItem(focusedRowHandle);
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
                    TransactionDataJual selectedItem = transactionDataList[selectedRowHandle];

                    // Show a confirmation dialog before deleting
                    DialogResult result = MessageBox.Show("Are you sure you want to delete this row?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                TransactionDataJual data = transactionDataList[i];
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
            if (maxperiode - periode >= 2)
            {
                XtraMessageBox.Show("Input transaksi mundur ditolak", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (total<0)
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
            POS_Services.UpdateTransactionNumber(FakturPenjualanHeader.NO_TRANSAKSI);
            NewTransaction();
        }

        private List<DTODaftarBarangPending> GetItemPenjualanDataPending()
        {
            List<DTODaftarBarangPending> ListItemsPenjualan = new();

            for (int i = 0; i < transactionDataList.Count; i++)
            {
                TransactionDataJual data = transactionDataList[i];
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
                    var newProduct = new TransactionDataJual
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

        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Load_RefreshData();
        }
    }
}
