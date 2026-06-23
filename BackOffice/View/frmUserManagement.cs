using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using BackOffice.DataLayer;
using BackOffice.UI;
using Pos.Shared.Auth;

namespace BackOffice.View
{
    /// <summary>
    /// Layar manajemen user: buat/edit user, set/reset password (hash PBKDF2),
    /// aktif/lock, dan assign akses per aplikasi (POS_USER_ACCESS) dengan role.
    /// UI dibangun programatik (tanpa designer) agar mudah dipelihara.
    /// </summary>
    public class frmUserManagement : XtraForm
    {
        // Palet warna sederhana agar tampilan konsisten dengan skin terang.
        private static readonly Color Good = Color.FromArgb(46, 160, 67);
        private static readonly Color Bad = Color.FromArgb(201, 64, 64);

        private readonly AuthRepository _auth = new(global.connectionString);
        private readonly PasswordCryptographyPbkdf2 _crypto = new();

        private readonly GridControl _userGrid = new();
        private readonly GridView _userView;
        private readonly TextEdit _txtUsername = new();
        private readonly TextEdit _txtFullName = new();
        private readonly LabelControl _lblStatus = new();

        private readonly GridControl _accessGrid = new();
        private readonly GridView _accessView;
        private readonly LookUpEdit _cmbApp = new();
        private readonly LookUpEdit _cmbRole = new();

        private GroupControl _grpDetail = null!;

        private List<UserAccount> _users = new();
        private int? _selectedUserId;

        public frmUserManagement()
        {
            _userView = new GridView(_userGrid);
            _accessView = new GridView(_accessGrid);

            Text = "Manajemen User";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1060, 660);
            MinimumSize = new Size(900, 580);
            AutoScaleMode = AutoScaleMode.Dpi;

            BuildLayout();
            BackOfficeTheme.StyleGrid(_userView);
            BackOfficeTheme.StyleGrid(_accessView);
            LoadLookups();
            LoadUsers();
        }

        private void BuildLayout()
        {
            // ---- Split utama: daftar user (kiri) | detail (kanan) ----
            var split = new SplitContainerControl
            {
                Dock = DockStyle.Fill,
                Horizontal = true,
                SplitterPosition = 320,
                FixedPanel = SplitFixedPanel.Panel1
            };
            split.Panel1.MinSize = 280;
            split.Panel2.MinSize = 560;
            Controls.Add(split);

            BuildUserListPanel(split.Panel1);
            BuildDetailPanel(split.Panel2);
        }

        // ------------------------------------------------------- panel daftar user

        private void BuildUserListPanel(SplitGroupPanel host)
        {
            var grp = new GroupControl
            {
                Text = "Daftar User",
                Dock = DockStyle.Fill
            };
            grp.Padding = new Padding(2, 0, 2, 2);
            host.Controls.Add(grp);

            // Footer aksi
            var footer = new PanelControl
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            footer.Padding = new Padding(8);
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            flow.Controls.Add(MakeButton("Refresh", (_, _) => LoadUsers(), 110));
            flow.Controls.Add(MakeButton("User Baru", (_, _) => CreateUser(), 120, primary: true));
            footer.Controls.Add(flow);

            // Grid user
            _userGrid.MainView = _userView;
            _userGrid.Dock = DockStyle.Fill;
            _userView.OptionsBehavior.Editable = false;
            _userView.OptionsView.ShowGroupPanel = false;
            _userView.OptionsView.ShowIndicator = false;
            _userView.OptionsView.EnableAppearanceEvenRow = true;
            _userView.OptionsSelection.EnableAppearanceFocusedCell = false;
            _userView.OptionsFind.AlwaysVisible = true;
            _userView.OptionsFind.FindNullPrompt = "Cari user...";
            _userView.FocusedRowChanged += (_, _) => OnUserSelected();

            grp.Controls.Add(_userGrid);
            grp.Controls.Add(footer);
            // Sisakan ruang header GroupControl.
            _userGrid.BringToFront();
        }

        // ------------------------------------------------------------ panel detail

        private void BuildDetailPanel(SplitGroupPanel host)
        {
            // --- Bagian identitas & status (atas) ---
            var grpId = new GroupControl
            {
                Text = "Identitas & Status Akun",
                Dock = DockStyle.Top,
                Height = 230
            };
            grpId.Padding = new Padding(2, 0, 2, 2);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(14, 12, 14, 10)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtUsername.Properties.ReadOnly = true;
            _txtUsername.Dock = DockStyle.Fill;
            _txtFullName.Properties.NullValuePrompt = "Nama lengkap user";
            _txtFullName.Dock = DockStyle.Fill;

            table.Controls.Add(MakeFieldLabel("Username"), 0, 0);
            table.Controls.Add(_txtUsername, 1, 0);
            table.Controls.Add(MakeFieldLabel("Nama Lengkap"), 0, 1);
            table.Controls.Add(_txtFullName, 1, 1);

            // Baris aksi identitas
            var idActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                Margin = new Padding(0, 5, 0, 3),
                Padding = Padding.Empty
            };
            idActions.Controls.Add(MakeButton("Simpan Nama", (_, _) => SaveFullName(), 120, primary: true));
            idActions.Controls.Add(MakeButton("Reset Password", (_, _) => ResetPassword(), 130));
            idActions.Controls.Add(MakeButton("Aktif / Nonaktif", (_, _) => ToggleActive(), 130));
            idActions.Controls.Add(MakeButton("Lock / Unlock", (_, _) => ToggleLocked(), 120));
            table.Controls.Add(idActions, 1, 2);

