import { describe, expect, it } from 'vitest';
import { badanToggleAktif, inisial, paramKatalog } from './katalog';

describe('katalog', () => {
  it('inisial memakai dua karakter pertama', () => {
    expect(inisial('Beras Premium')).toBe('BE');
    expect(inisial('  9 Sabun')).toBe('9S');
    expect(inisial('--')).toBe('?');
  });
  it('paramKatalog merapikan pencarian', () => {
    expect(paramKatalog('  gula ', 'Y', '', 2)).toEqual({ q: 'gula', aktif: 'Y', kategori: '', page: 2, size: 100 });
  });
  it('badanToggleAktif membalik aktif saja', () => {
    const b = badanToggleAktif({ nama: 'A', barcode: null, satuan: 'PCS', kategori_id: '1', harga: 10, beli: 5, aktif: true });
    expect(b).toEqual({ nama: 'A', barcode: '', satuan: 'PCS', kategori_id: '1', harga: 10, beli: 5, aktif: false });
  });
});

import { adaPerubahan, ringkasMargin } from './katalog';

describe('ringkasMargin', () => {
  it('hitung margin Rp dan persen dari harga jual', () => {
    expect(ringkasMargin(5000, 4000)).toEqual({ tipe: 'ok', margin: 1000, persen: 20 });
  });
  it('rugi saat jual di bawah HPP, impas saat sama', () => {
    expect(ringkasMargin(3000, 4000).tipe).toBe('rugi');
    expect(ringkasMargin(4000, 4000).tipe).toBe('impas');
  });
  it('HPP kosong atau nol tidak bisa dihitung', () => {
    expect(ringkasMargin(5000, '')).toEqual({ tipe: 'tanpa-hpp', margin: null, persen: null });
    expect(ringkasMargin('5000', 0).tipe).toBe('tanpa-hpp');
  });
});

describe('adaPerubahan', () => {
  const p = { nama: 'Teh', barcode: '', satuan: 'PCS', kategori_id: '1', harga: 5000, beli: 4000, aktif: true };
  const dasar = { produk: p, satuan: [{ satuan: 'DUS', isi: 12, harga: 50000, barcode: '' }] };
  it('sama = tidak berubah, abaikan spasi dan tipe angka', () => {
    expect(adaPerubahan(dasar, { produk: { ...p, nama: ' Teh ', harga: '5000' }, satuan: [{ satuan: 'DUS ', isi: '12', harga: 50000, barcode: null }] })).toBe(false);
  });
  it('harga, satuan, atau status berubah = berubah', () => {
    expect(adaPerubahan(dasar, { produk: { ...p, harga: 5500 }, satuan: dasar.satuan })).toBe(true);
    expect(adaPerubahan(dasar, { produk: p, satuan: [] })).toBe(true);
    expect(adaPerubahan(dasar, { produk: { ...p, aktif: false }, satuan: dasar.satuan })).toBe(true);
  });
});
