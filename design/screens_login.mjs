import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { box, txt, icon, vec, strokePath, gambar, absRect } from './lib.mjs';

const FOTO = fileURLToPath(new URL('./assets/login_bg.webp', import.meta.url));

const r2 = (n) => Math.round(n * 10) / 10;
const rad = (d) => (d * Math.PI) / 180;

/** Daun berbentuk lensa dari titik (x,y) searah sudut a (derajat), panjang L, setengah lebar w. */
function daun(x, y, a, L, w) {
  const c = Math.cos(rad(a)), s = Math.sin(rad(a));
  const tx = x + L * c, ty = y + L * s, mx = x + (L / 2) * c, my = y + (L / 2) * s;
  const nx = -s * w, ny = c * w;
  return `M${r2(x)} ${r2(y)}Q${r2(mx + nx)} ${r2(my + ny)} ${r2(tx)} ${r2(ty)}Q${r2(mx - nx)} ${r2(my - ny)} ${r2(x)} ${r2(y)}z`;
}

/** Mahkota sawit kecil dari samping: daun menyebar ke atas dan menjuntai. */
function mahkota(cx, cy, r) {
  let d = '';
  for (const a of [-175, -150, -125, -100, -75, -50, -25, 0, 25, 160]) d += daun(cx, cy, a, r * (a === 25 || a === 160 ? 0.7 : 1), r * 0.17);
  return d;
}

/** Pelepah besar: tulang melengkung dengan anak daun di kedua sisi. */
function pelepah(x0, y0, a0, panjang, lengkung, anak) {
  let d = '';
  let x = x0, y = y0, a = a0;
  const n = 26, step = panjang / n;
  for (let i = 0; i < n; i++) {
    const t = i / n;
    const l = anak * (1 - t * 0.78);
    d += daun(x, y, a - 58, l, l * 0.16) + daun(x, y, a + 58, l, l * 0.16);
    a += lengkung * step;
    x += step * Math.cos(rad(a));
    y += step * Math.sin(rad(a));
  }
  d += daun(x0, y0, a0, panjang * 0.98, 1.6);
  return d;
}

function palet(t) {
  const g = t.mode === 'gelap';
  return {
    g,
    langit: g ? ['#0B2237', '#16405A', '#10241D'] : ['#2F8FE8', '#BEE0F5', '#F3FAF0'],
    jauh: g ? '#2F5A44' : '#9CCB88', tengah: g ? '#27623A' : '#62AE4C', dekat: g ? '#1C4E2C' : '#3B9440',
    kabut: g ? '#10241D' : '#EEF7EA',
    mahkota: g ? '#173F26' : '#2C7D33', daun: g ? ['#2F7A3E', '#154A25'] : ['#58B040', '#1E6B2A'],
    lime: g ? '#5E8F2A' : '#BDE24C', hijau: g ? ['#1B6B2D', '#0F3F1B'] : ['#2E9A35', '#0E6A29'],
    judul: g ? '#E6F5EA' : '#0A5A2A', tagline: g ? '#CDE8D5' : '#143D26',
    kartu: g ? '#17211B' : '#FFFFFF', teks: g ? '#8BE0A6' : '#0B5A2B', muted: g ? '#9DB0A3' : '#5B665E',
    isi: g ? '#223028' : '#F4F6F5', garis: g ? '#33443A' : '#E1E6E2', ikon: g ? '#9DB0A3' : '#3D4A41',
    hijauTua: g ? '#8BE0A6' : '#0E7A2E', cincin: g ? '#17211B' : '#FFFFFF', avatar: g ? '#1F3A2A' : '#E3F1E0',
    tombol: g ? ['#3E9E34', '#145C27'] : ['#43B22E', '#0D6A2A'],
  };
}

