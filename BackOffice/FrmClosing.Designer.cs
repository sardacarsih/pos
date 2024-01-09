
namespace BackOffice
{
    partial class FrmClosing
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
            cmbbulan = new DevExpress.XtraEditors.ComboBoxEdit();
            comboBoxEdit_Remise = new DevExpress.XtraEditors.ComboBoxEdit();
            dedaritgl = new DevExpress.XtraEditors.DateEdit();
            desampaitgl = new DevExpress.XtraEditors.DateEdit();
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbbulan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_Remise.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dedaritgl.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dedaritgl.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)desampaitgl.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)desampaitgl.Properties.CalendarTimeProperties).BeginInit();
            SuspendLayout();
            // 
            // labelControl3
            // 
            labelControl3.Appearance.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            labelControl3.Appearance.Options.UseFont = true;
            labelControl3.Location = new Point(20, 58);
            labelControl3.Margin = new Padding(2);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(45, 17);
            labelControl3.TabIndex = 9;
            labelControl3.Text = "Periode";
            // 
            // labelControl1
            // 
            labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            labelControl1.Location = new Point(21, 8);
            labelControl1.Margin = new Padding(2);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(310, 13);
            labelControl1.TabIndex = 9;
            labelControl1.Text = "Proses ini diperlukan untuk menghitung semua tagihan";
            // 
            // sbtutupbuku
            // 
            sbtutupbuku.Location = new Point(142, 189);
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
            setahun.Location = new Point(246, 55);
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
            setahun.EditValueChanged += setahun_EditValueChanged;
            // 
            // cmbbulan
            // 
            cmbbulan.Location = new Point(88, 55);
            cmbbulan.Margin = new Padding(2);
            cmbbulan.Name = "cmbbulan";
            cmbbulan.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbbulan.Properties.ImmediatePopup = true;
            cmbbulan.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cmbbulan.Size = new Size(139, 20);
            cmbbulan.TabIndex = 10;
            cmbbulan.SelectedIndexChanged += cmbbulan_SelectedIndexChanged;
            // 
            // comboBoxEdit_Remise
            // 
            comboBoxEdit_Remise.Location = new Point(88, 89);
            comboBoxEdit_Remise.Margin = new Padding(2);
            comboBoxEdit_Remise.Name = "comboBoxEdit_Remise";
            comboBoxEdit_Remise.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            comboBoxEdit_Remise.Properties.ImmediatePopup = true;
            comboBoxEdit_Remise.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            comboBoxEdit_Remise.Size = new Size(139, 20);
            comboBoxEdit_Remise.TabIndex = 10;
            comboBoxEdit_Remise.SelectedIndexChanged += comboBoxEdit_Remise_SelectedIndexChanged;
            // 
            // dedaritgl
            // 
            dedaritgl.EditValue = null;
            dedaritgl.Location = new Point(88, 127);
            dedaritgl.Name = "dedaritgl";
            dedaritgl.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dedaritgl.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dedaritgl.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dedaritgl.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dedaritgl.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
            dedaritgl.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dedaritgl.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            dedaritgl.Size = new Size(100, 20);
            dedaritgl.TabIndex = 13;
            // 
            // desampaitgl
            // 
            desampaitgl.EditValue = null;
            desampaitgl.Location = new Point(203, 127);
            desampaitgl.Name = "desampaitgl";
            desampaitgl.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            desampaitgl.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            desampaitgl.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
            desampaitgl.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            desampaitgl.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
            desampaitgl.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            desampaitgl.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");
            desampaitgl.Size = new Size(100, 20);
            desampaitgl.TabIndex = 14;
            // 
            // FrmClosing
            // 
            Appearance.Options.UseFont = true;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(370, 249);
            Controls.Add(desampaitgl);
            Controls.Add(dedaritgl);
            Controls.Add(sbtutupbuku);
            Controls.Add(setahun);
            Controls.Add(comboBoxEdit_Remise);
            Controls.Add(cmbbulan);
            Controls.Add(labelControl3);
            Controls.Add(labelControl1);
            Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Margin = new Padding(1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmClosing";
            Text = "Tutup Buku ";
            Load += FrmClosing_Load;
            ((System.ComponentModel.ISupportInitialize)setahun.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbbulan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_Remise.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dedaritgl.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dedaritgl.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)desampaitgl.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)desampaitgl.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.SpinEdit setahun;
        private DevExpress.XtraEditors.ComboBoxEdit cmbbulan;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton sbtutupbuku;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit_Remise;
        private DevExpress.XtraEditors.DateEdit dedaritgl;
        private DevExpress.XtraEditors.DateEdit desampaitgl;
    }
}