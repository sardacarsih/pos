import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_produk_page.dart';

class _ApiProduk extends ApiClient {
  _ApiProduk() : super(baseUrl: 'http://localhost', store: SecureTokenStore());

  final kiriman = <({Object? body, String? key})>[];

  @override
  Future<dynamic> get(String p, {Map<String, dynamic>? q}) async => switch (p) {
    '/api/v1/admin/produk' => {'items': [], 'ada_lagi': false},
    '/api/v1/admin/kategori' => [
      {'id': '1', 'nama': 'Minuman'},
    ],
    _ => throw StateError('GET tidak diharapkan: $p'),
  };

  @override
  Future<dynamic> post(
    String p, {
    Object? body,
    bool auth = true,
    Map<String, String>? headers,
  }) async {
    expect(p, '/api/v1/admin/produk');
    kiriman.add((body: body, key: headers?['Idempotency-Key']));
    if (kiriman.length == 1) throw ApiException('Koneksi terputus');
    return {
      ...(body as Map<String, dynamic>),
      'id': 1,
      'kode': 'KB-26-00001',
      'kategori': 'Minuman',
    };
  }
}

void main() {
  testWidgets(
    'tambah produk dan beberapa satuan, lalu retry dengan draf sama',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(360, 640);
      addTearDown(tester.view.resetDevicePixelRatio);
      addTearDown(tester.view.resetPhysicalSize);
      final api = _ApiProduk();
      await tester.pumpWidget(
        ProviderScope(
          overrides: [apiProvider.overrideWithValue(api)],
          child: const MaterialApp(home: AdminProdukPage()),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Tambah produk'));
      await tester.pumpAndSettle();

      Finder field(String label) => find.byWidgetPredicate(
        (w) => w is TextField && w.decoration?.labelText == label,
      );
      Future<void> isi(String label, String nilai, {int index = 0}) async {
        final input = field(label).at(index);
        await tester.ensureVisible(input);
        await tester.enterText(input, nilai);
        await tester.pumpAndSettle();
      }

      Future<void> tombol(String label) async {
        final button = find.text(label);
        await tester.ensureVisible(button);
        await tester.tap(button);
        await tester.pumpAndSettle();
      }

      await isi('Nama', 'Teh Kotak');
      await isi('Harga jual (Rp)', '5000');
      await isi('Harga beli (Rp)', '4000');
      await tombol('Tambah satuan');
      await isi('Satuan tambahan', 'BOX');
      await isi('Isi per satuan', '12');
      await isi('Harga jual satuan (Rp)', '55000');
      await isi('Barcode satuan (opsional)', '899012');
      await tombol('Tambah satuan');
      await isi('Satuan tambahan', 'PAK', index: 1);
      await isi('Isi per satuan', '6,5', index: 1);
      await isi('Harga jual satuan (Rp)', '30000,25', index: 1);
      await tombol('Simpan');
    expect(find.text('Koneksi terputus'), findsOneWidget);
      expect(
        tester
            .widget<TextField>(field('Satuan tambahan').first)
            .controller!
            .text,
        'BOX',
      );
      await tombol('Simpan');
      expect(api.kiriman, hasLength(2));
      expect(api.kiriman.first.key, isNotEmpty);
      expect(api.kiriman.first.key, api.kiriman.last.key);
      expect(api.kiriman.first.body, api.kiriman.last.body);
      expect((api.kiriman.last.body as Map)['satuan_lain'], [
        {'satuan': 'BOX', 'isi': 12, 'harga': 55000, 'barcode': '899012'},
        {'satuan': 'PAK', 'isi': 6.5, 'harga': 30000.25, 'barcode': null},
      ]);
      expect(find.text('Teh Kotak'), findsOneWidget);
      await tester.pump(const Duration(milliseconds: 500));
    },
  );
}
