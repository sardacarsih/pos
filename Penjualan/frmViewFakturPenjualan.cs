using DevExpress.XtraReports.UI;
using Penjualan.Laporan;
using Penjualan.Model;

namespace Penjualan
{
    public partial class frmViewFakturPenjualan : DevExpress.XtraEditors.XtraForm
    {
        public DTOFakturPenjualanHeader FakturPenjualanHeader { get; set; }
        public List<DTODaftarBarang> ListItemsPenjualan { get; set; }

        public frmViewFakturPenjualan()
        {
            InitializeComponent();
        }

        private void frmViewFakturPenjualan_Load(object sender, EventArgs e)
        {
            txtfaktur.Text = FakturPenjualanHeader.NO_TRANSAKSI;
            txtpelanggan.Text = FakturPenjualanHeader.NAMA_PELANGGAN;
            txtangsuran.Text = FakturPenjualanHeader.TENOR.ToString();
            gridControl1.DataSource = ListItemsPenjualan.ToList();
        }
    }
}