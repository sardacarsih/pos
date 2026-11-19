import { box, txt, icon, solid, deco } from './lib.mjs';
import { rp } from './theme.mjs';
import { phone, appBar, head, chip, searchBar, button, addBtn, fotoHeader } from './screens_a.mjs';

/** Data BBM contoh (ilustrasi; harga dan stok tangki hanya isian desain). */
export const BBM = [
  { id: 'pertalite', nama: 'Pertalite', ket: 'RON 90', jenis: 'Bensin', harga: 10000, tangki: '8.420 L', c1: '#8BD36A', c2: '#2F8F3A' },
  { id: 'pertamax', nama: 'Pertamax', ket: 'RON 92', jenis: 'Bensin', harga: 12200, tangki: '6.150 L', c1: '#6FA8F5', c2: '#1F4FB8' },
  { id: 'turbo', nama: 'Pertamax Turbo', ket: 'RON 98', jenis: 'Bensin', harga: 13100, tangki: '3.800 L', c1: '#F0707A', c2: '#A3121F' },
  { id: 'dexlite', nama: 'Dexlite', ket: 'CN 51', jenis: 'Solar', harga: 13000, tangki: '5.200 L', c1: '#F6D55C', c2: '#C79A00' },
  { id: 'dex', nama: 'Pertamina Dex', ket: 'CN 53', jenis: 'Solar', harga: 13500, tangki: '2.900 L', c1: '#9AA3AE', c2: '#3F4854' },
  { id: 'solar', nama: 'Biosolar', ket: 'CN 48', jenis: 'Solar', harga: 6800, tangki: '9.700 L', c1: '#F4B860', c2: '#C26A12' },
];

const TETES = 'M12 2.500c3.600 4.400 6.500 7.600 6.500 11.200a6.500 6.500 0 01-13 0C5.500 10.100 8.400 6.900 12 2.500z';

/** "Foto" produk: ilustrasi gradien per jenis BBM dengan tetesan dan kilau. Ganti dengan foto asli lewat image.place pada slot ini. */
export function fuelArt(parent, f, w, h, o = {}) {
  const r = o.r ?? Math.min(w, h) * 0.22;
  const tile = box(parent, {
    name: `foto ${f.nama}`, w, h, r, layout: 'horizontal', main: 'center', cross: 'center', clip: true,
    fill: { type: 'linearGradient', stops: [f.c1, f.c2] },
  });
  const m = Math.min(w, h);
  deco(tile, -m * 0.25, -m * 0.3, m * 0.9, '#FFFFFF', 0.12);
  deco(tile, w - m * 0.45, h - m * 0.4, m * 0.8, '#000000', 0.1);
  const dw = m * 0.42;
  solid(tile, TETES, dw, dw * (17.7 / 13), '#FFFFFF', 0.95, 'tetes');
  return tile;
}

