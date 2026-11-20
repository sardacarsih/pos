// Helper murni module Persediaan: tab, rentang kartu stok, validasi penyesuaian (cermin server), label mutasi.
export type TabSedia = 'kartu' | 'habis' | 'opname' | 'sesuai' | 'valuasi' | 'valuasi-item' | 'rusak';

export const TAB_SEDIA: { id: TabSedia; label: string }[] = [
  { id: 'kartu', label: 'Kartu stok' },
  { id: 'habis', label: 'Stok habis' },
  { id: 'opname', label: 'Stock opname' },
  { id: 'sesuai', label: 'Penyesuaian' },
  { id: 'valuasi', label: 'Valuasi & alert' },
  { id: 'valuasi-item', label: 'Valuasi Item' },
  { id: 'rusak', label: 'Barang rusak' }
];

const POLA_TANGGAL = /^\d{4}-\d{2}-\d{2}$/;

function tanggalValid(s: string): boolean {
  if (!POLA_TANGGAL.test(s)) return false;
  const d = new Date(s + 'T00:00:00Z');
  return !Number.isNaN(d.getTime()) && d.toISOString().slice(0, 10) === s;
}

export function awalTahun(hariIni: string): string {
  return hariIni.slice(0, 4) + '-01-01';
}

/** Default 1 Jan..hari ini, clamp ke tahun berjalan; rentang terbalik/tanggal rusak = lempar (cermin server). */
export function rentangKartu(dari: string | null | undefined, sampai: string | null | undefined, hariIni: string): { dari: string; sampai: string } {
  const awal = awalTahun(hariIni);
  const d = dari?.trim() || awal;
  const s = sampai?.trim() || hariIni;
  if (!tanggalValid(d) || !tanggalValid(s)) throw new Error('Tanggal tidak valid (YYYY-MM-DD)');
  const dc = d < awal ? awal : d > hariIni ? hariIni : d;
  const sc = s < awal ? awal : s > hariIni ? hariIni : s;
  if (sc < dc) throw new Error('Rentang tanggal terbalik');
  return { dari: dc, sampai: sc };
}

export interface IsianSesuai { produk_id: number; qty: string; tanggal: string; alasan: string }

const MAKS_QTY = 99999999.9999;
const POLA_QTY = /^-?\d+(\.\d{1,4})?$/;

/** Mengembalikan pesan galat pertama atau null bila valid. Pesan sama dengan server. */
export function validasiSesuaikan(f: IsianSesuai, hariIni: string): string | null {
  if (!f.produk_id) return 'Pilih produk terlebih dahulu';
  const qty = f.qty.trim();
  if (!POLA_QTY.test(qty)) return 'Qty penyesuaian tidak valid';
  const n = Number(qty);
  if (n === 0) return 'Qty penyesuaian tidak boleh nol';
  if (Math.abs(n) > MAKS_QTY) return 'Qty penyesuaian tidak valid';
  const alasan = f.alasan.trim();
  if (alasan.length < 3 || alasan.length > 250) return 'Alasan wajib diisi (3-250 karakter)';
  const t = f.tanggal.trim();
  if (!tanggalValid(t)) return 'Tanggal tidak valid (YYYY-MM-DD)';
  if (t < awalTahun(hariIni) || t > hariIni) return 'Tanggal penyesuaian harus dalam tahun berjalan sampai hari ini';
  return null;
}

const LABEL_MUTASI: Record<string, string> = {
  pembelian: 'Pembelian',
  penjualan: 'Penjualan',
  opname: 'Stock opname',
  rusak: 'Barang rusak',
  penyesuaian: 'Penyesuaian stok',
  penyesuaian_balik: 'Pembalikan penyesuaian'
};

export function labelJenisMutasi(jenis: string): string {
  return LABEL_MUTASI[jenis] ?? jenis;
}
