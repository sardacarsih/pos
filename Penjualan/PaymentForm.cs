using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using Penjualan.BusinessLayer;
using Penjualan.Laporan;
using Penjualan.Model;


namespace Penjualan
{
    public partial class PaymentForm : DevExpress.XtraEditors.XtraForm
    {
        private const string DEFAULT_CUSTOMER_NIK = "00.00004";
        PendingController controller = new();
        public bool PendingFaktur { get; set; }
        private string jenis_pembayaran = "TUNAI";
        private string ket_pembayaran = "KAS";
        string NIK, STATUS, UNIT_KERJA;
        double ID;
        decimal JUMLAHFAKTUR, LIMIT;
        public DTOFakturPenjualanHeader FakturPenjualanHeader { get; set; }
        public List<DTOFakturPenjualanDetail> ListItemsPenjualan { get; set; }


        public DateTime BulananDari { get; set; }
        public DateTime BulananSampai { get; set; }
        public DateTime Remise1Dari { get; set; }
        public DateTime Remise1Sampai { get; set; }
        public DateTime Remise2Dari { get; set; }
        public DateTime Remise2Sampai { get; set; }


        public PaymentForm()
        {
            InitializeComponent();
        }



        private void PaymentForm_Load(object sender, EventArgs e)
        {
            FinSettingsDataAccess finsetting = new();
            txttotal.Text = FakturPenjualanHeader.TOTAL.ToString();
            JUMLAHFAKTUR = FakturPenjualanHeader.TOTAL;
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
            // Set the default value for searchLookUpEdit1
            int index = datasource.FindIndex(item => item.NIK == DEFAULT_CUSTOMER_NIK);
            if (index >= 0)
                searchLookUpEdit1.EditValue = datasource[index].NIK;
        }

