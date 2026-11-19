// Modul Kas & Bank: tipe dan logika murni (validasi isian, label). Tanpa DOM.
export type JenisAkun = 'KAS' | 'BANK';
export type Sumber = 'PENJUALAN' | 'BELI_BAYAR' | 'PINJAMAN' | 'MANUAL' | 'TRANSFER';

export interface Akun {
  id: number; kode: string; nama: string; jenis: JenisAkun; no_rekening: string | null; atas_nama: string | null;
  saldo_awal: number; tgl_saldo_awal: string; default_untuk: JenisAkun | null; aktif: boolean; saldo: number;
}
export interface Mutasi {
  id: number; tanggal: string; masuk: number; keluar: number; saldo: number; keterangan: string;
  sumber: Sumber; ref: string | null; user: string | null; pembalik: boolean; dibalik: boolean;
}
export interface Buku { saldo_awal: number; saldo_akhir: number; mutasi: Mutasi[] }

export const labelSumber: Record<Sumber, string> = {
  PENJUALAN: 'Penjualan tunai', BELI_BAYAR: 'Bayar supplier', PINJAMAN: 'Pinjaman', MANUAL: 'Manual', TRANSFER: 'Transfer'
};

/** Hanya mutasi manual/transfer yang belum dibalik dan bukan pembalik yang boleh dibatalkan dari web. */
export const bisaDibatal = (m: Mutasi) => (m.sumber === 'MANUAL' || m.sumber === 'TRANSFER') && !m.dibalik && !m.pembalik;

export type JenisInput = 'M' | 'K' | 'TRANSFER';
export interface IsianMutasi { jenis: JenisInput; akun: number | ''; ke: number | ''; tanggal: string; nominal: number | undefined; keterangan: string }

/** Pesan kesalahan pertama, atau '' bila isian valid. */
export function cekMutasi(i: IsianMutasi, hariIni: string): string {
  if (!i.akun) return 'Pilih akun.';
  if (i.jenis === 'TRANSFER' && (!i.ke || i.ke === i.akun)) return 'Pilih akun tujuan yang berbeda.';
  if (!i.tanggal || i.tanggal > hariIni) return 'Tanggal wajib dan tidak boleh melewati hari ini.';
  if (!Number.isSafeInteger(i.nominal) || !i.nominal || i.nominal <= 0) return 'Nominal harus bilangan bulat lebih dari nol.';
  if (!i.keterangan.trim() || i.keterangan.trim().length > 250) return 'Keterangan wajib, maksimal 250 karakter.';
  return '';
}

/** Path + body API untuk isian yang sudah valid. */
export function permintaanMutasi(i: IsianMutasi): { path: string; body: object } {
  const dasar = { tanggal: i.tanggal, nominal: i.nominal, keterangan: i.keterangan.trim() };
  return i.jenis === 'TRANSFER'
    ? { path: '/kas/transfer', body: { ...dasar, dari_akun: i.akun, ke_akun: i.ke } }
    : { path: '/kas/mutasi', body: { ...dasar, akun_id: i.akun, arah: i.jenis } };
}
