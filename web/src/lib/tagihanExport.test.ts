import { describe, expect, it } from 'vitest';
import { buildPotonganWorkbook } from './tagihanExport';
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
