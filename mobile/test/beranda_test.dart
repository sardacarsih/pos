import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/beranda_widgets.dart';
import 'package:kopkar_pos/core/models.dart';

Widget _host(Widget w) => MaterialApp(
  home: Scaffold(body: SingleChildScrollView(child: w)),
);

Ringkasan _r({int limit = 1000000, int belanja = 0, int? sisa}) => Ringkasan.fromJson({
  'nama': 'Siti',
  'periode': 202611,
  'dari': '2026-11-01',
  'sampai': '2026-11-30',
  'limit': limit,
  'belanja_periode': belanja,
  'sisa_limit': sisa ?? (limit == 0 ? null : limit - belanja),
});

void main() {
  group('logika murni', () {
    test('status limit mengikuti batas 80% dan 100%', () {
      expect(statusLimit(0, 0), StatusLimit.takDibatasi);
      expect(statusLimit(500, 1000), StatusLimit.aman);
      expect(statusLimit(799, 1000), StatusLimit.aman);
      expect(statusLimit(800, 1000), StatusLimit.hampir);
      expect(statusLimit(1000, 1000), StatusLimit.hampir);
      expect(statusLimit(1001, 1000), StatusLimit.terlampaui);
    });

    test('persen terpakai dibatasi 0..100', () {
      expect(persenTerpakai(250, 1000), 25);
      expect(persenTerpakai(5000, 1000), 100);
      expect(persenTerpakai(-5, 1000), 0);
      expect(persenTerpakai(100, 0), 0);
    });

    test('delta: naik, turun, sama, dan tanpa pembanding', () {
      expect(hitungDelta(150, 100).arah, ArahDelta.naik);
      expect(hitungDelta(150, 100).persen, 50);
      expect(hitungDelta(50, 100).arah, ArahDelta.turun);
      expect(hitungDelta(100, 100).arah, ArahDelta.tetap);
      expect(hitungDelta(100, 0).arah, ArahDelta.tidakAda);
      expect(teksDelta(hitungDelta(100, 0)), '—');
      expect(teksDelta(hitungDelta(150, 100)), '▲ 50% vs kemarin');
      expect(teksDelta(hitungDelta(50, 100)), '▼ 50% vs kemarin');
    });

    test('proporsi berjumlah 100 atau 0/0', () {
      expect(proporsi(1, 2), (33, 67));
      expect(proporsi(0, 0), (0, 0));
      expect(proporsi(5, 0), (100, 0));
    });

    test('tinggi batang relatif ke maksimum dan minimal terlihat', () {
      expect(tinggiBatang(0, [0, 10, 100]), 0);
      expect(tinggiBatang(100, [0, 10, 100]), 1);
      expect(tinggiBatang(10, [0, 10, 100]), closeTo(0.1, 1e-9));
      expect(tinggiBatang(1, [1, 1000]), 0.04);
      expect(tinggiBatang(5, [0, 0]), 0);
    });

    test('rupiah ringkas dan tanggal', () {
      expect(singkatRupiah(950), '950');
      expect(singkatRupiah(12500), '13 rb');
      expect(singkatRupiah(1250000), '1,3 jt');
      expect(singkatRupiah(2500000000), '2,5 M');
      expect(tanggalPanjang('2026-11-18'), 'Rabu, 18 November 2026');
      expect(tanggalPanjang('salah'), '');
      expect(hariSingkat('2026-11-18'), 'Rab');
    });
  });

  group('widget', () {
    testWidgets('LimitHero aman: sisa limit dan persen terpakai tampil', (t) async {
      await t.pumpWidget(_host(LimitHero(r: _r(belanja: 250000))));
      expect(find.text('Sisa limit'), findsOneWidget);
      expect(find.textContaining('(25%)'), findsOneWidget);
      expect(find.textContaining('Hampir habis'), findsNothing);
      expect(find.textContaining('terlampaui'), findsNothing);
    });

    testWidgets('LimitHero hampir habis dan terlampaui diberi label', (t) async {
      await t.pumpWidget(_host(LimitHero(r: _r(belanja: 850000))));
      expect(find.textContaining('Hampir habis'), findsOneWidget);
      await t.pumpWidget(_host(LimitHero(r: _r(belanja: 1200000, sisa: -200000))));
      expect(find.textContaining('Limit terlampaui'), findsOneWidget);
      // Sisa tidak pernah tampil negatif.
      expect(find.textContaining('-'), findsNothing);
    });

    testWidgets('LimitHero tanpa limit menampilkan Tidak dibatasi tanpa bar', (t) async {
      await t.pumpWidget(_host(LimitHero(r: _r(limit: 0, belanja: 300000))));
      expect(find.text('Tidak dibatasi'), findsOneWidget);
      expect(find.byType(LinearProgressIndicator), findsNothing);
    });

    testWidgets('KpiKartu menampilkan pembanding dan tidak mengarang angka tanpa pembanding', (t) async {
      await t.pumpWidget(_host(
        Column(
          children: [
            KpiKartu(label: 'Total', nilai: 'Rp 150', delta: hitungDelta(150, 100)),
            KpiKartu(label: 'Total2', nilai: 'Rp 100', delta: hitungDelta(100, 0)),
          ],
        ),
      ));
      expect(find.text('▲ 50% vs kemarin'), findsOneWidget);
      expect(find.text('—'), findsOneWidget);
    });

    testWidgets('PerhatianKartu memanggil onTap dan tanpa onTap tidak menampilkan panah', (t) async {
      var diketuk = 0;
      await t.pumpWidget(_host(
        Column(
          children: [
            PerhatianKartu(tingkat: Tingkat.peringatan, judul: '2 akun terkunci', pesan: 'Buka kunci', onTap: () => diketuk++),
            const PerhatianKartu(tingkat: Tingkat.bahaya, judul: 'Periode tidak terbuka', pesan: 'Tutup'),
          ],
        ),
      ));
      await t.tap(find.text('2 akun terkunci'));
      expect(diketuk, 1);
      expect(find.byIcon(Icons.chevron_right), findsOneWidget);
    });

    testWidgets('TrenBatang menyediakan ringkasan nilai untuk pembaca layar', (t) async {
      final handle = t.ensureSemantics();
      final tren = [
        for (var i = 0; i < 7; i++)
          HariJual.fromJson({'tanggal': '2026-11-${12 + i}', 'jumlah': i, 'total': i * 100000}),
      ];
      await t.pumpWidget(_host(TrenBatang(tren: tren)));
      expect(
        find.bySemanticsLabel(RegExp(r'^Penjualan harian: Kam Rp 0, Jum Rp 100\.000')),
        findsOneWidget,
      );
      handle.dispose();
    });

    testWidgets('ChipPeriode: terbuka dan tertutup', (t) async {
      await t.pumpWidget(_host(const ChipPeriode(kode: null, periode: 202611, dari: '2026-11-01', sampai: '2026-11-30')));
      expect(find.textContaining('Periode terbuka'), findsOneWidget);
      await t.pumpWidget(_host(const ChipPeriode(kode: 'PERIODE_TUTUP')));
      expect(find.text('Periode tertutup'), findsOneWidget);
    });
  });
}
