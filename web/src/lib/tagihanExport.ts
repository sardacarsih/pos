export interface ComparisonItem { nik: string; nama: string; unit: string; tagihan: number; rekap: number; selisih: number }
export interface ComparisonReport { periode: number; remise: number; dari: string; sampai: string; tagihan: number; rekap: number; selisih: number; items: ComparisonItem[] }

const periodName = (remise: number) => ['', 'remise-1', 'remise-2', 'bulanan'][remise] || String(remise);

async function saveWorkbook(workbook: { xlsx: { writeBuffer(): Promise<ArrayBuffer | Uint8Array | Buffer | unknown> } }, filename: string) {
  const bytes = (await workbook.xlsx.writeBuffer()) as BlobPart;
  const url = URL.createObjectURL(new Blob([bytes], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }));
  const link = document.createElement('a'); link.href = url; link.download = filename;
  try { document.body.appendChild(link); link.click(); } finally { link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000); }
}

/**
 * Format "Potongan Koperasi". Bulanan (Template_Koperasi_Bulanan.xlsx): NO | NIK | NAMA | KEBUN | PONTIANAK.
 * Non bulanan / Remise I-II (Template_Koperasi_Harian.xlsx): NO | NIK | NAMA | KEBUN.
 * NIK tetap NIK database (teks). KEBUN = rekap kredit anggota; PONTIANAK (hanya bulanan) = 0. Urut NIK.
 */
export async function buildPotonganWorkbook(rows: ComparisonItem[], bulanan = true) {
  const ExcelJS = await import('exceljs');
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Kopkar Kusuma Lestari';
  const sheet = workbook.addWorksheet('Potongan Koperasi');
  sheet.columns = [{ width: 9.1 }, { width: 13 }, { width: 35.1 }, { width: 13.3 }, ...(bulanan ? [{ width: 13.3 }] : [])];
  sheet.getRow(1).values = bulanan ? ['NO', 'NIK', 'NAMA', 'KEBUN', 'PONTIANAK'] : ['NO', 'NIK', 'NAMA', 'KEBUN'];
  sheet.getRow(1).font = { bold: true };
  sheet.getColumn(2).numFmt = '@';
  [...rows].sort((a, b) => a.nik.localeCompare(b.nik, 'id', { numeric: true })).forEach((item, i) => {
    sheet.addRow([i + 1, String(item.nik), item.nama, item.rekap, ...(bulanan ? [0] : [])]);
  });
  return workbook;
}
export async function exportPotongan(report: ComparisonReport, rows: ComparisonItem[]) {
  await saveWorkbook(await buildPotonganWorkbook(rows, report.remise === 3), `potongan-koperasi-${report.periode}-${periodName(report.remise)}.xlsx`);
}
