import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

const _kunciTema = 'tema_mode';

/// Pilihan tema pengguna; default ikut sistem sampai tombol tema diketuk.
class TemaModeNotifier extends Notifier<ThemeMode> {
  @override
  ThemeMode build() {
    _muat();
    return ThemeMode.system;
  }

  Future<void> _muat() async {
    try {
      final v = (await SharedPreferences.getInstance()).getString(_kunciTema);
      final mode = ThemeMode.values.where((m) => m.name == v);
      if (mode.isNotEmpty) state = mode.first;
    } catch (_) {} // tanpa penyimpanan, tema tetap ikut sistem
  }

  /// Terang↔gelap berdasarkan tema yang sedang tampil, lalu disimpan.
  Future<void> ganti(Brightness tampil) async {
    state = tampil == Brightness.dark ? ThemeMode.light : ThemeMode.dark;
    try {
      await (await SharedPreferences.getInstance()).setString(_kunciTema, state.name);
    } catch (_) {}
  }
}

final temaModeProvider = NotifierProvider<TemaModeNotifier, ThemeMode>(
  TemaModeNotifier.new,
);

/// Tombol ikon matahari/bulan untuk app bar dan layar login.
class TemaTombol extends ConsumerWidget {
  const TemaTombol({super.key, this.warna});
  final Color? warna;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final gelap = Theme.of(context).brightness == Brightness.dark;
    return IconButton(
      tooltip: gelap ? 'Mode terang' : 'Mode gelap',
      icon: Icon(
        gelap ? Icons.light_mode_outlined : Icons.dark_mode_outlined,
        color: warna,
      ),
      onPressed: () => ref
          .read(temaModeProvider.notifier)
          .ganti(Theme.of(context).brightness),
    );
  }
}
