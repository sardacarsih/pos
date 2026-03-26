using System;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BackOffice
{
    public class ExcelExporter
    {
        public static void AddWorksheet(ExcelPackage package, DataSet dataSet, string sheetName, string periode)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Determine the appropriate formatting method based on sheetName
            switch (sheetName)
            {
                case "Rekap Tagihan":
                    FormatWorksheetRekapTagihan(worksheet, dataSet.Tables[0], periode);
                    break;
                case "Tagihan Detail":
                    FormatWorksheetTagihanDetail(worksheet, dataSet.Tables[1], periode);
                    break;
                default:
                    throw new ArgumentException("Invalid sheet name");
            }
        }

        private static void FormatWorksheetRekapTagihan(ExcelWorksheet worksheet, DataTable table, string periode)
        {
            // Sort the DataTable by UNIT_KERJA
            DataView dataView = table.DefaultView;
            dataView.Sort = "UNIT_KERJA ASC";
            DataTable sortedTable = dataView.ToTable();

            worksheet.Cells[3, 1].Value = "REKAP TAGIHAN";
            worksheet.Cells[3, 1, 3, 5].Merge = true;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = periode;
            worksheet.Cells[4, 1, 4, 5].Merge = true;
            worksheet.Cells[4, 1, 4, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Load data from DataTable
            worksheet.Cells[7, 1].LoadFromDataTable(sortedTable, true);

            worksheet.Cells[7, 5, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTotalRow = worksheet.Dimension.End.Row;

            worksheet.Cells[lastTotalRow + 1, 5].Formula = $"=SUM(E7:E{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 5].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 5].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(4).Width = 13;
            worksheet.Column(5).Width = 13;
        }

        private static void FormatWorksheetTagihanDetail(ExcelWorksheet worksheet, DataTable table, string periode)
        {
            // Sort the DataTable by UNIT_KERJA
            DataView dataView = table.DefaultView;
            dataView.Sort = "UNIT_KERJA ASC";
            DataTable sortedTable = dataView.ToTable();

            worksheet.Cells[3, 1].Value = "TAGIHAN KARYAWAN";
            worksheet.Cells[3, 1, 3, 10].Merge = true;
            worksheet.Cells[3, 1, 3, 10].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 10].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value =periode;
            worksheet.Cells[4, 1, 4, 10].Merge = true;
            worksheet.Cells[4, 1, 4, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Load data from DataTable
            worksheet.Cells[7, 1].LoadFromDataTable(sortedTable, true);

            worksheet.Cells[7,5, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTotalRow = worksheet.Dimension.End.Row;

            worksheet.Cells[lastTotalRow + 1, 10].Formula = $"=SUM(J8:J{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 10].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 10].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            //worksheet.Column(1).Width = 8;
            worksheet.Column(10).Width = 15;
        }

        private static string GetFullNumberFormat()
        {
            return "#,##0.00";
        }
    }
}
