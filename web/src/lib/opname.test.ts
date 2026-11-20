import { describe, expect, it } from 'vitest';
import { ApiError } from './api';
import { bedaStok, hasilSimpanBelumPasti, jumlahFisik, kolomHitungan, payloadHitungan, statusHitungan } from './opname';

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

describe('status hitungan multi-satuan', () => {
  const dus = { satuan: 'DUS', isi: 12, qty: '' };
  const pcs = { satuan: 'PCS', isi: 1, qty: '' };

  it('menggabungkan 2 dus x 12 + 3 pcs = 27', () => {
    const s = statusHitungan([{ ...dus, qty: '2' }, { ...pcs, qty: '3' }]);
    expect(s).toEqual({ nilai: 27, galat: null, terisi: 2 });
  });

  it('kolom kosong dilewati; semua kosong berarti belum dihitung', () => {
    expect(statusHitungan([{ ...dus, qty: '2' }, pcs])).toEqual({ nilai: 24, galat: null, terisi: 1 });
    expect(statusHitungan([dus, pcs])).toEqual({ nilai: null, galat: null, terisi: 0 });
  });

  it('nol eksplisit berarti fisik kosong', () => {
    expect(statusHitungan([{ ...pcs, qty: '0' }]).nilai).toBe(0);
  });

  it('menerima koma desimal dan faktor pecahan', () => {
    expect(statusHitungan([{ satuan: 'PACK', isi: 2.5, qty: '0,5' }, { ...pcs, qty: '1' }]).nilai).toBe(2.25);
  });

  it('menolak negatif, lebih dari 4 desimal, dan total lebih dari 1 miliar', () => {
    expect(statusHitungan([{ ...pcs, qty: '-1' }]).galat).toContain('tidak valid');
    expect(statusHitungan([{ ...pcs, qty: '1,23456' }]).galat).toContain('tidak valid');
    expect(statusHitungan([{ ...dus, qty: '100000000' }]).galat).toContain('1 miliar');
    expect(statusHitungan([{ ...pcs, qty: '1000000000' }, { ...dus, qty: '1' }]).galat).toContain('1 miliar');
  });

  it('menolak konversi yang perlu lebih dari 4 desimal, termasuk saat jumlahnya pas 4 desimal', () => {
    expect(statusHitungan([{ satuan: 'PACK', isi: 2.5, qty: '0.0001' }]).galat).toContain('4 desimal');
    const dua = statusHitungan([
      { satuan: 'PACK', isi: 2.5, qty: '0.0001' },
      { satuan: 'SLOP', isi: 2.5, qty: '0.0001' },
    ]);
    expect(dua.galat).toContain('4 desimal'); // 0.00025 + 0.00025 = 0.0005 lolos cek total saja
  });

  it('tepat di batas presisi dan miliar tanpa galat float', () => {
    expect(statusHitungan([{ ...pcs, qty: '999999999.9999' }]).nilai).toBe(999999999.9999);
    expect(statusHitungan([{ ...pcs, qty: '1000000000' }]).nilai).toBe(1000000000);
    expect(statusHitungan([{ satuan: 'PACK', isi: 2.5, qty: '123456789.1234' }]).nilai).toBe(308641972.8085);
    expect(statusHitungan([{ ...dus, qty: '1.50' }]).nilai).toBe(18);
  });
});

describe('kolom dan payload hitungan', () => {
  const barang = {
    satuan: 'PCS',
    satuan_tambahan: [
      { satuan: 'DUS', isi: 12, barcode: 'B-DUS' },
      { satuan: 'PACK', isi: 2.5, barcode: 'B-PACK' },
    ],
  };

  it('membuat kolom dasar + tambahan, mempertahankan isian saat dimuat ulang', () => {
    const kolom = kolomHitungan(barang);
    expect(kolom.map((k) => [k.satuan, k.isi])).toEqual([['PCS', 1], ['DUS', 12], ['PACK', 2.5]]);
    kolom[1].qty = '2';
    const lama = new Map(kolom.map((k) => [k.satuan.toUpperCase(), k.qty]));
    const baru = kolomHitungan({ satuan: 'PCS', satuan_tambahan: [{ satuan: 'DUS', isi: 24, barcode: 'B-DUS' }] }, lama);
    expect(baru.map((k) => [k.satuan, k.isi, k.qty])).toEqual([['PCS', 1, ''], ['DUS', 24, '2']]);
  });

  it('produk tanpa satuan tambahan tetap satu kolom', () => {
    expect(kolomHitungan({ satuan: 'PCS' })).toEqual([{ satuan: 'PCS', isi: 1, qty: '' }]);
  });

  it('payload hanya kolom terisi, angka dan faktor string desimal', () => {
    const kolom = kolomHitungan(barang);
    kolom[0].qty = '3';
    kolom[1].qty = '2';
    expect(payloadHitungan(kolom)).toEqual([
      { satuan: 'PCS', qty: '3', isi: '1' },
      { satuan: 'DUS', qty: '2', isi: '12' },
    ]);
    kolom[0].qty = '1,5';
    expect(payloadHitungan(kolom)[0]).toEqual({ satuan: 'PCS', qty: '1.5', isi: '1' });
  });
});
