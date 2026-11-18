import { describe, expect, it } from 'vitest';
import { ApiError } from './api';
import { bedaStok, hasilSimpanBelumPasti, jumlahFisik } from './opname';

describe('jumlah fisik', () => {
  it.each([
    ['0', 0], [' 1,2345 ', 1.2345], ['.5', 0.5], ['1000000000', 1_000_000_000]
  ])('menerima %s', (teks, nilai) => expect(jumlahFisik(teks)).toBe(nilai));

  it.each(['', ',', '-1', '1e3', '0x20', '1.000,5', '1,23456', '1000000000.0001'])(
    'menolak %s', (teks) => expect(jumlahFisik(teks)).toBeNull()
  );

  it('membulatkan selisih sesuai presisi stok', () => {
    expect(bedaStok(0.3, 0.1)).toBe(0.2);
    expect(bedaStok(0.1234, 0.1234)).toBe(0);
  });
});

describe('hasil simpan', () => {
  it.each([0, 408, 500, 503])('status %i memerlukan retry dengan payload semula', (status) => {
    expect(hasilSimpanBelumPasti(new ApiError('Gagal', status))).toBe(true);
  });

  it('membedakan penolakan validasi dan respons yang gagal dibaca', () => {
    expect(hasilSimpanBelumPasti(new ApiError('Jumlah tidak valid', 400))).toBe(false);
    expect(hasilSimpanBelumPasti(new SyntaxError('Respons terputus'))).toBe(true);
  });
});
