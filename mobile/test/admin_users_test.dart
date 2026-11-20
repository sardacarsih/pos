import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/features/admin/admin_page.dart';
import 'package:kopkar_pos/features/auth/login_page.dart';
import 'package:kopkar_pos/main.dart' show redirectUntuk;

class _Store implements TokenStore {
  ({String access, String refresh})? tokens;
  @override
  Future<({String access, String refresh})?> read() async => tokens;
  @override
  Future<void> clear() async => tokens = null;
  @override
  Future<void> write(String access, String refresh) async {
    tokens = (access: access, refresh: refresh);
  }

  @override
  Future<String> deviceId() async => 'perangkat-uji';
}

class _Api extends ApiClient {
  _Api() : super(baseUrl: 'http://localhost', store: _Store());
  final accounts = <String, Map<String, dynamic>>{
    'admin': {'id': 1, 'nama': 'Admin Uji', 'role': 'ADMIN', 'aktif': true},
    'kasir': {
      'id': 2,
      'nama': 'Raka',
      'role': 'KASIR',
      'aktif': true,
      'terkunci': true,
    },
    'ami': {
      'id': 3,
      'nama': 'Ami',
      'nik': 'NIK123',
      'role': 'ANGGOTA',
      'aktif': false,
    },
  };
  final passwords = {'admin': 'AdminUji'};
  final pending = <String>{};
  var resets = 0;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? q}) async {
    if (path == '/api/v1/admin/users') {
      return [
        for (final entry in accounts.entries)
          {'username': entry.key, ...entry.value},
      ];
    }
    if (path == '/api/v1/me') {
      final username = (await store.read())!.access;
      return {
        ...accounts[username]!,
        'ganti_password': pending.contains(username),
      };
    }
    if (path == '/api/v1/pelanggan' && q?['belum_akun'] == true) {
      return {
        'items': [
          {'nik': 'NIK456', 'nama': 'Rani', 'unit_nama': 'Gudang'},
        ],
        'ada_lagi': false,
      };
    }
    throw StateError(path);
  }

  @override
  Future<dynamic> post(
    String path, {
    Object? body,
    bool auth = true,
    Map<String, String>? headers,
  }) async {
    final form = body as Map? ?? {};
    if (path == '/api/v1/auth/login') {
      final username = form['username'] as String;
      if (passwords[username] != form['password']) {
        throw ApiException('Login gagal');
      }
      return {
        ...accounts[username]!,
        'ganti_password': pending.contains(username),
        'access_token': username,
        'refresh_token': 'uji',
      };
    }
    if (path == '/api/v1/admin/users/2/reset-password') {
      resets++;
      if (resets == 1) throw ApiException('Koneksi terputus', offline: true);
      accounts['kasir']!['terkunci'] = false;
      pending.add('kasir');
      passwords['kasir'] = 'TEMPUJIX';
      return {'password_sementara': 'TEMPUJIX'};
    }
    if (path == '/api/v1/admin/users') {
      final username = form['username'] as String;
      accounts[username] = {
        'id': 4,
        'nama': form['nama'],
        'role': form['role'],
        'aktif': true,
      };
      pending.add(username);
      passwords[username] = 'TEMPBUAT';
      return {'password_sementara': 'TEMPBUAT'};
    }
    if (path == '/api/v1/auth/ganti-password') {
      final username = (await store.read())!.access;
      if (form['password_lama'] != passwords[username]) {
        throw ApiException('Password lama salah');
      }
      passwords[username] = form['password_baru'];
      pending.remove(username);
      return null;
    }
    throw StateError(path);
  }

  @override
  Future<dynamic> put(String path, {Object? body, Map<String, String>? headers}) async {
    if (path != '/api/v1/admin/users/3/aktif') throw StateError(path);
    accounts['ami']!['aktif'] = (body as Map)['aktif'];
  }
}

