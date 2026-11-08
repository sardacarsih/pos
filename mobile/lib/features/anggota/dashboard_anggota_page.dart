import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'transaksi_saya_page.dart' show KartuLimit;

/// Dashboard anggota: limit periode berjalan (dengan rentang tanggal), transaksi terakhir, aksi cepat.
class DashboardAnggotaPage extends ConsumerStatefulWidget {
  const DashboardAnggotaPage({
    super.key,
    required this.aktif,
    required this.onBelanja,
  });
  final bool aktif;
  final VoidCallback onBelanja;

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
    return TropicalScaffold(
      title: 'Dashboard',
      actions: [
        IconButton(
          tooltip: 'Kartu Anggota',
          icon: const Icon(Icons.qr_code_2, color: Colors.white),
          onPressed: () => context.go('/saya/kartu-anggota'),
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
          padding: const EdgeInsets.all(16),
          children: [
            if (_loading && d == null)
              const Padding(
                padding: EdgeInsets.all(48),
                child: Center(child: CircularProgressIndicator()),
              ),
            if (_error != null)
              Card(
                color: cs.errorContainer,
                child: ListTile(
                  title: Text(_error!),
                  trailing: TextButton(
                    onPressed: _muat,
                    child: const Text('Coba lagi'),
                  ),
                ),
              ),
            if (d != null) ...[
              if (d.ringkasan != null) ...[
                Text(
                  'Halo, ${d.ringkasan!.nama}',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                Text(
                  'Periode ${namaPeriode(d.ringkasan!.periode)} · '
                  '${rentangTanggal(d.ringkasan!.dari, d.ringkasan!.sampai)}',
                ),
                const SizedBox(height: 8),
                KartuLimit(r: d.ringkasan!),
              ],
              if (!d.periodeTerbuka)
                Card(
                  color: cs.errorContainer,
                  child: ListTile(
                    leading: const Icon(Icons.lock_outline),
                    title: Text(d.pesan ?? 'Periode belum dibuka'),
                    subtitle: const Text(hubungiAdminPeriode),
                  ),
                ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: d.periodeTerbuka ? widget.onBelanja : null,
                      icon: const Icon(Icons.shopping_cart_outlined),
                      label: const Text('Belanja Mandiri'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => context.go('/saya/kartu-anggota'),
                      icon: const Icon(Icons.qr_code_2),
                      label: const Text('Kartu Anggota'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Text(
                'Transaksi terakhir',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              if (d.terakhir.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(child: Text('Belum ada transaksi pada periode ini')),
                ),
              for (final t in d.terakhir)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(
                      rupiah(t.total),
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Text(
                      '${t.tanggal} ${t.jam}\n${t.no}${t.tenor > 1 ? ' · ${t.tenor}x angsuran' : ''}',
                    ),
                    isThreeLine: true,
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.go(
                      '/saya/faktur/${Uri.encodeComponent(t.no)}',
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
