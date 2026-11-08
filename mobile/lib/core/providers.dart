import 'dart:typed_data';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api.dart';
import 'cart.dart';
import 'checkout.dart';
import 'models.dart';

/// Alamat server (HTTPS) diberikan saat build: --dart-define=API_URL=https://...
const apiUrl = String.fromEnvironment(
  'API_URL',
  defaultValue: 'https://api.example.com',
);

final tokenStoreProvider = Provider<TokenStore>(
  (_) => const SecureTokenStore(),
);

final checkoutStoreProvider = Provider<CheckoutStore>(
  (_) => const SecureCheckoutStore(),
);

final apiProvider = Provider<ApiClient>(
  (ref) => ApiClient(
    baseUrl: apiUrl,
    store: ref.watch(tokenStoreProvider),
    onExpired: () => ref.read(authProvider.notifier).sesiBerakhir(),
  ),
);

class AuthState {
  const AuthState({this.profil, this.memulihkan = false});
  final Profil? profil;
  final bool memulihkan;
}

class AuthNotifier extends Notifier<AuthState> {
  @override
  AuthState build() => const AuthState(memulihkan: true);

  Future<void> pulihkan() async {
    try {
      final p = await ref.read(apiProvider).pulihkan();
      state = AuthState(profil: p);
    } on ApiException {
      state = const AuthState(); // offline saat start: minta login ulang
    }
  }

  Future<void> login(String username, String password) async {
    final api = ref.read(apiProvider);
    final p = await api.login(username.trim(), password);
    state = AuthState(profil: await api.me(p));
  }

  Future<void> logout() async {
    ref.read(cartProvider.notifier).kosongkan();
    try {
      await ref.read(apiProvider).logout();
    } on ApiException {
      // token lokal tetap dihapus oleh ApiClient.logout
    }
    state = const AuthState();
  }

  void sesiBerakhir() => state = const AuthState();
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);

class CartState {
  const CartState(this.cart, this.v);
  final Cart cart;
  final int v;
}

class CartNotifier extends Notifier<CartState> {
  @override
  CartState build() => CartState(Cart(), 0);

  void _bump() => state = CartState(state.cart, state.v + 1);
  void tambah(Barang b, {int qty = 1, int? qtyCenti}) {
    state.cart.tambah(b, qty: qty, qtyCenti: qtyCenti);
    _bump();
  }

  void atur(int id, int qty) {
    state.cart.atur(id, qty);
    _bump();
  }

  void aturCenti(int id, int qtyCenti) {
    state.cart.aturCenti(id, qtyCenti);
    _bump();
  }

  void kosongkan() {
    state.cart.kosongkan();
    _bump();
  }
}

final cartProvider = NotifierProvider<CartNotifier, CartState>(
  CartNotifier.new,
);

/// Status periode penjualan hari ini. null = gagal dimuat -> tidak memblokir di UI (server tetap menolak).
/// Periode bermasalah (tutup/belum diatur/tidak valid) memblokir transaksi karena limit hutang bergantung pada periode.
final periodeProvider =
    FutureProvider<({bool terbuka, String? pesan, String? kode})?>((ref) async {
      try {
        final j = await ref.read(apiProvider).periodeStatus();
        return (
          terbuka: j['terbuka'] == true,
          pesan: j['pesan'] as String?,
          kode: j['kode'] as String?,
        );
      } on ApiException {
        return null;
      }
    });

/// True bila ada koneksi jaringan. Fase 1 wajib online: tombol Bayar diblokir saat false.
final onlineProvider = StreamProvider<bool>((ref) async* {
  final c = Connectivity();
  bool ok(List<ConnectivityResult> r) =>
      r.isNotEmpty && !r.contains(ConnectivityResult.none);
  yield ok(await c.checkConnectivity());
  yield* c.onConnectivityChanged.map(ok);
});

/// Foto produk unggahan admin, di-cache selama sesi. Kunci memuat versi: ganti foto = kunci baru = muat ulang.
/// null (gagal/tidak ada) -> tampilan memakai ikon atau foto bawaan.
final fotoProdukProvider =
    FutureProvider.family<Uint8List?, ({int id, int versi})>((ref, k) async {
      try {
        return await ref.read(apiProvider).fotoProduk(k.id);
      } on ApiException {
        return null;
      }
    });

/// Profil akun sendiri (nama, kontak, foto_versi). Di-invalidate setelah simpan/ganti foto; ikut berganti saat user ganti.
final profilSayaProvider = FutureProvider<Map<String, dynamic>>((ref) {
  ref.watch(authProvider.select((a) => a.profil?.id));
  return ref.read(apiProvider).profilSaya();
});

/// Foto profil sendiri; kunci = versi foto (ganti foto = versi baru = unduh ulang, selain itu dari cache sesi).
final fotoProfilProvider = FutureProvider.family<Uint8List?, int>((ref, versi) async {
  try {
    return await ref.read(apiProvider).fotoProfil();
  } on ApiException {
    return null;
  }
});

/// Foto anggota untuk verifikasi di kasir (null = tidak ada foto -> inisial).
final fotoAnggotaProvider = FutureProvider.family<Uint8List?, String>((ref, nik) async {
  try {
    return await ref.read(apiProvider).fotoAnggota(nik);
  } on ApiException {
    return null;
  }
});

/// Foto akun lain untuk daftar admin; kunci memuat versi (ganti foto = versi baru = unduh ulang).
final fotoUserProvider = FutureProvider.family<Uint8List?, ({int id, int versi})>((ref, k) async {
  try {
    return await ref.read(apiProvider).fotoUser(k.id);
  } on ApiException {
    return null;
  }
});
