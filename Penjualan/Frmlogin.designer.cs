namespace Penjualan
{
    partial class Frmlogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frmlogin));
            behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(components);
            tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            panelBranding = new DevExpress.XtraEditors.PanelControl();
            pictureBoxLogo = new DevExpress.XtraEditors.PictureEdit();
            lblBrandName = new DevExpress.XtraEditors.LabelControl();
            lblTagline = new DevExpress.XtraEditors.LabelControl();
            lblversi = new DevExpress.XtraEditors.LabelControl();
            panelRight = new DevExpress.XtraEditors.PanelControl();
            layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            lblWelcome = new DevExpress.XtraEditors.LabelControl();
            lblSubtitle = new DevExpress.XtraEditors.LabelControl();
            txtuserid = new DevExpress.XtraEditors.TextEdit();
            txtpwd = new DevExpress.XtraEditors.TextEdit();
            simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            Login = new DevExpress.XtraEditors.SimpleButton();
            layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            emptySpaceTop = new DevExpress.XtraLayout.EmptySpaceItem();
            layoutItemWelcome = new DevExpress.XtraLayout.LayoutControlItem();
            layoutItemSubtitle = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceAfterTitle = new DevExpress.XtraLayout.EmptySpaceItem();
            layoutItemUserID = new DevExpress.XtraLayout.LayoutControlItem();
            layoutItemPassword = new DevExpress.XtraLayout.LayoutControlItem();
            layoutGroupButtons = new DevExpress.XtraLayout.LayoutControlGroup();
            layoutItemCancel = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceBetweenButtons = new DevExpress.XtraLayout.EmptySpaceItem();
            layoutItemLogin = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceBottom = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).BeginInit();
            tableLayoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)panelBranding).BeginInit();
            panelBranding.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelRight).BeginInit();
            panelRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)layoutControl1).BeginInit();
            layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroup1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceTop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemWelcome).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemSubtitle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceAfterTitle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemUserID).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemPassword).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutGroupButtons).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemCancel).BeginInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceBetweenButtons).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemLogin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceBottom).BeginInit();
            SuspendLayout();
            //
            // tableLayoutMain
            //
            tableLayoutMain.ColumnCount = 2;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutMain.RowCount = 1;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Controls.Add(panelBranding, 0, 0);
            tableLayoutMain.Controls.Add(panelRight, 1, 0);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Margin = new Padding(0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.Size = new Size(820, 520);
            tableLayoutMain.TabIndex = 0;
            //
            // panelBranding
            //
            panelBranding.Appearance.BackColor = Color.FromArgb(15, 23, 42);
            panelBranding.Appearance.Options.UseBackColor = true;
            panelBranding.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelBranding.Controls.Add(pictureBoxLogo);
            panelBranding.Controls.Add(lblBrandName);
            panelBranding.Controls.Add(lblTagline);
            panelBranding.Controls.Add(lblversi);
            panelBranding.Dock = DockStyle.Fill;
            panelBranding.Location = new Point(0, 0);
            panelBranding.Margin = new Padding(0);
            panelBranding.Name = "panelBranding";
            panelBranding.Size = new Size(328, 520);
            panelBranding.TabIndex = 0;
            panelBranding.Paint += panelBranding_Paint;
            panelBranding.Resize += panelBranding_Resize;
            //
            // pictureBoxLogo
            //
            pictureBoxLogo.EditValue = (Image)resources.GetObject("Frmlogin.IconOptions.Image");
            pictureBoxLogo.Location = new Point(124, 160);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Properties.AllowFocused = false;
            pictureBoxLogo.Properties.Appearance.BackColor = Color.Transparent;
            pictureBoxLogo.Properties.Appearance.Options.UseBackColor = true;
            pictureBoxLogo.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            pictureBoxLogo.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Never;
            pictureBoxLogo.Properties.ShowMenu = false;
            pictureBoxLogo.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            pictureBoxLogo.Properties.ReadOnly = true;
            pictureBoxLogo.Size = new Size(80, 80);
            pictureBoxLogo.TabIndex = 0;
            pictureBoxLogo.TabStop = false;
            //
            // lblBrandName
            //
            lblBrandName.Appearance.Font = new Font("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point);
            lblBrandName.Appearance.ForeColor = Color.White;
            lblBrandName.Appearance.Options.UseFont = true;
            lblBrandName.Appearance.Options.UseForeColor = true;
            lblBrandName.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblBrandName.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblBrandName.Location = new Point(0, 252);
            lblBrandName.Name = "lblBrandName";
            lblBrandName.Size = new Size(328, 40);
            lblBrandName.TabIndex = 1;
            lblBrandName.Text = "Point of Sale";
            //
            // lblTagline
            //
            lblTagline.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblTagline.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
            lblTagline.Appearance.Options.UseFont = true;
            lblTagline.Appearance.Options.UseForeColor = true;
            lblTagline.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblTagline.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            lblTagline.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblTagline.Location = new Point(0, 298);
            lblTagline.Name = "lblTagline";
            lblTagline.Size = new Size(328, 40);
            lblTagline.TabIndex = 2;
            lblTagline.Text = "Smart Accounting && Inventory System";
            //
            // lblversi
            //
            lblversi.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblversi.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            lblversi.Appearance.Options.UseFont = true;
            lblversi.Appearance.Options.UseForeColor = true;
            lblversi.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblversi.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblversi.Location = new Point(0, 490);
            lblversi.Name = "lblversi";
            lblversi.Size = new Size(328, 20);
            lblversi.TabIndex = 3;
            lblversi.Text = "Version";
            //
            // panelRight
            //
            panelRight.Appearance.BackColor = Color.White;
            panelRight.Appearance.Options.UseBackColor = true;
            panelRight.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelRight.Controls.Add(layoutControl1);
            panelRight.Dock = DockStyle.Fill;
            panelRight.Location = new Point(328, 0);
            panelRight.Margin = new Padding(0);
            panelRight.Name = "panelRight";
            panelRight.Size = new Size(492, 520);
            panelRight.TabIndex = 1;
            //
            // layoutControl1
            //
            layoutControl1.AllowCustomization = false;
            layoutControl1.Appearance.Control.BackColor = Color.White;
            layoutControl1.Appearance.Control.Options.UseBackColor = true;
            layoutControl1.Controls.Add(lblWelcome);
            layoutControl1.Controls.Add(lblSubtitle);
            layoutControl1.Controls.Add(txtuserid);
            layoutControl1.Controls.Add(txtpwd);
            layoutControl1.Controls.Add(simpleButton1);
            layoutControl1.Controls.Add(Login);
            layoutControl1.Dock = DockStyle.Fill;
            layoutControl1.Location = new Point(0, 0);
            layoutControl1.Name = "layoutControl1";
            layoutControl1.Root = layoutControlGroup1;
            layoutControl1.Size = new Size(492, 520);
            layoutControl1.TabIndex = 0;
            //
            // lblWelcome
            //
            lblWelcome.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
            lblWelcome.Appearance.ForeColor = Color.FromArgb(30, 41, 59);
            lblWelcome.Appearance.Options.UseFont = true;
            lblWelcome.Appearance.Options.UseForeColor = true;
            lblWelcome.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            lblWelcome.Location = new Point(52, 140);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(388, 37);
            lblWelcome.TabIndex = 10;
            lblWelcome.Text = "Welcome Back";
            //
            // lblSubtitle
            //
            lblSubtitle.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblSubtitle.Appearance.ForeColor = Color.FromArgb(100, 108, 139);
            lblSubtitle.Appearance.Options.UseFont = true;
            lblSubtitle.Appearance.Options.UseForeColor = true;
            lblSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            lblSubtitle.Location = new Point(52, 181);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(388, 19);
            lblSubtitle.TabIndex = 11;
            lblSubtitle.Text = "Sign in to your account";
            //
            // txtuserid
            //
            txtuserid.Location = new Point(52, 244);
            txtuserid.Name = "txtuserid";
            txtuserid.Properties.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtuserid.Properties.Appearance.Options.UseFont = true;
            txtuserid.Size = new Size(388, 36);
            txtuserid.StyleController = layoutControl1;
            txtuserid.TabIndex = 0;
            txtuserid.KeyDown += txtuserid_KeyDown;
            //
            // txtpwd
            //
            txtpwd.Location = new Point(52, 314);
            txtpwd.Name = "txtpwd";
            txtpwd.Properties.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtpwd.Properties.Appearance.Options.UseFont = true;
            txtpwd.Properties.UseSystemPasswordChar = true;
            txtpwd.Size = new Size(388, 36);
            txtpwd.StyleController = layoutControl1;
            txtpwd.TabIndex = 1;
            txtpwd.KeyDown += txtpwd_KeyDown;
            //
            // simpleButton1
            //
            simpleButton1.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            simpleButton1.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            simpleButton1.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            simpleButton1.Appearance.Options.UseBackColor = true;
            simpleButton1.Appearance.Options.UseFont = true;
            simpleButton1.Appearance.Options.UseForeColor = true;
            simpleButton1.Location = new Point(52, 384);
            simpleButton1.MinimumSize = new Size(100, 44);
            simpleButton1.Name = "simpleButton1";
            simpleButton1.Size = new Size(136, 44);
            simpleButton1.TabIndex = 3;
            simpleButton1.Text = "Cancel";
            simpleButton1.Click += SimpleButton1_Click;
            //
            // Login
            //
            Login.Appearance.BackColor = Color.FromArgb(37, 99, 235);
            Login.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            Login.Appearance.ForeColor = Color.White;
            Login.Appearance.Options.UseBackColor = true;
            Login.Appearance.Options.UseFont = true;
            Login.Appearance.Options.UseForeColor = true;
            Login.Location = new Point(304, 384);
            Login.MinimumSize = new Size(100, 44);
            Login.Name = "Login";
            Login.Size = new Size(136, 44);
            Login.TabIndex = 2;
            Login.Text = "Login";
            Login.Click += Login_Click;
            //
            // layoutControlGroup1
            //
            layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            layoutControlGroup1.GroupBordersVisible = false;
            layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
                emptySpaceTop,
                layoutItemWelcome,
                layoutItemSubtitle,
                emptySpaceAfterTitle,
                layoutItemUserID,
                layoutItemPassword,
                layoutGroupButtons,
                emptySpaceBottom
            });
            layoutControlGroup1.Name = "Root";
            layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(50, 50, 30, 30);
            layoutControlGroup1.Size = new Size(492, 520);
            layoutControlGroup1.TextVisible = false;
            //
            // emptySpaceTop
            //
            emptySpaceTop.AllowHotTrack = false;
            emptySpaceTop.Location = new Point(0, 0);
            emptySpaceTop.MinSize = new Size(1, 20);
            emptySpaceTop.Name = "emptySpaceTop";
            emptySpaceTop.Size = new Size(392, 108);
            emptySpaceTop.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            emptySpaceTop.TextSize = new Size(0, 0);
            //
            // layoutItemWelcome
            //
            layoutItemWelcome.Control = lblWelcome;
            layoutItemWelcome.Location = new Point(0, 108);
            layoutItemWelcome.Name = "layoutItemWelcome";
            layoutItemWelcome.Size = new Size(392, 41);
            layoutItemWelcome.TextSize = new Size(0, 0);
            layoutItemWelcome.TextVisible = false;
            //
            // layoutItemSubtitle
            //
            layoutItemSubtitle.Control = lblSubtitle;
            layoutItemSubtitle.Location = new Point(0, 149);
            layoutItemSubtitle.Name = "layoutItemSubtitle";
            layoutItemSubtitle.Size = new Size(392, 23);
            layoutItemSubtitle.TextSize = new Size(0, 0);
            layoutItemSubtitle.TextVisible = false;
            //
            // emptySpaceAfterTitle
            //
            emptySpaceAfterTitle.AllowHotTrack = false;
            emptySpaceAfterTitle.Location = new Point(0, 172);
            emptySpaceAfterTitle.MaxSize = new Size(0, 24);
            emptySpaceAfterTitle.MinSize = new Size(1, 24);
            emptySpaceAfterTitle.Name = "emptySpaceAfterTitle";
            emptySpaceAfterTitle.Size = new Size(392, 24);
            emptySpaceAfterTitle.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            emptySpaceAfterTitle.TextSize = new Size(0, 0);
            //
            // layoutItemUserID
            //
            layoutItemUserID.Control = txtuserid;
            layoutItemUserID.Location = new Point(0, 196);
            layoutItemUserID.Name = "layoutItemUserID";
            layoutItemUserID.Size = new Size(392, 60);
            layoutItemUserID.Text = "User ID";
            layoutItemUserID.TextLocation = DevExpress.Utils.Locations.Top;
            layoutItemUserID.TextSize = new Size(60, 15);
            layoutItemUserID.AppearanceItemCaption.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            layoutItemUserID.AppearanceItemCaption.ForeColor = Color.FromArgb(30, 41, 59);
            layoutItemUserID.AppearanceItemCaption.Options.UseFont = true;
            layoutItemUserID.AppearanceItemCaption.Options.UseForeColor = true;
            //
            // layoutItemPassword
            //
            layoutItemPassword.Control = txtpwd;
            layoutItemPassword.Location = new Point(0, 256);
            layoutItemPassword.Name = "layoutItemPassword";
            layoutItemPassword.Size = new Size(392, 60);
            layoutItemPassword.Text = "Password";
            layoutItemPassword.TextLocation = DevExpress.Utils.Locations.Top;
            layoutItemPassword.TextSize = new Size(60, 15);
            layoutItemPassword.AppearanceItemCaption.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            layoutItemPassword.AppearanceItemCaption.ForeColor = Color.FromArgb(30, 41, 59);
            layoutItemPassword.AppearanceItemCaption.Options.UseFont = true;
            layoutItemPassword.AppearanceItemCaption.Options.UseForeColor = true;
            //
            // layoutGroupButtons
            //
            layoutGroupButtons.GroupBordersVisible = false;
            layoutGroupButtons.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
                layoutItemCancel,
                emptySpaceBetweenButtons,
                layoutItemLogin
            });
            layoutGroupButtons.LayoutMode = DevExpress.XtraLayout.Utils.LayoutMode.Table;
            layoutGroupButtons.Location = new Point(0, 326);
            layoutGroupButtons.Name = "layoutGroupButtons";
            layoutGroupButtons.Padding = new DevExpress.XtraLayout.Utils.Padding(0, 0, 10, 0);
            layoutGroupButtons.Size = new Size(392, 70);
            layoutGroupButtons.TextVisible = false;
            layoutGroupButtons.OptionsTableLayoutGroup.ColumnDefinitions.AddRange(new DevExpress.XtraLayout.ColumnDefinition[] {
                new DevExpress.XtraLayout.ColumnDefinition { SizeType = System.Windows.Forms.SizeType.Percent, Width = 35 },
                new DevExpress.XtraLayout.ColumnDefinition { SizeType = System.Windows.Forms.SizeType.Percent, Width = 30 },
                new DevExpress.XtraLayout.ColumnDefinition { SizeType = System.Windows.Forms.SizeType.Percent, Width = 35 }
            });
            layoutGroupButtons.OptionsTableLayoutGroup.RowDefinitions.AddRange(new DevExpress.XtraLayout.RowDefinition[] {
                new DevExpress.XtraLayout.RowDefinition { SizeType = System.Windows.Forms.SizeType.AutoSize }
            });
            //
            // layoutItemCancel
            //
            layoutItemCancel.Control = simpleButton1;
            layoutItemCancel.Location = new Point(0, 0);
            layoutItemCancel.Name = "layoutItemCancel";
            layoutItemCancel.Size = new Size(136, 48);
            layoutItemCancel.TextSize = new Size(0, 0);
            layoutItemCancel.TextVisible = false;
            layoutItemCancel.OptionsTableLayoutItem.ColumnIndex = 0;
            layoutItemCancel.OptionsTableLayoutItem.RowIndex = 0;
            //
            // emptySpaceBetweenButtons
            //
            emptySpaceBetweenButtons.AllowHotTrack = false;
            emptySpaceBetweenButtons.Location = new Point(136, 0);
            emptySpaceBetweenButtons.Name = "emptySpaceBetweenButtons";
            emptySpaceBetweenButtons.Size = new Size(116, 48);
            emptySpaceBetweenButtons.TextSize = new Size(0, 0);
            emptySpaceBetweenButtons.OptionsTableLayoutItem.ColumnIndex = 1;
            emptySpaceBetweenButtons.OptionsTableLayoutItem.RowIndex = 0;
            //
            // layoutItemLogin
            //
            layoutItemLogin.Control = Login;
            layoutItemLogin.Location = new Point(252, 0);
            layoutItemLogin.Name = "layoutItemLogin";
            layoutItemLogin.Size = new Size(136, 48);
            layoutItemLogin.TextSize = new Size(0, 0);
            layoutItemLogin.TextVisible = false;
            layoutItemLogin.OptionsTableLayoutItem.ColumnIndex = 2;
            layoutItemLogin.OptionsTableLayoutItem.RowIndex = 0;
            //
            // emptySpaceBottom
            //
            emptySpaceBottom.AllowHotTrack = false;
            emptySpaceBottom.Location = new Point(0, 396);
            emptySpaceBottom.MinSize = new Size(1, 20);
            emptySpaceBottom.Name = "emptySpaceBottom";
            emptySpaceBottom.Size = new Size(392, 64);
            emptySpaceBottom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            emptySpaceBottom.TextSize = new Size(0, 0);
            //
            // Frmlogin
            //
            Appearance.Options.UseFont = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(820, 520);
            Controls.Add(tableLayoutMain);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.None;
            IconOptions.Image = (Image)resources.GetObject("Frmlogin.IconOptions.Image");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(820, 520);
            Name = "Frmlogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Login";
            Load += Frmlogin_Load;
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).EndInit();
            tableLayoutMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)panelBranding).EndInit();
            panelBranding.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelRight).EndInit();
            panelRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)layoutControl1).EndInit();
            layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroup1).EndInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceTop).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemWelcome).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemSubtitle).EndInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceAfterTitle).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemUserID).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemPassword).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutGroupButtons).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemCancel).EndInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceBetweenButtons).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutItemLogin).EndInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceBottom).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private DevExpress.XtraEditors.PanelControl panelBranding;
        private DevExpress.XtraEditors.PanelControl panelRight;
        private DevExpress.XtraEditors.PictureEdit pictureBoxLogo;
        private DevExpress.XtraEditors.LabelControl lblBrandName;
        private DevExpress.XtraEditors.LabelControl lblTagline;
        private DevExpress.XtraEditors.LabelControl lblversi;
        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraEditors.LabelControl lblWelcome;
        private DevExpress.XtraEditors.LabelControl lblSubtitle;
        private DevExpress.XtraEditors.TextEdit txtuserid;
        private DevExpress.XtraEditors.TextEdit txtpwd;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.SimpleButton Login;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceTop;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemWelcome;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemSubtitle;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceAfterTitle;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemUserID;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemPassword;
        private DevExpress.XtraLayout.LayoutControlGroup layoutGroupButtons;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemCancel;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceBetweenButtons;
        private DevExpress.XtraLayout.LayoutControlItem layoutItemLogin;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceBottom;
    }
}
