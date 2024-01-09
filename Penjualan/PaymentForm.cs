using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using Penjualan.BusinessLayer;
using Penjualan.Laporan;
using Penjualan.Model;
using System.Data;
using System.Globalization;

namespace Penjualan
{
    public partial class PaymentForm : DevExpress.XtraEditors.XtraForm
    {
        PendingController controller = new();
        public bool PendingFaktur { get; set; }
        private string jenis_pembayaran = "TUNAI";
        private string ket_pembayaran = "KAS";
        string NIK, STATUS, UNIT_KERJA;
        double ID, JUMLAHFAKTUR;
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
            JUMLAHFAKTUR = Convert.ToDouble(FakturPenjualanHeader.TOTAL);
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

        private void Load_angsuranStatik(int tenor)
        {
            Dictionary<int, string> Angsuran = new();

            // Define maximum allowed installment values for the selected month
            int maxAllowedInstallments = maxInstallments[tenor];

            for (int i = 1; i <= maxAllowedInstallments; i++)
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
            // Set the default value for searchLookUpEdit1
            int index = datasource.FindIndex(item => item.NIK == "00.00004");

            searchLookUpEdit1.EditValue = datasource[index].NIK;
        }

        private static List<DTOPelanggan> GetPelanggan()
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

        private const int SinglePaymentTenor = 1;

        private void SimpanFakturPenjualan(string ispending)
        {
            int tenor = leangsuran?.EditValue as int? ?? SinglePaymentTenor;

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

            if (tenor == SinglePaymentTenor)
            {
                HandleSinglePayment();
            }
            else
            {
                HandleInstallmentPayments();
            }

            if (PendingFaktur)
            {
                controller.DeletePendingFaktur(FakturPenjualanHeader.NO_TRANSAKSI);
            }

            POS_Services.UpdateTransactionNumber(FakturPenjualanHeader.NO_TRANSAKSI);
        }

        private void HandleSinglePayment()
        {
            POS_Services.InsertFaktur_Penjualan(FakturPenjualanHeader, ListItemsPenjualan);
        }

        private void HandleInstallmentPayments()
        {
            List<DTOAngsuranKreditBarang> Daftar_Tagihan_Kredit_Barang = CalculateAngsuranKreditBarang(FakturPenjualanHeader.NO_TRANSAKSI, FakturPenjualanHeader.TANGGAL, FakturPenjualanHeader.TOTAL, FakturPenjualanHeader.TENOR);

            POS_Services.InsertFaktur_Penjualan_Angsuran(FakturPenjualanHeader, ListItemsPenjualan, Daftar_Tagihan_Kredit_Barang);
        }

