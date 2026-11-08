import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api.dart';
import 'cart.dart';
import 'models.dart';

/// Alamat server (HTTPS) diberikan saat build: --dart-define=API_URL=https://...
const apiUrl = String.fromEnvironment('API_URL', defaultValue: 'https://api.example.com');

final tokenStoreProvider = Provider<TokenStore>((_) => const SecureTokenStore());

final apiProvider = Provider<ApiClient>((ref) => ApiClient(
      baseUrl: apiUrl,
      store: ref.watch(tokenStoreProvider),
      onExpired: () => ref.read(authProvider.notifier).sesiBerakhir(),
    ));

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

final authProvider = NotifierProvider<AuthNotifier, AuthState>(AuthNotifier.new);

class CartState {
  const CartState(this.cart, this.v);
  final Cart cart;
  final int v;
}

class CartNotifier extends Notifier<CartState> {
  @override
  CartState build() => CartState(Cart(), 0);

  void _bump() => state = CartState(state.cart, state.v + 1);
  void tambah(Barang b) {
    state.cart.tambah(b);
    _bump();
  }

  void atur(int id, int qty) {
    state.cart.atur(id, qty);
    _bump();
  }

  void kosongkan() {
    state.cart.kosongkan();
    _bump();
  }
}

final cartProvider = NotifierProvider<CartNotifier, CartState>(CartNotifier.new);

/// True bila ada koneksi jaringan. Fase 1 wajib online: tombol Bayar diblokir saat false.
final onlineProvider = StreamProvider<bool>((ref) async* {
  final c = Connectivity();
  bool ok(List<ConnectivityResult> r) => r.isNotEmpty && !r.contains(ConnectivityResult.none);
  yield ok(await c.checkConnectivity());
  yield* c.onConnectivityChanged.map(ok);
});
