// Cetak urutan tab di FigCraft yang berjalan.
import { connect } from './live.mjs';

const c = connect();
await c.init();
const r = await c.call('run_command', { command: 'doc.list', params: {} });
const docs = JSON.parse(r.result.content.find((x) => x.type === 'text').text);
console.log(docs.map((d) => `${d.index}:${d.name}${d.active ? '*' : ''}`).join('  '));
c.close();
