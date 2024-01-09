namespace BackOffice
{
    partial class frmfixedform
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SBFixed = new DevExpress.XtraEditors.SimpleButton();
            this.sbbackup = new DevExpress.XtraEditors.SimpleButton();
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.btnimport = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // SBFixed
            // 
            this.SBFixed.Location = new System.Drawing.Point(23, 28);
            this.SBFixed.Name = "SBFixed";
            this.SBFixed.Size = new System.Drawing.Size(75, 23);
            this.SBFixed.TabIndex = 0;
            this.SBFixed.Text = "Fixed";
            this.SBFixed.Click += new System.EventHandler(this.SBFixed_Click);
            // 
            // sbbackup
            // 
            this.sbbackup.Location = new System.Drawing.Point(23, 57);
            this.sbbackup.Name = "sbbackup";
            this.sbbackup.Size = new System.Drawing.Size(75, 23);
            this.sbbackup.TabIndex = 0;
            this.sbbackup.Text = "Backup";
            this.sbbackup.Click += new System.EventHandler(this.sbbackup_Click);
            // 
            // gridControl1
            // 
            this.gridControl1.Location = new System.Drawing.Point(23, 86);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(899, 312);
            this.gridControl1.TabIndex = 1;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            // 
            // btnimport
            // 
            this.btnimport.Location = new System.Drawing.Point(122, 28);
            this.btnimport.Name = "btnimport";
            this.btnimport.Size = new System.Drawing.Size(75, 23);
            this.btnimport.TabIndex = 0;
            this.btnimport.Text = "Import";
            this.btnimport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // frmfixedform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 410);
            this.Controls.Add(this.gridControl1);
            this.Controls.Add(this.btnimport);
            this.Controls.Add(this.sbbackup);
            this.Controls.Add(this.SBFixed);
            this.Name = "frmfixedform";
            this.Text = "frmfixedform";
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SimpleButton SBFixed;
        private DevExpress.XtraEditors.SimpleButton sbbackup;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton btnimport;
    }
}