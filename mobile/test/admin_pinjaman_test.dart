import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_pinjaman_page.dart';

PengajuanPinjaman _p(String status, {int? verif, int? setuju, int? antrian}) =>
    PengajuanPinjaman.fromJson({
      'id': 7,
      'jumlah': 2000000,
      'tenor': 6,
      'angsuran': 352246.88,
      'antrian': antrian,
      'status': status,
      'keperluan': 'Renovasi',
      'tgl_aju': '2026-11-19T09:00:00',
      'no_transaksi': null,
      'alasan': null,
      'nik': 'N1',
      'nama': 'Ani',
      'verif_user': verif,
      'setuju_user': setuju,
    });

class FakeApi extends ApiClient {
  FakeApi() : super(baseUrl: 'http://localhost', store: _Store());
  final dikirim = <String>[];
  final filterLaporan = <String?>[];

  @override
  Future<({List<PengajuanPinjaman> items, List<String> hak, int userId})>
  daftarPinjaman({String? status, String? q}) async =>
      (items: [_p('DIAJUKAN')], hak: ['VERIFIKASI'], userId: 9);

  @override
  Future<AntrianPinjaman> antrianPinjaman() async => AntrianPinjaman.fromJson({
    'periode': 202611,
    'terpakai': 0,
    'plafon': null,
    'sisa': null,
    'items': [],
  });

  @override
  Future<LaporanPinjaman> laporanPinjaman({String? q}) async {
    filterLaporan.add(q);
    return LaporanPinjaman.fromJson({
      'jumlah': 1,
      'total_pinjaman': 3000000,
      'total_sisa_pokok': 2500000.5,
      'items': [
        {
          'no_transaksi': 'P-26-000001',
          'tanggal': '2026-11-19',
          'nik': 'N2',
          'nama': 'Budi',
          'unit_kerja': '00',
          'pinjaman': 3000000,
          'tenor': 6,
          'angsuran': 528370.32,
          'angsuran_lewat': 1,
          'sisa_pokok': 2500000.5,
          'jatuh_tempo_akhir': '2027-04-30',
        },
      ],
    });
  }

  @override
  Future<String> prosesPinjaman(
    int id,
    String aksi, {
    String? catatan,
    required String idempotencyKey,
  }) async {
    dikirim.add('$id/$aksi/${catatan ?? ''}');
    return '$id';
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

void main() {
  test('aksi pengurus mengikuti status, hak, pengurus berbeda, dan kepala antrian (sama dengan web)', () {
    expect(aksiPinjaman(_p('DIAJUKAN'), ['VERIFIKASI'], 9, false), [
      'verifikasi',
      'tolak',
    ]);
    expect(aksiPinjaman(_p('DIAJUKAN'), ['CAIR'], 9, false), isEmpty);
    expect(aksiPinjaman(_p('DIVERIFIKASI'), ['SETUJU'], 9, false), [
      'setujui',
      'tolak',
    ]);
    expect(aksiPinjaman(_p('DIVERIFIKASI', verif: 9), ['SETUJU'], 9, false), [
      'tolak',
    ]);
    expect(aksiPinjaman(_p('DISETUJUI'), ['CAIR'], 9, false), isEmpty);
    expect(aksiPinjaman(_p('DISETUJUI'), ['CAIR'], 9, true), ['cairkan']);
    expect(
      aksiPinjaman(_p('DISETUJUI', setuju: 9), ['CAIR'], 9, true),
      isEmpty,
    );
    expect(
      aksiPinjaman(_p('DICAIRKAN'), ['VERIFIKASI', 'SETUJU', 'CAIR'], 9, true),
      isEmpty,
    );
  });

  testWidgets(
    'pengurus memverifikasi, tolak wajib alasan, dan laporan tampil',
    (t) async {
      final api = FakeApi();
      await t.pumpWidget(
        ProviderScope(
          overrides: [apiProvider.overrideWithValue(api)],
          child: const MaterialApp(home: AdminPinjamanPage()),
        ),
      );
      await t.pumpAndSettle();

      expect(find.textContaining('Ani · Rp 2.000.000'), findsOneWidget);
      expect(api.filterLaporan, isEmpty);
      // Tolak tanpa alasan ditahan di dialog.
      await t.tap(find.widgetWithText(OutlinedButton, 'Tolak'));
      await t.pumpAndSettle();
      await t.tap(find.widgetWithText(FilledButton, 'Tolak'));
      await t.pumpAndSettle();
      expect(find.text('Alasan penolakan wajib diisi'), findsOneWidget);
      expect(api.dikirim, isEmpty);
      await t.tap(find.text('Batal'));
      await t.pumpAndSettle();

      await t.tap(find.widgetWithText(FilledButton, 'Verifikasi'));
      await t.pumpAndSettle();
      await t.enterText(find.byType(TextField).last, 'dokumen lengkap');
      await t.tap(find.widgetWithText(FilledButton, 'Verifikasi').last);
      await t.pumpAndSettle();
      expect(api.dikirim, ['7/verifikasi/dokumen lengkap']);

      await t.tap(find.text('Laporan'));
      await t.pumpAndSettle();
      expect(
        find.text('1 pinjaman aktif · total Rp 3.000.000'),
        findsOneWidget,
      );
      expect(find.textContaining('Budi · sisa Rp 2.500.001'), findsOneWidget);
      await t.enterText(
        find.widgetWithText(TextField, 'Cari nama / NIK'),
        '  N2  ',
      );
      await t.tap(find.byTooltip('Cari'));
      await t.pumpAndSettle();
      expect(api.filterLaporan, [null, 'N2']);
    },
  );
}
