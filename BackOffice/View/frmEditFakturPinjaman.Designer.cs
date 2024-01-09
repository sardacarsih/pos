namespace BackOffice.View
{
    partial class frmEditFakturPinjaman
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEditFakturPinjaman));
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.leangsuran = new DevExpress.XtraEditors.LookUpEdit();
            this.searchLookUpEdit1 = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.txtfaktur = new DevExpress.XtraEditors.TextEdit();
            this.sbsimpancetak = new DevExpress.XtraEditors.SimpleButton();
            this.groupControl2 = new DevExpress.XtraEditors.GroupControl();
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.txtpelanggan = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.leangsuran.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtfaktur.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).BeginInit();
            this.groupControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtpelanggan.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // labelControl4
            // 
            this.labelControl4.Location = new System.Drawing.Point(349, 31);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(66, 13);
            this.labelControl4.TabIndex = 13;
            this.labelControl4.Text = "Jlh. Angsuran";
            // 
            // leangsuran
            // 
            this.leangsuran.Location = new System.Drawing.Point(421, 25);
            this.leangsuran.Name = "leangsuran";
            this.leangsuran.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.leangsuran.Properties.ReadOnly = true;
            this.leangsuran.Size = new System.Drawing.Size(74, 20);
            this.leangsuran.TabIndex = 6;
            this.leangsuran.KeyDown += new System.Windows.Forms.KeyEventHandler(this.leangsuran_KeyDown);
            // 
            // searchLookUpEdit1
            // 
            this.searchLookUpEdit1.Location = new System.Drawing.Point(421, 2);
            this.searchLookUpEdit1.Name = "searchLookUpEdit1";
            this.searchLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.searchLookUpEdit1.Properties.PopupView = this.searchLookUpEdit1View;
            this.searchLookUpEdit1.Size = new System.Drawing.Size(127, 20);
            this.searchLookUpEdit1.TabIndex = 0;
            this.searchLookUpEdit1.Popup += new System.EventHandler(this.searchLookUpEdit1_Popup);
            this.searchLookUpEdit1.EditValueChanged += new System.EventHandler(this.searchLookUpEdit1_EditValueChanged);
            this.searchLookUpEdit1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.searchLookUpEdit1_KeyDown);
            // 
            // searchLookUpEdit1View
            // 
            this.searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            this.searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // txtfaktur
            // 
            this.txtfaktur.Location = new System.Drawing.Point(23, 1);
            this.txtfaktur.Name = "txtfaktur";
            this.txtfaktur.Properties.AdvancedModeOptions.Label = "Nomor Faktur";
            this.txtfaktur.Properties.Appearance.Options.UseFont = true;
            this.txtfaktur.Properties.EditValueChangedDelay = 1000;
            this.txtfaktur.Properties.ReadOnly = true;
            this.txtfaktur.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            this.txtfaktur.Size = new System.Drawing.Size(153, 34);
            this.txtfaktur.TabIndex = 5;
            // 
            // sbsimpancetak
            // 
            this.sbsimpancetak.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("sbsimpancetak.ImageOptions.Image")));
            this.sbsimpancetak.Location = new System.Drawing.Point(554, 2);
            this.sbsimpancetak.Name = "sbsimpancetak";
            this.sbsimpancetak.Size = new System.Drawing.Size(89, 36);
            this.sbsimpancetak.TabIndex = 1;
            this.sbsimpancetak.Text = "Update";
            this.sbsimpancetak.Click += new System.EventHandler(this.sbsimpancetak_Click);
            // 
            // groupControl2
            // 
            this.groupControl2.Controls.Add(this.gridControl1);
            this.groupControl2.Location = new System.Drawing.Point(23, 52);
            this.groupControl2.Name = "groupControl2";
            this.groupControl2.Size = new System.Drawing.Size(620, 351);
            this.groupControl2.TabIndex = 3;
            this.groupControl2.Text = "Angsuran";
            // 
            // gridControl1
            // 
            this.gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl1.Location = new System.Drawing.Point(2, 23);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(616, 326);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowFooter = true;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // txtpelanggan
            // 
            this.txtpelanggan.Location = new System.Drawing.Point(182, 1);
            this.txtpelanggan.Name = "txtpelanggan";
            this.txtpelanggan.Properties.AdvancedModeOptions.Label = "Pelanggan";
            this.txtpelanggan.Properties.Appearance.Options.UseFont = true;
            this.txtpelanggan.Properties.EditValueChangedDelay = 1000;
            this.txtpelanggan.Properties.ReadOnly = true;
            this.txtpelanggan.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            this.txtpelanggan.Size = new System.Drawing.Size(153, 34);
            this.txtpelanggan.TabIndex = 4;
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(365, 5);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(50, 13);
            this.labelControl1.TabIndex = 13;
            this.labelControl1.Text = "Pelanggan";
            // 
            // frmEditFaktur
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(655, 415);
            this.Controls.Add(this.txtpelanggan);
            this.Controls.Add(this.txtfaktur);
            this.Controls.Add(this.sbsimpancetak);
            this.Controls.Add(this.searchLookUpEdit1);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.leangsuran);
            this.Controls.Add(this.groupControl2);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmEditFaktur";
            this.Text = "Edit Faktur";
            this.Load += new System.EventHandler(this.frmEditFaktur_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PaymentForm_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.leangsuran.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtfaktur.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).EndInit();
            this.groupControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtpelanggan.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LookUpEdit leangsuran;
        private DevExpress.XtraEditors.SearchLookUpEdit searchLookUpEdit1;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit1View;
        private DevExpress.XtraEditors.TextEdit txtfaktur;
        private DevExpress.XtraEditors.SimpleButton sbsimpancetak;
        private DevExpress.XtraEditors.GroupControl groupControl2;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.TextEdit txtpelanggan;
        private DevExpress.XtraEditors.LabelControl labelControl1;
    }
}