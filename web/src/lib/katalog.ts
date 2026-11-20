import { statusHarga } from './beranda';
/** Filter daftar produk -> parameter query API (kosong = tidak difilter). */
export function paramKatalog(q: string, aktif: '' | 'Y' | 'N', kategori: string, page: number): Record<string, string | number> {
  return { q: q.trim(), aktif, kategori, page, size: 100 };
}

/** Inisial untuk avatar produk tanpa foto: dua karakter pertama nama (huruf/angka), kapital. */
export function inisial(nama: string): string {
  return (nama.match(/[\p{L}\p{N}]/gu) || []).slice(0, 2).join('').toUpperCase() || '?';
}

/** Isi badan PUT /admin/produk/{id} dengan status aktif dibalik (sisanya tidak berubah). */
export function badanToggleAktif(p: { nama: string; barcode?: string | null; satuan: string; kategori_id: string; harga: number; beli: number; aktif: boolean }) {
  return { nama: p.nama, barcode: p.barcode ?? '', satuan: p.satuan, kategori_id: p.kategori_id, harga: Number(p.harga), beli: Number(p.beli), aktif: !p.aktif };
}

export type IsianProduk = { nama: string; barcode?: string | null; satuan: string; kategori_id: string; harga: number | string; beli: number | string; aktif: boolean };

/** Badan POST/PUT /admin/produk: angka dinormalkan, barcode kosong jadi string kosong. */
export function badanProduk(p: IsianProduk) {
  return { nama: p.nama.trim(), barcode: (p.barcode ?? '').trim(), satuan: p.satuan.trim(), kategori_id: p.kategori_id, harga: Number(p.harga), beli: Number(p.beli), aktif: p.aktif };
}

/** Isian awal modal Tambah produk (id 0 = belum tersimpan). */
export function produkBaru(kategoriId = '') {
  return { id: 0, nama: '', barcode: '', satuan: 'PCS', kategori_id: kategoriId, harga: '', beli: '', aktif: true };
}

/** Pesan kesalahan isian produk baru sebelum dikirim (server tetap memvalidasi); null = lolos. */
export function cekProdukBaru(p: IsianProduk): string | null {
  if (!p.nama.trim()) return 'Nama wajib diisi.';
  if (!p.satuan.trim()) return 'Satuan wajib diisi.';
  if (!p.kategori_id) return 'Pilih kategori.';
  const harga = Number(p.harga), beli = Number(p.beli);
  if (!(harga > 0) || !(beli > 0)) return 'Harga jual dan harga beli harus lebih dari 0.';
  if (harga <= beli) return 'Harga jual harus lebih besar dari harga beli.';
  return null;
}

export type RingkasMargin = { tipe: 'ok' | 'impas' | 'rugi' | 'tanpa-hpp'; margin: number | null; persen: number | null };

/** Margin langsung saat mengetik harga: Rp dan % dari harga jual; HPP kosong = belum bisa dihitung. */
export function ringkasMargin(harga: number | string, beli: number | string | null | undefined): RingkasMargin {
  const s = statusHarga(Number(harga) || 0, Number(beli) || 0);
  if (s.tipe === 'tanpa-hpp' || s.margin === null) return { tipe: 'tanpa-hpp', margin: null, persen: null };
  const jual = Number(harga) || 0;
  return { tipe: s.tipe === 'rugi' ? 'rugi' : s.margin === 0 ? 'impas' : 'ok', margin: s.margin, persen: jual > 0 ? (s.margin / jual) * 100 : null };
}

export type SatuanIsian = { satuan: string; isi?: number | string; harga?: number | string; barcode?: string | null };

/** True bila isian produk atau satuan tambahan berbeda dari kondisi awal (spasi tepi dan tipe angka diabaikan). */
export function adaPerubahan(awal: { produk: IsianProduk; satuan: SatuanIsian[] }, kini: { produk: IsianProduk; satuan: SatuanIsian[] }): boolean {
  const norm = (x: { produk: IsianProduk; satuan: SatuanIsian[] }) =>
    JSON.stringify([badanProduk(x.produk), x.satuan.map((s) => [s.satuan.trim(), Number(s.isi) || 0, Number(s.harga) || 0, (s.barcode ?? '').trim()])]);
  return norm(awal) !== norm(kini);
}
