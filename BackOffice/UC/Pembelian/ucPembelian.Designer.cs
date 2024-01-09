namespace BackOffice.UC
{
    partial class ucPembelian
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPembelian));
            barcodeTextBox = new DevExpress.XtraEditors.TextEdit();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            No = new DevExpress.XtraGrid.Columns.GridColumn();
            ProductId = new DevExpress.XtraGrid.Columns.GridColumn();
            Barcode = new DevExpress.XtraGrid.Columns.GridColumn();
            Kode_Item = new DevExpress.XtraGrid.Columns.GridColumn();
            ProductName = new DevExpress.XtraGrid.Columns.GridColumn();
            Satuan = new DevExpress.XtraGrid.Columns.GridColumn();
            Qty = new DevExpress.XtraGrid.Columns.GridColumn();
            Hpp = new DevExpress.XtraGrid.Columns.GridColumn();
            Price = new DevExpress.XtraGrid.Columns.GridColumn();
            Bruto = new DevExpress.XtraGrid.Columns.GridColumn();
            Potongan = new DevExpress.XtraGrid.Columns.GridColumn();
            Total = new DevExpress.XtraGrid.Columns.GridColumn();
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
            lookUpEditSupplier = new DevExpress.XtraEditors.LookUpEdit();
            labelControl5 = new DevExpress.XtraEditors.LabelControl();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            txtpotongan = new DevExpress.XtraEditors.TextEdit();
            texthpplama = new DevExpress.XtraEditors.TextEdit();
            txttotal = new DevExpress.XtraEditors.TextEdit();
            texthargajual = new DevExpress.XtraEditors.TextEdit();
            txthargabeli = new DevExpress.XtraEditors.TextEdit();
            txtqty = new DevExpress.XtraEditors.TextEdit();
            txtsatuan = new DevExpress.XtraEditors.TextEdit();
            txtnamabarang = new DevExpress.XtraEditors.TextEdit();
            txtItemBarang = new DevExpress.XtraEditors.TextEdit();
            detanggal = new DevExpress.XtraEditors.DateEdit();
            txttermin = new DevExpress.XtraEditors.TextEdit();
            txtuser = new DevExpress.XtraEditors.TextEdit();
            txtnotransaksi = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).BeginInit();
            groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)lookUpEditSupplier.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpotongan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)texthpplama.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txttotal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)texthargajual.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txthargabeli.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtqty.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtsatuan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnamabarang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtItemBarang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txttermin.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtuser.Properties).BeginInit();
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
            barcodeTextBox.TabIndex = 5;
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
            gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { No, ProductId, Barcode, Kode_Item, ProductName, Satuan, Qty, Hpp, Price, Bruto, Potongan, Total });
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowFooter = true;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.CellValueChanged += gridView1_CellValueChanged;
            gridView1.KeyDown += gridView1_KeyDown;
            gridView1.ValidatingEditor += gridView1_ValidatingEditor;
            gridView1.RowCountChanged += gridView1_RowCountChanged;
            // 
            // No
            // 
            No.Caption = "No";
            No.FieldName = "No";
            No.MaxWidth = 40;
            No.MinWidth = 40;
            No.Name = "No";
            No.OptionsColumn.AllowEdit = false;
            No.Visible = true;
            No.VisibleIndex = 0;
            No.Width = 40;
            // 
            // ProductId
            // 
            ProductId.Caption = "ProductId";
            ProductId.FieldName = "ProductId";
            ProductId.Name = "ProductId";
            ProductId.OptionsColumn.AllowEdit = false;
            // 
            // Barcode
            // 
            Barcode.Caption = "Barcode";
            Barcode.FieldName = "Barcode";
            Barcode.Name = "Barcode";
            Barcode.OptionsColumn.AllowEdit = false;
            // 
            // Kode_Item
            // 
            Kode_Item.Caption = "Kode Item";
            Kode_Item.FieldName = "Kode_Item";
            Kode_Item.Name = "Kode_Item";
            Kode_Item.OptionsColumn.AllowEdit = false;
            // 
            // ProductName
            // 
            ProductName.Caption = "Nama Barang";
            ProductName.FieldName = "ProductName";
            ProductName.MinWidth = 150;
            ProductName.Name = "ProductName";
            ProductName.OptionsColumn.AllowEdit = false;
            ProductName.Visible = true;
            ProductName.VisibleIndex = 1;
            ProductName.Width = 150;
            // 
            // Satuan
            // 
            Satuan.Caption = "Satuan";
            Satuan.FieldName = "Satuan";
            Satuan.MaxWidth = 50;
            Satuan.MinWidth = 50;
            Satuan.Name = "Satuan";
            Satuan.OptionsColumn.AllowEdit = false;
            Satuan.Visible = true;
            Satuan.VisibleIndex = 2;
            Satuan.Width = 50;
            // 
            // Qty
            // 
            Qty.Caption = "Qty";
            Qty.FieldName = "Qty";
            Qty.MaxWidth = 50;
            Qty.MinWidth = 70;
            Qty.Name = "Qty";
            Qty.Visible = true;
            Qty.VisibleIndex = 3;
            Qty.Width = 70;
            // 
            // Hpp
            // 
            Hpp.Caption = "Harga";
            Hpp.DisplayFormat.FormatString = "n0";
            Hpp.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Hpp.FieldName = "Hpp";
            Hpp.MaxWidth = 80;
            Hpp.MinWidth = 80;
            Hpp.Name = "Hpp";
            Hpp.Visible = true;
            Hpp.VisibleIndex = 4;
            Hpp.Width = 80;
            // 
            // Price
            // 
            Price.DisplayFormat.FormatString = "n0";
            Price.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Price.FieldName = "Price";
            Price.MaxWidth = 80;
            Price.MinWidth = 80;
            Price.Name = "Price";
            Price.OptionsColumn.AllowEdit = false;
            Price.Width = 80;
            // 
            // Bruto
            // 
            Bruto.Caption = "Bruto";
            Bruto.DisplayFormat.FormatString = "n0";
            Bruto.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Bruto.FieldName = "Bruto";
            Bruto.MaxWidth = 80;
            Bruto.MinWidth = 80;
            Bruto.Name = "Bruto";
            Bruto.OptionsColumn.AllowEdit = false;
            Bruto.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Bruto", "{0:n0}") });
            Bruto.Visible = true;
            Bruto.VisibleIndex = 5;
            Bruto.Width = 80;
            // 
            // Potongan
            // 
            Potongan.Caption = "Potongan";
            Potongan.DisplayFormat.FormatString = "n0";
            Potongan.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Potongan.FieldName = "Potongan";
            Potongan.MaxWidth = 80;
            Potongan.MinWidth = 80;
            Potongan.Name = "Potongan";
            Potongan.OptionsColumn.AllowEdit = false;
            Potongan.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Potongan", "{0:n0}") });
            Potongan.Visible = true;
            Potongan.VisibleIndex = 6;
            Potongan.Width = 80;
            // 
            // Total
            // 
            Total.Caption = "Total";
            Total.DisplayFormat.FormatString = "n0";
            Total.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Total.FieldName = "Total";
            Total.MaxWidth = 80;
            Total.MinWidth = 80;
            Total.Name = "Total";
            Total.OptionsColumn.AllowEdit = false;
            Total.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Total", "{0:n0}") });
            Total.Visible = true;
            Total.VisibleIndex = 7;
            Total.Width = 80;
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
            barDockControlTop.Size = new Size(1097, 58);
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
            barDockControlLeft.Location = new Point(0, 58);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 586);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(1097, 58);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 586);
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
            groupControl1.Controls.Add(lookUpEditSupplier);
            groupControl1.Controls.Add(labelControl5);
            groupControl1.Controls.Add(labelControl3);
            groupControl1.Controls.Add(labelControl4);
            groupControl1.Controls.Add(labelControl2);
            groupControl1.Controls.Add(labelControl1);
            groupControl1.Controls.Add(txtpotongan);
            groupControl1.Controls.Add(texthpplama);
            groupControl1.Controls.Add(txttotal);
            groupControl1.Controls.Add(texthargajual);
            groupControl1.Controls.Add(txthargabeli);
            groupControl1.Controls.Add(txtqty);
            groupControl1.Controls.Add(txtsatuan);
            groupControl1.Controls.Add(txtnamabarang);
            groupControl1.Controls.Add(txtItemBarang);
            groupControl1.Controls.Add(barcodeTextBox);
            groupControl1.Controls.Add(detanggal);
            groupControl1.Controls.Add(txttermin);
            groupControl1.Controls.Add(txtuser);
            groupControl1.Controls.Add(txtnotransaksi);
            groupControl1.Location = new Point(20, 76);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new Size(1063, 186);
            groupControl1.TabIndex = 0;
            groupControl1.Text = "Informasi Transaksi";
            // 
            // lookUpEditSupplier
            // 
            lookUpEditSupplier.Location = new Point(92, 82);
            lookUpEditSupplier.MenuManager = barManager1;
            lookUpEditSupplier.Name = "lookUpEditSupplier";
            lookUpEditSupplier.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lookUpEditSupplier.Size = new Size(274, 20);
            lookUpEditSupplier.TabIndex = 3;
            lookUpEditSupplier.KeyDown += lookUpEditSupplier_KeyDown;
            // 
            // labelControl5
            // 
            labelControl5.Location = new Point(7, 109);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(32, 13);
            labelControl5.TabIndex = 2;
            labelControl5.Text = "Termin";
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(7, 85);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(38, 13);
            labelControl3.TabIndex = 2;
            labelControl3.Text = "Supplier";
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(153, 109);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(19, 13);
            labelControl4.TabIndex = 1;
            labelControl4.Text = "Hari";
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
            // txtpotongan
            // 
            txtpotongan.Location = new Point(762, 142);
            txtpotongan.Name = "txtpotongan";
            txtpotongan.Properties.AdvancedModeOptions.Label = "Potongan";
            txtpotongan.Properties.Appearance.Options.UseFont = true;
            txtpotongan.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtpotongan.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txtpotongan.Properties.MaskSettings.Set("mask", "n");
            txtpotongan.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtpotongan.Properties.UseMaskAsDisplayFormat = true;
            txtpotongan.Size = new Size(73, 34);
            txtpotongan.TabIndex = 11;
            txtpotongan.KeyDown += txtpotongan_KeyDown;
            // 
            // texthpplama
            // 
            texthpplama.Location = new Point(654, 102);
            texthpplama.Name = "texthpplama";
            texthpplama.Properties.AdvancedModeOptions.Label = "HPP Lama";
            texthpplama.Properties.Appearance.Options.UseFont = true;
            texthpplama.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            texthpplama.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            texthpplama.Properties.MaskSettings.Set("mask", "n");
            texthpplama.Properties.ReadOnly = true;
            texthpplama.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            texthpplama.Properties.UseMaskAsDisplayFormat = true;
            texthpplama.Size = new Size(102, 34);
            texthpplama.TabIndex = 14;
            texthpplama.KeyDown += txtharga_KeyDown;
            // 
            // txttotal
            // 
            txttotal.Location = new Point(949, 142);
            txttotal.Name = "txttotal";
            txttotal.Properties.AdvancedModeOptions.Label = "Total";
            txttotal.Properties.Appearance.Options.UseFont = true;
            txttotal.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txttotal.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txttotal.Properties.MaskSettings.Set("mask", "n");
            txttotal.Properties.ReadOnly = true;
            txttotal.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txttotal.Properties.UseMaskAsDisplayFormat = true;
            txttotal.Size = new Size(102, 34);
            txttotal.TabIndex = 13;
            txttotal.KeyDown += texthargajual_KeyDown;
            // 
            // texthargajual
            // 
            texthargajual.Location = new Point(841, 142);
            texthargajual.Name = "texthargajual";
            texthargajual.Properties.AdvancedModeOptions.Label = "Harga Jual";
            texthargajual.Properties.Appearance.Options.UseFont = true;
            texthargajual.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            texthargajual.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            texthargajual.Properties.MaskSettings.Set("mask", "n");
            texthargajual.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            texthargajual.Properties.UseMaskAsDisplayFormat = true;
            texthargajual.Size = new Size(102, 34);
            texthargajual.TabIndex = 12;
            texthargajual.KeyDown += texthargajual_KeyDown;
            // 
            // txthargabeli
            // 
            txthargabeli.Location = new Point(654, 142);
            txthargabeli.Name = "txthargabeli";
            txthargabeli.Properties.AdvancedModeOptions.Label = "Harga Beli";
            txthargabeli.Properties.Appearance.Options.UseFont = true;
            txthargabeli.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txthargabeli.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txthargabeli.Properties.MaskSettings.Set("mask", "n");
            txthargabeli.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txthargabeli.Properties.UseMaskAsDisplayFormat = true;
            txthargabeli.Size = new Size(102, 34);
            txthargabeli.TabIndex = 10;
            txthargabeli.KeyDown += txtharga_KeyDown;
            // 
            // txtqty
            // 
            txtqty.Location = new Point(586, 142);
            txtqty.Name = "txtqty";
            txtqty.Properties.AdvancedModeOptions.Label = "Qty";
            txtqty.Properties.Appearance.Options.UseFont = true;
            txtqty.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtqty.Size = new Size(62, 34);
            txtqty.TabIndex = 9;
            txtqty.KeyDown += txtqty_KeyDown;
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
            txtsatuan.TabIndex = 8;
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
            txtnamabarang.TabIndex = 7;
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
            txtItemBarang.TabIndex = 6;
            txtItemBarang.KeyDown += barcodeTextBox_KeyDown;
            // 
            // detanggal
            // 
            detanggal.EditValue = null;
            detanggal.Location = new Point(92, 56);
            detanggal.Name = "detanggal";
            detanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            detanggal.Size = new Size(164, 20);
            detanggal.TabIndex = 2;
            detanggal.KeyDown += detanggal_KeyDown;
            // 
            // txttermin
            // 
            txttermin.Location = new Point(92, 106);
            txttermin.Name = "txttermin";
            txttermin.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txttermin.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txttermin.Properties.MaskSettings.Set("mask", "d");
            txttermin.Size = new Size(55, 20);
            txttermin.TabIndex = 4;
            txttermin.ToolTip = "Termin Pembayaran";
            // 
            // txtuser
            // 
            txtuser.Location = new Point(266, 30);
            txtuser.Name = "txtuser";
            txtuser.Properties.ReadOnly = true;
            txtuser.Size = new Size(100, 20);
            txtuser.TabIndex = 1;
            // 
            // txtnotransaksi
            // 
            txtnotransaksi.Location = new Point(92, 30);
            txtnotransaksi.Name = "txtnotransaksi";
            txtnotransaksi.Properties.ReadOnly = true;
            txtnotransaksi.Size = new Size(164, 20);
            txtnotransaksi.TabIndex = 0;
            // 
            // ucPembelian
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupControl1);
            Controls.Add(gridControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucPembelian";
            Size = new Size(1097, 644);
            Load += ucPembelian_Load;
            KeyDown += ucPenjualan_KeyDown;
            PreviewKeyDown += ucPenjualan_PreviewKeyDown;
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).EndInit();
            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)lookUpEditSupplier.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpotongan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)texthpplama.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txttotal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)texthargajual.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txthargabeli.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtqty.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtsatuan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnamabarang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtItemBarang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txttermin.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtuser.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraEditors.TextEdit barcodeTextBox;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn ProductId;
        private DevExpress.XtraGrid.Columns.GridColumn ProductName;
        private DevExpress.XtraGrid.Columns.GridColumn Barcode;
        private DevExpress.XtraGrid.Columns.GridColumn Satuan;
        private DevExpress.XtraGrid.Columns.GridColumn Qty;
        private DevExpress.XtraGrid.Columns.GridColumn Price;
        private DevExpress.XtraGrid.Columns.GridColumn Potongan;
        private DevExpress.XtraGrid.Columns.GridColumn Total;
        private DevExpress.XtraGrid.Columns.GridColumn No;
        private DevExpress.XtraGrid.Columns.GridColumn Kode_Item;
        private DevExpress.XtraGrid.Columns.GridColumn Bruto;
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
        private DevExpress.XtraEditors.TextEdit txttermin;
        private DevExpress.XtraEditors.TextEdit txtuser;
        private DevExpress.XtraEditors.TextEdit txtnotransaksi;
        private DevExpress.XtraGrid.Columns.GridColumn Hpp;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem3;
        private DevExpress.XtraEditors.TextEdit txtpotongan;
        private DevExpress.XtraEditors.TextEdit txthargabeli;
        private DevExpress.XtraEditors.TextEdit txtqty;
        private DevExpress.XtraEditors.TextEdit txtsatuan;
        private DevExpress.XtraEditors.TextEdit txtnamabarang;
        private DevExpress.XtraEditors.TextEdit txtItemBarang;
        private DevExpress.XtraEditors.LookUpEdit lookUpEditSupplier;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.TextEdit texthpplama;
        private DevExpress.XtraEditors.TextEdit texthargajual;
        private DevExpress.XtraEditors.TextEdit txttotal;
    }
}
