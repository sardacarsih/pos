import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/beranda_widgets.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/core/theme.dart';
import 'package:kopkar_pos/features/admin/admin_dashboard_page.dart';
import 'package:kopkar_pos/features/admin/admin_katalog_page.dart';
import 'package:kopkar_pos/features/anggota/dashboard_anggota_page.dart';
import 'package:kopkar_pos/features/kasir/dashboard_page.dart';

const _hariIni = '2026-11-18';

class _ApiBeranda extends ApiClient {
  _ApiBeranda() : super(baseUrl: 'http://localhost', store: SecureTokenStore());

  bool terbuka = true;
  bool periodeAda = true;
  bool demo = false;
  bool gagal = false;
  bool gagalStok = false;
  bool adaTerakhir = false;
  int stok = 0;
  int terkunci = 0;
  int hargaRendah = 0;
  int kasir = 2;
  final fakturDiminta = <String>[];
  final rentangDiminta = <Rentang>[];
  final responsTertunda = <Rentang, Completer<RincianPenjualan>>{};

  Map<String, dynamic> get _dasar => {
    'tanggal': _hariIni,
    'periode': 202611,
    'periode_dari': periodeAda ? '2026-11-01' : null,
    'periode_sampai': periodeAda ? '2026-11-30' : null,
    'periode_terbuka': terbuka,
    'periode_kode': terbuka ? null : 'PERIODE_TUTUP',
    'periode_pesan': terbuka ? null : 'Periode sedang ditutup',
  };

  @override
  Future<DashboardKasir> dashboardKasir() async {
    if (gagal) throw ApiException('Koneksi putus');
    return DashboardKasir.fromJson({
      ..._dasar,
      'ringkasan': {
        'jumlah': 2,
        'total': 789000,
        'tunai': 499000,
        'kredit': 290000,
      },
      'terakhir': [
        if (adaTerakhir) ...[
          {
            'no_transaksi': 'BBM-26-000001',
            'jam': '10:15',
            'nama_pelanggan': 'Siti Aminah',
            'jenis_bayar': 'TUNAI',
            'total': 25000,
          },
          {
            'no_transaksi': 'BBM-26-000002',
            'jam': '10:20',
            'nama_pelanggan': '',
            'jenis_bayar': 'KREDIT',
            'total': 30000,
          },
        ],
      ],
    });
  }

  @override
  Future<Faktur> fakturKasir(String no) async {
    fakturDiminta.add(no);
    return Faktur.fromJson({
      'no_transaksi': no,
      'tanggal': _hariIni,
      'jam': '10:15',
      'nama_kasir': 'Rina',
      'kasir': 'kasir-01',
      'nama_pelanggan': 'Siti Aminah',
      'jenis_bayar': 'TUNAI',
    });
  }

  @override
  Future<DashboardAnggota> dashboardSaya() async {
    if (gagal) throw ApiException('Koneksi putus');
    return DashboardAnggota.fromJson({
      ..._dasar,
      'belanja_demo': demo,
      'pesan': 'Periode sedang ditutup',
      'ringkasan': {
        'nama': 'Siti Anggota',
        'periode': 202611,
        'dari': '2026-11-01',
        'sampai': '2026-11-30',
        'limit': 10000000,
        'sisa_limit': 9250000,
        'belanja_periode': 750000,
      },
      'terakhir': [
        if (adaTerakhir)
          {
            'no_transaksi': 'BBM-26-000009',
            'tanggal': _hariIni,
            'jam': '11:05',
            'jenis_bayar': 'KREDIT',
            'tenor': 3,
            'total': 120000,
          },
      ],
    });
  }

  @override
  Future<DashboardAdmin> dashboardAdmin() async {
    if (gagal) throw ApiException('Koneksi putus');
    return DashboardAdmin.fromJson({
      ..._dasar,
      'ringkasan': {'jumlah': 5, 'total': 750000},
      'kemarin': {'tanggal': '2026-11-17', 'jumlah': 4, 'total': 500000},
      'user': {'terkunci': terkunci},
      'perhatian': {'harga_rendah': hargaRendah},
      'tren': [
        for (var hari = 12; hari <= 18; hari++)
          {'tanggal': '2026-11-$hari', 'total': hari * 10000000},
      ],
    });
  }

