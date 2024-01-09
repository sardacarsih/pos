namespace BackOffice.UC
{
    partial class ucPenerimaanPembayaran
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPenerimaanPembayaran));
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.sbbatal = new DevExpress.XtraEditors.SimpleButton();
            this.sbsimpan = new DevExpress.XtraEditors.SimpleButton();
            this.searchLookUpEdit_unitkerja = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.spinEdit_tahun = new DevExpress.XtraEditors.SpinEdit();
            this.comboBoxEdit_remise = new DevExpress.XtraEditors.ComboBoxEdit();
            this.comboBoxEdit_bulan = new DevExpress.XtraEditors.ComboBoxEdit();
            this.sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit_unitkerja.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinEdit_tahun.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_remise.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_bulan.Properties)).BeginInit();
            this.sidePanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl1.Location = new System.Drawing.Point(0, 0);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(832, 401);
            this.gridControl1.TabIndex = 2;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowFooter = true;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            this.gridView1.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.gridView1_CellValueChanged);
            this.gridView1.ValidatingEditor += new DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler(this.gridView1_ValidatingEditor);
            // 
            // sidePanel1
            // 
            this.sidePanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sidePanel1.Controls.Add(this.labelControl1);
            this.sidePanel1.Controls.Add(this.sbbatal);
            this.sidePanel1.Controls.Add(this.sbsimpan);
            this.sidePanel1.Controls.Add(this.searchLookUpEdit_unitkerja);
            this.sidePanel1.Controls.Add(this.spinEdit_tahun);
            this.sidePanel1.Controls.Add(this.comboBoxEdit_remise);
            this.sidePanel1.Controls.Add(this.comboBoxEdit_bulan);
            this.sidePanel1.Location = new System.Drawing.Point(0, 0);
            this.sidePanel1.Name = "sidePanel1";
            this.sidePanel1.Size = new System.Drawing.Size(832, 39);
            this.sidePanel1.TabIndex = 7;
            this.sidePanel1.Text = "sidePanel1";
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(3, 6);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(36, 13);
            this.labelControl1.TabIndex = 4;
            this.labelControl1.Text = "Periode";
            // 
            // sbbatal
            // 
            this.sbbatal.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("sbbatal.ImageOptions.Image")));
            this.sbbatal.Location = new System.Drawing.Point(566, 1);
            this.sbbatal.Name = "sbbatal";
            this.sbbatal.Size = new System.Drawing.Size(91, 34);
            this.sbbatal.TabIndex = 3;
            this.sbbatal.Text = "Batal";
            this.sbbatal.Click += new System.EventHandler(this.sbbatal_Click);
            // 
            // sbsimpan
            // 
            this.sbsimpan.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("sbsimpan.ImageOptions.Image")));
            this.sbsimpan.Location = new System.Drawing.Point(663, 1);
            this.sbsimpan.Name = "sbsimpan";
            this.sbsimpan.Size = new System.Drawing.Size(91, 34);
            this.sbsimpan.TabIndex = 3;
            this.sbsimpan.Text = "Simpan";
            this.sbsimpan.Click += new System.EventHandler(this.sbsimpan_Click);
            // 
            // searchLookUpEdit_unitkerja
            // 
            this.searchLookUpEdit_unitkerja.Location = new System.Drawing.Point(407, 3);
            this.searchLookUpEdit_unitkerja.Name = "searchLookUpEdit_unitkerja";
            this.searchLookUpEdit_unitkerja.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.searchLookUpEdit_unitkerja.Properties.PopupView = this.searchLookUpEdit1View;
            this.searchLookUpEdit_unitkerja.Size = new System.Drawing.Size(153, 20);
            this.searchLookUpEdit_unitkerja.TabIndex = 2;
            this.searchLookUpEdit_unitkerja.EditValueChanged += new System.EventHandler(this.searchLookUpEdit_unitkerja_EditValueChanged);
            // 
            // searchLookUpEdit1View
            // 
            this.searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            this.searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // spinEdit_tahun
            // 
            this.spinEdit_tahun.EditValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.spinEdit_tahun.Location = new System.Drawing.Point(182, 3);
            this.spinEdit_tahun.Name = "spinEdit_tahun";
            this.spinEdit_tahun.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.spinEdit_tahun.Size = new System.Drawing.Size(64, 20);
            this.spinEdit_tahun.TabIndex = 1;
            this.spinEdit_tahun.EditValueChanged += new System.EventHandler(this.spinEdit_tahun_EditValueChanged);
            // 
            // comboBoxEdit_remise
            // 
            this.comboBoxEdit_remise.Location = new System.Drawing.Point(252, 3);
            this.comboBoxEdit_remise.Name = "comboBoxEdit_remise";
            this.comboBoxEdit_remise.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxEdit_remise.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.comboBoxEdit_remise.Size = new System.Drawing.Size(149, 20);
            this.comboBoxEdit_remise.TabIndex = 0;
            this.comboBoxEdit_remise.SelectedIndexChanged += new System.EventHandler(this.comboBoxEdit_remise_SelectedIndexChanged);
            // 
            // comboBoxEdit_bulan
            // 
            this.comboBoxEdit_bulan.Location = new System.Drawing.Point(55, 3);
            this.comboBoxEdit_bulan.Name = "comboBoxEdit_bulan";
            this.comboBoxEdit_bulan.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxEdit_bulan.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.comboBoxEdit_bulan.Size = new System.Drawing.Size(121, 20);
            this.comboBoxEdit_bulan.TabIndex = 0;
            this.comboBoxEdit_bulan.SelectedIndexChanged += new System.EventHandler(this.comboBoxEdit_bulan_SelectedIndexChanged);
            // 
            // sidePanel2
            // 
            this.sidePanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sidePanel2.Controls.Add(this.gridControl1);
            this.sidePanel2.Location = new System.Drawing.Point(0, 39);
            this.sidePanel2.Name = "sidePanel2";
            this.sidePanel2.Size = new System.Drawing.Size(832, 401);
            this.sidePanel2.TabIndex = 8;
            this.sidePanel2.Text = "sidePanel2";
            // 
            // ucPenerimaanPembayaran
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.sidePanel2);
            this.Controls.Add(this.sidePanel1);
            this.Name = "ucPenerimaanPembayaran";
            this.Size = new System.Drawing.Size(832, 440);
            this.Load += new System.EventHandler(this.ucPenerimaanPembayaran_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.sidePanel1.ResumeLayout(false);
            this.sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit_unitkerja.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spinEdit_tahun.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_remise.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_bulan.Properties)).EndInit();
            this.sidePanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton sbsimpan;
        private DevExpress.XtraEditors.SearchLookUpEdit searchLookUpEdit_unitkerja;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit1View;
        private DevExpress.XtraEditors.SpinEdit spinEdit_tahun;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit_remise;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit_bulan;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
        private DevExpress.XtraEditors.SimpleButton sbbatal;
    }
}
