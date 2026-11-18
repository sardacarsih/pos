// Sekali jalan: terapkan gaya referensi (krem-oranye, serif) ke screens_b dan screens_c.
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('screens_b.mjs', 'utf8');
const rep = (a, b) => {
  if (!s.includes(a)) throw new Error('b tidak ketemu: ' + a.slice(0, 60));
  s = s.replace(a, b);
};
rep("import { phone, appBar, button, field, chip, searchBar, barang } from './screens_a.mjs';", "import { phone, appBar, button, field, chip, searchBar, barang, thumb, head } from './screens_a.mjs';");

const i0 = s.indexOf('function stepper');
const i1 = s.indexOf('function sumRow');
s = s.slice(0, i0) + `function stepper(parent, t, qty) {
  const s = box(parent, { name: 'stepper qty', layout: 'horizontal', cross: 'center', gap: 0, r: 14, fill: t.surface, stroke: t.outline });
  const mk = (ic) => { const b = box(s, { name: 'tombol ' + ic, w: 40, h: 40, layout: 'horizontal', main: 'center', cross: 'center' }); icon(b, ic, t.accent, 18, 2.5); };
  mk('minus');
  const q = box(s, { name: 'qty', w: 28, h: 40, layout: 'horizontal', main: 'center', cross: 'center' });
  txt(q, String(qty), { size: 16, weight: 'SemiBold', color: t.text });
  mk('plus');
}

function cartLine(parent, t, nama, qual, hargaSat, qty) {
  const r = box(parent, { name: 'baris ' + nama, sh: 'fill', layout: 'horizontal', pad: [10, 12, 10, 10], cross: 'center', gap: 12, r: 20, fill: t.surface });
  thumb(r, t, nama, 64);
  const c = box(r, { name: 'info', sh: 'fill', layout: 'vertical', gap: 4 });
  const nm = box(c, { name: 'nama', sh: 'fill', layout: 'horizontal', cross: 'baseline', gap: 5, wrap: true });
  txt(nm, qual ? nama + ',' : nama, { size: 16, color: t.text, serif: true });
  if (qual) txt(nm, qual, { size: 16, color: t.muted, serif: true });
  txt(c, rp(hargaSat * qty), { size: 16, weight: 'SemiBold', color: t.accent });
  stepper(r, t, qty);
}

` + s.slice(i1);

rep("fill: t.surface, layout: 'vertical', pad: [0, 0, 24, 0] });", "fill: t.bg, layout: 'vertical', pad: [0, 0, 24, 0] });");
rep("const ti = box(sh, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [0, 16, 8, 16], cross: 'center' });", "const ti = box(sh, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [0, 16, 8, 20], cross: 'center' });");

const k0 = s.indexOf("txt(ti, 'Keranjang', { size: 22");
const k1 = s.indexOf("const w = box(sh, { name: 'peringatan stok'");
s = s.slice(0, k0) + `const th = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(th, t, 'Keranjang', '3 item', 28);
  const xb = box(ti, { name: 'tutup', w: 44, h: 44, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(xb, 'x', t.text, 20);
  const ls = box(sh, { name: 'baris keranjang', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  cartLine(ls, t, 'Beras Premium', '5 kg', 68500, 1);
  cartLine(ls, t, 'Minyak Goreng', '2 L', 36000, 1);
  cartLine(ls, t, 'Teh Celup', '25 pcs', 9800, 2);
  ` + s.slice(k1);

rep("  sumRow(ft, t, 'Perkiraan total', rp(124100), { big: true });\n  txt(ft, 'Potongan diskon", "  txt(ft, 'Potongan diskon");
rep("button(ft, t, 'Bayar', { h: 56 });", "button(ft, t, 'Bayar', { h: 56, split: rp(124100) });");
rep("txt(r, v, { size: o.big ? 24 : 14, weight: o.big ? 'Bold' : 'Medium', color: o.color ?? t.text });", "txt(r, v, { size: o.big ? 26 : 14, weight: o.big ? 'SemiBold' : 'Medium', color: o.color ?? t.text, serif: !!o.big });");

