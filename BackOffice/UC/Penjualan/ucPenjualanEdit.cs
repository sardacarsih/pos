using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Model;
using BackOffice.View;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucPenjualanEdit : UserControl
    {
        PenjualanController controller = new();
        private string jenis_pembayaran = string.Empty;
        private string ket_pembayaran = string.Empty;
        string NIK, STATUS, UNIT_KERJA;
        double ID, LIMIT_HUTANG;
        bool ispending = false;
        List<DTOPRODUCTS> Barang;
        private BindingList<TransactionDataJual> transactionDataList;
        //public DTOFakturPenjualanHeader header { get; set; }
        public List<DTODaftarBarang> items { get; set; }

        public void SetPenjualanHeader(DTOFakturPenjualanHeader penjualanHeader)
        {
            FinSettingsDataAccess finsetting = new();
            Load_angsuran(finsetting.GetMaxAngsuran());
            Load_Pelanggan();
            // Set the data from PenjualanHeader to the controls in ucPenjualanEdit
            // Example:
            txtnotransaksi.Text = penjualanHeader.NO_TRANSAKSI;
            detanggal.EditValue = Convert.ToDateTime(penjualanHeader.TANGGAL.ToString("dd-MMM-yyyy"));
            leangsuran.EditValue = penjualanHeader.TENOR;
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            // Set the default value for searchLookUpEdit1
            int index = datasource.FindIndex(item => item.NIK == penjualanHeader.NIK);
            if (index != -1)
            {
                searchLookUpEdit1.EditValue = datasource[index].NIK;
            }

            // Set other controls as needed
        }
        List<DTOPelanggan> datasource;
        private void Load_Pelanggan()
        {
            datasource = GetPelanggan();
            searchLookUpEdit1.Properties.DataSource = datasource;
            searchLookUpEdit1.Properties.DisplayMember = "NAMA_PELANGGAN";
            searchLookUpEdit1.Properties.ValueMember = "NIK";

        }

        private List<DTOPelanggan> GetPelanggan()
        {
            string query = @"SELECT A.ID_PELANGGAN, A.NIK, A.NAMA_PELANGGAN, A.UNIT_KERJA,K.NAMA UNITKERJA, A.STATUS, A.LIMIT_HUTANG FROM FIN_ANGGOTA A
                            JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA
                            WHERE A.AKTIF='Y' ORDER BY A.NAMA_PELANGGAN";
            using OracleConnection connection = new(global.connectionString);

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            List<DTOPelanggan> pelangganList = connection.Query<DTOPelanggan>(query).ToList();

            connection.Close();

            return pelangganList;
        }

        private void Load_angsuran(int tenor)
        {

            Dictionary<int, string> Angsuran = new();
            for (int i = 1; i <= tenor; i++)
            {
                Angsuran.Add(i, i + " Kali");
            }

            leangsuran.Properties.DataSource = Angsuran;
        }

        public void SetItemPenjualanData(List<DTODaftarBarang> itemPenjualanData)
        {
            // Process and display the itemPenjualanData in ucPenjualanEdit
            // Example:
            foreach (var item in itemPenjualanData)
            {
                var newProduct = new TransactionDataJual
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


        public ucPenjualanEdit()
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
                //int JUMLAH=int.Parse(barcode.Substring(0, barcode.Length-1));
                var scantransaksi = Barang.Where(t => t.BARCODE == barcode).FirstOrDefault();
                if (scantransaksi != null)
                {
                    int productid = Convert.ToInt32(scantransaksi.PRODUCTID);
                    string kodeitem = scantransaksi.KODE_ITEM.ToString();
                    string productname = scantransaksi.PRODUCTNAME.ToString();
                    string satuan = scantransaksi.SATUAN.ToString();
                    decimal price = Convert.ToDecimal(scantransaksi.PRICE);
                    decimal hpp = Convert.ToDecimal(scantransaksi.BELI);
                    if (price <= hpp)
                    {
                        XtraMessageBox.Show("Harga jual <= dari harga beli\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                    var existingProduct = transactionDataList.FirstOrDefault(p => p.Barcode == barcode);
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

                        var existingProduct = transactionDataList.FirstOrDefault(p => p.ProductId == productid);
                        if (existingProduct != null)
                        {
                            existingProduct.Qty++;
                            existingProduct.UpdateTotal();

                        }
                        else
                        {
                            var newProduct = new TransactionDataJual
                            {
                                ProductId = productid,
                                Barcode = barcodefromx,
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
                blbiupdate.PerformClick();
            }
        }
        decimal total;
        private void HitungTotalHarga()
        {
            total = transactionDataList.Sum(p => p.Total);
        }

        private void ucPenjualanEdit_Load(object sender, EventArgs e)
        {

            Load_RefreshData();
            barcodeTextBox.Focus();
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
            catch
            {

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
            command.Parameters.Add("kodebrg", kodebrg);
            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                decimal MINQTY = reader.GetDecimal(0);
                decimal POTONGAN = reader.GetDecimal(1);

                decimal discount;
                if (qty % MINQTY == 0)
                {
                    decimal kelipatan = qty / MINQTY;
                    discount = qty * POTONGAN;
                }
                else if (qty >= MINQTY)
                {
                    int result = (int)Math.Floor(qty / MINQTY);
                    discount = qty * POTONGAN;
                }
                else
                {
                    discount = 0;
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

        private void ucPenjualan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                // Your F5 key press logic here
                // For example, you can refresh the user control or perform any desired action
                blbiupdate.PerformClick();
            }
        }

        private void LoadDataFakturPending()
        {
            //// Tampilkan form untuk memilih produk secara manual
            //using frmPending PendingForm = new();
            //PendingForm.StartPosition = FormStartPosition.CenterScreen;
            //if (PendingForm.ShowDialog() == DialogResult.OK)
            //{
            //    string notransaksi = PendingForm.No_Transaksi;
            //    DateTime tanggal = PendingForm.Tanggal;
            //    string jam = PendingForm.Jam;

            //    txtnotransaksi.Text = notransaksi;
            //    detanggal.Text = tanggal.ToString("dd-MMM-yyyy");
            //    txtjam.Text = jam;

            //    var itempending = controller.GetDaftarBarangPending(notransaksi).ToList();               
            //    foreach (var item in itempending)
            //    {
            //        var newProduct = new TransactionData
            //        {
            //            ProductId = item.PRODUCT_ID,
            //            Barcode = item.BARCODE,
            //            Kode_Item = item.KODE_BARANG,
            //            ProductName = item.NAMA_BARANG,
            //            Satuan = item.SATUAN,
            //            Hpp = item.HPP,
            //            Price = item.HARGA_BARANG,
            //            Qty = item.JUMLAH_BARANG
            //        };

            //        newProduct.UpdateTotal();
            //        transactionDataList.Add(newProduct);
            //    }
            //    // Calculate and update the total price
            //    HitungTotalHarga();
            //    gridView1.RefreshData();
            //    ispending = true;
            //}
        }

        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Load_RefreshData();
        }

        private void blbitutup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Find the parent form of the user control
            frmEditFakturPenjualan parentForm = (frmEditFakturPenjualan)FindForm();

            // Check if the parent form was found and if it is a valid form
            if (parentForm != null && parentForm is Form)
            {
                // Close the parent form
                parentForm.Close();
            }

        }

        private void searchLookUpEdit1_Popup(object sender, EventArgs e)
        {
            // Hide columns "id_pelanggan" and "unit_kerja"
            searchLookUpEdit1.Properties.View.Columns["ID_PELANGGAN"].Visible = false;
            searchLookUpEdit1.Properties.View.Columns["UNIT_KERJA"].Visible = false;
            searchLookUpEdit1.Properties.View.Columns["NIK"].Width = 60;
            searchLookUpEdit1.Properties.View.Columns["NAMA_PELANGGAN"].Width = 160;
            searchLookUpEdit1.Properties.View.Columns["UNITKERJA"].Width = 120;
        }

        private void blbiupdate_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(searchLookUpEdit1.Text))
            {
                XtraMessageBox.Show("Nama Pelaggan tidak boleh kosong", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
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
            var VBruto = transactionDataList.Sum(p => p.Qty * p.Price);
            var VPotongan = transactionDataList.Sum(p => p.Potongan);
            var VTotal = transactionDataList.Sum(p => p.Total);

            DTOFakturPenjualanHeader FakturPenjualanHeader = new()
            {
                NO_TRANSAKSI = txtnotransaksi.Text,
                TANGGAL = Convert.ToDateTime(detanggal.Text),
                JAM = txtjam.Text,
                KASIR = txtkasir.Text,
                BRUTO = VBruto,
                POTONGAN = VPotongan,
                TOTAL = VTotal,
                TENOR= (int)leangsuran.EditValue
        };


            List<DTOFakturPenjualanDetail> ListItemsPenjualan = GetItemPenjualanData();

            int tenor;
            if (leangsuran.EditValue == null)
            {
                tenor = 1;
            }
            else
            {
                tenor = (int)leangsuran.EditValue;
            }
            FakturPenjualanHeader.JENIS_BAYAR = "KREDIT";
            FakturPenjualanHeader.KET_BAYAR = "TAGIHAN";
            FakturPenjualanHeader.ID_PELANGGAN = ID;
            FakturPenjualanHeader.NIK = NIK;
            FakturPenjualanHeader.NAMA_PELANGGAN = searchLookUpEdit1.Text;
            FakturPenjualanHeader.STATUS = STATUS;
            FakturPenjualanHeader.UNIT_KERJA = UNIT_KERJA;
            FakturPenjualanHeader.TENOR = tenor;
            FakturPenjualanHeader.ANGSURAN = FakturPenjualanHeader.TOTAL / FakturPenjualanHeader.TENOR;
            FakturPenjualanHeader.PENDING = "T";
            //menghapus faktur penjualan,detail penjualan dan daftar angsuran penjualan kredit
            controller.HapusFakturPenjualan(FakturPenjualanHeader.NO_TRANSAKSI);
            if (tenor == 1)
            {

                POS_Services.InsertFaktur_Penjualan(FakturPenjualanHeader, ListItemsPenjualan);
            }
            else
            {
                // controller.HapusFakturPenjualanAngsuran(FakturPenjualanHeader.NO_TRANSAKSI);
                List<DTOAngsuranKreditBarang> Daftar_Tagihan_Kredit_Barang = CalculateAngsuranKreditBarang(FakturPenjualanHeader.NO_TRANSAKSI, FakturPenjualanHeader.TANGGAL, FakturPenjualanHeader.TOTAL, FakturPenjualanHeader.TENOR);

                POS_Services.InsertFaktur_Penjualan_Angsuran(FakturPenjualanHeader, ListItemsPenjualan, Daftar_Tagihan_Kredit_Barang);

            }
            XtraMessageBox.Show("Faktur Penjualan berhail diupdate", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static List<DTOAngsuranKreditBarang> CalculateAngsuranKreditBarang(string nomortransaksi, DateTime tanggalBelanja, decimal jumlahBelanja, int waktuangsuran)
        {
            List<DTOAngsuranKreditBarang> listAngsuran = new();
            decimal saldoAwal = jumlahBelanja;
            decimal P; // Installment amount

            // Calculate the installment amount
            P = saldoAwal / waktuangsuran;

            // Calculate installment for each month within the specified duration
            for (int i = 1; i <= waktuangsuran; i++)
            {

                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i).AddDays(-tanggalBelanja.AddMonths(i).Day); // Set to the last day of the month
                DateTime tanggalJatuhTempo = new DateTime(bulanBerikutnya.Year, bulanBerikutnya.Month, bulanBerikutnya.Day);
                int p_periode = Convert.ToInt32(tanggalJatuhTempo.ToString("yyyyMM"));

                decimal saldoAkhir = saldoAwal - P;

                DTOAngsuranKreditBarang angsuran = new()
                {
                    PERIODE = p_periode,
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
        private void searchLookUpEdit1_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (searchLookUpEdit1.EditValue != null)
                {

                    // Get the selected item from the control
                    DTOPelanggan selectedObject = (DTOPelanggan)searchLookUpEdit1.GetSelectedDataRow();

                    // Access the values from the selected object

                    ID = Convert.ToDouble(selectedObject.ID_PELANGGAN);
                    NIK = selectedObject.NIK;
                    STATUS = selectedObject.STATUS;
                    UNIT_KERJA = selectedObject.UNIT_KERJA;


                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<int, int> maxInstallments = new()
        {
                { 1, 12 }, // January
                { 2, 11 }, // February
                { 3, 10 }, // March
                { 4, 9 },  // April
                { 5, 8 },  // May
                { 6, 7 },  // June
                { 7, 6 },  // July 
                { 8, 5 },  // August 
                { 9, 4 },  // September 
                { 10, 3 }, // October 
                { 11, 2 }, // November
                { 12, 1 }  // December
         };

        private void leangsuran_EditValueChanged(object sender, EventArgs e)
        {

        }
    }
}
