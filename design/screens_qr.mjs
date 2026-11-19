import { box, txt, icon, solid, strokePath, absRect, deco } from './lib.mjs';
import { dark } from './theme.mjs';
import { phone, appBar, button } from './screens_a.mjs';

/** Pola QR contoh (dekoratif, BUKAN kode yang bisa dipindai). Deterministik dari seed. */
function qrPath(n = 25, seed = 7) {
  let s = seed;
  const rnd = () => ((s = (s * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
  const finder = (x, y) => (x < 8 && y < 8) || (x >= n - 8 && y < 8) || (x < 8 && y >= n - 8);
  const inFinder = (cx, cy, x, y) => {
    const dx = x - cx, dy = y - cy;
    if (dx < 0 || dy < 0 || dx > 6 || dy > 6) return null;
    const ring = dx === 0 || dy === 0 || dx === 6 || dy === 6;
    const core = dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4;
    return ring || core;
  };
  let d = '';
  for (let y = 0; y < n; y++) {
    for (let x = 0; x < n; x++) {
      let on;
      const f1 = inFinder(0, 0, x, y), f2 = inFinder(n - 7, 0, x, y), f3 = inFinder(0, n - 7, x, y);
      if (f1 !== null) on = f1;
      else if (f2 !== null) on = f2;
      else if (f3 !== null) on = f3;
      else if (finder(x, y)) on = false;
      else on = rnd() < 0.48;
      if (on) d += `M${x} ${y}h1v1h-1z`;
    }
  }
  return d;
}

export function qrArt(parent, size, color = '#1E1A17') {
  return solid(parent, qrPath(), size, size, color, 1, 'qr contoh (dekoratif)');
}

const GELAP = { type: 'linearGradient', stops: ['#2D2723', '#0E0C0A'] };

function camPhone(t, name, x, y) {
  const f = box(null, { name: `${name} (${t.mode})`, x, y, w: 375, h: 812, fill: GELAP, layout: 'vertical', clip: true });
  deco(f, 220, 90, 220, '#7ED957', 0.07);
  deco(f, -60, 480, 260, '#FFFFFF', 0.04);
  const sb = box(f, { name: 'status bar', sh: 'fill', h: 28, layout: 'horizontal', pad: [0, 24, 0, 24], main: 'spaceBetween', cross: 'center' });
  txt(sb, '9:41', { size: 12, weight: 'SemiBold', color: '#FFFFFF' });
  txt(sb, '5G  ▮▮▮', { size: 10, color: '#FFFFFF' });
  return f;
}

function camBar(f, judul, torch = true) {
  const b = box(f, { name: 'app bar', sh: 'fill', h: 64, layout: 'horizontal', pad: [0, 12, 0, 12], cross: 'center', gap: 8 });
  const bk = box(b, { name: 'tombol kembali', w: 44, h: 44, r: 22, layout: 'horizontal', main: 'center', cross: 'center', fill: '#FFFFFF22' });
  icon(bk, 'back', '#FFFFFF', 22);
  const ti = box(b, { name: 'wadah judul', sh: 'fill', layout: 'horizontal' });
  txt(ti, judul, { size: 24, color: '#FFFFFF', serif: true });
  if (torch) {
    const fl = box(b, { name: 'senter', w: 44, h: 44, r: 22, layout: 'horizontal', main: 'center', cross: 'center', fill: '#FFFFFF22' });
    icon(fl, 'flash', '#FFFFFF', 20);
  }
}

/** Bidikan: 4 siku + garis pindai. */
function bidik(parent, warna, garis = true) {
  const v = box(parent, { name: 'area bidik', w: 260, h: 260 });
  strokePath(v, 'M2 36V14a12 12 0 0112-12h22', 0, 0, 36, 36, warna, 6, 'siku kiri-atas');
  strokePath(v, 'M2 2h22a12 12 0 0112 12v22', 224, 0, 36, 36, warna, 6, 'siku kanan-atas');
  strokePath(v, 'M2 2v22a12 12 0 0012 12h22', 0, 224, 36, 36, warna, 6, 'siku kiri-bawah');
  strokePath(v, 'M36 2v22a12 12 0 01-12 12H2', 224, 224, 36, 36, warna, 6, 'siku kanan-bawah');
  if (garis) absRect(v, 20, 128, 220, 3, warna, 2, 0.9, 'garis pindai');
  return v;
}

function tengah(f, warna, hint, garis = true) {
  const m = box(f, { name: 'area kamera', sh: 'fill', sv: 'fill', layout: 'vertical', main: 'center', cross: 'center', gap: 24 });
  bidik(m, warna, garis);
  if (hint) txt(m, hint, { size: 15, color: '#FFFFFF', align: 'center', w: 260 });
  return m;
}

export function qrScan(t, x, y) {
  const f = camPhone(t, 'Anggota QR - scan', x, y);
  camBar(f, 'Scan kartu anggota');
  tengah(f, '#7ED957', 'Arahkan kamera ke QR pada kartu anggota');
  const pill = box(f, { name: 'info otomatis', sh: 'fill', layout: 'horizontal', main: 'center', pad: [0, 0, 16, 0] });
  const c = box(pill, { name: 'catatan', h: 32, r: 16, pad: [0, 14, 0, 14], layout: 'horizontal', cross: 'center', gap: 6, fill: '#FFFFFF22' });
  icon(c, 'check', '#FFFFFF', 14, 2.5);
  txt(c, 'Otomatis memilih bila anggota cocok', { size: 12, weight: 'Medium', color: '#FFFFFF' });
  const ac = box(f, { name: 'aksi', sh: 'fill', layout: 'vertical', pad: [0, 16, 28, 16], gap: 12 });
  button(ac, dark, 'Cari anggota manual', { kind: 'secondary', icon: 'search' });
  return f;
}

function sheet(f, t) {
  const sh = box(f, { name: 'bottom sheet', sh: 'fill', r: [28, 28, 0, 0], fill: t.bg, layout: 'vertical', pad: [0, 16, 24, 16], gap: 14 });
  const hd = box(sh, { name: 'pegangan', sh: 'fill', h: 28, layout: 'horizontal', main: 'center', cross: 'center' });
  box(hd, { name: 'garis pegangan', w: 36, h: 4, r: 2, fill: t.outline });
  return sh;
}

export function qrDitemukan(t, x, y) {
  const f = camPhone(t, 'Anggota QR - ditemukan', x, y);
  camBar(f, 'Scan kartu anggota', false);
  tengah(f, '#8BD6A8', '', false);
  const sh = sheet(f, t);
  const ok = box(sh, { name: 'status', h: 30, r: 15, pad: [0, 12, 0, 10], layout: 'horizontal', cross: 'center', gap: 6, fill: t.okCont });
  icon(ok, 'check', t.ok, 16, 2.5);
  txt(ok, 'QR terbaca · anggota aktif', { size: 12, weight: 'SemiBold', color: t.ok });
  const kartu = box(sh, { name: 'kartu anggota', sh: 'fill', layout: 'horizontal', pad: 16, cross: 'center', gap: 14, r: 20, fill: t.surface });
  const av = box(kartu, { name: 'avatar', w: 56, h: 56, r: 28, fill: t.primaryCont, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(av, 'user', t.onPrimaryCont, 26);
  const c = box(kartu, { name: 'info', sh: 'fill', layout: 'vertical', gap: 3 });
  txt(c, 'Budi Santoso', { size: 24, color: t.text, serif: true, sh: 'fill' });
  txt(c, '12.34.0021 · Unit Produksi', { size: 13, color: t.muted, sh: 'fill' });
  const tg = box(c, { name: 'label', sh: 'fill', layout: 'horizontal', wrap: true, gap: 6, pad: [4, 0, 0, 0] });
  for (const s of ['BULANAN', 'Limit Rp 2.000.000 / periode']) {
    const g = box(tg, { name: s, h: 24, r: 12, pad: [0, 10, 0, 10], layout: 'horizontal', cross: 'center', fill: t.surface2 });
    txt(g, s, { size: 11, weight: 'SemiBold', color: t.muted });
  }
  button(sh, t, 'Pilih anggota ini', { icon: 'check' });
  button(sh, t, 'Scan ulang', { kind: 'secondary', icon: 'scan' });
  return f;
}

export function qrTidakDitemukan(t, x, y) {
  const f = camPhone(t, 'Anggota QR - tidak ditemukan', x, y);
  camBar(f, 'Scan kartu anggota', false);
  tengah(f, '#F2B8B5', '', false);
  const sh = sheet(f, t);
  const hd = box(sh, { name: 'judul', sh: 'fill', layout: 'horizontal', cross: 'center', gap: 12 });
  const ic = box(hd, { name: 'ikon galat', w: 48, h: 48, r: 24, layout: 'horizontal', main: 'center', cross: 'center', fill: t.errorCont });
  icon(ic, 'alert', t.onErrorCont, 24);
  txt(hd, 'Pelanggan tidak ditemukan', { size: 24, color: t.text, serif: true, sh: 'fill' });
  txt(sh, 'QR tidak cocok dengan anggota aktif. Pilihan saat ini tidak berubah.', { size: 14, color: t.muted, sh: 'fill' });
  const cur = box(sh, { name: 'pilihan saat ini', sh: 'fill', layout: 'horizontal', pad: 14, cross: 'center', gap: 10, r: 16, fill: t.surface });
  icon(cur, 'user', t.muted, 20);
  const c = box(cur, { name: 'info', sh: 'fill', layout: 'vertical', gap: 1 });
  txt(c, 'Dipilih saat ini', { size: 11, color: t.muted });
  txt(c, 'Pelanggan Kas · 00.00004', { size: 15, weight: 'SemiBold', color: t.text, sh: 'fill' });
  button(sh, t, 'Scan ulang', { icon: 'scan' });
  button(sh, t, 'Cari anggota manual', { kind: 'secondary', icon: 'search' });
  return f;
}

export function qrIzin(t, x, y) {
  const f = phone(t, 'Anggota QR - kamera tidak tersedia', x, y);
  appBar(f, t, 'Scan kartu anggota', { back: true });
  const m = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', main: 'center', cross: 'center', gap: 16, pad: [0, 32, 0, 32] });
  const ic = box(m, { name: 'ikon', w: 96, h: 96, r: 32, layout: 'horizontal', main: 'center', cross: 'center', fill: t.surface2 });
  icon(ic, 'scan', t.muted, 44, 2);
  txt(m, 'Kamera tidak tersedia', { size: 28, color: t.text, serif: true, align: 'center', sh: 'fill' });
  txt(m, 'Izin kamera ditolak atau perangkat tidak punya kamera. Aplikasi tetap bisa dipakai penuh lewat pencarian anggota.', { size: 14, color: t.muted, align: 'center', sh: 'fill' });
  const ac = box(f, { name: 'aksi', sh: 'fill', layout: 'vertical', pad: [0, 16, 28, 16], gap: 12 });
  button(ac, t, 'Cari anggota manual', { icon: 'search' });
  button(ac, t, 'Buka pengaturan', { kind: 'secondary' });
  return f;
}

/** Sisi anggota: kartu digital berisi QR untuk dipindai kasir. */
export function kartuAnggota(t, x, y) {
  const f = phone(t, 'Anggota - kartu QR', x, y);
  appBar(f, t, 'Kartu Anggota', { back: true });
  const body = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [8, 16, 16, 16], gap: 16 });
  const kartu = box(body, { name: 'kartu', sh: 'fill', layout: 'vertical', cross: 'center', pad: [24, 24, 24, 24], gap: 14, r: 28, fill: { type: 'linearGradient', stops: [t.primaryCont, t.surface] } });
  txt(kartu, 'KOPKAR KUSUMA LESTARI', { size: 12, weight: 'SemiBold', color: t.accent, ls: 2 });
  txt(kartu, 'Budi Santoso', { size: 30, color: t.text, serif: true, align: 'center', sh: 'fill' });
  txt(kartu, '12.34.0021 · Unit Produksi', { size: 13, color: t.muted, align: 'center', sh: 'fill' });
  const tile = box(kartu, { name: 'wadah QR', w: 240, h: 240, r: 24, pad: 16, layout: 'horizontal', main: 'center', cross: 'center', fill: '#FFFFFF' });
  qrArt(tile, 208);
  txt(kartu, 'Tunjukkan QR ini ke kasir', { size: 15, weight: 'SemiBold', color: t.text, align: 'center', sh: 'fill' });
  const n = box(body, { name: 'catatan', sh: 'fill', layout: 'horizontal', pad: [12, 14, 12, 14], gap: 10, cross: 'center', r: 14, fill: t.warnCont });
  icon(n, 'shield', t.warn, 18);
  txt(n, 'QR berisi NIK Anda. Jangan bagikan tangkapan layarnya.', { size: 12, color: t.warn, sh: 'fill' });
  box(body, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  button(body, t, 'Terangkan layar', { kind: 'secondary', icon: 'flash' });
  return f;
}
