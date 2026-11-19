import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'admin_opname_baru_page.dart';

String fmtQty(double v) =>
    v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

/// Riwayat dokumen stock opname (dari desktop, mobile, dan web) + mulai opname baru.
class AdminOpnamePage extends ConsumerStatefulWidget {
  const AdminOpnamePage({super.key});

  @override
  ConsumerState<AdminOpnamePage> createState() => _AdminOpnamePageState();
}

class _AdminOpnamePageState extends ConsumerState<AdminOpnamePage> {
  final _scroll = ScrollController();
  final List<SoRingkas> _items = [];
  int _tahun = DateTime.now().year;
  bool _loading = false;
  bool _lagi = false;
  String? _error;
  int _page = 1;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_lagi &&
          !_loading &&
          _error == null &&
          _scroll.position.pixels > _scroll.position.maxScrollExtent - 300) {
        _muat(reset: false);
      }
    });
    _muat(reset: true);
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  void _pesan(String m) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

  Future<void> _muat({required bool reset}) async {
    final requestId = ++_requestId;
    final page = reset ? 1 : _page + 1;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) {
        _items.clear();
        _lagi = false;
      }
    });
    try {
      final r = await ref
          .read(apiProvider)
          .daftarSo(tahun: _tahun, page: page);
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _items.addAll(r.items);
        _page = page;
        _lagi = r.lagi;
      });
    } on ApiException catch (e) {
      if (mounted && requestId == _requestId) setState(() => _error = e.pesan);
    } finally {
      if (mounted && requestId == _requestId) setState(() => _loading = false);
    }
  }

  void _ganti(int delta) {
    setState(() => _tahun += delta);
    _muat(reset: true);
  }

  Future<void> _baru() async {
    final nomor = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const AdminOpnameBaruPage()),
    );
    if (nomor == null || !mounted) return;
    _pesan('Dokumen $nomor tersimpan');
    _muat(reset: true);
  }

  Future<void> _detail(SoRingkas s) async {
    try {
      final d = await ref.read(apiProvider).detailSo(s.nomor);
      if (!mounted) return;
      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        builder: (c) => DraggableScrollableSheet(
          expand: false,
          initialChildSize: 0.7,
          maxChildSize: 0.95,
          builder: (c, ctrl) => ListView(
            controller: ctrl,
            padding: const EdgeInsets.all(16),
            children: [
              Text(d.nomor, style: Theme.of(c).textTheme.titleMedium),
              Text('${d.tanggal} · ${d.items.length} barang'),
              const SizedBox(height: 8),
              for (final b in d.items)
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(b.nama),
                  subtitle: Text(
                    '${b.kode} · sistem ${fmtQty(b.sistem)} → fisik ${fmtQty(b.fisik)}',
                  ),
                  trailing: Text(
                    '${b.selisih > 0 ? '+' : ''}${fmtQty(b.selisih)}',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: b.selisih < 0
                          ? const Color(0xFFB3261E)
                          : b.selisih > 0
                          ? const Color(0xFF2E7D32)
                          : null,
                    ),
                  ),
                ),
            ],
          ),
        ),
      );
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    }
  }

  @override
  // Dipakai sebagai tab di AdminKatalogPage: header, profil, dan keluar ada di halaman induk.
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: Colors.transparent,
    floatingActionButton: FloatingActionButton.extended(
      onPressed: _baru,
      icon: const Icon(Icons.add),
      label: const Text('Opname baru'),
    ),
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              IconButton(
                tooltip: 'Tahun sebelumnya',
                icon: const Icon(Icons.chevron_left),
                onPressed: _loading ? null : () => _ganti(-1),
              ),
              Text('Tahun $_tahun'),
              IconButton(
                tooltip: 'Tahun berikutnya',
                icon: const Icon(Icons.chevron_right),
                onPressed: _loading ? null : () => _ganti(1),
              ),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => _muat(reset: true),
            child: ListView(
              controller: _scroll,
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 88),
              children: [
                if (_error != null)
                  Card(
                    color: Theme.of(context).colorScheme.errorContainer,
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: [
                          Text(_error!),
                          TextButton(
                            onPressed: () => _muat(reset: true),
                            child: const Text('Coba lagi'),
                          ),
                        ],
                      ),
                    ),
                  ),
                if (!_loading && _error == null && _items.isEmpty)
                  const Padding(
                    padding: EdgeInsets.all(36),
                    child: Center(child: Text('Belum ada dokumen SO tahun ini.')),
                  ),
                for (final s in _items)
                  Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      onTap: () => _detail(s),
                      title: Text(
                        s.nomor,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      subtitle: Text('${s.tanggal} · ${s.jumlahItem} barang'),
                      trailing: Text(
                        rupiah(s.nilaiSelisih.round()),
                        style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: s.nilaiSelisih < 0
                              ? const Color(0xFFB3261E)
                              : null,
                        ),
                      ),
                    ),
                  ),
                if (_loading)
                  const Padding(
                    padding: EdgeInsets.all(24),
                    child: LoadingKopkar(),
                  ),
              ],
            ),
          ),
        ),
      ],
    ),
  );
}
