using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using Penjualan.BusinessLayer;
using Penjualan.Laporan;
using Penjualan.Model;
using System.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Penjualan
{
    public partial class frmEditFaktur : DevExpress.XtraEditors.XtraForm
    {
        string NIK, STATUS, UNIT_KERJA,ket_pembayaran,jenis_pembayaran;
        double ID;
        decimal LIMIT_HUTANG;
        public DTOFakturPenjualanHeader FakturPenjualanHeader { get; set; }
        public List<DTODaftarBarang> ListItemsPenjualan { get; set; }

        public frmEditFaktur()
        {
            InitializeComponent();
        }


        
        private void frmEditFaktur_Load(object sender, EventArgs e)
        {
            FinSettingsDataAccess finsetting = new();
            Load_angsuran(finsetting.GetMaxAngsuran());
            Load_Pelanggan();
            leangsuran.Text = FakturPenjualanHeader.TENOR.ToString();
            gridControl1.DataSource = ListItemsPenjualan.ToList();
            txtfaktur.Text = FakturPenjualanHeader.NO_TRANSAKSI;
            txtpelanggan.Text = FakturPenjualanHeader.NAMA_PELANGGAN;
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
            int index = datasource.FindIndex(item => item.NIK == FakturPenjualanHeader.NIK);

            searchLookUpEdit1.EditValue = datasource[index].NIK;
        }

        private static List<DTOPelanggan> GetPelanggan()
        {
            string query = "SELECT ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, LIMIT_HUTANG FROM FIN_ANGGOTA WHERE AKTIF='Y' ORDER BY NAMA_PELANGGAN";

            using OracleConnection connection = new (Global.connectionString);

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
                if(searchLookUpEdit1.EditValue.ToString()== FakturPenjualanHeader.NIK)
                {
                    return;
                }
                SimpanFakturPenjualan();
                //cetak
                //CetakFaktur();
                XtraMessageBox.Show("Faktur Penjualan berhaisl diubah", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
        }

        private void SimpanFakturPenjualan()
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
            

            //if (tenor == 1)
            //{
            //    //POS_Services.InsertFaktur_Penjualan(FakturPenjualanHeader, ListItemsPenjualan);
            //}
            //else
            //{
               
            //    List<DTOAngsuranKreditBarang> Daftar_Tagihan_Kredit_Barang = CalculateAngsuranKreditBarang(FakturPenjualanHeader.NO_TRANSAKSI, FakturPenjualanHeader.TANGGAL, FakturPenjualanHeader.TOTAL, FakturPenjualanHeader.TENOR);

            //    //POS_Services.InsertFaktur_Penjualan_Angsuran(FakturPenjualanHeader, ListItemsPenjualan, Daftar_Tagihan_Kredit_Barang);
                
            //}
            
            POS_Services.UpdateFakturPenjualan(FakturPenjualanHeader);

        }
        private void CetakFaktur()
        {
            rptFakturPenjualan report = new()
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
            report.Parameters["Bayar"].Value = 0;
            report.Parameters["Kembalian"].Value = 0;
            
            ReportPrintTool tool = new(report);
            tool.Print();
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
                        leangsuran.EditValue = 1;
                        labelControl4.Visible = false;
                        leangsuran.Visible = false;
                    }
                    else
                    {
                        jenis_pembayaran = "KREDIT";
                        ket_pembayaran = "TAGIHAN";
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


        private void PelangganTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtfaktur.Text != "")
                {
                    int index = datasource.FindIndex(item => item.NIK == txtfaktur.Text);

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
            }
        }

        private void searchLookUpEdit1_Popup(object sender, EventArgs e)
        {
            // Hide columns "id_pelanggan" and "unit_kerja"
            searchLookUpEdit1.Properties.View.Columns["ID_PELANGGAN"].Visible = false;
            searchLookUpEdit1.Properties.View.Columns["UNIT_KERJA"].Visible = false;
            searchLookUpEdit1.Properties.View.Columns["NAMA_PELANGGAN"].Width = 200;
        }

        private void leangsuran_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
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