// tablet
rep("cartLine(cl, t, 'Beras Premium 5 kg', 68500, 1);\n  cartLine(cl, t, 'Minyak Goreng 2 L', 36000, 1);\n  cartLine(cl, t, 'Teh Celup 25 pcs', 9800, 2);", "cartLine(cl, t, 'Beras Premium', '5 kg', 68500, 1);\n  cartLine(cl, t, 'Minyak Goreng', '2 L', 36000, 1);\n  cartLine(cl, t, 'Teh Celup', '25 pcs', 9800, 2);");
rep("const cl = box(r, { name: 'baris', sh: 'fill', sv: 'fill', layout: 'vertical', clip: true });", "const cl = box(r, { name: 'baris', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });");
rep("txt(ti, 'Keranjang', { size: 22, weight: 'SemiBold', color: t.text, sh: 'fill' });\n  txt(ti, 'Kosongkan'", "const th2 = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });\n  head(th2, t, 'Keranjang', '3 item', 28);\n  txt(ti, 'Kosongkan'");
rep("  sumRow(ft, t, 'Perkiraan total', rp(124100), { big: true });\n  button(ft, t, 'Bayar');", "  button(ft, t, 'Bayar', { split: rp(124100) });");
rep("layout: 'vertical', fill: t.surface });\n  const ti = box(r, { name: 'judul'", "layout: 'vertical', fill: t.bg });\n  const ti = box(r, { name: 'judul'");
const d0 = s.indexOf("const ls = box(l, { name: 'daftar'");
const d1 = s.indexOf("box(row, { name: 'pembatas'");
s = s.slice(0, d0) + `const ls = box(l, { name: 'daftar', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  barang(ls, t, 'Beras Premium', 'BR-0012 · Sembako', rp(68500), 'Stok 42', { qual: '5 kg' });
  barang(ls, t, 'Minyak Goreng', 'MG-0031 · Sembako', rp(36000), 'Stok 18', { qual: '2 L' });
  barang(ls, t, 'Gula Pasir', 'GP-0007 · Sembako', rp(17500), 'Stok 60', { qual: '1 kg' });
  barang(ls, t, 'Teh Celup', 'TC-0101 · Minuman', rp(9800), 'Stok 7', { qual: '25 pcs' });
  barang(ls, t, 'Kopi Sachet', 'KS-0044 · Minuman', rp(14500), 'Habis', { qual: '10 pcs' });
  ` + s.slice(d1);
rep("const sb = box(l, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [4, 16, 8, 16] });", "const sb = box(l, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [0, 16, 12, 16] });");
rep("const ch = box(l, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 8, 16], gap: 8 });", "const ch = box(l, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 12, 16], gap: 8 });");
rep("txt(hd, 'Faktur tersimpan', { size: 22, weight: 'SemiBold', color: t.text, align: 'center', sh: 'fill' });", "txt(hd, 'Faktur tersimpan', { size: 28, color: t.text, align: 'center', sh: 'fill', serif: true });");
writeFileSync('screens_b.mjs', s);

let c = readFileSync('screens_c.mjs', 'utf8');
const rc = (a, b) => {
  if (!c.includes(a)) throw new Error('c tidak ketemu: ' + a.slice(0, 60));
  c = c.replace(a, b);
};
rc("txt(d, 'User baru', { size: 22, weight: 'SemiBold', color: t.text });", "txt(d, 'User baru', { size: 26, color: t.text, serif: true });");
rc("txt(h, 'User dibuat', { size: 22, weight: 'SemiBold', color: t.text });", "txt(h, 'User dibuat', { size: 26, color: t.text, serif: true });");
rc("txt(kl, rp(1250000), { size: 32, weight: 'Bold', color: t.onPrimaryCont });", "txt(kl, rp(1250000), { size: 36, color: t.onPrimaryCont, serif: true });");
writeFileSync('screens_c.mjs', c);
console.log('patch ok');
