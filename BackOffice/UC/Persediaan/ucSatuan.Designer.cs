namespace BackOffice.UC
{
    partial class ucSatuan
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucSatuan));
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            TXTSATUAN = new DevExpress.XtraEditors.TextEdit();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar2 = new DevExpress.XtraBars.Bar();
            barLargeButtonItem1 = new DevExpress.XtraBars.BarLargeButtonItem();
            barLargeButtonItem2 = new DevExpress.XtraBars.BarLargeButtonItem();
            barLargeButtonItem3 = new DevExpress.XtraBars.BarLargeButtonItem();
            bar3 = new DevExpress.XtraBars.Bar();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)TXTSATUAN.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gridControl1.Location = new Point(0, 115);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(319, 403);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            gridControl1.DoubleClick += gridControl1_DoubleClick;
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // TXTSATUAN
            // 
            TXTSATUAN.Location = new Point(3, 89);
            TXTSATUAN.Name = "TXTSATUAN";
            TXTSATUAN.Size = new Size(316, 20);
            TXTSATUAN.TabIndex = 1;
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar2, bar3 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { barLargeButtonItem1, barLargeButtonItem2, barLargeButtonItem3 });
            barManager1.MainMenu = bar2;
            barManager1.MaxItemId = 3;
            barManager1.StatusBar = bar3;
            // 
            // bar2
            // 
            bar2.BarName = "Main menu";
            bar2.DockCol = 0;
            bar2.DockRow = 0;
            bar2.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar2.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem1), new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem2), new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem3) });
            bar2.OptionsBar.MultiLine = true;
            bar2.OptionsBar.UseWholeRow = true;
            bar2.Text = "Main menu";
            // 
            // barLargeButtonItem1
            // 
            barLargeButtonItem1.Caption = "Tambah";
            barLargeButtonItem1.Id = 0;
            barLargeButtonItem1.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.Image");
            barLargeButtonItem1.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.LargeImage");
            barLargeButtonItem1.Name = "barLargeButtonItem1";
            barLargeButtonItem1.ItemClick += barLargeButtonItem1_ItemClick;
            // 
            // barLargeButtonItem2
            // 
            barLargeButtonItem2.Caption = "Ubah";
            barLargeButtonItem2.Id = 1;
            barLargeButtonItem2.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem2.ImageOptions.Image");
            barLargeButtonItem2.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem2.ImageOptions.LargeImage");
            barLargeButtonItem2.Name = "barLargeButtonItem2";
            barLargeButtonItem2.ItemClick += barLargeButtonItem2_ItemClick;
            // 
            // barLargeButtonItem3
            // 
            barLargeButtonItem3.Caption = "Hapus";
            barLargeButtonItem3.Id = 2;
            barLargeButtonItem3.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem3.ImageOptions.Image");
            barLargeButtonItem3.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem3.ImageOptions.LargeImage");
            barLargeButtonItem3.Name = "barLargeButtonItem3";
            barLargeButtonItem3.ItemClick += barLargeButtonItem3_ItemClick;
            // 
            // bar3
            // 
            bar3.BarName = "Status bar";
            bar3.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Bottom;
            bar3.DockCol = 0;
            bar3.DockRow = 0;
            bar3.DockStyle = DevExpress.XtraBars.BarDockStyle.Bottom;
            bar3.OptionsBar.AllowQuickCustomization = false;
            bar3.OptionsBar.DrawDragBorder = false;
            bar3.OptionsBar.UseWholeRow = true;
            bar3.Text = "Status bar";
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(323, 56);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 498);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(323, 20);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 56);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 442);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(323, 56);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 442);
            // 
            // ucSatuan
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(TXTSATUAN);
            Controls.Add(gridControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucSatuan";
            Size = new Size(323, 518);
            Load += ucSatuan_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)TXTSATUAN.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.TextEdit TXTSATUAN;
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.Bar bar2;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem2;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem3;
        private DevExpress.XtraBars.Bar bar3;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
    }
}
