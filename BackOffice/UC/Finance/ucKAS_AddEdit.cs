
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
    public partial class ucKAS_AddEdit : UserControl
    {
        AnggotaController controller = new();
        //Using singleton pattern to create an instance to ucModule3
        private static ucKAS_AddEdit? _instance;
        public static ucKAS_AddEdit Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucKAS_AddEdit();
                return _instance;
            }
        }

        private Dictionary<int, string> mykasbank = new()
                    {
                        {1,"KAS" },
                        {2,"BANK" }
                    };
        public ucKAS_AddEdit()
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
        private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (lejenisbayar.EditValue == null || txtpinjaman.Text == "0" || txtketerangan.Text == "")
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
            lejenisbayar.EditValue = null;
            lebayarvia.ItemIndex = 0;
            txtpinjaman.Text = "0";
            txtketerangan.Text = "";
            lblterbilang.Text = "";
            gridControl1.DataSource = null;

        }

        string NIK, STATUS, UNIT_KERJA;

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

            lebayarvia.Properties.DataSource = Angsuran;
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
            if (lejenisbayar.EditValue == null)
            {
                XtraMessageBox.Show("Nama Pelanggan harus diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            txtjam.Text = DateTime.Now.ToString("HH:mm:ss");

            string nofaktur = txtnotransaksi.Text;
            DateTime tanggal = Convert.ToDateTime(detanggal.Text);
            string jam = txtjam.Text;
            string kasir = txtkasir.Text;
            Int32 kode_pelanggan = Convert.ToInt32(lejenisbayar.EditValue.ToString());
            string nama_pelanggan = lejenisbayar.Text;
           


            NewTransaction();
        }
        private void blbisimulasi_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (lejenisbayar.EditValue == null || txtpinjaman.Text == "0" || txtketerangan.Text == "")
            {
                XtraMessageBox.Show("Semua data wajib diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            string nofaktur = txtnotransaksi.Text;
            DateTime tanggal = Convert.ToDateTime(detanggal.Text);
            decimal pinjaman = decimal.Parse(txtpinjaman.Text);
            int waktu_angsuran = Convert.ToUInt16(lebayarvia.EditValue.ToString());

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
            NewTransaction();
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

        private void ucPembayaran_Load(object sender, EventArgs e)
        {
            var jenis = Finance_Services.JenisBayar();
            lejenisbayar.Properties.DataSource = jenis;
            lejenisbayar.Properties.ValueMember = "Id";
            lejenisbayar.Properties.DisplayMember = "Nama";

            
            lekasbank.Properties.DataSource = mykasbank;
            lekasbank.Properties.DisplayMember = "Value";
            lekasbank.Properties.ValueMember = "Key";


           
           
        }

        private void lekasbank_EditValueChanged(object sender, EventArgs e)
        {
            if(sender is LookUpEdit lookUpEdit)
            {
                int selectedkey = (int)lekasbank.EditValue;
                string selectedvalue = mykasbank[selectedkey];

                var via = Finance_Services.PembayaranVia(selectedvalue);
                lebayarvia.Properties.DataSource = via;
                lebayarvia.Properties.ValueMember = "Id";
                lebayarvia.Properties.DisplayMember = "Nama";
            }
           

        }
    }
}
