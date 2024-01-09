namespace BackOffice.UC
{
    partial class ucAnalisaLabaRugi
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucAnalisaLabaRugi));
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            sbrefresh = new DevExpress.XtraEditors.SimpleButton();
            sbexport = new DevExpress.XtraEditors.SimpleButton();
            sbcetak = new DevExpress.XtraEditors.SimpleButton();
            dateEdit2 = new DevExpress.XtraEditors.DateEdit();
            dateEdit1 = new DevExpress.XtraEditors.DateEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            imageCollection1 = new DevExpress.Utils.ImageCollection(components);
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dateEdit2.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit2.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties.CalendarTimeProperties).BeginInit();
            sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).BeginInit();
            SuspendLayout();
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(sbrefresh);
            sidePanel1.Controls.Add(sbexport);
            sidePanel1.Controls.Add(sbcetak);
            sidePanel1.Controls.Add(dateEdit2);
            sidePanel1.Controls.Add(dateEdit1);
            sidePanel1.Controls.Add(labelControl1);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(801, 42);
            sidePanel1.TabIndex = 1;
            sidePanel1.Text = "sidePanel1";
            // 
            // sbrefresh
            // 
            sbrefresh.ImageOptions.Image = (Image)resources.GetObject("sbrefresh.ImageOptions.Image");
            sbrefresh.Location = new Point(279, 5);
            sbrefresh.Name = "sbrefresh";
            sbrefresh.Size = new Size(75, 23);
            sbrefresh.TabIndex = 5;
            sbrefresh.Text = "Refresh";
            sbrefresh.ToolTip = "Refresh Data Penjualan";
            sbrefresh.Click += sbrefresh_Click;
            // 
            // sbexport
            // 
            sbexport.ImageOptions.Image = (Image)resources.GetObject("sbexport.ImageOptions.Image");
            sbexport.Location = new Point(714, 8);
            sbexport.Name = "sbexport";
            sbexport.Size = new Size(75, 23);
            sbexport.TabIndex = 3;
            sbexport.Text = "Export";
            sbexport.Click += sbexport_Click;
            // 
            // sbcetak
            // 
            sbcetak.ImageOptions.Image = (Image)resources.GetObject("sbcetak.ImageOptions.Image");
            sbcetak.Location = new Point(633, 8);
            sbcetak.Name = "sbcetak";
            sbcetak.Size = new Size(75, 23);
            sbcetak.TabIndex = 3;
            sbcetak.Text = "Preview";
            sbcetak.Click += sbcetak_Click;
            // 
            // dateEdit2
            // 
            dateEdit2.EditValue = null;
            dateEdit2.Location = new Point(176, 5);
            dateEdit2.Name = "dateEdit2";
            dateEdit2.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit2.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit2.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dateEdit2.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit2.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            dateEdit2.Size = new Size(100, 20);
            dateEdit2.TabIndex = 1;
            dateEdit2.EditValueChanged += dateEdit2_EditValueChanged;
            // 
            // dateEdit1
            // 
            dateEdit1.EditValue = null;
            dateEdit1.Location = new Point(70, 5);
            dateEdit1.Name = "dateEdit1";
            dateEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit1.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit1.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dateEdit1.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit1.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            dateEdit1.Size = new Size(100, 20);
            dateEdit1.TabIndex = 1;
            dateEdit1.EditValueChanged += dateEdit1_EditValueChanged;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(15, 12);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(38, 13);
            labelControl1.TabIndex = 0;
            labelControl1.Text = "Tanggal";
            // 
            // sidePanel2
            // 
            sidePanel2.Controls.Add(gridControl1);
            sidePanel2.Dock = DockStyle.Fill;
            sidePanel2.Location = new Point(0, 42);
            sidePanel2.Name = "sidePanel2";
            sidePanel2.Size = new Size(801, 400);
            sidePanel2.TabIndex = 2;
            sidePanel2.Text = "sidePanel2";
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.Size = new Size(801, 400);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsView.ShowGroupPanel = false;
            // imageCollection1
            // 
            imageCollection1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("imageCollection1.ImageStream");
            imageCollection1.Images.SetKeyName(0, "print_32x32.png");
            imageCollection1.Images.SetKeyName(1, "edit_32x32.png");
            imageCollection1.Images.SetKeyName(2, "cancel_32x32.png");
            // 
            // ucAnalisaLabaRugi
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucAnalisaLabaRugi";
            Size = new Size(801, 442);
            Load += ucAnalisaLabaRugi_Load;
            sidePanel1.ResumeLayout(false);
            sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dateEdit2.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit2.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties).EndInit();
            sidePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.SimpleButton sbcetak;
        private DevExpress.XtraEditors.DateEdit dateEdit2;
        private DevExpress.XtraEditors.DateEdit dateEdit1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton sbexport;
        private DevExpress.Utils.ImageCollection imageCollection1;
        private DevExpress.XtraEditors.SimpleButton sbrefresh;
    }
}
