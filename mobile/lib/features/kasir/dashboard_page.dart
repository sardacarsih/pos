import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../profil/profil_page.dart';
import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'riwayat_page.dart';

/// Ringkasan hari ini milik kasir. [aktif] berubah true saat tab dibuka -> data dimuat ulang.
class DashboardPage extends ConsumerStatefulWidget {
  const DashboardPage({super.key, required this.aktif, required this.onJual});
  final bool aktif;
  final VoidCallback onJual;

  @override
  ConsumerState<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends ConsumerState<DashboardPage> {
  DashboardKasir? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void didUpdateWidget(covariant DashboardPage old) {
    super.didUpdateWidget(old);
    if (widget.aktif && !old.aktif) _muat();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await ref.read(apiProvider).dashboardKasir();
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

  @override
  Widget build(BuildContext context) {
    final d = _data;
    final profil = ref.watch(authProvider).profil;
    return TropicalScaffold(
      title: 'Dashboard',
      actions: [
        IconButton(
          tooltip: 'Profil saya',
          icon: const ProfilAvatar(radius: 14),
          onPressed: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const ProfilPage()),
          ),
        ),
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
            if (profil != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(
                  'Halo, ${profil.nama}',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
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
              ),
              const SizedBox(height: 12),
              _kartu(
                'Total penjualan hari ini',
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
              const SizedBox(height: 16),
              GradientActionButton(
                label: 'Mulai jual',
                icon: Icons.point_of_sale,
                onPressed: d.periodeTerbuka ? widget.onJual : null,
              ),
              const SizedBox(height: 20),
              Text(
                'Transaksi terakhir',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              if (d.terakhir.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(child: Text('Belum ada transaksi hari ini.')),
                ),
              for (final t in d.terakhir)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    onTap: () => bukaDetailTransaksi(context, ref, t.no),
                    title: Text(
                      t.pelanggan.isEmpty ? t.no : t.pelanggan,
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Wrap(
                        spacing: 8,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        children: [
                          StatePill(text: t.jenisBayar),
                          Text('${t.jam} · ${t.no}'),
                        ],
                      ),
                    ),
                    trailing: Text(
                      rupiah(t.total),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Shell kasir: tab Dashboard (awal) dan Jual (layar Pilih BBM).
class KasirShell extends ConsumerStatefulWidget {
  const KasirShell({super.key, required this.jual});
  final Widget jual;

  @override
  ConsumerState<KasirShell> createState() => _KasirShellState();
}

class _KasirShellState extends ConsumerState<KasirShell> {
  int _i = 0;

  @override
  Widget build(BuildContext context) => Scaffold(
    // Keyboard ditangani Scaffold halaman anak; tanpa ini inset keyboard terpotong dua kali
    // dan daftar produk tinggal beberapa piksel saat mencari.
    resizeToAvoidBottomInset: false,
    // Halaman anak adalah Scaffold sendiri; inset bawah sudah ditangani NavigationBar di sini.
    body: MediaQuery.removePadding(
      context: context,
      removeBottom: true,
      child: IndexedStack(
        index: _i,
        children: [
          DashboardPage(
            aktif: _i == 0,
            onJual: () {
              ref.invalidate(periodeProvider);
              setState(() => _i = 1);
            },
          ),
          widget.jual,
          RiwayatPage(aktif: _i == 2),
        ],
      ),
    ),
    // Disembunyikan saat keyboard terbuka: tanpa ini layar jual overflow di viewport sempit.
    bottomNavigationBar: MediaQuery.viewInsetsOf(context).bottom > 0
        ? null
        : NavigationBar(
            selectedIndex: _i,
            onDestinationSelected: (v) {
              ref.invalidate(periodeProvider);
              setState(() => _i = v);
            },
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: 'Dashboard',
              ),
              NavigationDestination(
                icon: Icon(Icons.point_of_sale_outlined),
                selectedIcon: Icon(Icons.point_of_sale),
                label: 'Jual',
              ),
              NavigationDestination(
                icon: Icon(Icons.receipt_long_outlined),
                selectedIcon: Icon(Icons.receipt_long),
                label: 'Riwayat',
              ),
            ],
          ),
  );
}