  @override
  Future<int> stokHabisJumlah() async {
    if (gagalStok) throw ApiException('Stok gagal diperiksa');
    return stok;
  }

  @override
  Future<RincianPenjualan> rincianPenjualan(String dari, String sampai) async {
    final r = Rentang(dari, sampai);
    rentangDiminta.add(r);
    final tertunda = responsTertunda.remove(r);
    return tertunda == null ? _rincian(r, kasir: kasir) : await tertunda.future;
  }
}

Map<String, int> _bagian(int jumlah, int total) => {
  'jumlah': jumlah,
  'total': total,
};

RincianPenjualan _rincian(Rentang r, {int kasir = 2, String nama = 'Rina'}) {
  final hari = jumlahHari(r);
  return RincianPenjualan.fromJson({
    'dari': r.dari,
    'sampai': r.sampai,
    'hari': hari,
    'ringkasan': kasir == 0
        ? <String, dynamic>{}
        : {
            'tunai': _bagian(1, 451000),
            'kredit': _bagian(2, 231000),
            'lain': _bagian(1, 52000),
            'total': _bagian(4, 734000),
          },
    'per_kasir': [
      if (kasir > 0)
        {
          'nama': nama,
          'kasir': 'kasir-01',
          'tunai': _bagian(1, 451000),
          'total': _bagian(1, 451000),
        },
      if (kasir > 1)
        {
          'kasir': 'kasir-desktop',
          'kredit': _bagian(2, 231000),
          'lain': _bagian(1, 52000),
          'total': _bagian(3, 283000),
        },
      for (var i = 2; i < kasir; i++) {'kasir': 'kasir-$i'},
    ],
    'harian': [
      for (var i = 0; i < hari; i++)
        {'tanggal': tambahHari(r.dari, i), 'tunai': i == 0 ? 451000 : 0},
    ],
  });
}

Widget _host(
  _ApiBeranda api,
  Widget layar, {
  bool gelap = false,
  double skala = 1,
}) => ProviderScope(
  overrides: [apiProvider.overrideWithValue(api)],
  child: MaterialApp(
    theme: kopkarTheme(gelap ? Brightness.dark : Brightness.light),
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context)
          .copyWith(textScaler: TextScaler.linear(skala)),
      child: child!,
    ),
    home: layar,
  ),
);

Future<void> _ketuk(WidgetTester t, Finder finder, {bool settle = true}) async {
  if (finder.hitTestable().evaluate().isEmpty) {
    await t.ensureVisible(finder);
    await t.pump();
  }
  expect(finder.hitTestable(), findsOneWidget);
  await t.tap(finder.hitTestable());
  if (settle) await t.pumpAndSettle();
}

Finder _kartuKasir(String nama) => find.ancestor(
  of: find.text(nama),
  matching: find.byType(KasirRincianKartu),
);

