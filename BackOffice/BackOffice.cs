using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Laporan;
using BackOffice.Model;
using BackOffice.UC;
using BackOffice.UC.Persediaan;
using BackOffice.View;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraReports.UI;
using Oracle.ManagedDataAccess.Client;
using Pos.Shared.Auth;
using BackOffice.UI;

namespace BackOffice
{
    public partial class BackOffice : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        public BackOffice()
        {
            InitializeComponent();
            ShowLoggedInUser();
            SetupUserManagementMenu();
            ApplyRoleMenuVisibility();
            ConfigureSidebar();
            ConfigureModernShell();
        }

        private void ShowLoggedInUser()
        {
            string username = string.IsNullOrWhiteSpace(LoginInfo.userID)
                ? "-"
                : LoginInfo.userID;

            var userItem = new BarStaticItem
            {
                Alignment = BarItemLinkAlignment.Right,
                Caption = $"User: {username}",
                Name = "barLoggedInUser"
            };

            fluentDesignFormControl1.Items.Add(userItem);
            fluentDesignFormControl1.TitleItemLinks.Add(userItem);
        }

        private void ConfigureSidebar()
        {
            accordionControl1.Width = 264;
            accordionControl1.ResizeMode = AccordionControlResizeMode.None;
            accordionControl1.ScrollBarMode = ScrollBarMode.Touch;
            accordionControl1.ShowFilterControl = ShowFilterControl.Auto;

            accordionControlElementDashBoard.Text = "Dashboard";
            accordionControlElement30.Text = "Pengaturan";
            accordionControlElement7.Text = "Stok Opname";
            accordionControlElement15.Text = "Daftar Stok Opname";
            accordionControlElement16.Text = "Kartu Stok";

            HideUnavailableSidebarItems();
            ApplySidebarIcons();
            BackOfficeTheme.StyleSidebar(accordionControl1);

            foreach (AccordionControlElement group in SidebarGroups)
            {
                group.Expanded = false;
            }

            AccordionControlElement? initialGroup = SidebarGroups.FirstOrDefault(group => group.Visible);
            if (initialGroup is not null)
            {
                initialGroup.Expanded = true;
            }

            accordionControl1.ElementClick += AccordionControl1_ElementClick;
        }

        private AccordionControlElement[] SidebarGroups =>
        [
            Master,
            accordionControlElement2,
            Penjualan,
            Persediaa,
            accordionControlElement19,
            accordionControlElement3
        ];

        private void HideUnavailableSidebarItems()
        {
            accordionControlElementDashBoard.Visible = false;
            accordionControlSeparator1.Visible = false;
            accordionControlElement9.Visible = false;
            accordionControlElement33.Visible = false;
            accordionControlElement23.Visible = false;
        }

        private void ApplySidebarIcons()
        {
            SetSidebarIcon(accordionControlElement10, "▣");
            SetSidebarIcon(accordionControlElement11, "◆");
            SetSidebarIcon(accordionControlElement12, "●");
            SetSidebarIcon(accordionControlElement13, "↔");
            SetSidebarIcon(accordionControlElement14, "⌂");
            SetSidebarIcon(accordionControlElement5, "▤");

            SetSidebarIcon(accordionControlElement1, "↓");
            SetSidebarIcon(accordionControlElement8, "≡");

            SetSidebarIcon(accordionControlElementDaftarPenjualan, "≡");
            SetSidebarIcon(accordionControlElementTansaksiPending, "∿");
            SetSidebarIcon(accordionControlElement34, "↗");

            SetSidebarIcon(accordionControlElement24, "!");
            SetSidebarIcon(accordionControlElement7, "✓");
            SetSidebarIcon(accordionControlElement15, "≡");
            SetSidebarIcon(accordionControlElement4, "□");
            SetSidebarIcon(accordionControlElement16, "#");

            SetSidebarIcon(accordionControlElement32, "$");
            SetSidebarIcon(accordionControlElement21, "→");
            SetSidebarIcon(accordionControlElement20, "¤");
            SetSidebarIcon(accordionControlElement22, "+");
            SetSidebarIcon(accordionControlElement30, "⚙");
            SetSidebarIcon(accordionControlElement17, "▤");
            SetSidebarIcon(accordionControlElement27, "≡");
            SetSidebarIcon(accordionControlElement29, "↙");
            SetSidebarIcon(accordionControlElement28, "↙");
            SetSidebarIcon(accordionControlElement25, "◫");

            SetSidebarIcon(accordionControlElement18, "◷");
            SetSidebarIcon(acctutupbuku, "✓");
            SetSidebarIcon(accordionControlElement6, "✓");
        }

        private static void SetSidebarIcon(
            AccordionControlElement element,
            string glyph)
        {
            element.ImageOptions.Image = BackOfficeTheme.CreateSidebarIcon(glyph);
        }

