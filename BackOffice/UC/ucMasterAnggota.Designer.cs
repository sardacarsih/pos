namespace BackOffice.UC
{
    partial class ucMasterAnggota
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucMasterAnggota));
            this.sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            this.radioGroup1 = new DevExpress.XtraEditors.RadioGroup();
            this.sbbatal = new DevExpress.XtraEditors.SimpleButton();
            this.sbaddorupdate = new DevExpress.XtraEditors.SimpleButton();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.comboBoxEdit_status = new DevExpress.XtraEditors.ComboBoxEdit();
            this.checkEditnonaktif = new DevExpress.XtraEditors.CheckEdit();
            this.checkEditnonanggota = new DevExpress.XtraEditors.CheckEdit();
            this.lookUpEdit1 = new DevExpress.XtraEditors.LookUpEdit();
            this.dateEdit_tma = new DevExpress.XtraEditors.DateEdit();
            this.textEdit_nama = new DevExpress.XtraEditors.TextEdit();
            this.textEdit_LIMITHUTANG = new DevExpress.XtraEditors.TextEdit();
            this.textEdit_nik = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radioGroup1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_status.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEditnonaktif.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEditnonanggota.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateEdit_tma.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateEdit_tma.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_nama.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_LIMITHUTANG.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_nik.Properties)).BeginInit();
            this.sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // sidePanel1
            // 
            this.sidePanel1.Controls.Add(this.radioGroup1);
            this.sidePanel1.Controls.Add(this.sbbatal);
            this.sidePanel1.Controls.Add(this.sbaddorupdate);
            this.sidePanel1.Controls.Add(this.pictureEdit1);
            this.sidePanel1.Controls.Add(this.comboBoxEdit_status);
            this.sidePanel1.Controls.Add(this.checkEditnonaktif);
            this.sidePanel1.Controls.Add(this.checkEditnonanggota);
            this.sidePanel1.Controls.Add(this.lookUpEdit1);
            this.sidePanel1.Controls.Add(this.dateEdit_tma);
            this.sidePanel1.Controls.Add(this.textEdit_nama);
            this.sidePanel1.Controls.Add(this.textEdit_LIMITHUTANG);
            this.sidePanel1.Controls.Add(this.textEdit_nik);
            this.sidePanel1.Controls.Add(this.labelControl4);
            this.sidePanel1.Controls.Add(this.labelControl5);
            this.sidePanel1.Controls.Add(this.labelControl3);
            this.sidePanel1.Controls.Add(this.labelControl6);
            this.sidePanel1.Controls.Add(this.labelControl2);
            this.sidePanel1.Controls.Add(this.labelControl1);
            this.sidePanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.sidePanel1.Location = new System.Drawing.Point(0, 0);
            this.sidePanel1.Name = "sidePanel1";
            this.sidePanel1.Size = new System.Drawing.Size(710, 155);
            this.sidePanel1.TabIndex = 1;
            this.sidePanel1.Text = "sidePanel1";
            // 
            // radioGroup1
            // 
            this.radioGroup1.Location = new System.Drawing.Point(376, 5);
            this.radioGroup1.Name = "radioGroup1";
            this.radioGroup1.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Anggota"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Pelanggan"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Anggota or Pelanggan Aktif")});
            this.radioGroup1.Size = new System.Drawing.Size(178, 96);
            this.radioGroup1.TabIndex = 8;
            this.radioGroup1.SelectedIndexChanged += new System.EventHandler(this.radioGroup1_SelectedIndexChanged);
            // 
            // sbbatal
            // 
            this.sbbatal.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("sbbatal.ImageOptions.Image")));
            this.sbbatal.Location = new System.Drawing.Point(405, 114);
            this.sbbatal.Name = "sbbatal";
            this.sbbatal.Size = new System.Drawing.Size(91, 37);
            this.sbbatal.TabIndex = 7;
            this.sbbatal.Text = "Batal";
            this.sbbatal.Click += new System.EventHandler(this.sbbatal_Click);
            // 
            // sbaddorupdate
            // 
            this.sbaddorupdate.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("sbaddorupdate.ImageOptions.Image")));
            this.sbaddorupdate.Location = new System.Drawing.Point(308, 114);
            this.sbaddorupdate.Name = "sbaddorupdate";
            this.sbaddorupdate.Size = new System.Drawing.Size(91, 37);
            this.sbaddorupdate.TabIndex = 7;
            this.sbaddorupdate.Text = "Simpan";
            this.sbaddorupdate.Click += new System.EventHandler(this.sbaddorupdate_Click);
            // 
            // pictureEdit1
            // 
            this.pictureEdit1.Location = new System.Drawing.Point(560, 3);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit1.Size = new System.Drawing.Size(147, 146);
            this.pictureEdit1.TabIndex = 6;
            this.pictureEdit1.Click += new System.EventHandler(this.pictureEdit1_Click);
            // 
            // comboBoxEdit_status
            // 
            this.comboBoxEdit_status.Location = new System.Drawing.Point(107, 81);
            this.comboBoxEdit_status.Name = "comboBoxEdit_status";
            this.comboBoxEdit_status.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxEdit_status.Size = new System.Drawing.Size(100, 20);
            this.comboBoxEdit_status.TabIndex = 5;
            // 
            // checkEditnonaktif
            // 
            this.checkEditnonaktif.Location = new System.Drawing.Point(270, 9);
            this.checkEditnonaktif.Name = "checkEditnonaktif";
            this.checkEditnonaktif.Properties.Caption = "Non Aktif";
            this.checkEditnonaktif.Size = new System.Drawing.Size(114, 20);
            this.checkEditnonaktif.TabIndex = 4;
            // 
            // checkEditnonanggota
            // 
            this.checkEditnonanggota.Location = new System.Drawing.Point(270, 33);
            this.checkEditnonanggota.Name = "checkEditnonanggota";
            this.checkEditnonanggota.Properties.Caption = "Non Anggota";
            this.checkEditnonanggota.Size = new System.Drawing.Size(114, 20);
            this.checkEditnonanggota.TabIndex = 4;
            // 
            // lookUpEdit1
            // 
            this.lookUpEdit1.Location = new System.Drawing.Point(107, 105);
            this.lookUpEdit1.Name = "lookUpEdit1";
            this.lookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lookUpEdit1.Size = new System.Drawing.Size(100, 20);
            this.lookUpEdit1.TabIndex = 3;
            // 
            // dateEdit_tma
            // 
            this.dateEdit_tma.EditValue = null;
            this.dateEdit_tma.Location = new System.Drawing.Point(107, 59);
            this.dateEdit_tma.Name = "dateEdit_tma";
            this.dateEdit_tma.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateEdit_tma.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateEdit_tma.Size = new System.Drawing.Size(100, 20);
            this.dateEdit_tma.TabIndex = 2;
            this.dateEdit_tma.EditValueChanged += new System.EventHandler(this.dateEdit1_EditValueChanged);
            // 
            // textEdit_nama
            // 
            this.textEdit_nama.Location = new System.Drawing.Point(107, 33);
            this.textEdit_nama.Name = "textEdit_nama";
            this.textEdit_nama.Size = new System.Drawing.Size(157, 20);
            this.textEdit_nama.TabIndex = 1;
            // 
            // textEdit_LIMITHUTANG
            // 
            this.textEdit_LIMITHUTANG.Location = new System.Drawing.Point(107, 129);
            this.textEdit_LIMITHUTANG.Name = "textEdit_LIMITHUTANG";
            this.textEdit_LIMITHUTANG.Size = new System.Drawing.Size(100, 20);
            this.textEdit_LIMITHUTANG.TabIndex = 1;
            // 
            // textEdit_nik
            // 
            this.textEdit_nik.Location = new System.Drawing.Point(107, 9);
            this.textEdit_nik.Name = "textEdit_nik";
            this.textEdit_nik.Properties.ReadOnly = true;
            this.textEdit_nik.Size = new System.Drawing.Size(100, 20);
            this.textEdit_nik.TabIndex = 1;
            // 
            // labelControl4
            // 
            this.labelControl4.Location = new System.Drawing.Point(12, 59);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(46, 13);
            this.labelControl4.TabIndex = 0;
            this.labelControl4.Text = "TANGGAL";
            // 
            // labelControl5
            // 
            this.labelControl5.Location = new System.Drawing.Point(12, 84);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(38, 13);
            this.labelControl5.TabIndex = 0;
            this.labelControl5.Text = "STATUS";
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(12, 108);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(58, 13);
            this.labelControl3.TabIndex = 0;
            this.labelControl3.Text = "UNIT KERJA";
            this.labelControl3.Click += new System.EventHandler(this.labelControl3_Click);
            // 
            // labelControl6
            // 
            this.labelControl6.Location = new System.Drawing.Point(12, 132);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(71, 13);
            this.labelControl6.TabIndex = 0;
            this.labelControl6.Text = "LIMIT HUTANG";
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(12, 36);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(29, 13);
            this.labelControl2.TabIndex = 0;
            this.labelControl2.Text = "NAMA";
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(12, 12);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(17, 13);
            this.labelControl1.TabIndex = 0;
            this.labelControl1.Text = "NIK";
            // 
            // sidePanel2
            // 
            this.sidePanel2.Controls.Add(this.gridControl1);
            this.sidePanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sidePanel2.Location = new System.Drawing.Point(0, 155);
            this.sidePanel2.Name = "sidePanel2";
            this.sidePanel2.Size = new System.Drawing.Size(710, 353);
            this.sidePanel2.TabIndex = 2;
            this.sidePanel2.Text = "sidePanel2";
            // 
            // gridControl1
            // 
            this.gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl1.Location = new System.Drawing.Point(0, 0);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(710, 353);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsBehavior.Editable = false;
            this.gridView1.OptionsFind.AlwaysVisible = true;
            this.gridView1.OptionsFind.SearchInPreview = true;
            this.gridView1.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            this.gridView1.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.gridView1_RowCellStyle);
            this.gridView1.DoubleClick += new System.EventHandler(this.gridView1_DoubleClick);
            // 
            // ucMasterAnggota
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.sidePanel2);
            this.Controls.Add(this.sidePanel1);
            this.Name = "ucMasterAnggota";
            this.Size = new System.Drawing.Size(710, 508);
            this.Load += new System.EventHandler(this.ucMasterAnggota_Load);
            this.sidePanel1.ResumeLayout(false);
            this.sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radioGroup1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit_status.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEditnonaktif.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEditnonanggota.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateEdit_tma.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateEdit_tma.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_nama.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_LIMITHUTANG.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit_nik.Properties)).EndInit();
            this.sidePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.LookUpEdit lookUpEdit1;
        private DevExpress.XtraEditors.DateEdit dateEdit_tma;
        private DevExpress.XtraEditors.TextEdit textEdit_nik;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.CheckEdit checkEditnonaktif;
        private DevExpress.XtraEditors.CheckEdit checkEditnonanggota;
        private DevExpress.XtraEditors.TextEdit textEdit_nama;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.SimpleButton sbaddorupdate;
        private DevExpress.XtraEditors.PictureEdit pictureEdit1;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit_status;
        private DevExpress.XtraEditors.SimpleButton sbbatal;
        private DevExpress.XtraEditors.TextEdit textEdit_LIMITHUTANG;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.RadioGroup radioGroup1;
    }
}
