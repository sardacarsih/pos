// Helper murni untuk Beranda (mudah diuji): limit anggota, pembanding KPI, proporsi, dan grafik tren.

export type StatusLimit = 'tak-dibatasi' | 'aman' | 'hampir' | 'terlampaui';

/** Status limit belanja periode: limit <= 0 berarti tidak dibatasi; >= 80% hampir habis; > 100% terlampaui. */
export function statusLimit(belanja: number, limit: number): StatusLimit {
  if (!limit || limit <= 0) return 'tak-dibatasi';
  const p = belanja / limit;
  return p > 1 ? 'terlampaui' : p >= 0.8 ? 'hampir' : 'aman';
}

/** Persen terpakai untuk bar (0..100; bar tidak melebihi penuh). */
export function persenTerpakai(belanja: number, limit: number): number {
  if (!limit || limit <= 0) return 0;
  return Math.max(0, Math.min(100, Math.round((belanja / limit) * 100)));
}

export type ArahDelta = 'naik' | 'turun' | 'tetap' | 'tidak-ada';

/** Selisih persen terhadap pembanding. Pembanding 0/kosong -> 'tidak-ada' (tidak menampilkan angka palsu). */
export function delta(sekarang: number, pembanding: number): { arah: ArahDelta; persen: number } {
  if (!pembanding || pembanding <= 0) return { arah: 'tidak-ada', persen: 0 };
  const persen = Math.round(((sekarang - pembanding) / pembanding) * 100);
  return { arah: persen > 0 ? 'naik' : persen < 0 ? 'turun' : 'tetap', persen };
}

export function teksDelta(d: { arah: ArahDelta; persen: number }): string {
  if (d.arah === 'tidak-ada') return '—';
  if (d.arah === 'tetap') return 'sama dengan kemarin';
  return `${d.arah === 'naik' ? '▲' : '▼'} ${Math.abs(d.persen)}% vs kemarin`;
}

/** Proporsi dua bagian dalam persen (jumlah 100, atau 0/0 bila total nol). */
export function proporsi(a: number, b: number): [number, number] {
  const t = a + b;
  if (t <= 0) return [0, 0];
  const pa = Math.round((a / t) * 100);
  return [pa, 100 - pa];
}

/** Tinggi batang 0..100 relatif terhadap nilai terbesar; nilai > 0 minimal 4% agar tetap terlihat. */
export function tinggiBatang(nilai: number, semua: number[]): number {
  const maks = Math.max(0, ...semua);
  if (maks <= 0 || nilai <= 0) return 0;
  return Math.max(4, Math.round((nilai / maks) * 100));
}

/** Rupiah ringkas untuk label grafik: 950 -> "950", 12_500 -> "13 rb", 1_250_000 -> "1,3 jt". */
export function singkat(n: number): string {
  const a = Math.abs(n);
  if (a >= 1e9) return `${(n / 1e9).toFixed(1).replace('.', ',')} M`;
  if (a >= 1e6) return `${(n / 1e6).toFixed(1).replace('.', ',')} jt`;
  if (a >= 1e3) return `${Math.round(n / 1e3)} rb`;
  return String(n);
}

/** "2026-11-18" -> "Rab" (hari singkat, bahasa Indonesia). */
export function hariSingkat(tanggal: string): string {
  const d = new Date(`${tanggal}T00:00:00`);
  return Number.isNaN(d.getTime()) ? '' : new Intl.DateTimeFormat('id-ID', { weekday: 'short' }).format(d);
}

// ---- rincian penjualan: rentang waktu

export type Preset = 'hari' | 'tujuh' | 'periode' | 'bebas';
export interface Rentang { dari: string; sampai: string }
/** Batas rentang yang diterima server (hari). */
export const MAKS_HARI_RINCIAN = 92;
/** Grafik harian hanya ditampilkan sampai sejauh ini (lebih dari ini batang terlalu rapat). */
export const MAKS_HARI_GRAFIK = 31;

const MS_HARI = 86_400_000;
const keMs = (iso: string) => Date.parse(`${iso}T00:00:00Z`);
const keIso = (ms: number) => new Date(ms).toISOString().slice(0, 10);
const tanggalValid = (s: string) => /^\d{4}-\d{2}-\d{2}$/.test(s) && !Number.isNaN(keMs(s)) && keIso(keMs(s)) === s;

export function tambahHari(iso: string, n: number): string { return keIso(keMs(iso) + n * MS_HARI); }
export function jumlahHari(r: Rentang): number { return Math.round((keMs(r.sampai) - keMs(r.dari)) / MS_HARI) + 1; }

/** Rentang dari pilihan cepat. "Hari ini" mengikuti tanggal server. Periode berjalan dipotong sampai hari ini; null bila tidak diketahui. */
export function rentangPreset(preset: Exclude<Preset, 'bebas'>, hariIni: string, periodeDari?: string | null, periodeSampai?: string | null): Rentang | null {
  if (!tanggalValid(hariIni)) return null;
  if (preset === 'hari') return { dari: hariIni, sampai: hariIni };
  if (preset === 'tujuh') return { dari: tambahHari(hariIni, -6), sampai: hariIni };
  if (!periodeDari || !periodeSampai || !tanggalValid(periodeDari) || !tanggalValid(periodeSampai) || periodeDari > hariIni) return null;
  return { dari: periodeDari, sampai: periodeSampai < hariIni ? periodeSampai : hariIni };
}

/** Pesan galat untuk rentang yang tidak akan diterima server; null bila valid. */
export function validasiRentang(r: Rentang, hariIni: string): string | null {
  if (!tanggalValid(r.dari) || !tanggalValid(r.sampai)) return 'Isi tanggal awal dan akhir.';
  if (r.dari > r.sampai) return 'Tanggal awal tidak boleh setelah tanggal akhir.';
  if (r.sampai > hariIni) return 'Tanggal akhir tidak boleh melewati hari ini.';
  if (jumlahHari(r) > MAKS_HARI_RINCIAN) return `Rentang maksimal ${MAKS_HARI_RINCIAN} hari.`;
  return null;
}

/** Persen bagian terhadap total (0 bila total 0). Tunai + kredit + lain dapat berjumlah 99-101 karena pembulatan. */
export function persenJenis(bagian: number, total: number): number {
  return total > 0 ? Math.round((bagian / total) * 100) : 0;
}
