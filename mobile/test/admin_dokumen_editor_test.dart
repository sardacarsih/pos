import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_dokumen_editor_page.dart';

class _Api extends ApiClient {
  _Api() : super(baseUrl: 'http://localhost', store: SecureTokenStore());
}

class _Kiriman {
  final alasan = <String>[];
  final kunci = <String>[];
  final qty = <List<String>>[];
  var gagal = 1;
}

Future<_Kiriman> _buka(WidgetTester t, {bool perluAlasan = true}) async {
  final k = _Kiriman();
  await t.pumpWidget(
    ProviderScope(
      overrides: [apiProvider.overrideWithValue(_Api())],
      child: MaterialApp(
        home: AdminDokumenEditorPage(
          judul: 'Ubah SO-26-000001',
          labelQty: 'Fisik',
          perluAlasan: perluAlasan,
          awal: [
            BarisEditor(produkId: 7, kode: 'P007', nama: 'Tepung', satuan: 'Kg', qty: '3'),
          ],
          simpan: (baris, alasan, key) async {
            k.alasan.add(alasan);
            k.kunci.add(key);
            k.qty.add([for (final b in baris) b.qtyKirim!]);
            if (k.gagal-- > 0) throw ApiException('Dokumen sudah berubah', status: 409);
          },
        ),
      ),
    ),
  );
  return k;
}

void main() {
  testWidgets('alasan wajib; error server tampil; retry isi sama memakai kunci sama', (t) async {
    await t.binding.setSurfaceSize(const Size(400, 800));
    addTearDown(() => t.binding.setSurfaceSize(null));
    final k = await _buka(t);

    await t.tap(find.text('Simpan'));
    await t.pump();
    expect(find.text('Alasan wajib diisi.'), findsOneWidget);
    expect(k.kunci, isEmpty);

    await t.enterText(find.widgetWithText(TextField, 'Alasan perubahan'), 'salah hitung');
    await t.tap(find.text('Simpan'));
    await t.pumpAndSettle();
    expect(find.text('Dokumen sudah berubah'), findsOneWidget);
    expect(k.kunci, hasLength(1));

    await t.tap(find.text('Simpan')); // isi sama -> kunci sama
    await t.pumpAndSettle();
    expect(k.kunci, hasLength(2));
    expect(k.kunci[1], k.kunci[0]);
    expect(k.qty.last, ['3']);
  });

  testWidgets('isi berubah setelah gagal -> kunci idempotensi baru; koma desimal diterima', (t) async {
    await t.binding.setSurfaceSize(const Size(400, 800));
    addTearDown(() => t.binding.setSurfaceSize(null));
    final k = await _buka(t);
    await t.enterText(find.widgetWithText(TextField, 'Alasan perubahan'), 'koreksi');
    await t.tap(find.text('Simpan'));
    await t.pumpAndSettle();

    await t.enterText(find.widgetWithText(TextField, 'Fisik (Kg)'), '2,5');
    await t.tap(find.text('Simpan'));
    await t.pumpAndSettle();
    expect(k.kunci, hasLength(2));
    expect(k.kunci[1], isNot(k.kunci[0]));
    expect(k.qty.last, ['2.5']);
  });

  testWidgets('baris dihapus sampai kosong ditolak di klien', (t) async {
    await t.binding.setSurfaceSize(const Size(400, 800));
    addTearDown(() => t.binding.setSurfaceSize(null));
    final k = await _buka(t, perluAlasan: false);
    await t.tap(find.byTooltip('Hapus Tepung'));
    await t.pump();
    await t.tap(find.text('Simpan'));
    await t.pump();
    expect(find.text('Daftar barang masih kosong.'), findsOneWidget);
    expect(k.kunci, isEmpty);
  });
}
