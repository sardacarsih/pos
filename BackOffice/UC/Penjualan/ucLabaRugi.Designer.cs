namespace BackOffice.UC
{
    partial class ucLabaRugi
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucLabaRugi));
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            sbcetak = new DevExpress.XtraEditors.SimpleButton();
            spinEdit1 = new DevExpress.XtraEditors.SpinEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)spinEdit1.Properties).BeginInit();
            sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            SuspendLayout();
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(sbcetak);
            sidePanel1.Controls.Add(spinEdit1);
            sidePanel1.Controls.Add(labelControl1);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(714, 40);
            sidePanel1.TabIndex = 0;
            // 
            // sbcetak
            // 
            sbcetak.ImageOptions.Image = (Image)resources.GetObject("sbcetak.ImageOptions.Image");
            sbcetak.Location = new Point(257, 11);
            sbcetak.Name = "sbcetak";
            sbcetak.Size = new Size(75, 23);
            sbcetak.TabIndex = 5;
            sbcetak.Text = "Preview";
            sbcetak.Click += sbcetak_Click;
            // 
            // spinEdit1
            // 
            spinEdit1.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            spinEdit1.Location = new Point(123, 9);
            spinEdit1.Name = "spinEdit1";
            spinEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            spinEdit1.Size = new Size(90, 20);
            spinEdit1.TabIndex = 4;
            spinEdit1.EditValueChanged += spinEdit1_EditValueChanged;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(7, 16);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(30, 13);
            labelControl1.TabIndex = 2;
            labelControl1.Text = "Tahun";
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
            gridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(714, 339);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowFooter = true;
            gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // ucLabaRugi
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucLabaRugi";
            Size = new Size(714, 412);
            Load += ucLabaRugi_Load;
            sidePanel1.ResumeLayout(false);
            sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)spinEdit1.Properties).EndInit();
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
        private DevExpress.XtraEditors.SpinEdit spinEdit1;
        private DevExpress.XtraEditors.SimpleButton sbcetak;
    }
}
