import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_satuan_page.dart';

class _ApiSatuan extends ApiClient {
  _ApiSatuan() : super(baseUrl: 'http://localhost', store: SecureTokenStore());

  final daftar = [
    {'id': '1', 'nama': 'PCS'},
    {'id': '2', 'nama': 'BOX'},
  ];
  final log = <String>[];

  @override
  Future<dynamic> get(String p, {Map<String, dynamic>? q}) async {
    if (p == '/api/v1/admin/satuan') return daftar;
    if (p == '/api/v1/admin/satuan/1/jejak') {
      return {'id': '1', 'nama': 'PCS', 'bisa_hapus': false, 'jumlah_produk': 3};
    }
    if (p == '/api/v1/admin/satuan/2/jejak') {
      return {'id': '2', 'nama': 'BOX', 'bisa_hapus': true, 'jumlah_produk': 0};
    }
    throw StateError('GET tidak diharapkan: $p');
  }

  @override
  Future<dynamic> post(
    String p, {
    Object? body,
    bool auth = true,
    Map<String, String>? headers,
  }) async {
    log.add('POST $p ${(body as Map)['nama']}');
    daftar.add({'id': '9', 'nama': body['nama'] as String});
    return {'id': '9', 'nama': body['nama']};
  }

  @override
  Future<dynamic> put(String p, {Object? body}) async {
    log.add('PUT $p ${(body as Map)['nama']}');
    daftar[1] = {'id': '2', 'nama': body['nama'] as String};
  }

  @override
  Future<dynamic> delete(String p) async {
    log.add('DELETE $p');
    daftar.removeWhere((e) => '/api/v1/admin/satuan/${e['id']}' == p);
  }
}

void main() {
  testWidgets('tambah, ubah, dan hapus satuan; satuan terpakai tidak bisa dihapus', (
    tester,
  ) async {
    final api = _ApiSatuan();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [apiProvider.overrideWithValue(api)],
        child: const MaterialApp(home: AdminSatuanPage()),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('PCS'), findsOneWidget);
    expect(find.text('BOX'), findsOneWidget);

    // Tambah: nama dikirim huruf besar.
    await tester.tap(find.text('Tambah satuan'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'dus');
    await tester.tap(find.text('Simpan'));
    await tester.pumpAndSettle();
    expect(api.log.last, 'POST /api/v1/admin/satuan DUS');
    expect(find.text('DUS'), findsOneWidget);

    // Ubah BOX -> KOTAK.
    await tester.tap(find.byTooltip('Ubah BOX'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'kotak');
    await tester.tap(find.text('Simpan'));
    await tester.pumpAndSettle();
    expect(api.log.last, 'PUT /api/v1/admin/satuan/2 KOTAK');
    expect(find.text('KOTAK'), findsOneWidget);

    // Terpakai 3 produk: dialog penolakan, tanpa DELETE.
    await tester.tap(find.byTooltip('Hapus PCS'));
    await tester.pumpAndSettle();
    expect(find.textContaining('masih dipakai 3 produk'), findsOneWidget);
    await tester.tap(find.text('Tutup'));
    await tester.pumpAndSettle();
    expect(api.log.any((l) => l.startsWith('DELETE')), isFalse);

    // Tidak terpakai: konfirmasi lalu DELETE.
    await tester.tap(find.byTooltip('Hapus KOTAK'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Hapus'));
    await tester.pumpAndSettle();
    expect(api.log.last, 'DELETE /api/v1/admin/satuan/2');
    expect(find.text('KOTAK'), findsNothing);
  });
}
