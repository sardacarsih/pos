import { describe, expect, it } from 'vitest';
import { TAB_SEDIA, labelJenisMutasi, rentangKartu, validasiSesuaikan } from './persediaan';

const HARI = '2026-11-19';

describe('tab persediaan', () => {
  it('enam tab tetap dengan id dan label', () => {
    expect(TAB_SEDIA.map(t => t.id)).toEqual(['kartu', 'habis', 'opname', 'sesuai', 'valuasi', 'valuasi-item', 'rusak']);
    expect(TAB_SEDIA.map(t => t.label)).toEqual(['Kartu stok', 'Stok habis', 'Stock opname', 'Penyesuaian', 'Valuasi & alert', 'Valuasi Item', 'Barang rusak']);
  });
});

describe('rentang kartu stok', () => {
  it('default 1 Jan tahun berjalan sampai hari ini', () => {
    expect(rentangKartu(null, null, HARI)).toEqual({ dari: '2026-01-01', sampai: HARI });
    expect(rentangKartu('', '', HARI)).toEqual({ dari: '2026-01-01', sampai: HARI });
  });
  it('di-clamp ke tahun berjalan', () => {
    expect(rentangKartu('2025-12-01', '2027-02-01', HARI)).toEqual({ dari: '2026-01-01', sampai: HARI });
    expect(rentangKartu('2026-03-01', '2026-03-31', HARI)).toEqual({ dari: '2026-03-01', sampai: '2026-03-31' });
  });
  it('rentang terbalik ditolak', () => {
    expect(() => rentangKartu('2026-05-01', '2026-04-01', HARI)).toThrow('Rentang tanggal terbalik');
  });
  it('tanggal tidak valid ditolak', () => {
    expect(() => rentangKartu('2026-02-30', null, HARI)).toThrow('Tanggal tidak valid (YYYY-MM-DD)');
  });
});

describe('validasi penyesuaian (cermin server)', () => {
  const benar = { produk_id: 1, qty: '5', tanggal: '2026-11-01', alasan: 'Koreksi hitung fisik' };
  it('isian valid lolos', () => {
    expect(validasiSesuaikan(benar, HARI)).toBeNull();
    expect(validasiSesuaikan({ ...benar, qty: '-2.5' }, HARI)).toBeNull();
    expect(validasiSesuaikan({ ...benar, qty: '0.0001' }, HARI)).toBeNull();
    expect(validasiSesuaikan({ ...benar, qty: '99999999.9999' }, HARI)).toBeNull();
  });
  it('produk wajib dipilih', () => {
    expect(validasiSesuaikan({ ...benar, produk_id: 0 }, HARI)).toBe('Pilih produk terlebih dahulu');
  });
  it('qty nol, skala > 4, dan di atas maksimum ditolak', () => {
    expect(validasiSesuaikan({ ...benar, qty: '0' }, HARI)).toBe('Qty penyesuaian tidak boleh nol');
    expect(validasiSesuaikan({ ...benar, qty: '1.00001' }, HARI)).toBe('Qty penyesuaian tidak valid');
    expect(validasiSesuaikan({ ...benar, qty: '100000000' }, HARI)).toBe('Qty penyesuaian tidak valid');
    expect(validasiSesuaikan({ ...benar, qty: 'abc' }, HARI)).toBe('Qty penyesuaian tidak valid');
    expect(validasiSesuaikan({ ...benar, qty: '' }, HARI)).toBe('Qty penyesuaian tidak valid');
  });
  it('alasan trim 3-250 karakter', () => {
    expect(validasiSesuaikan({ ...benar, alasan: '  ab ' }, HARI)).toBe('Alasan wajib diisi (3-250 karakter)');
    expect(validasiSesuaikan({ ...benar, alasan: '   ' }, HARI)).toBe('Alasan wajib diisi (3-250 karakter)');
    expect(validasiSesuaikan({ ...benar, alasan: 'a'.repeat(251) }, HARI)).toBe('Alasan wajib diisi (3-250 karakter)');
    expect(validasiSesuaikan({ ...benar, alasan: '  abc  ' }, HARI)).toBeNull();
  });
  it('tanggal harus dalam tahun berjalan sampai hari ini', () => {
    expect(validasiSesuaikan({ ...benar, tanggal: '2025-12-31' }, HARI)).toBe('Tanggal penyesuaian harus dalam tahun berjalan sampai hari ini');
    expect(validasiSesuaikan({ ...benar, tanggal: '2026-11-20' }, HARI)).toBe('Tanggal penyesuaian harus dalam tahun berjalan sampai hari ini');
    expect(validasiSesuaikan({ ...benar, tanggal: '2026-13-01' }, HARI)).toBe('Tanggal tidak valid (YYYY-MM-DD)');
    expect(validasiSesuaikan({ ...benar, tanggal: '2026-01-01' }, HARI)).toBeNull();
    expect(validasiSesuaikan({ ...benar, tanggal: HARI }, HARI)).toBeNull();
  });
});

describe('label jenis mutasi', () => {
  it('jenis dikenal dilabeli manusiawi, jenis asing apa adanya', () => {
    expect(labelJenisMutasi('pembelian')).toBe('Pembelian');
    expect(labelJenisMutasi('penjualan')).toBe('Penjualan');
    expect(labelJenisMutasi('opname')).toBe('Stock opname');
    expect(labelJenisMutasi('rusak')).toBe('Barang rusak');
    expect(labelJenisMutasi('penyesuaian')).toBe('Penyesuaian stok');
    expect(labelJenisMutasi('penyesuaian_balik')).toBe('Pembalikan penyesuaian');
    expect(labelJenisMutasi('lain')).toBe('lain');
  });
});
