import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'core/providers.dart';
import 'core/tema_mode.dart';
import 'core/theme.dart';
import 'core/update.dart';
import 'features/admin/admin_dashboard_page.dart';
import 'features/admin/admin_katalog_page.dart';
import 'features/admin/admin_page.dart';
import 'features/admin/admin_rekap_page.dart';
import 'features/anggota/anggota_shell.dart';
import 'features/anggota/transaksi_saya_page.dart';
import 'features/auth/login_page.dart';
import 'features/kasir/dashboard_page.dart';
import 'features/kasir/kasir_page.dart';

void main() => runApp(const ProviderScope(child: KopkarApp()));

/// Tiap role hanya boleh berada di bagian aplikasinya sendiri. ANGGOTA langsung ke "Transaksi Saya".
String? redirectUntuk(AuthState a, String loc) {
  if (a.memulihkan) return loc == '/mulai' ? null : '/mulai';
  final p = a.profil;
  if (p == null) return loc == '/login' ? null : '/login';
  if (p.gantiPassword) return loc == '/ganti' ? null : '/ganti';
  final home = switch (p.role) {
    'KASIR' => '/kasir',
    'ANGGOTA' => '/saya',
    _ => '/admin',
  };
  if (loc == '/login' || loc == '/mulai' || loc == '/ganti') return home;
  if (!loc.startsWith(home)) return home;
  return null;
}

final _navKey = GlobalKey<NavigatorState>();

final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(authProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);
  return GoRouter(
    navigatorKey: _navKey,
    initialLocation: '/mulai',
    refreshListenable: refresh,
    redirect: (ctx, st) =>
        redirectUntuk(ref.read(authProvider), st.matchedLocation),
    routes: [
      GoRoute(
        path: '/mulai',
        builder: (_, _) =>
            const Scaffold(body: Center(child: CircularProgressIndicator())),
      ),
      GoRoute(path: '/login', builder: (_, _) => const LoginPage()),
      GoRoute(path: '/ganti', builder: (_, _) => const GantiPasswordPage()),
      GoRoute(
        path: '/kasir',
        builder: (_, _) => const KasirShell(jual: KasirPage()),
      ),
      GoRoute(
        path: '/saya',
        builder: (_, _) => const AnggotaShell(),
        routes: [
          GoRoute(
            path: 'faktur/:no',
            builder: (_, s) => DetailTransaksiPage(no: s.pathParameters['no']!),
          ),
          GoRoute(
            path: 'kartu-anggota',
            builder: (_, _) => const KartuAnggotaPage(),
          ),
        ],
      ),
      GoRoute(
        path: '/admin',
        builder: (_, _) =>
            const AdminShell(
              produk: AdminKatalogPage(),
              rekap: AdminRekapPage(),
              user: AdminPage(),
            ),
      ),
    ],
  );
});

class KopkarApp extends ConsumerStatefulWidget {
  const KopkarApp({super.key});
  @override
  ConsumerState<KopkarApp> createState() => _KopkarAppState();
}

class _KopkarAppState extends ConsumerState<KopkarApp>
    with WidgetsBindingObserver {
  bool _sedangCek = false;
  bool _menunggu = false;
  String? _pushPending;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    Future.microtask(() => ref.read(authProvider.notifier).pulihkan());
    final push = ref.read(pushProvider);
    push.onTap = (no) {
      _pushPending = no;
      _bukaNotifikasi();
    };
    Future.microtask(() async {
      await push.initialize();
      if (mounted) await push.sync(ref.read(authProvider).profil);
    });
    // Cek setelah sesi pulih (bukan di layar /mulai), sekali tiap login/pemulihan.
    ref.listenManual(authProvider, (_, a) {
      if (!a.memulihkan) {
        _cekUpdate();
        unawaited(push.sync(a.profil));
        _bukaNotifikasi();
      }
    });
  }

  @override
  void dispose() {
    ref.read(pushProvider).onTap = null;
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState s) {
    if (s == AppLifecycleState.resumed && !ref.read(authProvider).memulihkan) {
      _cekUpdate();
      unawaited(ref.read(pushProvider).sync(ref.read(authProvider).profil));
    }
  }

  void _bukaNotifikasi() {
    final state = ref.read(authProvider);
    final no = _pushPending;
    if (no == null || state.memulihkan || state.profil == null || state.profil!.gantiPassword) return;
    _pushPending = null;
    if (state.profil!.role != 'ANGGOTA') return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) ref.read(routerProvider).go('/saya/faktur/${Uri.encodeComponent(no)}');
    });
  }

  Future<void> _cekUpdate() async {
    final ctx = _navKey.currentContext;
    if (_sedangCek || ctx == null) return;
    // Sedang di layar bayar/scan/struk (halaman di atas shell): jangan potong transaksi, coba lagi sebentar lagi.
    if (_navKey.currentState?.canPop() ?? false) {
      if (!_menunggu) {
        _menunggu = true;
        Future.delayed(const Duration(minutes: 1), () {
          _menunggu = false;
          if (mounted) _cekUpdate();
        });
      }
      return;
    }
    _sedangCek = true;
    try {
      await tawarkanUpdate(ctx);
    } finally {
      _sedangCek = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Kopkar Kusuma Lestari',
      theme: kopkarTheme(Brightness.light),
      darkTheme: kopkarTheme(Brightness.dark),
      themeMode: ref.watch(temaModeProvider),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