        private void CetakFaktur()
        {
            rptFakturJual report = new()
            {
                DataSource = ListItemsPenjualan
            };
            // Set the default paper height
            int defaultPageHeight = 1169;
            report.PageHeight = defaultPageHeight;

            // Calculate the paper height based on the number of records
            if (ListItemsPenjualan.Count > 40)
            {
                float paperHeight = CalculatePaperHeight(ListItemsPenjualan.Count);

                // Set the calculated paper height with an explicit cast to int
                report.PageHeight = (int)paperHeight;
            }

            // Set other properties
            report.PageWidth = 297;
            report.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            report.PaperName = "Roll Paper 76 x 297 mm";

            report.Parameters["Nama_Toko"].Value = "KOPKAR - KUSUMA LESTARI";
            report.Parameters["NoFaktur"].Value = FakturPenjualanHeader.NO_TRANSAKSI;
            report.Parameters["Pelanggan"].Value = FakturPenjualanHeader.NIK + ", " + FakturPenjualanHeader.NAMA_PELANGGAN.ToUpper() + " , " + FakturPenjualanHeader.UNIT_KERJA;
            report.Parameters["Jenis_Bayar"].Value = FakturPenjualanHeader.JENIS_BAYAR;
            report.Parameters["Waktu"].Value = FakturPenjualanHeader.TANGGAL.ToString("dd-MMM-yy") + " " + FakturPenjualanHeader.JAM;
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

        private float CalculatePaperHeight(int numberOfRecords)
        {
            // Adjust the paper height based on the number of records
            // You might need to fine-tune these values based on your actual requirements
            float defaultPageHeight = 1169.0f; // Base paper height
            float heightPerRecord = 0.5f; // Height per record

            float calculatedHeight = defaultPageHeight + (numberOfRecords * heightPerRecord);

            return calculatedHeight;
        }

        decimal bayar = 0;
        decimal kembalian = 0;
        private void txtcash_EditValueChanged(object sender, EventArgs e)
        {
            var total = decimal.Parse(txttotal.Text);
            bayar = decimal.Parse(txtcash.Text);
            kembalian = bayar - total;
            txtkembali.Text = kembalian.ToString();
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
                        sbsimpancetak.Enabled = true;
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
                        sbsimpancetak.Enabled = true;
                        double LimitHutang = Checking_Limit(NIK);
                        if (LimitHutang != 0)
                        {
                            double GetSalesTotal = Checking_JumlahHutang(NIK, STATUS);
                            double total = GetSalesTotal + JUMLAHFAKTUR;
                            if (total > LimitHutang)
                            {
                                var lebih = total - LimitHutang;
                                sbsimpancetak.Enabled = false;
                                XtraMessageBox.Show("Limit Hutang Waserda Telah Melebihi Batas \nRp." + lebih, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private double Checking_Limit(string nIK)
        {
            double limit = 0;
            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new("SELECT LIMIT_HUTANG FROM FIN_ANGGOTA WHERE NIK = :Nik", connection);
                command.Parameters.Add(new OracleParameter("Nik", nIK));

                object result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    limit = Convert.ToDouble(result);
                }
            }

            return limit;
        }

        private double Checking_JumlahHutang(string nIK, string sTATUS)
        {
            var harini = DateTime.Today;
            DateTime dari, sampai;
            DateTime lastDayOfMonth = new(harini.Year, harini.Month, DateTime.DaysInMonth(harini.Year, harini.Month));
            if (sTATUS == "BULANAN")
            {
                dari = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                sampai = lastDayOfMonth;
            }
            else
            {
                if (harini.Day < 16)
                {
                    dari = new(harini.Year, harini.Month, 1);
                    sampai = new(harini.Year, harini.Month, 15);
                }
                else
                {
                    dari = new(harini.Year, harini.Month, 16);
                    sampai = lastDayOfMonth;
                }

            }
            double total = 0;

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new("SELECT SUM(TOTAL) FROM POS_PENJUALAN WHERE NIK = :Nik AND TANGGAL BETWEEN :Dari AND :Sampai", connection);
                command.Parameters.Add(new OracleParameter("Nik", nIK));
                command.Parameters.Add(new OracleParameter("Dari", dari));
                command.Parameters.Add(new OracleParameter("Sampai", sampai));

                object result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    total = Convert.ToDouble(result);
                }
            }

            return total;
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

                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i-1);
                DateTime tanggalJatuhTempo = new DateTime(bulanBerikutnya.Year, bulanBerikutnya.Month, DateTime.DaysInMonth(bulanBerikutnya.Year, bulanBerikutnya.Month));
                int p_periode = int.Parse(tanggalJatuhTempo.ToString("yyyyMM"));

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

        private void leangsuran_EditValueChanged(object sender, EventArgs e)
        {
            //sbsimpancetak.Enabled = true;
            //var tanggaltransaksi = FakturPenjualanHeader.TANGGAL;
            //var angsuran = (int)leangsuran.EditValue;

            //// Check if the selected installment exceeds the maximum allowed for the given month
            //if (maxInstallments.TryGetValue(tanggaltransaksi.Month, out int maxAllowed) && angsuran > maxAllowed)
            //{
            //    sbsimpancetak.Enabled = false;
            //    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(tanggaltransaksi.Month);
            //    XtraMessageBox.Show($"Untuk bulan {monthName}, Angsuran potongan barang hanya diperbolehkan maksimal {maxAllowed} kali Angsuran.", "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //}
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
    }
}