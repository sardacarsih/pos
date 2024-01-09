namespace BackOffice
{
    partial class PaymentForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PaymentForm));
            this.groupControl2 = new DevExpress.XtraEditors.GroupControl();
            this.lblkembali = new DevExpress.XtraEditors.LabelControl();
            this.lblbayar = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.sbtutup = new DevExpress.XtraEditors.SimpleButton();
            this.sbsimpancetak = new DevExpress.XtraEditors.SimpleButton();
            this.txtkembali = new DevExpress.XtraEditors.TextEdit();
            this.txtcash = new DevExpress.XtraEditors.TextEdit();
            this.txttotal = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.leangsuran = new DevExpress.XtraEditors.LookUpEdit();
            this.searchLookUpEdit1 = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.PelangganTextBox = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).BeginInit();
            this.groupControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtkembali.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtcash.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txttotal.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.leangsuran.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PelangganTextBox.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl2
            // 
            this.groupControl2.Controls.Add(this.lblkembali);
            this.groupControl2.Controls.Add(this.lblbayar);
            this.groupControl2.Controls.Add(this.labelControl1);
            this.groupControl2.Controls.Add(this.sbtutup);
            this.groupControl2.Controls.Add(this.sbsimpancetak);
            this.groupControl2.Controls.Add(this.txtkembali);
            this.groupControl2.Controls.Add(this.txtcash);
            this.groupControl2.Controls.Add(this.txttotal);
            this.groupControl2.Location = new System.Drawing.Point(23, 89);
            this.groupControl2.Name = "groupControl2";
            this.groupControl2.Size = new System.Drawing.Size(334, 215);
            this.groupControl2.TabIndex = 3;
            this.groupControl2.Text = "Pembayaran Detail";
            // 
            // lblkembali
            // 
            this.lblkembali.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblkembali.Appearance.Options.UseFont = true;
            this.lblkembali.Location = new System.Drawing.Point(19, 124);
            this.lblkembali.Name = "lblkembali";
            this.lblkembali.Size = new System.Drawing.Size(74, 25);
            this.lblkembali.TabIndex = 2;
            this.lblkembali.Text = "Kembali";
            // 
            // lblbayar
            // 
            this.lblbayar.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblbayar.Appearance.Options.UseFont = true;
            this.lblbayar.Location = new System.Drawing.Point(19, 86);
            this.lblbayar.Name = "lblbayar";
            this.lblbayar.Size = new System.Drawing.Size(53, 25);
            this.lblbayar.TabIndex = 2;
            this.lblbayar.Text = "Bayar";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(19, 42);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(46, 25);
            this.labelControl1.TabIndex = 2;
            this.labelControl1.Text = "Total";
            // 
            // sbtutup
            // 
            this.sbtutup.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("sbtutup.ImageOptions.SvgImage")));
            this.sbtutup.Location = new System.Drawing.Point(189, 173);
            this.sbtutup.Name = "sbtutup";
            this.sbtutup.Size = new System.Drawing.Size(129, 37);
            this.sbtutup.TabIndex = 2;
            this.sbtutup.Text = "Tutup";
            this.sbtutup.Click += new System.EventHandler(this.sbtutup_Click);
            // 
            // sbsimpancetak
            // 
            this.sbsimpancetak.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("sbsimpancetak.ImageOptions.SvgImage")));
            this.sbsimpancetak.Location = new System.Drawing.Point(16, 173);
            this.sbsimpancetak.Name = "sbsimpancetak";
            this.sbsimpancetak.Size = new System.Drawing.Size(124, 37);
            this.sbsimpancetak.TabIndex = 1;
            this.sbsimpancetak.Text = "Simpan";
            this.sbsimpancetak.Click += new System.EventHandler(this.sbsimpancetak_Click);
            // 
            // txtkembali
            // 
            this.txtkembali.EditValue = "0";
            this.txtkembali.Location = new System.Drawing.Point(124, 121);
            this.txtkembali.Name = "txtkembali";
            this.txtkembali.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtkembali.Properties.Appearance.Options.UseFont = true;
            this.txtkembali.Properties.Appearance.Options.UseTextOptions = true;
            this.txtkembali.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.txtkembali.Properties.DisplayFormat.FormatString = "###,###,###,##0.00";
            this.txtkembali.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtkembali.Properties.EditFormat.FormatString = "###,###,###,##0.00";
            this.txtkembali.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtkembali.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            this.txtkembali.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            this.txtkembali.Properties.MaskSettings.Set("mask", "###,###,###,##0.00");
            this.txtkembali.Properties.ReadOnly = true;
            this.txtkembali.Properties.UseMaskAsDisplayFormat = true;
            this.txtkembali.Size = new System.Drawing.Size(194, 32);
            this.txtkembali.TabIndex = 5;
            // 
            // txtcash
            // 
            this.txtcash.EditValue = "0";
            this.txtcash.Location = new System.Drawing.Point(124, 83);
            this.txtcash.Name = "txtcash";
            this.txtcash.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtcash.Properties.Appearance.Options.UseFont = true;
            this.txtcash.Properties.Appearance.Options.UseTextOptions = true;
            this.txtcash.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.txtcash.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            this.txtcash.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtcash.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            this.txtcash.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtcash.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            this.txtcash.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            this.txtcash.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            this.txtcash.Properties.UseMaskAsDisplayFormat = true;
            this.txtcash.Size = new System.Drawing.Size(194, 32);
            this.txtcash.TabIndex = 0;
            this.txtcash.EditValueChanged += new System.EventHandler(this.txtcash_EditValueChanged);
            this.txtcash.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtcash_KeyDown);
            // 
            // txttotal
            // 
            this.txttotal.EditValue = "0";
            this.txttotal.Location = new System.Drawing.Point(124, 39);
            this.txttotal.Name = "txttotal";
            this.txttotal.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txttotal.Properties.Appearance.Options.UseFont = true;
            this.txttotal.Properties.Appearance.Options.UseTextOptions = true;
            this.txttotal.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.txttotal.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            this.txttotal.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txttotal.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            this.txttotal.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txttotal.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            this.txttotal.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            this.txttotal.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            this.txttotal.Properties.ReadOnly = true;
            this.txttotal.Properties.UseMaskAsDisplayFormat = true;
            this.txttotal.Size = new System.Drawing.Size(194, 32);
            this.txttotal.TabIndex = 4;
            // 
            // labelControl4
            // 
            this.labelControl4.Location = new System.Drawing.Point(212, 22);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(66, 13);
            this.labelControl4.TabIndex = 13;
            this.labelControl4.Text = "Jlh. Angsuran";
            this.labelControl4.Visible = false;
            // 
            // leangsuran
            // 
            this.leangsuran.Location = new System.Drawing.Point(212, 42);
            this.leangsuran.Name = "leangsuran";
            this.leangsuran.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.leangsuran.Size = new System.Drawing.Size(66, 20);
            this.leangsuran.TabIndex = 2;
            this.leangsuran.Visible = false;
            this.leangsuran.KeyDown += new System.Windows.Forms.KeyEventHandler(this.leangsuran_KeyDown);
            this.leangsuran.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.leangsuran_KeyPress);
            // 
            // searchLookUpEdit1
            // 
            this.searchLookUpEdit1.Location = new System.Drawing.Point(23, 42);
            this.searchLookUpEdit1.Name = "searchLookUpEdit1";
            this.searchLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.searchLookUpEdit1.Properties.PopupView = this.searchLookUpEdit1View;
            this.searchLookUpEdit1.Size = new System.Drawing.Size(153, 20);
            this.searchLookUpEdit1.TabIndex = 1;
            this.searchLookUpEdit1.Popup += new System.EventHandler(this.searchLookUpEdit1_Popup);
            this.searchLookUpEdit1.EditValueChanged += new System.EventHandler(this.searchLookUpEdit1_EditValueChanged);
            this.searchLookUpEdit1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.searchLookUpEdit1_KeyDown);
            this.searchLookUpEdit1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.searchLookUpEdit1_KeyPress);
            // 
            // searchLookUpEdit1View
            // 
            this.searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            this.searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // PelangganTextBox
            // 
            this.PelangganTextBox.Location = new System.Drawing.Point(23, 1);
            this.PelangganTextBox.Name = "PelangganTextBox";
            this.PelangganTextBox.Properties.AdvancedModeOptions.Label = "Pelanggan";
            this.PelangganTextBox.Properties.Appearance.Options.UseFont = true;
            this.PelangganTextBox.Properties.EditValueChangedDelay = 1000;
            this.PelangganTextBox.Properties.PasswordChar = '@';
            this.PelangganTextBox.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            this.PelangganTextBox.Size = new System.Drawing.Size(153, 34);
            this.PelangganTextBox.TabIndex = 0;
            this.PelangganTextBox.EditValueChanged += new System.EventHandler(this.PelangganTextBox_EditValueChanged);
            this.PelangganTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PelangganTextBox_KeyDown);
            this.PelangganTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.PelangganTextBox_KeyPress);
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(23, 68);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(95, 13);
            this.labelControl2.TabIndex = 13;
            this.labelControl2.Text = "F5, Penjualan Tunai";
            // 
            // PaymentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(381, 318);
            this.Controls.Add(this.PelangganTextBox);
            this.Controls.Add(this.searchLookUpEdit1);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.leangsuran);
            this.Controls.Add(this.groupControl2);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PaymentForm";
            this.Text = "Pembayaran";
            this.Load += new System.EventHandler(this.PaymentForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PaymentForm_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).EndInit();
            this.groupControl2.ResumeLayout(false);
            this.groupControl2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtkembali.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtcash.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txttotal.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.leangsuran.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PelangganTextBox.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.GroupControl groupControl2;
        private DevExpress.XtraEditors.SimpleButton sbtutup;
        private DevExpress.XtraEditors.SimpleButton sbsimpancetak;
        private DevExpress.XtraEditors.TextEdit txtkembali;
        private DevExpress.XtraEditors.TextEdit txtcash;
        private DevExpress.XtraEditors.TextEdit txttotal;
        private DevExpress.XtraEditors.LabelControl lblkembali;
        private DevExpress.XtraEditors.LabelControl lblbayar;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LookUpEdit leangsuran;
        private DevExpress.XtraEditors.SearchLookUpEdit searchLookUpEdit1;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit1View;
        private DevExpress.XtraEditors.TextEdit PelangganTextBox;
        private DevExpress.XtraEditors.LabelControl labelControl2;
    }
}