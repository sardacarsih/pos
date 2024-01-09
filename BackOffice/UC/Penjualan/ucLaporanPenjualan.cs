using BackOffice.BussinessLayer;
using BackOffice.Laporan;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSplashScreen;

namespace BackOffice.UC
{
    public partial class ucLaporanPenjualan : UserControl
    {

        //Using singleton pattern to create an instance to ucModule3
        private static ucLaporanPenjualan? _instance;
        public static ucLaporanPenjualan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucLaporanPenjualan();
                return _instance;
            }
        }
        public ucLaporanPenjualan()
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
                            var jualkredit = LaporanManager.PenjualanKredit(daritanggal, sampaitanggal);
                            report = new rptPenjualanKredit
                            {
                                DataSource = jualkredit,
                                RequestParameters = true
                            };
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
