// Jembatan ke FigCraft yang sedang terbuka (figcraft --control PORT) lewat MCP stdio:
//   node live.mjs tools                      daftar tool MCP
//   node live.mjs call <tool> '<json args>'  panggil satu tool
import { spawn } from 'node:child_process';
import readline from 'node:readline';

const CLI = process.env.FIGCRAFT_CLI ?? 'C:\\Program Files\\ArtCraft\\FigCraft\\figcraft-cli.exe';
const PORT = process.env.FIGCRAFT_PORT ?? '9333';

export function connect() {
  const p = spawn(CLI, ['mcp', '--attach', PORT], { stdio: ['pipe', 'pipe', 'inherit'] });
  const rl = readline.createInterface({ input: p.stdout });
  const waits = new Map();
  let id = 0;
  rl.on('line', (l) => {
    try {
      const m = JSON.parse(l);
      if (m.id !== undefined && waits.has(m.id)) {
        waits.get(m.id)(m);
        waits.delete(m.id);
      }
    } catch { /* abaikan baris non-JSON */ }
  });
  const send = (method, params) =>
    new Promise((res) => {
      const i = ++id;
      waits.set(i, res);
      p.stdin.write(JSON.stringify({ jsonrpc: '2.0', id: i, method, params }) + '\n');
    });
  return {
    async init() {
      await send('initialize', { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'live', version: '1' } });
      p.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) + '\n');
    },
    tools: () => send('tools/list', {}),
    call: (name, args) => send('tools/call', { name, arguments: args }),
    close: () => p.kill(),
  };
}

if (process.argv[1]?.endsWith('live.mjs')) {
  const [cmd, name, json] = process.argv.slice(2);
  const c = connect();
  await c.init();
  const r = cmd === 'tools' ? await c.tools() : await c.call(name, json ? JSON.parse(json) : {});
  console.log(JSON.stringify(r, null, 1).slice(0, 6000));
  c.close();
}