/** Login bertema perkebunan sawit (referensi): lanskap ilustrasi, kartu putih, tombol hijau. */
export function loginHutan(t, x, y, o = {}) {
  const p = palet(t);
  const namaFrame = o.dialog ? 'Login - bantuan password' : o.error ? 'Login - tidak berhasil' : 'Login - perkebunan';
  const f = box(null, { name: `${namaFrame} (${t.mode})`, x, y, w: 375, h: 812, fill: { type: 'linearGradient', stops: p.langit }, clip: true });

  const ada = existsSync(FOTO);
  if (ada) {
    gambar(f, FOTO, 0, 0, 375, 812, 'fill', 'foto latar kebun sawit');
    // Mode gelap: foto yang sama diredupkan agar kartu dan teks tetap nyaman.
    if (p.g) absRect(f, 0, 0, 375, 812, '#0B2237', 0, 0.62, 'peredup mode gelap');
  } else {
  // Lanskap atas (di belakang judul dan kartu)
  vec(f, 'M0 238 C70 208 130 248 200 224 S320 198 375 228 V720 H0Z', { type: 'linearGradient', stops: [p.jauh, p.kabut] }, 1, 'bukit jauh');
  for (let xx = 4; xx < 380; xx += 30) vec(f, mahkota(xx, 246 + 10 * Math.sin(xx / 29), 11), p.mahkota, 0.55, 'sawit jauh');
  vec(f, 'M0 284 C60 254 120 288 190 266 S310 248 375 274 V720 H0Z', { type: 'linearGradient', stops: [p.tengah, p.kabut] }, 1, 'bukit tengah');
  for (let xx = 14; xx < 380; xx += 36) vec(f, mahkota(xx, 270 + 9 * Math.sin(xx / 23), 17), p.mahkota, 0.9, 'sawit tengah');
  // Pelepah besar pojok kanan atas
  vec(f, pelepah(392, -18, 152, 300, 0.55, 78), { type: 'linearGradient', stops: p.daun }, 1, 'pelepah kanan atas 1');
  vec(f, pelepah(392, -10, 118, 230, 0.7, 66), { type: 'linearGradient', stops: p.daun }, 0.95, 'pelepah kanan atas 2');

  // Lanskap bawah
  vec(f, 'M0 700 C70 675 150 705 230 685 S330 670 375 690 V812 H0Z', p.jauh, 1, 'bukit bawah jauh');
  for (let xx = 6; xx < 380; xx += 30) vec(f, mahkota(xx, 712 + 8 * Math.sin(xx / 21), 12), p.mahkota, 0.7, 'sawit bawah');
  vec(f, 'M0 738 C80 712 160 742 250 724 S340 714 375 730 V812 H0Z', p.tengah, 1, 'bukit bawah tengah');
  for (let xx = 18; xx < 380; xx += 38) vec(f, mahkota(xx, 742 + 7 * Math.sin(xx / 17), 19), p.mahkota, 0.95, 'sawit bawah besar');
  vec(f, pelepah(-18, 790, -28, 240, -0.5, 70), { type: 'linearGradient', stops: p.daun }, 1, 'pelepah kiri bawah 1');
  vec(f, pelepah(-14, 812, -8, 190, -0.6, 58), { type: 'linearGradient', stops: p.daun }, 0.95, 'pelepah kiri bawah 2');
  vec(f, 'M0 772 C90 742 190 802 290 766 S350 744 375 748 V812 H0Z', p.lime, 1, 'ombak lime');
  vec(f, 'M0 796 C120 770 240 822 375 778 V812 H0Z', { type: 'linearGradient', stops: p.hijau }, 1, 'ombak hijau');

  }

  // Status bar
  txt(f, '9:41', { x: 28, y: 14, size: 15, weight: 'SemiBold', color: '#FFFFFF', name: 'jam' });
  txt(f, '5G  ▮▮▮', { x: 290, y: 15, size: 12, color: '#FFFFFF', name: 'sinyal' });

  // Merek
  txt(f, 'Kopkar', { x: 30, y: 82, size: 38, weight: 'Bold', color: p.judul, font: 'Poppins', name: 'merek 1' });
  txt(f, 'Kusuma Lestari', { x: 30, y: 122, size: 38, weight: 'Bold', color: p.judul, font: 'Poppins', name: 'merek 2' });
  strokePath(f, 'M2 22C70 2 150 -2 215 8', 28, 172, 216, 26, p.g ? '#7ED957' : '#4DA83A', 4, 'garis merek');
  txt(f, 'Bersama Kita Tumbuh\nLebih Baik', { x: 32, y: 204, size: 16, weight: 'Italic', color: p.tagline, font: 'Poppins', name: 'tagline' });

  // Kartu
  const kartu = box(f, { name: 'kartu login', x: 20, y: 308, w: 335, layout: 'vertical', pad: [52, 24, 24, 24], gap: 14, cross: 'center', r: 32, fill: p.kartu, shadow: { y: 8, blur: 24 } });
  txt(kartu, 'Selamat Datang Kembali!', { size: 21, weight: 'Bold', color: p.teks, font: 'Poppins', align: 'center', sh: 'fill' });
  txt(kartu, 'Masuk untuk mengakses akun Anda', { size: 14, color: p.muted, font: 'Poppins', align: 'center', sh: 'fill' });
  box(kartu, { name: 'jarak', sh: 'fill', h: 2 });
  const input = (ikon, teks, trail) => {
    const i = box(kartu, { name: `input ${teks}`, sh: 'fill', h: 52, r: 18, layout: 'horizontal', pad: [0, 18, 0, 18], cross: 'center', gap: 14, fill: p.isi, stroke: p.garis });
    icon(i, ikon, p.ikon, 20, 2.2);
    txt(i, teks, { size: 16, color: p.muted, font: 'Poppins', sh: 'fill' });
    if (trail) icon(i, trail, p.ikon, 20, 2.2);
  };
  input('user', 'Username');
  input('lock', 'Password', 'eye');
  if (o.error) txt(kartu, 'Username atau password tidak sesuai. Coba lagi.', { size: 12, color: p.g ? '#F2B8B5' : '#B3261E', font: 'Poppins', align: 'center', sh: 'fill' });
  const baris = box(kartu, { name: 'bantuan password', sh: 'fill', layout: 'horizontal', main: 'max', cross: 'center' });
  txt(baris, 'Lupa password?', { size: 14, weight: 'SemiBold', color: p.hijauTua, font: 'Poppins' });
  const tombol = box(kartu, { name: 'tombol Masuk', sh: 'fill', h: 56, r: 28, layout: 'horizontal', main: 'center', cross: 'center', gap: 10, fill: { type: 'linearGradient', stops: p.tombol }, shadow: { y: 6, blur: 14, color: '#0D6A2A55' } });
  icon(tombol, 'login', '#FFFFFF', 22, 2.2);
  txt(tombol, 'Masuk', { size: 17, weight: 'SemiBold', color: '#FFFFFF', font: 'Poppins' });
  const theme = box(f, { name: 'tombol tema', x: 326, y: 42, w: 36, h: 36, r: 18, layout: 'horizontal', main: 'center', cross: 'center', fill: '#FFFFFF44' });
  icon(theme, 'flash', '#FFFFFF', 18);

  // Avatar bundar di tepi atas kartu
  const cincin = box(f, { name: 'cincin avatar', x: 137, y: 270, w: 100, h: 80, r: 40, fill: p.cincin });
  const av = box(f, { name: 'avatar', x: 150, y: 278, w: 74, h: 74, r: 37, layout: 'horizontal', main: 'center', cross: 'center', fill: p.avatar });
  icon(av, 'users', p.hijauTua, 36, 2.4);
  return f;
}
