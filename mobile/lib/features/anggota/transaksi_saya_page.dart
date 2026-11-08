import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Layar utama ANGGOTA: transaksi milik sendiri per periode, dengan ringkasan limit di atas.
class TransaksiSayaPage extends ConsumerStatefulWidget {
  const TransaksiSayaPage({super.key});
  @override
  ConsumerState<TransaksiSayaPage> createState() => _TransaksiSayaPageState();
}

class _TransaksiSayaPageState extends ConsumerState<TransaksiSayaPage> {
  late int _periode;
  Ringkasan? _ringkasan;
  final List<TransaksiRingkas> _items = [];
  bool _loading = true, _lagi = false, _memuatLagi = false;
  String? _error;
  int _page = 1;
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    final n = DateTime.now();
    _periode = n.year * 100 + n.month;
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300)
        _muatLagi();
    });
    _muat();
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _muat() async {
    final api = ref.read(apiProvider);
    final periode = _periode;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await Future.wait([
        api.ringkasan(periode),
        api.transaksi(periode),
      ]);
      if (!mounted || periode != _periode) return;
      final t = r[1] as ({List<TransaksiRingkas> items, bool lagi});
      setState(() {
        _ringkasan = r[0] as Ringkasan;
        _items
          ..clear()
          ..addAll(t.items);
        _lagi = t.lagi;
        _page = 1;
      });
    } on ApiException catch (e) {
      if (mounted && periode == _periode) {
        setState(() {
          _error = e.pesan;
          _ringkasan = null;
          _items.clear();
        });
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _muatLagi() async {
    if (!_lagi || _memuatLagi || _loading) return;
    _memuatLagi = true;
    try {
      final t = await ref
          .read(apiProvider)
          .transaksi(_periode, page: _page + 1);
      if (!mounted) return;
      setState(() {
        _items.addAll(t.items);
        _lagi = t.lagi;
        _page++;
      });
    } on ApiException {
      // biarkan; pengguna bisa tarik untuk menyegarkan
    } finally {
      _memuatLagi = false;
    }
  }

  void _geser(int d) {
    setState(() => _periode = geserPeriode(_periode, d));
    _muat();
  }

  @override
  Widget build(BuildContext context) {
    return TropicalScaffold(
      title: 'Transaksi Saya',
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
          controller: _scroll,
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            _PeriodeBar(periode: _periode, onGeser: _geser),
            const SizedBox(height: 12),
            if (_loading && _ringkasan == null)
              const Padding(
                padding: EdgeInsets.all(48),
                child: Center(child: CircularProgressIndicator()),
              ),
            if (_error != null) _ErrorBox(pesan: _error!, onUlang: _muat),
            if (_ringkasan != null) KartuLimit(r: _ringkasan!),
            const SizedBox(height: 16),
            if (_ringkasan != null && _items.isEmpty && !_loading)
              const Padding(
                padding: EdgeInsets.all(32),
                child: Center(
                  child: Text('Belum ada transaksi pada periode ini'),
                ),
              ),
            for (final t in _items)
              Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  minVerticalPadding: 12,
                  title: Text(
                    rupiah(t.total),
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  subtitle: Text(
                    '${t.tanggal} ${t.jam}\n${t.no} · ${t.jenisBayar}${t.tenor > 1 ? ' · ${t.tenor}x angsuran' : ''}',
                  ),
                  isThreeLine: true,
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () =>
                      context.go('/saya/faktur/${Uri.encodeComponent(t.no)}'),
                ),
              ),
            if (_lagi)
              const Padding(
                padding: EdgeInsets.all(16),
                child: Center(child: CircularProgressIndicator()),
              ),
          ],
        ),
      ),
    );
  }
}

class _PeriodeBar extends StatelessWidget {
  const _PeriodeBar({required this.periode, required this.onGeser});
  final int periode;
  final void Function(int) onGeser;
  @override
  Widget build(BuildContext context) => Row(
    children: [
      IconButton(
        tooltip: 'Periode sebelumnya',
        onPressed: () => onGeser(-1),
        icon: const Icon(Icons.chevron_left),
      ),
      Expanded(
        child: Center(
          child: Text(
            namaPeriode(periode),
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ),
      ),
      IconButton(
        tooltip: 'Periode berikutnya',
        onPressed: () => onGeser(1),
        icon: const Icon(Icons.chevron_right),
      ),
    ],
  );
}

class KartuLimit extends StatelessWidget {
  const KartuLimit({super.key, required this.r, this.judul = 'Belanja periode ini'});
  final Ringkasan r;
  final String judul;
  @override
  Widget build(BuildContext context) {
    final tak = r.limit == 0;
    final pakai = tak ? 0.0 : (r.belanja / r.limit).clamp(0.0, 1.0);
    final lewat = !tak && r.belanja > r.limit;
    final cs = Theme.of(context).colorScheme;
    return Card(
      color: cs.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              judul,
              style: Theme.of(context).textTheme.labelLarge,
            ),
            Text(
              rentangTanggal(r.dari, r.sampai),
              style: Theme.of(context).textTheme.bodySmall,
            ),
            Text(
              rupiah(r.belanja),
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: 12),
            if (tak)
              const Text('Limit hutang: tidak dibatasi')
            else ...[
              Semantics(
                label: 'Pemakaian limit ${(pakai * 100).round()} persen',
                child: LinearProgressIndicator(
                  value: pakai,
                  minHeight: 10,
                  borderRadius: BorderRadius.circular(5),
                  color: lewat ? cs.error : null,
                ),
              ),
              const SizedBox(height: 8),
              Text('Limit ${rupiah(r.limit)} · Sisa ${rupiah(r.sisa ?? 0)}'),
            ],
          ],
        ),
      ),
    );
  }
}

