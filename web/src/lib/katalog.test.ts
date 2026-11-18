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
