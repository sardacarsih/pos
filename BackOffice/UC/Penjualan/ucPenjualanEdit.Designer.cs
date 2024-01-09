namespace BackOffice.UC
{
    partial class ucPenjualanEdit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPenjualanEdit));
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
            blbiupdate = new DevExpress.XtraBars.BarLargeButtonItem();
            blbitutup = new DevExpress.XtraBars.BarLargeButtonItem();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            bbibayar = new DevExpress.XtraBars.BarButtonItem();
            bbipending = new DevExpress.XtraBars.BarButtonItem();
            bbibatal = new DevExpress.XtraBars.BarButtonItem();
            barLargeButtonItem2 = new DevExpress.XtraBars.BarLargeButtonItem();
            groupControl1 = new DevExpress.XtraEditors.GroupControl();
            searchLookUpEdit1 = new DevExpress.XtraEditors.SearchLookUpEdit();
            searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            leangsuran = new DevExpress.XtraEditors.LookUpEdit();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            detanggal = new DevExpress.XtraEditors.DateEdit();
            txtjam = new DevExpress.XtraEditors.TextEdit();
            txtkasir = new DevExpress.XtraEditors.TextEdit();
            txtnotransaksi = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).BeginInit();
            groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1View).BeginInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtjam.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtkasir.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnotransaksi.Properties).BeginInit();
            SuspendLayout();
            // 
            // barcodeTextBox
            // 
            barcodeTextBox.Location = new Point(399, 177);
            barcodeTextBox.Name = "barcodeTextBox";
            barcodeTextBox.Properties.AdvancedModeOptions.Label = "Barcode";
            barcodeTextBox.Properties.Appearance.Options.UseFont = true;
            barcodeTextBox.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            barcodeTextBox.Size = new Size(359, 34);
            barcodeTextBox.TabIndex = 0;
            barcodeTextBox.KeyDown += barcodeTextBox_KeyDown;
            // 
            // gridControl1
            // 
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridControl1.Location = new Point(18, 222);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(785, 379);
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
            ProductName.MinWidth = 170;
            ProductName.Name = "ProductName";
            ProductName.OptionsColumn.AllowEdit = false;
            ProductName.Visible = true;
            ProductName.VisibleIndex = 1;
            ProductName.Width = 200;
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
            Qty.MinWidth = 50;
            Qty.Name = "Qty";
            Qty.Visible = true;
            Qty.VisibleIndex = 3;
            Qty.Width = 50;
            // 
            // Hpp
            // 
            Hpp.Caption = "Hpp";
            Hpp.FieldName = "Hpp";
            Hpp.Name = "Hpp";
            // 
            // Price
            // 
            Price.Caption = "Harga";
            Price.DisplayFormat.FormatString = "n0";
            Price.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            Price.FieldName = "Price";
            Price.MaxWidth = 80;
            Price.MinWidth = 80;
            Price.Name = "Price";
            Price.OptionsColumn.AllowEdit = false;
            Price.Visible = true;
            Price.VisibleIndex = 4;
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
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bbibayar, bbipending, bbibatal, barLargeButtonItem2, blbiupdate, blbitutup });
            barManager1.MainMenu = bar1;
            barManager1.MaxItemId = 14;
            // 
            // bar1
            // 
            bar1.BarName = "Custom 3";
            bar1.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Top;
            bar1.DockCol = 0;
            bar1.DockRow = 0;
            bar1.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar1.FloatLocation = new Point(1291, 355);
            bar1.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(blbiupdate), new DevExpress.XtraBars.LinkPersistInfo(blbitutup) });
            bar1.OptionsBar.MultiLine = true;
            bar1.OptionsBar.UseWholeRow = true;
            bar1.Text = "Custom 3";
            // 
            // blbiupdate
            // 
            blbiupdate.Caption = "Simpan";
            blbiupdate.Id = 7;
            blbiupdate.ImageOptions.Image = (Image)resources.GetObject("blbiupdate.ImageOptions.Image");
            blbiupdate.ImageOptions.LargeImage = (Image)resources.GetObject("blbiupdate.ImageOptions.LargeImage");
            blbiupdate.Name = "blbiupdate";
            blbiupdate.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            blbiupdate.ItemClick += blbiupdate_ItemClick;
            // 
            // blbitutup
            // 
            blbitutup.Caption = "Tutup";
            blbitutup.Id = 9;
            blbitutup.ImageOptions.Image = (Image)resources.GetObject("blbitutup.ImageOptions.Image");
            blbitutup.ImageOptions.LargeImage = (Image)resources.GetObject("blbitutup.ImageOptions.LargeImage");
            blbitutup.Name = "blbitutup";
            blbitutup.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            blbitutup.ItemClick += blbitutup_ItemClick;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(817, 58);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 644);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(817, 0);
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
            barDockControlRight.Location = new Point(817, 58);
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
            groupControl1.Controls.Add(searchLookUpEdit1);
            groupControl1.Controls.Add(labelControl4);
            groupControl1.Controls.Add(leangsuran);
            groupControl1.Controls.Add(labelControl3);
            groupControl1.Controls.Add(labelControl2);
            groupControl1.Controls.Add(labelControl1);
            groupControl1.Controls.Add(detanggal);
            groupControl1.Controls.Add(txtjam);
            groupControl1.Controls.Add(txtkasir);
            groupControl1.Controls.Add(txtnotransaksi);
            groupControl1.Location = new Point(20, 76);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new Size(373, 140);
            groupControl1.TabIndex = 1;
            groupControl1.Text = "Informasi Transaksi";
            // 
            // searchLookUpEdit1
            // 
            searchLookUpEdit1.EditValue = "";
            searchLookUpEdit1.Location = new Point(92, 82);
            searchLookUpEdit1.Name = "searchLookUpEdit1";
            searchLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            searchLookUpEdit1.Properties.NullText = "";
            searchLookUpEdit1.Properties.PopupView = searchLookUpEdit1View;
            searchLookUpEdit1.Size = new Size(274, 20);
            searchLookUpEdit1.TabIndex = 5;
            searchLookUpEdit1.Popup += searchLookUpEdit1_Popup;
            searchLookUpEdit1.EditValueChanged += searchLookUpEdit1_EditValueChanged;
            // 
            // searchLookUpEdit1View
            // 
            searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(7, 111);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(66, 13);
            labelControl4.TabIndex = 16;
            labelControl4.Text = "Jlh. Angsuran";
            // 
            // leangsuran
            // 
            leangsuran.Location = new Point(92, 108);
            leangsuran.Name = "leangsuran";
            leangsuran.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            leangsuran.Properties.NullText = "";
            leangsuran.Size = new Size(80, 20);
            leangsuran.TabIndex = 6;
            leangsuran.EditValueChanged += leangsuran_EditValueChanged;
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(7, 85);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(50, 13);
            labelControl3.TabIndex = 10;
            labelControl3.Text = "Pelanggan";
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
            // detanggal
            // 
            detanggal.EditValue = null;
            detanggal.Location = new Point(92, 56);
            detanggal.Name = "detanggal";
            detanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            detanggal.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            detanggal.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            detanggal.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            detanggal.Properties.ReadOnly = true;
            detanggal.Properties.UseMaskAsDisplayFormat = true;
            detanggal.Size = new Size(164, 20);
            detanggal.TabIndex = 3;
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
            txtkasir.TabIndex = 2;
            // 
            // txtnotransaksi
            // 
            txtnotransaksi.Location = new Point(92, 30);
            txtnotransaksi.Name = "txtnotransaksi";
            txtnotransaksi.Properties.ReadOnly = true;
            txtnotransaksi.Size = new Size(164, 20);
            txtnotransaksi.TabIndex = 1;
            // 
            // ucPenjualanEdit
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupControl1);
            Controls.Add(gridControl1);
            Controls.Add(barcodeTextBox);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucPenjualanEdit";
            Size = new Size(817, 644);
            Load += ucPenjualanEdit_Load;
            KeyDown += ucPenjualan_KeyDown;
            PreviewKeyDown += ucPenjualan_PreviewKeyDown;
            ((System.ComponentModel.ISupportInitialize)barcodeTextBox.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl1).EndInit();
            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1View).EndInit();
            ((System.ComponentModel.ISupportInitialize)leangsuran.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)detanggal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtjam.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtkasir.Properties).EndInit();
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
        private DevExpress.XtraBars.BarLargeButtonItem blbiupdate;
        private DevExpress.XtraBars.BarLargeButtonItem blbitutup;
        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.DateEdit detanggal;
        private DevExpress.XtraEditors.TextEdit txtjam;
        private DevExpress.XtraEditors.TextEdit txtkasir;
        private DevExpress.XtraEditors.TextEdit txtnotransaksi;
        private DevExpress.XtraGrid.Columns.GridColumn Hpp;
        private DevExpress.XtraEditors.SearchLookUpEdit searchLookUpEdit1;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit1View;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LookUpEdit leangsuran;
        private DevExpress.XtraEditors.LabelControl labelControl3;
    }
}
