namespace BackOffice.UC
{
    partial class ucDaftarKAS
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucDaftarKAS));
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar2 = new DevExpress.XtraBars.Bar();
            barStaticItem1 = new DevExpress.XtraBars.BarStaticItem();
            bei_bulan = new DevExpress.XtraBars.BarEditItem();
            repositoryItemComboBox1 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            bei_tahun = new DevExpress.XtraBars.BarEditItem();
            repositoryItemSpinEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            barLargeButtonItem1 = new DevExpress.XtraBars.BarLargeButtonItem();
            BLBICETAK = new DevExpress.XtraBars.BarLargeButtonItem();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            repositoryItemLookUpEdit_Bulan = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            repositoryItemSpinEdit_Tahun = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            repositoryItemLookUpEdit_Remise = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            repositoryItemComboBox_Bulan = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repositoryItemComboBox2 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repositoryItemRadioGroup1 = new DevExpress.XtraEditors.Repository.RepositoryItemRadioGroup();
            imageCollection1 = new DevExpress.Utils.ImageCollection(components);
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemSpinEdit1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemLookUpEdit_Bulan).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemSpinEdit_Tahun).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemLookUpEdit_Remise).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox_Bulan).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemRadioGroup1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 56);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(611, 384);
            gridControl1.TabIndex = 2;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsView.ShowFooter = true;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.PopupMenuShowing += gridView1_PopupMenuShowing;
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar2 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { barStaticItem1, BLBICETAK, bei_bulan, bei_tahun, barLargeButtonItem1 });
            barManager1.MainMenu = bar2;
            barManager1.MaxItemId = 10;
            barManager1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemLookUpEdit_Bulan, repositoryItemSpinEdit_Tahun, repositoryItemLookUpEdit_Remise, repositoryItemComboBox_Bulan, repositoryItemComboBox1, repositoryItemSpinEdit1, repositoryItemComboBox2, repositoryItemRadioGroup1 });
            // 
            // bar2
            // 
            bar2.BarName = "Main menu";
            bar2.DockCol = 0;
            bar2.DockRow = 0;
            bar2.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar2.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(barStaticItem1), new DevExpress.XtraBars.LinkPersistInfo(bei_bulan), new DevExpress.XtraBars.LinkPersistInfo(bei_tahun), new DevExpress.XtraBars.LinkPersistInfo(barLargeButtonItem1), new DevExpress.XtraBars.LinkPersistInfo(BLBICETAK) });
            bar2.OptionsBar.MultiLine = true;
            bar2.OptionsBar.UseWholeRow = true;
            bar2.Text = "Main menu";
            // 
            // barStaticItem1
            // 
            barStaticItem1.Caption = "Periode";
            barStaticItem1.Id = 3;
            barStaticItem1.Name = "barStaticItem1";
            // 
            // bei_bulan
            // 
            bei_bulan.Caption = "Bulan";
            bei_bulan.Edit = repositoryItemComboBox1;
            bei_bulan.Id = 5;
            bei_bulan.Name = "bei_bulan";
            bei_bulan.Size = new Size(100, 0);
            bei_bulan.EditValueChanged += bei_bulan_EditValueChanged;
            // 
            // repositoryItemComboBox1
            // 
            repositoryItemComboBox1.AutoHeight = false;
            repositoryItemComboBox1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemComboBox1.Name = "repositoryItemComboBox1";
            repositoryItemComboBox1.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            // 
            // bei_tahun
            // 
            bei_tahun.Caption = "Tahun";
            bei_tahun.Edit = repositoryItemSpinEdit1;
            bei_tahun.Id = 6;
            bei_tahun.Name = "bei_tahun";
            bei_tahun.EditValueChanged += bei_tahun_EditValueChanged;
            // 
            // repositoryItemSpinEdit1
            // 
            repositoryItemSpinEdit1.AutoHeight = false;
            repositoryItemSpinEdit1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemSpinEdit1.Name = "repositoryItemSpinEdit1";
            // 
            // barLargeButtonItem1
            // 
            barLargeButtonItem1.Caption = "Add";
            barLargeButtonItem1.Id = 9;
            barLargeButtonItem1.ImageOptions.Image = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.Image");
            barLargeButtonItem1.ImageOptions.LargeImage = (Image)resources.GetObject("barLargeButtonItem1.ImageOptions.LargeImage");
            barLargeButtonItem1.Name = "barLargeButtonItem1";
            barLargeButtonItem1.ItemClick += barLargeButtonItem1_ItemClick;
            // 
            // BLBICETAK
            // 
            BLBICETAK.Caption = "Cetak";
            BLBICETAK.Id = 4;
            BLBICETAK.ImageOptions.Image = (Image)resources.GetObject("BLBICETAK.ImageOptions.Image");
            BLBICETAK.ImageOptions.LargeImage = (Image)resources.GetObject("BLBICETAK.ImageOptions.LargeImage");
            BLBICETAK.Name = "BLBICETAK";
            BLBICETAK.ItemClick += BLBICETAK_ItemClick;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(611, 56);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 440);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(611, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 56);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 384);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(611, 56);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 384);
            // 
            // repositoryItemLookUpEdit_Bulan
            // 
            repositoryItemLookUpEdit_Bulan.AutoHeight = false;
            repositoryItemLookUpEdit_Bulan.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemLookUpEdit_Bulan.Name = "repositoryItemLookUpEdit_Bulan";
            // 
            // repositoryItemSpinEdit_Tahun
            // 
            repositoryItemSpinEdit_Tahun.AutoHeight = false;
            repositoryItemSpinEdit_Tahun.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemSpinEdit_Tahun.Name = "repositoryItemSpinEdit_Tahun";
            // 
            // repositoryItemLookUpEdit_Remise
            // 
            repositoryItemLookUpEdit_Remise.AutoHeight = false;
            repositoryItemLookUpEdit_Remise.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemLookUpEdit_Remise.Name = "repositoryItemLookUpEdit_Remise";
            // 
            // repositoryItemComboBox_Bulan
            // 
            repositoryItemComboBox_Bulan.AutoHeight = false;
            repositoryItemComboBox_Bulan.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemComboBox_Bulan.Name = "repositoryItemComboBox_Bulan";
            // 
            // repositoryItemComboBox2
            // 
            repositoryItemComboBox2.AutoHeight = false;
            repositoryItemComboBox2.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            repositoryItemComboBox2.Name = "repositoryItemComboBox2";
            // 
            // repositoryItemRadioGroup1
            // 
            repositoryItemRadioGroup1.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] { new DevExpress.XtraEditors.Controls.RadioGroupItem("Rekap", "Rekap", true, null, ""), new DevExpress.XtraEditors.Controls.RadioGroupItem("Detail", "Detail") });
            repositoryItemRadioGroup1.Name = "repositoryItemRadioGroup1";
            // 
            // imageCollection1
            // 
            imageCollection1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("imageCollection1.ImageStream");
            imageCollection1.Images.SetKeyName(0, "edit_32x32.png");
            // 
            // ucDaftarKAS
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(gridControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "ucDaftarKAS";
            Size = new Size(611, 440);
            Load += ucDaftarKAS_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemSpinEdit1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemLookUpEdit_Bulan).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemSpinEdit_Tahun).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemLookUpEdit_Remise).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox_Bulan).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemComboBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemRadioGroup1).EndInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.Bar bar2;
        private DevExpress.XtraBars.BarStaticItem barStaticItem1;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit repositoryItemLookUpEdit_Bulan;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit_Tahun;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit repositoryItemLookUpEdit_Remise;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraBars.BarLargeButtonItem BLBICETAK;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox_Bulan;
        private DevExpress.XtraBars.BarEditItem bei_bulan;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox1;
        private DevExpress.XtraBars.BarEditItem bei_tahun;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit1;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox2;
        private DevExpress.XtraEditors.Repository.RepositoryItemRadioGroup repositoryItemRadioGroup1;
        private DevExpress.Utils.ImageCollection imageCollection1;
        private DevExpress.XtraBars.BarLargeButtonItem barLargeButtonItem1;
    }
}
