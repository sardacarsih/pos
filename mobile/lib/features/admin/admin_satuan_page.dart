import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Master satuan produk. Semua isian satuan (produk dan satuan jual tambahan) memilih dari daftar ini.
class AdminSatuanPage extends ConsumerStatefulWidget {
  const AdminSatuanPage({super.key});
  @override
  ConsumerState<AdminSatuanPage> createState() => _AdminSatuanPageState();
}

class _AdminSatuanPageState extends ConsumerState<AdminSatuanPage> {
  List<Satuan> _semua = [];
  bool _loading = true;
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
      final s = await ref.read(apiProvider).semuaSatuan();
      if (mounted) setState(() => _semua = s);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _info(String t) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t)));

  /// Tambah (s == null) atau ubah nama satuan.
  Future<void> _form([Satuan? s]) async {
    final nama = TextEditingController(text: s?.nama ?? '');
    String? galat;
    var sibuk = false;
    final ok = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (c) => StatefulBuilder(
        builder: (c, set) => AlertDialog(
          title: Text(s == null ? 'Tambah satuan' : 'Ubah satuan'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TextField(
                controller: nama,
                autofocus: true,
                maxLength: 20,
                textCapitalization: TextCapitalization.characters,
                decoration: InputDecoration(
                  labelText: 'Nama satuan',
                  errorText: galat,
                ),
              ),
              if (s != null)
                const Text(
                  'Produk yang memakai satuan ini ikut berganti nama. '
                  'Riwayat transaksi tidak berubah.',
                ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: sibuk ? null : () => Navigator.pop(c, false),
              child: const Text('Batal'),
            ),
            FilledButton(
              onPressed: sibuk
                  ? null
                  : () async {
                      final n = nama.text.trim().toUpperCase();
                      if (n.isEmpty) {
                        set(() => galat = 'Nama satuan wajib diisi');
                        return;
                      }
                      set(() {
                        sibuk = true;
                        galat = null;
                      });
                      try {
                        final api = ref.read(apiProvider);
                        s == null
                            ? await api.buatSatuan(n)
                            : await api.ubahSatuan(s.id, n);
                        if (c.mounted) Navigator.pop(c, true);
                      } on ApiException catch (e) {
                        set(() {
                          sibuk = false;
                          galat = e.pesan;
                        });
                      }
                    },
              child: Text(sibuk ? 'Menyimpan…' : 'Simpan'),
            ),
          ],
        ),
      ),
    );
    if (ok == true && mounted) {
      _info(s == null ? 'Satuan ditambahkan' : 'Satuan diubah');
      _muat();
    }
  }

  Future<void> _hapus(Satuan s) async {
    final api = ref.read(apiProvider);
    try {
      final n = await api.jumlahProdukSatuan(s.id);
      if (!mounted) return;
      if (n > 0) {
        await showDialog<void>(
          context: context,
          builder: (c) => AlertDialog(
            title: const Text('Tidak bisa dihapus'),
            content: Text(
              'Satuan ${s.nama} masih dipakai $n produk. '
              'Ganti satuan produk itu dulu.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(c),
                child: const Text('Tutup'),
              ),
            ],
          ),
        );
        return;
      }
      final yakin = await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          title: const Text('Hapus satuan?'),
          content: Text('${s.nama} akan dihapus dan tidak dapat dibatalkan.'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: const Text('Batal'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(c, true),
              child: const Text('Hapus'),
            ),
          ],
        ),
      );
      if (yakin != true) return;
      await api.hapusSatuan(s.id);
      if (!mounted) return;
      _info('Satuan dihapus');
      _muat();
    } on ApiException catch (e) {
      if (mounted) _info(e.pesan);
    }
  }

  @override
  Widget build(BuildContext context) {
    // Dipakai sebagai tab di AdminKatalogPage: header, profil, dan keluar ada di halaman induk.
    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: _loading || _error != null
          ? null
          : FloatingActionButton.extended(
              heroTag: 'satuan-tambah',
              onPressed: _form,
              icon: const Icon(Icons.add),
              label: const Text('Tambah satuan'),
            ),
      body: _loading
          ? const LoadingKopkar()
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
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 88),
                children: [
                  Text(
                    'Satuan produk. Semua satuan dipilih dari daftar ini; '
                    'satuan yang masih dipakai produk tidak dapat dihapus.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 8),
                  if (_semua.isEmpty) const Text('Belum ada satuan.'),
                  for (final s in _semua)
                    Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        title: Text(s.nama),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              tooltip: 'Ubah ${s.nama}',
                              icon: const Icon(Icons.edit_outlined),
                              onPressed: () => _form(s),
                            ),
                            IconButton(
                              tooltip: 'Hapus ${s.nama}',
                              icon: const Icon(Icons.delete_outline),
                              onPressed: () => _hapus(s),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}
