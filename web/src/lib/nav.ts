// Struktur menu per peran (satu sumber data untuk sidebar dan bottom-nav) + helper lencana. Murni: tanpa komponen/DOM.
import type { Role } from './types';

export type NamaIkon =
  | 'beranda' | 'pengguna' | 'produk' | 'stokHabis' | 'opname' | 'hargaRendah'
  | 'rekap' | 'tagihan' | 'profil' | 'kasir' | 'belanja' | 'riwayat' | 'kartu' | 'pembelian' | 'kas' | 'pinjaman';

export type JenisBadge = 'stok_habis' | 'harga_rendah';

/** `pendek` = label ringkas untuk bottom-nav (lebar sempit); aria-label tetap memakai `label` penuh. */
export interface ItemMenu { id: string; label: string; pendek?: string; ikon: NamaIkon; badge?: JenisBadge }
export interface Kelompok { judul: string | null; item: ItemMenu[] }
export interface Menu {
  kelompok: Kelompok[];
  /** Tujuan bottom-nav (maks. 4); tombol "Lainnya" ditambahkan oleh komponen sehingga totalnya maks. 5. */
  utama: string[];
}
export type Badge = Partial<Record<JenisBadge, number>>;

/** Maksimum tujuan di bottom-nav termasuk "Lainnya" (pedoman Material/HIG). */
export const MAKS_BOTTOM_NAV = 5;

/** Semua id view yang dikenali router di +page.svelte. */
export const VIEW_DIKENAL = ['dashboard', 'belanja', 'riwayat', 'kartu', 'profil', 'users', 'anggota-admin', 'produk', 'persediaan', 'harga', 'rekap', 'tagihan-rekap', 'pembelian', 'kas', 'pinjaman'] as const;

const AKUN: Kelompok = { judul: 'Akun', item: [{ id: 'profil', label: 'Profil', ikon: 'profil' }] };
const PINJAMAN: ItemMenu = { id: 'pinjaman', label: 'Pinjaman', ikon: 'pinjaman' };
const PEMBELIAN: ItemMenu = { id: 'pembelian', label: 'Pembelian', ikon: 'pembelian' };

export const MENU_OFFLINE: Menu = { kelompok: [{ judul: null, item: [PEMBELIAN] }], utama: ['pembelian'] };

const MENU: Record<Role, Menu> = {
  PEMBELIAN: { kelompok: [{ judul: 'Pengadaan', item: [PEMBELIAN] }], utama: ['pembelian'] },
  KASIR: {
    kelompok: [
      { judul: null, item: [{ id: 'dashboard', label: 'Beranda', ikon: 'beranda' }, { id: 'belanja', label: 'Kasir', ikon: 'kasir' }, { id: 'riwayat', label: 'Riwayat', ikon: 'riwayat' }] },
      AKUN
    ],
    utama: ['dashboard', 'belanja', 'riwayat', 'profil']
  },
  ANGGOTA: {
    kelompok: [
      { judul: null, item: [{ id: 'dashboard', label: 'Beranda', ikon: 'beranda' }, { id: 'belanja', label: 'Belanja', ikon: 'belanja' }, { id: 'riwayat', label: 'Transaksi saya', pendek: 'Transaksi', ikon: 'riwayat' }, { id: 'kartu', label: 'Kartu anggota', pendek: 'Kartu', ikon: 'kartu' }, PINJAMAN] },
      AKUN
    ],
    utama: ['dashboard', 'belanja', 'riwayat', 'kartu']
  },
  ADMIN: {
    kelompok: [
      { judul: 'Ringkasan', item: [{ id: 'dashboard', label: 'Beranda', ikon: 'beranda' }] },
      { judul: 'Pantau', item: [{ id: 'persediaan', label: 'Persediaan', pendek: 'Stok', ikon: 'stokHabis', badge: 'stok_habis' }, { id: 'harga', label: 'Harga ≤ HPP', ikon: 'hargaRendah', badge: 'harga_rendah' }] },
      { judul: 'Katalog', item: [{ id: 'produk', label: 'Katalog produk', ikon: 'produk' }] },
      { judul: 'Pengadaan', item: [PEMBELIAN] },
      { judul: 'Anggota & keuangan', item: [{ id: 'rekap', label: 'Rekap anggota', pendek: 'Rekap', ikon: 'rekap' }, { id: 'tagihan-rekap', label: 'Tagihan vs Rekap', ikon: 'tagihan' }, { id: 'kas', label: 'Kas & Bank', pendek: 'Kas', ikon: 'kas' }, PINJAMAN] },
      { judul: 'Pengelolaan', item: [{ id: 'users', label: 'Pengguna', ikon: 'pengguna' }, { id: 'anggota-admin', label: 'Anggota Koperasi', ikon: 'pengguna' }] },
      AKUN
    ],
    utama: ['dashboard', 'persediaan', 'rekap']
  }
};

export function menuUntuk(peran: Role): Menu { return MENU[peran]; }
export function semuaItem(m: Menu): ItemMenu[] { return m.kelompok.flatMap(k => k.item); }
export function itemUtama(m: Menu): ItemMenu[] { const semua = semuaItem(m); return m.utama.map(id => semua.find(i => i.id === id)!).filter(Boolean); }
/** Label halaman aktif (untuk topbar / judul). */
export function labelView(m: Menu, id: string): string { return semuaItem(m).find(i => i.id === id)?.label ?? ''; }

/** Teks lencana: kosong untuk 0/negatif/bukan angka, "99+" untuk >= 100. */
export function labelBadge(n: number | null | undefined): string {
  if (!n || !Number.isFinite(n) || n <= 0) return '';
  return n >= 100 ? '99+' : String(Math.floor(n));
}

export function jumlahBadge(item: ItemMenu, badge: Badge): number { return item.badge ? badge[item.badge] ?? 0 : 0; }

/** Teks untuk pembaca layar: "Stok habis, 12 produk perlu perhatian". Hanya nama bila tidak ada lencana. */
export function ariaBadge(item: ItemMenu, n: number): string {
  const l = labelBadge(n);
  return l ? `${item.label}, ${l} produk perlu perhatian` : item.label;
}
