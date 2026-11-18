import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/katalog.dart';

void main() {
  test('pesanTidakBisaHapus memuat rincian bila ada', () {
    expect(pesanTidakBisaHapus('penjualan (3)'), contains('penjualan (3)'));
    expect(pesanTidakBisaHapus('  '), contains('riwayat transaksi'));
    expect(pesanTidakBisaHapus(''), contains('Nonaktifkan'));
  });
}
