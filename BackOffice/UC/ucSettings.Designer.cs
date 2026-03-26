namespace BackOffice.UC
{
    partial class ucSettings
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
            repositoryItemToggleSwitch1 = new DevExpress.XtraEditors.Repository.RepositoryItemToggleSwitch();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemToggleSwitch1).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Location = new Point(3, 43);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemToggleSwitch1 });
            gridControl1.Size = new Size(377, 257);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.RowUpdated += gridView1_RowUpdated;
            gridView1.ValidatingEditor += gridView1_ValidatingEditor;
            // 
            // repositoryItemToggleSwitch1
            // 
            repositoryItemToggleSwitch1.AutoHeight = false;
            repositoryItemToggleSwitch1.Name = "repositoryItemToggleSwitch1";
            repositoryItemToggleSwitch1.OffText = "Off";
            repositoryItemToggleSwitch1.OnText = "On";
            repositoryItemToggleSwitch1.ValueOff = "T";
            repositoryItemToggleSwitch1.ValueOn = "Y";
            // 
            // labelControl2
            // 
            labelControl2.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            labelControl2.Appearance.Options.UseFont = true;
            labelControl2.Location = new Point(3, 3);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(239, 25);
            labelControl2.TabIndex = 6;
            labelControl2.Text = "Pengaturan Parameter";
            // 
            // ucSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(labelControl2);
            Controls.Add(gridControl1);
            Name = "ucSettings";
            Size = new Size(401, 603);
            Load += ucSettings_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemToggleSwitch1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.Repository.RepositoryItemToggleSwitch repositoryItemToggleSwitch1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
    }
}
