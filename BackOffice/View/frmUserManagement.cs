using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using BackOffice.DataLayer;
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
        private readonly AuthRepository _auth = new(global.connectionString);
        private readonly PasswordCryptographyPbkdf2 _crypto = new();

        private readonly GridControl _userGrid = new();
        private readonly GridView _userView;
        private readonly TextEdit _txtUsername = new();
        private readonly TextEdit _txtFullName = new();

        private readonly GridControl _accessGrid = new();
        private readonly GridView _accessView;
        private readonly LookUpEdit _cmbApp = new();
        private readonly LookUpEdit _cmbRole = new();

        private List<UserAccount> _users = new();
        private int? _selectedUserId;

        public frmUserManagement()
        {
            _userView = new GridView(_userGrid);
            _accessView = new GridView(_accessGrid);

            Text = "Manajemen User";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(940, 560);

            BuildLayout();
            LoadLookups();
            LoadUsers();
        }

        private void BuildLayout()
        {
            // ---- Daftar user (kiri) ----
            _userGrid.MainView = _userView;
            _userGrid.SetBounds(12, 12, 380, 470);
            _userGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _userView.OptionsBehavior.Editable = false;
            _userView.OptionsView.ShowGroupPanel = false;
            _userView.FocusedRowChanged += (_, _) => OnUserSelected();
            Controls.Add(_userGrid);

            var btnRefresh = MakeButton("Refresh", 12, 490, 90, (_, _) => LoadUsers());
            var btnNew = MakeButton("User Baru", 110, 490, 100, (_, _) => CreateUser());
            Controls.Add(btnRefresh);
            Controls.Add(btnNew);

            // ---- Editor (kanan) ----
            int x = 410;
            Controls.Add(MakeLabel("Username", x, 16));
            _txtUsername.Properties.ReadOnly = true;
            _txtUsername.SetBounds(x + 90, 12, 200, 22);
            _txtUsername.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            Controls.Add(_txtUsername);

            Controls.Add(MakeLabel("Nama Lengkap", x, 48));
            _txtFullName.SetBounds(x + 90, 44, 200, 22);
            _txtFullName.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            Controls.Add(_txtFullName);

            Controls.Add(MakeButton("Simpan Nama", x + 90, 74, 110, (_, _) => SaveFullName()));
            Controls.Add(MakeButton("Reset Password", x + 210, 74, 110, (_, _) => ResetPassword()));
            Controls.Add(MakeButton("Aktif/Nonaktif", x, 110, 150, (_, _) => ToggleActive()));
            Controls.Add(MakeButton("Lock/Unlock", x + 160, 110, 150, (_, _) => ToggleLocked()));

            // ---- Akses (app + role) ----
            Controls.Add(MakeLabel("Akses Aplikasi & Role", x, 150));
            _accessGrid.MainView = _accessView;
            _accessGrid.SetBounds(x, 174, 510, 230);
            _accessGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _accessView.OptionsBehavior.Editable = false;
            _accessView.OptionsView.ShowGroupPanel = false;
            Controls.Add(_accessGrid);

            Controls.Add(MakeLabel("App", x, 420));
            _cmbApp.Properties.DisplayMember = "AppName";
            _cmbApp.Properties.ValueMember = "AppId";
            _cmbApp.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AppName", "Aplikasi"));
            _cmbApp.SetBounds(x + 40, 416, 180, 22);
            _cmbApp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(_cmbApp);

            Controls.Add(MakeLabel("Role", x + 230, 420));
            _cmbRole.Properties.DisplayMember = "RoleName";
            _cmbRole.Properties.ValueMember = "RoleId";
            _cmbRole.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("RoleName", "Role"));
            _cmbRole.SetBounds(x + 270, 416, 150, 22);
            _cmbRole.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(_cmbRole);

            Controls.Add(MakeButton("Beri / Ubah Akses", x, 448, 160, (_, _) => GrantAccess()));
            Controls.Add(MakeButton("Cabut Akses", x + 170, 448, 150, (_, _) => RevokeAccess()));
        }

        // ------------------------------------------------------------ helpers UI

        private static LabelControl MakeLabel(string text, int x, int y)
        {
            var l = new LabelControl { Text = text };
            l.SetBounds(x, y, 85, 20);
            return l;
        }

        private static SimpleButton MakeButton(string text, int x, int y, int w, EventHandler onClick)
        {
            var b = new SimpleButton { Text = text };
            b.SetBounds(x, y, w, 26);
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
                _txtUsername.Text = u.Username;
                _txtFullName.Text = u.FullName ?? string.Empty;
                _accessGrid.DataSource = _auth.GetUserAccess(u.UserId);
                _accessView.PopulateColumns();
                HideAccessColumns();
            }
            else
            {
                _selectedUserId = null;
                _txtUsername.Text = string.Empty;
                _txtFullName.Text = string.Empty;
                _accessGrid.DataSource = null;
            }
        }

        private void HideAccessColumns()
        {
            foreach (var name in new[] { "UserId", "RoleId" })
            {
                var col = _accessView.Columns.ColumnByFieldName(name);
                if (col is not null)
                {
                    col.Visible = false;
                }
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
