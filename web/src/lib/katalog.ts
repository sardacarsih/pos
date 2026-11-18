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
