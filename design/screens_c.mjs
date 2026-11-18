import { box, txt, icon } from './lib.mjs';
import { rp } from './theme.mjs';
import { phone, appBar, button, field } from './screens_a.mjs';

function row(parent, t, l, v, o = {}) {
  const r = box(parent, { name: `baris ${l}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
  txt(r, l, { size: o.big ? 16 : 14, weight: o.big ? 'SemiBold' : 'Regular', color: o.big ? t.text : t.muted });
  txt(r, v, { size: o.big ? 18 : 14, weight: o.big ? 'Bold' : 'Medium', color: t.text });
}

export function transaksiSaya(t, x, y) {
  const f = phone(t, 'Anggota - Transaksi Saya', x, y);
  appBar(f, t, 'Transaksi Saya', { actions: ['qr', 'logout'] });
  const pr = box(f, { name: 'pemilih periode', sh: 'fill', layout: 'horizontal', pad: [0, 8, 0, 8], cross: 'center' });
  const a = box(pr, { name: 'periode sebelumnya', w: 48, h: 48, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(a, 'back', t.text);
  const m = box(pr, { name: 'periode', sh: 'fill', layout: 'horizontal', main: 'center' });
  txt(m, 'November 2026', { size: 16, weight: 'SemiBold', color: t.text });
  const b = box(pr, { name: 'periode berikutnya', w: 48, h: 48, layout: 'horizontal', main: 'center', cross: 'center' });
  icon(b, 'chevR', t.text);
  const kl = box(f, { name: 'kartu limit', sh: 'fill', layout: 'vertical', pad: 20, gap: 12, r: 20, fill: t.primaryCont });
  txt(kl, 'Belanja periode ini', { size: 13, weight: 'Medium', color: t.onPrimaryCont });
  txt(kl, rp(1250000), { size: 36, color: t.onPrimaryCont, serif: true });
  const bar = box(kl, { name: 'bar pemakaian', sh: 'fill', h: 10, r: 5, fill: t.surface, layout: 'horizontal' });
  box(bar, { name: 'terpakai 62%', w: 190, h: 10, r: 5, fill: t.primary });
  const lr = box(kl, { name: 'limit & sisa', sh: 'fill', layout: 'horizontal', main: 'spaceBetween' });
  txt(lr, 'Limit ' + rp(2000000), { size: 13, color: t.onPrimaryCont });
  txt(lr, 'Sisa ' + rp(750000), { size: 13, weight: 'SemiBold', color: t.onPrimaryCont });
  const sp = box(f, { name: 'jarak', sh: 'fill', h: 8 });
  const hd = box(f, { name: 'judul daftar', sh: 'fill', layout: 'horizontal', pad: [8, 16, 4, 16] });
  txt(hd, 'Riwayat transaksi', { size: 13, weight: 'SemiBold', color: t.muted });
  const ls = box(f, { name: 'daftar transaksi', sh: 'fill', sv: 'fill', layout: 'vertical', clip: true, pad: [0, 16, 0, 16], gap: 8 });
  for (const [tot, tgl, no, jb] of [[rp(445000), '07 Nov 2026 · 14:02', 'BBM-26-000119', 'KREDIT · 3x angsuran'], [rp(124100), '03 Nov 2026 · 09:15', 'BBM-26-000087', 'KREDIT · sekali bayar'], [rp(676000), '01 Nov 2026 · 16:40', 'BBM-26-000061', 'KREDIT · 6x angsuran']]) {
    const c = box(ls, { name: `kartu ${no}`, sh: 'fill', layout: 'horizontal', pad: 16, cross: 'center', gap: 12, r: 16, fill: t.surface, stroke: t.outline });
    const i = box(c, { name: 'info', sh: 'fill', layout: 'vertical', gap: 3 });
    txt(i, tot, { size: 18, weight: 'SemiBold', color: t.text });
    txt(i, tgl, { size: 13, color: t.muted });
    txt(i, `${no} · ${jb}`, { size: 12, color: t.muted });
    icon(c, 'chevR', t.muted, 22);
  }
  return f;
}

export function detailFaktur(t, x, y) {
  const f = phone(t, 'Anggota - rincian faktur', x, y);
  appBar(f, t, 'BBM-26-000119', { back: true });
  const b = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [4, 16, 16, 16], gap: 14, clip: true });
  const m = box(b, { name: 'meta', sh: 'fill', layout: 'vertical', gap: 2 });
  txt(m, '07 Nov 2026 · 14:02', { size: 14, color: t.text });
  txt(m, 'KREDIT · 3x angsuran', { size: 13, color: t.muted });
  const it = box(b, { name: 'barang', sh: 'fill', layout: 'vertical', pad: 16, gap: 10, r: 16, fill: t.surface, stroke: t.outline });
  for (const [n, q, h, tot] of [['Pertalite', '25,00 L', 10000, 250000], ['Dexlite', '15,00 L', 13000, 195000]]) {
    const r = box(it, { name: `baris ${n}`, sh: 'fill', layout: 'horizontal', main: 'spaceBetween', cross: 'center' });
    const c = box(r, { name: 'info', layout: 'vertical', gap: 1 });
    txt(c, n, { size: 14, weight: 'Medium', color: t.text });
    txt(c, `${q} × ${rp(h)}`, { size: 12, color: t.muted });
    txt(r, rp(tot), { size: 14, color: t.text });
  }
  box(it, { name: 'garis', sh: 'fill', h: 1, fill: t.outline });
  row(it, t, 'Bruto', rp(445000));
  row(it, t, 'Potongan', rp(0));
  row(it, t, 'Total', rp(445000), { big: true });
  const jd = box(b, { name: 'jadwal angsuran', sh: 'fill', layout: 'vertical', pad: 16, gap: 10, r: 16, fill: t.surface, stroke: t.outline });
  txt(jd, 'Jadwal angsuran', { size: 14, weight: 'SemiBold', color: t.text });
  row(jd, t, 'Ke-1 · 30 Nov 2026', rp(148333));
  row(jd, t, 'Ke-2 · 31 Des 2026', rp(148333));
  row(jd, t, 'Ke-3 · 31 Jan 2027', rp(148334));
  return f;
}

export function adminUsers(t, x, y) {
  const f = phone(t, 'Admin - daftar user', x, y);
  appBar(f, t, 'Admin · User Mobile', { actions: ['logout'] });
  const ls = box(f, { name: 'daftar user', sh: 'fill', sv: 'fill', layout: 'vertical', clip: true });
  const tag = (p, s, bg, fg) => { const g = box(p, { name: `label ${s}`, h: 22, r: 11, pad: [0, 8, 0, 8], layout: 'horizontal', cross: 'center', fill: bg }); txt(g, s, { size: 11, weight: 'SemiBold', color: fg }); };
  const u = (nama, un, role, extra, st) => {
    const r = box(ls, { name: `user ${un}`, sh: 'fill', h: 76, layout: 'horizontal', pad: [0, 8, 0, 16], cross: 'center', gap: 12 });
    const c = box(r, { name: 'info', sh: 'fill', layout: 'vertical', gap: 4 });
    txt(c, `${nama} (${un})`, { size: 16, weight: 'Medium', color: t.text, sh: 'fill' });
    const g = box(c, { name: 'label', layout: 'horizontal', gap: 6, cross: 'center' });
    tag(g, role, t.primaryCont, t.onPrimaryCont);
    if (extra) txt(g, extra, { size: 12, color: t.muted });
    if (st === 'nonaktif') tag(g, 'NONAKTIF', t.surface2, t.muted);
    if (st === 'kunci') tag(g, 'TERKUNCI', t.warnCont, t.warn);
    const mo = box(r, { name: 'menu', w: 48, h: 48, layout: 'horizontal', main: 'center', cross: 'center' });
    icon(mo, 'more', t.muted);
    box(ls, { name: 'pemisah', sh: 'fill', h: 1, fill: t.outline, opacity: 0.5 });
  };
  u('Rina Kasir', 'kasir.rina', 'KASIR', '', '');
  u('Dedi Kasir', 'kasir.dedi', 'KASIR', '', 'kunci');
  u('Budi Santoso', 'budi.santoso', 'ANGGOTA', '12.34.0021', '');
  u('Sari Wulandari', 'sari.w', 'ANGGOTA', '12.35.0033', 'nonaktif');
  u('Admin Koperasi', 'admin', 'ADMIN', '', '');
  const fb = box(f, { name: 'ruang tombol', sh: 'fill', layout: 'horizontal', pad: [8, 16, 24, 16], main: 'max' });
  const fab = box(fb, { name: 'tombol user baru', h: 56, r: 16, pad: [0, 20, 0, 16], layout: 'horizontal', cross: 'center', gap: 8, fill: { type: 'linearGradient', stops: t.grad }, shadow: { y: 4, blur: 10 } });
  icon(fab, 'plus', t.onPrimary, 22);
  txt(fab, 'User', { size: 16, weight: 'SemiBold', color: t.onPrimary });
  return f;
}

export function adminBuat(t, x, y) {
  const f = phone(t, 'Admin - user baru', x, y);
  const sc = box(f, { name: 'latar digelapkan', sh: 'fill', sv: 'fill', fill: t.scrim, layout: 'vertical', main: 'center', pad: [0, 24, 0, 24] });
  const d = box(sc, { name: 'dialog user baru', sh: 'fill', layout: 'vertical', pad: 24, gap: 14, r: 28, fill: t.surface });
  txt(d, 'User baru', { size: 26, color: t.text, serif: true });
  field(d, t, 'Username (huruf kecil)', 'budi.santoso', { value: true });
  field(d, t, 'Nama', 'Budi Santoso', { value: true });
  field(d, t, 'Role', 'ANGGOTA', { value: true, trail: 'chevD' });
  field(d, t, 'NIK anggota', '12.34.0021', { value: true, focus: true });
  const ac = box(d, { name: 'aksi', sh: 'fill', layout: 'horizontal', main: 'max', gap: 8, pad: [8, 0, 0, 0] });
  const bt = (l, p) => { const b = box(ac, { name: l, h: 44, r: 22, pad: [0, 20, 0, 20], layout: 'horizontal', cross: 'center', fill: p ? t.primary : 'none' }); txt(b, l, { size: 14, weight: 'SemiBold', color: p ? t.onPrimary : t.primary }); };
  bt('Batal', false); bt('Buat', true);
  return f;
}

export function passwordSementara(t, x, y) {
  const f = phone(t, 'Admin - password sementara', x, y);
  const sc = box(f, { name: 'latar digelapkan', sh: 'fill', sv: 'fill', fill: t.scrim, layout: 'vertical', main: 'center', pad: [0, 24, 0, 24] });
  const d = box(sc, { name: 'dialog password', sh: 'fill', layout: 'vertical', pad: 24, gap: 14, r: 28, fill: t.surface });
  const h = box(d, { name: 'judul', layout: 'horizontal', gap: 10, cross: 'center' });
  icon(h, 'lock', t.primary, 24);
  txt(h, 'User dibuat', { size: 26, color: t.text, serif: true });
  txt(d, 'Password sementara (hanya tampil sekali). User wajib menggantinya saat login pertama.', { size: 14, color: t.muted, sh: 'fill' });
  const pw = box(d, { name: 'password', sh: 'fill', h: 64, r: 14, layout: 'horizontal', main: 'center', cross: 'center', fill: t.surface2 });
  txt(pw, '7f3a9c1e04b2', { size: 24, weight: 'Bold', color: t.text, ls: 2 });
  const b = box(d, { name: 'aksi', sh: 'fill', layout: 'vertical' });
  button(b, t, 'Sudah dicatat', { h: 52 });
  return f;
}

export function gantiPassword(t, x, y) {
  const f = phone(t, 'Ganti password wajib', x, y);
  appBar(f, t, 'Ganti Password');
  const b = box(f, { name: 'isi', sh: 'fill', sv: 'fill', layout: 'vertical', pad: [8, 24, 24, 24], gap: 16 });
  const i = box(b, { name: 'info', sh: 'fill', layout: 'horizontal', pad: 14, gap: 10, cross: 'center', r: 14, fill: t.warnCont });
  icon(i, 'lock', t.warn, 22);
  txt(i, 'Anda memakai password sementara. Buat password baru untuk melanjutkan.', { size: 13, color: t.warn, sh: 'fill' });
  field(b, t, 'Password sementara', '••••••••••••', { trail: 'eye' });
  field(b, t, 'Password baru (min. 8 karakter)', '••••••••••', { focus: true, trail: 'eye' });
  field(b, t, 'Ulangi password baru', '••••••••••', { trail: 'eye' });
  box(b, { name: 'pengisi', sh: 'fill', sv: 'fill' });
  button(b, t, 'Simpan');
  return f;
}
