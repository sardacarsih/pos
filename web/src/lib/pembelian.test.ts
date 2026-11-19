import { describe, expect, it } from 'vitest';
import { cariBarang, payloadBeli, totalBeli, type BarangBeli, type DrafBeli } from './pembelian';

const barang: BarangBeli[] = [
  { id: 1, kode: 'TEH', nama: 'Teh', beli: '3000', satuan: [
    { satuan: 'PCS', isi: '1', barcode: '007' }, { satuan: 'BOX', isi: '12', barcode: '00042' }
  ] },
  { id: 2, kode: 'KOPI', nama: 'Kopi', beli: '5000', satuan: [
    { satuan: 'PCS', isi: '1', barcode: '00042' }
  ] }
];

function draf(): DrafBeli {
  return {
    id: 'draft', supplier_id: 'SUP', no_faktur: ' A-001 ', tanggal_faktur: '2026-11-18',
    tanggal_terima: '2026-11-18', jenis: 'TEMPO', jatuh_tempo: '2026-12-01', uang_muka: 50,
    metode: 'TRANSFER', referensi: ' DP-01 ', baris: [
      { id: '1', product_id: 1, kode: 'TEH', nama: 'Teh', qty: '1,125',
        satuan: 'BOX', isi: '12', harga: 100, potongan: 13 }
    ]
  };
}

describe('pembelian web', () => {
  it('mempertahankan nol awal dan menawarkan semua hasil persis yang ambigu', () => {
    expect(cariBarang(barang, '00042').persis.map(p => p.satuan.satuan)).toEqual(['BOX', 'PCS']);
    expect(cariBarang(barang, '42').persis).toEqual([]);
    expect(cariBarang(barang, ' tEh ').hasil[0].produk.id).toBe(1);
  });

  it('membulatkan bruto per baris dan mengirim jumlah serta faktor sebagai string', () => {
    expect(totalBeli(draf().baris)).toEqual({ bruto: 113, potongan: 13, total: 100 });
    expect(payloadBeli(draf())).toMatchObject({
      no_faktur: 'A-001', uang_muka: 50, metode: 'TRANSFER', referensi: 'DP-01',
      baris: [{ qty: '1.125', isi: '12', harga: 100, potongan: 13 }]
    });
  });

  it('menolak potongan di atas bruto dan uang muka sebesar total', () => {
    const data = draf();
    data.uang_muka = 100;
    expect(() => payloadBeli(data)).toThrow(/uang muka/);
    data.baris[0].potongan = 114;
    expect(totalBeli(data.baris)).toBeNull();
  });
});
