import type { Line } from './types';

/** Harga per satuan jual baris: satuan tambahan bila dipilih dan ada, selain itu satuan dasar. */
export function infoSatuan(l: Line): { satuan: string; harga: number } {
  const s = l.satuan ? l.product.satuan_lain?.find(x => x.satuan === l.satuan) : undefined;
  return s ? { satuan: s.satuan, harga: s.harga } : { satuan: l.product.satuan, harga: l.product.harga };
}

/** Satuan yang dikirim ke server: kosong (undefined) = satuan dasar. */
export function satuanKirim(l: Line): string | undefined {
  return l.satuan && l.product.satuan_lain?.some(x => x.satuan === l.satuan) ? l.satuan : undefined;
}
