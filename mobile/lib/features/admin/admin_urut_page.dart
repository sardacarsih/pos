import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Atur urutan produk aktif satu kategori di "Pilih Produk" kasir (seret untuk memindah).
class AdminUrutPage extends ConsumerStatefulWidget {
  const AdminUrutPage({super.key});
  @override
  ConsumerState<AdminUrutPage> createState() => _AdminUrutPageState();
}

class _AdminUrutPageState extends ConsumerState<AdminUrutPage> {
  List<Kategori> _kategori = [];
  String? _kat;
  List<ProdukAdmin> _items = [];
  List<int> _awal = [];
  bool _loading = true, _menyimpan = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muatKategori();
  }

  Future<void> _muatKategori() async {
    try {
      final api = ref.read(apiProvider);
      final semua = await api.semuaKategori();
      final tampil = (await api.kategoriTampil()).toSet();
      final k = tampil.isEmpty
          ? semua
          : [
              for (final x in semua)
                if (tampil.contains(x.id)) x,
            ];
      if (!mounted) return;
      setState(() {
        _kategori = k;
        _kat = k.isEmpty ? null : k.first.id;
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
    if (_kat == null) {
      setState(() => _loading = false);
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final hasil = <ProdukAdmin>[];
      // Server membatasi 30 item per halaman; ambil semua halaman (maks ~1000 produk).
      for (var page = 1; page <= 34; page++) {
        final r = await ref
            .read(apiProvider)
            .produkAdmin(kategori: _kat, aktif: 'Y', page: page);
        hasil.addAll(r.items);
        if (!r.lagi) break;
      }
      if (!mounted) return;
      setState(() {
        _items = hasil;
        _awal = [for (final p in hasil) p.id];
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  bool get _berubah {
    final now = [for (final p in _items) p.id];
    if (now.length != _awal.length) return true;
    for (var i = 0; i < now.length; i++) {
      if (now[i] != _awal[i]) return true;
    }
    return false;
  }

  Future<void> _simpan() async {
    setState(() => _menyimpan = true);
    try {
      final ids = [for (final p in _items) p.id];
      await ref.read(apiProvider).setUrutProduk(ids);
      if (!mounted) return;
      setState(() => _awal = ids);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Urutan disimpan')));
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.pesan)));
      }
    } finally {
      if (mounted) setState(() => _menyimpan = false);
    }
  }

  @override
  Widget build(BuildContext context) => TropicalScaffold(
    title: 'Urutan produk',
    leading: IconButton(
      tooltip: 'Kembali',
      icon: const Icon(Icons.arrow_back, color: Colors.white),
      onPressed: () => Navigator.of(context).maybePop(),
    ),
    bottomNavigationBar: _error != null || _items.isEmpty
        ? null
        : SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
              child: GradientActionButton(
                label: _menyimpan ? 'Menyimpan…' : 'Simpan urutan',
                icon: Icons.save_outlined,
                onPressed: _berubah && !_menyimpan ? _simpan : null,
              ),
            ),
          ),
    body: Column(
      children: [
        if (_kategori.isNotEmpty)
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                for (final k in _kategori)
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ChoiceChip(
                      label: Text(k.nama),
                      selected: _kat == k.id,
                      onSelected: (_) {
                        if (_berubah) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Simpan urutan dulu sebelum pindah kategori'),
                            ),
                          );
                          return;
                        }
                        setState(() => _kat = k.id);
                        _muat();
                      },
                    ),
                  ),
              ],
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: Text(
            'Seret ikon di kanan untuk memindah. Produk yang belum diurutkan tampil di bawah, abjad.',
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ),
        Expanded(
          child: _loading
              ? const LoadingKopkar()
              : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!, textAlign: TextAlign.center),
                      TextButton(
                        onPressed: _kategori.isEmpty ? _muatKategori : _muat,
                        child: const Text('Coba lagi'),
                      ),
                    ],
                  ),
                )
              : _items.isEmpty
              ? const Center(child: Text('Tidak ada produk aktif'))
              : ReorderableListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
                  itemCount: _items.length,
                  onReorderItem: (a, b) => setState(() {
                    _items.insert(b, _items.removeAt(a));
                  }),
                  itemBuilder: (_, i) {
                    final p = _items[i];
                    return Card(
                      key: ValueKey(p.id),
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        leading: CircleAvatar(child: Text('${i + 1}')),
                        title: Text(p.nama),
                        subtitle: Text('${p.kode} · ${p.satuan}'),
                        trailing: ReorderableDragStartListener(
                          index: i,
                          child: const Icon(Icons.drag_handle),
                        ),
                      ),
                    );
                  },
                ),
        ),
      ],
    ),
  );
}
