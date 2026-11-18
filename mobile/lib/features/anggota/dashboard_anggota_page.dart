import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/beranda_widgets.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Beranda anggota: sisa limit periode berjalan (hero) -> aksi cepat -> transaksi terakhir.
class DashboardAnggotaPage extends ConsumerStatefulWidget {
  const DashboardAnggotaPage({
    super.key,
    required this.aktif,
    required this.onBelanja,
    this.onRiwayat,
  });
  final bool aktif;
  final VoidCallback onBelanja;
  final VoidCallback? onRiwayat;

  @override
  ConsumerState<DashboardAnggotaPage> createState() => _DashboardAnggotaState();
}

class _DashboardAnggotaState extends ConsumerState<DashboardAnggotaPage> {
  DashboardAnggota? _d;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void didUpdateWidget(DashboardAnggotaPage old) {
    super.didUpdateWidget(old);
    if (widget.aktif && !old.aktif) _muat(); // segarkan setelah belanja
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await ref.read(apiProvider).dashboardSaya();
      if (mounted) setState(() => _d = d);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final d = _d;
    final cs = Theme.of(context).colorScheme;
    final profil = ref.watch(authProvider).profil;
    final r = d?.ringkasan;
    return TropicalScaffold(
      title: 'Beranda',
      actions: [
        IconButton(
          tooltip: 'Kartu Anggota',
          icon: const Icon(Icons.qr_code_2, color: Colors.white),
          onPressed: () => context.go('/saya/kartu-anggota'),
        ),
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
              const Padding(padding: EdgeInsets.all(48), child: LoadingKopkar()),
            if (_error != null)
              Card(
                color: cs.errorContainer,
                child: ListTile(
                  title: Text(_error!),
                  trailing: TextButton(onPressed: _muat, child: const Text('Coba lagi')),
                ),
              ),
            if (d != null) ...[
              BerandaHeader(
                leading: const ProfilAvatar(radius: 24),
                nama: (r?.nama ?? profil?.nama ?? '').split(' ').first,
                tanggal: DateTime.now().toIso8601String().substring(0, 10),
                chip: r == null
                    ? null
                    : ChipPeriode(
                        kode: d.periodeTerbuka ? null : 'PERIODE_TUTUP',
                        periode: r.periode,
                        dari: r.dari,
                        sampai: r.sampai,
                      ),
              ),
              const SizedBox(height: 12),
              if (d.belanjaDemo) ...[
                const BannerBeranda(
                  tingkat: Tingkat.peringatan,
                  judul: 'Mode demo',
                  pesan: 'Belanja mandiri hanya simulasi dan tidak tersimpan.',
                ),
                const SizedBox(height: 12),
              ],
              if (r != null)
                LimitHero(r: r)
              else
                const BannerBeranda(
                  tingkat: Tingkat.peringatan,
                  judul: 'Data limit belum tersedia',
                  pesan: 'Tanggal hari ini di luar periode yang diatur.',
                ),
              if (!d.periodeTerbuka) ...[
                const SizedBox(height: 12),
                BannerBeranda(
                  tingkat: Tingkat.bahaya,
                  judul: 'Belanja belum bisa dilakukan',
                  pesan: d.pesan ?? 'Periode belum dibuka',
                  catatan: hubungiAdminPeriode,
                ),
              ],
              const SizedBox(height: 12),
              AksiUtama(
                label: 'Belanja mandiri',
                icon: Icons.shopping_cart_outlined,
                onPressed: d.periodeTerbuka ? widget.onBelanja : null,
              ),
              const SizedBox(height: 8),
              AksiUtama(
                label: 'Kartu anggota',
                icon: Icons.qr_code_2,
                sekunder: true,
                onPressed: () => context.go('/saya/kartu-anggota'),
              ),
              JudulBagian(
                'Transaksi terakhir',
                aksi: widget.onRiwayat == null
                    ? null
                    : TextButton(onPressed: widget.onRiwayat, child: const Text('Lihat semua')),
              ),
              if (d.terakhir.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(child: Text('Belum ada transaksi pada periode ini')),
                ),
              for (final t in d.terakhir)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(rupiah(t.total), style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text(
                      '${t.tanggal} ${t.jam}\n${t.no}${t.tenor > 1 ? ' · ${t.tenor}x angsuran' : ''}',
                    ),
                    isThreeLine: true,
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.go('/saya/faktur/${Uri.encodeComponent(t.no)}'),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}
