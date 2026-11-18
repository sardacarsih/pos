import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { box, txt, icon, gambar, absRect } from './lib.mjs';

const FOTO = fileURLToPath(new URL('./assets/login_bg.webp', import.meta.url));
const grad = (t) => ({ type: 'linearGradient', stops: t.grad });
const PUTIH = '#FFFFFF', PUTIH_LEMBUT = '#FFFFFFB3', KACA = '#FFFFFF2E';

/** Strip foto kebun di belakang status bar dan app bar (tinggi 120), dengan overlay agar teks putih terbaca. */
export function fotoHeader(f, t, w = 375, h = 120) {
  const strip = box(f, { name: 'strip foto header', x: 0, y: 0, w, h, clip: true, abs: true, fill: { type: 'linearGradient', stops: ['#2F8FE8', '#BEE0F5'] } });
  if (existsSync(FOTO)) gambar(strip, FOTO, 0, 0, w, Math.round((w * 1844) / 853), 'fill', 'foto kebun');
  absRect(strip, 0, 0, w, h, '#0B2237', 0, t.fotoOverlay, 'overlay foto');
  return strip;
}
import { rp } from './theme.mjs';

export const PW = 375, PH = 812;

/** Bingkai ponsel + status bar. */
export function phone(t, name, x, y, w = PW, h = PH) {
  const f = box(null, { name: `${name} (${t.mode})`, x, y, w, h, fill: t.bg, layout: 'vertical', clip: true });
  fotoHeader(f, t, w);
  const sb = box(f, { name: 'status bar', sh: 'fill', h: 28, layout: 'horizontal', pad: [0, 24, 0, 24], main: 'spaceBetween', cross: 'center' });
  txt(sb, '9:41', { size: 12, weight: 'SemiBold', color: PUTIH });
  txt(sb, '5G  ▮▮▮', { size: 10, color: PUTIH });
  return f;
}

/** Judul serif dengan keterangan abu-abu, mis. "Keranjang, 3 item". */
export function head(parent, t, main, qual, size = 28, warna = {}) {
  const r = box(parent, { name: 'judul', layout: 'horizontal', cross: 'baseline', gap: 6 });
  txt(r, qual ? main + ',' : main, { size, color: warna.main ?? t.text, serif: true });
  if (qual) txt(r, qual, { size, color: warna.qual ?? t.muted, serif: true });
  return r;
}

