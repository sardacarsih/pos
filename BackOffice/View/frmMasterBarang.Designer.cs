namespace BackOffice.View
{
    partial class frmMasterBarang
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMasterBarang));
            txtkode_barang = new DevExpress.XtraEditors.TextEdit();
            txtnama_barang = new DevExpress.XtraEditors.TextEdit();
            txtbarkode = new DevExpress.XtraEditors.TextEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            labelControl5 = new DevExpress.XtraEditors.LabelControl();
            labelControl6 = new DevExpress.XtraEditors.LabelControl();
            txthargaBeli = new DevExpress.XtraEditors.TextEdit();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            bar2 = new DevExpress.XtraBars.Bar();
            bbisimpan = new DevExpress.XtraBars.BarLargeButtonItem();
            bbitutup = new DevExpress.XtraBars.BarLargeButtonItem();
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            lesatuan = new DevExpress.XtraEditors.LookUpEdit();
            checkEdit_nonaktif = new DevExpress.XtraEditors.CheckEdit();
            lblnonaktif = new DevExpress.XtraEditors.LabelControl();
            txthargaJual = new DevExpress.XtraEditors.TextEdit();
            labelControl8 = new DevExpress.XtraEditors.LabelControl();
            lookUpEdit1 = new DevExpress.XtraEditors.LookUpEdit();
            ((System.ComponentModel.ISupportInitialize)txtkode_barang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtnama_barang.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtbarkode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txthargaBeli.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lesatuan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)checkEdit_nonaktif.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txthargaJual.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lookUpEdit1.Properties).BeginInit();
            SuspendLayout();
            // 
            // txtkode_barang
            // 
            txtkode_barang.Location = new Point(109, 74);
            txtkode_barang.Name = "txtkode_barang";
            txtkode_barang.Properties.ReadOnly = true;
            txtkode_barang.Size = new Size(100, 20);
            txtkode_barang.TabIndex = 7;
            // 
            // txtnama_barang
            // 
            txtnama_barang.Location = new Point(109, 100);
            txtnama_barang.Name = "txtnama_barang";
            txtnama_barang.Size = new Size(208, 20);
            txtnama_barang.TabIndex = 0;
            txtnama_barang.KeyDown += txtnama_barang_KeyDown;
            // 
            // txtbarkode
            // 
            txtbarkode.Location = new Point(109, 152);
            txtbarkode.Name = "txtbarkode";
            txtbarkode.Size = new Size(208, 20);
            txtbarkode.TabIndex = 2;
            txtbarkode.KeyDown += txtbarkode_KeyDown;
            // 
            // labelControl1
            // 
            labelControl1.Location = new Point(12, 77);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(61, 13);
            labelControl1.TabIndex = 2;
            labelControl1.Text = "Kode Barang";
            // 
            // labelControl2
            // 
            labelControl2.Location = new Point(12, 103);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(64, 13);
            labelControl2.TabIndex = 2;
            labelControl2.Text = "Nama Barang";
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(12, 129);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(34, 13);
            labelControl3.TabIndex = 2;
            labelControl3.Text = "Satuan";
            // 
            // labelControl4
            // 
            labelControl4.Location = new Point(12, 155);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(39, 13);
            labelControl4.TabIndex = 2;
            labelControl4.Text = "Barkode";
            // 
            // labelControl5
            // 
            labelControl5.Location = new Point(12, 181);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(40, 13);
            labelControl5.TabIndex = 2;
            labelControl5.Text = "Kategori";
            // 
            // labelControl6
            // 
            labelControl6.Location = new Point(12, 214);
            labelControl6.Name = "labelControl6";
            labelControl6.Size = new Size(48, 13);
            labelControl6.TabIndex = 2;
            labelControl6.Text = "Harga Beli";
            // 
            // txthargaBeli
            // 
            txthargaBeli.Location = new Point(109, 211);
            txthargaBeli.Name = "txthargaBeli";
            txthargaBeli.Properties.BeepOnError = true;
            txthargaBeli.Properties.DisplayFormat.FormatString = "n";
            txthargaBeli.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txthargaBeli.Properties.EditFormat.FormatString = "n";
            txthargaBeli.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txthargaBeli.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txthargaBeli.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txthargaBeli.Properties.MaskSettings.Set("mask", "n");
            txthargaBeli.Properties.UseMaskAsDisplayFormat = true;
            txthargaBeli.Size = new Size(208, 20);
            txthargaBeli.TabIndex = 4;
            txthargaBeli.KeyDown += txthargaBeli_KeyDown;
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] { bar2 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            barManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bbisimpan, bbitutup });
            barManager1.MainMenu = bar2;
            barManager1.MaxItemId = 3;
            // 
            // bar2
            // 
            bar2.BarName = "Main menu";
            bar2.DockCol = 0;
            bar2.DockRow = 0;
            bar2.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            bar2.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] { new DevExpress.XtraBars.LinkPersistInfo(bbisimpan), new DevExpress.XtraBars.LinkPersistInfo(bbitutup) });
            bar2.OptionsBar.MultiLine = true;
            bar2.OptionsBar.UseWholeRow = true;
            bar2.Text = "Main menu";
            // 
            // bbisimpan
            // 
            bbisimpan.Caption = "Simpan";
            bbisimpan.Id = 0;
            bbisimpan.ImageOptions.Image = (Image)resources.GetObject("bbisimpan.ImageOptions.Image");
            bbisimpan.ImageOptions.LargeImage = (Image)resources.GetObject("bbisimpan.ImageOptions.LargeImage");
            bbisimpan.Name = "bbisimpan";
            bbisimpan.ItemClick += bbisimpan_ItemClick;
            // 
            // bbitutup
            // 
            bbitutup.Caption = "Tutup";
            bbitutup.Id = 1;
            bbitutup.ImageOptions.Image = (Image)resources.GetObject("bbitutup.ImageOptions.Image");
            bbitutup.ImageOptions.LargeImage = (Image)resources.GetObject("bbitutup.ImageOptions.LargeImage");
            bbitutup.Name = "bbitutup";
            bbitutup.ItemClick += bbitutup_ItemClick;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(339, 56);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 311);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(339, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 56);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 255);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(339, 56);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 255);
            // 
            // lesatuan
            // 
            lesatuan.Location = new Point(109, 126);
            lesatuan.Name = "lesatuan";
            lesatuan.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lesatuan.Size = new Size(208, 20);
            lesatuan.TabIndex = 1;
            lesatuan.KeyDown += lesatuan_KeyDown;
            // 
            // checkEdit_nonaktif
            // 
            checkEdit_nonaktif.Location = new Point(111, 273);
            checkEdit_nonaktif.MenuManager = barManager1;
            checkEdit_nonaktif.Name = "checkEdit_nonaktif";
            checkEdit_nonaktif.Properties.Caption = "";
            checkEdit_nonaktif.Properties.GlyphAlignment = DevExpress.Utils.HorzAlignment.Default;
            checkEdit_nonaktif.Size = new Size(75, 20);
            checkEdit_nonaktif.TabIndex = 6;
            // 
            // lblnonaktif
            // 
            lblnonaktif.Location = new Point(12, 276);
            lblnonaktif.Name = "lblnonaktif";
            lblnonaktif.Size = new Size(45, 13);
            lblnonaktif.TabIndex = 2;
            lblnonaktif.Text = "Non Aktif";
            // 
            // txthargaJual
            // 
            txthargaJual.Location = new Point(109, 247);
            txthargaJual.Name = "txthargaJual";
            txthargaJual.Properties.BeepOnError = true;
            txthargaJual.Properties.DisplayFormat.FormatString = "n";
            txthargaJual.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txthargaJual.Properties.EditFormat.FormatString = "n";
            txthargaJual.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            txthargaJual.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txthargaJual.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txthargaJual.Properties.MaskSettings.Set("mask", "n");
            txthargaJual.Properties.UseMaskAsDisplayFormat = true;
            txthargaJual.Size = new Size(208, 20);
            txthargaJual.TabIndex = 5;
            txthargaJual.KeyDown += txthargaJual_KeyDown;
            // 
            // labelControl8
            // 
            labelControl8.Location = new Point(12, 250);
            labelControl8.Name = "labelControl8";
            labelControl8.Size = new Size(51, 13);
            labelControl8.TabIndex = 2;
            labelControl8.Text = "Harga Jual";
            // 
            // lookUpEdit1
            // 
            lookUpEdit1.Location = new Point(109, 178);
            lookUpEdit1.MenuManager = barManager1;
            lookUpEdit1.Name = "lookUpEdit1";
            lookUpEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lookUpEdit1.Size = new Size(207, 20);
            lookUpEdit1.TabIndex = 3;
            lookUpEdit1.KeyDown += lookUpEdit1_KeyDown;
            // 
            // frmMasterBarang
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(339, 311);
            ControlBox = false;
            Controls.Add(lookUpEdit1);
            Controls.Add(checkEdit_nonaktif);
            Controls.Add(lblnonaktif);
            Controls.Add(labelControl8);
            Controls.Add(labelControl6);
            Controls.Add(labelControl5);
            Controls.Add(labelControl4);
            Controls.Add(labelControl3);
            Controls.Add(labelControl2);
            Controls.Add(labelControl1);
            Controls.Add(lesatuan);
            Controls.Add(txthargaJual);
            Controls.Add(txthargaBeli);
            Controls.Add(txtbarkode);
            Controls.Add(txtnama_barang);
            Controls.Add(txtkode_barang);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Name = "frmMasterBarang";
            Text = "Tambah Barang";
            FormClosing += frmMasterBarang_FormClosing;
            Load += frmMasterBarang_Load;
            ((System.ComponentModel.ISupportInitialize)txtkode_barang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtnama_barang.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtbarkode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txthargaBeli.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)lesatuan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)checkEdit_nonaktif.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txthargaJual.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)lookUpEdit1.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.TextEdit txtkode_barang;
        private DevExpress.XtraEditors.TextEdit txtnama_barang;
        private DevExpress.XtraEditors.TextEdit txtbarkode;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.TextEdit txthargaBeli;
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.Bar bar2;
        private DevExpress.XtraBars.BarLargeButtonItem bbisimpan;
        private DevExpress.XtraBars.BarLargeButtonItem bbitutup;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraEditors.CheckEdit checkEdit_nonaktif;
        private DevExpress.XtraEditors.LabelControl lblnonaktif;
        private DevExpress.XtraEditors.LookUpEdit lesatuan;
        private DevExpress.XtraEditors.LabelControl labelControl8;
        private DevExpress.XtraEditors.TextEdit txthargaJual;
        private DevExpress.XtraEditors.LookUpEdit lookUpEdit1;
    }
}