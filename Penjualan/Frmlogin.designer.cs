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
            txtpwd = new DevExpress.XtraEditors.TextEdit();
            txtuserid = new DevExpress.XtraEditors.TextEdit();
            simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            Login = new DevExpress.XtraEditors.SimpleButton();
            lblversi = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).BeginInit();
            SuspendLayout();
            // 
            // txtpwd
            // 
            txtpwd.Location = new Point(176, 155);
            txtpwd.Margin = new Padding(2);
            txtpwd.Name = "txtpwd";
            txtpwd.Properties.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtpwd.Properties.Appearance.Options.UseFont = true;
            txtpwd.Properties.Name = "textEdit1";
            txtpwd.Properties.UseSystemPasswordChar = true;
            txtpwd.Size = new Size(92, 28);
            txtpwd.TabIndex = 1;
            txtpwd.ToolTip = "Password";
            txtpwd.EditValueChanged += txtpwd_EditValueChanged;
            txtpwd.KeyDown += txtpwd_KeyDown;
            // 
            // txtuserid
            // 
            txtuserid.Location = new Point(73, 155);
            txtuserid.Margin = new Padding(2);
            txtuserid.Name = "txtuserid";
            txtuserid.Properties.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtuserid.Properties.Appearance.Options.UseFont = true;
            txtuserid.Properties.Name = "textEdit1";
            txtuserid.Size = new Size(92, 28);
            txtuserid.TabIndex = 0;
            txtuserid.ToolTip = "UserID";
            txtuserid.KeyDown += txtuserid_KeyDown;
            // 
            // simpleButton1
            // 
            simpleButton1.Location = new Point(20, 151);
            simpleButton1.Margin = new Padding(3, 4, 3, 4);
            simpleButton1.Name = "simpleButton1";
            simpleButton1.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            simpleButton1.Size = new Size(33, 27);
            simpleButton1.TabIndex = 3;
            simpleButton1.ToolTip = "Cancel";
            simpleButton1.Click += SimpleButton1_Click;
            // 
            // Login
            // 
            Login.Location = new Point(281, 151);
            Login.Margin = new Padding(3, 4, 3, 4);
            Login.Name = "Login";
            Login.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            Login.Size = new Size(36, 27);
            Login.TabIndex = 2;
            Login.ToolTip = "Login";
            Login.Click += Login_Click;
            // 
            // lblversi
            // 
            lblversi.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lblversi.Appearance.ForeColor = Color.White;
            lblversi.Appearance.Options.UseFont = true;
            lblversi.Appearance.Options.UseForeColor = true;
            lblversi.Location = new Point(237, 10);
            lblversi.Name = "lblversi";
            lblversi.Size = new Size(42, 15);
            lblversi.TabIndex = 4;
            lblversi.Text = "Version";
            // 
            // Frmlogin
            // 
            Appearance.Options.UseFont = true;
            AutoScaleMode = AutoScaleMode.None;
            BackgroundImageLayoutStore = ImageLayout.Stretch;
            BackgroundImageStore = Properties.Resources.penjualan;
            ClientSize = new Size(339, 191);
            Controls.Add(lblversi);
            Controls.Add(txtpwd);
            Controls.Add(txtuserid);
            Controls.Add(simpleButton1);
            Controls.Add(Login);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.None;
            IconOptions.Image = (Image)resources.GetObject("Frmlogin.IconOptions.Image");
            Location = new Point(10, 50);
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Frmlogin";
            Text = "Login";
            Load += Frmlogin_Load;
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpwd.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtuserid.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraEditors.SimpleButton Login;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.TextEdit txtuserid;
        private DevExpress.XtraEditors.TextEdit txtpwd;
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private DevExpress.XtraEditors.LabelControl lblversi;
    }
}