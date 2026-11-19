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

  group('rincian penjualan: rentang', () {
    test('tambah hari lintas bulan/tahun dan jumlah hari inklusif', () {
      expect(tambahHari('2026-11-18', -6), '2026-11-12');
      expect(tambahHari('2027-01-02', -7), '2026-12-26');
      expect(tambahHari('2026-02-28', 1), '2026-03-01');
      expect(jumlahHari(const Rentang('2026-11-18', '2026-11-18')), 1);
      expect(jumlahHari(const Rentang('2026-11-12', '2026-11-18')), 7);
    });

    test('pilihan cepat mengikuti tanggal server', () {
      expect(rentangPreset(PresetRentang.hari, '2026-11-18'), const Rentang('2026-11-18', '2026-11-18'));
      expect(rentangPreset(PresetRentang.tujuh, '2026-11-18'), const Rentang('2026-11-12', '2026-11-18'));
      expect(rentangPreset(PresetRentang.hari, 'salah'), isNull);
      expect(rentangPreset(PresetRentang.bebas, '2026-11-18'), isNull);
    });

    test('periode berjalan dipotong sampai hari ini, null bila tidak diketahui', () {
      expect(
        rentangPreset(PresetRentang.periode, '2026-11-18', periodeDari: '2026-11-01', periodeSampai: '2026-11-30'),
        const Rentang('2026-11-01', '2026-11-18'),
      );
      expect(
        rentangPreset(PresetRentang.periode, '2026-12-05', periodeDari: '2026-11-01', periodeSampai: '2026-11-30'),
        const Rentang('2026-11-01', '2026-11-30'),
      );
      expect(rentangPreset(PresetRentang.periode, '2026-11-18'), isNull);
      expect(
        rentangPreset(PresetRentang.periode, '2026-10-31', periodeDari: '2026-11-01', periodeSampai: '2026-11-30'),
        isNull,
      );
    });

    test('validasi rentang meniru aturan server', () {
      const h = '2026-11-18';
      expect(validasiRentang(const Rentang('2026-11-12', h), h), isNull);
      expect(validasiRentang(const Rentang('', h), h), contains('tanggal'));
      expect(validasiRentang(const Rentang('2026-11-19', '2026-11-18'), h), contains('setelah'));
      expect(validasiRentang(const Rentang('2026-11-18', '2026-11-19'), h), contains('hari ini'));
      expect(validasiRentang(const Rentang('2026-08-18', '2026-11-17'), h), isNull); // tepat 92 hari
      expect(validasiRentang(const Rentang('2026-08-17', '2026-11-17'), h), contains('92'));
      expect(persenJenis(1, 3), 33);
      expect(persenJenis(5, 0), 0);
    });
  });

  group('rincian penjualan: model dan widget', () {
    final json = <String, dynamic>{
      'dari': '2026-11-16',
      'sampai': '2026-11-18',
      'hari': 3,
      'ringkasan': {
        'tunai': {'jumlah': 2, 'total': 400},
        'kredit': {'jumlah': 2, 'total': 270},
        'lain': {'jumlah': 1, 'total': 50},
        'total': {'jumlah': 5, 'total': 720},
      },
      'per_kasir': [
        {
          'kasir': 'k1',
          'nama': 'Rina',
          'tunai': {'jumlah': 2, 'total': 400},
          'kredit': {'jumlah': 1, 'total': 200},
          'lain': {'jumlah': 0, 'total': 0},
          'total': {'jumlah': 3, 'total': 600},
        },
      ],
      'harian': [
        {'tanggal': '2026-11-16', 'tunai': 100, 'kredit': 0, 'lain': 0, 'jumlah': 1},
        {'tanggal': '2026-11-17', 'tunai': 0, 'kredit': 0, 'lain': 0, 'jumlah': 0},
        {'tanggal': '2026-11-18', 'tunai': 300, 'kredit': 270, 'lain': 50, 'jumlah': 4},
      ],
    };

    test('model membaca ringkasan, kasir (pecahan di level yang sama), dan harian', () {
      final r = RincianPenjualan.fromJson(json);
      expect(r.hari, 3);
      expect(r.ringkasan.total.total, 720);
      expect(r.ringkasan.tunai.total + r.ringkasan.kredit.total + r.ringkasan.lain.total, r.ringkasan.total.total);
      expect(r.perKasir.single.nama, 'Rina');
      expect(r.perKasir.single.pecahan.total.total, 600);
      expect(r.harian.length, 3);
      expect(r.harian[2].kredit, 270);
    });

    testWidgets('KasirRincianKartu menampilkan tunai, kredit, total; Lainnya hanya bila ada', (t) async {
      final r = RincianPenjualan.fromJson(json);
      await t.pumpWidget(_host(KasirRincianKartu(judul: 'Rina', subjudul: 'k1', pecahan: r.perKasir.single.pecahan)));
      expect(find.text('Rina'), findsOneWidget);
      expect(find.text('Tunai'), findsOneWidget);
      expect(find.text('Kredit'), findsOneWidget);
      expect(find.text('Lainnya'), findsNothing);
      expect(find.textContaining('Total · 3 trx'), findsOneWidget);
      await t.pumpWidget(_host(KasirRincianKartu(judul: 'Semua kasir', pecahan: r.ringkasan, total: true)));
      expect(find.text('Lainnya'), findsOneWidget);
    });

    testWidgets('JenisKartu menampilkan total, jumlah transaksi, dan persen', (t) async {
      final r = RincianPenjualan.fromJson(json);
      await t.pumpWidget(_host(JenisKartu(label: 'Tunai', bagian: r.ringkasan.tunai, persen: 56, warna: Colors.green)));
      expect(find.text('Tunai'), findsOneWidget);
      expect(find.text('2 transaksi · 56%'), findsOneWidget);
    });

    testWidgets('TrenBertumpuk menyediakan ringkasan tunai/kredit untuk pembaca layar', (t) async {
      final handle = t.ensureSemantics();
      final r = RincianPenjualan.fromJson(json);
      await t.pumpWidget(_host(TrenBertumpuk(harian: r.harian)));
      expect(find.bySemanticsLabel(RegExp(r'^Penjualan harian tunai dan kredit: Sen tunai Rp 100, kredit Rp 0')), findsOneWidget);
      handle.dispose();
    });
  });

  test('statusMargin: tanpa HPP, rugi, impas', () {
    expect(statusMargin(700, 0), (tanpaHpp: true, margin: null));
    expect(statusMargin(900, 1000), (tanpaHpp: false, margin: -100));
    expect(statusMargin(1000, 1000), (tanpaHpp: false, margin: 0));
  });
}
