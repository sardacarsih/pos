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
