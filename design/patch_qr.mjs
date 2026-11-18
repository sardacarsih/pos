import { readFileSync, writeFileSync } from 'node:fs';
function ubah(berkas, pasangan) {
  let s = readFileSync(berkas, 'utf8');
  for (const [a, b] of pasangan) {
    if (!s.includes(a)) throw new Error(`${berkas}: tidak ketemu: ${a.slice(0, 60)}`);
    s = s.replace(a, b);
  }
  writeFileSync(berkas, s);
}
// Picker pelanggan: tombol scan QR yang jelas di bawah pencarian.
ubah('screens_b.mjs', [[
  "searchBar(sb, t, 'budi', { value: true });\n  const ls = box(f, { name: 'hasil'",
  "searchBar(sb, t, 'budi', { value: true });\n  const qrb = box(f, { name: 'baris scan QR', sh: 'fill', layout: 'vertical', pad: [0, 16, 8, 16] });\n  button(qrb, t, 'Scan QR kartu anggota', { kind: 'secondary', icon: 'qr', h: 48 });\n  const ls = box(f, { name: 'hasil'",
]]);
ubah('screens_c.mjs', [["appBar(f, t, 'Transaksi Saya', { actions: ['logout'] });", "appBar(f, t, 'Transaksi Saya', { actions: ['qr', 'logout'] });"]]);
ubah('build.mjs', [
  ["import { transaksiSaya,", "import { qrScan, qrDitemukan, qrTidakDitemukan, qrIzin, kartuAnggota } from './screens_qr.mjs';\nimport { transaksiSaya,"],
  ["    bbmTablet(light, 0, 2 * H);", "    // Pemilihan anggota via QR\n    susun([[], [], [], [[qrScan, light], [qrDitemukan, light], [qrTidakDitemukan, light], [qrIzin, light], [qrDitemukan, dark], [qrTidakDitemukan, dark]]]);\n    bbmTablet(light, 0, 2 * H);"],
  ["    [[transaksiSaya, light], [detailFaktur, light]],\n    [[transaksiSaya, dark], [detailFaktur, dark]],", "    [[transaksiSaya, light], [detailFaktur, light], [kartuAnggota, light]],\n    [[transaksiSaya, dark], [detailFaktur, dark], [kartuAnggota, dark]],"],
]);
console.log('patch qr ok');
