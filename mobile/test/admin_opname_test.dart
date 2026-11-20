import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/hitungan.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_opname_page.dart';

typedef _Kiriman = ({
  String key,
  List<({int produkId, List<HitunganKirim> hitungan})> items,
});

class _ApiOpname extends ApiClient {
  _ApiOpname() : super(baseUrl: 'http://localhost', store: SecureTokenStore());

  static const nomorSo = 'SO-26-000001';
  final riwayatSo = <SoRingkas>[];
  final kiriman = <_Kiriman>[];
  bool sekaliPutus = true;

  @override
  Future<({List<SoRingkas> items, bool lagi})> daftarSo({
    required int tahun,
    int page = 1,
  }) async => (items: List.of(riwayatSo), lagi: false);

  @override
  Future<({List<StokItem> items, bool lagi})> cariStok({
    String? q,
    int page = 1,
    int size = 20,
  }) async => (
    items: [
      StokItem.fromJson({
        'id': 1,
        'kode': 'P001',
        'barcode': '899001',
        'nama': 'Minyak goreng',
        'stok': 5,
        'satuan': 'Liter',
      }),
      StokItem.fromJson({
        'id': 2,
        'kode': 'P002',
        'barcode': '899002',
        'nama': 'Gula pasir',
        'stok': 5,
        'satuan': 'Pcs',
        'satuan_tambahan': [
          {'satuan': 'DUS', 'isi': 12, 'barcode': '900DUS'},
        ],
      }),
    ],
    lagi: false,
  );

  @override
  Future<SoDokumen> simpanSo(
    List<({int produkId, List<HitunganKirim> hitungan})> items, {
    required String idempotencyKey,
  }) async {
    kiriman.add((key: idempotencyKey, items: List.of(items)));
    if (sekaliPutus && kiriman.length == 1) {
      riwayatSo.add(
        SoRingkas.fromJson({
          'nomor_so': nomorSo,
          'tanggal': '2026-11-18',
          'jumlah_item': 1,
          'nilai_selisih': '-500.00',
        }),
      );
      throw ApiException('Koneksi terputus', offline: true);
    }
    return detailSo(nomorSo);
  }

  @override
  Future<SoDokumen> detailSo(String nomor) async => SoDokumen.fromJson({
    'nomor_so': nomor,
    'tanggal': '2026-11-18',
    'items': [
      {
        'kode': 'P001',
        'nama': 'Minyak goreng',
        'jumlah_sistem': '5.0000',
        'qty_fisik': '0',
        'selisih': '-5.0000',
      },
    ],
  });
}

