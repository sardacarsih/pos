namespace BackOffice.UC
{
    partial class ucBarangRusak
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucBarangRusak));
            barcodeTextBox = new DevExpress.XtraEditors.TextEdit();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar1 = new DevExpress.XtraBars.Bar();
            blbisimpan = new DevExpress.XtraBars.BarLargeButtonItem();
            barLargeButtonItem3 = new DevExpress.XtraBars.BarLargeButtonItem();
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
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            txtketerangan = new DevExpress.XtraEditors.TextEdit();
            txtqtyfisik = new DevExpress.XtraEditors.TextEdit();
            txthpp = new DevExpress.XtraEditors.TextEdit();
            txtsatuan = new DevExpress.XtraEditors.TextEdit();
            txtnamabarang = new DevExpress.XtraEditors.TextEdit();
            txtItemBarang = new DevExpress.XtraEditors.TextEdit();
            detanggal = new DevExpress.XtraEditors.DateEdit();
            txtnotransaksi = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).BeginInit();
            groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtketerangan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtqtyfisik.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txthpp.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtsatuan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnamabarang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtItemBarang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).BeginInit();
            SuspendLayout();
            // 
            // barcodeTextBox
            // 
            barcodeTextBox.Location = new Point(7, 142);
            barcodeTextBox.Name = "barcodeTextBox";
            barcodeTextBox.Properties.AdvancedModeOptions.Label = "Barcode";
            barcodeTextBox.Properties.Appearance.Options.UseFont = true;
            barcodeTextBox.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            barcodeTextBox.Size = new Size(115, 34);
            barcodeTextBox.TabIndex = 1;
            barcodeTextBox.KeyDown += barcodeTextBox_KeyDown;
            // 
            // gridControl1
            // 
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridControl1.Location = new Point(18, 297);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(1065, 304);
            gridControl1.TabIndex = 1;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowFooter = true;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.CellValueChanged += gridView1_CellValueChanged;
            gridView1.KeyDown += gridView1_KeyDown;
            gridView1.ValidatingEditor += gridView1_ValidatingEditor;
            gridView1.RowCountChanged += gridView1_RowCountChanged;
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar1 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bbibayar, bbipending, bbibatal, barLargeButtonItem2, blbisimpan, blbibatal, barLargeButtonItem3 });
            barManager1.MainMenu = bar1;
            barManager1.MaxItemId = 15;
            // 
            // bar1
            // 
            bar1.BarName = "Custom 3";
            bar1.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Top;
            bar1.DockCol = 0;
            bar1.DockRow = 0;
            bar1.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar1.FloatLocation = new Point(1291, 355);
            bar1.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(blbisimpan), new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem3), new DevExpress.XtraBars.LinkPersistInfo(blbibatal) });
            bar1.OptionsBar.MultiLine = true;
            bar1.OptionsBar.UseWholeRow = true;
            bar1.Text = "Custom 3";
            // 
            // blbisimpan
            // 
            blbisimpan.Caption = "&Simpan";
            blbisimpan.Hint = "Klik this or Press END Button";
            blbisimpan.Id = 7;
            blbisimpan.ImageOptions.Image = (Image)resources.GetObject("blbisimpan.ImageOptions.Image");
            blbisimpan.ImageOptions.LargeImage = (Image)resources.GetObject("blbisimpan.ImageOptions.LargeImage");
            blbisimpan.Name = "blbisimpan";
            blbisimpan.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            blbisimpan.ItemClick += blbisimpan_ItemClick;
            // 
            // barLargeButtonItem3
            // 
            barLargeButtonItem3.Id = 14;
            barLargeButtonItem3.Name = "barLargeButtonItem3";
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
            barDockControlTop.Size = new Size(1097, 56);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 644);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(1097, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 56);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 588);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(1097, 56);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 588);
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
            groupControl1.Controls.Add(labelControl2);
            groupControl1.Controls.Add(labelControl1);
            groupControl1.Controls.Add(txtketerangan);
            groupControl1.Controls.Add(txtqtyfisik);
            groupControl1.Controls.Add(txthpp);
            groupControl1.Controls.Add(txtsatuan);
            groupControl1.Controls.Add(txtnamabarang);
            groupControl1.Controls.Add(txtItemBarang);
            groupControl1.Controls.Add(barcodeTextBox);
            groupControl1.Controls.Add(detanggal);
            groupControl1.Controls.Add(txtnotransaksi);
            groupControl1.Location = new Point(20, 76);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new Size(1063, 186);
            groupControl1.TabIndex = 0;
            groupControl1.Text = "Informasi Transaksi";
            // 
            // labelControl2
            // 
            labelControl2.Location = new Point(7, 59);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(38, 13);
            labelControl2.TabIndex = 1;
            labelControl2.Text = "Tanggal";
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(7, 33);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(61, 13);
            labelControl1.TabIndex = 0;
            labelControl1.Text = "No Transaksi";
            // 
            // txtketerangan
            // 
            txtketerangan.Location = new Point(786, 142);
            txtketerangan.Name = "txtketerangan";
            txtketerangan.Properties.AdvancedModeOptions.Label = "Keterangan";
            txtketerangan.Properties.Appearance.Options.UseFont = true;
            txtketerangan.Properties.NullValuePrompt = "Rusak";
            txtketerangan.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtketerangan.Properties.UseMaskAsDisplayFormat = true;
            txtketerangan.Size = new Size(272, 34);
            txtketerangan.TabIndex = 3;
            txtketerangan.KeyDown += txtketerangan_KeyDown;
            // 
            // txtqtyfisik
            // 
            txtqtyfisik.Location = new Point(586, 142);
            txtqtyfisik.Name = "txtqtyfisik";
            txtqtyfisik.Properties.AdvancedModeOptions.Label = "Qty";
            txtqtyfisik.Properties.Appearance.Options.UseFont = true;
            txtqtyfisik.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtqtyfisik.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txtqtyfisik.Properties.MaskSettings.Set("mask", "n");
            txtqtyfisik.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtqtyfisik.Properties.UseMaskAsDisplayFormat = true;
            txtqtyfisik.Size = new Size(90, 34);
            txtqtyfisik.TabIndex = 2;
            txtqtyfisik.KeyDown += txtqtyfisik_KeyDown;
            // 
            // txthpp
            // 
            txthpp.Location = new Point(682, 142);
            txthpp.Name = "txthpp";
            txthpp.Properties.AdvancedModeOptions.Label = "Hpp";
            txthpp.Properties.Appearance.Options.UseFont = true;
            txthpp.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txthpp.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txthpp.Properties.MaskSettings.Set("mask", "n");
            txthpp.Properties.ReadOnly = true;
            txthpp.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txthpp.Properties.UseMaskAsDisplayFormat = true;
            txthpp.Size = new Size(98, 34);
            txthpp.TabIndex = 8;
            txthpp.KeyDown += barcodeTextBox_KeyDown;
            // 
            // txtsatuan
            // 
            txtsatuan.Location = new Point(528, 142);
            txtsatuan.Name = "txtsatuan";
            txtsatuan.Properties.AdvancedModeOptions.Label = "Satuan";
            txtsatuan.Properties.Appearance.Options.UseFont = true;
            txtsatuan.Properties.ReadOnly = true;
            txtsatuan.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtsatuan.Size = new Size(52, 34);
            txtsatuan.TabIndex = 6;
            txtsatuan.KeyDown += barcodeTextBox_KeyDown;
            // 
            // txtnamabarang
            // 
            txtnamabarang.Location = new Point(231, 142);
            txtnamabarang.Name = "txtnamabarang";
            txtnamabarang.Properties.AdvancedModeOptions.Label = "Nama Barang";
            txtnamabarang.Properties.Appearance.Options.UseFont = true;
            txtnamabarang.Properties.ReadOnly = true;
            txtnamabarang.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtnamabarang.Size = new Size(291, 34);
            txtnamabarang.TabIndex = 5;
            txtnamabarang.KeyDown += barcodeTextBox_KeyDown;
            // 
            // txtItemBarang
            // 
            txtItemBarang.Location = new Point(128, 142);
            txtItemBarang.Name = "txtItemBarang";
            txtItemBarang.Properties.AdvancedModeOptions.Label = "Item Barang";
            txtItemBarang.Properties.Appearance.Options.UseFont = true;
            txtItemBarang.Properties.ReadOnly = true;
            txtItemBarang.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtItemBarang.Size = new Size(97, 34);
            txtItemBarang.TabIndex = 4;
            txtItemBarang.KeyDown += barcodeTextBox_KeyDown;
            // 
            // detanggal
            // 
            detanggal.EditValue = null;
            detanggal.Location = new Point(92, 56);
            detanggal.Name = "detanggal";
            detanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            detanggal.Size = new Size(164, 20);
            detanggal.TabIndex = 0;
            detanggal.KeyDown += detanggal_KeyDown;
            // 
            // txtnotransaksi
            // 
            txtnotransaksi.Location = new Point(92, 30);
            txtnotransaksi.Name = "txtnotransaksi";
            txtnotransaksi.Properties.ReadOnly = true;
            txtnotransaksi.Size = new Size(164, 20);
            txtnotransaksi.TabIndex = 7;
            // 
            // ucBarangRusak
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupControl1);
            Controls.Add(gridControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucBarangRusak";
            Size = new Size(1097, 644);
            Load += ucStockOpname_Load;
            KeyDown += ucPenjualan_KeyDown;
            PreviewKeyDown += ucPenjualan_PreviewKeyDown;
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).EndInit();
            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)txtketerangan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtqtyfisik.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txthpp.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtsatuan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnamabarang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtItemBarang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraEditors.TextEdit barcodeTextBox;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
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
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.DateEdit detanggal;
        private DevExpress.XtraEditors.TextEdit txtnotransaksi;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem3;
        private DevExpress.XtraEditors.TextEdit txtqtyfisik;
        private DevExpress.XtraEditors.TextEdit txtsatuan;
        private DevExpress.XtraEditors.TextEdit txtnamabarang;
        private DevExpress.XtraEditors.TextEdit txtItemBarang;
        private DevExpress.XtraEditors.TextEdit txtketerangan;
        private DevExpress.XtraEditors.TextEdit txthpp;
    }
}
