import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Ringkasan hari ini semua kasir, per kasir, barang terlaris, dan jumlah user mobile.
/// [aktif] berubah true saat tab dibuka -> data dimuat ulang.
class AdminDashboardPage extends ConsumerStatefulWidget {
  const AdminDashboardPage({super.key, required this.aktif});
  final bool aktif;

  @override
  ConsumerState<AdminDashboardPage> createState() => _AdminDashboardPageState();
}

class _AdminDashboardPageState extends ConsumerState<AdminDashboardPage> {
  DashboardAdmin? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void didUpdateWidget(covariant AdminDashboardPage old) {
    super.didUpdateWidget(old);
    if (widget.aktif && !old.aktif) _muat();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await ref.read(apiProvider).dashboardAdmin();
      if (mounted) setState(() => _data = d);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Widget _kartu(String label, String nilai, {IconData? icon}) => Card(
    margin: EdgeInsets.zero,
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (icon != null) ...[
                Icon(icon, size: 18),
                const SizedBox(width: 6),
              ],
              Flexible(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          SizedBox(
            height: 30,
            child: FittedBox(
              fit: BoxFit.scaleDown,
              alignment: Alignment.centerLeft,
              child: Text(
                nilai,
                style: Theme.of(context).textTheme.titleLarge
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
            ),
          ),
        ],
      ),
    ),
  );

  Widget _judul(String t) => Padding(
    padding: const EdgeInsets.only(top: 20, bottom: 8),
    child: Text(t, style: Theme.of(context).textTheme.titleMedium),
  );

  Widget _kosong(String t) => Padding(
    padding: const EdgeInsets.all(20),
    child: Center(child: Text(t)),
  );

  @override
  Widget build(BuildContext context) {
    final d = _data;
    return TropicalScaffold(
      title: 'Dashboard Admin',
      actions: [
        const ProfilAksi(),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      body: RefreshIndicator(
        onRefresh: _muat,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
          children: [
            const SapaanProfil(),
            if (_loading && d == null)
              const Padding(
                padding: EdgeInsets.all(40),
                child: Center(child: CircularProgressIndicator()),
              ),
            if (_error != null)
              Card(
                color: Theme.of(context).colorScheme.errorContainer,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(_error!),
                      TextButton(
                        onPressed: _muat,
                        child: const Text('Coba lagi'),
                      ),
                    ],
                  ),
                ),
              ),
            if (d != null) ...[
              StatusPeriodeCard(
                kode: d.periodeKode,
                pesan: d.periodePesan,
                tanggal: d.tanggal,
                periode: d.periode,
                dari: d.periodeDari,
                sampai: d.periodeSampai,
              ),
              const SizedBox(height: 12),
              _kartu(
                'Total penjualan hari ini (semua kasir)',
                rupiah(d.total),
                icon: Icons.payments_outlined,
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(child: _kartu('Transaksi', '${d.jumlah}')),
                  const SizedBox(width: 8),
                  Expanded(child: _kartu('Tunai', rupiah(d.tunai))),
                  const SizedBox(width: 8),
                  Expanded(child: _kartu('Kredit', rupiah(d.kredit))),
                ],
              ),
              _judul('Penjualan per kasir'),
              if (d.perKasir.isEmpty) _kosong('Belum ada transaksi hari ini.'),
              for (final k in d.perKasir)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(
                      k.nama.isEmpty ? k.kasir : '${k.nama} (${k.kasir})',
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Text('${k.jumlah} transaksi'),
                    trailing: Text(
                      rupiah(k.total),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
              _judul('Barang terlaris hari ini'),
              if (d.terlaris.isEmpty) _kosong('Belum ada penjualan hari ini.'),
              for (final t in d.terlaris)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(
                      t.nama.isEmpty ? t.kode : t.nama,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Text(
                      '${t.kode} · terjual ${formatQty(t.qty, '')}',
                    ),
                    trailing: Text(
                      rupiah(t.total),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
              _judul('User mobile'),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  StatePill(text: 'Admin ${d.userAdmin}'),
                  StatePill(text: 'Kasir ${d.userKasir}'),
                  StatePill(text: 'Anggota ${d.userAnggota}'),
                  StatePill(
                    text: 'Nonaktif ${d.userNonaktif}',
                    color: const Color(0xFF78817B),
                  ),
                  StatePill(
                    text: 'Terkunci ${d.userTerkunci}',
                    color: const Color(0xFFD68A00),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Shell admin: tab Dashboard (awal), Produk, dan User.
class AdminShell extends StatefulWidget {
  const AdminShell({
    super.key,
    required this.produk,
    required this.kategori,
    required this.user,
  });
  final Widget produk;
  final Widget kategori;
  final Widget user;

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _i = 0;

  @override
  Widget build(BuildContext context) => Scaffold(
    // Keyboard ditangani Scaffold halaman anak (jangan dihitung dua kali).
    resizeToAvoidBottomInset: false,
    // Halaman anak adalah Scaffold sendiri; inset bawah sudah ditangani NavigationBar di sini.
    body: MediaQuery.removePadding(
      context: context,
      removeBottom: true,
      child: IndexedStack(
        index: _i,
        children: [
          AdminDashboardPage(aktif: _i == 0),
          widget.produk,
          widget.kategori,
          widget.user,
        ],
      ),
    ),
    // Disembunyikan saat keyboard terbuka agar layar tidak overflow di viewport sempit.
    bottomNavigationBar: MediaQuery.viewInsetsOf(context).bottom > 0
        ? null
        : NavigationBar(
            selectedIndex: _i,
            onDestinationSelected: (v) => setState(() => _i = v),
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: 'Dashboard',
              ),
              NavigationDestination(
                icon: Icon(Icons.inventory_2_outlined),
                selectedIcon: Icon(Icons.inventory_2),
                label: 'Produk',
              ),
              NavigationDestination(
                icon: Icon(Icons.category_outlined),
                selectedIcon: Icon(Icons.category),
                label: 'Kategori',
              ),
              NavigationDestination(
                icon: Icon(Icons.group_outlined),
                selectedIcon: Icon(Icons.group),
                label: 'User',
              ),
            ],
          ),
  );
}
