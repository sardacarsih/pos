namespace Penjualan
{
    partial class Frmlogin
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new(typeof(Frmlogin));
            cardPanel = new PosLoginUi.LoginCardPanel();
            avatarControl = new PosLoginUi.LoginAvatarControl();
            lblWelcome = new Label();
            lblApplication = new Label();
            lblSubtitle = new Label();
            accentLine = new Panel();
            lblUserId = new Label();
            userInputPanel = new PosLoginUi.RoundedInputPanel();
            txtuserid = new DevExpress.XtraEditors.TextEdit();
            lblPassword = new Label();
            passwordInputPanel = new PosLoginUi.RoundedInputPanel();
            txtpwd = new DevExpress.XtraEditors.TextEdit();
            passwordEyeButton = new PosLoginUi.PasswordEyeButton();
            Login = new PosLoginUi.RoundedActionButton();
            footerLine = new Panel();
            securityShield = new PosLoginUi.SecurityShieldControl();
            lblSecurityTitle = new Label();
            lblSecurityText = new Label();
            lblversi = new Label();
            simpleButton1 = new PosLoginUi.CircularCloseButton();
            cardPanel.SuspendLayout();
            userInputPanel.SuspendLayout();
            passwordInputPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).BeginInit();
            SuspendLayout();

            cardPanel.Controls.Add(avatarControl);
            cardPanel.Controls.Add(lblWelcome);
            cardPanel.Controls.Add(lblApplication);
            cardPanel.Controls.Add(lblSubtitle);
            cardPanel.Controls.Add(accentLine);
            cardPanel.Controls.Add(lblUserId);
            cardPanel.Controls.Add(userInputPanel);
            cardPanel.Controls.Add(lblPassword);
            cardPanel.Controls.Add(passwordInputPanel);
            cardPanel.Controls.Add(Login);
            cardPanel.Controls.Add(footerLine);
            cardPanel.Controls.Add(securityShield);
            cardPanel.Controls.Add(lblSecurityTitle);
            cardPanel.Controls.Add(lblSecurityText);
            cardPanel.Controls.Add(lblversi);
            cardPanel.Location = new Point(920, 60);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(470, 744);
            cardPanel.TabIndex = 0;

            userInputPanel.Controls.Add(txtuserid);
            userInputPanel.InputKind = PosLoginUi.LoginInputKind.User;
            userInputPanel.Location = new Point(54, 322);
            userInputPanel.Name = "userInputPanel";
            userInputPanel.Size = new Size(362, 54);
            userInputPanel.TabIndex = 6;

            passwordInputPanel.Controls.Add(txtpwd);
            passwordInputPanel.Controls.Add(passwordEyeButton);
            passwordInputPanel.InputKind = PosLoginUi.LoginInputKind.Password;
            passwordInputPanel.Location = new Point(54, 427);
            passwordInputPanel.Name = "passwordInputPanel";
            passwordInputPanel.Size = new Size(362, 54);
            passwordInputPanel.TabIndex = 8;

            avatarControl.Location = new Point(191, 50);
            avatarControl.Name = "avatarControl";
            avatarControl.Size = new Size(88, 88);
            avatarControl.TabIndex = 0;

            lblWelcome.BackColor = Color.Transparent;
            lblWelcome.Font = new Font("Bahnschrift", 26F, FontStyle.Bold);
            lblWelcome.ForeColor = Color.FromArgb(8, 39, 91);
            lblWelcome.Location = new Point(42, 150);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(386, 48);
            lblWelcome.TabIndex = 1;
            lblWelcome.Text = "";
            lblWelcome.TextAlign = ContentAlignment.MiddleCenter;
            lblWelcome.Visible = false;

            lblApplication.BackColor = Color.Transparent;
            lblApplication.Font = new Font("Bahnschrift", 20F, FontStyle.Bold);
            lblApplication.ForeColor = Color.FromArgb(10, 84, 218);
            lblApplication.Location = new Point(42, 198);
            lblApplication.Name = "lblApplication";
            lblApplication.Size = new Size(386, 24);
            lblApplication.TabIndex = 2;
            lblApplication.Text = "Penjualan";
            lblApplication.TextAlign = ContentAlignment.MiddleCenter;

            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 10.5F);
            lblSubtitle.ForeColor = Color.FromArgb(80, 96, 126);
            lblSubtitle.Location = new Point(42, 224);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(386, 26);
            lblSubtitle.TabIndex = 3;
            lblSubtitle.Text = "Silakan masuk untuk melanjutkan";
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;

            accentLine.BackColor = Color.FromArgb(13, 91, 224);
            accentLine.Location = new Point(204, 262);
            accentLine.Name = "accentLine";
            accentLine.Size = new Size(62, 3);
            accentLine.TabIndex = 4;

            lblUserId.BackColor = Color.Transparent;
            lblUserId.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblUserId.ForeColor = Color.FromArgb(8, 39, 91);
            lblUserId.Location = new Point(54, 294);
            lblUserId.Name = "lblUserId";
            lblUserId.Size = new Size(350, 24);
            lblUserId.TabIndex = 5;
            lblUserId.Text = "Username";

            txtuserid.Dock = DockStyle.Fill;
            txtuserid.Location = new Point(48, 2);
            txtuserid.Name = "txtuserid";
            txtuserid.Properties.Appearance.Font = new Font("Segoe UI", 11F);
            txtuserid.Properties.Appearance.ForeColor = Color.FromArgb(45, 61, 91);
            txtuserid.Properties.Appearance.Options.UseFont = true;
            txtuserid.Properties.Appearance.Options.UseForeColor = true;
            txtuserid.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            txtuserid.Properties.AutoHeight = false;
            txtuserid.Properties.Appearance.BackColor = Color.FromArgb(253, 254, 255);
            txtuserid.Properties.Appearance.Options.UseBackColor = true;
            txtuserid.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            txtuserid.Properties.NullValuePrompt = "Masukkan username";
            txtuserid.Properties.NullValuePromptShowForEmptyValue = true;
            txtuserid.Size = new Size(268, 50);
            txtuserid.TabIndex = 0;
            txtuserid.KeyDown += txtuserid_KeyDown;

            lblPassword.BackColor = Color.Transparent;
            lblPassword.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblPassword.ForeColor = Color.FromArgb(8, 39, 91);
            lblPassword.Location = new Point(54, 399);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(350, 24);
            lblPassword.TabIndex = 7;
            lblPassword.Text = "Password";

            txtpwd.Dock = DockStyle.Fill;
            txtpwd.Location = new Point(48, 2);
            txtpwd.Name = "txtpwd";
            txtpwd.Properties.Appearance.Font = new Font("Segoe UI", 11F);
            txtpwd.Properties.Appearance.ForeColor = Color.FromArgb(45, 61, 91);
            txtpwd.Properties.Appearance.Options.UseFont = true;
            txtpwd.Properties.Appearance.Options.UseForeColor = true;
            txtpwd.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            txtpwd.Properties.AutoHeight = false;
            txtpwd.Properties.Appearance.BackColor = Color.FromArgb(253, 254, 255);
            txtpwd.Properties.Appearance.Options.UseBackColor = true;
            txtpwd.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            txtpwd.Properties.NullValuePrompt = "Masukkan password";
            txtpwd.Properties.NullValuePromptShowForEmptyValue = true;
            txtpwd.Properties.UseSystemPasswordChar = true;
            txtpwd.Size = new Size(268, 50);
            txtpwd.TabIndex = 1;
            txtpwd.KeyDown += txtpwd_KeyDown;

            passwordEyeButton.Dock = DockStyle.Right;
            passwordEyeButton.Location = new Point(316, 2);
            passwordEyeButton.Name = "passwordEyeButton";
            passwordEyeButton.Size = new Size(40, 50);
            passwordEyeButton.TabIndex = 9;
            passwordEyeButton.Click += PasswordEyeButton_Click;

            Login.BackColor = Color.FromArgb(10, 84, 218);
            Login.CornerRadius = 15;
            Login.Cursor = Cursors.Hand;
            Login.FlatAppearance.BorderSize = 0;
            Login.Font = new Font("Bahnschrift", 12F, FontStyle.Bold);
            Login.ForeColor = Color.White;
            Login.Location = new Point(54, 514);
            Login.Name = "Login";
            Login.Size = new Size(362, 58);
            Login.TabIndex = 2;
            Login.Text = "LOGIN";
            Login.UseVisualStyleBackColor = false;
            Login.Click += Login_Click;

            footerLine.BackColor = Color.FromArgb(224, 231, 242);
            footerLine.Location = new Point(54, 610);
            footerLine.Name = "footerLine";
            footerLine.Size = new Size(362, 1);
            footerLine.TabIndex = 11;

            securityShield.Location = new Point(54, 630);
            securityShield.Name = "securityShield";
            securityShield.Size = new Size(38, 42);
            securityShield.TabIndex = 12;

            lblSecurityTitle.BackColor = Color.Transparent;
            lblSecurityTitle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblSecurityTitle.ForeColor = Color.FromArgb(26, 53, 99);
            lblSecurityTitle.Location = new Point(103, 628);
            lblSecurityTitle.Name = "lblSecurityTitle";
            lblSecurityTitle.Size = new Size(313, 22);
            lblSecurityTitle.TabIndex = 13;
            lblSecurityTitle.Text = "Keamanan data adalah prioritas utama";

            lblSecurityText.BackColor = Color.Transparent;
            lblSecurityText.Font = new Font("Segoe UI", 8F);
            lblSecurityText.ForeColor = Color.FromArgb(91, 109, 140);
            lblSecurityText.Location = new Point(103, 651);
            lblSecurityText.Name = "lblSecurityText";
            lblSecurityText.Size = new Size(313, 22);
            lblSecurityText.TabIndex = 14;
            lblSecurityText.Text = "Dilindungi dengan keamanan berlapis";

            lblversi.BackColor = Color.Transparent;
            lblversi.Font = new Font("Segoe UI", 7.5F);
            lblversi.ForeColor = Color.FromArgb(130, 145, 169);
            lblversi.Location = new Point(54, 696);
            lblversi.Name = "lblversi";
            lblversi.Size = new Size(362, 18);
            lblversi.TabIndex = 15;
            lblversi.Text = "Version";
            lblversi.TextAlign = ContentAlignment.MiddleCenter;

            simpleButton1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            simpleButton1.BackColor = Color.Transparent;
            simpleButton1.Cursor = Cursors.Hand;
            simpleButton1.DialogResult = DialogResult.Cancel;
            simpleButton1.FlatAppearance.BorderSize = 0;
            simpleButton1.Location = new Point(1472, 18);
            simpleButton1.Name = "simpleButton1";
            simpleButton1.Size = new Size(46, 42);
            simpleButton1.TabIndex = 3;
            simpleButton1.UseVisualStyleBackColor = false;
            simpleButton1.Click += SimpleButton1_Click;

            AcceptButton = Login;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(234, 242, 255);
            CancelButton = simpleButton1;
            ClientSize = new Size(1536, 864);
            Controls.Add(simpleButton1);
            Controls.Add(cardPanel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            IconOptions.Image = (Image)resources.GetObject("Frmlogin.IconOptions.Image");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Frmlogin";
            StartPosition = FormStartPosition.Manual;
            Text = "Login - Point of Sale";
            FormClosed += Frmlogin_FormClosed;
            Load += Frmlogin_Load;
            Resize += Frmlogin_Resize;
            Shown += Frmlogin_Shown;
            userInputPanel.ResumeLayout(false);
            passwordInputPanel.ResumeLayout(false);
            cardPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).EndInit();
            ResumeLayout(false);
        }

        private PosLoginUi.LoginCardPanel cardPanel;
        private PosLoginUi.LoginAvatarControl avatarControl;
        private Label lblWelcome;
        private Label lblApplication;
        private Label lblSubtitle;
        private Panel accentLine;
        private Label lblUserId;
        private PosLoginUi.RoundedInputPanel userInputPanel;
        private DevExpress.XtraEditors.TextEdit txtuserid;
        private Label lblPassword;
        private PosLoginUi.RoundedInputPanel passwordInputPanel;
        private DevExpress.XtraEditors.TextEdit txtpwd;
        private PosLoginUi.PasswordEyeButton passwordEyeButton;
        private PosLoginUi.RoundedActionButton Login;
        private Panel footerLine;
        private PosLoginUi.SecurityShieldControl securityShield;
        private Label lblSecurityTitle;
        private Label lblSecurityText;
        private Label lblversi;
        private PosLoginUi.CircularCloseButton simpleButton1;
    }
}
