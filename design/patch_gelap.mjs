// Sekali jalan: pisahkan bagian terang dan bagian gelap (tidak dicampur dalam satu baris).
import { readFileSync, writeFileSync } from 'node:fs';

let s = readFileSync('build.mjs', 'utf8');
const a = s.indexOf('const dokumen = {');
const b = s.indexOf('for (const [role, bangun]');
if (a < 0 || b < 0) throw new Error('penanda tidak ketemu');
const baru = `// Tiap file: bagian TERANG dulu (alur utama lalu kondisi/perangkat), baru bagian GELAP; tidak ada baris campuran.
const dokumen = {
  umum: () => susun([
    [[login, light], [gantiPassword, light]],
    [[login, dark], [gantiPassword, dark]],
  ]),
  // Alur BBM: pilih BBM -> jumlah -> keranjang -> pelanggan (+QR) -> bayar -> faktur.
  kasir: () => susun([
    [[bbmGrid, light], [bbmDetail, light], [bbmKeranjang, light], [pelanggan, light], [qrScan, light], [qrDitemukan, light], [qrTidakDitemukan, light], [qrIzin, light], [bayarTunai, light], [bayarKredit, light], [bayarGagalLimit, light], [bbmStruk, light]],
    [[bbmGrid, light, { offline: true }], [bbmTablet, light, undefined, TAB]],
    [[bbmGrid, dark], [bbmDetail, dark], [bbmKeranjang, dark], [qrDitemukan, dark], [qrTidakDitemukan, dark], [bayarTunai, dark]],
    [[bbmTablet, dark, undefined, TAB]],
  ]),
  anggota: () => susun([
    [[transaksiSaya, light], [detailFaktur, light], [kartuAnggota, light]],
    [[transaksiSaya, dark], [detailFaktur, dark], [kartuAnggota, dark]],
  ]),
  admin: () => susun([
    [[adminUsers, light], [adminBuat, light], [passwordSementara, light]],
    [[adminUsers, dark], [adminBuat, dark], [passwordSementara, dark]],
  ]),
};

`;
s = s.slice(0, a) + baru + s.slice(b);
writeFileSync('build.mjs', s);
console.log('ok');
