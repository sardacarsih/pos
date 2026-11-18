// Satu dokumen per area aplikasi: umum, kasir, anggota, admin.
// Pemakaian: node build.mjs  (butuh figcraft-cli.exe; lokasi lewat env FIGCRAFT_CLI)
import { execFileSync } from 'node:child_process';
import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { cmds, reset, setAwalan } from './lib.mjs';
import { light, dark } from './theme.mjs';
import { login, kasirList } from './screens_a.mjs';
import { keranjang, pelanggan, bayarTunai, bayarKredit, bayarGagalLimit, struk, tablet } from './screens_b.mjs';
import { bbmGrid, bbmDetail, bbmKeranjang, bbmStruk, bbmTablet } from './screens_bbm.mjs';
import { loginHutan } from './screens_login.mjs';
import { qrScan, qrDitemukan, qrTidakDitemukan, qrIzin, kartuAnggota } from './screens_qr.mjs';
import { transaksiSaya, detailFaktur, adminUsers, adminBuat, passwordSementara, gantiPassword } from './screens_c.mjs';
import {
  dashboardKasir, dashboardKasirKosong, dashboardKasirPeriodeTutup, riwayatKasir, printerDialog, kalenderRiwayat,
  dashboardAnggota, belanjaAnggota, belanjaCheckout, belanjaDemoDialog,
  dashboardAdmin, adminProduk, adminFormProduk,
  adminPhotoOrPrice, adminUrutanProduk, adminRekap, adminRekapDetail,
  adminUsersError, profil, profilFotoSheet, hapusFotoDialog, loginHelpDialog,
  fotoCrop, updateDialog, changelog, stateLoadingError,
} from './screens_extra.mjs';

const CLI = process.env.FIGCRAFT_CLI ?? 'C:\\Program Files\\ArtCraft\\FigCraft\\figcraft-cli.exe';
const H = 812 + 100;

