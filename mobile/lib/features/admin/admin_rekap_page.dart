import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../anggota/transaksi_saya_page.dart'
    show DetailTransaksiPage, KartuLimit;
import '../profil/profil_aksi.dart';

/// Rekap transaksi per anggota (admin) dengan periode sebagai parameter.
class AdminRekapPage extends ConsumerStatefulWidget {
  const AdminRekapPage({super.key});
  @override
  ConsumerState<AdminRekapPage> createState() => _AdminRekapPageState();
}

class _AdminRekapPageState extends ConsumerState<AdminRekapPage> {
  final _q = TextEditingController();
  List<PeriodeInfo> _periodes = const [];
  int? _periode;
  RekapAnggota? _rekap;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _awal();
  }

  @override
  void dispose() {
    _q.dispose();
    super.dispose();
  }

  Future<void> _awal() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref.read(apiProvider).periodeAdmin();
      if (!mounted) return;
      if (r.items.isEmpty) {
        setState(() {
          _error = 'Belum ada periode yang diatur.';
          _loading = false;
        });
        return;
      }
      // Awal: periode hari ini bila ada di daftar, selain itu periode terbaru.
      final awal = r.items.any((p) => p.periode == r.sekarang)
          ? r.sekarang!
          : r.items.first.periode;
      setState(() {
        _periodes = r.items;
        _periode = awal;
      });
      await _muat();
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.pesan;
          _loading = false;
        });
      }
    }
  }

  Future<void> _muat() async {
    final p = _periode;
    if (p == null) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref.read(apiProvider).rekapAnggota(p);
      if (!mounted || p != _periode) return;
      setState(() => _rekap = r);
    } on ApiException catch (e) {
      if (mounted && p == _periode) {
        setState(() {
          _error = e.pesan;
          _rekap = null;
        });
      }
    } finally {
      if (mounted && p == _periode) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final r = _rekap;
    final q = _q.text.trim().toLowerCase();
    final items = r == null
        ? <RekapItem>[]
        : [
            for (final x in r.items)
              if (q.isEmpty || '${x.nama} ${x.nik}'.toLowerCase().contains(q)) x,
          ];
    // Urut jumlah belanja tertinggi; seri: transaksi terbanyak, lalu nama.
    items.sort((a, b) {
      final t = b.total.compareTo(a.total);
      if (t != 0) return t;
      final n = b.jumlah.compareTo(a.jumlah);
      return n != 0 ? n : a.nama.compareTo(b.nama);
    });
    return TropicalScaffold(
      title: 'Admin · Rekap',
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
          _PeriodeNav(
            periodes: _periodes,
            periode: _periode,
            enabled: !_loading,
            onPilih: (p) {
              setState(() => _periode = p);
              _muat();
            },
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: TextField(
              controller: _q,
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(
                labelText: 'Cari nama atau NIK',
                prefixIcon: Icon(Icons.search),
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _muat,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
                children: [
                  if (_loading)
                    const Padding(
                      padding: EdgeInsets.all(40),
                      child: LoadingKopkar(),
                    ),
                  if (_error != null)
                    Card(
                      color: cs.errorContainer,
                      child: ListTile(
                        title: Text(_error!),
                        trailing: TextButton(
                          onPressed: _periode == null ? _awal : _muat,
                          child: const Text('Coba lagi'),
                        ),
                      ),
                    ),
                  if (r != null && !_loading) ...[
                    _Ringkasan(r: r),
                    const SizedBox(height: 8),
                    if (items.isEmpty)
                      Padding(
                        padding: const EdgeInsets.all(32),
                        child: Center(
                          child: Text(
                            r.items.isEmpty
                                ? 'Belum ada transaksi anggota pada periode ini.'
                                : 'Tidak ada anggota yang cocok.',
                          ),
                        ),
                      ),
                    for (final x in items)
                      Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          onTap: () => Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) =>
                                  RekapAnggotaPage(item: x, periode: r.periode),
                            ),
                          ),
                          title: Text(
                            x.nama,
                            style: const TextStyle(fontWeight: FontWeight.w600),
                          ),
                          // Transaksi tunai memakai pelanggan umum; semua anggota dianggap kredit, jadi tanpa rincian tunai/kredit.
                          subtitle: Text(
                            '${x.nik} · ${x.unit}\n${x.jumlah} transaksi',
                          ),
                          trailing: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(
                                rupiah(x.total),
                                style: const TextStyle(fontWeight: FontWeight.w700),
                              ),
                              Text(
                                x.sisaLimit == null
                                    ? 'Limit tak terbatas'
                                    : x.sisaLimit! < 0
                                        ? 'Melebihi limit ${rupiah(-x.sisaLimit!)}'
                                        : 'Sisa ${rupiah(x.sisaLimit!)}',
                                style: Theme.of(context).textTheme.bodySmall
                                    ?.copyWith(
                                      color: (x.sisaLimit ?? 0) < 0 ? cs.error : null,
                                    ),
                              ),
                            ],
                          ),
                        ),
                      ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Navigasi periode sederhana: panah kiri = periode sebelumnya, kanan = berikutnya (hanya periode yang ada).
class _PeriodeNav extends StatelessWidget {
  const _PeriodeNav({
    required this.periodes,
    required this.periode,
    required this.enabled,
    required this.onPilih,
  });
  final List<PeriodeInfo> periodes; // terbaru dulu
  final int? periode;
  final bool enabled;
  final void Function(int) onPilih;

  @override
  Widget build(BuildContext context) {
    final i = periodes.indexWhere((p) => p.periode == periode);
    final lebihLama = i >= 0 && i + 1 < periodes.length ? periodes[i + 1] : null;
    final lebihBaru = i > 0 ? periodes[i - 1] : null;
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
      child: Row(
        children: [
          IconButton(
            tooltip: 'Periode sebelumnya',
            onPressed: enabled && lebihLama != null
                ? () => onPilih(lebihLama.periode)
                : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Expanded(
            child: Center(
              child: Text(
                periode == null ? '' : namaPeriode(periode!),
                style: Theme.of(context).textTheme.titleMedium,
              ),
            ),
          ),
          IconButton(
            tooltip: 'Periode berikutnya',
            onPressed: enabled && lebihBaru != null
                ? () => onPilih(lebihBaru.periode)
                : null,
            icon: const Icon(Icons.chevron_right),
          ),
        ],
      ),
    );
  }
}

class _Ringkasan extends StatelessWidget {
  const _Ringkasan({required this.r});
  final RekapAnggota r;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    Widget kolom(String judul, String isi) => Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(judul, style: t.bodySmall),
          Text(isi, style: t.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
        ],
      ),
    );
    return Card(
      margin: EdgeInsets.zero,
      color: Theme.of(context).colorScheme.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Periode ${namaPeriode(r.periode)}', style: t.labelLarge),
            Text(rentangTanggal(r.dari, r.sampai), style: t.bodySmall),
            const SizedBox(height: 4),
            Text(rupiah(r.total), style: t.headlineSmall),
            Text('Total belanja kredit anggota', style: t.bodySmall),
            const SizedBox(height: 8),
            Row(
              children: [
                kolom('Anggota', '${r.jumlahAnggota}'),
                kolom('Transaksi', '${r.jumlahTransaksi}'),
              ],
            ),

          ],
        ),
      ),
    );
  }
}