void main() {
  testWidgets(
    'Kasir menampilkan pembayaran, aksi jual, riwayat, dan periode tertutup',
    (t) async {
      final api = _ApiBeranda();
      var jual = 0;
      var riwayat = 0;
      await t.pumpWidget(
        _host(
          api,
          DashboardPage(
            aktif: true,
            onJual: () => jual++,
            onRiwayat: () => riwayat++,
          ),
        ),
      );
      await t.pumpAndSettle();

      expect(find.text('Rp 789.000'), findsOneWidget);
      expect(find.text('Tunai'), findsOneWidget);
      expect(find.text('Rp 499.000'), findsOneWidget);
      expect(find.text('Kredit'), findsOneWidget);
      expect(find.text('Rp 290.000'), findsOneWidget);
      await _ketuk(t, find.text('Mulai jual'));
      await _ketuk(t, find.text('Riwayat'));
      expect(jual, 1);
      expect(riwayat, 1);
      expect(find.text('Belum ada transaksi hari ini.'), findsOneWidget);

      final refresh = t.widget<RefreshIndicator>(find.byType(RefreshIndicator));
      api.gagal = true;
      await refresh.onRefresh();
      await t.pumpAndSettle();
      expect(
        find.text('Pembaruan gagal · menampilkan data terakhir'),
        findsOneWidget,
      );
      expect(find.text('Rp 789.000'), findsOneWidget);

      api.gagal = false;
      api.terbuka = false;
      await _ketuk(t, find.text('Coba lagi'));
      expect(find.text('Periode sedang ditutup'), findsOneWidget);
      final tombol = find.ancestor(
        of: find.text('Mulai jual'),
        matching: find.byType(FilledButton),
      );
      expect(t.widget<FilledButton>(tombol).onPressed, isNull);

      // Transaksi terakhir: nama pelanggan, tapi nomor faktur bila pelanggan kosong.
      api.adaTerakhir = true;
      await t
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await t.pumpAndSettle();
      await t.scrollUntilVisible(find.text('Siti Aminah'), 200);
      expect(find.text('Siti Aminah'), findsOneWidget);
      expect(find.text('10:15 · BBM-26-000001 · TUNAI'), findsOneWidget);
      expect(find.text('Rp 25.000'), findsOneWidget);
      expect(find.text('BBM-26-000002'), findsOneWidget);
      expect(find.text('Rp 30.000'), findsOneWidget);
      await _ketuk(t, find.text('Siti Aminah'));
      expect(api.fakturDiminta, ['BBM-26-000001']);
      expect(find.text('Kasir: Rina'), findsOneWidget);
    },
  );

  testWidgets(
    'Anggota bisa mencoba lagi, belanja demo, membuka riwayat dan kartu QR',
    (t) async {
      final api = _ApiBeranda()..gagal = true;
      var belanja = 0;
      var riwayat = 0;
      final router = GoRouter(
        routes: [
          GoRoute(
            path: '/',
            builder: (_, _) => DashboardAnggotaPage(
              aktif: true,
              onBelanja: () => belanja++,
              onRiwayat: () => riwayat++,
            ),
          ),
          GoRoute(
            path: '/saya/kartu-anggota',
            builder: (_, _) => const Scaffold(body: Text('QR anggota')),
          ),
          GoRoute(
            path: '/saya/faktur/:no',
            builder: (_, s) =>
                Scaffold(body: Text('Faktur ${s.pathParameters['no']}')),
          ),
        ],
      );
      addTearDown(router.dispose);
      await t.pumpWidget(
        ProviderScope(
          overrides: [apiProvider.overrideWithValue(api)],
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await t.pumpAndSettle();
      expect(find.text('Beranda tidak dapat dimuat'), findsOneWidget);
      expect(find.text('Kartu anggota'), findsOneWidget);

      api.gagal = false;
      api.demo = true;
      await _ketuk(t, find.text('Coba lagi'));
      expect(find.text('Sisa limit'), findsOneWidget);
      expect(find.text('Mode demo'), findsOneWidget);
      await _ketuk(t, find.text('Belanja mandiri'));
      await _ketuk(t, find.text('Lihat semua'));
      expect(belanja, 1);
      expect(riwayat, 1);
      expect(
        find.text('Belum ada transaksi pada periode ini.'),
        findsOneWidget,
      );
      await _ketuk(t, find.text('Kartu anggota'));
      expect(find.text('QR anggota'), findsOneWidget);

      api.terbuka = false;
      router.go('/');
      await t.pumpAndSettle();
      expect(find.text('Belanja belum bisa dilakukan'), findsOneWidget);
      final tombol = find.ancestor(
        of: find.text('Belanja mandiri'),
        matching: find.byType(FilledButton),
      );
      expect(t.widget<FilledButton>(tombol).onPressed, isNull);

      // Transaksi terakhir periode ini, lengkap dengan tenor angsuran, menuju detail faktur.
      api.adaTerakhir = true;
      await t
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await t.pumpAndSettle();
      await t.scrollUntilVisible(find.text('Rp 120.000'), 200);
      expect(
        find.text('$_hariIni 11:05 · BBM-26-000009 · 3x angsuran'),
        findsOneWidget,
      );
      await _ketuk(t, find.text('Rp 120.000'));
      expect(find.text('Faktur BBM-26-000009'), findsOneWidget);
    },
  );

  testWidgets(
    'Admin menampilkan rekap penuh, pecahan per kasir, dan batas daftar',
    (t) async {
      await t.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => t.binding.setSurfaceSize(null));
      final api = _ApiBeranda()..kasir = 50;
      await t.pumpWidget(_host(api, const AdminDashboardPage(aktif: true)));
      await t.pumpAndSettle();
      expect(api.rentangDiminta, [const Rentang(_hariIni, _hariIni)]);
      final semua = _kartuKasir('Semua kasir');
      final rina = _kartuKasir('Rina');
      final desktop = _kartuKasir('kasir-desktop');
      expect(t.getTopLeft(semua).dy, lessThan(t.getTopLeft(rina).dy));
      expect(
        find.descendant(of: semua, matching: find.text('Rp 734.000')),
        findsOneWidget,
      );
      expect(
        find.descendant(of: rina, matching: find.text('Tunai · 1 transaksi')),
        findsOneWidget,
      );
      expect(
        find.descendant(of: rina, matching: find.text('Rp 451.000')),
        findsNWidgets(2),
      );
      expect(
        find.descendant(of: rina, matching: find.text('Kredit · 0 transaksi')),
        findsOneWidget,
      );
      expect(
        find.descendant(of: rina, matching: find.text('Rp 0')),
        findsOneWidget,
      );
      await t.ensureVisible(desktop);
      await t.pumpAndSettle();
      expect(
        find.descendant(
          of: desktop,
          matching: find.text('Kredit · 2 transaksi'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(of: desktop, matching: find.text('Rp 231.000')),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: desktop,
          matching: find.text('Lainnya · 1 transaksi'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: desktop,
          matching: find.text('Total · 3 transaksi'),
        ),
        findsOneWidget,
      );
      await t.scrollUntilVisible(
        find.text('Menampilkan 50 kasir dengan penjualan terbesar.'),
        650,
      );
      expect(
        find.text('Menampilkan 50 kasir dengan penjualan terbesar.'),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'Admin memuat semua pilihan rentang, mengabaikan respons lama, dan mengulang rentang gagal',
    (t) async {
      await t.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => t.binding.setSurfaceSize(null));
      final api = _ApiBeranda();
      const tujuh = Rentang('2026-11-12', _hariIni);
      const periode = Rentang('2026-11-01', _hariIni);
      final lambat = Completer<RincianPenjualan>();
      final gagal = Completer<RincianPenjualan>();
      api.responsTertunda[tujuh] = lambat;
      api.responsTertunda[periode] = gagal;
      await t.pumpWidget(_host(api, const AdminDashboardPage(aktif: true)));
      await t.pumpAndSettle();

      await _ketuk(t, find.text('7 hari'), settle: false);
      await t.pump();
      expect(
        find.bySemanticsLabel('Memuat penjualan per kasir'),
        findsOneWidget,
      );
      expect(find.text('Rina'), findsNothing);
      await _ketuk(t, find.text('Periode berjalan'), settle: false);
      await t.pump();
      gagal.completeError(ApiException('Rincian gagal'));
      await t.pumpAndSettle();
      expect(
        find.text('Penjualan per kasir tidak dapat dimuat'),
        findsOneWidget,
      );
      await _ketuk(t, find.text('Coba lagi'));
      expect(api.rentangDiminta, [
        const Rentang(_hariIni, _hariIni),
        tujuh,
        periode,
        periode,
      ]);
      expect(find.text('Rina'), findsOneWidget);

      lambat.complete(_rincian(tujuh, nama: 'Respons lama'));
      await t.pumpAndSettle();
      expect(find.text('Respons lama'), findsNothing);
      expect(find.text('Rina'), findsOneWidget);

      api.periodeAda = false;
      await t
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await t.pumpAndSettle();
      expect(api.rentangDiminta.last, const Rentang(_hariIni, _hariIni));
      final hariIni = find.widgetWithText(ChoiceChip, 'Hari ini');
      expect(t.widget<ChoiceChip>(hariIni).selected, isTrue);

      api.kasir = 0;
      await _ketuk(t, find.text('Pilih tanggal'));
      await t.tap(find.byIcon(Icons.edit_outlined));
      await t.pumpAndSettle();
      final input = find.byType(TextField);
      await t.enterText(input.at(0), '11/3/2026');
      await t.enterText(input.at(1), '11/5/2026');
      await _ketuk(t, find.text('OK'));
      expect(
        api.rentangDiminta.last,
        const Rentang('2026-11-03', '2026-11-05'),
      );
      expect(
        find.text('Belum ada transaksi pada 03 Nov 2026 – 05 Nov 2026.'),
        findsOneWidget,
      );
      expect(find.text('Rina'), findsNothing);
    },
  );

  testWidgets(
    'Admin membedakan kegagalan cek stok dan membuka tujuan peringatan',
    (t) async {
      final api = _ApiBeranda()..gagalStok = true;
      final tab = <int>[];
      final awal = adminKatalogTab.value;
      addTearDown(() => adminKatalogTab.value = awal);
      await t.pumpWidget(
        _host(api, AdminDashboardPage(aktif: true, onBuka: tab.add)),
      );
      await t.pumpAndSettle();
      expect(find.text('Status stok habis belum tersedia'), findsOneWidget);
      expect(
        find.text('Tidak ada yang perlu perhatian saat ini'),
        findsNothing,
      );

      api.gagalStok = false;
      api.stok = 4;
      await _ketuk(t, find.text('Coba lagi'));
      await _ketuk(t, find.text('4 produk stok habis'));
      expect(tab.last, adminTabProduk);
      expect(adminKatalogTab.value, 1);

      api.hargaRendah = 2;
      api.terkunci = 1;
      final refresh = t.widget<RefreshIndicator>(find.byType(RefreshIndicator));
      await refresh.onRefresh();
      await t.pumpAndSettle();
      await _ketuk(t, find.text('1 akun terkunci'));
      expect(tab.last, adminTabUser);
      await _ketuk(t, find.text('2 produk harga jual ≤ HPP'));
      expect(tab.last, adminTabProduk);
      expect(adminKatalogTab.value, 2);
    },
  );

  for (final (lebar, tinggi) in [
    (320.0, 720.0),
    (390.0, 840.0),
    (1024.0, 900.0),
  ]) {
    for (final gelap in [false, true]) {
      for (final skala in [1.0, 2.0]) {
        testWidgets('Layout Beranda $lebar dp, gelap=$gelap, teks $skala', (
          t,
        ) async {
          await t.binding.setSurfaceSize(Size(lebar, tinggi));
          addTearDown(() => t.binding.setSurfaceSize(null));
          final semantik = t.ensureSemantics();
          final api = _ApiBeranda();
          for (final (layar, akhir) in <(Widget, String)>[
            (
              DashboardPage(aktif: true, onJual: () {}, onRiwayat: () {}),
              'Belum ada transaksi hari ini.',
            ),
            (
              DashboardAnggotaPage(
                aktif: true,
                onBelanja: () {},
                onRiwayat: () {},
              ),
              'Belum ada transaksi pada periode ini.',
            ),
            (const AdminDashboardPage(aktif: true), 'User mobile'),
          ]) {
            await t.pumpWidget(_host(api, layar, gelap: gelap, skala: skala));
            await t.pumpAndSettle();
            await expectLater(t, meetsGuideline(androidTapTargetGuideline));
            final vertikal = find.byWidgetPredicate(
              (widget) =>
                  widget is Scrollable &&
                  widget.axisDirection == AxisDirection.down,
            );
            if (layar is AdminDashboardPage) {
              await t.scrollUntilVisible(
                find.text('7 hari'),
                180,
                scrollable: vertikal,
              );
              await _ketuk(t, find.text('7 hari'));
              expect(
                api.rentangDiminta.last,
                const Rentang('2026-11-12', _hariIni),
              );
              await expectLater(t, meetsGuideline(androidTapTargetGuideline));
            }
            await t.scrollUntilVisible(
              find.text(akhir),
              280,
              scrollable: vertikal,
            );
            await t.pumpAndSettle();
            final galat = t.takeException();
            expect(
              galat,
              isNull,
              reason:
                  '${layar.runtimeType}: ${galat is FlutterError ? galat.toStringDeep() : galat}',
            );
            expect(t.getSize(find.byType(AppBar)).height, lessThan(90));
          }
          semantik.dispose();
        });
      }
    }
  }
}
