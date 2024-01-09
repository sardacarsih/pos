namespace BackOffice.UC
{
    partial class ucPinjaman
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPinjaman));
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar1 = new DevExpress.XtraBars.Bar();
            blbisimpan = new DevExpress.XtraBars.BarLargeButtonItem();
            blbisimulasi = new DevExpress.XtraBars.BarLargeButtonItem();
            blbibatal = new DevExpress.XtraBars.BarLargeButtonItem();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            bbibayar = new DevExpress.XtraBars.BarButtonItem();
            bbipending = new DevExpress.XtraBars.BarButtonItem();
            bbibatal = new DevExpress.XtraBars.BarButtonItem();
            barLargeButtonItem2 = new DevExpress.XtraBars.BarLargeButtonItem();
            groupControl1 = new DevExpress.XtraEditors.GroupControl();
            txtketerangan = new DevExpress.XtraEditors.MemoEdit();
            lblterbilang = new DevExpress.XtraEditors.LabelControl();
            labelControl7 = new DevExpress.XtraEditors.LabelControl();
            labelControl9 = new DevExpress.XtraEditors.LabelControl();
            labelControl8 = new DevExpress.XtraEditors.LabelControl();
            labelControl5 = new DevExpress.XtraEditors.LabelControl();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            leangsuran = new DevExpress.XtraEditors.LookUpEdit();
            leanggota = new DevExpress.XtraEditors.LookUpEdit();
            detanggal = new DevExpress.XtraEditors.DateEdit();
            txtjam = new DevExpress.XtraEditors.TextEdit();
            txtkasir = new DevExpress.XtraEditors.TextEdit();
            txtbunga = new DevExpress.XtraEditors.TextEdit();
            txtpinjaman = new DevExpress.XtraEditors.TextEdit();
            txtnotransaksi = new DevExpress.XtraEditors.TextEdit();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            pictureBoxVideo = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).BeginInit();
            groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtketerangan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)leanggota.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtjam.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtkasir.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtbunga.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpinjaman.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVideo).BeginInit();
            SuspendLayout();
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar1 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bbibayar, bbipending, bbibatal, barLargeButtonItem2, blbisimpan, blbibatal, blbisimulasi });
            barManager1.MainMenu = bar1;
            barManager1.MaxItemId = 11;
            // 
            // bar1
            // 
            bar1.BarName = "Custom 3";
            bar1.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Left;
            bar1.DockCol = 0;
            bar1.DockRow = 0;
            bar1.DockStyle = DevExpress.XtraBars.BarDockStyle.Left;
            bar1.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(blbisimpan), new DevExpress.XtraBars.LinkPersistInfo(blbisimulasi), new DevExpress.XtraBars.LinkPersistInfo(blbibatal) });
            bar1.OptionsBar.MultiLine = true;
            bar1.OptionsBar.UseWholeRow = true;
            bar1.Text = "Custom 3";
            // 
            // blbisimpan
            // 
            blbisimpan.Caption = "&Simpan";
            blbisimpan.Enabled = false;
            blbisimpan.Id = 7;
            blbisimpan.ImageOptions.Image = (Image)resources.GetObject("blbisimpan.ImageOptions.Image");
            blbisimpan.ImageOptions.LargeImage = (Image)resources.GetObject("blbisimpan.ImageOptions.LargeImage");
            blbisimpan.Name = "blbisimpan";
            blbisimpan.ItemClick += blbisimpan_ItemClick;
            // 
            // blbisimulasi
            // 
            blbisimulasi.Caption = "Simulasi";
            blbisimulasi.Id = 10;
            blbisimulasi.ImageOptions.Image = (Image)resources.GetObject("blbisimulasi.ImageOptions.Image");
            blbisimulasi.ImageOptions.LargeImage = (Image)resources.GetObject("blbisimulasi.ImageOptions.LargeImage");
            blbisimulasi.Name = "blbisimulasi";
            blbisimulasi.ItemClick += blbisimulasi_ItemClick;
            // 
            // blbibatal
            // 
            blbibatal.Caption = "Batal";
            blbibatal.Id = 9;
            blbibatal.ImageOptions.Image = (Image)resources.GetObject("blbibatal.ImageOptions.Image");
            blbibatal.ImageOptions.LargeImage = (Image)resources.GetObject("blbibatal.ImageOptions.LargeImage");
            blbibatal.Name = "blbibatal";
            blbibatal.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            blbibatal.ItemClick += blbibatal_ItemClick;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(876, 0);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 658);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(876, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 0);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(67, 658);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(876, 0);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 658);
            // 
            // bbibayar
            // 
            bbibayar.Caption = "Bayar";
            bbibayar.Id = 0;
            bbibayar.ImageOptions.Image = (Image)resources.GetObject("bbibayar.ImageOptions.Image");
            bbibayar.ImageOptions.LargeImage = (Image)resources.GetObject("bbibayar.ImageOptions.LargeImage");
            bbibayar.Name = "bbibayar";
            bbibayar.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbipending
            // 
            bbipending.Caption = "Pending";
            bbipending.Id = 1;
            bbipending.ImageOptions.Image = (Image)resources.GetObject("bbipending.ImageOptions.Image");
            bbipending.ImageOptions.LargeImage = (Image)resources.GetObject("bbipending.ImageOptions.LargeImage");
            bbipending.Name = "bbipending";
            bbipending.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbibatal
            // 
            bbibatal.Caption = "Batal";
            bbibatal.Id = 2;
            bbibatal.ImageOptions.Image = (Image)resources.GetObject("bbibatal.ImageOptions.Image");
            bbibatal.ImageOptions.LargeImage = (Image)resources.GetObject("bbibatal.ImageOptions.LargeImage");
            bbibatal.Name = "bbibatal";
            bbibatal.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // barLargeButtonItem2
            // 
            barLargeButtonItem2.Caption = "bayar";
            barLargeButtonItem2.Id = 6;
            barLargeButtonItem2.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem2.ImageOptions.Image");
            barLargeButtonItem2.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem2.ImageOptions.LargeImage");
            barLargeButtonItem2.Name = "barLargeButtonItem2";
            barLargeButtonItem2.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // groupControl1
            // 
            groupControl1.Controls.Add(txtketerangan);
            groupControl1.Controls.Add(lblterbilang);
            groupControl1.Controls.Add(labelControl7);
            groupControl1.Controls.Add(labelControl9);
            groupControl1.Controls.Add(labelControl8);
            groupControl1.Controls.Add(labelControl5);
            groupControl1.Controls.Add(labelControl4);
            groupControl1.Controls.Add(labelControl3);
            groupControl1.Controls.Add(labelControl2);
            groupControl1.Controls.Add(labelControl1);
            groupControl1.Controls.Add(leangsuran);
            groupControl1.Controls.Add(leanggota);
            groupControl1.Controls.Add(detanggal);
            groupControl1.Controls.Add(txtjam);
            groupControl1.Controls.Add(txtkasir);
            groupControl1.Controls.Add(txtbunga);
            groupControl1.Controls.Add(txtpinjaman);
            groupControl1.Controls.Add(txtnotransaksi);
            groupControl1.Location = new Point(73, 10);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new Size(372, 275);
            groupControl1.TabIndex = 9;
            groupControl1.Text = "Informasi Transaksi";
            // 
            // txtketerangan
            // 
            txtketerangan.Location = new Point(90, 197);
            txtketerangan.MenuManager = barManager1;
            txtketerangan.Name = "txtketerangan";
            txtketerangan.Size = new Size(276, 40);
            txtketerangan.TabIndex = 12;
            txtketerangan.EditValueChanged += txtketerangan_EditValueChanged;
            // 
            // lblterbilang
            // 
            lblterbilang.Location = new Point(92, 243);
            lblterbilang.Name = "lblterbilang";
            lblterbilang.Size = new Size(0, 13);
            lblterbilang.TabIndex = 9;
            // 
            // labelControl7
            // 
            labelControl7.Location = new Point(7, 243);
            labelControl7.Name = "labelControl7";
            labelControl7.Size = new Size(44, 13);
            labelControl7.TabIndex = 9;
            labelControl7.Text = "Terbilang";
            // 
            // labelControl9
            // 
            labelControl9.Location = new Point(7, 207);
            labelControl9.Name = "labelControl9";
            labelControl9.Size = new Size(56, 13);
            labelControl9.TabIndex = 9;
            labelControl9.Text = "Keterangan";
            // 
            // labelControl8
            // 
            labelControl8.Location = new Point(9, 178);
            labelControl8.Name = "labelControl8";
            labelControl8.Size = new Size(30, 13);
            labelControl8.TabIndex = 9;
            labelControl8.Text = "Bunga";
            // 
            // labelControl5
            // 
            labelControl5.Location = new Point(7, 143);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(33, 13);
            labelControl5.TabIndex = 9;
            labelControl5.Text = "Jumlah";
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(9, 112);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(66, 13);
            labelControl4.TabIndex = 9;
            labelControl4.Text = "Jlh. Angsuran";
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(7, 85);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(41, 13);
            labelControl3.TabIndex = 9;
            labelControl3.Text = "Anggota";
            // 
            // labelControl2
            // 
            labelControl2.Location = new Point(7, 59);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(38, 13);
            labelControl2.TabIndex = 10;
            labelControl2.Text = "Tanggal";
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(7, 33);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(61, 13);
            labelControl1.TabIndex = 11;
            labelControl1.Text = "No Transaksi";
            // 
            // leangsuran
            // 
            leangsuran.Location = new Point(92, 109);
            leangsuran.Name = "leangsuran";
            leangsuran.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            leangsuran.Size = new Size(164, 20);
            leangsuran.TabIndex = 8;
            leangsuran.EditValueChanged += leangsuran_EditValueChanged;
            // 
            // leanggota
            // 
            leanggota.Location = new Point(92, 82);
            leanggota.Name = "leanggota";
            leanggota.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            leanggota.Size = new Size(164, 20);
            leanggota.TabIndex = 8;
            leanggota.Popup += leanggota_Popup;
            leanggota.EditValueChanged += leanggota_EditValueChanged;
            // 
            // detanggal
            // 
            detanggal.EditValue = null;
            detanggal.Location = new Point(92, 56);
            detanggal.Name = "detanggal";
            detanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            detanggal.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            detanggal.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            detanggal.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
            detanggal.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            detanggal.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            detanggal.Size = new Size(164, 20);
            detanggal.TabIndex = 7;
            detanggal.EditValueChanged += detanggal_EditValueChanged;
            // 
            // txtjam
            // 
            txtjam.Location = new Point(266, 56);
            txtjam.Name = "txtjam";
            txtjam.Properties.ReadOnly = true;
            txtjam.Size = new Size(100, 20);
            txtjam.TabIndex = 4;
            // 
            // txtkasir
            // 
            txtkasir.Location = new Point(266, 30);
            txtkasir.Name = "txtkasir";
            txtkasir.Properties.ReadOnly = true;
            txtkasir.Size = new Size(100, 20);
            txtkasir.TabIndex = 5;
            // 
            // txtbunga
            // 
            txtbunga.EditValue = "0";
            txtbunga.Location = new Point(92, 171);
            txtbunga.Name = "txtbunga";
            txtbunga.Properties.Appearance.Options.UseTextOptions = true;
            txtbunga.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            txtbunga.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            txtbunga.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtbunga.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            txtbunga.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtbunga.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtbunga.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            txtbunga.Properties.ReadOnly = true;
            txtbunga.Size = new Size(164, 20);
            txtbunga.TabIndex = 6;
            txtbunga.KeyUp += txtpinjaman_KeyUp;
            // 
            // txtpinjaman
            // 
            txtpinjaman.EditValue = "0";
            txtpinjaman.Location = new Point(92, 140);
            txtpinjaman.Name = "txtpinjaman";
            txtpinjaman.Properties.Appearance.Options.UseTextOptions = true;
            txtpinjaman.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            txtpinjaman.Properties.DisplayFormat.FormatString = "###,###,###,##0.00;";
            txtpinjaman.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtpinjaman.Properties.EditFormat.FormatString = "###,###,###,##0.00;";
            txtpinjaman.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txtpinjaman.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtpinjaman.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            txtpinjaman.Size = new Size(164, 20);
            txtpinjaman.TabIndex = 6;
            txtpinjaman.EditValueChanged += txtpinjaman_EditValueChanged;
            txtpinjaman.KeyUp += txtpinjaman_KeyUp;
            // 
            // txtnotransaksi
            // 
            txtnotransaksi.Location = new Point(92, 30);
            txtnotransaksi.Name = "txtnotransaksi";
            txtnotransaksi.Properties.ReadOnly = true;
            txtnotransaksi.Size = new Size(164, 20);
            txtnotransaksi.TabIndex = 6;
            // 
            // gridControl1
            // 
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gridControl1.Location = new Point(73, 291);
            gridControl1.MainView = gridView1;
            gridControl1.MenuManager = barManager1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(785, 294);
            gridControl1.TabIndex = 19;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // pictureBoxVideo
            // 
            pictureBoxVideo.Location = new Point(467, 60);
            pictureBoxVideo.Name = "pictureBoxVideo";
            pictureBoxVideo.Size = new Size(321, 225);
            pictureBoxVideo.TabIndex = 24;
            pictureBoxVideo.TabStop = false;
            // 
            // ucPinjaman
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pictureBoxVideo);
            Controls.Add(gridControl1);
            Controls.Add(groupControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucPinjaman";
            Size = new Size(876, 658);
            Load += ucPinjaman_Load;
            PreviewKeyDown += ucPenjualan_PreviewKeyDown;
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).EndInit();
            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)txtketerangan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)leanggota.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtjam.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtkasir.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtbunga.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpinjaman.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVideo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.BarButtonItem bbibayar;
        private DevExpress.XtraBars.BarButtonItem bbipending;
        private DevExpress.XtraBars.BarButtonItem bbibatal;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraBars.Bar bar1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem2;
        private DevExpress.XtraBars.BarLargeButtonItem blbisimpan;
        private DevExpress.XtraBars.BarLargeButtonItem blbibatal;
        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LookUpEdit leanggota;
        private DevExpress.XtraEditors.DateEdit detanggal;
        private DevExpress.XtraEditors.TextEdit txtjam;
        private DevExpress.XtraEditors.TextEdit txtkasir;
        private DevExpress.XtraEditors.TextEdit txtnotransaksi;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LookUpEdit leangsuran;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.TextEdit txtpinjaman;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraBars.BarLargeButtonItem blbisimulasi;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.LabelControl lblterbilang;
        private DevExpress.XtraEditors.LabelControl labelControl7;
        private DevExpress.XtraEditors.LabelControl labelControl9;
        private DevExpress.XtraEditors.LabelControl labelControl8;
        private DevExpress.XtraEditors.TextEdit txtbunga;
        private PictureBox pictureBoxVideo;
        private DevExpress.XtraEditors.MemoEdit txtketerangan;
    }
}
