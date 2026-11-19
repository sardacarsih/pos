import { expect, it } from 'vitest';
import { infoSatuan, satuanKirim } from './satuan';
import type { Line, Product } from './types';

const p: Product = { id: 1, nama: 'Teh', kode: 'T1', barcode: '', satuan: 'PCS', harga: 5000, stok: 10, kategori_id: '1', kategori: 'X', foto: null, satuan_lain: [{ satuan: 'BOX', isi: 12, harga: 55000, barcode: '' }] };

it('satuan dasar bila tidak dipilih', () => {
  const l: Line = { product: p, qty: 1 };
  expect(infoSatuan(l)).toEqual({ satuan: 'PCS', harga: 5000 });
  expect(satuanKirim(l)).toBeUndefined();
});

it('satuan tambahan memakai harga satuan itu dan dikirim ke server', () => {
  const l: Line = { product: p, qty: 2, satuan: 'BOX' };
  expect(infoSatuan(l)).toEqual({ satuan: 'BOX', harga: 55000 });
  expect(satuanKirim(l)).toBe('BOX');
});

it('satuan yang tidak dikenal jatuh ke satuan dasar', () => {
  const l: Line = { product: p, qty: 1, satuan: 'DUS' };
  expect(infoSatuan(l).harga).toBe(5000);
  expect(satuanKirim(l)).toBeUndefined();
});
