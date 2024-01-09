namespace BackOffice.UC
{
    partial class ucPenjualan
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPenjualan));
            this.txttotalpenjualan = new DevExpress.XtraEditors.TextEdit();
            this.barcodeTextBox = new DevExpress.XtraEditors.TextEdit();
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.No = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ProductId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Barcode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Kode_Item = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ProductName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Satuan = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Qty = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Hpp = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Price = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Bruto = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Potongan = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Total = new DevExpress.XtraGrid.Columns.GridColumn();
            this.barManager1 = new DevExpress.XtraBars.BarManager(this.components);
            this.bar1 = new DevExpress.XtraBars.Bar();
            this.blbibayar = new DevExpress.XtraBars.BarLargeButtonItem();
            this.blbipending = new DevExpress.XtraBars.BarLargeButtonItem();
            this.barLargeButtonItem1 = new DevExpress.XtraBars.BarLargeButtonItem();
            this.barLargeButtonItem3 = new DevExpress.XtraBars.BarLargeButtonItem();
            this.blbibatal = new DevExpress.XtraBars.BarLargeButtonItem();
            this.barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            this.bbibayar = new DevExpress.XtraBars.BarButtonItem();
            this.bbipending = new DevExpress.XtraBars.BarButtonItem();
            this.bbibatal = new DevExpress.XtraBars.BarButtonItem();
            this.barLargeButtonItem2 = new DevExpress.XtraBars.BarLargeButtonItem();
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.detanggal = new DevExpress.XtraEditors.DateEdit();
            this.txtjam = new DevExpress.XtraEditors.TextEdit();
            this.txtkasir = new DevExpress.XtraEditors.TextEdit();
            this.txtnotransaksi = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.txttotalpenjualan.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.barcodeTextBox.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.barManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.detanggal.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.detanggal.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtjam.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtkasir.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtnotransaksi.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // txttotalpenjualan
            // 
            this.txttotalpenjualan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txttotalpenjualan.Location = new System.Drawing.Point(394, 76);
            this.txttotalpenjualan.Name = "txttotalpenjualan";
            this.txttotalpenjualan.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.txttotalpenjualan.Properties.Appearance.ForeColor = System.Drawing.Color.Blue;
            this.txttotalpenjualan.Properties.Appearance.Options.UseFont = true;
            this.txttotalpenjualan.Properties.Appearance.Options.UseForeColor = true;
            this.txttotalpenjualan.Properties.AutoHeight = false;
            this.txttotalpenjualan.Properties.ReadOnly = true;
            this.txttotalpenjualan.Size = new System.Drawing.Size(409, 123);
            this.txttotalpenjualan.TabIndex = 2;
            // 
            // barcodeTextBox
            // 
            this.barcodeTextBox.Location = new System.Drawing.Point(7, 82);
            this.barcodeTextBox.Name = "barcodeTextBox";
            this.barcodeTextBox.Properties.AdvancedModeOptions.Label = "Barcode";
            this.barcodeTextBox.Properties.Appearance.Options.UseFont = true;
            this.barcodeTextBox.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            this.barcodeTextBox.Size = new System.Drawing.Size(359, 34);
            this.barcodeTextBox.TabIndex = 4;
            this.barcodeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.barcodeTextBox_KeyDown);
            // 
            // gridControl1
            // 
            this.gridControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControl1.Location = new System.Drawing.Point(18, 205);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(785, 396);
            this.gridControl1.TabIndex = 1;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.No,
            this.ProductId,
            this.Barcode,
            this.Kode_Item,
            this.ProductName,
            this.Satuan,
            this.Qty,
            this.Hpp,
            this.Price,
            this.Bruto,
            this.Potongan,
            this.Total});
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowFooter = true;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            this.gridView1.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.gridView1_CellValueChanged);
            this.gridView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gridView1_KeyDown);
            this.gridView1.ValidatingEditor += new DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler(this.gridView1_ValidatingEditor);
            this.gridView1.RowCountChanged += new System.EventHandler(this.gridView1_RowCountChanged);
            // 
            // No
            // 
            this.No.Caption = "No";
            this.No.FieldName = "No";
            this.No.MaxWidth = 40;
            this.No.MinWidth = 40;
            this.No.Name = "No";
            this.No.OptionsColumn.AllowEdit = false;
            this.No.Visible = true;
            this.No.VisibleIndex = 0;
            this.No.Width = 40;
            // 
            // ProductId
            // 
            this.ProductId.Caption = "ProductId";
            this.ProductId.FieldName = "ProductId";
            this.ProductId.Name = "ProductId";
            this.ProductId.OptionsColumn.AllowEdit = false;
            // 
            // Barcode
            // 
            this.Barcode.Caption = "Barcode";
            this.Barcode.FieldName = "Barcode";
            this.Barcode.Name = "Barcode";
            this.Barcode.OptionsColumn.AllowEdit = false;
            // 
            // Kode_Item
            // 
            this.Kode_Item.Caption = "Kode Item";
            this.Kode_Item.FieldName = "Kode_Item";
            this.Kode_Item.Name = "Kode_Item";
            this.Kode_Item.OptionsColumn.AllowEdit = false;
            // 
            // ProductName
            // 
            this.ProductName.Caption = "Nama Barang";
            this.ProductName.FieldName = "ProductName";
            this.ProductName.MinWidth = 200;
            this.ProductName.Name = "ProductName";
            this.ProductName.OptionsColumn.AllowEdit = false;
            this.ProductName.Visible = true;
            this.ProductName.VisibleIndex = 1;
            this.ProductName.Width = 200;
            // 
            // Satuan
            // 
            this.Satuan.Caption = "Satuan";
            this.Satuan.FieldName = "Satuan";
            this.Satuan.MaxWidth = 50;
            this.Satuan.MinWidth = 50;
            this.Satuan.Name = "Satuan";
            this.Satuan.OptionsColumn.AllowEdit = false;
            this.Satuan.Visible = true;
            this.Satuan.VisibleIndex = 2;
            this.Satuan.Width = 50;
            // 
            // Qty
            // 
            this.Qty.Caption = "Qty";
            this.Qty.FieldName = "Qty";
            this.Qty.MaxWidth = 50;
            this.Qty.MinWidth = 50;
            this.Qty.Name = "Qty";
            this.Qty.Visible = true;
            this.Qty.VisibleIndex = 3;
            this.Qty.Width = 50;
            // 
            // Hpp
            // 
            this.Hpp.Caption = "Hpp";
            this.Hpp.FieldName = "Hpp";
            this.Hpp.Name = "Hpp";
            // 
            // Price
            // 
            this.Price.Caption = "Harga";
            this.Price.DisplayFormat.FormatString = "n0";
            this.Price.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.Price.FieldName = "Price";
            this.Price.MaxWidth = 80;
            this.Price.MinWidth = 80;
            this.Price.Name = "Price";
            this.Price.OptionsColumn.AllowEdit = false;
            this.Price.Visible = true;
            this.Price.VisibleIndex = 4;
            this.Price.Width = 80;
            // 
            // Bruto
            // 
            this.Bruto.Caption = "Bruto";
            this.Bruto.DisplayFormat.FormatString = "n0";
            this.Bruto.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.Bruto.FieldName = "Bruto";
            this.Bruto.MaxWidth = 80;
            this.Bruto.MinWidth = 80;
            this.Bruto.Name = "Bruto";
            this.Bruto.OptionsColumn.AllowEdit = false;
            this.Bruto.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] {
            new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Bruto", "{0:n0}")});
            this.Bruto.Visible = true;
            this.Bruto.VisibleIndex = 5;
            this.Bruto.Width = 80;
            // 
            // Potongan
            // 
            this.Potongan.Caption = "Potongan";
            this.Potongan.DisplayFormat.FormatString = "n0";
            this.Potongan.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.Potongan.FieldName = "Potongan";
            this.Potongan.MaxWidth = 80;
            this.Potongan.MinWidth = 80;
            this.Potongan.Name = "Potongan";
            this.Potongan.OptionsColumn.AllowEdit = false;
            this.Potongan.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] {
            new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Potongan", "{0:n0}")});
            this.Potongan.Visible = true;
            this.Potongan.VisibleIndex = 6;
            this.Potongan.Width = 80;
            // 
            // Total
            // 
            this.Total.Caption = "Total";
            this.Total.DisplayFormat.FormatString = "n0";
            this.Total.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.Total.FieldName = "Total";
            this.Total.MaxWidth = 80;
            this.Total.MinWidth = 80;
            this.Total.Name = "Total";
            this.Total.OptionsColumn.AllowEdit = false;
            this.Total.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] {
            new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Total", "{0:n0}")});
            this.Total.Visible = true;
            this.Total.VisibleIndex = 7;
            this.Total.Width = 80;
            // 
            // barManager1
            // 
            this.barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] {
            this.bar1});
            this.barManager1.DockControls.Add(this.barDockControlTop);
            this.barManager1.DockControls.Add(this.barDockControlBottom);
            this.barManager1.DockControls.Add(this.barDockControlLeft);
            this.barManager1.DockControls.Add(this.barDockControlRight);
            this.barManager1.Form = this;
            this.barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.bbibayar,
            this.bbipending,
            this.bbibatal,
            this.barLargeButtonItem2,
            this.blbibayar,
            this.blbipending,
            this.blbibatal,
            this.barLargeButtonItem1,
            this.barLargeButtonItem3});
            this.barManager1.MainMenu = this.bar1;
            this.barManager1.MaxItemId = 14;
            // 
            // bar1
            // 
            this.bar1.BarName = "Custom 3";
            this.bar1.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Top;
            this.bar1.DockCol = 0;
            this.bar1.DockRow = 0;
            this.bar1.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            this.bar1.FloatLocation = new System.Drawing.Point(1291, 355);
            this.bar1.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.blbibayar),
            new DevExpress.XtraBars.LinkPersistInfo(this.blbipending),
            new DevExpress.XtraBars.LinkPersistInfo(this.barLargeButtonItem1),
            new DevExpress.XtraBars.LinkPersistInfo(this.barLargeButtonItem3),
            new DevExpress.XtraBars.LinkPersistInfo(this.blbibatal)});
            this.bar1.OptionsBar.MultiLine = true;
            this.bar1.OptionsBar.UseWholeRow = true;
            this.bar1.Text = "Custom 3";
            // 
            // blbibayar
            // 
            this.blbibayar.Caption = "&Bayar";
            this.blbibayar.Hint = "Klik this or Press END Button";
            this.blbibayar.Id = 7;
            this.blbibayar.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("blbibayar.ImageOptions.Image")));
            this.blbibayar.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("blbibayar.ImageOptions.LargeImage")));
            this.blbibayar.Name = "blbibayar";
            this.blbibayar.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            this.blbibayar.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.blbibayar_ItemClick);
            // 
            // blbipending
            // 
            this.blbipending.Caption = "Pending";
            this.blbipending.Id = 8;
            this.blbipending.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("blbipending.ImageOptions.Image")));
            this.blbipending.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("blbipending.ImageOptions.LargeImage")));
            this.blbipending.Name = "blbipending";
            this.blbipending.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            this.blbipending.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.blbipending_ItemClick);
            // 
            // barLargeButtonItem1
            // 
            this.barLargeButtonItem1.Caption = "Proses";
            this.barLargeButtonItem1.Id = 12;
            this.barLargeButtonItem1.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem1.ImageOptions.Image")));
            this.barLargeButtonItem1.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem1.ImageOptions.LargeImage")));
            this.barLargeButtonItem1.Name = "barLargeButtonItem1";
            this.barLargeButtonItem1.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barLargeButtonItem1_ItemClick);
            // 
            // barLargeButtonItem3
            // 
            this.barLargeButtonItem3.Caption = "Refresh";
            this.barLargeButtonItem3.Id = 13;
            this.barLargeButtonItem3.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem3.ImageOptions.Image")));
            this.barLargeButtonItem3.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem3.ImageOptions.LargeImage")));
            this.barLargeButtonItem3.Name = "barLargeButtonItem3";
            this.barLargeButtonItem3.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barLargeButtonItem3_ItemClick);
            // 
            // blbibatal
            // 
            this.blbibatal.Caption = "Batal";
            this.blbibatal.Id = 9;
            this.blbibatal.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("blbibatal.ImageOptions.Image")));
            this.blbibatal.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("blbibatal.ImageOptions.LargeImage")));
            this.blbibatal.Name = "blbibatal";
            this.blbibatal.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            this.blbibatal.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.blbibatal_ItemClick);
            // 
            // barDockControlTop
            // 
            this.barDockControlTop.CausesValidation = false;
            this.barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.barDockControlTop.Location = new System.Drawing.Point(0, 0);
            this.barDockControlTop.Manager = this.barManager1;
            this.barDockControlTop.Size = new System.Drawing.Size(817, 56);
            // 
            // barDockControlBottom
            // 
            this.barDockControlBottom.CausesValidation = false;
            this.barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.barDockControlBottom.Location = new System.Drawing.Point(0, 644);
            this.barDockControlBottom.Manager = this.barManager1;
            this.barDockControlBottom.Size = new System.Drawing.Size(817, 0);
            // 
            // barDockControlLeft
            // 
            this.barDockControlLeft.CausesValidation = false;
            this.barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.barDockControlLeft.Location = new System.Drawing.Point(0, 56);
            this.barDockControlLeft.Manager = this.barManager1;
            this.barDockControlLeft.Size = new System.Drawing.Size(0, 588);
            // 
            // barDockControlRight
            // 
            this.barDockControlRight.CausesValidation = false;
            this.barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.barDockControlRight.Location = new System.Drawing.Point(817, 56);
            this.barDockControlRight.Manager = this.barManager1;
            this.barDockControlRight.Size = new System.Drawing.Size(0, 588);
            // 
            // bbibayar
            // 
            this.bbibayar.Caption = "Bayar";
            this.bbibayar.Id = 0;
            this.bbibayar.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("bbibayar.ImageOptions.Image")));
            this.bbibayar.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("bbibayar.ImageOptions.LargeImage")));
            this.bbibayar.Name = "bbibayar";
            this.bbibayar.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbipending
            // 
            this.bbipending.Caption = "Pending";
            this.bbipending.Id = 1;
            this.bbipending.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("bbipending.ImageOptions.Image")));
            this.bbipending.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("bbipending.ImageOptions.LargeImage")));
            this.bbipending.Name = "bbipending";
            this.bbipending.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbibatal
            // 
            this.bbibatal.Caption = "Batal";
            this.bbibatal.Id = 2;
            this.bbibatal.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("bbibatal.ImageOptions.Image")));
            this.bbibatal.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("bbibatal.ImageOptions.LargeImage")));
            this.bbibatal.Name = "bbibatal";
            this.bbibatal.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // barLargeButtonItem2
            // 
            this.barLargeButtonItem2.Caption = "bayar";
            this.barLargeButtonItem2.Id = 6;
            this.barLargeButtonItem2.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem2.ImageOptions.Image")));
            this.barLargeButtonItem2.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barLargeButtonItem2.ImageOptions.LargeImage")));
            this.barLargeButtonItem2.Name = "barLargeButtonItem2";
            this.barLargeButtonItem2.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.labelControl2);
            this.groupControl1.Controls.Add(this.labelControl1);
            this.groupControl1.Controls.Add(this.barcodeTextBox);
            this.groupControl1.Controls.Add(this.detanggal);
            this.groupControl1.Controls.Add(this.txtjam);
            this.groupControl1.Controls.Add(this.txtkasir);
            this.groupControl1.Controls.Add(this.txtnotransaksi);
            this.groupControl1.Location = new System.Drawing.Point(20, 76);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(372, 123);
            this.groupControl1.TabIndex = 0;
            this.groupControl1.Text = "Informasi Transaksi";
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(7, 59);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(38, 13);
            this.labelControl2.TabIndex = 10;
            this.labelControl2.Text = "Tanggal";
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(7, 33);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(61, 13);
            this.labelControl1.TabIndex = 11;
            this.labelControl1.Text = "No Transaksi";
            // 
            // detanggal
            // 
            this.detanggal.EditValue = null;
            this.detanggal.Location = new System.Drawing.Point(92, 56);
            this.detanggal.Name = "detanggal";
            this.detanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.detanggal.Properties.ReadOnly = true;
            this.detanggal.Size = new System.Drawing.Size(164, 20);
            this.detanggal.TabIndex = 1;
            // 
            // txtjam
            // 
            this.txtjam.Location = new System.Drawing.Point(266, 56);
            this.txtjam.Name = "txtjam";
            this.txtjam.Properties.ReadOnly = true;
            this.txtjam.Size = new System.Drawing.Size(100, 20);
            this.txtjam.TabIndex = 3;
            // 
            // txtkasir
            // 
            this.txtkasir.Location = new System.Drawing.Point(266, 30);
            this.txtkasir.Name = "txtkasir";
            this.txtkasir.Properties.ReadOnly = true;
            this.txtkasir.Size = new System.Drawing.Size(100, 20);
            this.txtkasir.TabIndex = 2;
            // 
            // txtnotransaksi
            // 
            this.txtnotransaksi.Location = new System.Drawing.Point(92, 30);
            this.txtnotransaksi.Name = "txtnotransaksi";
            this.txtnotransaksi.Properties.ReadOnly = true;
            this.txtnotransaksi.Size = new System.Drawing.Size(164, 20);
            this.txtnotransaksi.TabIndex = 0;
            // 
            // ucPenjualan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupControl1);
            this.Controls.Add(this.gridControl1);
            this.Controls.Add(this.txttotalpenjualan);
            this.Controls.Add(this.barDockControlLeft);
            this.Controls.Add(this.barDockControlRight);
            this.Controls.Add(this.barDockControlBottom);
            this.Controls.Add(this.barDockControlTop);
            this.Name = "ucPenjualan";
            this.Size = new System.Drawing.Size(817, 644);
            this.Load += new System.EventHandler(this.ucPenjualan_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ucPenjualan_KeyDown);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.ucPenjualan_PreviewKeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.txttotalpenjualan.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.barcodeTextBox.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.barManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            this.groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.detanggal.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.detanggal.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtjam.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtkasir.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtnotransaksi.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.TextEdit txttotalpenjualan;
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
        private DevExpress.XtraBars.BarLargeButtonItem blbibayar;
        private DevExpress.XtraBars.BarLargeButtonItem blbipending;
        private DevExpress.XtraBars.BarLargeButtonItem blbibatal;
        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.DateEdit detanggal;
        private DevExpress.XtraEditors.TextEdit txtjam;
        private DevExpress.XtraEditors.TextEdit txtkasir;
        private DevExpress.XtraEditors.TextEdit txtnotransaksi;
        private DevExpress.XtraGrid.Columns.GridColumn Hpp;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem3;
    }
}
