
using BackOffice.BussinessLayer;
using DevExpress.XtraEditors;
using Microsoft.VisualBasic;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using BackOffice.DataLayer;
using BackOffice.Model;
using BackOffice.Controller;

namespace BackOffice.UC
{
    public partial class ucPinjaman : UserControl
    {
        AnggotaController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucPinjaman? _instance;
        public static ucPinjaman Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPinjaman();
                return _instance;
            }
        }

        List<DTOSimulasiAngsuran> simulasiPINJAMAN = new();
        double bunga_efektif = 0;
        public ucPinjaman()
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




        private void NewTransaction()
        {
            bersihkan();
            txtnotransaksi.Text = GenerateTransactionNumber(Convert.ToDateTime(detanggal.Text));
        }




        private void Load_Pelanggan()
        {

            // DataTable dataTable = Tools_Services.AnggotaAktif();
            var datasource = controller.GetAnggotaAktif();
                    leanggota.Properties.DataSource = datasource;
                    leanggota.Properties.DisplayMember = "NAMA_PELANGGAN";
                    leanggota.Properties.ValueMember = "ID_PELANGGAN";
            
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
            if (leanggota.EditValue == null || txtpinjaman.Text == "0" || txtketerangan.Text == "")
            {
               // XtraMessageBox.Show("Semua data wajib diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (XtraMessageBox.Show("Anda akan membatalkan transaksi ini", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
           
            disablesimpan();
            bersihkan();
        }

        private void bersihkan()
        {
            detanggal.Text = DateTime.Today.ToString("dd-MMM-yyyy");
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
            leanggota.EditValue = null;
            leangsuran.ItemIndex = 0;
            txtpinjaman.Text = "0";
            txtketerangan.Text = "";
            lblterbilang.Text = "";
            simulasiPINJAMAN.Clear();
            gridControl1.DataSource = null;

        }

        string NIK,STATUS, UNIT_KERJA;

        private void barcodeTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.End)
            {
                bbibayar.PerformClick();

            }
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

        private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                XtraMessageBox.Show("Periode telah ditutup,transaksi tidak dapat dilakukan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MaxPeriodeFinder periodemax = new();
            var maxperiode = periodemax.GetMaxPeriode();
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
            if (leanggota.EditValue == null)
            {
                XtraMessageBox.Show("Nama Pelanggan harus diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");
         
            string nofaktur = txtnotransaksi.Text;
            DateTime tanggal = Convert.ToDateTime(detanggal.Text);
            string jam = txtjam.Text;
            string kasir = txtkasir.Text;
            Int32 kode_pelanggan = Convert.ToInt32(leanggota.EditValue.ToString());
            string nama_pelanggan = leanggota.Text;          
            decimal TOTALPINJAMAN = decimal.Parse(txtpinjaman.Text);           
            int waktu_angsuran = Convert.ToUInt16(leangsuran.EditValue.ToString());
            string keterangan = txtketerangan.Text;
            double jumlahAngsuran = Financial.Pmt((double)(bunga_efektif / 100), waktu_angsuran, (double)(-TOTALPINJAMAN), 0, DueDate.EndOfPeriod);




            DTOPinjaman PinjamanTunaiMaster = new()
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
                PINJAMAN = TOTALPINJAMAN,
                TENOR = waktu_angsuran,
                BUNGA=bunga_efektif,
                ANGSURAN = (decimal)jumlahAngsuran,
                KETERANGAN=keterangan
            };
            List<DTOSimulasiAngsuran> PinjamanTunaiDetail = CalculateAngsuranPinjaman(nofaktur,tanggal, TOTALPINJAMAN, waktu_angsuran,bunga_efektif);


            Finance_Services.InsertFaktur_PinjamanTunai(PinjamanTunaiMaster, PinjamanTunaiDetail);
            UpdateTransactionNumber(nofaktur);
            NewTransaction();
        }
        public void UpdateTransactionNumber(string transactionNumber)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'PINJAMAN'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('PINJAMAN', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                // Update existing record
                string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'PINJAMAN'";
                using OracleCommand updateCommand = new(updateQuery, connection);
                updateCommand.Parameters.Add("nomor", transactionNumber);
                updateCommand.ExecuteNonQuery();
            }
        }

        private void blbisimulasi_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (leanggota.EditValue == null || txtpinjaman.Text == "0" || txtketerangan.Text == "")
            {
                XtraMessageBox.Show("Semua data wajib diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            string nofaktur = txtnotransaksi.Text;
            DateTime tanggal = Convert.ToDateTime(detanggal.Text);
            decimal pinjaman = decimal.Parse(txtpinjaman.Text);
            int waktu_angsuran = Convert.ToUInt16(leangsuran.EditValue.ToString());
            var bunga = double.Parse(txtbunga.Text);
            simulasiPINJAMAN = CalculateAngsuranPinjaman(nofaktur,tanggal, pinjaman, waktu_angsuran, bunga);
            gridControl1.DataSource = simulasiPINJAMAN;

            gridView1.Columns["NO_TRANSAKSI"].Visible = false;
            gridView1.Columns["TanggalJatuhTempo"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            gridView1.Columns["TanggalJatuhTempo"].DisplayFormat.FormatString = "dd-MMM-yyyy";
           
            gridView1.Columns["SaldoAwal"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["SaldoAwal"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["Pokok"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Pokok"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["Bunga"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Bunga"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["Angsuran"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["Angsuran"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["SaldoAkhir"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["SaldoAkhir"].DisplayFormat.FormatString = "N2";
            enablesimpan();
        }

        private void enablesimpan()
        {
            blbisimpan.Enabled = true;
        }


        private void ucPinjaman_Load(object sender, EventArgs e)
        {
            FinSettingsDataAccess finsetting = new();
            bunga_efektif = finsetting.GetBungaEfektif();
            NewTransaction();
            Load_Pelanggan();
            Load_angsuran(finsetting.GetMaxAngsuran());
            txtbunga.Text= bunga_efektif.ToString();
           

        }


        private void txtpinjaman_KeyUp(object sender, KeyEventArgs e)
        {
            lblterbilang.Text = Tools_Services.TerbilangIndonesia(double.Parse(txtpinjaman.Text));
        }

        private void cboCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
           

        }

        private void detanggal_EditValueChanged(object sender, EventArgs e)
        {
            disablesimpan();
        }

        private void disablesimpan()
        {
           blbisimpan.Enabled = false;
        }

        private void leangsuran_EditValueChanged(object sender, EventArgs e)
        {
            disablesimpan();
        }

        private void txtpinjaman_EditValueChanged(object sender, EventArgs e)
        {
            disablesimpan();
        }

        private void txtketerangan_EditValueChanged(object sender, EventArgs e)
        {
            disablesimpan();
        }

        private void leanggota_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (leanggota.EditValue != null)
                {

                    // Get the selected item from the control
                    DTOAnggotaAktif selectedObject = (DTOAnggotaAktif)leanggota.GetSelectedDataRow();
                    if (selectedObject != null)
                    {
                        NIK = selectedObject.NIK;
                        STATUS = selectedObject.STATUS;
                        UNIT_KERJA = selectedObject.UNITKERJA;
                        disablesimpan();
                    }
                    
                }

            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void leanggota_Popup(object sender, EventArgs e)
        {
            // Hide columns "id_pelanggan" and "unit_kerja"
            leanggota.Properties.Columns["ID_PELANGGAN"].Visible = false;
            leanggota.Properties.Columns["UNIT_KERJA"].Visible = false;
            leanggota.Properties.Columns["LIMIT_HUTANG"].Visible = false;
            leanggota.Properties.Columns["NIK"].Width = 60;
            leanggota.Properties.Columns["NAMA_PELANGGAN"].Width = 160;
            leanggota.Properties.Columns["UNITKERJA"].Width = 120;
            leanggota.Properties.PopupWidth=400;
        }


        public static List<DTOSimulasiAngsuran> CalculateAngsuranPinjaman(string nomortransaksi,DateTime tanggalPinjaman, decimal jumlahPinjaman, int waktuangsuran, double bungapersen)
        {
            List<DTOSimulasiAngsuran> listAngsuran = new();
            double saldoAwal = (double)jumlahPinjaman; // Convert saldoAwal to double

           

            // Hitung angsuran setiap bulan selama waktuangsuran bulan
            for (int i = 0; i < waktuangsuran; i++)
            {
                DateTime bulanBerikutnya = tanggalPinjaman.AddMonths(i);
                DateTime tanggalJatuhTempo = new DateTime(bulanBerikutnya.Year, bulanBerikutnya.Month, DateTime.DaysInMonth(bulanBerikutnya.Year, bulanBerikutnya.Month));
                int p_periode = int.Parse(tanggalJatuhTempo.ToString("yyyyMM"));

                decimal bunga = Math.Round((decimal)(saldoAwal * (bungapersen / 100)), 2); // Convert bunga to decimal
                double jumlahAngsuran = Financial.Pmt((double)(bungapersen / 100), waktuangsuran - i, -saldoAwal, 0, DueDate.EndOfPeriod);
                decimal pokok = Math.Round((decimal)jumlahAngsuran - bunga, 2);
                double saldoAkhir = saldoAwal - (double)pokok; // Convert saldoAkhir to decimal

                listAngsuran.Add(new DTOSimulasiAngsuran()
                {
                    Periode= p_periode,
                    NO_TRANSAKSI = nomortransaksi,
                    TanggalJatuhTempo = tanggalJatuhTempo,
                    AngsuranKe = i + 1,
                    SaldoAwal = Math.Round((decimal)saldoAwal, 2), // Round saldoAwal to 2 decimal places
                    Pokok = Math.Round(pokok, 2), // Round pokok to 2 decimal places
                    Bunga = Math.Round(bunga, 2), // Round bunga to 2 decimal places
                    Angsuran = Math.Round((decimal)jumlahAngsuran, 2), // Round jumlahAngsuran to 2 decimal places
                    SaldoAkhir = Math.Round((decimal)saldoAkhir, 2) // Round saldoAkhir to 2 decimal places

                });

                saldoAwal = (double)saldoAkhir; // Convert saldoAwal to double for the next iteration
            }

            return listAngsuran;
        }
        public string GenerateTransactionNumber(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'PINJAMAN' AND nomor LIKE 'P-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();
                using (OracleCommand selectCommand = new OracleCommand(selectQuery, connection))
                {
                    string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

                    // Buat nomor transaksi baru untuk tahun saat ini
                    string newTransactionNumber;
                    if (string.IsNullOrEmpty(lastTransactionNumber))
                    {
                        newTransactionNumber = $"P-{currentYear.ToString("D2")}-000001"; // Jika belum ada nomor transaksi sebelumnya

                        
                    }
                    else
                    {
                        int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 6));
                        int newNumber = lastNumber + 1;
                        newTransactionNumber = $"P-{currentYear.ToString("D2")}-{newNumber.ToString("D6")}"; // Format nomor transaksi dengan leading zero
                    }

                    return newTransactionNumber;
                }
            }
        }

    }
}
