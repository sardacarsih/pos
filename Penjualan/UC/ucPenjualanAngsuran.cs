using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using Penjualan.BusinessLayer;
using Penjualan.Laporan;
using Penjualan.Model;
using System.Data;
using System.Windows.Markup;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Penjualan.UC
{
    public partial class ucPenjualanAngsuran : UserControl
    {
        //Using singleton pattern to create an instance to ucModule3
        private static ucPenjualanAngsuran? _instance;
        public static ucPenjualanAngsuran Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPenjualanAngsuran();
                return _instance;
            }
        }
        private readonly OracleDataAdapter dataAdapter;
        private readonly DataTable dataTable;
        
        public ucPenjualanAngsuran()
        {
            InitializeComponent();
            this.PreviewKeyDown += ucPenjualanl_Key;
        }

        private void ucPenjualanl_Key(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.B)
            {
                blbisimpan.PerformClick();

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
                DTOProductInfo productInfo = POS_Services.RetrieveProductInfo(barcode);
                if (productInfo.ProductId != 0)
                {
                    {
                        int productId = productInfo.ProductId;
                        string kodeitem = productInfo.KodeItem;
                        string productName = productInfo.ProductName;
                        string satuan = productInfo.Satuan;
                        decimal price = productInfo.Price;

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
                            gridView1.SetRowCellValue(newRowHandle, "ProductId", productId);
                            gridView1.SetRowCellValue(newRowHandle, "ProductName", productName);
                            gridView1.SetRowCellValue(newRowHandle, "Satuan", satuan);
                            gridView1.SetRowCellValue(newRowHandle, "Barcode", barcode);
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
                }
                else
                {
                    // Tampilkan form untuk memilih produk secara manual
                    using ProductForm productForm = new ();
                    productForm.StartPosition = FormStartPosition.CenterScreen;
                    productForm.SetSearchPanelValue(barcodeTextBox.Text);
                    if (productForm.ShowDialog() == DialogResult.OK)
                    {
                        int productId = productForm.ProductId;
                        string Kode_Item = productForm.Kode_Item;
                        string productName = productForm.ProductName;
                        string Satuan = productForm.Satuan;
                        decimal price = productForm.Price;

                        // Cari produk di datagrid
                        int rowHandle = gridView1.LocateByValue("Kode_Item", Kode_Item);
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
                            gridView1.SetRowCellValue(newRowHandle, "Kode_Item", Kode_Item);
                            gridView1.SetRowCellValue(newRowHandle, "ProductId", productId);
                            gridView1.SetRowCellValue(newRowHandle, "ProductName", productName);
                            gridView1.SetRowCellValue(newRowHandle, "Satuan", Satuan);
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
            if (e.KeyCode == Keys.F2)
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


        private void NewTransaction()
        {
            detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            string transactionNumber = GetNextTransactionNumberForYear(Convert.ToDateTime(detanggal.Text).Year);
            txtnotransaksi.Text = transactionNumber;
           
            gridControl1.DataSource = GetTransactionData();
            txttotalpenjualan.Text ="";
            lepelanggan.EditValue = null;
            barcodeTextBox.Focus();
        }

        private static string GetNextTransactionNumberForYear(int year)
        {
            // Get the last transaction number for the year
            string lastTransactionNumber = GetLastTransactionNumberForYear(year);

            // Extract the transaction number portion of the string
            string transactionNumberString = lastTransactionNumber.Substring(0, 6);

            // Parse the transaction number as an integer and increment it by 1
            int nextTransactionNumber = int.Parse(transactionNumberString) + 1;

            // Format the transaction number as a string with leading zeros
            string nextTransactionNumberString = nextTransactionNumber.ToString("D6");

            // Format the next transaction number for the year in the "000002-YY" format
            string nextTransactionNumberForYear = nextTransactionNumberString + "-" + (year % 100).ToString("00");

            return nextTransactionNumberForYear;
        }


        private static string GetLastTransactionNumberForYear(int year)
        {
            // Construct the SQL query to retrieve the last transaction number for the given year
            string query = "SELECT MAX(NO_TRANSAKSI) FROM POS_KREDIT_PENJUALAN_MASTER " +
                           "WHERE EXTRACT(YEAR FROM TANGGAL) = :year";

            using (OracleConnection conn = new(Global.connectionString))
            // Create a new OracleCommand object to execute the query
            using (OracleCommand command = new(query, conn))
            {
                // Add a parameter for the year to the command
                command.Parameters.Add(new OracleParameter("year", year));

                // Open the database connection
                conn.Open();

                // Execute the query and retrieve the result
                object result = command.ExecuteScalar();

                // Check if the result is a valid string value
                if (result != null && result != DBNull.Value && result is string lastTransactionNumber)
                {
                    // Check if the last transaction number is for the same year
                    if (lastTransactionNumber.EndsWith("-" + (year % 100).ToString("00")))
                    {
                        return lastTransactionNumber;
                    }
                }

                // If no transaction number was found for the year or it's a different year, return a new transaction number for the year
                return "000000-" + (year % 100).ToString("00");
            }
        }




        private void Load_Pelanggan()
        {

            DataTable dataTable = Pelanggan();
                    lepelanggan.Properties.DataSource = dataTable;
                    lepelanggan.Properties.DisplayMember = "NAMA_PELANGGAN";
                    lepelanggan.Properties.ValueMember = "ID_PELANGGAN";
                    lepelanggan.Properties.BestFit();
        }

        private static DataTable Pelanggan()
        {
            string query = "SELECT ID_PELANGGAN,NIK,NAMA_PELANGGAN,UNIT_KERJA,STATUS,LIMIT_HUTANG FROM FIN_ANGGOTA WHERE AKTIF='Y' ORDER BY NAMA_PELANGGAN";
            using OracleConnection connection = new(Global.connectionString);
            using OracleCommand _command = new(query, connection)
            {
                CommandType = CommandType.Text
            };
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            OracleDataReader dr;
            dr = _command.ExecuteReader();
            DataTable _dt = new DataTable();
            _dt.Load(dr);
            dr.Close();
            connection.Close();
            return _dt;
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
            int productId = Convert.ToInt32(gridView1.GetFocusedRowCellValue("ProductId"));
            decimal qty = Convert.ToDecimal(gridView1.GetFocusedRowCellValue("Qty"));

            
            // Query the database for discounts based on quantity
            string query = "SELECT POTONGAN FROM POS_POTONGANBERDASARKANQTY WHERE PRODUCTID = :productId AND MINQTY <= :qty ORDER BY MINQTY DESC";
            using var connection = new OracleConnection(Global.connectionString);
            var command = new OracleCommand(query, connection);

            command.Parameters.Add("productId", productId); // replace with actual product ID
            command.Parameters.Add("qty", qty);
            connection.Open();
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // Apply the discount to the "Discount" column for the current row
                decimal discount = reader.GetDecimal(0);
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
                // Parse the entered value and check if it's less than 0
                if (Convert.ToDecimal(e.Value) <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty Negatif atau 0 tidak diperbolehkan.";
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
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            NewTransaction();
        }

        string NIK,STATUS, UNIT_KERJA;

        private void barcodeTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.End)
            {
                bbibayar.PerformClick();

            }
        }

        private void ucPenjualanAngsuran_Load(object sender, EventArgs e)
        {
            NewTransaction();
            Load_Pelanggan();
            Load_angsuran();
        }

        private void Load_angsuran()
        {
            Dictionary<int, string> Angsuran = new()
            {
                {1,"1 Kali" },
                  {2,"2 Kali" },
                    {3,"3 Kali" },
                      {4,"4 Kali" },
                  {5,"5 Kali" },
                   {6,"6 Kali" },
                    {7,"7 Kali" },
                     {8,"8 Kali" },
                      {9,"9 Kali" },
                       {10,"10 Kali" },
                        {11,"11 Kali" },
                         {12,"12 Kali" }

            };
            leangsuran.Properties.DataSource = Angsuran;
            leangsuran.ItemIndex = 0;
        }

        private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (gridView1.RowCount == 0) return;
            if (lepelanggan.EditValue == null)
            {
                XtraMessageBox.Show("Nama Pelanggan harus diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            //// Get the total amount due from the main form
            var Bruto = Convert.ToDecimal(gridView1.Columns["Bruto"].SummaryItem.SummaryValue.ToString());
            var Potongan = Convert.ToDecimal(gridView1.Columns["Potongan"].SummaryItem.SummaryValue.ToString());
            var Total = Convert.ToDecimal(gridView1.Columns["Total"].SummaryItem.SummaryValue.ToString());

            string nofaktur = txtnotransaksi.Text;
            DateTime tanggal = Convert.ToDateTime(detanggal.Text);
            string jam = txtjam.Text;
            string kasir = txtkasir.Text;
            Int32 kode_pelanggan = Convert.ToInt32(lepelanggan.EditValue.ToString());
            string nama_pelanggan = lepelanggan.Text;
            int angsuran = Convert.ToInt16(leangsuran.EditValue.ToString());
            decimal uang_muka = decimal.Parse(txtuangmuka.Text);
            decimal sisa_tagihan = Total - uang_muka;
            int waktu_angsuran = Convert.ToUInt16(leangsuran.EditValue.ToString());
            decimal jlh_angsuran = sisa_tagihan / waktu_angsuran;


            DTOFakturPenjualanHeader faktur_angsuran = new()
            {
                NO_TRANSAKSI = nofaktur,
                TANGGAL = tanggal,
                JAM = jam,
                KASIR = kasir,
                ID_PELANGGAN = kode_pelanggan,
                NIK = NIK,
                NAMA_PELANGGAN = nama_pelanggan,
                UNIT_KERJA = UNIT_KERJA,
                STATUS = STATUS,
                BRUTO = Bruto,
                POTONGAN = Potongan,
                TOTAL = Total,
                TENOR = waktu_angsuran,
                ANGSURAN = jlh_angsuran
            };


            List <DTOFakturPenjualanDetail> fakturPenjualanData = GetFakturPenjualanData(gridView1);

            List<DTOAngsuranKreditBarang> Dafrtar_Tagihan_Kredit = CalculateAngsuranKreditBarang(nofaktur, tanggal, sisa_tagihan, waktu_angsuran);


            POS_Services.InsertFaktur_Penjualan_Angsuran(faktur_angsuran, fakturPenjualanData, Dafrtar_Tagihan_Kredit);
            NewTransaction();
        }


        public List<DTOFakturPenjualanDetail> GetFakturPenjualanData(GridView gridView)
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
                    HARGA_BARANG = (decimal)gridView.GetRowCellValue(i, "Price"),
                    BRUTO = (decimal)gridView.GetRowCellValue(i, "Bruto"),
                    POTONGAN = (decimal)gridView.GetRowCellValue(i, "Potongan"),
                    TOTAL_HARGA = (decimal)gridView.GetRowCellValue(i, "Total"),
                };
                ListItemsPenjualan.Add(data);
            }

            return ListItemsPenjualan;
        }

        private void blbisimulasi_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var Total = Convert.ToDecimal(gridView1.Columns["Total"].SummaryItem.SummaryValue.ToString());
            decimal uang_muka = decimal.Parse(txtuangmuka.Text);
            decimal jumlahPinjaman = Total - uang_muka;
            int waktuangsuran = Convert.ToUInt16(leangsuran.EditValue.ToString());
            string nofaktur = txtnotransaksi.Text;
            DateTime tanggalPinjaman = Convert.ToDateTime(detanggal.Text);


            rptSimulasiAngsuran report = new()
            {
                DataSource = CalculateAngsuranKreditBarang(nofaktur,tanggalPinjaman, jumlahPinjaman, waktuangsuran)
            };
            report.Parameters["nomor"].Value = txtnotransaksi.Text;
            report.Parameters["nikdannama"].Value = lepelanggan.Text;
            report.Parameters["unitkerja"].Value = UNIT_KERJA;
            report.Parameters["tanggal"].Value = detanggal.Text;
            report.Parameters["jumlah"].Value = Total;
            report.Parameters["uangmuka"].Value = uang_muka;
            report.Parameters["sisa"].Value = jumlahPinjaman;
            report.Parameters["tenor"].Value = waktuangsuran;
            report.Parameters["angsuran"].Value = jumlahPinjaman / waktuangsuran;

            // Create a report print tool for the main report
            ReportPrintTool tool = new ReportPrintTool(report);

            // Access the designer of the subreport
            XRSubreport subreport = report.FindControl("xrSubreport1", true) as XRSubreport;
            if (subreport != null)
            {
                rptSimulasiAngsuranSub subreportDesign = subreport.ReportSource as rptSimulasiAngsuranSub;
                if (subreportDesign != null)
                {
                    // Set the data source of the subreport
                    subreportDesign.DataSource = GetFakturPenjualanData(gridView1); 
                }
            }

            // Show the preview of the main report
            tool.ShowPreview();

        }
        decimal LIMIT_HUTANG;
        private void lepelanggan_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                if(lepelanggan.EditValue != null)
                {
                    LookUpEdit editor = (sender as LookUpEdit);
                    DataRowView row = editor.Properties.GetDataSourceRowByKeyValue(editor.EditValue) as DataRowView;
                    NIK = row["NIK"].ToString().ToUpper();
                    STATUS = row["STATUS"].ToString().ToUpper();
                    UNIT_KERJA = row["UNIT_KERJA"].ToString().ToUpper();
                    LIMIT_HUTANG = Convert.ToDecimal(row["LIMIT_HUTANG"]);
                }
                
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static List<DTOAngsuranKreditBarang> CalculateAngsuranKreditBarang(string nomortransaksi, DateTime tanggalBelanja, decimal jumlahBelanja, int waktuangsuran)
        {
            List<DTOAngsuranKreditBarang> listAngsuran = new List<DTOAngsuranKreditBarang>();
            decimal saldoAwal = jumlahBelanja;
            decimal P; // Installment amount

            // Calculate the installment amount
            P = saldoAwal / waktuangsuran;

            // Calculate installment for each month within the specified duration
            for (int i = 1; i <= waktuangsuran; i++)
            {
                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i);
                DateTime tanggalJatuhTempo = new(bulanBerikutnya.Year, bulanBerikutnya.Month, 1);
                decimal saldoAkhir = saldoAwal - P;

                DTOAngsuranKreditBarang angsuran = new()
                {
                    NO_TRANSAKSI = nomortransaksi,
                    TANGGALJATUHTEMPO = tanggalJatuhTempo,
                    ANGSURANKE = i,
                    SALDOAWAL = Math.Round(saldoAwal, 2),
                    ANGSURAN = Math.Round(P, 2),
                    SALDOAKHIR = Math.Round(saldoAkhir, 2)
                };

                listAngsuran.Add(angsuran);
                saldoAwal = saldoAkhir;
            }

            return listAngsuran;
        }
    }

   
}
