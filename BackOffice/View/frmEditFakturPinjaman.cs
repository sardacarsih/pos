using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using DevExpress.CodeParser;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;

namespace BackOffice.View
{
    public partial class frmEditFakturPinjaman : DevExpress.XtraEditors.XtraForm
    {
        string NIK, STATUS, UNIT_KERJA,ket_pembayaran,jenis_pembayaran;
        int ID;
        double LIMIT_HUTANG;
        AnggotaController controller = new();
        public DTOPinjaman FakturPinjamanHeader { get; set; }
        public List<DTOPinjamanDetail> ListPinjamanDetail { get; set; }

        public frmEditFakturPinjaman()
        {
            InitializeComponent();
        }


        
        private void frmEditFaktur_Load(object sender, EventArgs e)
        {
            FinSettingsDataAccess finsetting = new();
            Load_angsuran(finsetting.GetMaxAngsuran());
            Load_Pelanggan();
            leangsuran.ItemIndex = FakturPinjamanHeader.TENOR;
            gridControl1.DataSource = ListPinjamanDetail.ToList();
            gridView1.OptionsBehavior.Editable = false;
            RepositoryItemCheckEdit edit = new()
            {
                ValueUnchecked = "T",
                ValueChecked = "Y"
            };
            // Sort the grid view by the TANGGAL column in ascending order
            gridView1.Columns["ANGSURANKE"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            gridView1.Columns["NO_TRANSAKSI"].VisibleIndex = -1;
            gridView1.Columns["SALDOAWAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["SALDOAWAL"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["POKOK"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["POKOK"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["BUNGA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["BUNGA"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["ANGSURAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["ANGSURAN"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["SALDOAKHIR"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["SALDOAKHIR"].DisplayFormat.FormatString = "N2";
            gridView1.Columns["ISTAGIH"].ColumnEdit = edit;
            txtfaktur.Text = FakturPinjamanHeader.NO_TRANSAKSI;
            txtpelanggan.Text = FakturPinjamanHeader.NAMA_PELANGGAN;
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
        private void Load_Pelanggan()
        {
            var datasource = controller.GetAnggotaAktif();
            searchLookUpEdit1.Properties.DataSource = datasource;
            searchLookUpEdit1.Properties.DisplayMember = "NAMA_PELANGGAN";
            searchLookUpEdit1.Properties.ValueMember = "NIK";
            // Set the default value for searchLookUpEdit1
            int index = datasource.FindIndex(item => item.NIK == FakturPinjamanHeader.NIK);
            if(index != -1 )
            {
                searchLookUpEdit1.EditValue = datasource[index].NIK;
            }
            else
            {
                XtraMessageBox.Show("NIK Anggota tidak ada pada data aktif", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }

        private void sbsimpancetak_Click(object sender, EventArgs e)
        {
            if (searchLookUpEdit1.EditValue != null)
            {
                if(searchLookUpEdit1.EditValue.ToString()== FakturPinjamanHeader.NIK)
                {
                    return;
                }
                SimpanFakturPenjualan();
                //cetak
                //CetakFaktur();
                XtraMessageBox.Show("Faktur Pinjaman berhasil diubah", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            FakturPinjamanHeader.ID_PELANGGAN = ID;
            FakturPinjamanHeader.NIK = NIK;
            FakturPinjamanHeader.NAMA_PELANGGAN = searchLookUpEdit1.Text;
            FakturPinjamanHeader.STATUS = STATUS;
            FakturPinjamanHeader.UNIT_KERJA = UNIT_KERJA;

            Finance_Services.EditFakturPinjaman(FakturPinjamanHeader);

        }
        private void CetakFaktur()
        {
            //rptFakturPenjualan report = new()
            //{
            //    DataSource = ListItemsPenjualan
            //};
            //report.Parameters["Nama_Toko"].Value = "KOPKAR - KUSUMA LESTARI";
            //report.Parameters["NoFaktur"].Value = FakturPenjualanHeader.NO_TRANSAKSI;
            //report.Parameters["Pelanggan"].Value = FakturPenjualanHeader.NIK+", "+FakturPenjualanHeader.NAMA_PELANGGAN.ToUpper()+" , "+ FakturPenjualanHeader.UNIT_KERJA;
            //report.Parameters["Jenis_Bayar"].Value = FakturPenjualanHeader.JENIS_BAYAR;
            //report.Parameters["Waktu"].Value = FakturPenjualanHeader.TANGGAL.ToString("dd-MMM-yy")+" "+ FakturPenjualanHeader.JAM;
            //report.Parameters["Tenor"].Value = FakturPenjualanHeader.TENOR;
            //report.Parameters["Bruto"].Value = FakturPenjualanHeader.BRUTO;
            //report.Parameters["Potongan"].Value = FakturPenjualanHeader.POTONGAN;
            //report.Parameters["Total"].Value = FakturPenjualanHeader.TOTAL;

            //report.Parameters["Kasir"].Value = "";            
            //report.Parameters["Bayar"].Value = 0;
            //report.Parameters["Kembalian"].Value = 0;
            
            //ReportPrintTool tool = new(report);
            //tool.Print();
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
                    DTOAnggotaAktif selectedObject = (DTOAnggotaAktif)searchLookUpEdit1.GetSelectedDataRow();

                    // Access the values from the selected object
                   
                    ID = Convert.ToInt32(selectedObject.ID_PELANGGAN);
                    NIK = selectedObject.NIK;
                    STATUS = selectedObject.STATUS;
                    UNIT_KERJA = selectedObject.UNITKERJA;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                //// Set the default value for searchLookUpEdit1
                //int index2 = datasource.FindIndex(item => item.NIK == "00.00004");

                //searchLookUpEdit1.EditValue = datasource[index2].NIK;
                //sbsimpancetak.Enabled = true;
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
        

        public static List<DTOSimulasiAngsuran> CalculateAngsuranPinjaman(string nomortransaksi,DateTime tanggalBelanja, decimal jumlahBelanja, int waktuangsuran)
        {
            List<DTOSimulasiAngsuran> listAngsuran = new();
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

                DTOSimulasiAngsuran angsuran = new ()
                {
                    //NO_TRANSAKSI = nomortransaksi,
                    //TANGGALJATUHTEMPO = tanggalJatuhTempo,
                    //ANGSURANKE = i,
                    //SALDOAWAL = Math.Round(saldoAwal, 2),
                    //ANGSURAN = Math.Round(P, 2),
                    //SALDOAKHIR = Math.Round(saldoAkhir, 2)
                };

                listAngsuran.Add(angsuran);
                saldoAwal = saldoAkhir;
            }

            return listAngsuran;
        }
    
    }
}