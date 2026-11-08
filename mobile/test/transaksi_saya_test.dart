import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/anggota/transaksi_saya_page.dart';

class FakeApi extends ApiClient {
  FakeApi() : super(baseUrl: 'http://localhost', store: _Store());

  @override
  Future<Ringkasan> ringkasan(int periode) async => Ringkasan.fromJson({'nama': 'Budi', 'periode': periode, 'limit': 1000000, 'belanja_periode': 250000, 'sisa_limit': 750000});

  @override
  Future<({List<TransaksiRingkas> items, bool lagi})> transaksi(int periode, {int page = 1}) async => (
        items: [
          TransaksiRingkas.fromJson({'no_transaksi': 'W-26-000010', 'tanggal': '2026-11-02', 'jam': '10:00:00', 'jenis_bayar': 'KREDIT', 'tenor': 1, 'total': 250000}),
        ],
        lagi: false,
      );
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

void main() {
  testWidgets('layar Transaksi Saya menampilkan ringkasan limit dan daftar transaksi sendiri', (t) async {
    await t.pumpWidget(ProviderScope(
      overrides: [apiProvider.overrideWithValue(FakeApi())],
      child: const MaterialApp(home: TransaksiSayaPage()),
    ));
    await t.pumpAndSettle();

    expect(find.text('Transaksi Saya'), findsOneWidget);
    expect(find.text('Rp 250.000'), findsWidgets); // belanja periode + total transaksi
    expect(find.textContaining('Sisa Rp 750.000'), findsOneWidget);
    expect(find.text('W-26-000010', skipOffstage: false), findsNothing); // nomor tampil di subtitle gabungan
    expect(find.textContaining('W-26-000010'), findsOneWidget);
  });
}
