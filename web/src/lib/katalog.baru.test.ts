import { expect, it } from 'vitest';
import { badanProduk, cekProdukBaru, produkBaru } from './katalog';

const isi = { ...produkBaru('1'), nama: ' Teh ', harga: '5000', beli: '4000' };

it('badan produk menormalkan angka dan spasi', () => {
  expect(badanProduk({ ...isi, barcode: null })).toEqual({ nama: 'Teh', barcode: '', satuan: 'PCS', kategori_id: '1', harga: 5000, beli: 4000, aktif: true });
});

it('isian produk baru divalidasi', () => {
  expect(cekProdukBaru(isi)).toBeNull();
  expect(cekProdukBaru({ ...isi, nama: ' ' })).toMatch(/Nama/);
  expect(cekProdukBaru({ ...isi, kategori_id: '' })).toMatch(/kategori/);
  expect(cekProdukBaru({ ...isi, beli: '0' })).toMatch(/lebih dari 0/);
  expect(cekProdukBaru({ ...isi, harga: '4000' })).toMatch(/lebih besar/);
});
