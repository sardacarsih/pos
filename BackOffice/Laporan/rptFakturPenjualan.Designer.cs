using BackOffice.Model;

namespace BackOffice.Laporan
{
    partial class rptFakturPenjualan
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

        #region Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelKembalian = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBayar = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.objectDataPenjualan = new DevExpress.DataAccess.ObjectBinding.ObjectDataSource(this.components);
            this.Bayar = new DevExpress.XtraReports.Parameters.Parameter();
            this.Kembalian = new DevExpress.XtraReports.Parameters.Parameter();
            this.Nama_Toko = new DevExpress.XtraReports.Parameters.Parameter();
            this.Jenis_Bayar = new DevExpress.XtraReports.Parameters.Parameter();
            this.NoFaktur = new DevExpress.XtraReports.Parameters.Parameter();
            this.Waktu = new DevExpress.XtraReports.Parameters.Parameter();
            this.Pelanggan = new DevExpress.XtraReports.Parameters.Parameter();
            this.Kasir = new DevExpress.XtraReports.Parameters.Parameter();
            this.Potongan = new DevExpress.XtraReports.Parameters.Parameter();
            this.Tenor = new DevExpress.XtraReports.Parameters.Parameter();
            this.Bruto = new DevExpress.XtraReports.Parameters.Parameter();
            this.Total = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.objectDataPenjualan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // TopMargin
            // 
            this.TopMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel14,
            this.xrLabel13,
            this.xrLabel12,
            this.xrLabel11,
            this.xrLabel10});
            this.TopMargin.HeightF = 83F;
            this.TopMargin.Name = "TopMargin";
            // 
            // xrLabel14
            // 
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(?Jenis_Bayar=\'TUNAI\',?Jenis_Bayar ,Concat(?Jenis_Bayar,\' Pot:\',?Tenor,\' Kali\'" +
                    " ) )")});
            this.xrLabel14.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 67.87499F);
            this.xrLabel14.Multiline = true;
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.SizeF = new System.Drawing.SizeF(256F, 15F);
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.Text = "xrLabel14";
            // 
            // xrLabel13
            // 
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Waktu")});
            this.xrLabel13.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(126.6667F, 51.87498F);
            this.xrLabel13.Multiline = true;
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(139.3334F, 15F);
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.Text = "xrLabel13";
            // 
            // xrLabel12
            // 
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Pelanggan")});
            this.xrLabel12.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(9.999983F, 27.87498F);
            this.xrLabel12.Multiline = true;
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(4, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(256F, 20F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UsePadding = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "xrLabel12";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel11
            // 
            this.xrLabel11.AutoWidth = true;
            this.xrLabel11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?NoFaktur")});
            this.xrLabel11.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 51.87499F);
            this.xrLabel11.Multiline = true;
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(4, 4, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(116.6667F, 15F);
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UsePadding = false;
            this.xrLabel11.Text = "xrLabel11";
            // 
            // xrLabel10
            // 
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Nama_Toko")});
            this.xrLabel10.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 4.874981F);
            this.xrLabel10.Multiline = true;
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(256F, 23F);
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.Text = "xrLabel10";
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel15
            // 
            this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Kasir")});
            this.xrLabel15.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 20F);
            this.xrLabel15.Multiline = true;
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(100F, 20F);
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.Text = "xrLabel15";
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel15,
            this.xrLabel9,
            this.xrLabel8,
            this.xrLabelKembalian,
            this.xrLabelBayar,
            this.xrLabel4});
            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // xrLabel9
            // 
            this.xrLabel9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Kembalian"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "?Jenis_Bayar=\'TUNAI\'")});
            this.xrLabel9.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(179.7917F, 40F);
            this.xrLabel9.Multiline = true;
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(86.21F, 20F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.Text = "xrLabel9";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabel9.TextFormatString = "{0:N0}";
            // 
            // xrLabel8
            // 
            this.xrLabel8.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?Bayar"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "?Jenis_Bayar=\'TUNAI\'\n")});
            this.xrLabel8.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(179.7917F, 19.99999F);
            this.xrLabel8.Multiline = true;
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(86.21F, 20F);
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.Text = "xrLabel8";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabel8.TextFormatString = "{0:N0}";
            // 
            // xrLabelKembalian
            // 
            this.xrLabelKembalian.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "?Jenis_Bayar=\'TUNAI\'")});
            this.xrLabelKembalian.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabelKembalian.LocationFloat = new DevExpress.Utils.PointFloat(110F, 40F);
            this.xrLabelKembalian.Multiline = true;
            this.xrLabelKembalian.Name = "xrLabelKembalian";
            this.xrLabelKembalian.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabelKembalian.SizeF = new System.Drawing.SizeF(69.79F, 20F);
            this.xrLabelKembalian.StylePriority.UseFont = false;
            this.xrLabelKembalian.Text = "Kembali";
            // 
            // xrLabelBayar
            // 
            this.xrLabelBayar.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "?Jenis_Bayar=\'TUNAI\'")});
            this.xrLabelBayar.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabelBayar.LocationFloat = new DevExpress.Utils.PointFloat(110F, 19.99999F);
            this.xrLabelBayar.Multiline = true;
            this.xrLabelBayar.Name = "xrLabelBayar";
            this.xrLabelBayar.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabelBayar.SizeF = new System.Drawing.SizeF(69.79F, 20F);
            this.xrLabelBayar.StylePriority.UseFont = false;
            this.xrLabelBayar.Text = "Bayar";
            // 
            // xrLabel4
            // 
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Concat(\'Bruto : \',FormatString(\'{0:n0}\',?Bruto ),\n\',Potongan : \',FormatString(\'{0" +
                    ":n0}\',?Potongan ),\n\',Total : \' ,FormatString(\'{0:n0}\',?Total ))")});
            this.xrLabel4.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Regular, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 0F);
            this.xrLabel4.Multiline = true;
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(256F, 20F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "Bruto";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel7,
            this.xrLabel2});
            this.Detail.HeightF = 20F;
            this.Detail.Name = "Detail";
            // 
            // xrLabel7
            // 
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Concat([NAMA_BARANG],\' \',[JUMLAH_BARANG],\'@\',FormatString(\'{0:n0}\',[HARGA_BARANG]" +
                    " ))")});
            this.xrLabel7.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 0F);
            this.xrLabel7.Multiline = true;
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(199.5416F, 20F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.Text = "xrLabel7";
            // 
            // xrLabel2
            // 
            this.xrLabel2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TOTAL_HARGA]")});
            this.xrLabel2.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(209.5416F, 0F);
            this.xrLabel2.Multiline = true;
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(56.45842F, 20F);
            this.xrLabel2.StylePriority.UseBorders = false;
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UsePadding = false;
            this.xrLabel2.Text = "xrLabel2";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabel2.TextFormatString = "{0:N0}";
           
            // objectDataPenjualan
            // 
            this.objectDataPenjualan.DataSourceType = null;
            this.objectDataPenjualan.Name = "objectDataPenjualan";
            // 
            // Bayar
            // 
            this.Bayar.Description = "Bayar";
            this.Bayar.Name = "Bayar";
            this.Bayar.Type = typeof(int);
            this.Bayar.ValueInfo = "0";
            this.Bayar.Visible = false;
            // 
            // Kembalian
            // 
            this.Kembalian.Description = "Kembalian";
            this.Kembalian.Name = "Kembalian";
            this.Kembalian.Type = typeof(int);
            this.Kembalian.ValueInfo = "0";
            this.Kembalian.Visible = false;
            // 
            // Nama_Toko
            // 
            this.Nama_Toko.Description = "Nama_Toko";
            this.Nama_Toko.Name = "Nama_Toko";
            this.Nama_Toko.Visible = false;
            // 
            // Jenis_Bayar
            // 
            this.Jenis_Bayar.Description = "Jenis_Bayar";
            this.Jenis_Bayar.Name = "Jenis_Bayar";
            this.Jenis_Bayar.Visible = false;
            // 
            // NoFaktur
            // 
            this.NoFaktur.Description = "NoFaktur";
            this.NoFaktur.Name = "NoFaktur";
            this.NoFaktur.Visible = false;
            // 
            // Waktu
            // 
            this.Waktu.Description = "Waktu";
            this.Waktu.Name = "Waktu";
            this.Waktu.Visible = false;
            // 
            // Pelanggan
            // 
            this.Pelanggan.Description = "Pelanggan";
            this.Pelanggan.Name = "Pelanggan";
            this.Pelanggan.Visible = false;
            // 
            // Kasir
            // 
            this.Kasir.Description = "Kasir";
            this.Kasir.Name = "Kasir";
            this.Kasir.Visible = false;
            // 
            // Potongan
            // 
            this.Potongan.Description = "Potongan";
            this.Potongan.Name = "Potongan";
            this.Potongan.Type = typeof(int);
            this.Potongan.ValueInfo = "0";
            this.Potongan.Visible = false;
            // 
            // Tenor
            // 
            this.Tenor.Description = "Tenor";
            this.Tenor.Name = "Tenor";
            this.Tenor.Type = typeof(int);
            this.Tenor.ValueInfo = "0";
            this.Tenor.Visible = false;
            // 
            // Bruto
            // 
            this.Bruto.Description = "Bruto";
            this.Bruto.Name = "Bruto";
            this.Bruto.Type = typeof(decimal);
            this.Bruto.ValueInfo = "0";
            this.Bruto.Visible = false;
            // 
            // Total
            // 
            this.Total.Description = "Total";
            this.Total.Name = "Total";
            this.Total.Type = typeof(decimal);
            this.Total.ValueInfo = "0";
            this.Total.Visible = false;
            // 
            // rptFakturPenjualan
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.Detail});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.objectDataPenjualan});
            this.DataSource = this.objectDataPenjualan;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(15F, 8F, 83F, 60F);
            this.PageHeight = 1169;
            this.PageWidth = 299;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            this.PaperName = "Roll Paper 76 x 297 mm";
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.Bayar,
            this.Kembalian,
            this.Nama_Toko,
            this.Jenis_Bayar,
            this.NoFaktur,
            this.Waktu,
            this.Pelanggan,
            this.Kasir,
            this.Potongan,
            this.Tenor,
            this.Bruto,
            this.Total});
            this.PrinterName = "EPSON TM-U220 Receipt";
            this.RollPaper = true;
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this.objectDataPenjualan)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.DataAccess.ObjectBinding.ObjectDataSource objectDataPenjualan;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabelKembalian;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBayar;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.Parameters.Parameter Bayar;
        private DevExpress.XtraReports.Parameters.Parameter Kembalian;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.Parameters.Parameter Nama_Toko;
        private DevExpress.XtraReports.Parameters.Parameter Jenis_Bayar;
        private DevExpress.XtraReports.Parameters.Parameter NoFaktur;
        private DevExpress.XtraReports.Parameters.Parameter Waktu;
        private DevExpress.XtraReports.Parameters.Parameter Pelanggan;
        private DevExpress.XtraReports.Parameters.Parameter Kasir;
        private DevExpress.XtraReports.Parameters.Parameter Potongan;
        private DevExpress.XtraReports.Parameters.Parameter Tenor;
        private DevExpress.XtraReports.Parameters.Parameter Bruto;
        private DevExpress.XtraReports.Parameters.Parameter Total;
    }
}
