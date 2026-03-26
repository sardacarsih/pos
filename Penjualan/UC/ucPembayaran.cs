using QRCoder;
namespace Penjualan.UC
{
    public partial class ucPembayaran : UserControl
    {
        //Using singleton pattern to create an instance to ucModule3
        private static ucPembayaran _instance;
        public static ucPembayaran Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPembayaran();
                return _instance;
            }
        }
        public ucPembayaran()
        {
            InitializeComponent();
        }

        private void Pembayaran_Load(object sender, EventArgs e)
        {
            // Generate QR code
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("INISIALISASI_TRANSAKSI", QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new (qrCodeData);

            // Display QR code in picture box
            pictureBox1.Image = qrCode.GetGraphic(20);

        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
           
        }
    }
}
