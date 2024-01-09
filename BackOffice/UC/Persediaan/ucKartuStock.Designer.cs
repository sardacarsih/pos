namespace BackOffice.UC.Persediaan
{
    partial class ucKartuStock
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucKartuStock));
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            comboBoxEditBulan = new DevExpress.XtraEditors.ComboBoxEdit();
            sbcetak = new DevExpress.XtraEditors.SimpleButton();
            setahun = new DevExpress.XtraEditors.SpinEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            searchLookUpEdit1 = new DevExpress.XtraEditors.SearchLookUpEdit();
            searchLookUpEdit2View = new DevExpress.XtraGrid.Views.Grid.GridView();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)comboBoxEditBulan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit2View).BeginInit();
            sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            SuspendLayout();
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(comboBoxEditBulan);
            sidePanel1.Controls.Add(sbcetak);
            sidePanel1.Controls.Add(setahun);
            sidePanel1.Controls.Add(labelControl1);
            sidePanel1.Controls.Add(searchLookUpEdit1);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(714, 40);
            sidePanel1.TabIndex = 0;
            // 
            // comboBoxEditBulan
            // 
            comboBoxEditBulan.Location = new Point(66, 9);
            comboBoxEditBulan.Name = "comboBoxEditBulan";
            comboBoxEditBulan.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            comboBoxEditBulan.Size = new Size(148, 20);
            comboBoxEditBulan.TabIndex = 6;
            comboBoxEditBulan.SelectedIndexChanged += comboBoxEditBulan_SelectedIndexChanged;
            // 
            // sbcetak
            // 
            sbcetak.ImageOptions.Image = (Image)resources.GetObject("sbcetak.ImageOptions.Image");
            sbcetak.Location = new Point(579, 6);
            sbcetak.Name = "sbcetak";
            sbcetak.Size = new Size(75, 23);
            sbcetak.TabIndex = 5;
            sbcetak.Text = "Preview";
            sbcetak.Click += sbcetak_Click;
            // 
            // setahun
            // 
            setahun.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            setahun.Location = new Point(235, 9);
            setahun.Name = "setahun";
            setahun.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            setahun.Size = new Size(90, 20);
            setahun.TabIndex = 4;
            setahun.EditValueChanged += spinEdit1_EditValueChanged;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(7, 16);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(26, 13);
            labelControl1.TabIndex = 2;
            labelControl1.Text = "Bulan";
            // 
            // searchLookUpEdit1
            // 
            searchLookUpEdit1.Location = new Point(353, 10);
            searchLookUpEdit1.Name = "searchLookUpEdit1";
            searchLookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            searchLookUpEdit1.Properties.PopupView = searchLookUpEdit2View;
            searchLookUpEdit1.Size = new Size(209, 20);
            searchLookUpEdit1.TabIndex = 7;
            searchLookUpEdit1.EditValueChanged += searchLookUpEdit1_EditValueChanged;
            // 
            // searchLookUpEdit2View
            // 
            searchLookUpEdit2View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            searchLookUpEdit2View.Name = "searchLookUpEdit2View";
            searchLookUpEdit2View.OptionsSelection.EnableAppearanceFocusedCell = false;
            searchLookUpEdit2View.OptionsView.ShowGroupPanel = false;
            // 
            // sidePanel2
            // 
            sidePanel2.Controls.Add(gridControl1);
            sidePanel2.Dock = DockStyle.Fill;
            sidePanel2.Location = new Point(0, 40);
            sidePanel2.Name = "sidePanel2";
            sidePanel2.Size = new Size(714, 372);
            sidePanel2.TabIndex = 1;
            sidePanel2.Text = "sidePanel2";
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(714, 372);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // ucKartuStock
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucKartuStock";
            Size = new Size(714, 412);
            Load += ucKartuStock_Load;
            sidePanel1.ResumeLayout(false);
            sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)comboBoxEditBulan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)searchLookUpEdit2View).EndInit();
            sidePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SpinEdit setahun;
        private DevExpress.XtraEditors.SimpleButton sbcetak;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEditBulan;
        private DevExpress.XtraEditors.SearchLookUpEdit searchLookUpEdit1;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit2View;
    }
}
