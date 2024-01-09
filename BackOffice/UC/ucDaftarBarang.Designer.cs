namespace BackOffice.UC
{
    partial class ucDaftarBarang
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucDaftarBarang));
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridControl2 = new DevExpress.XtraGrid.GridControl();
            gridView2 = new DevExpress.XtraGrid.Views.Grid.GridView();
            simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            txtminqty = new DevExpress.XtraEditors.TextEdit();
            txtpotrp = new DevExpress.XtraEditors.TextEdit();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar2 = new DevExpress.XtraBars.Bar();
            barLargeButtonItem1 = new DevExpress.XtraBars.BarLargeButtonItem();
            blbiubahbarang = new DevExpress.XtraBars.BarLargeButtonItem();
            barLargeButtonItem3 = new DevExpress.XtraBars.BarLargeButtonItem();
            barCheckItem2 = new DevExpress.XtraBars.BarCheckItem();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            bbbaru = new DevExpress.XtraBars.BarButtonItem();
            bbubah = new DevExpress.XtraBars.BarButtonItem();
            bbhapus = new DevExpress.XtraBars.BarButtonItem();
            barCheckItem1 = new DevExpress.XtraBars.BarCheckItem();
            barLargeButtonItem2 = new DevExpress.XtraBars.BarLargeButtonItem();
            barEditItem1 = new DevExpress.XtraBars.BarEditItem();
            repositoryItemRadioGroup1 = new DevExpress.XtraEditors.Repository.RepositoryItemRadioGroup();
            barToggleSwitchItem1 = new DevExpress.XtraBars.BarToggleSwitchItem();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridControl2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtminqty.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtpotrp.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemRadioGroup1).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridControl1.Location = new Point(18, 75);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(774, 447);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsCustomization.CustomizationFormSearchBoxVisible = true;
            gridView1.OptionsFilter.AllowAutoFilterConditionChange = DevExpress.Utils.DefaultBoolean.True;
            gridView1.OptionsFind.AlwaysVisible = true;
            gridView1.OptionsFind.Behavior = DevExpress.XtraEditors.FindPanelBehavior.Filter;
            gridView1.OptionsFind.FindMode = DevExpress.XtraEditors.FindMode.Always;
            gridView1.OptionsFind.ShowSearchNavButtons = false;
            gridView1.OptionsView.ShowAutoFilterRow = true;
            gridView1.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.KeyUp += gridView1_KeyUp;
            gridView1.Click += gridView1_Click;
            // 
            // gridControl2
            // 
            gridControl2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            gridControl2.Location = new Point(798, 117);
            gridControl2.MainView = gridView2;
            gridControl2.Name = "gridControl2";
            gridControl2.Size = new Size(287, 405);
            gridControl2.TabIndex = 1;
            gridControl2.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView2 });
            // 
            // gridView2
            // 
            gridView2.GridControl = gridControl2;
            gridView2.Name = "gridView2";
            gridView2.OptionsBehavior.Editable = false;
            gridView2.OptionsView.ShowGroupPanel = false;
            // 
            // simpleButton1
            // 
            simpleButton1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            simpleButton1.Location = new Point(1012, 77);
            simpleButton1.Name = "simpleButton1";
            simpleButton1.Size = new Size(75, 34);
            simpleButton1.TabIndex = 2;
            simpleButton1.Text = "Add/Update";
            simpleButton1.Click += simpleButton1_Click;
            // 
            // txtminqty
            // 
            txtminqty.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtminqty.Location = new Point(800, 77);
            txtminqty.Name = "txtminqty";
            txtminqty.Properties.AdvancedModeOptions.Label = "Min Qty";
            txtminqty.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtminqty.Size = new Size(100, 34);
            txtminqty.TabIndex = 3;
            // 
            // txtpotrp
            // 
            txtpotrp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtpotrp.Location = new Point(906, 77);
            txtpotrp.Name = "txtpotrp";
            txtpotrp.Properties.AdvancedModeOptions.Label = "Potongan";
            txtpotrp.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtpotrp.Size = new Size(100, 34);
            txtpotrp.TabIndex = 3;
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar2 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bbbaru, bbubah, bbhapus, barCheckItem1, barLargeButtonItem1, blbiubahbarang, barLargeButtonItem3, barCheckItem2, barLargeButtonItem2, barEditItem1, barToggleSwitchItem1 });
            barManager1.MainMenu = bar2;
            barManager1.MaxItemId = 12;
            barManager1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemRadioGroup1 });
            // 
            // bar2
            // 
            bar2.BarName = "Main menu";
            bar2.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Top;
            bar2.DockCol = 0;
            bar2.DockRow = 0;
            bar2.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar2.FloatLocation = new Point(250, 97);
            bar2.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem1), new DevExpress.XtraBars.LinkPersistInfo(blbiubahbarang), new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem3), new DevExpress.XtraBars.LinkPersistInfo(barToggleSwitchItem1) });
            bar2.OptionsBar.MultiLine = true;
            bar2.OptionsBar.UseWholeRow = true;
            bar2.Text = "Main menu";
            // 
            // barLargeButtonItem1
            // 
            barLargeButtonItem1.Caption = "Baru";
            barLargeButtonItem1.Id = 5;
            barLargeButtonItem1.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.Image");
            barLargeButtonItem1.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.LargeImage");
            barLargeButtonItem1.Name = "barLargeButtonItem1";
            barLargeButtonItem1.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            barLargeButtonItem1.ItemClick += barLargeButtonItem1_ItemClick;
            // 
            // blbiubahbarang
            // 
            blbiubahbarang.Caption = "Ubah";
            blbiubahbarang.Id = 6;
            blbiubahbarang.ImageOptions.Image = (Image)resources.GetObject("blbiubahbarang.ImageOptions.Image");
            blbiubahbarang.ImageOptions.LargeImage = (Image)resources.GetObject("blbiubahbarang.ImageOptions.LargeImage");
            blbiubahbarang.Name = "blbiubahbarang";
            blbiubahbarang.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            blbiubahbarang.ItemClick += blbiubahbarang_ItemClick;
            // 
            // barLargeButtonItem3
            // 
            barLargeButtonItem3.Caption = "Hapus";
            barLargeButtonItem3.Id = 7;
            barLargeButtonItem3.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem3.ImageOptions.Image");
            barLargeButtonItem3.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem3.ImageOptions.LargeImage");
            barLargeButtonItem3.Name = "barLargeButtonItem3";
            barLargeButtonItem3.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            barLargeButtonItem3.ItemClick += barLargeButtonItem3_ItemClick;
            // 
            // barCheckItem2
            // 
            barCheckItem2.Caption = "Hanya Barang Diskon";
            barCheckItem2.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
            barCheckItem2.Id = 8;
            barCheckItem2.Name = "barCheckItem2";
            barCheckItem2.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(1116, 56);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 542);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(1116, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 56);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 486);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(1116, 56);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 486);
            // 
            // bbbaru
            // 
            bbbaru.Caption = "Baru";
            bbbaru.Id = 0;
            bbbaru.ImageOptions.Image = (Image)resources.GetObject("bbbaru.ImageOptions.Image");
            bbbaru.ImageOptions.LargeImage = (Image)resources.GetObject("bbbaru.ImageOptions.LargeImage");
            bbbaru.Name = "bbbaru";
            bbbaru.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbubah
            // 
            bbubah.Caption = "Ubah";
            bbubah.Id = 1;
            bbubah.ImageOptions.Image = (Image)resources.GetObject("bbubah.ImageOptions.Image");
            bbubah.ImageOptions.LargeImage = (Image)resources.GetObject("bbubah.ImageOptions.LargeImage");
            bbubah.Name = "bbubah";
            bbubah.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // bbhapus
            // 
            bbhapus.Caption = "Hapus";
            bbhapus.Id = 2;
            bbhapus.ImageOptions.Image = (Image)resources.GetObject("bbhapus.ImageOptions.Image");
            bbhapus.ImageOptions.LargeImage = (Image)resources.GetObject("bbhapus.ImageOptions.LargeImage");
            bbhapus.Name = "bbhapus";
            bbhapus.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // barCheckItem1
            // 
            barCheckItem1.Caption = "Diskon";
            barCheckItem1.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
            barCheckItem1.Id = 4;
            barCheckItem1.Name = "barCheckItem1";
            barCheckItem1.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // barLargeButtonItem2
            // 
            barLargeButtonItem2.Caption = "barLargeButtonItem2";
            barLargeButtonItem2.Id = 9;
            barLargeButtonItem2.Name = "barLargeButtonItem2";
            // 
            // barEditItem1
            // 
            barEditItem1.Caption = "barEditItem1";
            barEditItem1.Edit = repositoryItemRadioGroup1;
            barEditItem1.Id = 10;
            barEditItem1.Name = "barEditItem1";
            // 
            // repositoryItemRadioGroup1
            // 
            repositoryItemRadioGroup1.Name = "repositoryItemRadioGroup1";
            // 
            // barToggleSwitchItem1
            // 
            barToggleSwitchItem1.Caption = "Non Aktif";
            barToggleSwitchItem1.Id = 11;
            barToggleSwitchItem1.Name = "barToggleSwitchItem1";
            barToggleSwitchItem1.CheckedChanged += barToggleSwitchItem1_CheckedChanged;
            // 
            // ucDaftarBarang
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(txtpotrp);
            Controls.Add(txtminqty);
            Controls.Add(simpleButton1);
            Controls.Add(gridControl2);
            Controls.Add(gridControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucDaftarBarang";
            Size = new Size(1116, 542);
            Load += ucDaftarBarang_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridControl2).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView2).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtminqty.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtpotrp.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemRadioGroup1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.GridControl gridControl2;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView2;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.TextEdit txtminqty;
        private DevExpress.XtraEditors.TextEdit txtpotrp;
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.Bar bar2;
        private DevExpress.XtraBars.BarButtonItem bbbaru;
        private DevExpress.XtraBars.BarButtonItem bbubah;
        private DevExpress.XtraBars.BarButtonItem bbhapus;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraBars.BarCheckItem barCheckItem1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem1;
        private DevExpress.XtraBars.BarLargeButtonItem blbiubahbarang;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem3;
        private DevExpress.XtraBars.BarCheckItem barCheckItem2;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchItem1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem2;
        private DevExpress.XtraBars.BarEditItem barEditItem1;
        private DevExpress.XtraEditors.Repository.RepositoryItemRadioGroup repositoryItemRadioGroup1;
    }
}