/// Transaksi satu anggota pada satu periode (dari rekap), dengan ringkasan limit.
class RekapAnggotaPage extends ConsumerStatefulWidget {
  const RekapAnggotaPage({super.key, required this.item, required this.periode});
  final RekapItem item;
  final int periode;
  @override
  ConsumerState<RekapAnggotaPage> createState() => _RekapAnggotaPageState();
}

class _RekapAnggotaPageState extends ConsumerState<RekapAnggotaPage> {
  final _scroll = ScrollController();
  Ringkasan? _ringkasan;
  final List<TransaksiRingkas> _items = [];
  bool _loading = true, _lagi = false, _memuatLagi = false;
  String? _error;
  int _page = 1;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300) {
        _muatLagi();
      }
    });
    _muat();
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref
          .read(apiProvider)
          .transaksiAnggota(widget.item.nik, widget.periode);
      if (!mounted) return;
      setState(() {
        _ringkasan = r.ringkasan;
        _items
          ..clear()
          ..addAll(r.items);
        _lagi = r.lagi;
        _page = 1;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _muatLagi() async {
    if (!_lagi || _memuatLagi || _loading) return;
    _memuatLagi = true;
    try {
      final r = await ref
          .read(apiProvider)
          .transaksiAnggota(widget.item.nik, widget.periode, page: _page + 1);
      if (!mounted) return;
      setState(() {
        _items.addAll(r.items);
        _lagi = r.lagi;
        _page++;
      });
    } on ApiException {
      // biarkan; tarik untuk menyegarkan
    } finally {
      _memuatLagi = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return TropicalScaffold(
      title: widget.item.nama,
      leading: IconButton(
        tooltip: 'Kembali',
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        onPressed: () => Navigator.of(context).pop(),
      ),
      body: RefreshIndicator(
        onRefresh: _muat,
        child: ListView(
          controller: _scroll,
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            Text('${widget.item.nik} · ${widget.item.unit}'),
            const SizedBox(height: 8),
            if (_loading && _ringkasan == null)
              const Padding(
                padding: EdgeInsets.all(40),
                child: LoadingKopkar(),
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
            if (_ringkasan != null)
              KartuLimit(
                r: _ringkasan!,
                judul: 'Belanja periode ${namaPeriode(_ringkasan!.periode)}',
              ),
            const SizedBox(height: 12),
            if (_ringkasan != null && _items.isEmpty && !_loading)
              const Padding(
                padding: EdgeInsets.all(32),
                child: Center(child: Text('Tidak ada transaksi pada periode ini')),
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
                    '${t.tanggal} ${t.jam}\n${t.no} · ${t.jenisBayar}'
                    '${t.tenor > 1 ? ' · ${t.tenor}x angsuran' : ''}',
                  ),
                  isThreeLine: true,
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => DetailTransaksiPage(no: t.no, admin: true),
                    ),
                  ),
                ),
              ),
            if (_lagi)
              const Padding(
                padding: EdgeInsets.all(16),
                child: LoadingKopkar(),
              ),
          ],
        ),
      ),
    );
  }
}
