// Sekali jalan: komponen bersama v2 (header foto, tombol/chip gradien hijau) di screens_a.mjs.
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('screens_a.mjs', 'utf8');
const rep = (a, b) => {
  if (!s.includes(a)) throw new Error('tidak ketemu: ' + a.slice(0, 70));
  s = s.replace(a, b);
};

rep("import { box, txt, icon } from './lib.mjs';", `import { existsSync } from 'node:fs';
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
}`);

rep(`  const f = box(null, { name: \`\${name} (\${t.mode})\`, x, y, w, h, fill: t.bg, layout: 'vertical', clip: true });
  const sb = box(f, { name: 'status bar', sh: 'fill', h: 28, layout: 'horizontal', pad: [0, 24, 0, 24], main: 'spaceBetween', cross: 'center' });
  txt(sb, '9:41', { size: 12, weight: 'SemiBold', color: t.text });
  txt(sb, '5G  ▮▮▮', { size: 10, color: t.text });
  return f;`, `  const f = box(null, { name: \`\${name} (\${t.mode})\`, x, y, w, h, fill: t.bg, layout: 'vertical', clip: true });
  fotoHeader(f, t, w);
  const sb = box(f, { name: 'status bar', sh: 'fill', h: 28, layout: 'horizontal', pad: [0, 24, 0, 24], main: 'spaceBetween', cross: 'center' });
  txt(sb, '9:41', { size: 12, weight: 'SemiBold', color: PUTIH });
  txt(sb, '5G  ▮▮▮', { size: 10, color: PUTIH });
  return f;`);

// head: warna opsional
rep(`export function head(parent, t, main, qual, size = 28) {
  const r = box(parent, { name: 'judul', layout: 'horizontal', cross: 'baseline', gap: 6 });
  txt(r, qual ? main + ',' : main, { size, color: t.text, serif: true });
  if (qual) txt(r, qual, { size, color: t.muted, serif: true });
  return r;
}`, `export function head(parent, t, main, qual, size = 28, warna = {}) {
  const r = box(parent, { name: 'judul', layout: 'horizontal', cross: 'baseline', gap: 6 });
  txt(r, qual ? main + ',' : main, { size, color: warna.main ?? t.text, serif: true });
  if (qual) txt(r, qual, { size, color: warna.qual ?? t.muted, serif: true });
  return r;
}`);

const a0 = s.indexOf('export function appBar');
const a1 = s.indexOf('/** Tombol pil.');
s = s.slice(0, a0) + `/** App bar di atas foto (judul putih, tombol kaca) lalu tepi panel isi membulat yang menutup bawah foto. */
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

` + s.slice(a1);

rep("const fill = o.disabled ? t.surface2 : kind === 'primary' ? t.primary : kind === 'danger' ? t.error : t.surface;", "const fill = o.disabled ? t.surface2 : kind === 'primary' ? grad(t) : kind === 'danger' ? t.error : t.surface;");
rep("fill: on ? t.primary : t.surface, stroke: on ? undefined : t.outline });\n  txt(c, label, { size: 14, weight: 'Medium', color: on ? t.onPrimary : t.text });", "fill: on ? grad(t) : t.surface, stroke: on ? undefined : t.outline });\n  txt(c, label, { size: 14, weight: 'Medium', color: on ? t.onPrimary : t.text });");
rep("layout: 'horizontal', main: 'center', cross: 'center', fill: t.primary });\n  icon(b, 'plus', t.onPrimary, size * 0.5, 2.5);", "layout: 'horizontal', main: 'center', cross: 'center', fill: grad(t) });\n  icon(b, 'plus', t.onPrimary, size * 0.5, 2.5);");
rep("r: 16, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 8, fill: t.surface, stroke: o.focus", "r: 18, layout: 'horizontal', pad: [0, 16, 0, 16], cross: 'center', gap: 8, fill: t.surface2, stroke: o.focus");
writeFileSync('screens_a.mjs', s);
console.log('ok');
