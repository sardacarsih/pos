// Tampilkan satu dokumen di depan: node live_test.mjs <nama> (mis. kasir), lalu screenshot.
import { writeFileSync } from 'node:fs';
import { connect } from './live.mjs';

const nama = process.argv[2] ?? 'kasir';
const c = connect();
await c.init();
const t = (r) => r.result?.content?.find((x) => x.type === 'text')?.text ?? JSON.stringify(r.error);
const docs = JSON.parse(t(await c.call('run_command', { command: 'doc.list', params: {} })));
const d = docs.find((x) => x.name === nama);
if (!d) throw new Error('dokumen tidak ada: ' + nama);
await c.call('run_command', { command: 'doc.switch', params: { index: d.index } });
const s = await c.call('screenshot', {});
const img = s.result?.content?.find((x) => x.type === 'image');
if (img) writeFileSync(new URL('./live.png', import.meta.url), Buffer.from(img.data, 'base64'));
console.log('aktif:', nama, '| screenshot:', img ? 'live.png' : t(s).slice(0, 200));
c.close();
