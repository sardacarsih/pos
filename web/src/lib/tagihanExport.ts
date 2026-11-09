export interface ComparisonItem { nik: string; nama: string; unit: string; tagihan: number; rekap: number; selisih: number }
export interface ComparisonReport { periode: number; remise: number; dari: string; sampai: string; tagihan: number; rekap: number; selisih: number; items: ComparisonItem[] }

/** Build from the visible rows, so the exported totals correspond to the active filters. */
export async function buildComparisonWorkbook(report: ComparisonReport, rows: ComparisonItem[], search: string, differenceOnly: boolean) {
  const ExcelJS = await import('exceljs');
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Kopkar Kusuma Lestari';
  const sheet = workbook.addWorksheet('Tagihan vs Rekap', { views: [{ state: 'frozen', ySplit: 7 }] });
  const names: Record<number, string> = { 1: 'Remise I', 2: 'Remise II', 3: 'Bulanan' };
  sheet.columns = [{ width: 23 }, { width: 32 }, { width: 27 }, { width: 22 }, { width: 22 }, { width: 22 }];
  sheet.mergeCells('A1:F1'); sheet.getCell('A1').value = 'TAGIHAN VS REKAP';
  sheet.getCell('A1').font = { bold: true, size: 16, color: { argb: 'FF146A31' } };
  sheet.mergeCells('A2:F2'); sheet.getCell('A2').value = `Periode ${report.periode} · ${names[report.remise] || report.remise} · ${report.dari} s/d ${report.sampai}`;
  sheet.mergeCells('A3:F3'); sheet.getCell('A3').value = `Filter: ${differenceOnly ? 'hanya ada selisih' : 'semua selisih'}${search.trim() ? `; pencarian: ${search.trim()}` : ''}`;
  sheet.mergeCells('A4:F4'); sheet.getCell('A4').value = 'Tagihan Waserda dari BackOffice; rekap dari transaksi kredit. Selisih = Tagihan − Rekap.';
  sheet.getRow(6).values = ['Total baris diekspor', '', '', rows.reduce((n, x) => n + x.tagihan, 0), rows.reduce((n, x) => n + x.rekap, 0), rows.reduce((n, x) => n + x.selisih, 0)];
  sheet.getRow(6).font = { bold: true };
  const header = sheet.getRow(7); header.values = ['NIK', 'Nama', 'Unit kerja', 'Tagihan Waserda', 'Rekap kredit', 'Selisih'];
  header.font = { bold: true, color: { argb: 'FFFFFFFF' } }; header.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF147D37' } };
  header.alignment = { vertical: 'middle' }; header.height = 24;
  for (const item of rows) sheet.addRow([String(item.nik), item.nama, item.unit, item.tagihan, item.rekap, item.selisih]);
  for (let i = 6; i <= sheet.rowCount; i++) for (let col = 4; col <= 6; col++) sheet.getRow(i).getCell(col).numFmt = '#,##0;[Red]-#,##0';
  sheet.autoFilter = { from: 'A7', to: `F${Math.max(7, sheet.rowCount)}` };
  return workbook;
}
export async function exportComparison(report: ComparisonReport, rows: ComparisonItem[], search: string, differenceOnly: boolean) {
  const workbook = await buildComparisonWorkbook(report, rows, search, differenceOnly);
  await saveWorkbook(workbook, `tagihan-vs-rekap-${report.periode}-${periodName(report.remise)}.xlsx`);
}

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