        private static List<DTOPelanggan> GetPelanggan()
        {
            return POS_Services.GetPelangganAktif();
        }
        private void sbsimpancetak_Click(object sender, EventArgs e)
        {
            if (searchLookUpEdit1.EditValue != null)
            {
                if (!decimal.TryParse(txtcash.Text, out bayar))
                    bayar = 0;
                if (searchLookUpEdit1.EditValue.ToString() == DEFAULT_CUSTOMER_NIK && (bayar == 0 || kembalian < 0))
                {
                    txtcash.Focus();
                    XtraMessageBox.Show("Pembayaran harus lebih besar atau sama dengan", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    SimpanFakturPenjualan("T");
                    CetakFaktur();
                    this.DialogResult = DialogResult.OK;
                }
                catch (CreditLimitExceededException ex)
                {
                    sbsimpancetak.Enabled = false;
                    XtraMessageBox.Show(
                        $"Transaksi tidak dapat disimpan karena limit hutang telah terlampaui.\n\n" +
                        $"Hutang Saat Ini     : Rp. {ex.CurrentDebt:N0}\n" +
                        $"Jumlah Faktur Baru  : Rp. {ex.InvoiceAmount:N0}\n" +
                        $"Batas Limit Hutang  : Rp. {ex.Limit:N0}",
                        "Limit Hutang Melebihi Batas", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

            // Build credit limit check for non-cash customers
            CreditLimitCheck? creditCheck = null;
            if (NIK != DEFAULT_CUSTOMER_NIK && LIMIT != 0)
            {
                DateTime dari, sampai;
                if (STATUS == "BULANAN")
                {
                    dari = BulananDari;
                    sampai = BulananSampai;
                }
                else
                {
                    dari = Remise1Dari;
                    sampai = Remise2Sampai;
                }

                creditCheck = new CreditLimitCheck
                {
                    NIK = NIK,
                    STATUS = STATUS,
                    Limit = LIMIT,
                    InvoiceAmount = FakturPenjualanHeader.TOTAL,
                    PeriodFrom = dari,
                    PeriodTo = sampai
                };
            }

            if (tenor == SinglePaymentTenor)
            {
                POS_Services.InsertFaktur_Penjualan(FakturPenjualanHeader, ListItemsPenjualan, creditCheck);
            }
            else
            {
                List<DTOAngsuranKreditBarang> Daftar_Tagihan_Kredit_Barang = CalculateAngsuranKreditBarang(FakturPenjualanHeader.NO_TRANSAKSI, FakturPenjualanHeader.TANGGAL, FakturPenjualanHeader.TOTAL, FakturPenjualanHeader.TENOR);
                POS_Services.InsertFaktur_Penjualan_Angsuran(FakturPenjualanHeader, ListItemsPenjualan, Daftar_Tagihan_Kredit_Barang, creditCheck);
            }

            if (PendingFaktur)
            {
                controller.DeletePendingFaktur(FakturPenjualanHeader.NO_TRANSAKSI);
            }
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
            report.Parameters["Bayar"].Value = decimal.TryParse(txtcash.Text, out var bayarVal) ? bayarVal : 0m;
            report.Parameters["Kembalian"].Value = decimal.TryParse(txtkembali.Text, out var kembaliVal) ? kembaliVal : 0m;
            report.ShowPrintMarginsWarning = false;
            ReportPrintTool tool = new(report);
            tool.Print();
        }

        private float CalculatePaperHeight(int numberOfRecords)
        {
            float defaultPageHeight = 1169.0f; // Base paper height (fits ~40 records)
            float heightPerRecord = 25.0f; // Height per additional record

            int extraRecords = numberOfRecords - 40;
            float calculatedHeight = defaultPageHeight + (extraRecords * heightPerRecord);

            return calculatedHeight;
        }

        decimal bayar = 0;
        decimal kembalian = 0;
        private void txtcash_EditValueChanged(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txttotal.Text, out var total)) total = 0;
            if (!decimal.TryParse(txtcash.Text, out bayar)) bayar = 0;
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
                    LIMIT= selectedObject.LIMIT_HUTANG;

                    if (NIK == DEFAULT_CUSTOMER_NIK)
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
                        if (LIMIT != 0)
                        {
                            decimal totalHutang = Checking_JumlahHutang(NIK, STATUS);
                            decimal totalSetelahFaktur = totalHutang + JUMLAHFAKTUR;

                            if (totalSetelahFaktur > LIMIT)
                            {
                                decimal kelebihan = totalSetelahFaktur - LIMIT;
                                sbsimpancetak.Enabled = false;

                                string message =
                                    $"Transaksi tidak dapat disimpan karena limit hutang telah terlampaui.\n\n" +
                                    $"📌 **Detail Hutang:**\n" +
                                    $"- Hutang Saat Ini     : Rp. {totalHutang:N0}\n" +
                                    $"- Jumlah Faktur Baru  : Rp. {JUMLAHFAKTUR:N0}\n" +
                                    $"- Total Setelah Faktur: Rp. {totalSetelahFaktur:N0}\n" +
                                    $"- Batas Limit Hutang  : Rp. {LIMIT:N0}\n" +
                                    $"- Kelebihan Limit      : Rp. {kelebihan:N0}\n\n" +
                                    $"Silakan lakukan pelunasan terlebih dahulu atau hubungi bagian keuangan.";

                                XtraMessageBox.Show(
                                    message,
                                    "Limit Hutang Melebihi Batas",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
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

        

        private decimal Checking_JumlahHutang(string nIK, string sTATUS)
        {
            DateTime dari, sampai;

            if (sTATUS == "BULANAN")
            {
                dari = BulananDari;
                sampai = BulananSampai;
            }
            else
            {
                dari = Remise1Dari;
                sampai = Remise2Sampai;
            }

            return POS_Services.CheckingJumlahHutang(nIK, sTATUS, dari, sampai);
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
                        //int index2 = datasource.FindIndex(item => item.NIK == DEFAULT_CUSTOMER_NIK);

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
                int index2 = datasource.FindIndex(item => item.NIK == DEFAULT_CUSTOMER_NIK);
                if (index2 < 0) return;
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
            decimal P = Math.Floor(saldoAwal / waktuangsuran);

            for (int i = 1; i <= waktuangsuran; i++)
            {
                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i - 1);
                DateTime tanggalJatuhTempo = new DateTime(bulanBerikutnya.Year, bulanBerikutnya.Month, DateTime.DaysInMonth(bulanBerikutnya.Year, bulanBerikutnya.Month));
                int p_periode = int.Parse(tanggalJatuhTempo.ToString("yyyyMM"));

                decimal angsuranBulanIni = (i == waktuangsuran) ? saldoAwal : P;
                decimal saldoAkhir = saldoAwal - angsuranBulanIni;

                DTOAngsuranKreditBarang angsuran = new()
                {
                    PERIODE = p_periode,
                    NO_TRANSAKSI = nomortransaksi,
                    TANGGALJATUHTEMPO = tanggalJatuhTempo,
                    ANGSURANKE = i,
                    SALDOAWAL = Math.Round(saldoAwal, 2),
                    ANGSURAN = Math.Round(angsuranBulanIni, 2),
                    SALDOAKHIR = Math.Round(saldoAkhir, 2)
                };

                listAngsuran.Add(angsuran);
                saldoAwal = saldoAkhir;
            }

            return listAngsuran;
        }

        private void leangsuran_EditValueChanged(object sender, EventArgs e)
        {
        }
    }
}