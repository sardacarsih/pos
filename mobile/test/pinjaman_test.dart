import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/anggota/pinjaman_page.dart';

Map<String, dynamic> _aju(String status, {int? antrian}) => {
  'id': 7, 'jumlah': 3000000, 'tenor': 6, 'angsuran': 524861.73, 'antrian': antrian, 'status': status,
  'keperluan': 'Renovasi', 'tgl_aju': '2026-11-18T09:00:00', 'no_transaksi': null, 'alasan': null,
};

class FakeApi extends ApiClient {
  FakeApi(this.json) : super(baseUrl: 'http://localhost', store: _Store());
  Map<String, dynamic> json;
  final dikirim = <Map<String, Object>>[];

  @override
  Future<PinjamanSaya> pinjamanSaya() async => PinjamanSaya.fromJson(json);

  @override
  Future<void> ajukanPinjaman({required String idempotencyKey, required int jumlah, required int tenor, required String keperluan}) async {
    dikirim.add({'key': idempotencyKey, 'jumlah': jumlah, 'tenor': tenor, 'keperluan': keperluan});
    json = {...json, 'pengajuan': [_aju('DIAJUKAN')]};
  }
}

class _Store implements TokenStore {
  @override
  Future<void> clear() async {}
  @override
  Future<String> deviceId() async => 'x';
  @override
  Future<({String access, String refresh})?> read() async => null;
  @override
  Future<void> write(String access, String refresh) async {}
}

const _aturan = {'bunga': 1.6, 'maks_tenor': 12, 'maks_pinjaman': 5000000};

Future<FakeApi> _pasang(WidgetTester t, Map<String, dynamic> json) async {
  final api = FakeApi(json);
  await t.pumpWidget(ProviderScope(
    overrides: [apiProvider.overrideWithValue(api)],
    child: const MaterialApp(home: PinjamanPage()),
  ));
  await t.pumpAndSettle();
  return api;
}

void main() {
  testWidgets('pengajuan disetujui menampilkan nomor antrian dan menyembunyikan form', (t) async {
    await _pasang(t, {'aturan': _aturan, 'pengajuan': [_aju('DISETUJUI', antrian: 2)], 'pinjaman_aktif': []});
    expect(find.text('Nomor antrian pencairan: 2'), findsOneWidget);
    expect(find.text('Ajukan pinjaman'), findsNothing);
    expect(find.text('Batalkan pengajuan'), findsNothing);
  });

  testWidgets('form menolak jumlah di atas plafon lalu mengirim pengajuan valid', (t) async {
    final api = await _pasang(t, {'aturan': _aturan, 'pengajuan': [], 'pinjaman_aktif': []});
    expect(find.text('Ajukan pinjaman'), findsOneWidget);

    await t.enterText(find.widgetWithText(TextField, 'Jumlah pinjaman (Rp)'), '6000000');
    await t.enterText(find.widgetWithText(TextField, 'Keperluan'), 'Renovasi');
    await t.tap(find.text('Kirim pengajuan'));
    await t.pump();
    expect(find.text('Maksimal Rp 5.000.000'), findsOneWidget);
    expect(api.dikirim, isEmpty);

    await t.enterText(find.widgetWithText(TextField, 'Jumlah pinjaman (Rp)'), '3000000');
    await t.tap(find.text('Kirim pengajuan'));
    await t.pumpAndSettle();
    expect(api.dikirim.single['jumlah'], 3000000);
    expect(api.dikirim.single['tenor'], 12);
    expect(find.text('Batalkan pengajuan'), findsOneWidget);
  });
}
