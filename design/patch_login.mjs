import { readFileSync, writeFileSync } from 'node:fs';
let s = readFileSync('build.mjs', 'utf8');
const add = (a, b) => { if (!s.includes(a)) throw new Error('tidak ketemu: ' + a.slice(0, 50)); s = s.replace(a, b); };
add("import { qrScan,", "import { loginHutan } from './screens_login.mjs';\nimport { qrScan,");
add("    [[login, light], [gantiPassword, light]],\n    [[login, dark], [gantiPassword, dark]],", "    [[loginHutan, light], [gantiPassword, light]],\n    [[loginHutan, dark], [gantiPassword, dark]],");
writeFileSync('build.mjs', s);
console.log('ok');
