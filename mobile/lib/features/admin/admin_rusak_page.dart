import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'admin_dokumen_editor_page.dart';
import 'admin_opname_page.dart' show fmtQty;

String _qtyTeks(double v) =>
    v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

/// Riwayat dan pencatatan barang rusak (mengurangi stok sistem). Tab di halaman Produk admin.
class AdminRusakPage extends ConsumerStatefulWidget {
  const AdminRusakPage({super.key});

  @override
  ConsumerState<AdminRusakPage> createState() => _AdminRusakPageState();
}

class _AdminRusakPageState extends ConsumerState<AdminRusakPage> {
  final _items = <RusakRingkas>[];
  final int _tahun = DateTime.now().year.clamp(2000, 2100);
  int _page = 1, _requestId = 0;
  bool _loading = true, _lagi = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  Future<void> _muat({bool lanjut = false}) async {
    if (lanjut && _loading) return;
    final id = ++_requestId;
    final page = lanjut ? _page + 1 : 1;
    setState(() {
      _loading = true;
      _error = null;
      if (!lanjut) {
        _items.clear();
        _page = 1;
        _lagi = false;
      }
    });
    try {
      final r = await ref.read(apiProvider).daftarRusak(tahun: _tahun, page: page);
      if (!mounted || id != _requestId) return;
      setState(() {
        _items.addAll(r.items);
        _page = page;
        _lagi = r.lagi;
      });
    } on ApiException catch (e) {
      if (mounted && id == _requestId) setState(() => _error = e.pesan);
    } finally {
      if (mounted && id == _requestId) setState(() => _loading = false);
    }
  }

  Future<void> _baru() async {
    final api = ref.read(apiProvider);
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => AdminDokumenEditorPage(
          judul: 'Barang rusak baru',
          labelQty: 'Jumlah rusak',
          awal: const [],
          denganKeterangan: true,
          perluAlasan: false,
          simpan: (baris, alasan, key) async {
            await api.simpanRusak(
              [
                for (final b in baris)
                  (
                    produkId: b.produkId,
                    qty: b.qtyKirim!,
                    keterangan: b.keterangan.text.trim(),
                  ),
              ],
              idempotencyKey: key,
            );
          },
        ),
      ),
    );
    if (ok == true && mounted) await _muat();
  }

  Future<void> _detail(String nomor) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(builder: (_) => _RusakDetail(nomor)),
    );
    if (mounted) await _muat();
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: Align(
            alignment: Alignment.centerRight,
            child: FilledButton.icon(
              onPressed: _baru,
              icon: const Icon(Icons.add),
              label: const Text('Rusak baru'),
            ),
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _muat,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
              children: [
                Text('Riwayat tahun $_tahun', style: tema.textTheme.titleMedium),
                const SizedBox(height: 8),
                if (!_loading && _error == null && _items.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: Text('Belum ada dokumen barang rusak.'),
                  ),
                for (final s in _items)
                  Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      title: Text(s.nomor, style: tema.textTheme.titleSmall),
                      subtitle: Text(
                        '${s.tanggal} · ${s.jumlahItem} barang\nNilai ${rupiah(s.nilai.round())}',
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => _detail(s.nomor),
                    ),
                  ),
                if (_loading) const LoadingKopkar(),
                if (_error != null)
                  Card(
                    color: tema.colorScheme.errorContainer,
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(
                        children: [
                          Text(_error!),
                          TextButton(
                            onPressed: () => _muat(lanjut: _items.isNotEmpty),
                            child: const Text('Coba lagi'),
                          ),
                        ],
                      ),
                    ),
                  ),
                if (_lagi && !_loading)
                  OutlinedButton(
                    onPressed: () => _muat(lanjut: true),
                    child: const Text('Muat lebih banyak'),
                  ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _RusakDetail extends ConsumerStatefulWidget {
  const _RusakDetail(this.nomor);
  final String nomor;

  @override
  ConsumerState<_RusakDetail> createState() => _RusakDetailState();
}

class _RusakDetailState extends ConsumerState<_RusakDetail> {
  late Future<RusakDokumen> _detail = _muat();
  RusakDokumen? _dokumen;

  Future<RusakDokumen> _muat() async {
    final d = await ref.read(apiProvider).detailRusak(widget.nomor);
    _dokumen = d;
    return d;
  }

  void _pesan(String m) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

  Future<void> _ubah() async {
    final d = _dokumen;
    if (d == null) return;
    if (d.items.any((b) => b.produkId == null)) {
      _pesan('Ada produk yang sudah tidak ada; dokumen tidak bisa diubah.');
      return;
    }
    final api = ref.read(apiProvider);
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => AdminDokumenEditorPage(
          judul: 'Ubah ${d.nomor}',
          labelQty: 'Jumlah rusak (satuan dasar)',
          denganKeterangan: true,
          awal: [
            for (final b in d.items)
              BarisEditor(
                produkId: b.produkId!,
                kode: b.kode,
                nama: b.nama,
                satuan: '',
                qty: _qtyTeks(b.qty),
                keterangan: b.keterangan,
              ),
          ],
          simpan: (baris, alasan, key) async {
            await api.ubahRusak(
              d.nomor,
              versi: d.versi,
              alasan: alasan,
              items: [
                for (final b in baris)
                  (
                    produkId: b.produkId,
                    qty: b.qtyKirim!,
                    keterangan: b.keterangan.text.trim(),
                  ),
              ],
              idempotencyKey: key,
            );
          },
        ),
      ),
    );
    if (ok == true && mounted) setState(() => _detail = _muat());
  }

  Future<void> _hapus() async {
    final d = _dokumen;
    if (d == null) return;
    final alasan = TextEditingController();
    final ya = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: Text('Hapus ${d.nomor}?'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Dokumen dihapus permanen dan stok sistem bertambah kembali.'),
            TextField(
              controller: alasan,
              maxLength: 250,
              decoration: const InputDecoration(labelText: 'Alasan'),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Batal')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Hapus')),
        ],
      ),
    );
    final teks = alasan.text.trim();
    alasan.dispose();
    if (ya != true || !mounted) return;
    if (teks.isEmpty) {
      _pesan('Alasan wajib diisi.');
      return;
    }
    try {
      await ref.read(apiProvider).hapusRusak(
        d.nomor,
        versi: d.versi,
        alasan: teks,
        idempotencyKey: newCheckoutKey(),
      );
      if (mounted) Navigator.of(context).pop();
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(widget.nomor),
      actions: [
        IconButton(
          tooltip: 'Ubah dokumen',
          icon: const Icon(Icons.edit_outlined),
          onPressed: _ubah,
        ),
        IconButton(
          tooltip: 'Hapus dokumen',
          icon: const Icon(Icons.delete_outline),
          onPressed: _hapus,
        ),
      ],
    ),
    body: SafeArea(
      child: FutureBuilder<RusakDokumen>(
        future: _detail,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: LoadingKopkar());
          }
          if (snapshot.hasError) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: Text(snapshot.error.toString()),
            );
          }
          final d = snapshot.requireData;
          final tema = Theme.of(context);
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(
                '${d.tanggal} · ${d.items.length} barang · ${rupiah(d.total.round())}',
                style: tema.textTheme.titleMedium,
              ),
              const SizedBox(height: 12),
              for (final b in d.items)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(b.nama, style: tema.textTheme.titleSmall),
                        Text(b.kode),
                        const SizedBox(height: 8),
                        Text(
                          'Rusak ${fmtQty(b.qty)} · HPP ${rupiah(b.hpp.round())}',
                        ),
                        if (b.keterangan.isNotEmpty) Text(b.keterangan),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    ),
  );
}
