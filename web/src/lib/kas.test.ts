import { describe, expect, it } from 'vitest';
import { bisaDibatal, cekMutasi, permintaanMutasi, type IsianMutasi, type Mutasi } from './kas';

const isi = (x: Partial<IsianMutasi> = {}): IsianMutasi =>
  ({ jenis: 'M', akun: 1, ke: '', tanggal: '2026-11-18', nominal: 1000, keterangan: 'Setoran', ...x });

describe('kas', () => {
  it('validasi isian mutasi', () => {
    expect(cekMutasi(isi(), '2026-11-18')).toBe('');
    expect(cekMutasi(isi({ akun: '' }), '2026-11-18')).toMatch(/akun/);
    expect(cekMutasi(isi({ jenis: 'TRANSFER', ke: 1 }), '2026-11-18')).toMatch(/tujuan/);
    expect(cekMutasi(isi({ tanggal: '2026-11-19' }), '2026-11-18')).toMatch(/Tanggal/);
    expect(cekMutasi(isi({ nominal: 1.5 }), '2026-11-18')).toMatch(/Nominal/);
    expect(cekMutasi(isi({ keterangan: '  ' }), '2026-11-18')).toMatch(/Keterangan/);
  });
  it('permintaan manual vs transfer', () => {
    expect(permintaanMutasi(isi({ jenis: 'K' }))).toEqual({ path: '/kas/mutasi', body: { tanggal: '2026-11-18', nominal: 1000, keterangan: 'Setoran', akun_id: 1, arah: 'K' } });
    expect(permintaanMutasi(isi({ jenis: 'TRANSFER', ke: 2 })).body).toMatchObject({ dari_akun: 1, ke_akun: 2 });
  });
  it('hanya manual/transfer aktif yang bisa dibatal', () => {
    const m = { sumber: 'MANUAL', dibalik: false, pembalik: false } as Mutasi;
    expect(bisaDibatal(m)).toBe(true);
    expect(bisaDibatal({ ...m, sumber: 'PENJUALAN' })).toBe(false);
    expect(bisaDibatal({ ...m, dibalik: true })).toBe(false);
    expect(bisaDibatal({ ...m, pembalik: true })).toBe(false);
  });
});
