import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/katalog.dart';

void main() {
  test('pesanTidakBisaHapus memuat rincian bila ada', () {
    expect(pesanTidakBisaHapus('penjualan (3)'), contains('penjualan (3)'));
    expect(pesanTidakBisaHapus('  '), contains('riwayat transaksi'));
    expect(pesanTidakBisaHapus(''), contains('Nonaktifkan'));
  });

  test('cekProdukBaru memvalidasi isian', () {
    String? cek({String nama = 'Teh', String satuan = 'PCS', String kat = '1', int harga = 5000, int beli = 4000}) =>
        cekProdukBaru(nama: nama, satuan: satuan, kategoriId: kat, harga: harga, beli: beli);
    expect(cek(), isNull);
    expect(cek(nama: ' '), contains('Nama'));
    expect(cek(kat: ''), contains('kategori'));
    expect(cek(beli: 0), contains('lebih dari 0'));
    expect(cek(harga: 4000), contains('lebih besar'));
  });
}
