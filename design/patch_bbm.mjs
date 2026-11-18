// Sekali jalan: samakan data contoh layar lama dengan keranjang BBM (Rp 152.000) dan transaksi anggota BBM.
import { readFileSync, writeFileSync } from 'node:fs';

function ubah(berkas, pasangan) {
  let s = readFileSync(berkas, 'utf8');
  for (const [a, b, semua] of pasangan) {
    if (!s.includes(a)) throw new Error(`${berkas}: tidak ketemu: ${a.slice(0, 60)}`);
    s = semua ? s.split(a).join(b) : s.replace(a, b);
  }
  writeFileSync(berkas, s);
}

ubah('screens_b.mjs', [
  ['rp(124100)', 'rp(152000)', true],
  ["'150.000', { value: true, focus: true }", "'200.000', { value: true, focus: true }"],
  ['rp(25900)', 'rp(48000)', true],
  ['chip(q, t, rp(125000), false); chip(q, t, rp(150000), true);', 'chip(q, t, rp(160000), false); chip(q, t, rp(200000), true);'],
  ["sumRow(jd, t, 'Ke-1 · 30 Nov 2026', rp(41366));", "sumRow(jd, t, 'Ke-1 · 30 Nov 2026', rp(50666));"],
  ["sumRow(jd, t, 'Ke-2 · 31 Des 2026', rp(41366));", "sumRow(jd, t, 'Ke-2 · 31 Des 2026', rp(50666));"],
  ["sumRow(jd, t, 'Ke-3 · 31 Jan 2027', rp(41368));", "sumRow(jd, t, 'Ke-3 · 31 Jan 2027', rp(50668));"],
]);

ubah('screens_c.mjs', [
  ["[['Beras Premium 5 kg', 4, 68500, 274000], ['Minyak Goreng 2 L', 4, 36000, 144000], ['Gula Pasir 1 kg', 2, 17500, 35000]]", "[['Pertalite', '25,00 L', 10000, 250000], ['Dexlite', '15,00 L', 13000, 195000]]"],
  ["row(it, t, 'Bruto', rp(453000));", "row(it, t, 'Bruto', rp(445000));"],
  ["row(it, t, 'Potongan', rp(3000));", "row(it, t, 'Potongan', rp(0));"],
  ["row(it, t, 'Total', rp(450000), { big: true });", "row(it, t, 'Total', rp(445000), { big: true });"],
  ["row(jd, t, 'Ke-1 · 30 Nov 2026', rp(150000));", "row(jd, t, 'Ke-1 · 30 Nov 2026', rp(148333));"],
  ["row(jd, t, 'Ke-2 · 31 Des 2026', rp(150000));", "row(jd, t, 'Ke-2 · 31 Des 2026', rp(148333));"],
  ["row(jd, t, 'Ke-3 · 31 Jan 2027', rp(150000));", "row(jd, t, 'Ke-3 · 31 Jan 2027', rp(148334));"],
  ['[[rp(450000), ', '[[rp(445000), '],
]);
console.log('patch bbm ok');