class _ErrorBox extends StatelessWidget {
  const _ErrorBox({required this.pesan, required this.onUlang});
  final String pesan;
  final VoidCallback onUlang;
  @override
  Widget build(BuildContext context) => Card(
    color: Theme.of(context).colorScheme.errorContainer,
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Text(pesan),
          TextButton(onPressed: onUlang, child: const Text('Coba lagi')),
        ],
      ),
    ),
  );
}

class DetailTransaksiPage extends ConsumerStatefulWidget {
  const DetailTransaksiPage({super.key, required this.no});
  final String no;
  @override
  ConsumerState<DetailTransaksiPage> createState() =>
      _DetailTransaksiPageState();
}

class _DetailTransaksiPageState extends ConsumerState<DetailTransaksiPage> {
  late Future<Faktur> _f = ref.read(apiProvider).detail(widget.no);

  @override
  Widget build(BuildContext context) {
    return TropicalScaffold(
      title: widget.no,
      leading: IconButton(
        tooltip: 'Kembali',
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        onPressed: () => context.go('/saya'),
      ),
      body: FutureBuilder<Faktur>(
        future: _f,
        builder: (c, s) {
          if (s.connectionState != ConnectionState.done)
            return const Center(child: CircularProgressIndicator());
          if (s.hasError) {
            return Center(
              child: _ErrorBox(
                pesan: s.error is ApiException
                    ? (s.error as ApiException).pesan
                    : 'Gagal memuat',
                onUlang: () => setState(
                  () => _f = ref.read(apiProvider).detail(widget.no),
                ),
              ),
            );
          }
          final f = s.data!;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text('${f.tanggal} ${f.jam}'),
              Text(
                '${f.jenisBayar}${f.tenor > 1 ? ' · ${f.tenor}x angsuran' : ''}',
              ),
              const Divider(height: 24),
              for (final b in f.baris)
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(b.nama),
                  subtitle: Text(
                    '${formatQty(b.qty, b.satuan)} × ${rupiah(b.harga)}${b.potongan > 0 ? ' − potongan ${rupiah(b.potongan)}' : ''}',
                  ),
                  trailing: Text(rupiah(b.total)),
                ),
              const Divider(height: 24),
              _baris('Bruto', rupiah(f.bruto)),
              _baris('Potongan', rupiah(f.potongan)),
              _baris('Total', rupiah(f.total), tebal: true),
              if (f.jadwal.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(
                  'Jadwal angsuran',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                for (final j in f.jadwal)
                  _baris('Ke-${j.ke} · ${j.jatuhTempo}', rupiah(j.angsuran)),
              ],
            ],
          );
        },
      ),
    );
  }

  Widget _baris(String l, String v, {bool tebal = false}) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          l,
          style: tebal ? const TextStyle(fontWeight: FontWeight.bold) : null,
        ),
        Text(
          v,
          style: tebal ? const TextStyle(fontWeight: FontWeight.bold) : null,
        ),
      ],
    ),
  );
}

class KartuAnggotaPage extends ConsumerWidget {
  const KartuAnggotaPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profil = ref.watch(authProvider).profil;
    final nik = profil?.nik;
    return TropicalScaffold(
      title: 'Kartu Anggota',
      leading: IconButton(
        tooltip: 'Kembali',
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        onPressed: () => context.go('/saya'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          Card(
            color: Theme.of(context).colorScheme.primaryContainer,
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  Text(
                    'KOPKAR KUSUMA LESTARI',
                    style: Theme.of(context).textTheme.labelLarge?.copyWith(
                      letterSpacing: 1.4,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ),
                  const SizedBox(height: 14),
                  const ProfilAvatar(radius: 40),
                  const SizedBox(height: 10),
                  Text(
                    profil?.nama ?? 'Anggota',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 4),
                  Text(
                    nik ?? 'NIK tidak tersedia',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 20),
                  if (nik != null && nik.isNotEmpty)
                    MemberQrView(data: nik, size: 232)
                  else
                    const Icon(Icons.qr_code_2, size: 180),
                  const SizedBox(height: 16),
                  const Text(
                    'Tunjukkan QR ini ke kasir untuk memilih akun anggota.',
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            color: Theme.of(context).brightness == Brightness.dark
                ? const Color(0xFF523800)
                : const Color(0xFFFFEDC7),
            child: const Padding(
              padding: EdgeInsets.all(14),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.shield_outlined, color: Color(0xFF9C6200)),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'QR berisi NIK Anda. Jangan bagikan tangkapan layar atau tampilkan kepada orang yang tidak dikenal.',
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
