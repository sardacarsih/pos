using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace BackOffice.UC.Persediaan
{
    public partial class ucKartuStock : UserControl
    {
        PersediaanController controller = new();
        List<DTOKartuStok> KartuStockBarang;
        public ucKartuStock()
        {
            InitializeComponent();
        }



        private static void ExportToExcel(List<DTOStoctOpnameMaster> stockOpnameMasterList, string filePath)
        {
            // Set the license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("StockOpname");

            // Header for Master
            worksheet.Cells[1, 1].Value = "BULAN";
            worksheet.Cells[1, 2].Value = "NOMOR_SO";
            worksheet.Cells[1, 3].Value = "TANGGAL";
            worksheet.Cells[1, 4].Value = "TOTAL";


            // Header for Detail
            worksheet.Cells[1, 4].Value = "NOMOR_SO_DETAIL";
            worksheet.Cells[1, 5].Value = "KODE_BARANG";
            worksheet.Cells[1, 6].Value = "PRODUCTNAME";
            worksheet.Cells[1, 7].Value = "JUMLAHSISTEM";
            worksheet.Cells[1, 8].Value = "JUMLAHFISIK";
            worksheet.Cells[1, 9].Value = "SELISIH";
            worksheet.Cells[1, 10].Value = "HPP";
            worksheet.Cells[1, 11].Value = "TOTAL";

            // Populate data
            int row = 2;
            foreach (var master in stockOpnameMasterList)
            {
                worksheet.Cells[row, 1].Value = master.BULAN;
                worksheet.Cells[row, 2].Value = master.NOMOR_SO;
                worksheet.Cells[row, 3].Value = master.TANGGAL;
                worksheet.Cells[row, 3].Value = master.TOTAL;

                foreach (var detail in master.Details)
                {
                    worksheet.Cells[row, 4].Value = detail.NOMOR_SO;
                    worksheet.Cells[row, 5].Value = detail.KODE_BARANG;
                    worksheet.Cells[row, 6].Value = detail.PRODUCTNAME;
                    worksheet.Cells[row, 7].Value = detail.JUMLAHSISTEM;
                    worksheet.Cells[row, 8].Value = detail.JUMLAHFISIK;
                    worksheet.Cells[row, 9].Value = detail.SELISIH;
                    worksheet.Cells[row, 10].Value = detail.HPP;
                    worksheet.Cells[row, 11].Value = detail.TOTAL;

                    row++;
                }
            }

            // Save the file
            FileInfo excelFile = new(filePath);
            package.SaveAs(excelFile);
        }

        private void sbcetak_Click(object sender, EventArgs e)
        {
            rptkartustock report = new()
            {
                DataSource = KartuStockBarang,
                ShowPrintMarginsWarning = false
            };
            report.Parameters["NAMA"].Value = searchLookUpEdit1.Text + " (Periode : " + comboBoxEditBulan.Text +" "+setahun.Value+ " )";
            ReportPrintTool tool = new(report);
            tool.ShowPreview();

        }

        private void spinEdit1_EditValueChanged(object sender, EventArgs e)
        {
            load_periode();
        }

        private void ucKartuStock_Load(object sender, EventArgs e)
        {
            string[] daftarBulan =
            {
                "Januari", "Februari", "Maret", "April", "Mei", "Juni",
                "Juli", "Agustus", "September", "Oktober", "November", "Desember"
            };

            // Tetapkan array sebagai DataSource untuk ComboBoxEdit
            comboBoxEditBulan.Properties.Items.AddRange(daftarBulan);
            comboBoxEditBulan.SelectedIndex = 0;
            setahun.EditValue = DateTime.Today.Year;
            Load_Daftar_Barang();
        }

        private void Load_Daftar_Barang()
        {

            searchLookUpEdit1.Properties.DataSource = controller.FillDictionaryFromDatabase();
            searchLookUpEdit1.Properties.DisplayMember = "NAMA";
            searchLookUpEdit1.Properties.ValueMember = "KODE";
        }
        DateTime STARTDATE, ENDDATE;
        private void comboBoxEditBulan_SelectedIndexChanged(object sender, EventArgs e)
        {
            load_periode();
        }

        private void load_periode()
        {
            if (comboBoxEditBulan.SelectedIndex != -1 && setahun.Value != 0)
            {

                var BULAN = comboBoxEditBulan.SelectedIndex + 1;
                var TAHUN = (int)setahun.Value;


                STARTDATE = new DateTime(TAHUN, BULAN, 1);
                ENDDATE = new DateTime(TAHUN, BULAN, DateTime.DaysInMonth(TAHUN, BULAN));
                if (searchLookUpEdit1.EditValue != null)
                {
                    var KODE = searchLookUpEdit1.EditValue.ToString();
                    KartuStockBarang= controller.KartuStokBarang(KODE, STARTDATE, ENDDATE);
                    gridControl1.DataSource = KartuStockBarang;
                }

            }

        }

        private void searchLookUpEdit1_EditValueChanged(object sender, EventArgs e)
        {
            if(searchLookUpEdit1.EditValue!=null)
            {
                load_periode();
            }
        }
    }
}
