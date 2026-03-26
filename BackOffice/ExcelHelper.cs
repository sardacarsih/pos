using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Collections.Generic;
using System.Globalization;

namespace BackOffice
{
    public static class ExcelHelper
    {
        public static void AddWorksheet<T>(ExcelPackage package, string sheetName, string periode, List<T> data)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);
            worksheet.Cells["A6"].LoadFromCollection(data, true, TableStyles.Light9);
            worksheet.Cells[1, 1].Value = "PT. KALIMANTAN SAWIT KUSUMA";
            worksheet.Cells[2, 1].Value = "KOPERASI KARYAWAN";

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            if (worksheet.Tables.Count > 0)
            {
                foreach (var table in worksheet.Tables)
                {
                    table.ShowFilter = false;
                }
            }
            else
            {
                worksheet.Cells.AutoFilter = false;
            }

            if (data.Count > 0)
            {
                switch (sheetName)
                {
                    case "Rekap Upah":
                        FormatWorksheetRekapUpah(worksheet, periode);
                        break;
                    case "Tunjangan":
                        FormatWorksheetTunjangan(worksheet, periode);
                        break;
                    case "Premi Lembur":
                        FormatWorksheetPremi(worksheet, periode);
                        break;
                    case "Pot BPJS":
                        FormatWorksheetBPJS(worksheet, periode);
                        break;
                    case "Pot Tunjangan":
                        FormatWorksheetPotTunjangan(worksheet, periode);
                        break;
                    case "Pot Kantor":
                        FormatWorksheetPotKantor(worksheet, periode);
                        break;
                    case "Pot Koperasi":
                        FormatWorksheetPotKoperasi(worksheet, periode);
                        break;
                    case "Lampiran KAS":
                        FormatWorksheetLampiranKAS(worksheet, periode);
                        break;
                    default:
                        // Default formatting for other sheets
                        break;
                }
            }
        }

        private static void FormatWorksheetPotKoperasi(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR POTONGAN KOPERASI KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 5].Merge = true;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 5].Merge = true;
            worksheet.Cells[4, 1, 4, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 4, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 5;
        }

        private static void FormatWorksheetPotKantor(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR POTONGAN KANTOR KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 6].Merge = true;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 6].Merge = true;
            worksheet.Cells[4, 1, 4, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 6, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int columnNumber = 6;
            int endRow = worksheet.Dimension.End.Row;

            double sum = 0;
            for (int row = 7; row <= endRow; row++)
            {
                var cellValue = worksheet.Cells[row, columnNumber].Value;
                if (cellValue != null && double.TryParse(cellValue.ToString(), out double cellDoubleValue))
                {
                    sum += cellDoubleValue;
                }
            }

            int resultRow = endRow + 1;
            worksheet.Cells[resultRow, columnNumber].Value = sum;
            worksheet.Cells[resultRow, columnNumber].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[resultRow, columnNumber].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
        }

        private static void FormatWorksheetPotTunjangan(ExcelWorksheet worksheet, string periode)
        {
            int[] columnsToDelete = { 0, 1, 2, 3, 12 };
            for (int i = columnsToDelete.Length - 1; i >= 0; i--)
            {
                int columnIndex = columnsToDelete[i] + 1;
                worksheet.DeleteColumn(columnIndex);
            }

            if (worksheet.Tables.Count > 0)
            {
                foreach (var table in worksheet.Tables)
                {
                    table.ShowFilter = false;
                }
            }
            else
            {
                worksheet.Cells.AutoFilter = false;
            }

            worksheet.Cells[1, 1].Value = "CompanyInfo.NAMAPT";
            worksheet.Cells[2, 1].Value = " CompanyInfo.ESTATENAMA";
            worksheet.Cells[3, 1].Value = "DAFTAR POTONGAN TUNJANGAN KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 8].Merge = true;
            worksheet.Cells[3, 1, 3, 8].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 8].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 8].Merge = true;
            worksheet.Cells[4, 1, 4, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 7, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTarifRow = worksheet.Cells["G" + worksheet.Dimension.End.Row].End.Row;
            int lastTotalRow = worksheet.Cells["H" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastTarifRow + 1, 7].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTarifRow + 1, 7].Style.Font.Bold = true;
            worksheet.Cells[lastTotalRow + 1, 8].Formula = $"=SUM(H7:H{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 8].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 8].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(7).Width = 14;
            worksheet.Column(8).Width = 14;
        }

        private static void FormatWorksheetBPJS(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR POTONGAN BPJS KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 10].Merge = true;
            worksheet.Cells[3, 1, 3, 10].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 10].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 10].Merge = true;
            worksheet.Cells[4, 1, 4, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 7, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastKesRow = worksheet.Cells["G" + worksheet.Dimension.End.Row].End.Row;
            int lastJhttRow = worksheet.Cells["H" + worksheet.Dimension.End.Row].End.Row;
            int lastJpRow = worksheet.Cells["I" + worksheet.Dimension.End.Row].End.Row;
            int lastTotalRow = worksheet.Cells["J" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastKesRow + 1, 7].Formula = $"=SUM(G7:G{lastKesRow})";
            worksheet.Cells[lastKesRow + 1, 7].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastKesRow + 1, 7].Style.Font.Bold = true;
            worksheet.Cells[lastJhttRow + 1, 8].Formula = $"=SUM(H7:H{lastJhttRow})";
            worksheet.Cells[lastJhttRow + 1, 8].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastJhttRow + 1, 8].Style.Font.Bold = true;
            worksheet.Cells[lastJpRow + 1, 9].Formula = $"=SUM(I7:I{lastJpRow})";
            worksheet.Cells[lastJpRow + 1, 9].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastJpRow + 1, 9].Style.Font.Bold = true;
            worksheet.Cells[lastTotalRow + 1, 10].Formula = $"=SUM(J7:J{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 10].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 10].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
        }

        private static void FormatWorksheetPremi(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR PREMI DAN LEMBUR KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 6].Merge = true;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 6].Merge = true;
            worksheet.Cells[4, 1, 4, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 6, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTarifRow = worksheet.Cells["E" + worksheet.Dimension.End.Row].End.Row;
            int lastTotalRow = worksheet.Cells["F" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastTarifRow + 1, 5].Formula = $"=SUM(E7:E{lastTarifRow})";
            worksheet.Cells[lastTarifRow + 1, 5].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTarifRow + 1, 5].Style.Font.Bold = true;
            worksheet.Cells[lastTotalRow + 1, 6].Formula = $"=SUM(F7:F{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 6].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 6].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(4).Width = 13;
            worksheet.Column(5).Width = 13;
            worksheet.Column(6).Width = 13;
        }

        private static void FormatWorksheetTunjangan(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR TUNJANGAN KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 7].Merge = true;
            worksheet.Cells[3, 1, 3, 7].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 7].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 7].Merge = true;
            worksheet.Cells[4, 1, 4, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 7, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTotalRow = worksheet.Cells["G" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastTotalRow + 1, 7].Formula = $"=SUM(G7:G{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 7].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 7].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(7).Width = 13;
        }

        private static void FormatWorksheetRekapUpah(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "REKAP UPAH KARYAWAN STAFF DAN BULANAN";
            worksheet.Cells[3, 1, 3, 5].Merge = true;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 5].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 5].Merge = true;
            worksheet.Cells[4, 1, 4, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 5, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTotalRow = worksheet.Cells["E" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastTotalRow + 1, 5].Formula = $"=SUM(E7:E{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 5].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 5].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(4).Width = 13;
            worksheet.Column(5).Width = 13;
        }

        private static void FormatWorksheetLampiranKAS(ExcelWorksheet worksheet, string periode)
        {
            worksheet.Cells[3, 1].Value = "DAFTAR LAMPIRAN KAS";
            worksheet.Cells[3, 1, 3, 6].Merge = true;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Bold = true;
            worksheet.Cells[3, 1, 3, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[3, 1, 3, 6].Style.Font.Size = 12;

            worksheet.Cells[4, 1].Value = "PERIODE : " + periode;
            worksheet.Cells[4, 1, 4, 6].Merge = true;
            worksheet.Cells[4, 1, 4, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[7, 6, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Numberformat.Format = GetFullNumberFormat();

            int lastTarifRow = worksheet.Cells["E" + worksheet.Dimension.End.Row].End.Row;
            int lastTotalRow = worksheet.Cells["F" + worksheet.Dimension.End.Row].End.Row;

            worksheet.Cells[lastTarifRow + 1, 5].Formula = $"=SUM(E7:E{lastTarifRow})";
            worksheet.Cells[lastTarifRow + 1, 5].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTarifRow + 1, 5].Style.Font.Bold = true;
            worksheet.Cells[lastTotalRow + 1, 6].Formula = $"=SUM(F7:F{lastTotalRow})";
            worksheet.Cells[lastTotalRow + 1, 6].Style.Numberformat.Format = GetFullNumberFormat();
            worksheet.Cells[lastTotalRow + 1, 6].Style.Font.Bold = true;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(1).Width = 8;
            worksheet.Column(4).Width = 13;
            worksheet.Column(5).Width = 13;
            worksheet.Column(6).Width = 13;
        }

        private static string GetFullNumberFormat()
        {
            return "#,##0.00;(#,##0.00);\"\"";
        }
    }
}
