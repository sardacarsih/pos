// Layar tambahan yang mengikuti halaman Flutter aktif (dashboard, belanja, admin, profil).
import { box, txt, icon } from './lib.mjs';
import { rp } from './theme.mjs';
import { phone, appBar, button, field, chip, searchBar, thumb } from './screens_a.mjs';
import { loginHutan } from './screens_login.mjs';

const grad = (t) => ({ type: 'linearGradient', stops: t.grad });
const nav = (f, t, labels, active) => {
  const n = box(f, { name: 'navigasi bawah', sh: 'fill', h: 68, layout: 'horizontal', main: 'spaceAround', cross: 'center', fill: t.surface, stroke: t.outline });
  const icons = ['home', 'cart', 'receipt', 'grid'];
  labels.forEach((label, i) => {
    const c = box(n, { name: `tab ${label}`, sh: 'fill', layout: 'vertical', main: 'center', cross: 'center', gap: 3 });
    icon(c, icons[i] ?? 'grid', i === active ? t.accent : t.muted, 19);
    txt(c, label, { size: 10, weight: i === active ? 'SemiBold' : 'Regular', color: i === active ? t.accent : t.muted, align: 'center' });
  });
};
const card = (p, name, t, pad = 14) => box(p, { name, sh: 'fill', layout: 'vertical', pad, gap: 8, r: 18, fill: t.surface, stroke: t.outline });
const row = (p, name, t, a, b, right) => {
  const r = box(p, { name, sh: 'fill', layout: 'horizontal', pad: [10, 12, 10, 12], gap: 10, cross: 'center', r: 16, fill: t.surface });
  const c = box(r, { name: 'detail', sh: 'fill', layout: 'vertical', gap: 3 });
  txt(c, a, { size: 14, weight: 'SemiBold', color: t.text, sh: 'fill' });
  txt(c, b, { size: 11, color: t.muted, sh: 'fill' });
  if (right) txt(r, right, { size: 13, weight: 'SemiBold', color: t.accent });
  return r;
};
const metric = (p, t, label, val) => {
  const c = box(p, { name: `metrik ${label}`, sh: 'fill', layout: 'vertical', pad: 11, gap: 4, r: 14, fill: t.surface, stroke: t.outline });
  txt(c, label, { size: 10, color: t.muted, sh: 'fill' });
  txt(c, val, { size: 15, weight: 'Bold', color: t.text, sh: 'fill' });
};
const title = (p, t, s) => txt(p, s, { size: 17, weight: 'SemiBold', color: t.text, sh: 'fill' });
const content = (f, t, pad = [6, 16, 16, 16]) => box(f, { name: 'konten', sh: 'fill', sv: 'fill', layout: 'vertical', pad, gap: 10, clip: true });

export function loginHelpDialog(t, x, y) {
  const f = loginHutan(t, x, y, { dialog: true });
  const shade = box(f, { name: 'scrim bantuan login', x: 0, y: 0, w: 375, h: 812, fill: t.scrim, abs: true });
  const d = box(shade, { name: 'dialog lupa password', x: 22, y: 340, w: 331, layout: 'vertical', pad: 20, gap: 12, r: 24, fill: t.surface });
  title(d, t, 'Lupa password?');
  txt(d, 'Hubungi admin koperasi untuk meminta reset password akun Anda.', { size: 13, color: t.muted, sh: 'fill' });
  button(d, t, 'Mengerti', { h: 44 });
  return f;
}

export function dashboardKasir(t, x, y) {
  const f = phone(t, 'Kasir - dashboard', x, y);
  appBar(f, t, 'Dashboard', { actions: ['user', 'logout'] });
  const c = content(f, t, [4, 16, 8, 16]);
  txt(c, 'Selamat pagi, Rina', { size: 20, weight: 'Bold', color: t.text, sh: 'fill' });
  const period = card(c, 'status periode', t, 12);
  txt(period, 'PERIODE AKTIF · 01–30 NOV 2026', { size: 11, weight: 'SemiBold', color: t.accent, sh: 'fill' });
  txt(period, 'Transaksi bisa dilanjutkan', { size: 12, color: t.text, sh: 'fill' });
  const total = card(c, 'total penjualan', t, 14);
  txt(total, 'Penjualan hari ini', { size: 12, color: t.muted });
  txt(total, rp(2450000), { size: 26, weight: 'Bold', color: t.text });
  const ms = box(c, { name: 'metrik ringkas', sh: 'fill', layout: 'horizontal', gap: 8 });
  metric(ms, t, 'Transaksi', '18'); metric(ms, t, 'Tunai', rp(1_180_000)); metric(ms, t, 'Kredit', rp(1_270_000));
  button(c, t, 'Mulai jual', { icon: 'cart', h: 52 });
  title(c, t, 'Transaksi terakhir');
  row(c, 'transaksi terakhir 1', t, 'Budi Santoso', 'Kredit · 10:42 · INV-261117-018', rp(152000));
  row(c, 'transaksi terakhir 2', t, 'Pelanggan Kas', 'Tunai · 10:15 · INV-261117-017', rp(68500));
  nav(f, t, ['Dashboard', 'Jual', 'Riwayat'], 0);
  return f;
}