void main() {
  for (final skenario in ['reset', 'sistem', 'anggota']) {
    final buat = skenario != 'reset';
    testWidgets(
      buat
          ? 'Buat akun $skenario dan ganti password saat masuk'
          : 'Pulihkan akun terkunci dan ganti password saat masuk',
      (tester) async {
        final api = _Api();
        final scope = ProviderContainer(
          overrides: [
            apiProvider.overrideWithValue(api),
            profilSayaProvider.overrideWith((_) async => {'nama': 'Admin Uji'}),
          ],
        );
        addTearDown(scope.dispose);
        final auth = scope.read(authProvider.notifier);
        await auth.login('admin', 'AdminUji');
        await tester.binding.setSurfaceSize(const Size(390, 844));
        addTearDown(() => tester.binding.setSurfaceSize(null));
        Future<void> buka(Widget home) async {
          await tester.pumpWidget(
            UncontrolledProviderScope(
              container: scope,
              child: MaterialApp(
                theme: buat ? ThemeData.light() : ThemeData.dark(),
                builder: (context, child) => MediaQuery(
                  data: MediaQuery.of(context)
                      .copyWith(textScaler: TextScaler.linear(buat ? 1 : 1.35)),
                  child: child!,
                ),
                home: home,
              ),
            ),
          );
          await tester.pumpAndSettle();
        }

        Future<void> tekan(Finder target) async {
          await tester.ensureVisible(target);
          await tester.pumpAndSettle();
          await tester.tap(target.hitTestable());
          await tester.pumpAndSettle();
        }

        await buka(const AdminPage());
        final username = switch (skenario) {
          'anggota' => 'nik456',
          'sistem' => 'budi',
          _ => 'kasir',
        };
        if (skenario == 'anggota') {
          await tekan(find.text('Anggota (1)'));
          await tekan(find.text('Tambah akun anggota'));
          await tekan(find.text('Rani'));
          await tekan(find.text('Buat'));
        } else if (buat) {
          await tekan(find.text('Tambah pengguna'));
          final fields = find.descendant(
            of: find.byType(AlertDialog),
            matching: find.byType(TextField),
          );
          await tester.enterText(fields.at(0), 'BUDI');
          await tester.enterText(fields.at(1), 'Budi');
          await tester.pump();
          expect(
            tester
                .widget<FilledButton>(find.widgetWithText(FilledButton, 'Buat'))
                .onPressed,
            isNotNull,
          );
          await tekan(find.text('Buat'));
        } else {
          await tekan(find.byTooltip('Tindakan untuk admin'));
          final ownStatus = tester.widget<PopupMenuItem>(
            find.ancestor(
              of: find.text('Nonaktifkan'),
              matching: find.byWidgetPredicate((w) => w is PopupMenuItem),
            ),
          );
          expect(ownStatus.enabled, isFalse);
          await tester.binding.handlePopRoute();
          await tester.pumpAndSettle();
          await tekan(find.text('Anggota (1)'));
          await tester.enterText(find.byType(TextField), 'nik123');
          await tester.pump();
          expect(find.text('@ami'), findsOneWidget);
          await tekan(find.widgetWithText(ChoiceChip, 'Nonaktif'));
          await tekan(find.byTooltip('Tindakan untuk ami'));
          await tekan(find.text('Aktifkan'));
          await tekan(find.widgetWithText(FilledButton, 'Aktifkan'));
          expect(find.text('Tidak ada akun yang cocok'), findsOneWidget);
          expect(api.accounts['ami']!['aktif'], isTrue);
          expect(find.text('Akun ami diaktifkan.'), findsOneWidget);
          await tester.pump(const Duration(seconds: 4));
          await tester.pumpAndSettle();
          await tekan(find.text('Sistem (2)'));
          await tester.enterText(find.byType(TextField), ' KASIR ');
          await tester.pump();
          await tekan(find.text('Terkunci (1)'));
          await tekan(find.text('Reset password'));
          await tekan(find.text('Batal'));
          expect(api.resets, 0);
          await tekan(find.text('Reset password'));
          await tekan(find.widgetWithText(FilledButton, 'Reset password'));
          expect(find.text('Koneksi terputus'), findsOneWidget);
          await tekan(find.widgetWithText(FilledButton, 'Reset password'));
        }
        final temporary = api.passwords[username]!;
        expect(find.text(temporary), findsOneWidget);
        expect(find.text(username), findsOneWidget);
        await tester.binding.handlePopRoute();
        await tester.pumpAndSettle();
        expect(find.text('Sudah dicatat'), findsOneWidget);
        await tekan(find.text('Sudah dicatat'));
        if (!buat) {
          expect(find.text('Tidak ada akun yang cocok'), findsOneWidget);
          expect(find.text('Terkunci (0)'), findsOneWidget);
          expect(
            tester.widget<TextField>(find.byType(TextField)).controller!.text,
            ' KASIR ',
          );
          await tekan(find.text('Hapus filter'));
          expect(find.text('@kasir'), findsOneWidget);
        }

        await auth.login(username, temporary);
        await auth.pulihkan();
        expect(redirectUntuk(scope.read(authProvider), '/kasir'), '/ganti');
        await buka(const GantiPasswordPage());
        final fields = find.byType(TextField);
        await tester.enterText(fields.at(0), 'keliru');
        await tester.enterText(fields.at(1), 'PasswordBaruUji');
        await tester.enterText(fields.at(2), 'PasswordBaruUji');
        await tekan(find.text('Simpan'));
        expect(find.text('Password lama salah'), findsOneWidget);
        await tester.enterText(fields.at(0), temporary);
        await tekan(find.text('Simpan'));
        expect(scope.read(authProvider).profil, isNull);
        await auth.login(username, 'PasswordBaruUji');
        expect(
          redirectUntuk(scope.read(authProvider), '/login'),
          skenario == 'anggota' ? '/saya' : '/kasir',
        );
      },
    );
  }
}
