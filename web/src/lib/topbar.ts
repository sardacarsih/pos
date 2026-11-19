// Logika murni topbar: breadcrumb, status periode, inisial avatar, judul dokumen, dan navigasi keyboard menu.
import type { Role } from './types';
import type { Menu } from './nav';

export const NAMA_PERAN: Record<Role, string> = { KASIR: 'Kasir', ANGGOTA: 'Anggota', ADMIN: 'Admin', PEMBELIAN: 'Petugas pembelian' };

/** Kelompok menu yang tidak ditampilkan di breadcrumb karena redundan dengan judul halaman. */
const KELOMPOK_DISEMBUNYIKAN = ['Ringkasan'];

/** Breadcrumb: `Peran › Kelompok menu`. Halaman aktif adalah h1, bukan item breadcrumb. */
export function jejak(menu: Menu, view: string, peran: Role): string[] {
  const hasil = [NAMA_PERAN[peran]];
  const k = menu.kelompok.find(g => g.item.some(i => i.id === view));
  if (k?.judul && !KELOMPOK_DISEMBUNYIKAN.includes(k.judul)) hasil.push(k.judul);
  return hasil;
}

const BULAN = ['Jan', 'Feb', 'Mar', 'Apr', 'Mei', 'Jun', 'Jul', 'Agu', 'Sep', 'Okt', 'Nov', 'Des'];

/** 202611 -> "Nov 2026"; nilai tak valid dikembalikan apa adanya. */
export function namaPeriode(periode: number): string {
  const y = Math.floor(periode / 100), m = periode % 100;
  return m >= 1 && m <= 12 && y >= 2000 ? `${BULAN[m - 1]} ${y}` : String(periode);
}

export interface RespPeriode { terbuka?: boolean; kode?: string | null; pesan?: string | null; periode?: number | null; dari?: string | null; sampai?: string | null }
export type TingkatPeriode = 'ok' | 'peringatan' | 'bahaya';
export interface StatusPeriode { label: string; tingkat: TingkatPeriode; judul: string }

/** Status periode untuk chip topbar. Null bila respons tidak dikenali (chip tidak ditampilkan). */
export function statusPeriode(r: RespPeriode | null | undefined): StatusPeriode | null {
  if (!r || typeof r.terbuka !== 'boolean') return null;
  const nama = r.periode ? namaPeriode(r.periode) : '';
  const rentang = r.dari && r.sampai ? `${r.dari} s/d ${r.sampai}` : '';
  if (r.terbuka) {
    const label = nama ? `Periode ${nama} · Terbuka` : 'Periode terbuka';
    return { label, tingkat: 'ok', judul: rentang || label };
  }
  if (r.kode === 'PERIODE_TUTUP') return { label: 'Periode tutup', tingkat: 'bahaya', judul: r.pesan || rentang || 'Periode tutup' };
  if (r.kode === 'PERIODE_TIDAK_ADA') return { label: 'Periode belum diatur', tingkat: 'peringatan', judul: r.pesan || 'Periode belum diatur' };
  return { label: 'Periode belum valid', tingkat: 'peringatan', judul: r.pesan || 'Periode belum valid' };
}

/** Inisial avatar: dua kata -> dua huruf pertama; satu kata -> satu huruf; kosong -> "?". */
export function inisial(nama: string): string {
  const kata = (nama || '').trim().split(/\s+/).filter(Boolean);
  if (!kata.length) return '?';
  const a = Array.from(kata[0])[0];
  const b = kata.length > 1 ? Array.from(kata[1])[0] : '';
  return (a + b).toUpperCase();
}

/** Judul tab peramban: "Halaman · Kopkar POS" (hanya "Kopkar POS" bila belum ada halaman). */
export function judulDokumen(label: string): string { return label ? `${label} · Kopkar POS` : 'Kopkar POS'; }

/** Indeks item menu berikutnya untuk tombol navigasi keyboard (berputar); tombol lain tidak mengubah indeks. */
export function indeksMenu(sekarang: number, jumlah: number, tombol: string): number {
  if (jumlah <= 0) return 0;
  switch (tombol) {
    case 'ArrowDown': return (sekarang + 1) % jumlah;
    case 'ArrowUp': return (sekarang - 1 + jumlah) % jumlah;
    case 'Home': return 0;
    case 'End': return jumlah - 1;
    default: return sekarang;
  }
}
