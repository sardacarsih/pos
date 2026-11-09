import { describe, expect, it } from 'vitest';
import { buildComparisonWorkbook, type ComparisonReport } from './tagihanExport';
import ExcelJS from 'exceljs';

describe('Excel Tagihan vs Rekap', () => {
  it('exports filtered rows with text NIK and numeric, matching totals', async () => {
    const report: ComparisonReport = { periode: 202603, remise: 1, dari: '2026-03-01', sampai: '2026-03-15', tagihan: 900, rekap: 800, selisih: 100, items: [] };
    const rows = [{ nik: '001234', nama: '=Nama', unit: 'Kebun', tagihan: 300, rekap: 500, selisih: -200 }, { nik: '007', nama: 'Budi', unit: 'Kantor', tagihan: 50, rekap: 50, selisih: 0 }];
    const source = await buildComparisonWorkbook(report, rows.slice(0, 1), 'Kebun', true);
    const binary = await source.xlsx.writeBuffer();
    const loaded = new ExcelJS.Workbook();
    await loaded.xlsx.load(binary);
    const sheet = loaded.getWorksheet('Tagihan vs Rekap')!;
    expect(sheet.getCell('A2').value).toContain('202603');
    expect(sheet.getCell('A3').value).toContain('Kebun');
    expect(sheet.getCell('A8').value).toBe('001234');
    expect(sheet.getCell('B8').value).toBe('=Nama');
    expect(sheet.getCell('D8').value).toBe(300);
    expect(sheet.getCell('F8').value).toBe(-200);
    expect([sheet.getCell('D6').value, sheet.getCell('E6').value, sheet.getCell('F6').value]).toEqual([300, 500, -200]);
    expect(sheet.rowCount).toBe(8);
  });
});
