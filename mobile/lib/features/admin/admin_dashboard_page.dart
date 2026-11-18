import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/beranda_widgets.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'admin_katalog_page.dart' show adminKatalogTab;

/// Indeks tab di [AdminShell].
const adminTabBeranda = 0, adminTabProduk = 1, adminTabRekap = 2, adminTabUser = 3;

/// Beranda admin: perlu perhatian -> KPI hari ini (dengan pembanding kemarin) -> tren 7 hari -> detail.
/// [aktif] berubah true saat tab dibuka -> data dimuat ulang.
class AdminDashboardPage extends ConsumerStatefulWidget {
  const AdminDashboardPage({super.key, required this.aktif, this.onBuka});
  final bool aktif;

  /// Pindah ke tab shell (lihat `adminTab*`); sub-tab Produk diatur lewat [adminKatalogTab].
  final void Function(int tab)? onBuka;

  @override
  ConsumerState<AdminDashboardPage> createState() => _AdminDashboardPageState();
}

class _AdminDashboardPageState extends ConsumerState<AdminDashboardPage> {
  DashboardAdmin? _data;
  int? _stokHabis; // dimuat terpisah (agregasi berat di server)
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
      final api = ref.read(apiProvider);
      final d = await api.dashboardAdmin();
      if (mounted) setState(() => _data = d);
      // Tidak ditunggu: kartu stok habis muncul sendiri setelah dihitung; gagal = kartu tidak tampil.
      api.stokHabisJumlah().then((n) {
        if (mounted) setState(() => _stokHabis = n);
      }).catchError((_) {});
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _buka(int tab, {int? subTabProduk}) {
    if (subTabProduk != null) adminKatalogTab.value = subTabProduk;
    widget.onBuka?.call(tab);
  }

  List<Widget> _perhatian(DashboardAdmin d) {
    final item = <Widget>[
      if (!d.periodeTerbuka)
        PerhatianKartu(
          tingkat: Tingkat.bahaya,
          judul: 'Periode tidak terbuka',
          pesan: d.periodePesan ?? judulPeriode(d.periodeKode),
        ),
      if (d.userTerkunci > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '${d.userTerkunci} akun terkunci',
          pesan: 'Buka kunci di menu User.',
          onTap: () => _buka(adminTabUser),
        ),
      if (d.hargaRendah > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '${d.hargaRendah} produk harga jual ≤ HPP',
          pesan: 'Periksa dan naikkan harga jual.',
          onTap: () => _buka(adminTabProduk, subTabProduk: 2),
        ),
      if ((_stokHabis ?? 0) > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '$_stokHabis produk stok habis',
          pesan: 'Lakukan opname atau pembelian.',
          onTap: () => _buka(adminTabProduk, subTabProduk: 1),
        ),
    ];
    if (item.isEmpty) {
      return const [
        BannerBeranda(
          tingkat: Tingkat.ok,
          judul: 'Tidak ada yang perlu perhatian saat ini',
        ),
      ];
    }
    return [
      for (final w in item) ...[w, const SizedBox(height: 8)],
    ];
  }

  @override
  Widget build(BuildContext context) {
    final d = _data;
    final profil = ref.watch(authProvider).profil;
    return TropicalScaffold(
      title: 'Beranda',
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
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
          children: [
            if (_loading && d == null)
              const Padding(padding: EdgeInsets.all(40), child: LoadingKopkar()),
            if (_error != null)
              Card(
                color: Theme.of(context).colorScheme.errorContainer,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(_error!),
                      TextButton(onPressed: _muat, child: const Text('Coba lagi')),
                    ],
                  ),
                ),
              ),
            if (d != null) ...[
              BerandaHeader(
                leading: const ProfilAvatar(radius: 24),
                nama: (profil?.nama ?? '').split(' ').first,
                tanggal: d.tanggal,
                chip: ChipPeriode(
                  kode: d.periodeKode,
                  periode: d.periode,
                  dari: d.periodeDari,
                  sampai: d.periodeSampai,
                ),
              ),
              const JudulBagian('Perlu perhatian'),
              ..._perhatian(d),
              const JudulBagian('Hari ini'),
              KpiKartu(
                label: 'Total penjualan (semua kasir)',
                nilai: rupiah(d.total),
                hero: true,
                delta: d.kemarin == null ? null : hitungDelta(d.total, d.kemarin!.total),
              ),
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: KpiKartu(
                      label: 'Transaksi',
                      nilai: '${d.jumlah}',
                      delta: d.kemarin == null ? null : hitungDelta(d.jumlah, d.kemarin!.jumlah),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: KpiKartu(
                      label: 'Rata-rata/transaksi',
                      nilai: rupiah(d.jumlah == 0 ? 0 : (d.total / d.jumlah).round()),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              ProporsiKartu(tunai: d.tunai, kredit: d.kredit),
              if (d.tren.isNotEmpty) ...[
                const JudulBagian('Tren penjualan 7 hari'),
                Card(
                  margin: EdgeInsets.zero,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(10, 14, 10, 12),
                    child: TrenBatang(tren: d.tren),
                  ),
                ),
              ],
              const JudulBagian('Penjualan per kasir'),
              if (d.perKasir.isEmpty)
                const Padding(padding: EdgeInsets.all(20), child: Center(child: Text('Belum ada transaksi hari ini.'))),
              for (final k in d.perKasir)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(
                      k.nama.isEmpty ? k.kasir : '${k.nama} (${k.kasir})',
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Text('${k.jumlah} transaksi'),
                    trailing: Text(rupiah(k.total), style: const TextStyle(fontWeight: FontWeight.w700)),
                  ),
                ),
              const JudulBagian('Barang terlaris hari ini'),
              if (d.terlaris.isEmpty)
                const Padding(padding: EdgeInsets.all(20), child: Center(child: Text('Belum ada penjualan hari ini.'))),
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
                    subtitle: Text('${t.kode} · terjual ${formatQty(t.qty, '')}'),
                    trailing: Text(rupiah(t.total), style: const TextStyle(fontWeight: FontWeight.w700)),
                  ),
                ),
              const JudulBagian('User mobile'),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  StatePill(text: 'Admin ${d.userAdmin}'),
                  StatePill(text: 'Kasir ${d.userKasir}'),
                  StatePill(text: 'Anggota ${d.userAnggota}'),
                  StatePill(text: 'Nonaktif ${d.userNonaktif}', color: const Color(0xFF78817B)),
                  StatePill(text: 'Terkunci ${d.userTerkunci}', color: const Color(0xFFD68A00)),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Shell admin: tab Beranda (awal), Produk (Produk | Stok habis | Harga ≤ HPP | Opname | Kategori), Rekap anggota, dan User.
class AdminShell extends StatefulWidget {
  const AdminShell({
    super.key,
    required this.produk,
    required this.rekap,
    required this.user,
  });
  final Widget produk;
  final Widget rekap;
  final Widget user;

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _i = adminTabBeranda;

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
          AdminDashboardPage(
            aktif: _i == adminTabBeranda,
            onBuka: (tab) => setState(() => _i = tab),
          ),
          widget.produk,
          widget.rekap,
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
                label: 'Beranda',
              ),
              NavigationDestination(
                icon: Icon(Icons.inventory_2_outlined),
                selectedIcon: Icon(Icons.inventory_2),
                label: 'Produk',
              ),
              NavigationDestination(
                icon: Icon(Icons.summarize_outlined),
                selectedIcon: Icon(Icons.summarize),
                label: 'Rekap',
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
