
namespace BackOffice
{
    partial class FrmClosingYear
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
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            sbtutupbuku = new DevExpress.XtraEditors.SimpleButton();
            setahun = new DevExpress.XtraEditors.SpinEdit();
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).BeginInit();
            SuspendLayout();
            // 
            // labelControl3
            // 
            labelControl3.Appearance.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            labelControl3.Appearance.Options.UseFont = true;
            labelControl3.Location = new Point(20, 79);
            labelControl3.Margin = new Padding(2);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(35, 17);
            labelControl3.TabIndex = 9;
            labelControl3.Text = "Tahun";
            // 
            // labelControl1
            // 
            labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            labelControl1.Location = new Point(21, 8);
            labelControl1.Margin = new Padding(2);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(310, 26);
            labelControl1.TabIndex = 9;
            labelControl1.Text = "Proses ini diperlukan untuk menghitung saldo akhir persediaan barang dan memindahkannya ke awal tahun";
            // 
            // sbtutupbuku
            // 
            sbtutupbuku.Location = new Point(142, 150);
            sbtutupbuku.Margin = new Padding(2);
            sbtutupbuku.Name = "sbtutupbuku";
            sbtutupbuku.Size = new Size(75, 23);
            sbtutupbuku.TabIndex = 12;
            sbtutupbuku.Text = "Proses";
            sbtutupbuku.Click += sbtutupbuku_Click;
            // 
            // setahun
            // 
            setahun.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            setahun.Location = new Point(142, 79);
            setahun.Margin = new Padding(2);
            setahun.Name = "setahun";
            setahun.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            setahun.Properties.DisplayFormat.FormatString = "d";
            setahun.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            setahun.Properties.EditFormat.FormatString = "d";
            setahun.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            setahun.Properties.MaskSettings.Set("mask", "d");
            setahun.Size = new Size(85, 20);
            setahun.TabIndex = 11;
            // 
            // FrmClosingYear
            // 
            Appearance.Options.UseFont = true;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(370, 249);
            Controls.Add(sbtutupbuku);
            Controls.Add(setahun);
            Controls.Add(labelControl3);
            Controls.Add(labelControl1);
            Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Margin = new Padding(1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmClosingYear";
            Text = "Tutup Buku  Tahun";
            Load += FrmClosingYear_Load;
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.SpinEdit setahun;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton sbtutupbuku;
    }
}