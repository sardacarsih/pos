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
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            radioGroup1 = new DevExpress.XtraEditors.RadioGroup();
            sbbatal = new DevExpress.XtraEditors.SimpleButton();
            sbaddorupdate = new DevExpress.XtraEditors.SimpleButton();
            pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            comboBoxEdit_status = new DevExpress.XtraEditors.ComboBoxEdit();
            checkEditnonaktif = new DevExpress.XtraEditors.CheckEdit();
            checkEditnonanggota = new DevExpress.XtraEditors.CheckEdit();
            lookUpEdit1 = new DevExpress.XtraEditors.LookUpEdit();
            dateEdit_tma = new DevExpress.XtraEditors.DateEdit();
            textEdit_nama = new DevExpress.XtraEditors.TextEdit();
            textEdit_nik = new DevExpress.XtraEditors.TextEdit();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            labelControl5 = new DevExpress.XtraEditors.LabelControl();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl6 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            txtlimithutang = new DevExpress.XtraEditors.TextEdit();
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_status.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)checkEditnonaktif.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)checkEditnonanggota.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lookUpEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit_tma.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit_tma.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)textEdit_nama.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)textEdit_nik.Properties).BeginInit();
            sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtlimithutang.Properties).BeginInit();
            SuspendLayout();
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(txtlimithutang);
            sidePanel1.Controls.Add(radioGroup1);
            sidePanel1.Controls.Add(sbbatal);
            sidePanel1.Controls.Add(sbaddorupdate);
            sidePanel1.Controls.Add(pictureEdit1);
            sidePanel1.Controls.Add(comboBoxEdit_status);
            sidePanel1.Controls.Add(checkEditnonaktif);
            sidePanel1.Controls.Add(checkEditnonanggota);
            sidePanel1.Controls.Add(lookUpEdit1);
            sidePanel1.Controls.Add(dateEdit_tma);
            sidePanel1.Controls.Add(textEdit_nama);
            sidePanel1.Controls.Add(textEdit_nik);
            sidePanel1.Controls.Add(labelControl4);
            sidePanel1.Controls.Add(labelControl5);
            sidePanel1.Controls.Add(labelControl3);
            sidePanel1.Controls.Add(labelControl6);
            sidePanel1.Controls.Add(labelControl2);
            sidePanel1.Controls.Add(labelControl1);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(710, 155);
            sidePanel1.TabIndex = 1;
            sidePanel1.Text = "sidePanel1";
            // 
            // radioGroup1
            // 
            radioGroup1.Location = new Point(376, 5);
            radioGroup1.Name = "radioGroup1";
            radioGroup1.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] { new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Anggota"), new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Pelanggan"), new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Anggota or Pelanggan Aktif") });
            radioGroup1.Size = new Size(178, 96);
            radioGroup1.TabIndex = 8;
            radioGroup1.SelectedIndexChanged += radioGroup1_SelectedIndexChanged;
            // 
            // sbbatal
            // 
            sbbatal.ImageOptions.Image = (Image)resources.GetObject("sbbatal.ImageOptions.Image");
            sbbatal.Location = new Point(405, 114);
            sbbatal.Name = "sbbatal";
            sbbatal.Size = new Size(91, 37);
            sbbatal.TabIndex = 7;
            sbbatal.Text = "Batal";
            sbbatal.Click += sbbatal_Click;
            // 
            // sbaddorupdate
            // 
            sbaddorupdate.ImageOptions.Image = (Image)resources.GetObject("sbaddorupdate.ImageOptions.Image");
            sbaddorupdate.Location = new Point(308, 114);
            sbaddorupdate.Name = "sbaddorupdate";
            sbaddorupdate.Size = new Size(91, 37);
            sbaddorupdate.TabIndex = 7;
            sbaddorupdate.Text = "Simpan";
            sbaddorupdate.Click += sbaddorupdate_Click;
            // 
            // pictureEdit1
            // 
            pictureEdit1.Location = new Point(560, 3);
            pictureEdit1.Name = "pictureEdit1";
            pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            pictureEdit1.Size = new Size(147, 146);
            pictureEdit1.TabIndex = 6;
            pictureEdit1.Click += pictureEdit1_Click;
            // 
            // comboBoxEdit_status
            // 
            comboBoxEdit_status.Location = new Point(107, 81);
            comboBoxEdit_status.Name = "comboBoxEdit_status";
            comboBoxEdit_status.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            comboBoxEdit_status.Size = new Size(100, 20);
            comboBoxEdit_status.TabIndex = 5;
            // 
            // checkEditnonaktif
            // 
            checkEditnonaktif.Location = new Point(270, 9);
            checkEditnonaktif.Name = "checkEditnonaktif";
            checkEditnonaktif.Properties.Caption = "Non Aktif";
            checkEditnonaktif.Size = new Size(114, 20);
            checkEditnonaktif.TabIndex = 4;
            // 
            // checkEditnonanggota
            // 
            checkEditnonanggota.Location = new Point(270, 33);
            checkEditnonanggota.Name = "checkEditnonanggota";
            checkEditnonanggota.Properties.Caption = "Non Anggota";
            checkEditnonanggota.Size = new Size(114, 20);
            checkEditnonanggota.TabIndex = 4;
            // 
            // lookUpEdit1
            // 
            lookUpEdit1.Location = new Point(107, 105);
            lookUpEdit1.Name = "lookUpEdit1";
            lookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lookUpEdit1.Size = new Size(100, 20);
            lookUpEdit1.TabIndex = 3;
            // 
            // dateEdit_tma
            // 
            dateEdit_tma.EditValue = null;
            dateEdit_tma.Location = new Point(107, 59);
            dateEdit_tma.Name = "dateEdit_tma";
            dateEdit_tma.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit_tma.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit_tma.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dateEdit_tma.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit_tma.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
            dateEdit_tma.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit_tma.Properties.UseMaskAsDisplayFormat = true;
            dateEdit_tma.Size = new Size(100, 20);
            dateEdit_tma.TabIndex = 2;
            dateEdit_tma.EditValueChanged += dateEdit1_EditValueChanged;
            // 
            // textEdit_nama
            // 
            textEdit_nama.Location = new Point(107, 33);
            textEdit_nama.Name = "textEdit_nama";
            textEdit_nama.Size = new Size(157, 20);
            textEdit_nama.TabIndex = 1;
            // 
            // textEdit_nik
            // 
            textEdit_nik.Location = new Point(107, 9);
            textEdit_nik.Name = "textEdit_nik";
            textEdit_nik.Properties.ReadOnly = true;
            textEdit_nik.Size = new Size(100, 20);
            textEdit_nik.TabIndex = 1;
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(12, 59);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(46, 13);
            labelControl4.TabIndex = 0;
            labelControl4.Text = "TANGGAL";
            // 
            // labelControl5
            // 
            labelControl5.Location = new Point(12, 84);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(38, 13);
            labelControl5.TabIndex = 0;
            labelControl5.Text = "STATUS";
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(12, 108);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(58, 13);
            labelControl3.TabIndex = 0;
            labelControl3.Text = "UNIT KERJA";
            labelControl3.Click += labelControl3_Click;
            // 
            // labelControl6
            // 
            labelControl6.Location = new Point(12, 132);
            labelControl6.Name = "labelControl6";
            labelControl6.Size = new Size(71, 13);
            labelControl6.TabIndex = 0;
            labelControl6.Text = "LIMIT HUTANG";
            // 
            // labelControl2
            // 
            labelControl2.Location = new Point(12, 36);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(29, 13);
            labelControl2.TabIndex = 0;
            labelControl2.Text = "NAMA";
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(12, 12);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(17, 13);
            labelControl1.TabIndex = 0;
            labelControl1.Text = "NIK";
            // 
            // sidePanel2
            // 
            sidePanel2.Controls.Add(gridControl1);
            sidePanel2.Dock = DockStyle.Fill;
            sidePanel2.Location = new Point(0, 155);
            sidePanel2.Name = "sidePanel2";
            sidePanel2.Size = new Size(710, 353);
            sidePanel2.TabIndex = 2;
            sidePanel2.Text = "sidePanel2";
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(710, 353);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsFind.AlwaysVisible = true;
            gridView1.OptionsFind.SearchInPreview = true;
            gridView1.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.RowCellStyle += gridView1_RowCellStyle;
            gridView1.DoubleClick += gridView1_DoubleClick;
            // 
            // txtlimithutang
            // 
            txtlimithutang.Location = new Point(107, 128);
            txtlimithutang.Name = "txtlimithutang";
            txtlimithutang.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtlimithutang.Properties.EditFormat.FormatString = "n0";
            txtlimithutang.Properties.UseMaskAsDisplayFormat = true;
            txtlimithutang.Size = new Size(100, 20);
            txtlimithutang.TabIndex = 9;
            // 
            // ucMasterAnggota
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucMasterAnggota";
            Size = new Size(710, 508);
            Load += ucMasterAnggota_Load;
            sidePanel1.ResumeLayout(false);
            sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_status.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)checkEditnonaktif.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)checkEditnonanggota.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)lookUpEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit_tma.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit_tma.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)textEdit_nama.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)textEdit_nik.Properties).EndInit();
            sidePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtlimithutang.Properties).EndInit();
            ResumeLayout(false);
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
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.RadioGroup radioGroup1;
        private DevExpress.XtraEditors.TextEdit txtlimithutang;
    }
}
