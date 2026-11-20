import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/beranda_widgets.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';

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
  bool _loading = false;

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
    if (_loading) return;
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
    final profil = ref.watch(authProvider).profil;
    final r = d?.ringkasan;
    return BerandaScaffold(
      actions: [
        const ProfilAksi(warnaTema: null),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      onRefresh: _muat,
      memuat: _loading,
      adaData: d != null,
      galat: _error,
      children: [
        if (d != null) ...[
          BerandaHeader(
            nama: (r?.nama ?? profil?.nama)?.trim().split(' ').first,
            tanggal: isoTanggal(DateTime.now()),
            chip: r == null
                ? null
                : ChipPeriode(kode: d.periodeTerbuka ? null : 'PERIODE_TUTUP'),
          ),
          const SizedBox(height: 12),
          if (r != null)
            LimitHero(r: r)
          else
            const BannerBeranda(
              tingkat: Tingkat.peringatan,
              judul: 'Data limit belum tersedia',
              pesan: 'Tanggal hari ini di luar periode yang diatur.',
            ),
          const SizedBox(height: 12),
          if (d.belanjaDemo) ...[
            const BannerBeranda(
              tingkat: Tingkat.peringatan,
              judul: 'Mode demo',
              pesan: 'Belanja mandiri hanya simulasi dan tidak tersimpan.',
            ),
            const SizedBox(height: 8),
          ],
          if (!d.periodeTerbuka) ...[
            BannerBeranda(
              tingkat: Tingkat.bahaya,
              judul: 'Belanja belum bisa dilakukan',
              pesan: d.pesan ?? 'Periode belum dibuka',
              catatan: hubungiAdminPeriode,
            ),
            const SizedBox(height: 8),
          ],
          AksiUtama(
            label: 'Belanja mandiri',
            icon: Icons.shopping_cart_outlined,
            onPressed: d.periodeTerbuka ? widget.onBelanja : null,
          ),
          const SizedBox(height: 8),
        ],
        AksiUtama(
          label: 'Kartu anggota',
          icon: Icons.qr_code_2,
          sekunder: true,
          onPressed: () => context.go('/saya/kartu-anggota'),
        ),
        if (d != null) ...[
          JudulBagian(
            'Transaksi terakhir',
            aksi: widget.onRiwayat == null
                ? null
                : TextButton(
                    onPressed: widget.onRiwayat,
                    child: const Text('Lihat semua'),
                  ),
          ),
          DaftarBeranda(
            kosong: 'Belum ada transaksi pada periode ini.',
            baris: [
              for (final t in d.terakhir)
                BarisBeranda(
                  judul: rupiah(t.total),
                  detail:
                      '${t.tanggal} ${t.jam} · ${t.no}${t.tenor > 1 ? ' · ${t.tenor}x angsuran' : ''}',
                  onTap: () =>
                      context.go('/saya/faktur/${Uri.encodeComponent(t.no)}'),
                ),
            ],
          ),
        ],
      ],
    );
  }
}
