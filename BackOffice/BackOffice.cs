using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Laporan;
using BackOffice.Model;
using BackOffice.UC;
using BackOffice.UC.Persediaan;
using BackOffice.View;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice
{
    public partial class BackOffice : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        private readonly RBACController rbacController;
        public BackOffice()
        {
            InitializeComponent();
            IRBACManager repository = new RBACManager(); // Initialize your repository implementation
            rbacController = new RBACController(repository);
        }
        DateTime MAXBULANTAGIHAN;


        private void accordionControlElementDaftarPenjualan_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Penjualan";

            // Create a new instance of ucDaftarPembelian
            ucDaftarPenjualan newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }

        private void accordionControlElementTansaksiPending_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Analisa Laba/Rugi Penjualan";
            // Create a new instance of ucDaftarPembelian
            ucAnalisaLabaRugi newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void accordionControlElementReturPenjualan_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucReturPenjualan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucReturPenjualan.Instance);
                ucReturPenjualan.Instance.Dock = DockStyle.Fill;
                ucReturPenjualan.Instance.BringToFront();
            }
            else
                ucReturPenjualan.Instance.BringToFront();
        }

        private void accordionControlElementLaporan_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucLaporan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucLaporan.Instance);
                ucLaporan.Instance.Dock = DockStyle.Fill;
                ucLaporan.Instance.BringToFront();
            }
            else
                ucLaporan.Instance.BringToFront();
        }

        private void accordionControlElementKeluar_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Keluar dari Aplikasi Penjualan Kasir ? ",
         "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.No)
            { return; }
            else
            {
                this.Close();
                Application.Exit();
            }

        }

        private void accordionControlElementDashBoard_Click(object sender, EventArgs e)
        {
            //// Change the form's text
            //this.Text = "Dashboard";
            ////Add module1 to panel control
            //// Create a new instance of ucDaftarPembelian
            //ucDashBoard newInstance = new();

            //// Check if the fluentDesignFormContainer already contains a control
            //if (fluentDesignFormContainer.Controls.Count > 0)
            //{
            //    // Remove the existing control from the container
            //    fluentDesignFormContainer.Controls.RemoveAt(0);
            //}

            //// Add the new instance to the panel control
            //fluentDesignFormContainer.Controls.Add(newInstance);
            //newInstance.Dock = DockStyle.Fill;
            //newInstance.BringToFront();
        }

        private void accordionControlElement10_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucDaftarBarang.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucDaftarBarang.Instance);
                ucDaftarBarang.Instance.Dock = DockStyle.Fill;
                ucDaftarBarang.Instance.BringToFront();
            }
            else
                ucDaftarBarang.Instance.BringToFront();
        }

        private void accordionControl1_SizeChanged(object sender, EventArgs e)
        {

            if (accordionControl1.Size.Width == 0)
            {
                // Set the position and size of the fluentDesignFormContainer when the accordion is collapsed
                fluentDesignFormContainer.Dock = DockStyle.Fill;
            }
            else
            {
                // Set the position and size of the fluentDesignFormContainer when the accordion is expanded
                fluentDesignFormContainer.Dock = DockStyle.None;
                fluentDesignFormContainer.Left = accordionControl1.Width + 10;
                fluentDesignFormContainer.Width = this.ClientSize.Width - accordionControl1.Width - 10;
                fluentDesignFormContainer.Height = this.ClientSize.Height;
            }
        }

        private void accordionControlElement13_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucSatuan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucSatuan.Instance);
                ucSatuan.Instance.Dock = DockStyle.Fill;
                ucSatuan.Instance.BringToFront();
            }
            else
                ucSatuan.Instance.BringToFront();
        }

        private void accordionControlElement14_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucUnitKerja.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucUnitKerja.Instance);
                ucUnitKerja.Instance.Dock = DockStyle.Fill;
                ucUnitKerja.Instance.BringToFront();
            }
            else
                ucUnitKerja.Instance.BringToFront();
        }

        private void accordionControlElement17_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Laporan Tagihan";
            //Add module1 to panel control
            // Create a new instance of ucDaftarPembelian
            ucTagihan newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }

        private void acctutupbuku_Click(object sender, EventArgs e)
        {
            string username = "admin";
            string permissionName = "Tutup Buku Periode";

            if (rbacController.CheckPermission(username, permissionName))
            {
                // User has the required permission, perform the action
                FrmClosing frm = new()
                {
                    StartPosition = FormStartPosition.CenterScreen
                };
                frm.ShowDialog();
            }
            else
            {

                // Pengguna tidak memiliki izin yang diperlukan, tampilkan pesan kesalahan
                string pesanKesalahan = "Anda tidak memiliki izin untuk melakukan tindakan ini.";
                XtraMessageBox.Show(pesanKesalahan, "Akses Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void accordionControlElement18_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Periode";
            //Add module1 to panel control
            // Create a new instance of ucDaftarPembelian
            ucPeriode newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }

        private void accordionControlElement16_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Laporan Penjualan Tunai";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPenjualanTunai.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPenjualanTunai.Instance);
                ucPenjualanTunai.Instance.Dock = DockStyle.Fill;
                ucPenjualanTunai.Instance.BringToFront();
            }
            else
                ucPenjualanTunai.Instance.BringToFront();
        }

        private void accordionControlElement22_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Penerimaan Pembayaran";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPenerimaanPembayaran.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPenerimaanPembayaran.Instance);
                ucPenerimaanPembayaran.Instance.Dock = DockStyle.Fill;
                ucPenerimaanPembayaran.Instance.BringToFront();
            }
            else
                ucPenerimaanPembayaran.Instance.BringToFront();
        }

        private void accordionControlElement20_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Pinjaman Tunai";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPinjaman.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPinjaman.Instance);
                ucPinjaman.Instance.Dock = DockStyle.Fill;
                ucPinjaman.Instance.BringToFront();
            }
            else
                ucPinjaman.Instance.BringToFront();

        }

        private void accordionControlElement27_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Pinjaman";
            // Create a new instance of ucDaftarPembelian
            ucDaftarPinjaman newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }

        private void accordionControlElement29_Click(object sender, EventArgs e)
        {


            // Panggil metode DaftarPinjaman dengan bulan dan tahun yang diinginkan
            List<DTOPinjamanMaster> daftarPiutangPinjaman = LaporanManager.GetPiutangPinjaman();
            XtraReport report1 = new rptPinjamanTunaiPiutang
            {
                DataSource = daftarPiutangPinjaman,
                RequestParameters = true
            };
            report1.Parameters["MAXPERIODE"].Value = MAXBULANTAGIHAN = GetMaxDateFromTagihanPinjaman();
            report1.ShowPreview();
        }

        private static DateTime GetMaxDateFromTagihanPinjaman()
        {
            string query = "SELECT MAX(TGLTAGIH) FROM FIN_TAGIHAN_PINJAMAN";

            using (OracleConnection connection = new(global.connectionString))
            {
                using OracleCommand command = new(query, connection);
                connection.Open();
                object result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDateTime(result);
                }
                else
                {
                    // Return today's date as the default value
                    return DateTime.Today;
                }
            }

            throw new Exception("No data found or error occurred while retrieving the maximum date.");
        }

        private void accordionControlElement28_Click(object sender, EventArgs e)
        {


            // Panggil metode DaftarPinjaman dengan bulan dan tahun yang diinginkan
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> daftarPiutangkreditbarang = LaporanManager.GetPiutangKreditBarangBelumLunas();
            XtraReport report1 = new rptKreditBarangPiutang
            {
                DataSource = daftarPiutangkreditbarang,
                RequestParameters = true
            };
            report1.Parameters["MAXPERIODE"].Value = MAXBULANTAGIHAN = GetMaxDateFromTagihanPinjaman();
            report1.ShowPreview();
        }

        private void accordionControlElement30_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Pengaturan";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucSettings.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucSettings.Instance);
                ucSettings.Instance.Dock = DockStyle.Fill;
                ucSettings.Instance.BringToFront();
            }
            else
                ucSettings.Instance.BringToFront();
        }


        private void accordionControlElement1_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Laporan Penjualan";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucLaporanMaster.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucLaporanMaster.Instance);
                ucLaporanMaster.Instance.Dock = DockStyle.Fill;
                ucLaporanMaster.Instance.BringToFront();
            }
            else
                ucLaporanMaster.Instance.BringToFront();

        }

        private void accordionControlElement12_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Anggota / Pelanggan";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucMasterAnggota.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucMasterAnggota.Instance);
                ucMasterAnggota.Instance.Dock = DockStyle.Fill;
                ucMasterAnggota.Instance.BringToFront();
            }
            else
                ucMasterAnggota.Instance.BringToFront();
        }

        private void accordionControlElement4_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Persediaan";

            // Create a new instance of ucDaftarPembelian
            ucDaftarPersediaan newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void accordionControlElement5_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Laporan Master";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucLaporanMaster.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucLaporanMaster.Instance);
                ucLaporanMaster.Instance.Dock = DockStyle.Fill;
                ucLaporanMaster.Instance.BringToFront();
            }
            else
                ucLaporanMaster.Instance.BringToFront();
        }

        private void BackOffice_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if the user clicked the close button (X) or used Alt+F4
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = XtraMessageBox.Show("Anda akan keluar dari Program ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Cancel the form closing
                }
                else if (result == DialogResult.Yes)
                {
                    Application.Exit(); // Exit the application
                }
            }
        }

        private void accordionControlElement1_Click_1(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Pembelian";
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPembelian.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPembelian.Instance);
                ucPembelian.Instance.Dock = DockStyle.Fill;
                ucPembelian.Instance.BringToFront();
            }
            else
                ucPembelian.Instance.BringToFront();
        }

        private void accordionControlElement6_Click(object sender, EventArgs e)
        {
            frmfixedform f = new();
            f.ShowDialog();
        }

        private void accordionControlElement8_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Pembelian";

            // Create a new instance of ucDaftarPembelian
            ucDaftarPembelian newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }

        private void accordionControlElement7_Click(object sender, EventArgs e)
        {
            //// Change the form's text
            //this.Text = "Stock Opname";

            //// Create a new instance of ucDaftarPembelian
            //ucStockOpname newInstance = new();

            //// Check if the fluentDesignFormContainer already contains a control
            //if (fluentDesignFormContainer.Controls.Count > 0)
            //{
            //    // Remove the existing control from the container
            //    fluentDesignFormContainer.Controls.RemoveAt(0);
            //}

            //// Add the new instance to the panel control
            //fluentDesignFormContainer.Controls.Add(newInstance);
            //newInstance.Dock = DockStyle.Fill;
            //newInstance.BringToFront();

            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucStockOpname.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucStockOpname.Instance);
                ucStockOpname.Instance.Dock = DockStyle.Fill;
                ucStockOpname.Instance.BringToFront();
            }
            else
                ucStockOpname.Instance.BringToFront();
        }

        private void accordionControlElement15_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Stock Opname";

            // Create a new instance of ucDaftarPembelian
            ucDaftarSO newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void accordionControlElement16_Click_1(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Kartu Stock";

            // Create a new instance of ucDaftarPembelian
            ucKartuStock newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void accordionControlElement24_Click(object sender, EventArgs e)
        {
            //// Change the form's text
            //this.Text = "Barang Rusak";

            //// Create a new instance of ucDaftarPembelian
            //ucBarangRusak newInstance = new();

            //// Check if the fluentDesignFormContainer already contains a control
            //if (fluentDesignFormContainer.Controls.Count > 0)
            //{
            //    // Remove the existing control from the container
            //    fluentDesignFormContainer.Controls.RemoveAt(0);
            //}

            //// Add the new instance to the panel control
            //fluentDesignFormContainer.Controls.Add(newInstance);
            //newInstance.Dock = DockStyle.Fill;
            //newInstance.BringToFront();

            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucBarangRusak.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucBarangRusak.Instance);
                ucBarangRusak.Instance.Dock = DockStyle.Fill;
                ucBarangRusak.Instance.BringToFront();
            }
            else
                ucBarangRusak.Instance.BringToFront();


        }

        private void accordionControlElement25_Click(object sender, EventArgs e)
        {
            FrmJurnal frmJurnal = new FrmJurnal();
            //frmJurnal.TopLevel = false;
            //frmJurnal.FormBorderStyle = FormBorderStyle.None;
            //frmJurnal.Dock = DockStyle.Fill;

            int centerX = (Screen.PrimaryScreen.Bounds.Width - frmJurnal.Width) / 2;
            int centerY = (Screen.PrimaryScreen.Bounds.Height - frmJurnal.Height) / 2;

            // Set the location of the FluentDesignFormContainer
            frmJurnal.Location = new Point(centerX, centerY);
            // Add the form to the FluentDesignFormContainer
            //fluentDesignFormContainer.Controls.Add(frmJurnal);
            frmJurnal.Show();

        }

        private void accordionControlElement21_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Pembayaran";
            //Add module1 to panel control
            // Create a new instance of ucDaftarPembelian
            ucKAS_AddEdit newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void accordionControlElement32_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Daftar Transaksi KAS";
            //Add module1 to panel control
            // Create a new instance of ucDaftarPembelian
            ucDaftarKAS newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }

        private void BackOffice_Load(object sender, EventArgs e)
        {
            ////Add module1 to panel control
            //if (!fluentDesignFormContainer.Controls.Contains(ucDashBoard.Instance))
            //{
            //    fluentDesignFormContainer.Controls.Add(ucDashBoard.Instance);
            //    ucDashBoard.Instance.Dock = DockStyle.Fill;
            //    ucDashBoard.Instance.BringToFront();
            //}
            //else
            //    ucDashBoard.Instance.BringToFront();
        }

        private void accordionControlElement6_Click_1(object sender, EventArgs e)
        {
            string username = "admin";
            string permissionName = "Tutup Buku Periode";

            if (rbacController.CheckPermission(username, permissionName))
            {
                // User has the required permission, perform the action
                FrmClosingYear frm = new()
                {
                    StartPosition = FormStartPosition.CenterScreen
                };
                frm.ShowDialog();
            }
            else
            {

                // Pengguna tidak memiliki izin yang diperlukan, tampilkan pesan kesalahan
                string pesanKesalahan = "Anda tidak memiliki izin untuk melakukan tindakan ini.";
                XtraMessageBox.Show(pesanKesalahan, "Akses Ditolak", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void accordionControlElement34_Click(object sender, EventArgs e)
        {
            // Change the form's text
            this.Text = "Laba/Rugi Penjualan";
            // Create a new instance of ucDaftarPembelian
            ucLabaRugi newInstance = new();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();
        }
    }
}
