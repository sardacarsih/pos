import { jumlahFisik } from './opname';

/// Satu baris yang diedit pada Ubah Opname / Barang Rusak: jumlah dalam satuan dasar sebagai teks.
export interface BarisEdit {
  produk_id: number;
  kode: string;
  nama: string;
  satuan: string;
  qty: string;
  keterangan: string;
}

/// Pesan masalah pertama sebelum kirim, atau null bila siap. Nol hanya diizinkan untuk SO (`izinNol`).
export function bolehSimpan(baris: BarisEdit[], alasan: string, perluAlasan: boolean, izinNol: boolean): string | null {
  if (!baris.length) return 'Daftar barang masih kosong.';
  for (const b of baris) {
    const n = jumlahFisik(b.qty);
    if (n === null) return `Jumlah ${b.nama} belum diisi dengan angka yang valid.`;
    if (!izinNol && n <= 0) return `Jumlah ${b.nama} harus lebih dari 0.`;
  }
  if (perluAlasan && !alasan.trim()) return 'Alasan wajib diisi.';
  return null;
}

/// Kunci idempotensi dipakai ulang selama isi sama (retry aman); isi berubah = kunci baru.
export function kunciBaru(kunci: string, sidikLama: string, sidik: string, buat: () => string): [string, string] {
  return sidik === sidikLama ? [kunci, sidikLama] : [buat(), sidik];
}

/// Angka dari API (string atau number) menjadi teks isian tanpa pemisah ribuan.
export function teksQty(v: number | string): string {
  const n = Number(v);
  return Number.isFinite(n) ? String(n) : '';
}