/**
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
// Adapter keeps each frame variant explicit while the screen constructors stay simple.
const dashboardKasirError = (t, x, y) => dashboardKasirKosong(t, x, y, true);
const kasirPeriodeTutup = (t, x, y) => dashboardKasirPeriodeTutup(t, x, y);
const dashboardAnggotaEmpty = (t, x, y) => dashboardAnggota(t, x, y, true);
const anggotaPeriodeTutup = (t, x, y) => dashboardAnggota(t, x, y, 'closed');
const dashboardAdminEmpty = (t, x, y) => dashboardAdmin(t, x, y, true);
const riwayatKasirEmpty = (t, x, y) => riwayatKasir(t, x, y, true);
const printerKosong = (t, x, y) => printerDialog(t, x, y, 'kosong');
const pilihTanggalRiwayat = (t, x, y) => kalenderRiwayat(t, x, y);
const adminKategoriTab = (t, x, y) => adminProduk(t, x, y, true);
const adminRekapEmpty = (t, x, y) => adminRekap(t, x, y, true);
const adminLimitOver = (t, x, y) => adminRekapDetail(t, x, y, true);
const fotoPermission = (t, x, y) => fotoCrop(t, x, y, true);
const loadingState = (t, x, y) => stateLoadingError(t, x, y, 'loading');
const offlineState = (t, x, y) => stateLoadingError(t, x, y, 'offline');
const profilePassword = (t, x, y) => profil(t, x, y, { password: true });
const adminPhotoSource = (t, x, y) => adminPhotoOrPrice(t, x, y, 'photo');
const adminPriceConfirm = (t, x, y) => adminPhotoOrPrice(t, x, y, 'price');
const adminProductsEmpty = (t, x, y) => adminProduk(t, x, y, false, true);
const hapusFotoProfil = (t, x, y) => hapusFotoDialog(t, x, y);
const loginInvalid = (t, x, y) => loginHutan(t, x, y, { error: true });
const bayarTunaiKurang = (t, x, y) => bayarTunai(t, x, y, true);
const belanjaTanpaHasil = (t, x, y) => belanjaAnggota(t, x, y, { empty: true, withCart: false });
const belanjaGagal = (t, x, y) => belanjaAnggota(t, x, y, { error: true, withCart: false });
// Tiap file: bagian TERANG dulu (alur utama lalu kondisi/perangkat), baru bagian GELAP; tidak ada baris campuran.
const dokumen = {
  umum: () => susun([
    [[loginHutan, light], [loginInvalid, light], [loginHelpDialog, light], [gantiPassword, light], [profil, light], [profilePassword, light], [profilFotoSheet, light], [hapusFotoProfil, light], [fotoCrop, light], [changelog, light]],
    [[fotoPermission, light], [updateDialog, light], [loadingState, light], [stateLoadingError, light], [offlineState, light]],
    [[loginHutan, dark], [loginInvalid, dark], [loginHelpDialog, dark], [gantiPassword, dark], [profil, dark], [profilePassword, dark], [profilFotoSheet, dark], [hapusFotoProfil, dark], [fotoCrop, dark], [changelog, dark]],
    [[fotoPermission, dark], [updateDialog, dark], [loadingState, dark], [stateLoadingError, dark], [offlineState, dark]],
  ]),
  // Alur kasir mengikuti kode: dashboard, katalog BBM, keranjang, pelanggan, QR, bayar, faktur, riwayat.
  kasir: () => susun([
    [[dashboardKasir, light], [dashboardKasirKosong, light], [kasirPeriodeTutup, light], [dashboardKasirError, light], [bbmGrid, light], [bbmDetail, light], [bbmKeranjang, light], [pelanggan, light], [qrScan, dark], [qrDitemukan, light], [qrTidakDitemukan, light], [qrIzin, light], [bayarTunai, light], [bayarTunaiKurang, light], [bayarKredit, light], [bayarGagalLimit, light], [bbmStruk, light]],
    [[bbmGrid, light, { offline: true }], [riwayatKasir, light], [riwayatKasirEmpty, light], [pilihTanggalRiwayat, light], [printerDialog, light], [printerKosong, light], [bbmTablet, light, undefined, TAB]],
    [[dashboardKasir, dark], [dashboardKasirKosong, dark], [kasirPeriodeTutup, dark], [dashboardKasirError, dark], [bbmGrid, dark], [bbmDetail, dark], [bbmKeranjang, dark], [pelanggan, dark], [qrDitemukan, dark], [qrTidakDitemukan, dark], [qrIzin, dark], [bayarTunai, dark], [bayarTunaiKurang, dark], [bayarKredit, dark], [bayarGagalLimit, dark], [bbmStruk, dark]],
    [[bbmGrid, dark, { offline: true }], [riwayatKasir, dark], [riwayatKasirEmpty, dark], [pilihTanggalRiwayat, dark], [printerDialog, dark], [printerKosong, dark], [bbmTablet, dark, undefined, TAB]],
  ]),
  anggota: () => susun([
    [[dashboardAnggota, light], [dashboardAnggotaEmpty, light], [anggotaPeriodeTutup, light], [belanjaAnggota, light], [belanjaAnggota, light, { demo: true }], [belanjaTanpaHasil, light], [belanjaGagal, light], [belanjaCheckout, light], [belanjaDemoDialog, light], [transaksiSaya, light], [detailFaktur, light], [kartuAnggota, light]],
    [[dashboardAnggota, dark], [dashboardAnggotaEmpty, dark], [anggotaPeriodeTutup, dark], [belanjaAnggota, dark], [belanjaAnggota, dark, { demo: true }], [belanjaTanpaHasil, dark], [belanjaGagal, dark], [belanjaCheckout, dark], [belanjaDemoDialog, dark], [transaksiSaya, dark], [detailFaktur, dark], [kartuAnggota, dark]],
  ]),
  admin: () => susun([
    [[dashboardAdmin, light], [dashboardAdminEmpty, light], [adminProduk, light], [adminProductsEmpty, light], [adminKategoriTab, light], [adminFormProduk, light], [adminPhotoSource, light], [adminPriceConfirm, light], [adminUrutanProduk, light], [adminRekap, light], [adminRekapEmpty, light], [adminRekapDetail, light], [adminLimitOver, light], [adminUsers, light], [adminBuat, light], [passwordSementara, light], [adminUsersError, light]],
    [[dashboardAdmin, dark], [dashboardAdminEmpty, dark], [adminProduk, dark], [adminProductsEmpty, dark], [adminKategoriTab, dark], [adminFormProduk, dark], [adminPhotoSource, dark], [adminPriceConfirm, dark], [adminUrutanProduk, dark], [adminRekap, dark], [adminRekapEmpty, dark], [adminRekapDetail, dark], [adminLimitOver, dark], [adminUsers, dark], [adminBuat, dark], [passwordSementara, dark], [adminUsersError, dark]],
  ]),
};

for (const [role, bangun] of Object.entries(dokumen)) {
  reset();
  bangun();
  const skrip = `${role}.script.json`;
  const berkas = `${role}.figcraft`;
  const png = `png/${role}`;
  writeFileSync(skrip, JSON.stringify(cmds));
  const out = execFileSync(CLI, ['run', skrip, '--out', berkas], { encoding: 'utf8' });
  if (/"ok":false/.test(out)) throw new Error(`${role}: ${out}`);
  rmSync(png, { recursive: true, force: true });
  mkdirSync(png, { recursive: true });
  execFileSync(CLI, ['export', berkas, '--out', png], { encoding: 'utf8' });
  console.log(`${role}: ${cmds.length} perintah -> ${berkas}`);
}