/** App bar di atas foto (judul putih, tombol kaca) lalu tepi panel isi membulat yang menutup bawah foto. */
export function appBar(f, t, title, o = {}) {
  const b = box(f, { name: 'app bar', sh: 'fill', h: 64, layout: 'horizontal', pad: [0, 12, 0, o.back ? 8 : 20], cross: 'center', gap: 6 });
  if (o.back) {
    const bk = box(b, { name: 'tombol kembali', w: 44, h: 44, r: 22, layout: 'horizontal', main: 'center', cross: 'center', fill: KACA });
    icon(bk, 'back', PUTIH, 22);
  }
  const ti = box(b, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  head(ti, t, title, o.qual, 24, { main: PUTIH, qual: PUTIH_LEMBUT });
  for (const ic of o.actions ?? []) {
    const a = box(b, { name: 'aksi-' + ic, w: 44, h: 44, r: 22, layout: 'horizontal', main: 'center', cross: 'center', fill: KACA });
    icon(a, ic, PUTIH, 20);
  }
  box(f, { name: 'tepi panel', sh: 'fill', h: 28, r: [28, 28, 0, 0], fill: t.bg });
  return b;
}

/** Tombol pil. o.split = "Rp ..." menampilkan label terpisah garis (total | aksi) seperti referensi. */
export function button(parent, t, label, o = {}) {
  const kind = o.kind ?? 'primary';
  const fill = o.disabled ? t.surface2 : kind === 'primary' ? grad(t) : kind === 'danger' ? t.error : t.surface;
  const col = o.disabled ? t.muted : kind === 'primary' || kind === 'danger' ? t.onPrimary : t.accent;
  const b = box(parent, {
    name: `tombol ${label}`, sh: o.hug ? 'hug' : 'fill', h: o.h ?? 56, r: 28, layout: 'horizontal', main: 'center', cross: 'center', gap: 14,
    fill, stroke: kind === 'secondary' ? t.outline : undefined, pad: [0, 24, 0, 24],
  });
  if (o.icon) icon(b, o.icon, col, 20);
  if (o.split) {
    txt(b, o.split, { size: 17, weight: 'SemiBold', color: col, serif: true });
    box(b, { name: 'garis', w: 1, h: 24, fill: col, opacity: 0.45 });
  }
  txt(b, label, { size: 15, weight: 'SemiBold', color: col });
  return b;
}

export function field(parent, t, label, value, o = {}) {
  const f = box(parent, { name: `field ${label}`, sh: 'fill', layout: 'vertical', gap: 6 });
  txt(f, label, { size: o.labelSize ?? 12, weight: 'Medium', color: t.muted });
  const i = box(f, { name: 'input', sh: 'fill', h: o.h ?? 52, r: 18, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 8, fill: t.surface2, stroke: o.focus ? t.primary : t.outline, sw: o.focus ? 2 : 1 });
  txt(i, value, { size: 16, color: value.startsWith('•') || o.value ? t.text : t.muted, sh: 'fill' });
  if (o.trail) icon(i, o.trail, t.muted, 20);
  return f;
}

export function chip(parent, t, label, on) {
  const c = box(parent, { name: `chip ${label}`, h: 40, r: 20, layout: 'horizontal', pad: [0, 18, 0, 18], cross: 'center', gap: 6, fill: on ? grad(t) : t.surface, stroke: on ? undefined : t.outline });
  txt(c, label, { size: 14, weight: 'Medium', color: on ? t.onPrimary : t.text });
  return c;
}

export function searchBar(parent, t, text, o = {}) {
  const s = box(parent, { name: 'pencarian', sh: 'fill', h: 52, r: 26, layout: 'horizontal', pad: [0, 16, 0, 18], cross: 'center', gap: 10, fill: t.surface, stroke: t.outline });
  icon(s, 'search', t.muted, 20);
  txt(s, text, { size: 15, color: o.value ? t.text : t.muted, sh: 'fill' });
  if (o.scan) icon(s, 'scan', t.accent, 20);
  return s;
}

/** Thumbnail produk. Data produk tidak punya foto: tile berinisial (ganti foto bila nanti ada). */
export function thumb(parent, t, nama, size = 56) {
  const tl = box(parent, { name: 'thumbnail', w: size, h: size, r: 16, layout: 'horizontal', main: 'center', cross: 'center', fill: t.surface2 });
  txt(tl, nama.trim()[0].toUpperCase(), { size: size * 0.42, color: t.accent, serif: true });
  return tl;
}

export function addBtn(parent, t, size = 36) {
  const b = box(parent, { name: 'tombol tambah', w: size, h: size, r: size / 2, layout: 'horizontal', main: 'center', cross: 'center', fill: grad(t) });
  icon(b, 'plus', t.onPrimary, size * 0.5, 2.5);
  return b;
}

/** Baris barang: "Nama, 5 kg" (keterangan abu-abu), harga aksen, tombol + bundar. */
export function barang(parent, t, nama, sub, harga, stok, o = {}) {
  const r = box(parent, { name: `barang ${nama}`, sh: 'fill', layout: 'horizontal', pad: [10, 12, 10, 10], cross: 'center', gap: 12, r: 20, fill: t.surface });
  thumb(r, t, nama, 64);
  const col = box(r, { name: 'info', sh: 'fill', layout: 'vertical', gap: 3 });
  const [main, qual] = o.qual ? [nama, o.qual] : [nama, ''];
  const nm = box(col, { name: 'nama', sh: 'fill', layout: 'horizontal', cross: 'baseline', gap: 5, wrap: true });
  txt(nm, qual ? main + ',' : main, { size: 16, color: t.text, serif: true });
  if (qual) txt(nm, qual, { size: 16, color: t.muted, serif: true });
  txt(col, sub, { size: 12, color: t.muted, sh: 'fill' });
  const habis = stok.startsWith('Habis');
  const pr = box(col, { name: 'harga & stok', layout: 'horizontal', cross: 'center', gap: 8 });
  txt(pr, harga, { size: 16, weight: 'SemiBold', color: habis ? t.muted : t.accent });
  txt(pr, stok, { size: 12, color: habis ? t.error : t.muted });
  const ad = addBtn(r, t, 40);
  return r;
}

export function login(t, x, y) {
  const f = phone(t, 'Login', x, y);
  const body = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [56, 24, 24, 24], gap: 20 });
  const hd = box(body, { name: 'logo', sh: 'fill', layout: 'vertical', cross: 'center', gap: 14 });
  const lg = box(hd, { name: 'ikon toko', w: 76, h: 76, r: 28, fill: t.primaryCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(lg, 'cart', t.accent, 36);
  txt(hd, 'Kopkar Kusuma Lestari', { size: 28, color: t.text, align: 'center', sh: 'fill', serif: true });
  txt(hd, 'Masuk untuk melanjutkan', { size: 14, color: t.muted, align: 'center', sh: 'fill' });
  box(body, { name: 'jarak', sh: 'fill', h: 8 });
  field(body, t, 'Username', 'kasir.rina', { value: true, focus: true });
  field(body, t, 'Password', '••••••••••', { trail: 'eye' });
  button(body, t, 'Masuk');
  txt(body, 'Lupa password? Hubungi admin koperasi.', { size: 13, color: t.muted, align: 'center', sh: 'fill' });
  return f;
}

export function kasirList(t, x, y, o = {}) {
  const f = phone(t, o.offline ? 'Kasir - offline' : 'Kasir - daftar barang', x, y);
  if (o.offline) {
    const bn = box(f, { name: 'banner offline', sh: 'fill', h: 40, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 8, fill: t.warnCont });
    icon(bn, 'wifi', t.warn, 18);
    txt(bn, 'Tidak ada koneksi. Pembayaran dinonaktifkan.', { size: 13, weight: 'Medium', color: t.warn, sh: 'fill' });
  }
  appBar(f, t, 'Kasir', { actions: ['scan', 'logout'] });
  const sb = box(f, { name: 'bar cari', sh: 'fill', layout: 'vertical', pad: [0, 16, 12, 16] });
  searchBar(sb, t, 'Cari nama atau kode barang');
  const ch = box(f, { name: 'chip kategori', sh: 'fill', layout: 'horizontal', pad: [0, 16, 12, 16], gap: 8, clip: true });
  chip(ch, t, 'Semua', true); chip(ch, t, 'Sembako', false); chip(ch, t, 'Minuman', false); chip(ch, t, 'Snack', false);
  const ls = box(f, { name: 'daftar barang', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [0, 16, 0, 16], gap: 10, clip: true });
  barang(ls, t, 'Beras Premium', 'BR-0012 · Sembako', rp(68500), 'Stok 42', { qual: '5 kg' });
  barang(ls, t, 'Minyak Goreng', 'MG-0031 · Sembako', rp(36000), 'Stok 18', { qual: '2 L' });
  barang(ls, t, 'Gula Pasir', 'GP-0007 · Sembako', rp(17500), 'Stok 60', { qual: '1 kg' });
  barang(ls, t, 'Teh Celup', 'TC-0101 · Minuman', rp(9800), 'Stok 7', { qual: '25 pcs' });
  barang(ls, t, 'Kopi Sachet', 'KS-0044 · Minuman', rp(14500), 'Habis', { qual: '10 pcs' });
  barang(ls, t, 'Mie Instan Goreng', 'MI-0002 · Snack', rp(3500), 'Stok 120');
  const bar = box(f, { name: 'bar keranjang', sh: 'fill', layout: 'vertical', pad: [12, 16, 24, 16] });
  button(bar, t, 'Lihat keranjang · 3 item', { split: rp(122500), disabled: !!o.offline });
  return f;
}
