// Sekali jalan: tombol "+ User" admin mengikuti gradien hijau.
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('screens_c.mjs', 'utf8');
const rep = (a, b) => {
  if (!s.includes(a)) throw new Error('tidak ketemu: ' + a.slice(0, 70));
  s = s.replace(a, b);
};
rep("fill: t.primaryCont, shadow: { y: 3, blur: 8 } });\n  icon(fab, 'plus', t.onPrimaryCont, 22);\n  txt(fab, 'User', { size: 16, weight: 'SemiBold', color: t.onPrimaryCont });",
  "fill: { type: 'linearGradient', stops: t.grad }, shadow: { y: 4, blur: 10 } });\n  icon(fab, 'plus', t.onPrimary, 22);\n  txt(fab, 'User', { size: 16, weight: 'SemiBold', color: t.onPrimary });");
writeFileSync('screens_c.mjs', s);
console.log('ok');
