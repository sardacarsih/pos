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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucPeriode));
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            COLPERIODE = new DevExpress.XtraGrid.Columns.GridColumn();
            COLBULAN = new DevExpress.XtraGrid.Columns.GridColumn();
            COLREMISE1 = new DevExpress.XtraGrid.Columns.GridColumn();
            repositoryItemToggleSwitch1 = new DevExpress.XtraEditors.Repository.RepositoryItemToggleSwitch();
            gridColumn1 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn2 = new DevExpress.XtraGrid.Columns.GridColumn();
            COLREMISE2 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn3 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn4 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn7 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn5 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn6 = new DevExpress.XtraGrid.Columns.GridColumn();
            setahu = new DevExpress.XtraEditors.SpinEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemToggleSwitch1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)setahu.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureEdit1.Properties).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Location = new Point(3, 77);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemToggleSwitch1 });
            gridControl1.Size = new Size(1091, 323);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { COLPERIODE, COLBULAN, COLREMISE1, gridColumn1, gridColumn2, COLREMISE2, gridColumn3, gridColumn4, gridColumn7, gridColumn5, gridColumn6 });
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.RowUpdated += gridView1_RowUpdated;
            // 
            // COLPERIODE
            // 
            COLPERIODE.Caption = "PERIODE";
            COLPERIODE.FieldName = "PERIODE";
            COLPERIODE.Name = "COLPERIODE";
            // 
            // COLBULAN
            // 
            COLBULAN.Caption = "BULAN";
            COLBULAN.FieldName = "BULAN";
            COLBULAN.Name = "COLBULAN";
            COLBULAN.OptionsColumn.AllowEdit = false;
            COLBULAN.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            COLBULAN.Visible = true;
            COLBULAN.VisibleIndex = 0;
            // 
            // COLREMISE1
            // 
            COLREMISE1.Caption = "REMISE1";
            COLREMISE1.ColumnEdit = repositoryItemToggleSwitch1;
            COLREMISE1.FieldName = "REMISE1";
            COLREMISE1.Name = "COLREMISE1";
            COLREMISE1.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            COLREMISE1.Visible = true;
            COLREMISE1.VisibleIndex = 1;
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
            // gridColumn1
            // 
            gridColumn1.Caption = "DARI";
            gridColumn1.FieldName = "R1DARI";
            gridColumn1.Name = "gridColumn1";
            gridColumn1.OptionsColumn.AllowEdit = false;
            gridColumn1.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridColumn1.Visible = true;
            gridColumn1.VisibleIndex = 2;
            // 
            // gridColumn2
            // 
            gridColumn2.Caption = "SAMPAI";
            gridColumn2.FieldName = "R1SAMPAI";
            gridColumn2.Name = "gridColumn2";
            gridColumn2.OptionsColumn.AllowEdit = false;
            gridColumn2.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridColumn2.Visible = true;
            gridColumn2.VisibleIndex = 3;
            // 
            // COLREMISE2
            // 
            COLREMISE2.Caption = "REMISE2";
            COLREMISE2.ColumnEdit = repositoryItemToggleSwitch1;
            COLREMISE2.FieldName = "REMISE2";
            COLREMISE2.Name = "COLREMISE2";
            COLREMISE2.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            COLREMISE2.Visible = true;
            COLREMISE2.VisibleIndex = 4;
            // 
            // gridColumn3
            // 
            gridColumn3.Caption = "DARI";
            gridColumn3.FieldName = "R2DARI";
            gridColumn3.Name = "gridColumn3";
            gridColumn3.OptionsColumn.AllowEdit = false;
            gridColumn3.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridColumn3.Visible = true;
            gridColumn3.VisibleIndex = 5;
            // 
            // gridColumn4
            // 
            gridColumn4.Caption = "SAMPAI";
            gridColumn4.FieldName = "R2SAMPAI";
            gridColumn4.Name = "gridColumn4";
            gridColumn4.OptionsColumn.AllowEdit = false;
            gridColumn4.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridColumn4.Visible = true;
            gridColumn4.VisibleIndex = 6;
            // 
            // gridColumn7
            // 
            gridColumn7.Caption = "BULANAN";
            gridColumn7.ColumnEdit = repositoryItemToggleSwitch1;
            gridColumn7.FieldName = "BULANAN";
            gridColumn7.Name = "gridColumn7";
            gridColumn7.Visible = true;
            gridColumn7.VisibleIndex = 7;
            // 
            // gridColumn5
            // 
            gridColumn5.Caption = "DARI";
            gridColumn5.FieldName = "BDARI";
            gridColumn5.Name = "gridColumn5";
            gridColumn5.OptionsColumn.AllowEdit = false;
            gridColumn5.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            gridColumn5.Visible = true;
            gridColumn5.VisibleIndex = 8;
            // 
            // gridColumn6
            // 
            gridColumn6.Caption = "SAMPAI";
            gridColumn6.FieldName = "BSAMPAI";
            gridColumn6.Name = "gridColumn6";
            gridColumn6.OptionsColumn.AllowEdit = false;
            gridColumn6.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.True;
            gridColumn6.Visible = true;
            gridColumn6.VisibleIndex = 9;
            // 
            // setahu
            // 
            setahu.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            setahu.Location = new Point(52, 51);
            setahu.Name = "setahu";
            setahu.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            setahu.Properties.MaskSettings.Set("mask", "d");
            setahu.Properties.UseMaskAsDisplayFormat = true;
            setahu.Size = new Size(72, 20);
            setahu.TabIndex = 5;
            setahu.EditValueChanged += setahu_EditValueChanged;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(3, 54);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(30, 13);
            labelControl1.TabIndex = 6;
            labelControl1.Text = "Tahun";
            // 
            // labelControl2
            // 
            labelControl2.Appearance.Font = new Font("Tahoma", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            labelControl2.Appearance.Options.UseFont = true;
            labelControl2.Location = new Point(3, 3);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(212, 25);
            labelControl2.TabIndex = 6;
            labelControl2.Text = "Periode Tutup Buku.";
            // 
            // pictureEdit1
            // 
            pictureEdit1.EditValue = resources.GetObject("pictureEdit1.EditValue");
            pictureEdit1.Location = new Point(14, 425);
            pictureEdit1.Name = "pictureEdit1";
            pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            pictureEdit1.Size = new Size(163, 62);
            pictureEdit1.TabIndex = 11;
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(5, 406);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(114, 13);
            labelControl3.TabIndex = 6;
            labelControl3.Text = "Keterangan Status Lock";
            // 
            // ucPeriode
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pictureEdit1);
            Controls.Add(labelControl2);
            Controls.Add(labelControl3);
            Controls.Add(labelControl1);
            Controls.Add(setahu);
            Controls.Add(gridControl1);
            Name = "ucPeriode";
            Size = new Size(1117, 500);
            Load += ucPeriode_Load;
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemToggleSwitch1).EndInit();
            ((System.ComponentModel.ISupportInitialize)setahu.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureEdit1.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn COLPERIODE;
        private DevExpress.XtraGrid.Columns.GridColumn COLBULAN;
        private DevExpress.XtraGrid.Columns.GridColumn COLREMISE1;
        private DevExpress.XtraEditors.Repository.RepositoryItemToggleSwitch repositoryItemToggleSwitch1;
        private DevExpress.XtraGrid.Columns.GridColumn COLREMISE2;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SpinEdit setahu;
        private DevExpress.XtraEditors.PictureEdit pictureEdit1;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn5;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn6;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn7;
    }
}
