// Sekali jalan: sesuaikan layar yang punya header sendiri (struk, tablet, kamera QR) dengan gaya v2.
import { readFileSync, writeFileSync } from 'node:fs';

function ubah(berkas, pasangan) {
  let s = readFileSync(berkas, 'utf8');
  for (const [a, b] of pasangan) {
    if (!s.includes(a)) throw new Error(`${berkas}: tidak ketemu: ${a.slice(0, 70)}`);
    s = s.replace(a, b);
  }
  writeFileSync(berkas, s);
}

// Kamera QR: aksen hijau
ubah('screens_qr.mjs', [
  ["deco(f, 220, 90, 220, '#F2994A', 0.06);", "deco(f, 220, 90, 220, '#7ED957', 0.07);"],
  ["tengah(f, '#F2994A', 'Arahkan", "tengah(f, '#7ED957', 'Arahkan"],
]);

// Struk: header seragam lewat appBar
ubah('screens_bbm.mjs', [
  [
    "  const hd = box(f, { name: 'header sukses', sh: 'fill', layout: 'vertical', cross: 'center', pad: [24, 24, 16, 24], gap: 8 });\n  const ok = box(hd, { name: 'ikon sukses', w: 64, h: 64, r: 32, fill: t.okCont, layout: 'horizontal', main: 'center', cross: 'center' });\n  icon(ok, 'check', t.ok, 32, 3);\n  txt(hd, 'Faktur tersimpan', { size: 28, color: t.text, align: 'center', sh: 'fill', serif: true });",
    "  appBar(f, t, 'Faktur tersimpan', {});\n  const hd = box(f, { name: 'header sukses', sh: 'fill', layout: 'vertical', cross: 'center', pad: [4, 24, 12, 24], gap: 8 });\n  const ok = box(hd, { name: 'ikon sukses', w: 64, h: 64, r: 32, fill: t.okCont, layout: 'horizontal', main: 'center', cross: 'center' });\n  icon(ok, 'check', t.ok, 32, 3);",
  ],
  [
    "const f = box(null, { name: `Kasir BBM - tablet (${t.mode})`, x, y, w: 1024, h: 768, fill: t.bg, layout: 'vertical', clip: true });",
    "const f = box(null, { name: `Kasir BBM - tablet (${t.mode})`, x, y, w: 1024, h: 768, fill: t.bg, layout: 'vertical', clip: true });\n  fotoHeader(f, t, 1024, 92);",
  ],
  ["import { phone, appBar, head, chip, searchBar, button, addBtn } from './screens_a.mjs';", "import { phone, appBar, head, chip, searchBar, button, addBtn, fotoHeader } from './screens_a.mjs';"],
]);
console.log('ok');
