using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Laporan;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice
{
    public partial class PaymentForm : DevExpress.XtraEditors.XtraForm
    {
        PendingController controller = new();
        public bool PendingFaktur { get; set; }
        private string jenis_pembayaran="TUNAI";
        private string ket_pembayaran = "KAS";
        string NIK, STATUS, UNIT_KERJA;
        double ID,LIMIT_HUTANG;
        public DTOFakturPenjualanHeader FakturPenjualanHeader { get; set; }
        public List<DTOFakturPenjualanDetail> ListItemsPenjualan { get; set; }

        public PaymentForm()
        {
            InitializeComponent();
        }


        
        private void PaymentForm_Load(object sender, EventArgs e)
        {
            FinSettingsDataAccess finsetting = new();
            txttotal.Text = FakturPenjualanHeader.TOTAL.ToString();
            Load_angsuran(finsetting.GetMaxAngsuran());
            Load_Pelanggan();
            leangsuran.ItemIndex = 0;
            
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

        List<DTOPelanggan> datasource;
        private void Load_Pelanggan()
        {
            datasource = GetPelanggan();
            searchLookUpEdit1.Properties.DataSource = datasource;
            searchLookUpEdit1.Properties.DisplayMember = "NAMA_PELANGGAN";
            searchLookUpEdit1.Properties.ValueMember = "NIK";

            //// Hide columns "id_pelanggan" and "unit_kerja"
            //searchLookUpEdit1.Properties.View.Columns["ID_PELANGGAN"].Visible = false;
            //searchLookUpEdit1.Properties.View.Columns["UNIT_KERJA"].Visible = false;
            //searchLookUpEdit1.Properties.View.Columns["NAMA_PELANGGAN"].Width = 200;


            // Set the default value for searchLookUpEdit1
            int index = datasource.FindIndex(item => item.NIK == "00.00004");

            searchLookUpEdit1.EditValue = datasource[index].NIK;
        }

        private static List<DTOPelanggan> GetPelanggan()
        {
            string query = @"SELECT A.ID_PELANGGAN, A.NIK, A.NAMA_PELANGGAN, A.UNIT_KERJA,K.NAMA UNITKERJA, A.STATUS, A.LIMIT_HUTANG FROM FIN_ANGGOTA A
                            JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA
                            WHERE A.AKTIF='Y' ORDER BY A.NAMA_PELANGGAN";

            using OracleConnection connection = new (global.connectionString);

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            List<DTOPelanggan> pelangganList = connection.Query<DTOPelanggan>(query).ToList();

            connection.Close();

            return pelangganList;
        }
        private void sbsimpancetak_Click(object sender, EventArgs e)
        {
            if (searchLookUpEdit1.EditValue != null)
            {
                bayar = decimal.Parse(txtcash.Text);
                if (searchLookUpEdit1.EditValue.ToString() == "00.00004" && (bayar == 0 || kembalian < 0))
                {
                    txtcash.Focus();
                    XtraMessageBox.Show("Pembayaran harus lebih besar atau sama dengan", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                SimpanFakturPenjualan("T");
                //cetak
                CetakFaktur();
                
                this.DialogResult = DialogResult.OK;
            }
        }

        private void SimpanFakturPenjualan(string ispending)
        {
            int tenor;
            if (leangsuran.EditValue == null)
            {
                tenor = 1;
            }
            else
            {
                tenor = (int)leangsuran.EditValue;
            }
            FakturPenjualanHeader.JENIS_BAYAR = jenis_pembayaran;
            FakturPenjualanHeader.KET_BAYAR = ket_pembayaran;
            FakturPenjualanHeader.ID_PELANGGAN = ID;
            FakturPenjualanHeader.NIK = NIK;
            FakturPenjualanHeader.NAMA_PELANGGAN = searchLookUpEdit1.Text;
            FakturPenjualanHeader.STATUS = STATUS;
            FakturPenjualanHeader.UNIT_KERJA = UNIT_KERJA;
            FakturPenjualanHeader.TENOR = tenor;
            FakturPenjualanHeader.ANGSURAN = FakturPenjualanHeader.TOTAL / FakturPenjualanHeader.TENOR;
            FakturPenjualanHeader.PENDING = ispending;

            if (tenor == 1)
            {
                POS_Services.InsertFaktur_Penjualan(FakturPenjualanHeader, ListItemsPenjualan);
            }
            else
            {
               
                List<DTOAngsuranKreditBarang> Daftar_Tagihan_Kredit_Barang = CalculateAngsuranKreditBarang(FakturPenjualanHeader.NO_TRANSAKSI, FakturPenjualanHeader.TANGGAL, FakturPenjualanHeader.TOTAL, FakturPenjualanHeader.TENOR);

                POS_Services.InsertFaktur_Penjualan_Angsuran(FakturPenjualanHeader, ListItemsPenjualan, Daftar_Tagihan_Kredit_Barang);
                
            }
            if (PendingFaktur)
            {
                controller.DeletePendingFaktur(FakturPenjualanHeader.NO_TRANSAKSI);
            }
            POS_Services.UpdateTransactionNumber(FakturPenjualanHeader.NO_TRANSAKSI);

        }
        private void CetakFaktur()
        {
            rptFakturJual report = new()
            {
                DataSource = ListItemsPenjualan
            };
            report.Parameters["Nama_Toko"].Value = "KOPKAR - KUSUMA LESTARI";
            report.Parameters["NoFaktur"].Value = FakturPenjualanHeader.NO_TRANSAKSI;
            report.Parameters["Pelanggan"].Value = FakturPenjualanHeader.NIK+", "+FakturPenjualanHeader.NAMA_PELANGGAN.ToUpper()+" , "+ FakturPenjualanHeader.UNIT_KERJA;
            report.Parameters["Jenis_Bayar"].Value = FakturPenjualanHeader.JENIS_BAYAR;
            report.Parameters["Waktu"].Value = FakturPenjualanHeader.TANGGAL.ToString("dd-MMM-yy")+" "+ FakturPenjualanHeader.JAM;
            report.Parameters["Tenor"].Value = FakturPenjualanHeader.TENOR;
            report.Parameters["Bruto"].Value = FakturPenjualanHeader.BRUTO;
            report.Parameters["Potongan"].Value = FakturPenjualanHeader.POTONGAN;
            report.Parameters["Total"].Value = FakturPenjualanHeader.TOTAL;

            report.Parameters["Kasir"].Value = "";            
            report.Parameters["Bayar"].Value = decimal.Parse(txtcash.Text);
            report.Parameters["Kembalian"].Value = decimal.Parse(txtkembali.Text);
            report.ShowPrintMarginsWarning = false;
            ReportPrintTool tool = new(report);
            tool.Print();
        }

        decimal bayar = 0;
            decimal kembalian=0;
        private void txtcash_EditValueChanged(object sender, EventArgs e)
        {
            var total=decimal.Parse(txttotal.Text);
            bayar = decimal.Parse(txtcash.Text);
            kembalian = bayar - total;
            txtkembali.Text=kembalian.ToString();
        }

        private void radioGroup1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void txtcash_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
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

                    if (NIK == "00.00004")
                    {
                        jenis_pembayaran = "TUNAI";
                        ket_pembayaran = "KAS";
                        lblbayar.Visible = true;
                        lblkembali.Visible = true;
                        txtcash.Visible = true;
                        txtkembali.Visible = true;
                        leangsuran.EditValue = 1;
                        labelControl4.Visible = false;
                        leangsuran.Visible = false;
                    }
                    else
                    {
                        jenis_pembayaran = "KREDIT";
                        ket_pembayaran = "TAGIHAN";
                        lblbayar.Visible = false;
                        lblkembali.Visible = false;
                        txtcash.Visible = false;
                        txtkembali.Visible = false;
                        leangsuran.EditValue = 1;
                        labelControl4.Visible = true;
                        leangsuran.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PelangganTextBox_EditValueChanged(object sender, EventArgs e)
        {
            
        }

        private void PelangganTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (PelangganTextBox.Text != "")
                {
                    int index = datasource.FindIndex(item => item.NIK == PelangganTextBox.Text);

                    if (index != -1)
                    {
                        searchLookUpEdit1.EditValue = datasource[index].NIK;
                        sbsimpancetak.Enabled = true;
                    }
                    else
                    {
                        // Set the default value for searchLookUpEdit1
                        //int index2 = datasource.FindIndex(item => item.NIK == "00.00004");

                        //searchLookUpEdit1.EditValue = datasource[index2].NIK;
                        searchLookUpEdit1.EditValue = null;
                        sbsimpancetak.Enabled = false;
                    }
                }
                SendKeys.Send("{TAB}");
            }
        }

        private void searchLookUpEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void leangsuran_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void PelangganTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void searchLookUpEdit1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void PaymentForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                // Set the default value for searchLookUpEdit1
                int index2 = datasource.FindIndex(item => item.NIK == "00.00004");

                searchLookUpEdit1.EditValue = datasource[index2].NIK;
                sbsimpancetak.Enabled = true;
                txtcash.Focus();
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

        private void leangsuran_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void sbtutup_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        

        public static List<DTOAngsuranKreditBarang> CalculateAngsuranKreditBarang(string nomortransaksi,DateTime tanggalBelanja, decimal jumlahBelanja, int waktuangsuran)
        {
            List<DTOAngsuranKreditBarang> listAngsuran = new();
            decimal saldoAwal = jumlahBelanja;
            decimal P; // Installment amount

            // Calculate the installment amount
            P = saldoAwal / waktuangsuran;

            // Calculate installment for each month within the specified duration
            for (int i = 1; i <= waktuangsuran; i++)
            {
                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i);
                DateTime tanggalJatuhTempo = new (bulanBerikutnya.Year, bulanBerikutnya.Month, 1);
                decimal saldoAkhir = saldoAwal - P;

                DTOAngsuranKreditBarang angsuran = new ()
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