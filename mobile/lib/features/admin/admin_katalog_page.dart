import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../profil/profil_aksi.dart';
import 'admin_kategori_page.dart';
import 'admin_produk_page.dart';

/// Halaman Produk admin dengan dua tab: Produk (kelola produk) dan Kategori (kategori tampil di kasir).
class AdminKatalogPage extends ConsumerStatefulWidget {
  const AdminKatalogPage({super.key});
  @override
  ConsumerState<AdminKatalogPage> createState() => _AdminKatalogPageState();
}

class _AdminKatalogPageState extends ConsumerState<AdminKatalogPage>
    with SingleTickerProviderStateMixin {
  late final TabController _tab = TabController(length: 2, vsync: this)
    ..addListener(() {
      if (!_tab.indexIsChanging) setState(() {});
    });

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => TropicalScaffold(
    title: 'Admin · Produk',
    actions: [
      const ProfilAksi(),
      IconButton(
        tooltip: 'Keluar',
        icon: const Icon(Icons.logout, color: Colors.white),
        onPressed: () => ref.read(authProvider.notifier).logout(),
      ),
    ],
    body: Column(
      children: [
        TabBar(
          controller: _tab,
          tabs: const [Tab(text: 'Produk'), Tab(text: 'Kategori')],
        ),
        // IndexedStack: kedua tab tetap hidup (pencarian/filter dan perubahan kategori yang belum disimpan tidak hilang).
        Expanded(
          child: IndexedStack(
            index: _tab.index,
            children: const [AdminProdukPage(), AdminKategoriPage()],
          ),
        ),
      ],
    ),
  );
}
