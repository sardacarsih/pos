import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../profil/profil_aksi.dart';
import '../../core/widgets.dart';
import 'admin_urut_page.dart';

/// Kategori barang yang tampil di "Pilih Produk" kasir (berlaku untuk semua kasir).
class AdminKategoriPage extends ConsumerStatefulWidget {
  const AdminKategoriPage({super.key});
  @override
  ConsumerState<AdminKategoriPage> createState() => _AdminKategoriPageState();
}

class _AdminKategoriPageState extends ConsumerState<AdminKategoriPage> {
  List<Kategori> _semua = [];
  Set<String> _pilih = {}, _tersimpan = {};
  bool _loading = true, _menyimpan = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      final semua = await api.semuaKategori();
      final aktif = await api.kategoriTampil();
      if (!mounted) return;
      setState(() {
        _semua = semua;
        _tersimpan = aktif.toSet();
        _pilih = {...aktif};
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  bool get _berubah =>
      _pilih.length != _tersimpan.length || !_pilih.containsAll(_tersimpan);

  Future<void> _simpan() async {
    setState(() => _menyimpan = true);
    try {
      await ref.read(apiProvider).setKategoriTampil(_pilih.toList());
      if (!mounted) return;
      setState(() => _tersimpan = {..._pilih});
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Pengaturan disimpan')));
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
  Widget build(BuildContext context) {
    final ringkas = _pilih.isEmpty
        ? 'Semua kategori tampil'
        : '${_pilih.length} dari ${_semua.length} kategori tampil';
    return TropicalScaffold(
      title: 'Admin · Kategori',
      actions: [
        const ProfilAksi(),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      bottomNavigationBar: _loading || _error != null
          ? null
          : SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: GradientActionButton(
                  label: _menyimpan ? 'Menyimpan…' : 'Simpan',
                  icon: Icons.save_outlined,
                  onPressed: _berubah && !_menyimpan ? _simpan : null,
                ),
              ),
            ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_error!, textAlign: TextAlign.center),
                  TextButton(onPressed: _muat, child: const Text('Coba lagi')),
                ],
              ),
            )
          : RefreshIndicator(
              onRefresh: _muat,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
                children: [
                  Text(
                    'Kategori yang tampil di Pilih Produk kasir. '
                    'Berlaku untuk semua kasir; kosong = semua kategori.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          ringkas,
                          style: Theme.of(context).textTheme.titleSmall
                              ?.copyWith(fontWeight: FontWeight.w700),
                        ),
                      ),
                      TextButton(
                        onPressed: () =>
                            setState(() => _pilih = {for (final k in _semua) k.id}),
                        child: const Text('Pilih semua'),
                      ),
                      TextButton(
                        onPressed: _pilih.isEmpty
                            ? null
                            : () => setState(() => _pilih = {}),
                        child: const Text('Kosongkan'),
                      ),
                    ],
                  ),
                  OutlinedButton.icon(
                    onPressed: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => const AdminUrutPage()),
                    ),
                    icon: const Icon(Icons.swap_vert),
                    label: const Text('Atur urutan produk'),
                  ),
                  const SizedBox(height: 8),
                  for (final k in _semua)
                    Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: CheckboxListTile(
                        value: _pilih.contains(k.id),
                        title: Text(k.nama),
                        onChanged: (v) => setState(
                          () => v == true ? _pilih.add(k.id) : _pilih.remove(k.id),
                        ),
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}
