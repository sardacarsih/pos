import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/widgets.dart';

Widget _host(Widget w) => MaterialApp(home: Scaffold(body: w));

void main() {
  testWidgets('periode terbuka menampilkan nama periode dan rentang tanggal', (t) async {
    await t.pumpWidget(
      _host(
        const StatusPeriodeCard(
          kode: null,
          pesan: null,
          tanggal: '2026-11-08',
          periode: 202611,
          dari: '2026-10-26',
          sampai: '2026-11-25',
        ),
      ),
    );
    expect(find.textContaining('Periode November 2026 · 26 Okt 2026 – 25 Nov 2026'), findsOneWidget);
    expect(find.textContaining('2026-11-08'), findsOneWidget);
  });

  testWidgets('tanpa data periode (di luar semua periode) tidak error dan tanpa rentang', (t) async {
    await t.pumpWidget(
      _host(
        const StatusPeriodeCard(
          kode: 'PERIODE_TIDAK_ADA',
          pesan: 'Tanggal di luar rentang',
          tanggal: '2026-11-08',
        ),
      ),
    );
    expect(find.textContaining('Periode November'), findsNothing);
    expect(find.textContaining('Tanggal di luar rentang'), findsOneWidget);
  });

  testWidgets('periode tutup tetap menampilkan rentang di atas pesan', (t) async {
    await t.pumpWidget(
      _host(
        const StatusPeriodeCard(
          kode: 'PERIODE_TUTUP',
          pesan: 'Periode sudah ditutup',
          tanggal: '2026-03-10',
          periode: 202603,
          dari: '2026-02-26',
          sampai: '2026-03-13',
        ),
      ),
    );
    expect(find.textContaining('Periode Maret 2026 · 26 Feb 2026 – 13 Mar 2026'), findsOneWidget);
    expect(find.textContaining('Periode sudah ditutup'), findsOneWidget);
  });
}