        private void AccordionControl1_ElementClick(object sender, ElementClickEventArgs e)
        {
            AccordionControlElement? selectedGroup = FindContainingSidebarGroup(e.Element);
            if (selectedGroup is null)
            {
                return;
            }

            bool clickedTopLevelGroup = ReferenceEquals(e.Element, selectedGroup);
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                {
                    return;
                }

                if (!clickedTopLevelGroup)
                {
                    selectedGroup.Expanded = true;
                }

                if (!selectedGroup.Expanded)
                {
                    return;
                }

                foreach (AccordionControlElement group in SidebarGroups)
                {
                    if (!ReferenceEquals(group, selectedGroup))
                    {
                        group.Expanded = false;
                    }
                }
            }));
        }

        private AccordionControlElement? FindContainingSidebarGroup(AccordionControlElement element)
        {
            return SidebarGroups.FirstOrDefault(group =>
                ReferenceEquals(group, element) || ContainsElement(group, element));
        }

        private static bool ContainsElement(
            AccordionControlElement parent,
            AccordionControlElement target)
        {
            foreach (AccordionControlElement child in parent.Elements)
            {
                if (ReferenceEquals(child, target) || ContainsElement(child, target))
                {
                    return true;
                }
            }

            return false;
        }

        private void ConfigureModernShell()
        {
            BackColor = BackOfficeTheme.Canvas;
            fluentDesignFormContainer.BackColor = BackOfficeTheme.Canvas;
            fluentDesignFormContainer.Padding = new Padding(16, 16, 16, 0);
            fluentDesignFormContainer.ControlAdded += ContentContainer_ControlAdded;
            Resize += (_, _) => UpdateContentBounds();
            UpdateContentBounds();
            BackOfficeTheme.Apply(this);
        }

        private void ContentContainer_ControlAdded(object? sender, ControlEventArgs e)
        {
            Control? page = e.Control;
            if (page == null)
            {
                return;
            }

            page.Margin = Padding.Empty;
            page.Dock = DockStyle.Fill;
            page.BringToFront();
            BackOfficeTheme.Apply(page);
            BeginInvoke(UpdateContentBounds);
        }

        private void UpdateContentBounds()
        {
            int top = fluentDesignFormControl1.Bottom;
            int left = accordionControl1.Right;

            fluentDesignFormContainer.Dock = DockStyle.None;
            fluentDesignFormContainer.Anchor = AnchorStyles.None;
            fluentDesignFormContainer.SetBounds(
                left,
                top,
                Math.Max(0, ClientSize.Width - left),
                Math.Max(0, ClientSize.Height - top));

            foreach (Control control in fluentDesignFormContainer.Controls)
            {
                control.Margin = Padding.Empty;
                control.Dock = DockStyle.Fill;
            }
        }

        /// <summary>
        /// Daftar grup menu top-level yang boleh tampil per role.
        /// Role yang TIDAK terdaftar di sini menampilkan semua menu (mis. ADMIN).
        /// Nama grup harus cocok dengan label di <see cref="ApplyRoleMenuVisibility"/>.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> MenuByRole =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["PEMBELIAN"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Master", "Pembelian", "Persediaan"
                },
            };

        /// <summary>
        /// Tampilkan hanya grup menu yang sesuai akses (role) user. Role yang tidak
        /// terdaftar di <see cref="MenuByRole"/> melihat semua menu.
        /// </summary>
        private void ApplyRoleMenuVisibility()
        {
            if (LoginInfo.HasFullAccess)
            {
                return;
            }

            if (!MenuByRole.TryGetValue(LoginInfo.role ?? string.Empty, out HashSet<string>? allowed))
            {
                return; // role tidak dibatasi -> tampilkan semua menu
            }

            (string Name, DevExpress.XtraBars.Navigation.AccordionControlElement Element)[] groups =
            {
                ("DashBoard",  accordionControlElementDashBoard),
                ("Master",     Master),
                ("Pembelian",  accordionControlElement2),
                ("Penjualan",  Penjualan),
                ("Persediaan", Persediaa),
                ("Keuangan",   accordionControlElement19),
                ("Pengaturan", accordionControlElement3),
            };

            foreach ((string name, var element) in groups)
            {
                element.Visible = allowed.Contains(name);
            }

            // Separator hanya relevan saat DashBoard tampil.
            accordionControlSeparator1.Visible = allowed.Contains("DashBoard");
        }

        /// <summary>
        /// Tambah menu "Manajemen User" secara dinamis, hanya untuk role ADMIN.
        /// </summary>
        private void SetupUserManagementMenu()
        {
            if (!LoginInfo.HasFullAccess)
            {
                return;
            }

            var element = new DevExpress.XtraBars.Navigation.AccordionControlElement
            {
                Name = "accordionControlManajemenUser",
                Style = DevExpress.XtraBars.Navigation.ElementStyle.Item,
                Text = "Manajemen User"
            };
            SetSidebarIcon(element, "♙");
            element.Click += (_, _) =>
            {
                using var frm = new View.frmUserManagement();
                frm.ShowDialog(this);
            };
            accordionControlElement3.Elements.Add(element);
        }
        DateTime MAXBULANTAGIHAN;

        /// <summary>
        /// True jika user yang login punya akses ke modul Pembelian (APP_ID PEMBELIAN).
        /// Menampilkan pesan dan return false bila tidak.
        /// </summary>
        private static bool HasPembelianAccess()
        {
            if (LoginInfo.HasFullAccess
                || string.Equals(LoginInfo.role, "PEMBELIAN", StringComparison.OrdinalIgnoreCase)
                || LoginInfo.AccessibleApps.Contains(AppIds.Pembelian))
            {
                return true;
            }

            XtraMessageBox.Show(
                "Anda tidak punya akses ke modul Pembelian.",
                "Akses Ditolak",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>True jika role user yang login termasuk salah satu <paramref name="roles"/>.</summary>
        private static bool HasRole(params string[] roles)
        {
            if (LoginInfo.HasFullAccess)
            {
                return true;
            }

            foreach (string role in roles)
            {
                if (string.Equals(LoginInfo.role, role, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }


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
            UpdateContentBounds();
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
            if (HasRole("ADMIN", "MANAGER"))
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
            if (!HasPembelianAccess())
            {
                return;
            }

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
            if (!HasPembelianAccess())
            {
                return;
            }

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
            if (HasRole("ADMIN", "MANAGER"))
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

        private void accordionControlElement11_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucSupplier.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucSupplier.Instance);
                ucSupplier.Instance.Dock = DockStyle.Fill;
                ucSupplier.Instance.BringToFront();
            }
            else
                ucSupplier.Instance.BringToFront();
        }
    }
}
