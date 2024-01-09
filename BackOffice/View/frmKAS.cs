using BackOffice.Controller;
using BackOffice.DataLayer;
using DevExpress.Utils.About;
using DevExpress.XtraEditors;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice.View
{
    public partial class frmKAS : DevExpress.XtraEditors.XtraForm
    {
        public int productid { get; set; }
        public string kategori { get; set; }
        public string barcode { get; set; }
        public string kode_item { get; set; }
        public string productname { get; set; }
        public string satuan { get; set; }
        public decimal price { get; set; }
        public decimal beli { get; set; }
        public char aktif { get; set; }
        public string form_actions { get; set; }
        public string form_title { get; set; }
        public string Kode_Item
        {
            get { return kode_item; }
        }
        MasterDataController controller = new();
        public frmKAS()
        {
            InitializeComponent();
        }

        private void frmKAS_Load(object sender, EventArgs e)
        {
            this.Text = form_title;
            bbisimpan.Caption = form_actions;
            lesatuan.Properties.DataSource = controller.GetSatuan();
            lesatuan.Properties.ValueMember = "SATUAN";
            lesatuan.Properties.DisplayMember = "SATUAN";
            lookUpEdit1.Properties.DataSource = controller.GetKategori();
            lookUpEdit1.Properties.ValueMember = "KODE";
            lookUpEdit1.Properties.DisplayMember = "KATEGORI";

            if (form_actions == "Simpan")
            {
                txtkode_barang.Text = GenerateTransactionNumber(DateTime.Today);                
                txthargaBeli.Text = "0";
                txthargaJual.Text = "0";
                lblnonaktif.Visible = false;
                checkEdit_nonaktif.Visible = false;
            }
            else
            {
                lblnonaktif.Visible = true;
                checkEdit_nonaktif.Visible = true;
                txtkode_barang.Text = kode_item;
                txtnama_barang.Text = productname;
                lesatuan.Text = satuan;
                txtbarkode.Text = barcode;
                lookUpEdit1.EditValue = kategori;
                txthargaBeli.Text = beli.ToString();
                txthargaJual.Text = price.ToString();
                if (aktif == 'T')
                {
                    checkEdit_nonaktif.Checked = true;
                }
            }
            txtnama_barang.Focus();
        }
        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'BARANG' AND nomor LIKE 'KB-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand selectCommand = new(selectQuery, connection);
            string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

            // Buat nomor transaksi baru untuk tahun saat ini
            string newTransactionNumber;
            if (string.IsNullOrEmpty(lastTransactionNumber))
            {
                newTransactionNumber = $"KB-{currentYear.ToString("D2")}-00001"; // Jika belum ada nomor transaksi sebelumnya


            }
            else
            {
                int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 5));
                int newNumber = lastNumber + 1;
                newTransactionNumber = $"KB-{currentYear.ToString("D2")}-{newNumber.ToString("D5")}"; // Format nomor transaksi dengan leading zero
            }

            return newTransactionNumber;
        }
        public bool SaveProductData(string kodeItem, string productName, string barcode, decimal hargabeli,decimal price, string satuan, string kategori)
        {

            // Define the SQL query to insert the data into the POS_PRODUCT table
            string query = "INSERT INTO POS_PRODUCT (KODE_ITEM, PRODUCTNAME, BARCODE, BELI,PRICE,  SATUAN, KATEGORI_ID) " +
                           "VALUES (:kodeItem, :productName, :barcode, :hbeli,:price, :satuan, :kategori)";

            // Create a new Oracle connection using the connection string
            using OracleConnection connection = new(global.connectionString);
            // Open the connection
            connection.Open();

            // Create a new Oracle command using the SQL query and the connection
            using OracleCommand command = new(query, connection);
            // Bind the parameters to the command
            command.Parameters.Add(":kodeItem", OracleDbType.Varchar2, 20).Value = kodeItem;
            command.Parameters.Add(":productName", OracleDbType.Varchar2, 100).Value = productName;
            command.Parameters.Add(":barcode", OracleDbType.Varchar2, 20).Value = barcode;
            command.Parameters.Add(":hbeli", OracleDbType.Double).Value = hargabeli;
            command.Parameters.Add(":price", OracleDbType.Double).Value = price;
            command.Parameters.Add(":satuan", OracleDbType.Varchar2, 20).Value = satuan;
            command.Parameters.Add(":kategori", OracleDbType.Varchar2, 20).Value = kategori;

            // Execute the command and retrieve the number of rows affected
            int rowsAffected = command.ExecuteNonQuery();

            // Return true if the rows affected is greater than 0, indicating that the data was saved
            return rowsAffected > 0;
        }


        private void bbitutup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Close();
        }
        public bool EditProductData(string productName, string barcode, decimal price, string satuan, string kategori, decimal hargaBeli,string isaktif)
        {
            // Define the SQL query to update the data in the POS_PRODUCT table
            string query = "UPDATE POS_PRODUCT " +
                           " SET PRODUCTNAME = :productName, " +
                           "    BARCODE = :barcode, " +
                           "    PRICE = :price, " +
                           "    SATUAN = :satuan, " +
                           "    KATEGORI_ID = :kategori, " +
                            "    BELI = :hargaBeli, " +
                            "    AKTIF = :isaktif " +
                           "WHERE PRODUCTID = :Product_Id";

            // Create a new Oracle connection using the connection string
            using OracleConnection connection = new(global.connectionString);
            // Open the connection
            connection.Open();

            // Create a new Oracle command using the SQL query and the connection
            using OracleCommand command = new(query, connection);
            // Bind the parameters to the command
            command.Parameters.Add(":productName", OracleDbType.Varchar2, 100).Value = productName;
            command.Parameters.Add(":barcode", OracleDbType.Varchar2, 50).Value = barcode;
            command.Parameters.Add(":price", OracleDbType.Double).Value = price;
            command.Parameters.Add(":satuan", OracleDbType.Varchar2, 20).Value = satuan;
            command.Parameters.Add(":kategori", OracleDbType.Varchar2, 20).Value = kategori;
            command.Parameters.Add(":hargaBeli", OracleDbType.Double).Value = hargaBeli;            
            command.Parameters.Add(":isaktif", OracleDbType.Varchar2).Value = isaktif;
            command.Parameters.Add(":Product_Id", OracleDbType.Int32).Value = productid;
            // Execute the command and retrieve the number of rows affected
            int rowsAffected = command.ExecuteNonQuery();

            // Return true if the rows affected is greater than 0, indicating that the data was saved
            return rowsAffected > 0;
        }
        string kodeItem;
        private void bbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtnama_barang.Text)||lesatuan.EditValue==null || lookUpEdit1.EditValue == null)
                {
                    XtraMessageBox.Show("data wajib diisi");
                    return;
                }
                    bool sukses;
                kodeItem = txtkode_barang.Text;
                var productName = txtnama_barang.Text;
                var barcode = txtbarkode.Text;
                var price = decimal.Parse(txthargaJual.Text);
                var hargaBeli = decimal.Parse(txthargaBeli.Text);
                var satuan = lesatuan.Text;
                var kategori = lookUpEdit1.EditValue.ToString();

                if(hargaBeli==0)
                {
                    XtraMessageBox.Show("Silahkan tentukan harga beli terlebih dahulu", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (price == 0 )
                {
                    XtraMessageBox.Show("Silahkan tentukan harga jual terlebih dahulu", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (price <= hargaBeli )
                {
                    XtraMessageBox.Show("Harga jual tidak boleh <= dari harga beli", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (productid == 0)
                {
                    sukses = SaveProductData(kodeItem, productName, barcode, hargaBeli,price, satuan, kategori);
                    if (sukses)
                    {
                    UpdateTransactionNumber(txtkode_barang.Text);
                        ClearData();
                    txtkode_barang.Text = GenerateTransactionNumber(DateTime.Today);
                        XtraMessageBox.Show("Data berhasil disimpan", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                }
                else
                {
                var aktif = "Y";
                if(checkEdit_nonaktif.Checked) { aktif = "T"; };
                    sukses = EditProductData(productName, barcode,price, satuan, kategori, hargaBeli, aktif);
                    if (sukses)
                    {
                        XtraMessageBox.Show("Data berhasil diupdate", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                }
                txtnama_barang.Focus();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("DUPLIKAT_NAMA_BARANG"))
                {
                    XtraMessageBox.Show("Nama barang sudah ada,jika tidak terlihat pada daftar cek pada daftar barang nonaktif", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtnama_barang.Focus();
                }
                else if (ex.Message.Contains("DUPLIKAT_BARCODE"))
                {
                    XtraMessageBox.Show("Barcode ini sudah ada", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtbarkode.Focus();
                }
                else if (ex.Message.Contains("DUPLIKAT_KODE_BARANG"))
                {
                    XtraMessageBox.Show("Kode Barang ini sudah ada", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtkode_barang.Focus();
                }
                else
                {
                    XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }

        }

        private void ClearData()
        {
            txtnama_barang.Text = string.Empty;
            txtnama_barang.Text = string.Empty;
            txtbarkode.Text = string.Empty;
            lesatuan.EditValue = null;
            lookUpEdit1.EditValue= null;
            txthargaBeli.Text = "0";
            txthargaJual.Text = "0";
        }

        private void frmMasterBarang_FormClosing(object sender, FormClosingEventArgs e)
        {
            kode_item = kodeItem;
            this.DialogResult = DialogResult.OK;
        }
        public void UpdateTransactionNumber(string transactionNumber)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'BARANG'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('BARANG', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                // Update existing record
                string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'BARANG'";
                using OracleCommand updateCommand = new(updateQuery, connection);
                updateCommand.Parameters.Add("nomor", transactionNumber);
                updateCommand.ExecuteNonQuery();
            }
        }

        private void txtnama_barang_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void lesatuan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void txtbarkode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void lookUpEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void txthargaBeli_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void txthargaJual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }
    }
}