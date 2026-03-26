using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;
using System.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BackOffice.UC
{
    public partial class ucPembelian : UserControl
    {
        bool ispending = false;
        List<DTOPRODUCTS> Barang;
        private BindingList<TransactionDataBeli> transactionDataList;
        PembelianController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucPembelian? _instance;
        public static ucPembelian Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPembelian();
                return _instance;
            }
        }

        public ucPembelian()
        {
            InitializeComponent(); 
            transactionDataList = new BindingList<TransactionDataBeli>();
            gridControl1.DataSource = transactionDataList;

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

                    //karena ada barang yang beda harga tapi dimasukan dalam item yang sama dibolehkan item yng sama dalam baris
                    //var existingProduct = transactionDataList.FirstOrDefault(p => p.Barcode == barcode);
                    //if (existingProduct != null)
                    //{
                    //    XtraMessageBox.Show("Item Barang sudah ada pada faktur\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //    txtnamabarang.Text = string.Empty;
                    //    barcodeTextBox.Focus();
                    //    return;
                    //}
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

                        //karena ada barang yang beda harga tapi dimasukan dalam item yang sama dibolehkan item yng sama dalam baris
                        //var existingProduct = transactionDataList.FirstOrDefault(p => p.Barcode == barcode);
                        //if (existingProduct != null)
                        //{
                        //    XtraMessageBox.Show("Item Barang sudah ada pada faktur\n" + kodeitem + " " + productname, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //    txtnamabarang.Text = string.Empty;
                        //    barcodeTextBox.Focus();
                        //    return;
                        //}

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
            txttermin.Text = "0";

            txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text)) ;
            transactionDataList.Clear();
            barcodeTextBox.Focus();
            ispending=false;
            txtqty.Text = "0";
            txthargabeli.Text = "0";
            txtpotongan.Text = "0";
        }

        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = "SELECT nomor FROM nomor_transaksi WHERE kode = 'PEMBELIAN' AND nomor LIKE :prefix ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand selectCommand = new(selectQuery, connection);
            selectCommand.Parameters.Add(":prefix", OracleDbType.Varchar2).Value = $"B-{currentYear}%";
            string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

            // Buat nomor transaksi baru untuk tahun saat ini
            string newTransactionNumber;
            if (string.IsNullOrEmpty(lastTransactionNumber))
            {
                newTransactionNumber = $"B-{currentYear.ToString("D2")}-000001"; // Jika belum ada nomor transaksi sebelumnya


            }
            else
            {
                int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 6));
                int newNumber = lastNumber + 1;
                newTransactionNumber = $"B-{currentYear.ToString("D2")}-{newNumber.ToString("D6")}"; // Format nomor transaksi dengan leading zero
            }

            return newTransactionNumber;
        }
        
        private void gridView1_RowCountChanged(object sender, EventArgs e)
            => PembelianHelper.UpdateRowNumbers(gridView1);

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
            => PembelianHelper.HandleCellValueChanged(gridView1, e, transactionDataList, HitungTotalHarga, barcodeTextBox);

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
            => PembelianHelper.HandleGridKeyDown(e, gridControl1, gridView1, transactionDataList, HitungTotalHarga, barcodeTextBox);

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
            => PembelianHelper.ValidateEditor(gridView1, e);

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
        private List<DTOFakturPembelianDetail> GetItemPembelianData()
            => PembelianHelper.GetItemPembelianData(transactionDataList);
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
                blbisimpan.PerformClick();
            }
        }
        int productid;


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

        private void Subtotal_calc()
            => PembelianHelper.SubtotalCalc(txtqty, txthargabeli, txtpotongan, txttotal);

        private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (transactionDataList.Count == 0) return;
            if (lookUpEditSupplier.EditValue ==null)
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
                SUPPLIER_ID = lookUpEditSupplier.EditValue.ToString(),
                BRUTO = Bruto,
                POTONGAN = Potongan,
                TOTAL = Total,
                TERMIN = int.TryParse(txttermin.Text, out int termin) ? termin : 0,
                USERID = txtuser.Text
            };


            List<DTOFakturPembelianDetail> itemPembelianData = GetItemPembelianData();
            //insert pembelian dan set hpp dan harga jual pada product
            controller.InsertFaktur_Pembelian(PembelianHeader, itemPembelianData);
            controller.UpdateTransactionNumber(PembelianHeader.NO_TRANSAKSI);

            NewTransaction();
        }
        private void ucPembelian_Load(object sender, EventArgs e)
        {
            var supplier = controller.GetSuppliers();
            lookUpEditSupplier.Properties.DataSource= supplier;
            lookUpEditSupplier.Properties.ValueMember = "KODE";
            lookUpEditSupplier.Properties.DisplayMember = "NAMA";
            NewTransaction();
            PembelianHelper.DisableColumnSorting(gridView1);
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
                    txtsatuan, txtqty, txthargabeli, txtpotongan, texthpplama, texthargajual, txttotal);
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
    }
}
