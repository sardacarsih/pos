import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/models.dart';

void main() {
  test('RekapAnggota membaca ringkasan dan item (limit 0 = sisa null)', () {
    final r = RekapAnggota.fromJson({
      'periode': 202611,
      'dari': '2026-10-26',
      'sampai': '2026-11-25',
      'jumlah_anggota': 2,
      'jumlah_transaksi': 4,
      'total': 314000,
      'tunai': 0,
      'kredit': 314000,
      'items': [
        {
          'nik': '01.00062', 'nama': 'Aan', 'unit': 'KSK - SE',
          'jumlah': 3, 'total': 296000, 'tunai': 0, 'kredit': 296000,
          'limit': 300000, 'sisa_limit': 4000,
        },
        {
          'nik': '10.00001', 'nama': 'Abdul Hamid', 'unit': 'BBL',
          'jumlah': 1, 'total': 18000, 'tunai': 0, 'kredit': 18000,
          'limit': 0, 'sisa_limit': null,
        },
      ],
    });
    expect(r.periode, 202611);
    expect(r.items.length, 2);
    expect(r.items.first.sisaLimit, 4000);
    expect(r.items.last.sisaLimit, isNull);
    expect(r.jumlahTransaksi, 4);
  });

  test('PeriodeInfo membaca rentang bulanan', () {
    final p = PeriodeInfo.fromJson({'periode': 202610, 'dari': '2026-09-26', 'sampai': '2026-10-25'});
    expect(p.periode, 202610);
    expect(p.dari, '2026-09-26');
  });
}
