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
