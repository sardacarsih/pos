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
