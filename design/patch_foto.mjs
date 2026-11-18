// Sekali jalan: pakai foto latar (assets/login_bg.webp) bila ada; ilustrasi vektor jadi cadangan.
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('screens_login.mjs', 'utf8');
const rep = (a, b) => {
  if (!s.includes(a)) throw new Error('tidak ketemu: ' + a.slice(0, 60));
  s = s.replace(a, b);
};
rep("import { box, txt, icon, vec, strokePath } from './lib.mjs';", "import { existsSync } from 'node:fs';\nimport { fileURLToPath } from 'node:url';\nimport { box, txt, icon, vec, strokePath, gambar, absRect } from './lib.mjs';\n\nconst FOTO = fileURLToPath(new URL('./assets/login_bg.webp', import.meta.url));");
rep("  // Lanskap atas (di belakang judul dan kartu)\n", `  const ada = existsSync(FOTO);
  if (ada) {
    gambar(f, FOTO, 0, 0, 375, 812, 'fill', 'foto latar kebun sawit');
    // Mode gelap: foto yang sama diredupkan agar kartu dan teks tetap nyaman.
    if (p.g) absRect(f, 0, 0, 375, 812, '#0B2237', 0, 0.62, 'peredup mode gelap');
  } else {
  // Lanskap atas (di belakang judul dan kartu)
`);
rep("  // Status bar\n", "  }\n\n  // Status bar\n");
writeFileSync('screens_login.mjs', s);
console.log('ok');
