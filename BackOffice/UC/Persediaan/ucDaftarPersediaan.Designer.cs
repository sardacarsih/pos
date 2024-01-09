namespace BackOffice.UC.Persediaan
{
    partial class ucDaftarPersediaan
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucDaftarPersediaan));
            sidePanel1 = new DevExpress.XtraEditors.SidePanel();
            sbexport = new DevExpress.XtraEditors.SimpleButton();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            dateEdit1 = new DevExpress.XtraEditors.DateEdit();
            radioGroup1 = new DevExpress.XtraEditors.RadioGroup();
            sidePanel2 = new DevExpress.XtraEditors.SidePanel();
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            imageCollection1 = new DevExpress.Utils.ImageCollection(components);
            sidePanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).BeginInit();
            sidePanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).BeginInit();
            SuspendLayout();
            // 
            // sidePanel1
            // 
            sidePanel1.Controls.Add(sbexport);
            sidePanel1.Controls.Add(labelControl1);
            sidePanel1.Controls.Add(dateEdit1);
            sidePanel1.Controls.Add(radioGroup1);
            sidePanel1.Dock = DockStyle.Top;
            sidePanel1.Location = new Point(0, 0);
            sidePanel1.Name = "sidePanel1";
            sidePanel1.Size = new Size(714, 40);
            sidePanel1.TabIndex = 0;
            // 
            // sbexport
            // 
            sbexport.Location = new Point(455, 9);
            sbexport.Name = "sbexport";
            sbexport.Size = new Size(75, 23);
            sbexport.TabIndex = 3;
            sbexport.Text = "Export";
            sbexport.Click += sbexport_Click;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(7, 16);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(38, 13);
            labelControl1.TabIndex = 2;
            labelControl1.Text = "Tanggal";
            // 
            // dateEdit1
            // 
            dateEdit1.EditValue = null;
            dateEdit1.Location = new Point(63, 13);
            dateEdit1.Name = "dateEdit1";
            dateEdit1.Properties.BeepOnError = true;
            dateEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit1.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateEdit1.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dateEdit1.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit1.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            dateEdit1.Properties.UseMaskAsDisplayFormat = true;
            dateEdit1.Size = new Size(120, 20);
            dateEdit1.TabIndex = 1;
            dateEdit1.EditValueChanged += dateEdit1_EditValueChanged;
            // 
            // radioGroup1
            // 
            radioGroup1.Location = new Point(201, 3);
            radioGroup1.Name = "radioGroup1";
            radioGroup1.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] { new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Semua Stock"), new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Stock ( Minus )") });
            radioGroup1.Size = new Size(238, 33);
            radioGroup1.TabIndex = 0;
            radioGroup1.SelectedIndexChanged += radioGroup1_SelectedIndexChanged;
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
            gridControl1.Size = new Size(714, 341);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";
            gridView1.OptionsView.ShowFooter = true;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.PopupMenuShowing += gridView1_PopupMenuShowing;
            // 
            // imageCollection1
            // 
            imageCollection1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("imageCollection1.ImageStream");
            imageCollection1.Images.SetKeyName(0, "editcontact_32x32.png");
            // 
            // ucDaftarPersediaan
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sidePanel2);
            Controls.Add(sidePanel1);
            Name = "ucDaftarPersediaan";
            Size = new Size(714, 412);
            Load += ucDaftarPersediaan_Load;
            sidePanel1.ResumeLayout(false);
            sidePanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).EndInit();
            sidePanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)imageCollection1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.SidePanel sidePanel1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.DateEdit dateEdit1;
        private DevExpress.XtraEditors.RadioGroup radioGroup1;
        private DevExpress.XtraEditors.SidePanel sidePanel2;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton sbexport;
        private DevExpress.Utils.ImageCollection imageCollection1;
    }
}
