import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/beranda_widgets.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'riwayat_page.dart';

/// Beranda kasir: status periode -> aksi utama (Mulai jual) -> KPI hari ini -> transaksi terakhir.
/// [aktif] berubah true saat tab dibuka -> data dimuat ulang.
class DashboardPage extends ConsumerStatefulWidget {
  const DashboardPage({super.key, required this.aktif, required this.onJual, this.onRiwayat});
  final bool aktif;
  final VoidCallback onJual;
  final VoidCallback? onRiwayat;

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
              if (!d.periodeTerbuka) ...[
                const SizedBox(height: 12),
                BannerBeranda(
                  tingkat: Tingkat.bahaya,
                  judul: 'Penjualan diblokir',
                  pesan: d.periodePesan,
                  catatan: hubungiAdminPeriode,
                ),
              ],
              const SizedBox(height: 16),
              AksiUtama(
                label: 'Mulai jual',
                icon: Icons.point_of_sale,
                onPressed: d.periodeTerbuka ? widget.onJual : null,
              ),
              const SizedBox(height: 16),
              KpiKartu(label: 'Total penjualan hari ini', nilai: rupiah(d.total), hero: true),
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(child: KpiKartu(label: 'Transaksi', nilai: '${d.jumlah}')),
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
              JudulBagian(
                'Transaksi terakhir',
                aksi: widget.onRiwayat == null
                    ? null
                    : TextButton(onPressed: widget.onRiwayat, child: const Text('Riwayat')),
              ),
              if (d.terakhir.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(
                    child: Text('Belum ada transaksi hari ini. Tekan "Mulai jual" untuk memulai.', textAlign: TextAlign.center),
                  ),
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
                    trailing: Text(rupiah(t.total), style: const TextStyle(fontWeight: FontWeight.w700)),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Shell kasir: tab Beranda (awal), Jual (layar Pilih BBM), dan Riwayat.
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
            onRiwayat: () => setState(() => _i = 2),
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
                label: 'Beranda',
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
