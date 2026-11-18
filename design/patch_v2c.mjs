// Sekali jalan: nama BBM dan RON/CN ditumpuk (Poppins lebih lebar, tidak lagi terpotong ke baris baru).
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('screens_bbm.mjs', 'utf8');
const a = "  const nm = box(info, { name: 'nama', sh: 'fill', layout: 'horizontal', cross: 'baseline', gap: 5, wrap: true });\n  txt(nm, f.nama + ',', { size: 16, color: t.text, serif: true });\n  txt(nm, f.ket, { size: 16, color: t.muted, serif: true });\n";
if (!s.includes(a)) throw new Error('blok nama tidak ketemu');
s = s.replace(a, "  txt(info, f.nama, { size: 15, color: t.text, serif: true, sh: 'fill' });\n  txt(info, f.ket, { size: 12, weight: 'Medium', color: t.muted });\n");
writeFileSync('screens_bbm.mjs', s);
console.log('ok');
