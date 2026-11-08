import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'core/providers.dart';
import 'core/theme.dart';
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

final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(authProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);
  return GoRouter(
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

class _KopkarAppState extends ConsumerState<KopkarApp> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(authProvider.notifier).pulihkan());
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Kopkar Kusuma Lestari',
      theme: kopkarTheme(Brightness.light),
      darkTheme: kopkarTheme(Brightness.dark),
      themeMode: ThemeMode.system,
      routerConfig: ref.watch(routerProvider),
    );
  }
}