function sum(parent, t, l, v, big) {
  const r = box(parent, { name: `total ${l}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
  txt(r, l, { size: big ? 18 : 14, weight: big ? 'SemiBold' : 'Regular', color: big ? t.text : t.muted });
  txt(r, v, { size: big ? 26 : 14, weight: big ? 'SemiBold' : 'Medium', color: t.text, serif: !!big });
}

function kartuBbm(parent, t, f, w) {
  const c = box(parent, { name: `kartu ${f.nama}`, w, layout: 'vertical', pad: 8, gap: 8, r: 24, fill: t.surface });
  fuelArt(c, f, w - 16, 112, { r: 18 });
  const info = box(c, { name: 'info', sh: 'fill', layout: 'vertical', gap: 2, pad: [0, 4, 0, 4] });
  txt(info, f.nama, { size: 15, color: t.text, serif: true, sh: 'fill' });
  txt(info, f.ket, { size: 12, weight: 'Medium', color: t.muted });
  txt(info, `Tangki ${f.tangki}`, { size: 11, color: t.muted });
  const pr = box(c, { name: 'harga', sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center', pad: [0, 0, 0, 4] });
  const hp = box(pr, { name: 'harga per liter', layout: 'vertical' });
  txt(hp, rp(f.harga), { size: 16, weight: 'SemiBold', color: t.accent });
  txt(hp, 'per liter', { size: 11, color: t.muted });
  addBtn(pr, t, 36);
  return c;
}

export function bbmGrid(t, x, y, o = {}) {
  const f = phone(t, o.offline ? 'Kasir BBM - offline' : 'Kasir BBM - pilih BBM', x, y);
  if (o.offline) {
    const bn = box(f, { name: 'banner offline', sh: 'fill', h: 40, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 8, fill: t.warnCont });
    icon(bn, 'wifi', t.warn, 18);
    txt(bn, 'Tidak ada koneksi. Pembayaran dinonaktifkan.', { size: 13, weight: 'Medium', color: t.warn, sh: 'fill' });
  }
  appBar(f, t, 'Pilih BBM', { actions: ['scan', 'logout'] });
  const sb = box(f, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [0, 16, 12, 16] });
  searchBar(sb, t, 'Cari BBM atau pelumas');
  const ch = box(f, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 12, 16], gap: 8, clip: true });
  chip(ch, t, 'Semua', true); chip(ch, t, 'Bensin', false); chip(ch, t, 'Solar', false); chip(ch, t, 'Pelumas', false);
  const g = box(f, { name: 'grid BBM', sh: 'fill', sv: 'fill', layout: 'horizontal', wrap: true, gap: 12, pad: [0, 16, 0, 16], clip: true });
  for (const b of BBM.slice(0, 4)) kartuBbm(g, t, b, 165);
  const bar = box(f, { name: 'bar keranjang', sh: 'fill', layout: 'vertical', pad: [12, 16, 24, 16] });
  button(bar, t, 'Lihat keranjang · 2 item', { split: rp(152000), disabled: !!o.offline });
  return f;
}

export function bbmDetail(t, x, y) {
  const f = phone(t, 'Kasir BBM - isi jumlah', x, y);
  const b = BBM[1];
  appBar(f, t, 'Pertamax', { back: true });
  const art = box(f, { name: 'foto besar', sh: 'fill', layout: 'horizontal', pad: [0, 16, 0, 16] });
  fuelArt(art, b, 343, 190, { r: 28 });
  const body = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [16, 16, 0, 16], gap: 12 });
  const hd = box(body, { name: 'judul', sh: 'fill', layout: 'horizontal', cross: 'baseline', gap: 6, wrap: true });
  txt(hd, 'Pertamax,', { size: 32, color: t.text, serif: true });
  txt(hd, 'RON 92', { size: 32, color: t.muted, serif: true });
  const tags = box(body, { name: 'info', sh: 'fill', layout: 'horizontal', gap: 8, cross: 'center' });
  const tg = (s, hl) => { const g = box(tags, { name: s, h: 28, r: 14, pad: [0, 12, 0, 12], layout: 'horizontal', cross: 'center', fill: hl ? t.primaryCont : t.surface, stroke: hl ? undefined : t.outline }); txt(g, s, { size: 12, weight: 'Medium', color: hl ? t.onPrimaryCont : t.muted }); };
  tg('Rp 12.200 / liter', true); tg('Tangki 6.150 L', false); tg('Bensin', false);
  const seg = box(body, { name: 'pilih satuan', sh: 'fill', h: 48, r: 24, layout: 'horizontal', pad: 4, gap: 4, fill: t.surface2 });
  const sg = (s, on) => { const g = box(seg, { name: s, sh: 'fill', sv: 'fill', r: 20, layout: 'horizontal', main: 'center', cross: 'center', fill: on ? t.primary : 'none' }); txt(g, s, { size: 14, weight: 'SemiBold', color: on ? t.onPrimary : t.muted }); };
  sg('Liter', false); sg('Nominal (Rp)', true);
  const inp = box(body, { name: 'input jumlah', sh: 'fill', layout: 'vertical', pad: [16, 20, 16, 20], gap: 2, r: 20, fill: t.surface, stroke: t.primary, sw: 2 });
  txt(inp, 'Nominal', { size: 12, weight: 'Medium', color: t.muted });
  txt(inp, 'Rp 50.000', { size: 40, color: t.text, serif: true });
  const eq = box(inp, { name: 'hasil hitung', layout: 'horizontal', gap: 6, cross: 'center' });
  icon(eq, 'check', t.ok, 16, 2.5);
  txt(eq, '≈ 4,10 liter', { size: 14, weight: 'SemiBold', color: t.ok });
  const q = box(body, { name: 'nominal cepat', sh: 'fill', layout: 'horizontal', gap: 8, wrap: true });
  chip(q, t, 'Rp 20.000', false); chip(q, t, 'Rp 50.000', true); chip(q, t, 'Rp 100.000', false); chip(q, t, 'Isi penuh', false);
  txt(body, '4,10 L × Rp 12.200 = Rp 50.020 (liter dibulatkan 2 desimal, jadi total bisa beda tipis dari nominal). Total akhir dihitung server.', { size: 12, color: t.muted, sh: 'fill' });
  const bar = box(f, { name: 'bar aksi', sh: 'fill', layout: 'vertical', pad: [8, 16, 24, 16] });
  button(bar, t, 'Tambah ke keranjang', { split: rp(50020) });
  return f;
}

function barisKeranjang(parent, t, f, nama, qual, jumlah, harga, w) {
  const r = box(parent, { name: `baris ${nama}`, sh: 'fill', layout: 'horizontal', pad: [10, 12, 10, 10], cross: 'center', gap: 12, r: 20, fill: t.surface });
  if (f) fuelArt(r, f, 64, 64, { r: 16 });
  else { const tl = box(r, { name: 'thumbnail', w: 64, h: 64, r: 16, layout: 'horizontal', main: 'center', cross: 'center', fill: t.surface2 }); txt(tl, 'P', { size: 26, color: t.accent, serif: true }); }
  const c = box(r, { name: 'info', sh: 'fill', layout: 'vertical', gap: 4 });
  const nm = box(c, { name: 'nama', sh: 'fill', layout: 'horizontal', cross: 'baseline', gap: 5, wrap: true });
  txt(nm, nama + ',', { size: 16, color: t.text, serif: true });
  txt(nm, qual, { size: 16, color: t.muted, serif: true });
  txt(c, rp(harga), { size: 16, weight: 'SemiBold', color: t.accent });
  const e = box(r, { name: 'ubah jumlah', h: 40, r: 14, pad: [0, 12, 0, 12], layout: 'horizontal', cross: 'center', gap: 6, fill: t.surface, stroke: t.outline });
  txt(e, jumlah, { size: 15, weight: 'SemiBold', color: t.text });
  icon(e, 'chevD', t.accent, 16, 2.5);
}

export function bbmKeranjang(t, x, y) {
  const f = phone(t, 'Kasir BBM - keranjang', x, y);
  box(f, { name: 'latar digelapkan', sh: 'fill', h: 150, fill: t.scrim });
  const sh = box(f, { name: 'bottom sheet keranjang', sh: 'fill', sv: 'fill', r: [28, 28, 0, 0], fill: t.bg, layout: 'vertical', pad: [0, 0, 24, 0] });
  const hd = box(sh, { name: 'pegangan', sh: 'fill', h: 28, layout: 'horizontal', main: 'center', cross: 'center' });
  box(hd, { name: 'garis pegangan', w: 36, h: 4, r: 2, fill: t.outline });
  const ti = box(sh, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [0, 16, 8, 20], cross: 'center' });
  const th = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(th, t, 'Keranjang', '2 item', 28);
  const xb = box(ti, { name: 'tutup', w: 44, h: 44, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(xb, 'x', t.text, 20);
  const ls = box(sh, { name: 'baris keranjang', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  barisKeranjang(ls, t, BBM[0], 'Pertalite', '10,00 L', '10,00 L', 100000);
  barisKeranjang(ls, t, null, 'Pelumas Mesin', '1 L', '1 L', 52000);
  const ft = box(sh, { name: 'ringkasan & bayar', sh: 'fill', layout: 'vertical', pad: [12, 16, 0, 16], gap: 6 });
  txt(ft, 'Total akhir dihitung server saat bayar.', { size: 12, color: t.muted, sh: 'fill' });
  box(ft, { name: 'jarak', sh: 'fill', h: 6 });
  button(ft, t, 'Bayar', { split: rp(152000) });
  return f;
}

export function bbmStruk(t, x, y) {
  const f = phone(t, 'Faktur BBM tersimpan', x, y);
  appBar(f, t, 'Faktur tersimpan', {});
  const hd = box(f, { name: 'header sukses', sh: 'fill', layout: 'vertical', cross: 'center', pad: [4, 24, 12, 24], gap: 8 });
  const ok = box(hd, { name: 'ikon sukses', w: 64, h: 64, r: 32, fill: t.okCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(ok, 'check', t.ok, 32, 3);
  txt(hd, 'BBM-26-000124 · 08 Nov 2026 10:21:05', { size: 13, color: t.muted, align: 'center', sh: 'fill' });
  const cd = box(f, { name: 'kartu struk', sh: 'fill', layout: 'vertical', pad: 16, gap: 8, r: 20, fill: t.surface });
  txt(cd, 'Pelanggan Kas · Tunai', { size: 13, weight: 'SemiBold', color: t.text });
  for (const [n, q, tot] of [['Pertalite', '10,00 L × Rp 10.000', 100000], ['Pelumas Mesin', '1 L × Rp 52.000', 52000]]) {
    const r = box(cd, { name: `baris ${n}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
    const c = box(r, { name: 'info', layout: 'vertical', gap: 1 });
    txt(c, n, { size: 14, weight: 'Medium', color: t.text });
    txt(c, q, { size: 12, color: t.muted });
    txt(r, rp(tot), { size: 14, color: t.text });
  }
  box(cd, { name: 'garis', sh: 'fill', h: 1, fill: t.outline });
  sum(cd, t, 'Bruto', rp(152000));
  sum(cd, t, 'Potongan', rp(0));
  sum(cd, t, 'Total', rp(152000), true);
  sum(cd, t, 'Bayar', rp(200000));
  sum(cd, t, 'Kembalian', rp(48000));
  box(f, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  const ac = box(f, { name: 'aksi', sh: 'fill', layout: 'vertical', pad: [0, 16, 24, 16], gap: 12 });
  button(ac, t, 'Cetak / Bagikan PDF', { kind: 'secondary', icon: 'print' });
  button(ac, t, 'Transaksi baru');
  return f;
}

export function bbmTablet(t, x, y) {
  const f = box(null, { name: `Kasir BBM - tablet (${t.mode})`, x, y, w: 1024, h: 768, fill: t.bg, layout: 'vertical', clip: true });
  fotoHeader(f, t, 1024, 92);
  appBar(f, t, 'Pilih BBM', { actions: ['scan', 'logout'] });
  const row = box(f, { name: 'dua panel', sh: 'fill', sv: 'fill', layout: 'horizontal' });
  const l = box(row, { name: 'panel BBM', sh: 'fill', sv: 'fill', layout: 'vertical' });
  const sb = box(l, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [0, 16, 12, 16] });
  searchBar(sb, t, 'Cari BBM atau pelumas', { scan: true });
  const ch = box(l, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 12, 16], gap: 8 });
  chip(ch, t, 'Semua', true); chip(ch, t, 'Bensin', false); chip(ch, t, 'Solar', false); chip(ch, t, 'Pelumas', false);
  const g = box(l, { name: 'grid BBM', sh: 'fill', sv: 'fill', layout: 'horizontal', wrap: true, gap: 12, pad: [0, 16, 0, 16], clip: true });
  for (const b of BBM) kartuBbm(g, t, b, 183);
  box(row, { name: 'pembatas', w: 1, sv: 'fill', fill: t.outline });
  const r = box(row, { name: 'panel keranjang', w: 400, sv: 'fill', layout: 'vertical', fill: t.bg });
  const ti = box(r, { name: 'judul', sh: 'fill', layout: 'horizontal', pad: [16, 16, 8, 16], cross: 'center' });
  const th = box(ti, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(th, t, 'Keranjang', '2 item', 28);
  txt(ti, 'Kosongkan', { size: 14, weight: 'SemiBold', color: t.error });
  const cl = box(r, { name: 'baris', sh: 'fill', sv: 'fill', layout: 'vertical', gap: 10, pad: [0, 16, 0, 16], clip: true });
  barisKeranjang(cl, t, BBM[0], 'Pertalite', '10,00 L', '10,00 L', 100000);
  barisKeranjang(cl, t, null, 'Pelumas Mesin', '1 L', '1 L', 52000);
  const ft = box(r, { name: 'ringkasan & bayar', sh: 'fill', layout: 'vertical', pad: [12, 16, 24, 16], gap: 8 });
  button(ft, t, 'Bayar', { split: rp(152000) });
  return f;
}