            // Baris status
            table.Controls.Add(MakeFieldLabel("Status"), 0, 3);
            _lblStatus.AllowHtmlString = true;
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
            table.Controls.Add(_lblStatus, 1, 3);

            grpId.Controls.Add(table);

            // --- Bagian akses (mengisi sisa) ---
            var grpAccess = new GroupControl
            {
                Text = "Akses Aplikasi & Role",
                Dock = DockStyle.Fill
            };
            grpAccess.Padding = new Padding(2, 0, 2, 2);

            var accessFooter = BuildAccessFooter();

            _accessGrid.MainView = _accessView;
            _accessGrid.Dock = DockStyle.Fill;
            _accessView.OptionsBehavior.Editable = false;
            _accessView.OptionsView.ShowGroupPanel = false;
            _accessView.OptionsView.ShowIndicator = false;
            _accessView.OptionsView.EnableAppearanceEvenRow = true;

            grpAccess.Controls.Add(_accessGrid);
            grpAccess.Controls.Add(accessFooter);
            _accessGrid.BringToFront();

            // Container detail agar bisa di-disable saat tidak ada user terpilih.
            _grpDetail = new GroupControl
            {
                Dock = DockStyle.Fill,
                ShowCaption = false,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            _grpDetail.Controls.Add(grpAccess);
            _grpDetail.Controls.Add(grpId);

            host.Controls.Add(_grpDetail);
        }

        private PanelControl BuildAccessFooter()
        {
            var footer = new PanelControl
            {
                Dock = DockStyle.Bottom,
                Height = 94,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            footer.Padding = new Padding(10, 6, 10, 8);

            var stack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.Controls.Add(MakeFieldLabel("Tambah / ubah akses:"), 0, 0);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 0)
            };

            _cmbApp.Properties.DisplayMember = "AppName";
            _cmbApp.Properties.ValueMember = "AppId";
            _cmbApp.Properties.NullText = "Pilih aplikasi...";
            _cmbApp.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AppName", "Aplikasi"));
            _cmbApp.Width = 240;
            _cmbApp.Margin = new Padding(0, 2, 8, 2);

