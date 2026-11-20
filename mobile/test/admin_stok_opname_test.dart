import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/hitungan.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_stok_page.dart';

class _ApiStok extends ApiClient {
  _ApiStok() : super(baseUrl: 'http://localhost', store: SecureTokenStore());

  List<Map<String, dynamic>> tambahan = const [
    {'satuan': 'DUS', 'isi': 12, 'barcode': '900DUS'},
  ];
  String? galatSekali;
  final kiriman = <List<HitunganKirim>>[];
  final kunci = <String>[];
  final urutDiminta = <bool>[];

  @override
  Future<({List<StokItem> items, bool lagi, int? jumlah, bool kontrolStok})>
  stokHabis({String? q, int page = 1, bool urutQty = false}) async {
    urutDiminta.add(urutQty);
    return (
      items: [
        StokItem.fromJson({
          'id': 7,
          'kode': 'P007',
          'barcode': 'B7',
          'nama': 'Tepung',
          'stok': -2,
          'satuan': 'Kg',
          'satuan_tambahan': tambahan,
        }),
      ],
      lagi: false,
      jumlah: 1,
      kontrolStok: true,
    );
  }

  @override
  Future<String> opnameStok(
    int produkId,
    List<HitunganKirim> hitungan, {
    required String idempotencyKey,
  }) async {
    kiriman.add(hitungan);
    kunci.add(idempotencyKey);
    if (kiriman.length == 1 && galatSekali != null) {
      throw ApiException(galatSekali!, status: 400);
    }
    return 'SO-26-000009';
  }
}

void main() {
  testWidgets('stok habis: default urut Qty terendah, chip Nama A-Z memuat ulang', (
    t,
  ) async {
    final api = _ApiStok();
    await t.pumpWidget(
      ProviderScope(
        overrides: [apiProvider.overrideWithValue(api)],
        child: const MaterialApp(home: Scaffold(body: AdminStokPage())),
      ),
    );
    await t.pumpAndSettle();
    expect(api.urutDiminta, [true]);
    await t.tap(find.text('Nama A-Z'));
    await t.pumpAndSettle();
    expect(api.urutDiminta, [true, false]);
    await t.tap(find.text('Qty terendah'));
    await t.pumpAndSettle();
    expect(api.urutDiminta, [true, false, true]);
  });

  testWidgets(
    'opname cepat multi-satuan: total gabungan, muat ulang satuan, key stabil',
    (t) async {
      await t.binding.setSurfaceSize(const Size(320, 700));
      addTearDown(() => t.binding.setSurfaceSize(null));
      final api = _ApiStok()
        ..galatSekali =
            'Faktor konversi satuan "DUS" berubah (12 menjadi 24); '
            'muat ulang satuan produk ini sebelum meninjau';
      await t.pumpWidget(
        ProviderScope(
          overrides: [apiProvider.overrideWithValue(api)],
          child: const MaterialApp(home: Scaffold(body: AdminStokPage())),
        ),
      );
      await t.pumpAndSettle();
      expect(find.textContaining('1 produk aktif dengan stok'), findsOneWidget);

      await t.tap(find.text('Tepung'));
      await t.pumpAndSettle();
      final sheet = find.byType(BottomSheet);
      final fields =
          find.descendant(of: sheet, matching: find.byType(TextField));
      expect(fields, findsNWidgets(2));
      expect(find.text('1 DUS = 12 Kg'), findsOneWidget);

      await t.enterText(fields.at(1), '1');
      await t.enterText(fields.at(0), '0,5');
      await t.pumpAndSettle();
      expect(find.textContaining('Total fisik 12.5 Kg'), findsOneWidget);

      await t.tap(find.text('Simpan opname'));
      await t.pumpAndSettle();
      expect(find.text('Koreksi stok?'), findsOneWidget);
      expect(
        find.textContaining('0.5 Kg + 1 DUS × 12 = stok fisik 12.5'),
        findsOneWidget,
      );
      await t.tap(find.text('Ya, simpan'));
      await t.pumpAndSettle();

      // Ditolak server (bukan 5xx): draf tetap di layar, muncul tombol muat ulang satuan.
      expect(find.textContaining('muat ulang satuan'), findsWidgets);
      expect(find.text('Muat ulang satuan produk'), findsOneWidget);

      api.tambahan = const [
        {'satuan': 'DUS', 'isi': 24, 'barcode': '900DUS'},
      ];
      await t.tap(find.text('Muat ulang satuan produk'));
      await t.pumpAndSettle();
      expect(find.text('1 DUS = 24 Kg'), findsOneWidget);
      final kolom =
          find.descendant(of: sheet, matching: find.byType(TextField));
      expect(t.widget<TextField>(kolom.at(0)).controller!.text, '0,5');
      expect(t.widget<TextField>(kolom.at(1)).controller!.text, '1');
      expect(find.textContaining('Total fisik 24.5 Kg'), findsOneWidget);

      await t.tap(find.text('Simpan opname'));
      await t.pumpAndSettle();
      await t.tap(find.text('Ya, simpan'));
      await t.pumpAndSettle();

      // Kunci idempotensi tidak berganti di antara percobaan pada sheet yang sama.
      expect(api.kunci.toSet(), hasLength(1));
      expect(api.kiriman, hasLength(2));
      expect(api.kiriman.last, [
        (satuan: 'Kg', qty: '0.5', isi: '1'),
        (satuan: 'DUS', qty: '1', isi: '24'),
      ]);
      expect(find.text('Opname SO-26-000009 tersimpan'), findsOneWidget);
      expect(t.takeException(), isNull);
    },
  );
}
