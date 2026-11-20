import { ApiError } from './api';

export const MAKS_BARANG = 200;

export function jumlahFisik(teks: string): number | null {
  const nilai = teks.trim().replace(',', '.');
  if (!/^(?:\d+(?:\.\d{0,4})?|\.\d{1,4})$/.test(nilai)) return null;
  const jumlah = Number(nilai);
  return jumlah <= 1_000_000_000 ? jumlah : null;
}

export function bedaStok(fisik: number, sistem: number): number {
  return Number((fisik - sistem).toFixed(4));
}

export function hasilSimpanBelumPasti(error: unknown): boolean {
  return !(error instanceof ApiError)
    || error.status < 400 || error.status >= 500 || error.status === 408;
}

export interface SatuanTambahan { satuan: string; isi: number; barcode: string }
export interface KolomHitung { satuan: string; isi: number; qty: string }
export interface StatusHitung { nilai: number | null; galat: string | null; terisi: number }

const BATAS_TOTAL = 10n ** 13n; // 1 miliar dalam skala 4 desimal

function pecahan(teks: string): { v: bigint; s: number } | null {
  const m = /^(\d*)(?:\.(\d*))?$/.exec(teks);
  if (!m || (m[1] === '' && (m[2] ?? '') === '')) return null;
  const frac = m[2] ?? '';
  return { v: BigInt((m[1] || '0') + frac), s: frac.length };
}

/// Kolom input: satuan dasar (faktor 1) + satuan tambahan Katalog.
/// `qtyLama` (kunci nama satuan huruf besar) mempertahankan isian saat satuan dimuat ulang.
export function kolomHitungan(
  b: { satuan: string; satuan_tambahan?: SatuanTambahan[] },
  qtyLama?: Map<string, string>
): KolomHitung[] {
  const lama = (nama: string) => qtyLama?.get(nama.trim().toUpperCase()) ?? '';
  return [
    { satuan: b.satuan, isi: 1, qty: lama(b.satuan) },
    ...(b.satuan_tambahan ?? []).map((s) => ({ satuan: s.satuan, isi: s.isi, qty: lama(s.satuan) })),
  ];
}

/// Total fisik dalam satuan dasar dari kolom yang terisi; kolom kosong dilewati.
/// Semua kolom kosong = belum dihitung (nilai null tanpa galat); nol harus diisi eksplisit.
/// Aritmetika BigInt agar batas 4 desimal dan 1 miliar diperiksa eksak, bukan lewat float.
export function statusHitungan(kolom: KolomHitung[]): StatusHitung {
  const isi = kolom.filter((k) => k.qty.trim() !== '');
  if (!isi.length) return { nilai: null, galat: null, terisi: 0 };
  let total = 0n;
  for (const k of isi) {
    const n = jumlahFisik(k.qty);
    if (n === null) {
      return { nilai: null, galat: `Jumlah ${k.satuan} tidak valid: 0 sampai 1 miliar, maksimal 4 desimal, tanpa negatif.`, terisi: isi.length };
    }
    const q = pecahan(k.qty.trim().replace(',', '.'))!;
    const f = pecahan(String(k.isi));
    if (!f || f.v <= 0n) {
      return { nilai: null, galat: `Faktor konversi ${k.satuan} tidak valid.`, terisi: isi.length };
    }
    let v = q.v * f.v;
    let s = q.s + f.s;
    while (s > 4 && v % 10n === 0n) { v /= 10n; s--; }
    if (s > 4) {
      return { nilai: null, galat: `Hasil konversi ${k.satuan} (×${k.isi}) perlu lebih dari 4 desimal.`, terisi: isi.length };
    }
    total += v * 10n ** BigInt(4 - s);
  }
  if (total > BATAS_TOTAL) {
    return { nilai: null, galat: 'Total fisik melebihi 1 miliar.', terisi: isi.length };
  }
  return { nilai: Number(total) / 1e4, galat: null, terisi: isi.length };
}

/// Payload `hitungan` untuk API: hanya kolom terisi, angka dan faktor sebagai string desimal.
export function payloadHitungan(kolom: KolomHitung[]): { satuan: string; qty: string; isi: string }[] {
  return kolom
    .filter((k) => k.qty.trim() !== '')
    .map((k) => ({ satuan: k.satuan, qty: String(jumlahFisik(k.qty)), isi: String(k.isi) }));
}
