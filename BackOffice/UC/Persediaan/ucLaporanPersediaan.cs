using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.Charts.Model;
using DevExpress.CodeParser;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSplashScreen;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackOffice.UC
{
    public partial class ucLaporanPersediaan : UserControl
    {

        //Using singleton pattern to create an instance to ucModule3
        private static ucLaporanPersediaan? _instance;
        // Create an instance of PersediaanController
        PersediaanController controller = new PersediaanController();
        public static ucLaporanPersediaan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucLaporanPersediaan();
                return _instance;
            }
        }
        public ucLaporanPersediaan()
        {
            InitializeComponent();
        }

        private void ucLaporanPenjualan_Load(object sender, EventArgs e)
        {
            DateTime date = DateTime.Today;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddSeconds(-1);
            dateEdit1.DateTime= firstDayOfMonth;
            dateEdit2.DateTime = lastDayOfMonth;
            Load_Nama_barang();
        }

        private void Load_Nama_barang()
        {
            // Call the FillDictionaryFromDatabase method
            //Dictionary<string, string> daftarbarang = controller.FillDictionaryFromDatabase();
            //searchLookUpEdit1.Properties.DataSource = daftarbarang;
            //searchLookUpEdit1.Properties.ValueMember = "Key";
            //searchLookUpEdit1.Properties.DisplayMember= "Value";
           
        }

        private void btncetak_Click(object sender, EventArgs e)
        {
            using var handle = SplashScreenManager.ShowOverlayForm(this);
            try
            {
                if (DateTime.TryParse(dateEdit1.Text, out DateTime daritanggal) && DateTime.TryParse(dateEdit2.Text, out DateTime sampaitanggal))
                {
                    XtraReport report = null;

                    switch (radioGroup1.SelectedIndex)
                    {
                        case 0:
                            var jualtunai = LaporanManager.PenjualanTunai(daritanggal, sampaitanggal);
                            report = new rptPenjualanTunai
                            {
                                DataSource = jualtunai,
                                RequestParameters = true
                            };
                            break;
                        case 1:
                            var jualtempo = LaporanManager.PenjualanTempo(daritanggal, sampaitanggal);
                            report = new rptPenjualanTempo
                            {
                                DataSource = jualtempo,
                                RequestParameters = true
                            };
                            break;
                        case 2:
                            //var kartustok = controller.KartuStokBarang((int)searchLookUpEdit1.EditValue,daritanggal, sampaitanggal);
                            //report = new rptLaporanKartuStokBarang
                            //{
                            //    DataSource = kartustok,
                            //    RequestParameters = true
                            //};
                            break;
                        default:
                            // Handle the case where no option is selected
                            break;
                    }

                    if (report != null)
                    {
                        report.Parameters["dari"].Value = daritanggal;
                        report.Parameters["sampai"].Value = sampaitanggal;
                        report.ShowPreviewDialog();
                    }
                }
                else
                {
                    // Handle the case where date parsing fails
                }
            }
            catch (Exception ex)
            {
                // Handle any other exceptions that may occur during report generation
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
