// Muat ulang file role di FigCraft yang berjalan: tutup tab lama (tanpa menyimpan) lalu buka dari disk.
//   node reload_live.mjs [role ...]   (default: semua role)
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { connect } from './live.mjs';

const dir = path.dirname(fileURLToPath(import.meta.url));
const roles = process.argv.slice(2).length ? process.argv.slice(2) : ['umum', 'kasir', 'anggota', 'admin'];
const c = connect();
await c.init();
const t = (r) => r.result?.content?.find((x) => x.type === 'text')?.text ?? JSON.stringify(r.error);
const docs = async () => JSON.parse(t(await c.call('run_command', { command: 'doc.list', params: {} })));
for (const r of roles) {
  const d = (await docs()).find((x) => x.name === r);
  if (d) {
    const x = await c.call('run_command', { command: 'file.close', params: { index: d.index, force: true } });
    console.log('tutup', r, t(x).slice(0, 120));
  }
  const o = await c.call('open_file', { path: path.join(dir, `${r}.figcraft`) });
  console.log('buka', r, o.error ? JSON.stringify(o.error) : 'ok');
}
c.close();