export function dashboardKasirKosong(t, x, y, error = false) {
  const f = phone(t, error ? 'Kasir - dashboard gagal memuat' : 'Kasir - dashboard kosong', x, y);
  appBar(f, t, 'Dashboard', { actions: ['user', 'logout'] });
  const c = content(f, t);
  const p = card(c, error ? 'status gagal' : 'status tanpa transaksi', t, 20);
  icon(p, error ? 'alert' : 'receipt', error ? t.error : t.accent, 30);
  title(p, t, error ? 'Ringkasan belum tersedia' : 'Belum ada transaksi hari ini');
  txt(p, error ? 'Periksa koneksi lalu muat ulang data.' : 'Transaksi yang tersimpan hari ini akan tampil di sini.', { size: 13, color: t.muted, sh: 'fill' });
  if (error) button(p, t, 'Coba lagi', { kind: 'secondary', h: 44 });
  button(c, t, 'Mulai jual', { icon: 'cart' });
  nav(f, t, ['Dashboard', 'Jual', 'Riwayat'], 0);
  return f;
}

export function dashboardKasirPeriodeTutup(t, x, y) {
  const f = phone(t, 'Kasir - periode belum dibuka', x, y);
  appBar(f, t, 'Dashboard', { actions: ['user', 'logout'] });
  const c = content(f, t, [4, 16, 8, 16]);
  txt(c, 'Selamat pagi, Rina', { size: 20, weight: 'Bold', color: t.text, sh: 'fill' });
  const period = card(c, 'periode tutup', t, 14);
  txt(period, 'PERIODE BELUM DIBUKA', { size: 11, weight: 'SemiBold', color: t.error, sh: 'fill' });
  txt(period, 'Transaksi belum dapat dilakukan. Hubungi admin untuk informasi periode.', { size: 12, color: t.text, sh: 'fill' });
  const total = card(c, 'ringkasan tidak tersedia', t, 14);
  txt(total, 'Ringkasan penjualan', { size: 12, color: t.muted });
  txt(total, 'Belum tersedia', { size: 20, weight: 'Bold', color: t.muted });
  button(c, t, 'Mulai jual', { icon: 'cart', disabled: true, h: 50 });
  nav(f, t, ['Dashboard', 'Jual', 'Riwayat'], 0);
  return f;
}

export function riwayatKasir(t, x, y, empty = false) {
  const f = phone(t, empty ? 'Kasir - riwayat kosong' : 'Kasir - riwayat', x, y);
  appBar(f, t, 'Riwayat', { actions: ['user'] });
  const date = box(f, { name: 'pilih tanggal riwayat', sh: 'fill', h: 48, layout: 'horizontal', pad: [0, 8, 0, 8], cross: 'center', gap: 4 });
  icon(date, 'back', t.accent, 18);
  const pick = box(date, { name: 'tanggal aktif', sh: 'fill', h: 40, layout: 'horizontal', main: 'center', cross: 'center', gap: 7, r: 12, fill: t.surface, stroke: t.outline });
  icon(pick, 'grid', t.accent, 16);
  txt(pick, '17 Nov 2026 · Hari ini', { size: 12, weight: 'SemiBold', color: t.text });
  icon(date, 'chevR', t.muted, 18);
  const c = content(f, t, [4, 16, 12, 16]);
  if (empty) {
    const e = card(c, 'riwayat kosong', t, 24);
    icon(e, 'receipt', t.muted, 32); title(e, t, 'Transaksi belum ditemukan');
    txt(e, 'Coba ubah kata pencarian atau rentang tanggal.', { size: 13, color: t.muted, sh: 'fill' });
  } else {
    row(c, 'faktur satu', t, 'Budi Santoso', 'INV-261117-018 · 17 Nov, 10:42 · KREDIT', rp(152000));
    row(c, 'faktur dua', t, 'Pelanggan Kas', 'INV-261117-017 · 17 Nov, 10:15 · TUNAI', rp(68500));
    row(c, 'faktur tiga', t, 'Siti Aminah', 'INV-261117-016 · 17 Nov, 09:50 · KREDIT', rp(245000));
    txt(c, 'Tarik ke bawah untuk memuat ulang', { size: 11, color: t.muted, align: 'center', sh: 'fill' });
  }
  nav(f, t, ['Dashboard', 'Jual', 'Riwayat'], 2);
  return f;
}