void main() {
  testWidgets('hitung ulang, pertahankan draf, dan retry dokumen yang sama', (
    t,
  ) async {
    await t.binding.setSurfaceSize(const Size(320, 660));
    addTearDown(() => t.binding.setSurfaceSize(null));
    final api = _ApiOpname();
    await t.pumpWidget(
      ProviderScope(
        overrides: [apiProvider.overrideWithValue(api)],
        child: MaterialApp(
          darkTheme: ThemeData.dark(),
          themeMode: ThemeMode.dark,
          home: const Scaffold(body: AdminOpnamePage()),
        ),
      ),
    );
    await t.pumpAndSettle();
    expect(find.textContaining('Belum ada dokumen'), findsOneWidget);

    await t.tap(find.text('Opname baru'));
    await t.pumpAndSettle();
    final cari = find.byType(TextField).first;
    await t.tap(find.text('Scan barcode'));
    // Kembalikan hasil scanner sebelum membuka platform view kamera.
    t.state<NavigatorState>(find.byType(Navigator)).pop('899001');
    await t.pumpAndSettle();
    final fisik = find.byType(TextFormField);
    expect(fisik, findsOneWidget);
    t.view.viewInsets = const FakeViewPadding(bottom: 260);
    addTearDown(t.view.resetViewInsets);
    await t.pumpAndSettle();
    await t.enterText(fisik, '1,23456');
    await t.tap(find.text('Simpan hitungan'));
    await t.pumpAndSettle();
    expect(find.textContaining('4 desimal'), findsOneWidget);
    await t.enterText(fisik, '1,2345');
    await t.pumpAndSettle();
    expect(find.textContaining('Selisih -3,7655'), findsOneWidget);
    await t.enterText(fisik, '0');
    await t.tap(find.text('Simpan hitungan'));
    await t.pumpAndSettle();
    t.view.resetViewInsets();
    await t.pumpAndSettle();

    await t.enterText(cari, '899001');
    await t.testTextInput.receiveAction(TextInputAction.search);
    await t.pumpAndSettle();
    expect(t.widget<TextFormField>(fisik).controller!.text, '0');
    await t.tap(find.text('Batal'));
    await t.pumpAndSettle();
    expect(find.textContaining('1/1 dihitung'), findsOneWidget);

    await t.binding.handlePopRoute();
    await t.pumpAndSettle();
    expect(find.text('Batalkan opname?'), findsOneWidget);
    await t.tap(find.text('Lanjutkan'));
    await t.pumpAndSettle();

    await t.tap(find.text('Tinjau dan simpan'));
    await t.pumpAndSettle();
    await t.tap(find.text('Simpan dokumen'));
    await t.pumpAndSettle();
    expect(
      find.textContaining('Hasil simpan belum terkonfirmasi'),
      findsOneWidget,
    );
    expect(find.text('Kembali ke hitungan'), findsNothing);
    await t.tap(find.text('Coba simpan lagi'));
    await t.pumpAndSettle();

    expect(api.kiriman, hasLength(2));
    expect(api.kiriman.last.key, api.kiriman.first.key);
    expect(api.kiriman.last.items, api.kiriman.first.items);
    expect(api.kiriman.last.items.single.hitungan.single.qty, '0');
    expect(api.riwayatSo, hasLength(1));
    expect(find.text('Dokumen SO-26-000001 tersimpan.'), findsOneWidget);
    expect(find.text(_ApiOpname.nomorSo), findsOneWidget);
    await t.tap(find.text('Buka dokumen'));
    await t.pumpAndSettle();
    expect(find.text('Minyak goreng'), findsOneWidget);
    expect(find.text('5'), findsOneWidget);
    expect(find.text('0'), findsOneWidget);
    expect(find.text('-5'), findsOneWidget);
    expect(t.takeException(), isNull);
  });

  testWidgets(
    'barcode satuan tambahan fokus ke kolomnya; scan ulang tidak duplikat',
    (t) async {
      await t.binding.setSurfaceSize(const Size(320, 660));
      addTearDown(() => t.binding.setSurfaceSize(null));
      final api = _ApiOpname()..sekaliPutus = false;
      await t.pumpWidget(
        ProviderScope(
          overrides: [apiProvider.overrideWithValue(api)],
          child: MaterialApp(
            darkTheme: ThemeData.dark(),
            themeMode: ThemeMode.dark,
            home: const Scaffold(body: AdminOpnamePage()),
          ),
        ),
      );
      await t.pumpAndSettle();
      await t.tap(find.text('Opname baru'));
      await t.pumpAndSettle();

      await t.tap(find.text('Scan barcode'));
      t.state<NavigatorState>(find.byType(Navigator)).pop('900DUS');
      await t.pumpAndSettle();
      final fields = find.byType(TextFormField);
      expect(fields, findsNWidgets(2));
      expect(t.widget<TextFormField>(fields.at(0)).controller!.text, isEmpty);
      expect(t.widget<TextFormField>(fields.at(1)).controller!.text, isEmpty);
      // Barcode DUS mengarahkan fokus ke kolom DUS, bukan kolom satuan dasar.
      final tf1 = find
          .descendant(of: fields.at(1), matching: find.byType(TextField))
          .first;
      expect(t.widget<TextField>(tf1).focusNode!.hasFocus, isTrue);

      await t.enterText(fields.at(1), '2');
      await t.enterText(fields.at(0), '3');
      await t.pumpAndSettle();
      expect(
        find.textContaining('Total fisik 27 Pcs · Selisih +22'),
        findsOneWidget,
      );
      await t.tap(find.text('Simpan hitungan'));
      await t.pumpAndSettle();
      expect(
        find.textContaining('3 Pcs + 2 DUS × 12 = 27 Pcs'),
        findsOneWidget,
      );
      expect(find.text('1/1 dihitung · 1 berselisih'), findsOneWidget);

      // Scan ulang barang yang sama: entri lama terbuka kembali, isian utuh, tanpa baris baru.
      await t.tap(find.text('Scan barcode'));
      t.state<NavigatorState>(find.byType(Navigator)).pop('900DUS');
      await t.pumpAndSettle();
      expect(find.byType(TextFormField), findsNWidgets(2));
      expect(
        t
            .widget<TextFormField>(find.byType(TextFormField).at(1))
            .controller!
            .text,
        '2',
      );
      expect(
        t
            .widget<TextFormField>(find.byType(TextFormField).at(0))
            .controller!
            .text,
        '3',
      );
      await t.tap(find.text('Batal'));
      await t.pumpAndSettle();
      expect(find.text('Ubah jumlah'), findsOneWidget);

      await t.tap(find.text('Tinjau dan simpan'));
      await t.pumpAndSettle();
      await t.tap(find.text('Simpan dokumen'));
      await t.pumpAndSettle();

      expect(api.kiriman, hasLength(1));
      final baris = api.kiriman.single.items.single;
      expect(baris.produkId, 2);
      expect(baris.hitungan, [
        (satuan: 'Pcs', qty: '3', isi: '1'),
        (satuan: 'DUS', qty: '2', isi: '12'),
      ]);
      expect(t.takeException(), isNull);
    },
  );
}
