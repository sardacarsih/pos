// Buka file role di FigCraft yang sedang berjalan (figcraft --control 9333).
//   node open_live.mjs [role ...]   (default: semua role)
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { connect } from './live.mjs';

const dir = path.dirname(fileURLToPath(import.meta.url));
const roles = process.argv.slice(2).length ? process.argv.slice(2) : ['umum', 'kasir', 'anggota', 'admin'];
const c = connect();
await c.init();
for (const r of roles) {
  const x = await c.call('open_file', { path: path.join(dir, `${r}.figcraft`) });
  console.log(r, x.error ? JSON.stringify(x.error) : (x.result?.content?.[0]?.text ?? '').slice(0, 120));
}
c.close();