export function printerDialog(t, x, y, state = 'daftar') {
  const f = phone(t, `Kasir - printer Bluetooth ${state}`, x, y);
  appBar(f, t, 'Faktur Tersimpan', { back: true });
  const overlay = box(f, { name: 'scrim dialog', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(overlay, { name: 'dialog printer', x: 20, y: 260, w: 335, layout: 'vertical', pad: 20, gap: 10, r: 24, fill: t.surface });
  title(d, t, state === 'daftar' ? 'Pilih printer Bluetooth' : 'Printer belum tersedia');
  if (state === 'daftar') {
    row(d, 'printer terpasang', t, 'POS-58 Thermal', '00:11:22:AA:BB:CC', null);
    row(d, 'printer terpasang 2', t, 'RPP02N', 'A4:CF:12:11:82:09', null);
    button(d, t, 'Batal', { kind: 'secondary', h: 44 });
  } else {
    txt(d, 'Pasangkan printer thermal di pengaturan Bluetooth Android terlebih dahulu.', { size: 13, color: t.muted, sh: 'fill' });
    button(d, t, 'Mengerti', { h: 44 });
  }
  return f;
}

export function kalenderRiwayat(t, x, y) {
  const f = phone(t, 'Kasir - pilih tanggal riwayat', x, y);
  appBar(f, t, 'Riwayat', { back: true });
  const shade = box(f, { name: 'scrim kalender', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: 'pemilih tanggal', x: 20, y: 170, w: 335, layout: 'vertical', pad: 18, gap: 10, r: 24, fill: t.surface });
  txt(d, 'PILIH TANGGAL', { size: 10, weight: 'SemiBold', color: t.muted, ls: 1 });
  txt(d, 'Selasa, 17 November 2026', { size: 21, weight: 'Bold', color: t.text, sh: 'fill' });
  const month = box(d, { name: 'bulan kalender', sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
  icon(month, 'back', t.accent, 16); txt(month, 'November 2026', { size: 14, weight: 'SemiBold', color: t.text }); icon(month, 'chevR', t.accent, 16);
  const cal = box(d, { name: 'grid kalender', sh: 'fill', layout: 'vertical', gap: 4 });
  for (const vals of [['S','S','R','K','J','S','M'], ['–','–','–','–','–','–','1'], ['2','3','4','5','6','7','8'], ['9','10','11','12','13','14','15'], ['16','17','18','19','20','21','22'], ['23','24','25','26','27','28','29'], ['30','–','–','–','–','–','–']]) {
    const line = box(cal, { name: 'baris kalender', sh: 'fill', layout: 'horizontal', main: 'spaceAround' });
    vals.forEach((v) => {
      const cell = box(line, { name: `tanggal ${v}`, w: 32, h: 30, r: 16, layout: 'horizontal', main: 'center', cross: 'center', fill: v === '17' ? t.accent : 'none' });
      txt(cell, v, { size: 10, color: v === '17' ? '#FFFFFF' : v === '–' ? t.outline : t.text });
    });
  }
  button(d, t, 'Batal', { kind: 'secondary', h: 40 });
  button(d, t, 'Pilih tanggal', { h: 40 });
  return f;
}

export function dashboardAnggota(t, x, y, empty = false) {
  const tutup = empty === 'closed';
  const f = phone(t, tutup ? 'Anggota - periode belum dibuka' : empty ? 'Anggota - dashboard tanpa transaksi' : 'Anggota - dashboard', x, y);
  appBar(f, t, 'Dashboard Anggota', { actions: ['user', 'logout'] });
  const c = content(f, t, [4, 16, 8, 16]);
  txt(c, 'Halo, Budi Santoso', { size: 19, weight: 'Bold', color: t.text, sh: 'fill' });
  const p = card(c, 'ringkasan limit', t, 16);
  txt(p, 'Sisa limit periode', { size: 12, color: t.muted });
  txt(p, rp(1_250_000), { size: 28, weight: 'Bold', color: t.accent });
  txt(p, 'Terpakai Rp 750.000 dari Rp 2.000.000', { size: 12, color: t.muted });
  const bar = box(p, { name: 'progress penggunaan limit', sh: 'fill', h: 8, r: 4, fill: t.surface2 });
  box(bar, { name: 'progress 38 persen', w: 122, h: 8, r: 4, fill: grad(t) });
  if (tutup) {
    const lock = card(c, 'periode transaksi tertutup', t, 12);
    icon(lock, 'lock', t.warn, 20);
    txt(lock, 'Belanja periode belum dibuka. Hubungi admin untuk informasi periode.', { size: 12, color: t.text, sh: 'fill' });
  }
  const actions = box(c, { name: 'aksi anggota', sh: 'fill', layout: 'horizontal', gap: 8 });
  button(actions, t, 'Belanja Mandiri', { icon: 'cart', h: 48, disabled: tutup });
  button(actions, t, 'Kartu Anggota', { kind: 'secondary', icon: 'qr', h: 48 });
  title(c, t, 'Transaksi terakhir');
  if (empty && !tutup) {
    const e = card(c, 'belum ada transaksi', t, 20);
    icon(e, 'receipt', t.muted, 28); txt(e, 'Belum ada transaksi pada periode ini.', { size: 13, color: t.muted, sh: 'fill' });
  } else {
    row(c, 'anggota transaksi satu', t, rp(152000), '17 Nov 2026 · INV-261117-018 · 3x angsuran', '›');
    row(c, 'anggota transaksi dua', t, rp(85000), '15 Nov 2026 · INV-261115-007', '›');
  }
  nav(f, t, ['Dashboard', 'Belanja', 'Riwayat'], 0);
  return f;
}

export function belanjaAnggota(t, x, y, o = {}) {
  const state = o.error ? ' - daftar gagal dimuat' : o.empty ? ' - barang tidak ditemukan' : o.demo ? ' - mode demo' : '';
  const f = phone(t, `Anggota - belanja mandiri${state}`, x, y);
  appBar(f, t, 'Belanja Mandiri', { actions: ['user'] });
  if (o.demo) {
    const b = box(f, { name: 'banner mode demo', sh: 'fill', h: 32, layout: 'horizontal', main: 'center', cross: 'center', fill: t.warnCont });
    txt(b, 'MODE DEMO · TRANSAKSI TIDAK DISIMPAN', { size: 10, weight: 'Bold', color: t.warn });
  }
  const s = box(f, { name: 'pencarian belanja', sh: 'fill', layout: 'vertical', pad: [4, 16, 8, 16] });
  searchBar(s, t, 'Cari barang');
  const ch = box(f, { name: 'kategori belanja', sh: 'fill', layout: 'horizontal', gap: 8, pad: [0, 16, 8, 16], clip: true });
  chip(ch, t, 'Semua', true); chip(ch, t, 'BBM', false); chip(ch, t, 'Pelumas', false); chip(ch, t, 'ATK', false);
  const c = content(f, t, [0, 16, 8, 16]);
  if (o.error) {
    const e = card(c, 'belanja gagal dimuat', t, 14);
    icon(e, 'alert', t.error, 24); title(e, t, 'Barang gagal dimuat');
    txt(e, 'Periksa koneksi internet dan coba lagi.', { size: 12, color: t.muted, sh: 'fill' });
    button(e, t, 'Coba lagi', { kind: 'secondary', h: 42 });
  } else if (o.empty) {
    const e = card(c, 'belanja kosong', t, 14);
    icon(e, 'search', t.muted, 24); title(e, t, 'Barang tidak ditemukan');
    txt(e, 'Coba kata kunci atau kategori lain.', { size: 12, color: t.muted, sh: 'fill' });
  }
  if (!o.error && !o.empty) {
  const products = [['Pertalite', rp(10000), 'liter'], ['Pertamax', rp(12950), 'liter'], ['Oli Mesin 1L', rp(65000), 'botol']];
  products.forEach(([n, p, unit], i) => {
    const r = box(c, { name: `produk belanja ${i + 1}`, sh: 'fill', layout: 'horizontal', pad: 8, gap: 10, cross: 'center', r: 16, fill: t.surface });
    thumb(r, t, n, 52);
    const d = box(r, { name: 'info produk', sh: 'fill', layout: 'vertical', gap: 3 });
    txt(d, n, { size: 14, weight: 'SemiBold', color: t.text, sh: 'fill' });
    txt(d, `${p} / ${unit}`, { size: 11, color: t.muted, sh: 'fill' });
    const a = box(r, { name: 'tambah produk', w: 38, h: 38, r: 19, layout: 'horizontal', main: 'center', cross: 'center', fill: grad(t) });
    icon(a, 'plus', t.onPrimary, 18);
  });
  }
  if (!o.error && !o.empty && o.withCart !== false) {
    const b = card(c, 'ringkasan belanja mandiri', t, 10);
    txt(b, '2 barang · perkiraan Rp 32.950', { size: 12, color: t.text, sh: 'fill' });
    txt(b, 'Total akhir & limit dihitung server', { size: 10, color: t.muted, sh: 'fill' });
    button(b, t, 'Lihat keranjang', { h: 44, split: rp(32950) });
  }
  nav(f, t, ['Dashboard', 'Belanja', 'Riwayat'], 1);
  return f;
}

export function belanjaCheckout(t, x, y) {
  const f = phone(t, 'Anggota - keranjang dan tenor', x, y);
  appBar(f, t, 'Belanja Mandiri', { back: true });
  const c = content(f, t, [8, 16, 16, 16]);
  title(c, t, 'Keranjang · 2 barang');
  row(c, 'barang checkout pertalite', t, 'Pertalite · 2 liter', `${rp(10000)} / liter`, rp(20000));
  row(c, 'barang checkout oli', t, 'Oli Mesin · 1 botol', `${rp(65000)} / botol`, rp(65000));
  const ten = card(c, 'pilih tenor', t, 12);
  txt(ten, 'Tenor (1 = sekali bayar)', { size: 11, color: t.muted });
  txt(ten, '3x angsuran                         ⌄', { size: 14, color: t.text });
  const sum = card(c, 'total perkiraan', t, 14);
  txt(sum, 'Perkiraan total', { size: 11, color: t.muted });
  txt(sum, rp(85000), { size: 24, weight: 'Bold', color: t.text });
  txt(sum, 'Limit hutang & jumlah akhir diperiksa server saat simpan.', { size: 11, color: t.muted, sh: 'fill' });
  button(c, t, 'Belanja dengan Kredit', { h: 52 });
  nav(f, t, ['Dashboard', 'Belanja', 'Riwayat'], 1);
  return f;
}

export function belanjaDemoDialog(t, x, y) {
  const f = phone(t, 'Anggota - konfirmasi transaksi demo', x, y);
  appBar(f, t, 'Faktur Tersimpan', { back: true });
  const shade = box(f, { name: 'scrim dialog demo', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: 'dialog mode demo', x: 22, y: 284, w: 331, layout: 'vertical', pad: 20, gap: 12, r: 24, fill: t.surface });
  title(d, t, 'Mode DEMO');
  txt(d, 'Belanja ini hanya simulasi: faktur tidak tersimpan, limit hutang tidak berubah, dan transaksi tidak muncul di Riwayat.', { size: 13, color: t.muted, sh: 'fill' });
  button(d, t, 'Mengerti', { h: 46 });
  return f;
}

export function dashboardAdmin(t, x, y, empty = false) {
  const f = phone(t, empty ? 'Admin - dashboard tanpa transaksi' : 'Admin - dashboard', x, y);
  appBar(f, t, 'Dashboard Admin', { actions: ['user', 'logout'] });
  const c = content(f, t, [4, 16, 8, 16]);
  txt(c, 'Ringkasan koperasi', { size: 20, weight: 'Bold', color: t.text, sh: 'fill' });
  const per = card(c, 'periode admin', t, 10);
  txt(per, 'PERIODE AKTIF · NOVEMBER 2026', { size: 10, weight: 'SemiBold', color: t.accent, sh: 'fill' });
  txt(per, '17 Nov 2026 · 01–30 Nov', { size: 12, color: t.text });
  if (empty) {
    const e = card(c, 'dashboard admin kosong', t, 16);
    icon(e, 'receipt', t.muted, 28); title(e, t, 'Belum ada transaksi hari ini');
    txt(e, 'Ringkasan penjualan, kasir, dan produk terlaris akan tampil setelah transaksi tersimpan.', { size: 12, color: t.muted, sh: 'fill' });
  } else {
    const total = card(c, 'total penjualan semua kasir', t, 12);
    txt(total, 'Penjualan hari ini · semua kasir', { size: 11, color: t.muted });
    txt(total, rp(12500000), { size: 24, weight: 'Bold', color: t.text });
    const m = box(c, { name: 'metrik admin', sh: 'fill', layout: 'horizontal', gap: 6 });
    metric(m, t, 'Transaksi', '86'); metric(m, t, 'Tunai', rp(5_000_000)); metric(m, t, 'Kredit', rp(7_500_000));
    title(c, t, 'Penjualan per kasir');
    row(c, 'kasir rina', t, 'Rina · kasir.rina', '28 transaksi', rp(4_100_000));
    row(c, 'kasir doni', t, 'Doni · kasir.doni', '21 transaksi', rp(3_250_000));
    title(c, t, 'Barang terlaris');
    row(c, 'produk terlaris', t, 'Pertalite · BBM001', 'Terjual 412 liter', rp(4_120_000));
    title(c, t, 'User mobile');
    txt(c, 'Admin 2  ·  Kasir 6  ·  Anggota 248  ·  Nonaktif 3  ·  Terkunci 1', { size: 10, color: t.muted, sh: 'fill' });
  }
  nav(f, t, ['Dashboard', 'Produk', 'Rekap', 'User'], 0);
  return f;
}

export function adminProduk(t, x, y, categories = false, empty = false) {
  const f = phone(t, categories ? 'Admin - kategori yang tampil di kasir' : empty ? 'Admin - produk tidak ditemukan' : 'Admin - daftar produk', x, y);
  appBar(f, t, categories ? 'Admin · Produk' : 'Admin · Produk', { actions: ['user', 'logout'] });
  const tabs = box(f, { name: 'tab produk kategori', sh: 'fill', h: 44, layout: 'horizontal', main: 'spaceAround', cross: 'center', fill: t.surface });
  txt(tabs, 'Produk', { size: 13, weight: categories ? 'Regular' : 'SemiBold', color: categories ? t.muted : t.accent });
  txt(tabs, 'Kategori', { size: 13, weight: categories ? 'SemiBold' : 'Regular', color: categories ? t.accent : t.muted });
  const c = content(f, t, [8, 16, 12, 16]);
  if (categories) {
    txt(c, 'Pilih kategori untuk katalog kasir. Kosong berarti semua kategori tampil.', { size: 11, color: t.muted, sh: 'fill' });
    txt(c, '4 dari 6 kategori tampil                 Pilih semua · Kosongkan', { size: 10, weight: 'SemiBold', color: t.accent, sh: 'fill' });
    for (const [name, count, selected] of [['BBM', '6 produk', true], ['Pelumas', '8 produk', true], ['Sembako', '12 produk', true], ['Minuman', '14 produk', true], ['ATK', '4 produk', false], ['Lainnya', '3 produk', false]]) {
      const r = box(c, { name: `pilih kategori ${name}`, sh: 'fill', layout: 'horizontal', pad: [6, 12, 6, 12], gap: 10, cross: 'center', r: 14, fill: t.surface });
      const check = box(r, { name: selected ? 'terpilih' : 'tidak terpilih', w: 22, h: 22, r: 6, fill: selected ? t.accent : t.surface, stroke: selected ? t.accent : t.outline });
      if (selected) icon(check, 'check', '#FFFFFF', 15, 2.4);
      const detail = box(r, { name: 'detail kategori', sh: 'fill', layout: 'vertical', gap: 2 });
      txt(detail, name, { size: 14, weight: 'SemiBold', color: t.text });
      txt(detail, count, { size: 11, color: t.muted });
    }
    button(c, t, 'Atur urutan produk', { kind: 'secondary', h: 40 });
    button(c, t, 'Simpan pengaturan', { h: 44 });
  } else {
    searchBar(c, t, 'Cari nama, kode, atau barcode');
    const filters = box(c, { name: 'filter katalog', sh: 'fill', layout: 'horizontal', gap: 6, clip: true });
    chip(filters, t, 'Semua', true); chip(filters, t, 'Aktif', false); chip(filters, t, 'Nonaktif', false);
    row(c, 'produk pertalite', t, 'Pertalite · BBM001', `${rp(10000)} / liter · Aktif`, '⋮');
    row(c, 'produk pertamax', t, 'Pertamax · BBM002', `${rp(12950)} / liter · Aktif`, '⋮');
    row(c, 'produk pelumas', t, 'Oli Mesin 1L · PLS015', `${rp(65000)} / botol · Aktif`, '⋮');
  }
  if (empty) {
    const e = card(c, 'katalog kosong', t, 18);
    txt(e, categories ? 'Belum ada kategori.' : 'Produk tidak ditemukan.', { size: 13, color: t.muted, sh: 'fill' });
  } else if (!categories) {
    txt(c, 'Pilih produk untuk mengubah detail, foto, atau status aktifnya.', { size: 11, color: t.muted, sh: 'fill' });
  }
  nav(f, t, ['Dashboard', 'Produk', 'Rekap', 'User'], 1);
  return f;
}

export function adminFormProduk(t, x, y) {
  const f = phone(t, 'Admin - dialog ubah produk', x, y);
  appBar(f, t, 'Admin · Produk', { actions: ['user'] });
  const shade = box(f, { name: 'scrim form produk', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: 'dialog edit produk scroll', x: 20, y: 0, w: 335, h: 660, layout: 'vertical', pad: 16, gap: 4, r: 22, fill: t.surface, clip: true });
  title(d, t, 'Ubah produk · BBM001');
  txt(d, 'Foto produk', { size: 10, color: t.muted, align: 'center', sh: 'fill' });
  button(d, t, 'Ganti foto   ·   Hapus foto', { kind: 'secondary', h: 32 });
  field(d, t, 'Nama', 'Pertalite', { value: true, h: 44, labelSize: 11 });
  field(d, t, 'Barcode', '8999999500012', { value: true, h: 44, labelSize: 11 });
  field(d, t, 'Satuan', 'liter', { value: true, h: 44, labelSize: 11 });
  field(d, t, 'Kategori', 'BBM                         ⌄', { value: true, h: 44, labelSize: 11 });
  field(d, t, 'Harga jual (Rp)', '10.000', { value: true, h: 44, labelSize: 11 });
  field(d, t, 'Harga beli (Rp)', '9.300', { value: true, h: 44, labelSize: 11 });
  txt(d, '● Aktif (tampil di kasir)', { size: 12, weight: 'SemiBold', color: t.accent });
  const actions = box(d, { name: 'aksi dialog produk tetap', x: 16, y: 580, w: 303, h: 56, layout: 'horizontal', gap: 8, abs: true, fill: t.surface });
  button(actions, t, 'Batal', { kind: 'secondary', h: 44 });
  button(actions, t, 'Simpan', { h: 44 });
  return f;
}

export function adminUrutanProduk(t, x, y) {
  const f = phone(t, 'Admin - atur urutan produk', x, y);
  appBar(f, t, 'Urutan produk', { back: true });
  const c = content(f, t, [8, 16, 16, 16]);
  txt(c, 'Seret produk untuk mengubah urutan pada katalog kasir.', { size: 12, color: t.muted, sh: 'fill' });
  row(c, 'produk urut satu', t, 'Pertalite', 'BBM001 · liter', '☷');
  row(c, 'produk urut dua', t, 'Pertamax', 'BBM002 · liter', '☷');
  row(c, 'produk urut tiga', t, 'Pertamax Turbo', 'BBM003 · liter', '☷');
  row(c, 'produk urut empat', t, 'Dexlite', 'BBM004 · liter', '☷');
  box(c, { name: 'pengisi urutan', sh: 'fill', sv: 'fill' });
  button(c, t, 'Simpan urutan');
  return f;
}

export function adminPhotoOrPrice(t, x, y, variant = 'price') {
  const photo = variant === 'photo';
  const f = phone(t, photo ? 'Admin - dialog sumber foto produk' : 'Admin - konfirmasi perubahan harga', x, y);
  appBar(f, t, 'Admin · Produk');
  const shade = box(f, { name: `scrim dialog ${variant}`, sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: `dialog ${variant}`, x: 20, y: photo ? 490 : 284, w: 335, layout: 'vertical', pad: 20, gap: 10, r: 24, fill: t.surface });
  if (photo) {
    title(d, t, 'Pilih sumber foto');
    row(d, 'sumber galeri', t, 'Pilih dari galeri', 'Gunakan foto dari perangkat', '›');
    row(d, 'sumber kamera', t, 'Ambil dengan kamera', 'Foto produk baru', '›');
  } else {
    title(d, t, 'Ubah harga jual?');
    txt(d, 'Pertalite', { size: 13, weight: 'SemiBold', color: t.text });
    txt(d, `${rp(10000)}  →  ${rp(10500)}`, { size: 18, weight: 'Bold', color: t.accent, sh: 'fill' });
    txt(d, 'Harga baru akan dipakai pada penjualan berikutnya.', { size: 12, color: t.muted, sh: 'fill' });
    button(d, t, 'Batal', { kind: 'secondary', h: 44 });
    button(d, t, 'Ya, ubah', { h: 44 });
  }
  return f;
}

export function adminRekap(t, x, y, empty = false) {
  const f = phone(t, empty ? 'Admin - rekap tanpa transaksi' : 'Admin - rekap anggota', x, y);
  appBar(f, t, 'Admin · Rekap', { actions: ['user', 'logout'] });
  const p = box(f, { name: 'navigasi periode', sh: 'fill', h: 48, layout: 'horizontal', main: 'spaceBetween', cross: 'center', pad: [0, 16, 0, 16] });
  icon(p, 'back', t.accent, 18); txt(p, 'November 2026', { size: 14, weight: 'SemiBold', color: t.text }); icon(p, 'chevR', t.accent, 18);
  const c = content(f, t, [2, 16, 12, 16]);
  searchBar(c, t, 'Cari nama atau NIK');
  const sum = card(c, 'total rekap periode', t, 12);
  txt(sum, 'Total belanja kredit anggota', { size: 11, color: t.muted });
  txt(sum, rp(85_500_000), { size: 24, weight: 'Bold', color: t.text });
  txt(sum, '248 anggota · 1.024 transaksi · 01–30 Nov', { size: 11, color: t.muted });
  if (empty) txt(c, 'Belum ada transaksi anggota pada periode ini.', { size: 13, color: t.muted, align: 'center', sh: 'fill' });
  else {
    row(c, 'rekap budi', t, 'Budi Santoso', '12.34.0021 · Unit Produksi · 12 transaksi', `${rp(750000)}\nSisa ${rp(1250000)}`);
    row(c, 'rekap siti', t, 'Siti Aminah', '12.34.0042 · Unit Gudang · 8 transaksi', `${rp(510000)}\nSisa ${rp(1490000)}`);
    row(c, 'rekap limit lewat', t, 'Doni Saputra', '12.34.0068 · Unit Kantor · 19 transaksi', `${rp(2250000)}\nLewat ${rp(250000)}`);
  }
  nav(f, t, ['Dashboard', 'Produk', 'Rekap', 'User'], 2);
  return f;
}

export function adminRekapDetail(t, x, y, over = false) {
  const f = phone(t, over ? 'Admin - detail rekap limit terlampaui' : 'Admin - detail rekap anggota', x, y);
  appBar(f, t, 'Detail Anggota', { back: true });
  const c = content(f, t, [8, 16, 14, 16]);
  const p = card(c, 'identitas anggota rekap', t, 14);
  txt(p, 'Budi Santoso', { size: 18, weight: 'Bold', color: t.text });
  txt(p, '12.34.0021 · Unit Produksi', { size: 12, color: t.muted });
  txt(p, 'Periode November 2026', { size: 11, color: t.muted });
  const l = card(c, 'limit anggota rekap', t, 14);
  txt(l, 'Total belanja kredit', { size: 11, color: t.muted });
  txt(l, rp(over ? 2_250_000 : 750_000), { size: 22, weight: 'Bold', color: over ? t.error : t.text });
  txt(l, over ? `Melebihi limit ${rp(250000)}` : `Sisa limit ${rp(1250000)} dari ${rp(2000000)}`, { size: 12, color: over ? t.error : t.accent });
  title(c, t, 'Transaksi anggota');
  row(c, 'detail rekap faktur 1', t, 'INV-261117-018', '17 Nov · 3x angsuran', rp(152000));
  row(c, 'detail rekap faktur 2', t, 'INV-261115-007', '15 Nov · 1x', rp(85000));
  return f;
}

export function adminUsersError(t, x, y) {
  const f = phone(t, 'Admin - user gagal dimuat', x, y);
  appBar(f, t, 'Admin · User Mobile', { actions: ['user', 'logout'] });
  const c = content(f, t);
  const e = card(c, 'gagal memuat user', t, 18);
  icon(e, 'alert', t.error, 28); title(e, t, 'Daftar user gagal dimuat');
  txt(e, 'Periksa koneksi internet lalu coba muat ulang.', { size: 12, color: t.muted, sh: 'fill' });
  button(e, t, 'Coba lagi', { kind: 'secondary', h: 44 });
  nav(f, t, ['Dashboard', 'Produk', 'Rekap', 'User'], 3);
  return f;
}

export function profil(t, x, y, options = {}) {
  const f = phone(t, options.password ? 'Umum - profil dan ganti password' : 'Umum - profil saya', x, y);
  appBar(f, t, 'Profil Saya', { back: true });
  const c = content(f, t, [8, 16, 16, 16]);
  const av = box(c, { name: 'avatar profil', w: 88, h: 88, r: 44, layout: 'horizontal', main: 'center', cross: 'center', fill: t.primaryCont });
  icon(av, 'user', t.accent, 40);
  txt(c, 'Ketuk foto untuk mengubah', { size: 11, color: t.muted, align: 'center', sh: 'fill' });
  txt(c, 'rina.kasir · KASIR', { size: 11, color: t.muted, align: 'center', sh: 'fill' });
  if (options.password) {
    title(c, t, 'Ganti password');
    txt(c, 'Gunakan password baru minimal 8 karakter.', { size: 11, color: t.muted, sh: 'fill' });
    field(c, t, 'Password lama', '••••••••', { value: true, trail: 'eye' });
    field(c, t, 'Password baru (min. 8 karakter)', '••••••••', { value: true, trail: 'eye' });
    field(c, t, 'Ulangi password baru', '••••••••', { value: true, trail: 'eye' });
    if (options.error) txt(c, 'Konfirmasi password tidak sama.', { size: 12, color: t.error, sh: 'fill' });
    button(c, t, 'Ganti password', { h: 44 });
  } else {
    field(c, t, 'Nama tampilan', 'Rina Pratiwi', { value: true });
    field(c, t, 'Telepon (opsional)', '0812 3456 7890', { value: true });
    field(c, t, 'Email (opsional)', 'rina@example.id', { value: true });
    if (options.error) txt(c, 'Profil gagal disimpan. Periksa koneksi dan coba lagi.', { size: 12, color: t.error, sh: 'fill' });
    button(c, t, 'Simpan profil', { h: 48 });
    const setting = card(c, 'pengaturan profil', t, 4);
    row(setting, 'ganti password', t, 'Ganti password', 'Perbarui kredensial akun', '›');
    row(setting, 'riwayat versi', t, 'Riwayat versi', 'Versi 1.0.9 (10)', '›');
    row(setting, 'cek update', t, 'Cek pembaruan', 'Android saja', '›');
  }
  return f;
}

export function profilFotoSheet(t, x, y, hasPhoto = true) {
  const f = phone(t, 'Umum - opsi foto profil', x, y);
  appBar(f, t, 'Profil Saya', { back: true });
  const shade = box(f, { name: 'scrim sheet foto', sh: 'fill', sv: 'fill', fill: t.scrim });
  const s = box(shade, { name: 'bottom sheet foto', x: 0, y: 500, w: 375, h: 312, layout: 'vertical', pad: [12, 16, 20, 16], gap: 5, r: [24, 24, 0, 0], fill: t.surface });
  box(s, { name: 'handle sheet', w: 36, h: 4, r: 2, fill: t.outline });
  title(s, t, 'Foto profil');
  row(s, 'pilih galeri', t, 'Pilih dari galeri', 'Pilih gambar dari perangkat', '›');
  row(s, 'ambil kamera', t, 'Ambil dengan kamera', 'Gunakan kamera perangkat', '›');
  if (hasPhoto) row(s, 'hapus foto', t, 'Hapus foto', 'Kembali ke inisial nama', '›');
  return f;
}

export function hapusFotoDialog(t, x, y) {
  const f = phone(t, 'Umum - konfirmasi hapus foto', x, y);
  appBar(f, t, 'Profil Saya', { back: true });
  const shade = box(f, { name: 'scrim hapus foto', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: 'dialog konfirmasi hapus foto', x: 22, y: 310, w: 331, layout: 'vertical', pad: 20, gap: 12, r: 24, fill: t.surface });
  title(d, t, 'Hapus foto profil?');
  txt(d, 'Avatar akan kembali memakai inisial nama.', { size: 13, color: t.muted, sh: 'fill' });
  button(d, t, 'Batal', { kind: 'secondary', h: 44 });
  button(d, t, 'Hapus foto', { kind: 'danger', h: 44 });
  return f;
}

export function fotoCrop(t, x, y, permission = false) {
  const f = phone(t, permission ? 'Umum - izin foto gagal' : 'Umum - potong foto', x, y);
  appBar(f, t, permission ? 'Foto Profil' : 'Atur foto', { back: true });
  const c = content(f, t, [12, 20, 18, 20]);
  if (permission) {
    const e = card(c, 'akses foto tidak tersedia', t, 18);
    icon(e, 'alert', t.warn, 28); title(e, t, 'Akses kamera/galeri tidak tersedia');
    txt(e, 'Izinkan akses pada Pengaturan perangkat, atau pilih sumber lain.', { size: 12, color: t.muted, sh: 'fill' });
    button(e, t, 'Buka pengaturan', { kind: 'secondary', h: 44 });
  } else {
    const img = box(c, { name: 'area crop gambar', sh: 'fill', sv: 'fill', r: 18, fill: t.surface2, stroke: t.outline, layout: 'vertical', main: 'center', cross: 'center' });
    const crop = box(img, { name: 'bingkai potong persegi', w: 240, h: 240, r: 120, fill: t.primaryCont, stroke: t.accent, sw: 2, layout: 'horizontal', main: 'center', cross: 'center' });
    icon(crop, 'user', t.accent, 84);
    txt(c, 'Geser dan perbesar untuk menyesuaikan foto profil.', { size: 12, color: t.muted, align: 'center', sh: 'fill' });
    button(c, t, 'Gunakan foto');
  }
  return f;
}

export function updateDialog(t, x, y) {
  const f = phone(t, 'Umum - update tersedia', x, y);
  appBar(f, t, 'Profil Saya', { back: true });
  const shade = box(f, { name: 'scrim update', sh: 'fill', sv: 'fill', fill: t.scrim });
  const d = box(shade, { name: 'dialog pembaruan', x: 20, y: 270, w: 335, layout: 'vertical', pad: 20, gap: 10, r: 24, fill: t.surface });
  icon(d, 'download', t.accent, 30);
  title(d, t, 'Versi 1.1.0 tersedia');
  txt(d, 'Perbaikan pengalaman belanja dan keamanan. Ukuran 24 MB · SHA-256 diverifikasi sebelum pemasangan.', { size: 12, color: t.muted, sh: 'fill' });
  button(d, t, 'Unduh pembaruan', { h: 44 });
  button(d, t, 'Nanti', { kind: 'secondary', h: 44 });
  return f;
}

export function changelog(t, x, y) {
  const f = phone(t, 'Umum - riwayat versi', x, y);
  appBar(f, t, 'Riwayat versi', { back: true });
  const c = content(f, t, [10, 16, 16, 16]);
  const a = card(c, 'versi terbaru', t, 14);
  txt(a, 'v1.0.9 · build 10', { size: 16, weight: 'Bold', color: t.text });
  txt(a, '17 November 2026', { size: 11, color: t.muted });
  txt(a, 'Peningkatan stabilitas transaksi dan penyesuaian pengalaman kasir.', { size: 12, color: t.text, sh: 'fill' });
  const b = card(c, 'versi sebelumnya', t, 14);
  txt(b, 'v1.0.8 · build 9', { size: 15, weight: 'SemiBold', color: t.text });
  txt(b, 'Dukungan kartu anggota QR dan cetak nota Bluetooth Android.', { size: 12, color: t.muted, sh: 'fill' });
  return f;
}

export function stateLoadingError(t, x, y, kind = 'error') {
  const f = phone(t, `Umum - status ${kind}`, x, y);
  appBar(f, t, kind === 'loading' ? 'Memuat data' : kind === 'offline' ? 'Koneksi' : 'Status data', { back: true });
  const c = content(f, t, [12, 20, 20, 20]);
  const p = card(c, `status ${kind}`, t, 20);
  if (kind === 'loading') {
    const spin = box(p, { name: 'indikator memuat', w: 48, h: 48, r: 24, stroke: t.accent, sw: 4 });
    txt(p, 'Memuat data…', { size: 14, color: t.text, align: 'center', sh: 'fill' });
  } else {
    icon(p, kind === 'offline' ? 'wifi' : 'alert', kind === 'offline' ? t.warn : t.error, 30);
    title(p, t, kind === 'offline' ? 'Tidak ada koneksi' : 'Data gagal dimuat');
    txt(p, 'Data belum berubah. Periksa koneksi dan coba lagi.', { size: 12, color: t.muted, sh: 'fill' });
    button(p, t, 'Coba lagi', { kind: 'secondary', h: 44 });
  }
  return f;
}
