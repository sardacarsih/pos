namespace BackOffice.UC
{
    partial class ucPeriode
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
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            setahun = new DevExpress.XtraEditors.SpinEdit();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).BeginInit();
            sidePanel2.SuspendLayout();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(634, 308);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(setahun);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(634, 44);
            sidePanel1.TabIndex = 1;
            sidePanel1.Text = "sidePanel1";
            // 
            // setahun
            // 
            setahun.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            setahun.Location = new Point(22, 12);
            setahun.Name = "setahun";
            setahun.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            setahun.Size = new Size(100, 20);
            setahun.TabIndex = 2;
            // 
            // sidePanel2
            // 
            sidePanel2.Controls.Add(gridControl1);
            sidePanel2.Dock = DockStyle.Fill;
            sidePanel2.Location = new Point(0, 44);
            sidePanel2.Name = "sidePanel2";
            sidePanel2.Size = new Size(634, 308);
            sidePanel2.TabIndex = 2;
            sidePanel2.Text = "sidePanel2";
            // 
            // ucPeriode
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucPeriode";
            Size = new Size(634, 352);
            Load += ucPeriode_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            sidePanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).EndInit();
            sidePanel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.SpinEdit setahun;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
    }
}
