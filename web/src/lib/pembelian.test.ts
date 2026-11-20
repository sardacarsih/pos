import { describe, expect, it } from 'vitest';
import { tabBeli } from './pembelian';

describe('tabBeli', () => {
  it('ADMIN mendapat 5 tab termasuk hutang; PEMBELIAN 4 tanpa hutang', () => {
    expect(tabBeli('ADMIN').map(t => t.id)).toEqual(['input', 'antrean', 'riwayat', 'supplier', 'hutang']);
    expect(tabBeli('PEMBELIAN').map(t => t.id)).toEqual(['input', 'antrean', 'riwayat', 'supplier']);
    expect(tabBeli('KASIR').map(t => t.id)).toEqual(['input', 'antrean', 'riwayat', 'supplier']);
  });
});
import { angkaQty, bacaJumlahBeli, bacaRupiahBeli, bacaRupiahPecahan, cariBarang, formatJumlahBeli, hargaAwalBeli, hargaDasar, payloadBeli, rpBeli, selisihHarga, stokMasuk, tambahQty, teksQtyDasar, totalBeli,
  type BarangBeli, type DrafBeli } from './pembelian';

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
  it('mempertahankan presisi saldo faktur Backoffice dan menerima nol', () => {
    expect(bacaRupiahPecahan('1.975.000,25')).toBe('1975000.25');
    expect(bacaRupiahPecahan('0,00')).toBe('0');
    expect(bacaRupiahPecahan('9.000.000.000.000.000,01')).toBe('9000000000000000.01');
    expect(bacaRupiahPecahan('10,000')).toBeNull();
    expect(bacaRupiahPecahan('-1')).toBeNull();
  });
  it('membedakan titik ribuan di input dari titik desimal pada draf lama', () => {
    expect(bacaJumlahBeli('1.234')).toBe('1234');
    expect(bacaJumlahBeli('1.234,5')).toBe('1234.5');
    expect(angkaQty('1.234')).toBe('1.234');
    expect(bacaJumlahBeli('1,234')).toBe('1.234');
    expect(bacaJumlahBeli('1,2345')).toBeNull();
    expect(bacaJumlahBeli('12.50')).toBeNull();
    expect(formatJumlahBeli('1234.5625')).toBe('1.234,5625');
  });

  it('menerima rupiah bulat bertitik dan mempertahankan presisi tampilan historis', () => {
    expect(bacaRupiahBeli('12500')).toBe(12500);
    expect(bacaRupiahBeli('12.500')).toBe(12500);
    expect(bacaRupiahBeli('Rp\u00a012.500')).toBe(12500);
    expect(bacaRupiahBeli('12.50')).toBeNull();
    expect(bacaRupiahBeli('12.500,50')).toBeNull();
    expect(bacaRupiahBeli('9.000.000.000.000.001')).toBeNull();
    expect(rpBeli(12500)).toBe('Rp 12.500');
    expect(rpBeli('-12345678901234567890.2500')).toBe('-Rp 12.345.678.901.234.567.890,25');
  });

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

  it('harga awal memakai harga beli satuan bila ada, selain itu beli x isi', () => {
    expect(hargaAwalBeli(barang[0], { satuan: 'BOX', isi: '12', barcode: '', beli: '33000' })).toBe(33000);
    expect(hargaAwalBeli(barang[0], barang[0].satuan[1])).toBe(36000);
  });

  it('menghitung konversi stok tanpa error desimal dan menggabungkan per satuan dasar', () => {
    const baris = (qty: string, isi: string, id = 1) => ({ id: 'x', product_id: id, kode: '', nama: '', satuan: 'BOX', isi, qty, harga: 100, potongan: 0 });
    expect(teksQtyDasar(baris('2', '10'))).toBe('20');
    expect(teksQtyDasar(baris('0.3', '0.1'))).toBe('0.03');
    expect(teksQtyDasar(baris('1,5', '12'))).toBe('18');
    expect(teksQtyDasar(baris('', '12'))).toBeNull();
    const peta = new Map(barang.map(p => [p.id, p]));
    expect(stokMasuk([baris('2', '12'), baris('5', '1'), baris('1', '1', 2)], peta))
      .toEqual([{ satuan: 'PCS', qty: '30' }]);
  });

  it('scan ulang menambah jumlah dengan batas 999.999 dan hitung harga dasar', () => {
    expect(tambahQty('1')).toBe('2');
    expect(tambahQty('1.125')).toBe('2.125');
    expect(tambahQty('999999')).toBeNull();
    expect(hargaDasar(24000, '12')).toBe(2000);
    expect(hargaDasar(100, '3')).toBe(33.33);
    expect(hargaDasar(undefined, '3')).toBeNull();
  });

  it('memperingatkan harga yang menyimpang lebih dari 30% dari harga terakhir', () => {
    const box = barang[0].satuan[1];
    expect(selisihHarga(barang[0], box, 36000)).toBeNull();
    expect(selisihHarga(barang[0], box, 40000)).toBeNull();
    expect(selisihHarga(barang[0], box, 3600)).toBeCloseTo(-0.9);
    expect(selisihHarga(barang[0], { ...box, beli: '30000' }, 60000)).toBeCloseTo(1);
    expect(selisihHarga(barang[0], box, undefined)).toBeNull();
  });

  it('barcode menawarkan satuan dasar dan tambahan milik produk saja', () => {
    expect(cariBarang(barang, '007').persis.map(p => p.satuan.satuan)).toEqual(['PCS']);
    expect(cariBarang(barang, '00002').persis).toEqual([]);
    expect(barang[0].satuan.map(s => s.satuan)).toEqual(['PCS', 'BOX']);
  });

  it('menolak potongan di atas bruto dan uang muka sebesar total', () => {
    const data = draf();
    data.uang_muka = 100;
    expect(() => payloadBeli(data)).toThrow(/uang muka/);
    data.baris[0].potongan = 114;
    expect(totalBeli(data.baris)).toBeNull();
  });
});
