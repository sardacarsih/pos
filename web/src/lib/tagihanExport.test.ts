import { describe, expect, it } from 'vitest';
import { buildComparisonWorkbook, buildPotonganWorkbook, type ComparisonReport } from './tagihanExport';
import ExcelJS from 'exceljs';

describe('Excel Potongan Koperasi (format template bulanan)', () => {
  it('memakai kolom template: NO, NIK teks, NAMA, KEBUN = rekap, PONTIANAK = 0, urut NIK', async () => {
    const rows = [
      { nik: '05.00214', nama: 'Budi', unit: 'PKS', tagihan: 10, rekap: 250000, selisih: -240000 },
      { nik: '02.00214', nama: 'Ani', unit: 'KSK - NE', tagihan: 0, rekap: 125000, selisih: -125000 },
    ];
    const binary = await (await buildPotonganWorkbook(rows)).xlsx.writeBuffer();
    const loaded = new ExcelJS.Workbook();
    await loaded.xlsx.load(binary);
    const sheet = loaded.getWorksheet('Potongan Koperasi')!;
    expect(['A1', 'B1', 'C1', 'D1', 'E1'].map(c => sheet.getCell(c).value)).toEqual(['NO', 'NIK', 'NAMA', 'KEBUN', 'PONTIANAK']);
    expect(sheet.getCell('A1').font.bold).toBe(true);
    expect([2, 3].map(r => [sheet.getCell(`A${r}`).value, sheet.getCell(`B${r}`).value, sheet.getCell(`C${r}`).value, sheet.getCell(`D${r}`).value, sheet.getCell(`E${r}`).value])).toEqual([
      [1, '02.00214', 'Ani', 125000, 0],
      [2, '05.00214', 'Budi', 250000, 0],
    ]);
    expect(sheet.getCell('B2').numFmt).toBe('@');
    expect(sheet.rowCount).toBe(3);
  });

  it('non bulanan (remise I/II): hanya NO, NIK, NAMA, KEBUN tanpa kolom PONTIANAK', async () => {
    const rows = [{ nik: '02.00214', nama: 'Ani', unit: 'KSK - NE', tagihan: 0, rekap: 125000, selisih: -125000 }];
    const loaded = new ExcelJS.Workbook();
    await loaded.xlsx.load(await (await buildPotonganWorkbook(rows, false)).xlsx.writeBuffer());
    const sheet = loaded.getWorksheet('Potongan Koperasi')!;
    expect(sheet.getRow(1).values).toEqual([undefined, 'NO', 'NIK', 'NAMA', 'KEBUN']);
    expect(sheet.getRow(2).values).toEqual([undefined, 1, '02.00214', 'Ani', 125000]);
    expect(sheet.columnCount).toBe(4);
  });
});

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
