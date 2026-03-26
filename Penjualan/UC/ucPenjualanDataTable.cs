using Dapper;
using DevExpress.Utils.About;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using Penjualan.BusinessLayer;
using Penjualan.DataLayer;
using Penjualan.Model;
using System.Data;

namespace Penjualan.UC
{
    public partial class ucPenjualanDataTable : UserControl
    {
        List<DTOPRODUCTS> Barang;
        //Using singleton pattern to create an instance to ucModule3
        private static ucPenjualanDataTable? _instance;
        public static ucPenjualanDataTable Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPenjualanDataTable();
                return _instance;
            }
        }
        private readonly DataTable dataTable;

        public ucPenjualanDataTable()
        {
            InitializeComponent();
            this.PreviewKeyDown += ucPenjualanDataTable_Key;
        }

        private void ucPenjualanDataTable_Key(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.B)
            {
                blbibayar.PerformClick();

            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.P)
            {
                MessageBox.Show("ctrl P");

            }
        }


        private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string barcode = barcodeTextBox.Text;
                var scantransaksi = Barang.Where(t => t.BARCODE == barcode).FirstOrDefault();
                if (scantransaksi != null)
                {
                    int productid = Convert.ToInt32(scantransaksi.PRODUCTID);
                    string kodeitem = scantransaksi.KODE_ITEM.ToString();
                    string productname = scantransaksi.PRODUCTNAME.ToString();
                    string satuan = scantransaksi.SATUAN.ToString();
                    decimal price = Convert.ToDecimal(scantransaksi.PRICE);
                    decimal hpp = Convert.ToDecimal(scantransaksi.BELI);
                    // Check if price is less than or equal to hpp
                    if (price <= hpp)
                    {
                        // Price is less than or equal to hpp
                        // Rest of the code goes here...
                        // ...
                        XtraMessageBox.Show("Harga jual <= dari harga beli\n"+ kodeitem+" "+ productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop); return;

                    }
                    // Cari produk di datagrid
                    int rowHandle = gridView1.LocateByValue("Barcode", barcode);
                    if (rowHandle != DevExpress.XtraGrid.GridControl.InvalidRowHandle)
                    {
                        // Jika produk sudah ada di datagrid, tambahkan qty dan update total harga
                        decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Qty"));
                        qty++;
                        gridView1.SetRowCellValue(rowHandle, "Qty", qty);
                        decimal potongan = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Potongan"));
                        decimal harga = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Price"));
                        decimal subtotal = (qty * harga) - potongan;
                        gridView1.SetRowCellValue(rowHandle, "Total", subtotal);
                        gridView1.UpdateCurrentRow();
                    }
                    else
                    {
                        // Jika produk belum ada di datagrid, tambahkan produk baru
                        gridView1.AddNewRow();
                        int newRowHandle = gridView1.GetRowHandle(gridView1.DataRowCount);
                        gridView1.SetRowCellValue(newRowHandle, "Kode_Item", kodeitem);
                        gridView1.SetRowCellValue(newRowHandle, "ProductId", productid);
                        gridView1.SetRowCellValue(newRowHandle, "ProductName", productname);
                        gridView1.SetRowCellValue(newRowHandle, "Satuan", satuan);
                        gridView1.SetRowCellValue(newRowHandle, "Barcode", barcode);
                        gridView1.SetRowCellValue(newRowHandle, "Hpp", hpp);
                        gridView1.SetRowCellValue(newRowHandle, "Price", price);
                        gridView1.SetRowCellValue(newRowHandle, "Qty", 1);
                        gridView1.SetRowCellValue(newRowHandle, "Potongan", 0);
                        decimal subtotal = (1 * price) - Convert.ToDecimal(gridView1.GetRowCellValue(newRowHandle, "Potongan"));
                        gridView1.SetRowCellValue(newRowHandle, "Total", subtotal);
                        gridView1.UpdateCurrentRow();


                    }

                    // Hitung total harga
                    decimal total = 0;
                    for (int i = 0; i < gridView1.DataRowCount; i++)
                    {
                        decimal pricePerItem = Convert.ToDecimal(gridView1.GetRowCellValue(i, "Price"));
                        decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(i, "Qty"));
                        total += pricePerItem * qty;
                    }
                    txttotalpenjualan.Text = "Total: " + total.ToString("C");

                }
                else
                {
                    // Tampilkan form untuk memilih produk secara manual
                    using ProductForm productForm = new();
                    productForm.StartPosition = FormStartPosition.CenterScreen;                 
                    productForm.SetSearchPanelValue(barcodeTextBox.Text);
                    if (productForm.ShowDialog() == DialogResult.OK)
                    {
                        int productid = productForm.ProductId;
                        string kodeitem = productForm.Kode_Item;
                        string barcodefromx = productForm.Barcode;
                        string productname = productForm.ProductName;
                        string satuan = productForm.Satuan;
                        decimal price = productForm.Price;
                        decimal hpp = productForm.Hpp;
                        // Check if price is less than or equal to hpp
                        if (price <= hpp)
                        {
                            // Price is less than or equal to hpp
                            // Rest of the code goes here...
                            // ...
                            XtraMessageBox.Show("Harga jual <= dari harga beli\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop); return;

                        }
                        // Cari produk di datagrid
                        int rowHandle = gridView1.LocateByValue("Kode_Item", kodeitem);
                        if (rowHandle != DevExpress.XtraGrid.GridControl.InvalidRowHandle)
                        {
                            // Jika produk sudah ada di datagrid, tambahkan qty dan update total harga
                            decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Qty"));
                            qty++;
                            gridView1.SetRowCellValue(rowHandle, "Qty", qty);
                            decimal potongan = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Potongan"));
                            decimal harga = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Price"));
                            decimal bruto = qty * harga;
                            gridView1.SetRowCellValue(rowHandle, "Bruto", bruto);
                            decimal subtotal = (qty * harga) - potongan;
                            gridView1.SetRowCellValue(rowHandle, "Total", subtotal);
                            gridView1.UpdateCurrentRow();
                        }
                        else
                        {
                            // Jika produk belum ada di datagrid, tambahkan produk baru
                            gridView1.AddNewRow();
                            int newRowHandle = gridView1.GetRowHandle(gridView1.DataRowCount);
                            gridView1.SetRowCellValue(newRowHandle, "Kode_Item", kodeitem);
                            gridView1.SetRowCellValue(newRowHandle, "Barcode", barcodefromx);
                            gridView1.SetRowCellValue(newRowHandle, "ProductId", productid);
                            gridView1.SetRowCellValue(newRowHandle, "ProductName", productname);
                            gridView1.SetRowCellValue(newRowHandle, "Satuan", satuan);
                            gridView1.SetRowCellValue(newRowHandle, "Hpp", hpp);
                            gridView1.SetRowCellValue(newRowHandle, "Price", price);
                            gridView1.SetRowCellValue(newRowHandle, "Qty", 1);
                            decimal bruto = 1 * price;
                            gridView1.SetRowCellValue(newRowHandle, "Bruto", bruto);
                            gridView1.SetRowCellValue(newRowHandle, "Potongan", 0);
                            decimal subtotal = (1 * price) - Convert.ToDecimal(gridView1.GetRowCellValue(newRowHandle, "Potongan"));
                            gridView1.SetRowCellValue(newRowHandle, "Total", subtotal);
                            gridView1.UpdateCurrentRow();
                            gridView1.RefreshData();

                        }

                        // Hitung total harga
                        HitungTotalHarga();
                    }
                }
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
        }
        private void HitungTotalHarga()
        {
            decimal total = 0;
            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                decimal pricePerItem = Convert.ToDecimal(gridView1.GetRowCellValue(i, "Price"));
                decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(i, "Qty"));
                decimal potongan = Convert.ToDecimal(gridView1.GetRowCellValue(i, "Potongan"));
                total += (pricePerItem * qty)- potongan;
            }
            txttotalpenjualan.Text = "Total: " + total.ToString("C");
        }

        private void ucPenjualan_Load(object sender, EventArgs e)
        {
            Barang = DaftarBarang();
            NewTransaction();
        }
        private static List<DTOPRODUCTS> DaftarBarang()
         {
            string query = "SELECT * FROM pos_product ORDER BY productname";

            using OracleConnection connection = new(Global.connectionString);
            connection.Open();

            List<DTOPRODUCTS> productList = connection.Query<DTOPRODUCTS>(query).ToList();

            return productList;
        }


    private void NewTransaction()
        {
            detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");

            txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text)) ;
           
            gridControl1.DataSource = GetTransactionData();
            txttotalpenjualan.Text ="";
            barcodeTextBox.Focus();
        }

        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'PENJUALAN' AND nomor LIKE 'F-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using (OracleConnection connection = new OracleConnection(Global.connectionString))
            {
                connection.Open();
                using (OracleCommand selectCommand = new OracleCommand(selectQuery, connection))
                {
                    string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

                    // Buat nomor transaksi baru untuk tahun saat ini
                    string newTransactionNumber;
                    if (string.IsNullOrEmpty(lastTransactionNumber))
                    {
                        newTransactionNumber = $"F-{currentYear.ToString("D2")}-000001"; // Jika belum ada nomor transaksi sebelumnya

                       
                    }
                    else
                    {
                        int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 6));
                        int newNumber = lastNumber + 1;
                        newTransactionNumber = $"F-{currentYear.ToString("D2")}-{newNumber.ToString("D6")}"; // Format nomor transaksi dengan leading zero
                    }

                    return newTransactionNumber;
                }
            }
        }









        private DataTable GetTransactionData()
        {
            // Membuat tabel data transaksi dengan kolom-kolom yang sesuai
            DataTable transactionData = new ();
            transactionData.Columns.Add("No", typeof(Int32));
            transactionData.Columns.Add("ProductId", typeof(Int32));
            transactionData.Columns.Add("Barcode", typeof(string));
            transactionData.Columns.Add("Kode_Item", typeof(string));
            transactionData.Columns.Add("ProductName", typeof(string));
            transactionData.Columns.Add("Satuan", typeof(string));
            transactionData.Columns.Add("Hpp", typeof(decimal));
            transactionData.Columns.Add("Price", typeof(decimal));
            transactionData.Columns.Add("Qty", typeof(decimal));
            transactionData.Columns.Add("Bruto", typeof(decimal), "Price * Qty");
            transactionData.Columns.Add("Potongan", typeof(decimal));
            transactionData.Columns.Add("Total", typeof(decimal), "(Price * Qty)-Potongan");

            return transactionData;
        }
        private void gridView1_RowCountChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < gridView1.RowCount; i++)
            {

                gridView1.SetRowCellValue(i, gridView1.Columns["No"], i + 1);
            }
        }

        private void repositoryItemButtonEdit_HAPUS_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            gridView1.DeleteRow(gridView1.FocusedRowHandle);
            HitungTotalHarga();
        }
        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            // Check if the changed cell is in the "Qty" column
            if (e.Column.FieldName == "Qty")
            {
                HitungNilaiPotongan();

                // Get the current row
                int rowHandle = e.RowHandle;

                // Perform the calculations
                decimal qty = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Qty"));
                decimal price = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Price"));
                decimal discount = Convert.ToDecimal(gridView1.GetRowCellValue(rowHandle, "Potongan"));
                decimal bruto = qty * price;
                decimal total = (qty * price) - discount;

                // Update the "Total" column for the current row
                gridView1.SetRowCellValue(rowHandle, "Bruto", bruto);

                // Update the "Total" column for the current row
                gridView1.SetRowCellValue(rowHandle, "Total", total);               


                HitungTotalHarga();
                // Call the PostEditor method to update the GridView
                gridView1.UpdateCurrentRow();

            }

        }

        private void HitungNilaiPotongan()
        {
            // Get the value of the "Qty" column for the current row
            string kodebrg = gridView1.GetFocusedRowCellValue("Kode_Item").ToString();
            decimal qty = Convert.ToDecimal(gridView1.GetFocusedRowCellValue("Qty"));

            
            // Query the database for discounts based on quantity
            string query = "SELECT MINQTY,POTONGAN FROM POS_POTONGANBERDASARKANQTY WHERE KODE_ITEM = :kodebrg ";
            using var connection = new OracleConnection(Global.connectionString);
            var command = new OracleCommand(query, connection);

            command.Parameters.Add("kodebrg", kodebrg); // replace with actual product ID
            connection.Open();
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // Apply the discount to the "Discount" column for the current row
                decimal MINQTY = reader.GetDecimal(0);
                decimal POTONGAN = reader.GetDecimal(1);
                Decimal discount;
                if(qty% MINQTY == 0)
                {
                    decimal kelipatan = qty / MINQTY;
                    discount = kelipatan * POTONGAN;
                }
                else
                {
                    discount = POTONGAN;
                }
                gridView1.SetFocusedRowCellValue("Potongan", discount);
            }
            else
            {
                // No discount applies, set the "Discount" column to 0

                gridView1.SetFocusedRowCellValue("Potongan", 0);
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
        }

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            if (gridView1.FocusedColumn.FieldName == "Qty" && e.Value != null)
            {
                decimal qty;

                if (!decimal.TryParse(e.Value.ToString(), out qty))
                {
                    e.Valid = false;
                    e.ErrorText = "input hanya angka.";
                    return;
                }

                qty = Convert.ToDecimal(e.Value);
                if (qty < 0)
                {
                    e.Valid = false;
                    e.ErrorText = "QTY TIDAK BOLEH NEGATIVE.";
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
            if (tutup)
            {
                XtraMessageBox.Show("Periode telah ditutup,transaksi tidak dapat dilakukan","Info",MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MaxPeriodeFinder periodemax = new();
            var maxperiode = periodemax.GetMaxPeriode();
            if(periode> maxperiode)
            {
                XtraMessageBox.Show("Input transaksi dibatasi pada periode yang valid", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (maxperiode- periode >= 2)
            {
                XtraMessageBox.Show("Input transaksi mundur ditolak", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            if (gridView1.RowCount == 0) return;
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            //// Get the total amount due from the main form
            var Bruto = Convert.ToDecimal(gridView1.Columns["Bruto"].SummaryItem.SummaryValue.ToString());
            var Potongan = Convert.ToDecimal(gridView1.Columns["Potongan"].SummaryItem.SummaryValue.ToString());
            var Total = Convert.ToDecimal(gridView1.Columns["Total"].SummaryItem.SummaryValue.ToString());

            DTOFakturPenjualanHeader PenjualanHeader = new()
            {
                NO_TRANSAKSI = txtnotransaksi.Text,
                TANGGAL= Convert.ToDateTime(detanggal.Text),
                JAM = txtjam.Text,
                KASIR= txtkasir.Text,
                BRUTO=Bruto,
                POTONGAN=Potongan,
                TOTAL=Total
            };


            List<DTOFakturPenjualanDetail> itemPenjualanData = GetItemPenjualanData(gridView1);


            // Tampilkan form untuk memilih produk secara manual
            using PaymentForm paymentForm = new();
            paymentForm.StartPosition = FormStartPosition.CenterScreen;
            // Set the TotalAmount and Items properties
            paymentForm.FakturPenjualanHeader = PenjualanHeader;
             paymentForm.ListItemsPenjualan = itemPenjualanData;

            if (paymentForm.ShowDialog() == DialogResult.OK)
            {
                NewTransaction();
            }
        }
        public List<DTOFakturPenjualanDetail> GetItemPenjualanData(GridView gridView)
        {
            List<DTOFakturPenjualanDetail> ListItemsPenjualan = new();

            for (int i = 0; i < gridView.RowCount; i++)
            {
                DTOFakturPenjualanDetail data = new()
                {
                    BARIS = (Int32)gridView.GetRowCellValue(i, "No"),
                    PRODUCT_ID = (Int32)gridView.GetRowCellValue(i, "ProductId"),
                    KODE_BARANG = gridView.GetRowCellValue(i, "Kode_Item").ToString(),
                    BARCODE = gridView.GetRowCellValue(i, "Barcode").ToString(),
                    NAMA_BARANG = gridView.GetRowCellValue(i, "ProductName").ToString(),
                    SATUAN = gridView.GetRowCellValue(i, "Satuan").ToString(),
                    JUMLAH_BARANG = (decimal)gridView.GetRowCellValue(i, "Qty"),
                    HPP = (decimal)gridView.GetRowCellValue(i, "Hpp"),
                    HARGA_BARANG = (decimal)gridView.GetRowCellValue(i, "Price"),
                    BRUTO = (decimal)gridView.GetRowCellValue(i, "Bruto"),
                    POTONGAN = (decimal)gridView.GetRowCellValue(i, "Potongan"),
                    TOTAL_HARGA = (decimal)gridView.GetRowCellValue(i, "Total"),
                };
                ListItemsPenjualan.Add(data);
            }

            return ListItemsPenjualan;
        }

        private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            NewTransaction();
        }
    }
}
