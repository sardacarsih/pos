namespace BackOffice.View
{
    partial class rptparameter
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
            sbcetak = new DevExpress.XtraEditors.SimpleButton();
            radioGroup1 = new DevExpress.XtraEditors.RadioGroup();
            simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).BeginInit();
            SuspendLayout();
            // 
            // sbcetak
            // 
            sbcetak.Location = new Point(144, 154);
            sbcetak.Name = "sbcetak";
            sbcetak.Size = new Size(75, 23);
            sbcetak.TabIndex = 0;
            sbcetak.Text = "Preview";
            sbcetak.Click += sbcetak_Click;
            // 
            // radioGroup1
            // 
            radioGroup1.Location = new Point(37, 12);
            radioGroup1.Name = "radioGroup1";
            radioGroup1.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] { new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Daftar Tagihan Pinjaman"), new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "Daftar Tagihan Kredit Barang") });
            radioGroup1.Size = new Size(220, 111);
            radioGroup1.TabIndex = 1;
            // 
            // simpleButton1
            // 
            simpleButton1.Location = new Point(53, 154);
            simpleButton1.Name = "simpleButton1";
            simpleButton1.Size = new Size(75, 23);
            simpleButton1.TabIndex = 0;
            simpleButton1.Text = "Tutup";
            simpleButton1.Click += simpleButton1_Click;
            // 
            // rptparameter
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(299, 213);
            Controls.Add(radioGroup1);
            Controls.Add(simpleButton1);
            Controls.Add(sbcetak);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "rptparameter";
            Text = "Laporan Detail";
            ((System.ComponentModel.ISupportInitialize)radioGroup1.Properties).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.SimpleButton sbcetak;
        private DevExpress.XtraEditors.RadioGroup radioGroup1;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
    }
}