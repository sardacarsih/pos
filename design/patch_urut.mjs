import { readFileSync, writeFileSync } from 'node:fs';
let s = readFileSync('build.mjs', 'utf8');
const a = s.indexOf('/** Susun layar');
const b = s.indexOf('for (const [role, bangun]');
if (a < 0 || b < 0) throw new Error('penanda tidak ketemu');
const baru = `/**
 * Susun layar menurut urutan alur. Tiap baris = array [fn, tema, opsi?, lebar?]; nomor bertambah
 * mengikuti urutan baca (kiri ke kanan, atas ke bawah) dan menjadi awalan nama frame.
 */
function susun(baris) {
  let n = 0;
  baris.forEach((row, r) => {
    let x = 0;
    for (const [fn, tema, o, lebar] of row) {
      setAwalan(String(++n).padStart(2, '0'));
      o ? fn(tema, x, r * H, o) : fn(tema, x, r * H);
      x += (lebar ?? 375) + 60;
    }
  });
}

const TAB = 1024;
const dokumen = {
  umum: () => susun([
    [[login, light], [gantiPassword, light], [login, dark], [gantiPassword, dark]],
  ]),
  // Alur BBM: pilih BBM -> jumlah -> keranjang -> pelanggan (+QR) -> bayar -> faktur.
  kasir: () => susun([
    [[bbmGrid, light], [bbmDetail, light], [bbmKeranjang, light], [pelanggan, light], [qrScan, light], [qrDitemukan, light], [qrTidakDitemukan, light], [qrIzin, light], [bayarTunai, light], [bayarKredit, light], [bayarGagalLimit, light], [bbmStruk, light]],
    [[bbmGrid, light, { offline: true }], [bbmTablet, light, undefined, TAB], [bbmTablet, dark, undefined, TAB]],
    [[bbmGrid, dark], [bbmDetail, dark], [bbmKeranjang, dark], [qrDitemukan, dark], [qrTidakDitemukan, dark], [bayarTunai, dark]],
  ]),
  anggota: () => susun([
    [[transaksiSaya, light], [detailFaktur, light], [kartuAnggota, light], [transaksiSaya, dark], [detailFaktur, dark], [kartuAnggota, dark]],
  ]),
  admin: () => susun([
    [[adminUsers, light], [adminBuat, light], [passwordSementara, light], [adminUsers, dark], [adminBuat, dark], [passwordSementara, dark]],
  ]),
};

`;
s = s.slice(0, a) + baru + s.slice(b);
s = s.replace("import { cmds, reset } from './lib.mjs';", "import { cmds, reset, setAwalan } from './lib.mjs';");
writeFileSync('build.mjs', s);
console.log('ok');
