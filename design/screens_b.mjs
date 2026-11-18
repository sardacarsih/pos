import { box, txt, icon, dot } from './lib.mjs';
import { rp } from './theme.mjs';
import { phone, appBar, button, field, chip, searchBar, barang, thumb, head } from './screens_a.mjs';

function stepper(parent, t, qty) {
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

function sumRow(parent, t, l, v, o = {}) {
  const r = box(parent, { name: `total ${l}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
  txt(r, l, { size: o.big ? 18 : 14, weight: o.big ? 'SemiBold' : 'Regular', color: o.big ? t.text : t.muted });
  txt(r, v, { size: o.big ? 26 : 14, weight: o.big ? 'SemiBold' : 'Medium', color: o.color ?? t.text, serif: !!o.big });
}

export function keranjang(t, x, y) {
  const f = phone(t, 'Kasir - keranjang', x, y);
  const sc = box(f, { name: 'latar digelapkan', sh: 'fill', h: 150, fill: t.scrim });
  const sh = box(f, { name: 'bottom sheet keranjang', sh: 'fill', sv: 'fill', r: [28, 28, 0, 0], fill: t.bg, layout: 'vertical', pad: [0, 0, 24, 0] });
  const hd = box(sh, { name: 'pegangan', sh: 'fill', h: 28, layout: 'horizontal', main: 'center', cross: 'center' });
  box(hd, { name: 'garis pegangan', w: 36, h: 4, r: 2, fill: t.outline });
  const ti = box(sh, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [0, 16, 8, 20], cross: 'center' });
  const th = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(th, t, 'Keranjang', '3 item', 28);
  const xb = box(ti, { name: 'tutup', w: 44, h: 44, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(xb, 'x', t.text, 20);
  const ls = box(sh, { name: 'baris keranjang', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  cartLine(ls, t, 'Beras Premium', '5 kg', 68500, 1);
  cartLine(ls, t, 'Minyak Goreng', '2 L', 36000, 1);
  cartLine(ls, t, 'Teh Celup', '25 pcs', 9800, 2);
  const w = box(sh, { name: 'peringatan stok', sh: 'fill', layout: 'horizontal', pad: [8, 16, 8, 16], cross: 'center', gap: 8, fill: t.warnCont, r: 0 });
  icon(w, 'alert', t.warn, 18);
  txt(w, 'Teh Celup: qty 2 melebihi stok 1 (server menentukan)', { size: 12, color: t.warn, sh: 'fill' });
  const ft = box(sh, { name: 'ringkasan & bayar', sh: 'fill', layout: 'vertical', pad: [12, 16, 0, 16], gap: 6 });
  txt(ft, 'Potongan diskon qty dan total akhir dihitung server saat bayar.', { size: 12, color: t.muted, sh: 'fill' });
  box(ft, { name: 'jarak', sh: 'fill', h: 6 });
  button(ft, t, 'Bayar', { h: 56, split: rp(152000) });
  return f;
}

export function pelanggan(t, x, y) {
  const f = phone(t, 'Pilih pelanggan', x, y);
  appBar(f, t, 'Pilih Pelanggan', { back: true, actions: ['scan'] });
  const sb = box(f, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [4, 16, 8, 16] });
  searchBar(sb, t, 'budi', { value: true });
  const qrb = box(f, { name: 'baris scan QR', sh: 'fill', layout: 'vertical', pad: [0, 16, 8, 16] });
  button(qrb, t, 'Scan QR kartu anggota', { kind: 'secondary', icon: 'qr', h: 48 });
  const ls = box(f, { name: 'hasil', sh: 'fill', sv: 'fill', layout: 'vertical', clip: true });
  const row = (nama, sub, tag, sel) => {
    const r = box(ls, { name: `pelanggan ${nama}`, sh: 'fill', h: 72, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 12, fill: sel ? t.primaryCont : 'none' });
    const av = box(r, { name: 'avatar', w: 44, h: 44, r: 22, fill: t.surface2, layout: 'horizontal', main: 'center', cross: 'center' });
    icon(av, 'user', t.muted, 22);
    const c = box(r, { name: 'info', sh: 'fill', layout: 'vertical', gap: 2 });
    txt(c, nama, { size: 16, weight: 'Medium', color: sel ? t.onPrimaryCont : t.text, sh: 'fill' });
    txt(c, sub, { size: 12, color: sel ? t.onPrimaryCont : t.muted, sh: 'fill' });
    if (tag) { const b = box(r, { name: 'label', h: 24, r: 12, pad: [0, 10, 0, 10], layout: 'horizontal', cross: 'center', fill: t.surface2 }); txt(b, tag, { size: 11, weight: 'SemiBold', color: t.muted }); }
    if (sel) icon(r, 'check', t.onPrimaryCont, 22);
    box(ls, { name: 'pemisah', sh: 'fill', h: 1, fill: t.outline, opacity: 0.5 });
  };
  row('Budi Santoso', '12.34.0021 · Unit Produksi', '', true);
  row('Budiarti', '12.34.0087 · Unit Gudang', '', false);
  row('Budi Hartono', '12.35.0140 · Unit Kantor', '', false);
  row('Pelanggan Kas (Tunai)', '00.00004', 'TUNAI', false);
  const n = box(f, { name: 'catatan scan', sh: 'fill', layout: 'horizontal', pad: [12, 16, 24, 16], gap: 8, cross: 'center' });
  icon(n, 'scan', t.muted, 18);
  txt(n, 'Scan kartu anggota opsional. Bila tidak cocok: "Pelanggan tidak ditemukan", pilihan tidak berubah.', { size: 12, color: t.muted, sh: 'fill' });
  return f;
}

export function bayarTunai(t, x, y, short = false) {
  const f = phone(t, short ? 'Bayar - uang kurang' : 'Bayar - tunai', x, y);
  appBar(f, t, 'Pembayaran', { back: true });
  const b = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [8, 16, 16, 16], gap: 16 });
  const pc = box(b, { name: 'kartu pelanggan', sh: 'fill', layout: 'horizontal', pad: 16, cross: 'center', gap: 12, r: 16, fill: t.surface, stroke: t.outline });
  const av = box(pc, { name: 'avatar', w: 44, h: 44, r: 22, fill: t.primaryCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(av, 'user', t.onPrimaryCont, 22);
  const c = box(pc, { name: 'info', sh: 'fill', layout: 'vertical', gap: 2 });
  txt(c, 'Pelanggan Kas', { size: 16, weight: 'SemiBold', color: t.text, sh: 'fill' });
  txt(c, '00.00004 · Tunai', { size: 12, color: t.muted, sh: 'fill' });
  icon(pc, 'chevR', t.muted, 22);
  const tt = box(b, { name: 'total', sh: 'fill', layout: 'vertical', pad: 16, gap: 4, r: 16, fill: t.primaryCont });
  txt(tt, 'Perkiraan total', { size: 13, color: t.onPrimaryCont });
  txt(tt, rp(152000), { size: 32, weight: 'Bold', color: t.onPrimaryCont });
  txt(tt, 'Total akhir (diskon qty) dihitung server.', { size: 12, color: t.onPrimaryCont });
  field(b, t, 'Uang bayar (Rp)', short ? '100.000' : '200.000', { value: true, focus: true });
  const kb = box(b, { name: 'kembalian', sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center', pad: [4, 4, 4, 4] });
  txt(kb, short ? 'Kurang dari total' : 'Kembalian perkiraan', { size: 14, color: short ? t.error : t.muted });
  txt(kb, rp(short ? 52000 : 48000), { size: 20, weight: 'Bold', color: short ? t.error : t.ok });
  const q = box(b, { name: 'uang pas cepat', sh: 'fill', layout: 'horizontal', gap: 8 });
  chip(q, t, 'Uang pas', false); chip(q, t, rp(160000), false); chip(q, t, rp(200000), true);
  box(b, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  button(b, t, 'Simpan & Cetak', { icon: 'print' });
  return f;
}

export function bayarKredit(t, x, y) {
  const f = phone(t, 'Bayar - kredit anggota', x, y);
  appBar(f, t, 'Pembayaran', { back: true });
  const b = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [8, 16, 16, 16], gap: 16 });
  const pc = box(b, { name: 'kartu pelanggan', sh: 'fill', layout: 'horizontal', pad: 16, cross: 'center', gap: 12, r: 16, fill: t.surface, stroke: t.outline });
  const av = box(pc, { name: 'avatar', w: 44, h: 44, r: 22, fill: t.primaryCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(av, 'user', t.onPrimaryCont, 22);
  const c = box(pc, { name: 'info', sh: 'fill', layout: 'vertical', gap: 2 });
  txt(c, 'Budi Santoso', { size: 16, weight: 'SemiBold', color: t.text, sh: 'fill' });
  txt(c, '12.34.0021 · Limit Rp 2.000.000 / periode', { size: 12, color: t.muted, sh: 'fill' });
  icon(pc, 'chevR', t.muted, 22);
  const tt = box(b, { name: 'total', sh: 'fill', layout: 'vertical', pad: 16, gap: 4, r: 16, fill: t.primaryCont });
  txt(tt, 'Perkiraan total · Kredit', { size: 13, color: t.onPrimaryCont });
  txt(tt, rp(152000), { size: 32, weight: 'Bold', color: t.onPrimaryCont });
  const sel = box(b, { name: 'tenor', sh: 'fill', layout: 'vertical', gap: 6 });
  txt(sel, 'Tenor (1 = sekali bayar)', { size: 12, weight: 'Medium', color: t.muted });
  const i = box(sel, { name: 'dropdown tenor', sh: 'fill', h: 52, r: 12, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', fill: t.surface, stroke: t.primary, sw: 2 });
  txt(i, '3x angsuran', { size: 16, color: t.text, sh: 'fill' });
  icon(i, 'chevD', t.muted, 22);
  const jd = box(b, { name: 'jadwal ringkas', sh: 'fill', layout: 'vertical', pad: 16, gap: 8, r: 16, fill: t.surface, stroke: t.outline });
  txt(jd, 'Perkiraan angsuran', { size: 13, weight: 'SemiBold', color: t.text });
  sumRow(jd, t, 'Ke-1 · 30 Nov 2026', rp(50666));
  sumRow(jd, t, 'Ke-2 · 31 Des 2026', rp(50666));
  sumRow(jd, t, 'Ke-3 · 31 Jan 2027', rp(50668));
  const w = box(b, { name: 'info limit', sh: 'fill', layout: 'horizontal', pad: [10, 12, 10, 12], gap: 8, cross: 'center', r: 12, fill: t.surface2 });
  icon(w, 'shield', t.muted, 18);
  txt(w, 'Limit hutang diperiksa server saat menyimpan.', { size: 12, color: t.muted, sh: 'fill' });
  box(b, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  button(b, t, 'Simpan & Cetak', { icon: 'print' });
  return f;
}

export function bayarGagalLimit(t, x, y) {
  const f = phone(t, 'Bayar - limit terlampaui', x, y);
  appBar(f, t, 'Pembayaran', { back: true });
  const b = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [8, 16, 16, 16], gap: 16 });
  const e = box(b, { name: 'galat limit', sh: 'fill', layout: 'vertical', pad: 16, gap: 8, r: 16, fill: t.errorCont });
  const h = box(e, { name: 'judul galat', layout: 'horizontal', gap: 8, cross: 'center' });
  icon(h, 'alert', t.onErrorCont, 22);
  txt(h, 'Limit hutang terlampaui', { size: 16, weight: 'SemiBold', color: t.onErrorCont });
  txt(e, 'Transaksi tidak disimpan.', { size: 14, color: t.onErrorCont, sh: 'fill' });
  sumRow(e, { ...t, muted: t.onErrorCont, text: t.onErrorCont }, 'Belanja periode ini', rp(1950000));
  sumRow(e, { ...t, muted: t.onErrorCont, text: t.onErrorCont }, 'Faktur baru', rp(152000));
  sumRow(e, { ...t, muted: t.onErrorCont, text: t.onErrorCont }, 'Batas limit', rp(2000000));
  box(b, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  button(b, t, 'Ubah keranjang', { kind: 'secondary' });
  return f;
}

export function struk(t, x, y) {
  const f = phone(t, 'Faktur tersimpan', x, y);
  const hd = box(f, { name: 'header sukses', sh: 'fill', layout: 'vertical', cross: 'center', pad: [24, 24, 16, 24], gap: 8 });
  const ok = box(hd, { name: 'ikon sukses', w: 64, h: 64, r: 32, fill: t.okCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(ok, 'check', t.ok, 32, 3);
  txt(hd, 'Faktur tersimpan', { size: 28, color: t.text, align: 'center', sh: 'fill', serif: true });
  txt(hd, 'BBM-26-000124 · 08 Nov 2026 10:21:05', { size: 13, color: t.muted, align: 'center', sh: 'fill' });
  const cd = box(f, { name: 'kartu struk', sh: 'fill', layout: 'vertical', pad: 16, gap: 8, r: 16, fill: t.surface, stroke: t.outline });
  txt(cd, 'Pelanggan Kas · Tunai', { size: 13, weight: 'SemiBold', color: t.text });
  for (const [n, q, h, tot] of [['Beras Premium 5 kg', 1, 68500, 68500], ['Minyak Goreng 2 L', 1, 36000, 36000], ['Teh Celup 25 pcs', 2, 9800, 19600]]) {
    const r = box(cd, { name: `baris ${n}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
    const c = box(r, { name: 'info', layout: 'vertical', gap: 1 });
    txt(c, n, { size: 14, weight: 'Medium', color: t.text });
    txt(c, `${q} × ${rp(h)}`, { size: 12, color: t.muted });
    txt(r, rp(tot), { size: 14, color: t.text });
  }
  box(cd, { name: 'garis', sh: 'fill', h: 1, fill: t.outline });
  sumRow(cd, t, 'Bruto', rp(152000));
  sumRow(cd, t, 'Potongan', rp(0));
  sumRow(cd, t, 'Total', rp(152000), { big: true });
  sumRow(cd, t, 'Bayar', rp(150000));
  sumRow(cd, t, 'Kembalian', rp(48000), { color: t.ok });
  box(f, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  const ac = box(f, { name: 'aksi', sh: 'fill', layout: 'vertical', pad: [0, 16, 24, 16], gap: 12 });
  button(ac, t, 'Cetak / Bagikan PDF', { kind: 'secondary', icon: 'print' });
  button(ac, t, 'Transaksi baru');
  return f;
}

export function tablet(t, x, y) {
  const W = 1024, H = 768;
  const f = box(null, { name: `Kasir - tablet (${t.mode})`, x, y, w: W, h: H, fill: t.bg, layout: 'vertical', clip: true });
  appBar(f, t, 'Kasir', { actions: ['scan', 'logout'] });
  const row = box(f, { name: 'dua panel', sh: 'fill', sv: 'fill', layout: 'horizontal', gap: 0 });
  const l = box(row, { name: 'panel daftar', sh: 'fill', sv: 'fill', layout: 'vertical' });
  const sb = box(l, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [0, 16, 12, 16] });
  searchBar(sb, t, 'Cari nama atau kode barang', { scan: true });
  const ch = box(l, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 12, 16], gap: 8 });
  chip(ch, t, 'Semua', true); chip(ch, t, 'Sembako', false); chip(ch, t, 'Minuman', false); chip(ch, t, 'Snack', false);
  const ls = box(l, { name: 'daftar', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  barang(ls, t, 'Beras Premium', 'BR-0012 · Sembako', rp(68500), 'Stok 42', { qual: '5 kg' });
  barang(ls, t, 'Minyak Goreng', 'MG-0031 · Sembako', rp(36000), 'Stok 18', { qual: '2 L' });
  barang(ls, t, 'Gula Pasir', 'GP-0007 · Sembako', rp(17500), 'Stok 60', { qual: '1 kg' });
  barang(ls, t, 'Teh Celup', 'TC-0101 · Minuman', rp(9800), 'Stok 7', { qual: '25 pcs' });
  barang(ls, t, 'Kopi Sachet', 'KS-0044 · Minuman', rp(14500), 'Habis', { qual: '10 pcs' });
  box(row, { name: 'pembatas', w: 1, sv: 'fill', fill: t.outline });
  const r = box(row, { name: 'panel keranjang', w: 400, sv: 'fill', layout: 'vertical', fill: t.bg });
  const ti = box(r, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [16, 16, 8, 16], cross: 'center' });
  const th2 = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(th2, t, 'Keranjang', '3 item', 28);
  txt(ti, 'Kosongkan', { size: 14, weight: 'SemiBold', color: t.error });
  const cl = box(r, { name: 'baris', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  cartLine(cl, t, 'Beras Premium', '5 kg', 68500, 1);
  cartLine(cl, t, 'Minyak Goreng', '2 L', 36000, 1);
  cartLine(cl, t, 'Teh Celup', '25 pcs', 9800, 2);
  const ft = box(r, { name: 'ringkasan & bayar', sh: 'fill', layout: 'vertical', pad: [12, 16, 24, 16], gap: 8 });
  button(ft, t, 'Bayar', { split: rp(152000) });
  return f;
}
