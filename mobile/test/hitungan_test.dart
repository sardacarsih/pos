import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/hitungan.dart';
import 'package:kopkar_pos/core/models.dart';

StokItem _item({
  String satuan = 'Liter',
  List<Map<String, dynamic>> tambahan = const [],
}) => StokItem.fromJson({
  'id': 1,
  'kode': 'P001',
  'barcode': '899001',
  'nama': 'Minyak goreng',
  'stok': 5,
  'satuan': satuan,
  'satuan_tambahan': tambahan,
});

KolomHitung _k(String satuan, double isi, String qty) =>
    KolomHitung(satuan: satuan, isi: isi, qty: qty);

void main() {
  group('kolomHitungan', () {
    test('satuan dasar dulu, lalu satuan tambahan Katalog', () {
      final kolom = kolomHitungan(
        _item(
          tambahan: const [
            {'satuan': 'DUS', 'isi': 12, 'barcode': 'B12'},
            {'satuan': 'PACK', 'isi': 2.5, 'barcode': ''},
          ],
        ),
      );
      expect(kolom.map((k) => k.satuan), ['Liter', 'DUS', 'PACK']);
      expect(kolom.map((k) => k.isi), [1, 12, 2.5]);
      expect(kolom.every((k) => k.qty.isEmpty), isTrue);
    });

    test('produk tanpa satuan tambahan tetap satu kolom', () {
      final kolom = kolomHitungan(_item());
      expect(kolom, hasLength(1));
      expect(kolom.single.satuan, 'Liter');
    });

    test('isian lama dipertahankan per nama satuan (tidak peka huruf)', () {
      final kolom = kolomHitungan(
        _item(
          tambahan: const [
            {'satuan': 'Dus', 'isi': 24, 'barcode': 'B24'},
          ],
        ),
        qtyLama: {'DUS': '4', 'LITER': '3,5'},
      );
      expect(kolom[0].qty, '3,5');
      expect(kolom[1].qty, '4');
    });
  });

  group('statusHitungan', () {
    test('semua kolom kosong = belum dihitung, tanpa galat', () {
      final st = statusHitungan([_k('Liter', 1, ''), _k('DUS', 12, '  ')]);
      expect((st.nilai, st.galat, st.terisi), (null, null, 0));
    });

    test('nol eksplisit terhitung; kolom kosong lain dilewati', () {
      expect(statusHitungan([_k('Liter', 1, '0')]).nilai, 0);
      final st = statusHitungan([_k('Liter', 1, '3'), _k('DUS', 12, '')]);
      expect((st.nilai, st.terisi), (3, 1));
    });

    test('2 DUS x 12 + 3 PCS = 27 PCS', () {
      final st = statusHitungan([_k('PCS', 1, '3'), _k('DUS', 12, '2')]);
      expect(st.galat, isNull);
      expect(st.nilai, 27);
    });

    test('koma diterima dan faktor desimal dihitung eksak', () {
      final st = statusHitungan([_k('PCS', 1, '1'), _k('PACK', 2.5, '0,5')]);
      expect(st.galat, isNull);
      expect(st.nilai, 2.25);
    });

    test('jumlah tidak valid ditolak dengan pesan per satuan', () {
      for (final teks in ['-1', '1.12345', 'abc', '1e3']) {
        final st = statusHitungan([_k('PCS', 1, teks)]);
        expect(st.galat, contains('Jumlah PCS tidak valid'), reason: teks);
      }
    });

    test('hasil konversi lebih dari 4 desimal ditolak per kolom', () {
      // 200000000 x 2,5 = 500000000,0 — nol trailing dibuang sampai skala 0, tetap valid.
      final aman = statusHitungan([_k('PCS', 2.5, '200000000')]);
      expect(aman.galat, isNull);
      expect(aman.nilai, 500000000);
      // Faktor berdesimal lebih dari 4 dari Katalog tertangkap di kolomnya sendiri.
      final presisi = statusHitungan([_k('MIKRO', 0.00002, '3')]);
      expect(presisi.galat, contains('perlu lebih dari 4 desimal'));
      expect(presisi.galat, contains('MIKRO'));
      // 0.2555 x 1.2 = 0.30660 — nol trailing dibuang sampai 4 desimal, BUKAN ditolak.
      final sisa = statusHitungan([_k('LITER', 1.2, '0.2555')]);
      expect(sisa.galat, isNull);
      expect(sisa.nilai, 0.3066);
    });

    test('total gabungan di atas 1 miliar ditolak, pas 1 miliar boleh', () {
      final pas = statusHitungan([
        _k('PCS', 1, '999999999.9999'),
        _k('DUS', 1, '0.0001'),
      ]);
      expect(pas.galat, isNull);
      expect(pas.nilai, 1000000000);
      final lewat = statusHitungan([
        _k('PCS', 1, '999999999.9999'),
        _k('DUS', 1, '0.0002'),
      ]);
      expect(lewat.nilai, isNull);
      expect(lewat.galat, 'Total fisik melebihi 1 miliar.');
    });
  });

  group('payloadHitungan dan rincianHitungan', () {
    test(
      'hanya kolom terisi; qty ternormalisasi titik, faktor tanpa desimal nol',
      () {
        final payload = payloadHitungan([
          _k('PCS', 1, '3,5'),
          _k('DUS', 12, ''),
          _k('PACK', 2.5, '1'),
        ]);
        expect(payload, [
          (satuan: 'PCS', qty: '3.5', isi: '1'),
          (satuan: 'PACK', qty: '1', isi: '2.5'),
        ]);
      },
    );

    test(
      'rincian menyebut qty satuan dan faktor hanya selain satuan dasar',
      () {
        final r = rincianHitungan([_k('PCS', 1, '3'), _k('DUS', 12, '2')]);
        expect(r, '3 PCS + 2 DUS × 12');
        expect(
          rincianHitungan([_k('DUS', 12, '2'), _k('PCS', 1, '3')]),
          '2 DUS × 12 + 3 PCS',
        );
      },
    );

    test('isi bulat tampil tanpa notasi desimal/eksponensial', () {
      expect(teksDesimal(1000000), '1000000');
      expect(teksDesimal(2.5), '2.5');
    });
  });

  group('StokItem.satuanTambahan', () {
    test('kosong bila API tidak mengirim (multi_satuan tidak diminta)', () {
      expect(_item().satuanTambahan, isEmpty);
    });

    test('terisi dari payload satuan_tambahan', () {
      final b = _item(
        tambahan: const [
          {'satuan': 'DUS', 'isi': 12, 'barcode': 'B12'},
        ],
      );
      expect(b.satuanTambahan.single.satuan, 'DUS');
      expect(b.satuanTambahan.single.isi, 12);
      expect(b.satuanTambahan.single.barcode, 'B12');
    });

    test('stok dan isi bertipe string dari API tetap terparse (regresi 1.0.10)', () {
      final b = StokItem.fromJson({
        'id': 1,
        'kode': 'P001',
        'barcode': '',
        'nama': 'Gula',
        'satuan': 'PCS',
        'stok': '-13.5',
        'satuan_tambahan': [
          {'satuan': 'DUS', 'isi': '12', 'barcode': ''},
        ],
      });
      expect(b.stok, -13.5);
      expect(b.satuanTambahan.single.isi, 12);
      final kolom = kolomHitungan(b);
      kolom[0].qty = '3';
      kolom[1].qty = '2';
      expect(statusHitungan(kolom).nilai, 27);
    });
  });
}
