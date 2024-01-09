namespace Penjualan
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
            groupControl2 = new DevExpress.XtraEditors.GroupControl();
            lblkembali = new DevExpress.XtraEditors.LabelControl();
            lblbayar = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            sbtutup = new DevExpress.XtraEditors.SimpleButton();
            sbsimpancetak = new DevExpress.XtraEditors.SimpleButton();
            txtkembali = new DevExpress.XtraEditors.TextEdit();
            txtcash = new DevExpress.XtraEditors.TextEdit();
            txttotal = new DevExpress.XtraEditors.TextEdit();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            leangsuran = new DevExpress.XtraEditors.LookUpEdit();
            searchLookUpEdit1 = new DevExpress.XtraEditors.SearchLookUpEdit();
            searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            PelangganTextBox = new DevExpress.XtraEditors.TextEdit();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)groupControl2).BeginInit();
            groupControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtkembali.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtcash.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txttotal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1View).BeginInit();
            ((System.ComponentModel.ISupportInitialize)PelangganTextBox.Properties).BeginInit();
            SuspendLayout();
            // 
            // groupControl2
            // 
            groupControl2.Controls.Add(lblkembali);
            groupControl2.Controls.Add(lblbayar);
            groupControl2.Controls.Add(labelControl1);
            groupControl2.Controls.Add(sbtutup);
            groupControl2.Controls.Add(sbsimpancetak);
            groupControl2.Controls.Add(txtkembali);
            groupControl2.Controls.Add(txtcash);
            groupControl2.Controls.Add(txttotal);
            groupControl2.Location = new Point(23, 89);
            groupControl2.Name = "groupControl2";
            groupControl2.Size = new Size(334, 215);
            groupControl2.TabIndex = 3;
            groupControl2.Text = "Pembayaran Detail";
            // 
            // lblkembali
            // 
            lblkembali.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            lblkembali.Appearance.Options.UseFont = true;
            lblkembali.Location = new Point(19, 124);
            lblkembali.Name = "lblkembali";
            lblkembali.Size = new Size(74, 25);
            lblkembali.TabIndex = 2;
            lblkembali.Text = "Kembali";
            // 
            // lblbayar
            // 
            lblbayar.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            lblbayar.Appearance.Options.UseFont = true;
            lblbayar.Location = new Point(19, 86);
            lblbayar.Name = "lblbayar";
            lblbayar.Size = new Size(53, 25);
            lblbayar.TabIndex = 2;
            lblbayar.Text = "Bayar";
            // 
            // labelControl1
            // 
            labelControl1.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            labelControl1.Appearance.Options.UseFont = true;
            labelControl1.Location = new Point(19, 42);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(46, 25);
            labelControl1.TabIndex = 2;
            labelControl1.Text = "Total";
            // 
            // sbtutup
            // 
            sbtutup.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("sbtutup.ImageOptions.SvgImage");
            sbtutup.Location = new Point(189, 173);
            sbtutup.Name = "sbtutup";
            sbtutup.Size = new Size(129, 37);
            sbtutup.TabIndex = 2;
            sbtutup.Text = "Tutup";
            sbtutup.Click += sbtutup_Click;
            // 
            // sbsimpancetak
            // 
            sbsimpancetak.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("sbsimpancetak.ImageOptions.SvgImage");
            sbsimpancetak.Location = new Point(16, 173);
            sbsimpancetak.Name = "sbsimpancetak";
            sbsimpancetak.Size = new Size(124, 37);
            sbsimpancetak.TabIndex = 1;
            sbsimpancetak.Text = "Simpan";
            sbsimpancetak.Click += sbsimpancetak_Click;
            // 
            // txtkembali
            // 
            txtkembali.EditValue = "0";
            txtkembali.Location = new Point(124, 121);
            txtkembali.Name = "txtkembali";
            txtkembali.Properties.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            txtkembali.Properties.Appearance.Options.UseFont = true;
            txtkembali.Properties.Appearance.Options.UseTextOptions = true;
            txtkembali.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            txtkembali.Properties.DisplayFormat.FormatString = "###,###,###,##0.00";
            txtkembali.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtkembali.Properties.EditFormat.FormatString = "###,###,###,##0.00";
            txtkembali.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtkembali.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtkembali.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txtkembali.Properties.MaskSettings.Set("mask", "###,###,###,##0.00");
            txtkembali.Properties.ReadOnly = true;
            txtkembali.Properties.UseMaskAsDisplayFormat = true;
            txtkembali.Size = new Size(194, 32);
            txtkembali.TabIndex = 5;
            // 
            // txtcash
            // 
            txtcash.EditValue = "0";
            txtcash.Location = new Point(124, 83);
            txtcash.Name = "txtcash";
            txtcash.Properties.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            txtcash.Properties.Appearance.Options.UseFont = true;
            txtcash.Properties.Appearance.Options.UseTextOptions = true;
            txtcash.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            txtcash.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            txtcash.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtcash.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            txtcash.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtcash.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtcash.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txtcash.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            txtcash.Properties.UseMaskAsDisplayFormat = true;
            txtcash.Size = new Size(194, 32);
            txtcash.TabIndex = 0;
            txtcash.EditValueChanged += txtcash_EditValueChanged;
            txtcash.KeyDown += txtcash_KeyDown;
            // 
            // txttotal
            // 
            txttotal.EditValue = "0";
            txttotal.Location = new Point(124, 39);
            txttotal.Name = "txttotal";
            txttotal.Properties.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Regular, GraphicsUnit.Point);
            txttotal.Properties.Appearance.Options.UseFont = true;
            txttotal.Properties.Appearance.Options.UseTextOptions = true;
            txttotal.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            txttotal.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            txttotal.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txttotal.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            txttotal.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txttotal.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txttotal.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txttotal.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            txttotal.Properties.ReadOnly = true;
            txttotal.Properties.UseMaskAsDisplayFormat = true;
            txttotal.Size = new Size(194, 32);
            txttotal.TabIndex = 4;
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(212, 22);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(66, 13);
            labelControl4.TabIndex = 13;
            labelControl4.Text = "Jlh. Angsuran";
            labelControl4.Visible = false;
            // 
            // leangsuran
            // 
            leangsuran.Location = new Point(212, 42);
            leangsuran.Name = "leangsuran";
            leangsuran.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            leangsuran.Size = new Size(66, 20);
            leangsuran.TabIndex = 2;
            leangsuran.Visible = false;
            leangsuran.EditValueChanged += leangsuran_EditValueChanged;
            leangsuran.KeyDown += leangsuran_KeyDown;
            // searchLookUpEdit1
            // 
            searchLookUpEdit1.Location = new Point(23, 42);
            searchLookUpEdit1.Name = "searchLookUpEdit1";
            searchLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            searchLookUpEdit1.Properties.PopupView = searchLookUpEdit1View;
            searchLookUpEdit1.Size = new Size(153, 20);
            searchLookUpEdit1.TabIndex = 1;
            searchLookUpEdit1.Popup += searchLookUpEdit1_Popup;
            searchLookUpEdit1.EditValueChanged += searchLookUpEdit1_EditValueChanged;
            searchLookUpEdit1.KeyDown += searchLookUpEdit1_KeyDown;
            // 
            // searchLookUpEdit1View
            // 
            searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // PelangganTextBox
            // 
            PelangganTextBox.Location = new Point(23, 1);
            PelangganTextBox.Name = "PelangganTextBox";
            PelangganTextBox.Properties.AdvancedModeOptions.Label = "Pelanggan";
            PelangganTextBox.Properties.Appearance.Options.UseFont = true;
            PelangganTextBox.Properties.EditValueChangedDelay = 1000;
            PelangganTextBox.Properties.PasswordChar = '@';
            PelangganTextBox.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            PelangganTextBox.Size = new Size(153, 34);
            PelangganTextBox.TabIndex = 0;
            PelangganTextBox.KeyDown += PelangganTextBox_KeyDown;
            // labelControl2
            // 
            labelControl2.Location = new Point(23, 68);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(95, 13);
            labelControl2.TabIndex = 13;
            labelControl2.Text = "F5, Penjualan Tunai";
            // 
            // PaymentForm
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(381, 318);
            Controls.Add(PelangganTextBox);
            Controls.Add(searchLookUpEdit1);
            Controls.Add(labelControl2);
            Controls.Add(labelControl4);
            Controls.Add(leangsuran);
            Controls.Add(groupControl2);
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PaymentForm";
            Text = "Pembayaran";
            Load += PaymentForm_Load;
            KeyDown += PaymentForm_KeyDown;
            ((System.ComponentModel.ISupportInitialize)groupControl2).EndInit();
            groupControl2.ResumeLayout(false);
            groupControl2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)txtkembali.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtcash.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txttotal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1View).EndInit();
            ((System.ComponentModel.ISupportInitialize)PelangganTextBox.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
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