            _cmbRole.Properties.DisplayMember = "RoleName";
            _cmbRole.Properties.ValueMember = "RoleId";
            _cmbRole.Properties.NullText = "Pilih role...";
            _cmbRole.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("RoleName", "Role"));
            _cmbRole.Width = 170;
            _cmbRole.Margin = new Padding(0, 2, 8, 2);

            flow.Controls.Add(_cmbApp);
            flow.Controls.Add(_cmbRole);
            flow.Controls.Add(MakeButton("Beri / Ubah Akses", (_, _) => GrantAccess(), 150, primary: true));
            flow.Controls.Add(MakeButton("Cabut Akses", (_, _) => RevokeAccess(), 120));

            stack.Controls.Add(flow, 0, 1);
            footer.Controls.Add(stack);
            return footer;
        }

        // ------------------------------------------------------------ helpers UI

        private static LabelControl MakeFieldLabel(string text)
        {
            var l = new LabelControl { Text = text, Dock = DockStyle.Fill };
            l.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            return l;
        }

        private SimpleButton MakeButton(string text, EventHandler onClick, int width, bool primary = false)
        {
            var b = new SimpleButton
            {
                Text = text,
                Width = width,
                Height = 38,
                Margin = new Padding(0, 0, 8, 4)
            };
            if (primary)
            {
                BackOfficeTheme.StylePrimaryButton(b);
            }
            else
            {
                BackOfficeTheme.StyleSecondaryButton(b);
            }
            b.Click += onClick;
            return b;
        }

        // --------------------------------------------------------------- loading

        private void LoadLookups()
        {
            _cmbApp.Properties.DataSource = _auth.ListApps();
            _cmbRole.Properties.DataSource = _auth.ListRoles();
        }

        private void LoadUsers()
        {
            _users = _auth.ListUsers();
            _userGrid.DataSource = _users;
            _userView.PopulateColumns();
            SetColumnCaption("UserId", null, false);
            SetColumnCaption("Username", "Username", true);
            SetColumnCaption("FullName", "Nama", true);
            SetColumnCaption("IsActive", "Aktif", true);
            SetColumnCaption("IsLocked", "Terkunci", true);
            SetColumnCaption("FailedAttempts", "Gagal", true);
            _userView.BestFitColumns();
            OnUserSelected();
        }

        private void SetColumnCaption(string field, string? caption, bool visible)
        {
            var col = _userView.Columns.ColumnByFieldName(field);
            if (col is null)
            {
                return;
            }
            col.Visible = visible;
            if (caption is not null)
            {
                col.Caption = caption;
            }
        }

        private void OnUserSelected()
        {
            if (_userView.GetFocusedRow() is UserAccount u)
            {
                _selectedUserId = u.UserId;
                _grpDetail.Enabled = true;
                _txtUsername.Text = u.Username;
                _txtFullName.Text = u.FullName ?? string.Empty;
                UpdateStatusLabel(u);
                _accessGrid.DataSource = _auth.GetUserAccess(u.UserId);
                _accessView.PopulateColumns();
                ApplyAccessColumns();
            }
            else
            {
                _selectedUserId = null;
                _grpDetail.Enabled = false;
                _txtUsername.Text = string.Empty;
                _txtFullName.Text = string.Empty;
                _lblStatus.Text = string.Empty;
                _accessGrid.DataSource = null;
            }
        }

        private void UpdateStatusLabel(UserAccount u)
        {
            string active = u.IsActive
                ? $"<color={Good.R},{Good.G},{Good.B}>● Aktif</color>"
                : $"<color={Bad.R},{Bad.G},{Bad.B}>● Nonaktif</color>";
            string locked = u.IsLocked
                ? $"<color={Bad.R},{Bad.G},{Bad.B}>● Terkunci</color>"
                : $"<color={Good.R},{Good.G},{Good.B}>● Terbuka</color>";
            string fail = u.FailedAttempts > 0
                ? $"<color={Bad.R},{Bad.G},{Bad.B}>Gagal login: {u.FailedAttempts}</color>"
                : "Gagal login: 0";
            _lblStatus.Text = $"{active}     {locked}     {fail}";
        }

        private void ApplyAccessColumns()
        {
            SetAccessColumn("UserId", null, false);
            SetAccessColumn("RoleId", null, false);
            SetAccessColumn("AppId", "Kode App", true);
            SetAccessColumn("AppName", "Aplikasi", true);
            SetAccessColumn("RoleName", "Role", true);
            SetAccessColumn("IsActive", "Aktif", true);
            _accessView.BestFitColumns();
        }

        private void SetAccessColumn(string field, string? caption, bool visible)
        {
            var col = _accessView.Columns.ColumnByFieldName(field);
            if (col is null)
            {
                return;
            }
            col.Visible = visible;
            if (caption is not null)
            {
                col.Caption = caption;
            }
        }

        // --------------------------------------------------------------- actions

        private void CreateUser()
        {
            string username = XtraInputBox.Show("Username baru:", "User Baru", string.Empty)?.Trim().ToLower() ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                return;
            }
            if (_auth.UsernameExists(username))
            {
                XtraMessageBox.Show("Username sudah dipakai.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string fullName = XtraInputBox.Show("Nama lengkap:", "User Baru", string.Empty) ?? string.Empty;
            string password = XtraInputBox.Show("Password awal:", "User Baru", string.Empty) ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                XtraMessageBox.Show("Password tidak boleh kosong.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _auth.InsertUser(username, fullName, _crypto.GetHashPassword(password));
            XtraMessageBox.Show("User dibuat. Jangan lupa beri akses aplikasi & role.", "Sukses",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }

        private void SaveFullName()
        {
            if (_selectedUserId is not int id)
            {
                return;
            }
            _auth.UpdateUser(id, _txtFullName.Text.Trim());
            LoadUsers();
        }

        private void ResetPassword()
        {
            if (_selectedUserId is not int id)
            {
                return;
            }
            string password = XtraInputBox.Show("Password baru:", "Reset Password", string.Empty) ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            _auth.UpdatePassword(id, _crypto.GetHashPassword(password));
            XtraMessageBox.Show("Password diperbarui.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleActive()
        {
            if (_userView.GetFocusedRow() is not UserAccount u)
            {
                return;
            }
            _auth.SetActive(u.UserId, !u.IsActive);
            LoadUsers();
        }

        private void ToggleLocked()
        {
            if (_userView.GetFocusedRow() is not UserAccount u)
            {
                return;
            }
            _auth.SetLocked(u.UserId, !u.IsLocked);
            LoadUsers();
        }

        private void GrantAccess()
        {
            if (_selectedUserId is not int id)
            {
                return;
            }
            if (_cmbApp.EditValue is not string appId || _cmbRole.EditValue is null)
            {
                XtraMessageBox.Show("Pilih App dan Role dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int roleId = Convert.ToInt32(_cmbRole.EditValue);
            _auth.UpsertAccess(id, appId, roleId);
            OnUserSelected();
        }

        private void RevokeAccess()
        {
            if (_selectedUserId is not int id)
            {
                return;
            }
            if (_accessView.GetFocusedRow() is not UserAccessRow row)
            {
                return;
            }
            _auth.RemoveAccess(id, row.AppId);
            OnUserSelected();
        }
    }
}
