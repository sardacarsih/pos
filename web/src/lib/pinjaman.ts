// Modul pinjaman: tipe dan logika murni (aksi yang boleh, validasi pengajuan). Tanpa DOM.
export type Status = 'DIAJUKAN' | 'DIVERIFIKASI' | 'DISETUJUI' | 'DICAIRKAN' | 'DITOLAK' | 'DIBATALKAN';
export type Tahap = 'VERIFIKASI' | 'SETUJU' | 'CAIR';
export type Aksi = 'verifikasi' | 'setujui' | 'tolak' | 'cairkan';

export interface Pengajuan {
  id: number; nik: string; nama: string; jumlah: number; tenor: number; bunga: number; angsuran: number;
  keperluan: string; status: Status; tgl_aju: string;
  verif_nama: string | null; verif_tgl: string | null; verif_catatan: string | null;
  setuju_nama: string | null; setuju_tgl: string | null; setuju_catatan: string | null;
  tolak_nama: string | null; tolak_tgl: string | null; alasan: string | null;
  cair_nama: string | null; cair_tgl: string | null; no_transaksi: string | null;
  antrian: number | null; verif_user: number | null; setuju_user: number | null;
}
export interface Aturan { bunga: number; maks_tenor: number; maks_pinjaman: number }
export interface BarisJadwal { ke: number; periode: number; jatuh_tempo: string; saldo_awal: number; pokok: number; bunga: number; angsuran: number; saldo_akhir: number }
export interface Antrian { periode: number; dari: string; sampai: string; plafon: number | null; terpakai: number; sisa: number | null; items: Pengajuan[] }

export const labelStatus: Record<Status, string> = {
  DIAJUKAN: 'Diajukan', DIVERIFIKASI: 'Diverifikasi', DISETUJUI: 'Disetujui — antre dana', DICAIRKAN: 'Dicairkan',
  DITOLAK: 'Ditolak', DIBATALKAN: 'Dibatalkan'
};
export const TAHAP: Tahap[] = ['VERIFIKASI', 'SETUJU', 'CAIR'];
export const labelTahap: Record<Tahap, string> = { VERIFIKASI: 'Verifikasi', SETUJU: 'Persetujuan', CAIR: 'Pencairan' };
export const LANGKAH: Status[] = ['DIAJUKAN', 'DIVERIFIKASI', 'DISETUJUI', 'DICAIRKAN'];

/** Aksi yang ditawarkan ke pengurus; server tetap memeriksa ulang (status, hak, antrian, pengurus berbeda). */
export function aksiTersedia(p: Pengajuan, hak: Tahap[], userId: number, kepalaAntrian: boolean): Aksi[] {
  const pegang = p.verif_user === userId || p.setuju_user === userId;
  const ada = (t: Tahap) => hak.includes(t);
  switch (p.status) {
    case 'DIAJUKAN': return ada('VERIFIKASI') ? ['verifikasi', 'tolak'] : [];
    case 'DIVERIFIKASI': return ada('SETUJU') ? [...(pegang ? [] : ['setujui' as Aksi]), 'tolak'] : [];
    case 'DISETUJUI': return [...(ada('CAIR') && !pegang && kepalaAntrian ? ['cairkan' as Aksi] : []), ...(ada('SETUJU') ? ['tolak' as Aksi] : [])];
    default: return [];
  }
}

/** Pesan kesalahan pertama, atau '' bila isian valid. */
export function cekAjuan(jumlah: number | undefined, tenor: number | undefined, keperluan: string, a: Aturan): string {
  if (!Number.isSafeInteger(jumlah) || !jumlah || jumlah <= 0) return 'Jumlah pinjaman harus bilangan bulat lebih dari nol.';
  if (a.maks_pinjaman > 0 && jumlah > a.maks_pinjaman) return `Jumlah pinjaman maksimal Rp ${a.maks_pinjaman.toLocaleString('id-ID')}.`;
  if (!Number.isInteger(tenor) || !tenor || tenor < 1 || tenor > a.maks_tenor) return `Tenor 1 sampai ${a.maks_tenor} bulan.`;
  if (!keperluan.trim() || keperluan.trim().length > 200) return 'Keperluan wajib, maksimal 200 karakter.';
  return '';
}
