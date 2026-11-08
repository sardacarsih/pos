import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/foundation.dart' show compute;
import 'package:image/image.dart' as img;
import 'package:image_picker/image_picker.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Kelola produk (master POS_PRODUCT): cari, filter aktif, ubah, nonaktifkan. Tidak ada tambah/hapus.
class AdminProdukPage extends ConsumerStatefulWidget {
  const AdminProdukPage({super.key});

  @override
  ConsumerState<AdminProdukPage> createState() => _AdminProdukPageState();
}

class _AdminProdukPageState extends ConsumerState<AdminProdukPage> {
  final _q = TextEditingController();
  final _scroll = ScrollController();
  final List<ProdukAdmin> _items = [];
  List<Kategori> _kategori = [];
  String? _aktif; // null = semua, 'Y', 'N'
  bool _loading = false;
  bool _lagi = false;
  String? _error;
  int _page = 1;
  int _requestId = 0;
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    // Halaman berikutnya dimuat otomatis saat daftar hampir habis; tombol di dasar daftar jadi cadangan.
    _scroll.addListener(() {
      if (_lagi &&
          !_loading &&
          _error == null &&
          _scroll.position.pixels > _scroll.position.maxScrollExtent - 300) {
        _cari(reset: false);
      }
    });
    _cari(reset: true);
    ref
        .read(apiProvider)
        .semuaKategori()
        .then((k) {
          if (mounted) setState(() => _kategori = k);
        })
        .catchError((_) {});
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _q.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _pesan(String m) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

  Future<void> _cari({required bool reset}) async {
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
          .produkAdmin(q: _q.text.trim(), aktif: _aktif, page: page);
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

  /// Pilih foto (galeri/kamera), kecilkan, unggah. Mengembalikan versi baru atau null bila batal/gagal.
  Future<int?> _gantiFoto(int id) async {
    final sumber = await showModalBottomSheet<ImageSource>(
      context: context,
      builder: (c) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Pilih dari galeri'),
              onTap: () => Navigator.pop(c, ImageSource.gallery),
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Ambil dengan kamera'),
              onTap: () => Navigator.pop(c, ImageSource.camera),
            ),
          ],
        ),
      ),
    );
    if (sumber == null || !mounted) return null;
    try {
      // Dikecilkan di perangkat (maks 800 px, JPEG) supaya jauh di bawah batas 1 MB server.
      final x = await ImagePicker().pickImage(source: sumber);
      if (x == null) return null;
      // Dipotong persegi (tengah) dan diperkecil ke 600 px: pas dengan area gambar katalog tanpa celah.
      final hasil = await compute(fotoPersegi, await x.readAsBytes());
      if (hasil.bytes == null) {
        if (mounted) _pesan(hasil.galat ?? 'Berkas bukan gambar yang valid');
        return null;
      }
      final versi = await ref.read(apiProvider).unggahFoto(id, hasil.bytes!);
      if (mounted) _pesan('Foto disimpan');
      return versi;
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    } catch (_) {
      if (mounted)
        _pesan('Foto tidak dapat dipilih. Periksa izin kamera/galeri.');
    }
    return null;
  }

  Future<bool> _hapusFoto(int id) async {
    try {
      await ref.read(apiProvider).hapusFoto(id);
      if (mounted) _pesan('Foto dihapus');
      return true;
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
      return false;
    }
  }

  Future<void> _edit(ProdukAdmin p) async {
    // Lepas fokus kolom cari agar keyboard tidak muncul lagi setelah dialog ditutup.
    FocusManager.instance.primaryFocus?.unfocus();
    final nama = TextEditingController(text: p.nama);
    final barcode = TextEditingController(text: p.barcode);
    final satuan = TextEditingController(text: p.satuan);
    final harga = TextEditingController(text: '${p.harga}');
    final beli = TextEditingController(text: '${p.beli}');
    var kategoriId = p.kategoriId;
    var aktif = p.aktif;
    int? fotoVersi = p.fotoVersi;
    var fotoBerubah = false;
    var fotoSibuk = false;
    // Kategori produk bisa tidak ada di daftar (data lama): tampilkan apa adanya agar dropdown tidak error.
    final opsi = [
      for (final k in _kategori) k,
      if (!_kategori.any((k) => k.id == p.kategoriId) &&
          p.kategoriId.isNotEmpty)
        Kategori.fromJson({'id': p.kategoriId, 'nama': p.kategori}),
    ];
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => StatefulBuilder(
        builder: (c, set) => AlertDialog(
          title: Text('Ubah produk\n${p.kode}'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                SizedBox(
                  height: 160,
                  width: 160,
                  child: FuelArtwork(
                    name: nama.text,
                    height: 160,
                    radius: 16,
                    produkId: p.id,
                    fotoVersi: fotoVersi,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: fotoSibuk
                            ? null
                            : () async {
                                set(() => fotoSibuk = true);
                                final v = await _gantiFoto(p.id);
                                set(() {
                                  fotoSibuk = false;
                                  if (v != null) {
                                    fotoVersi = v;
                                    fotoBerubah = true;
                                  }
                                });
                              },
                        icon: const Icon(Icons.add_a_photo_outlined),
                        label: Text(
                          fotoVersi == null ? 'Tambah foto' : 'Ganti foto',
                        ),
                      ),
                    ),
                    if (fotoVersi != null) ...[
                      const SizedBox(width: 8),
                      IconButton(
                        tooltip: 'Hapus foto',
                        onPressed: fotoSibuk
                            ? null
                            : () async {
                                set(() => fotoSibuk = true);
                                final ok = await _hapusFoto(p.id);
                                set(() {
                                  fotoSibuk = false;
                                  if (ok) {
                                    fotoVersi = null;
                                    fotoBerubah = true;
                                  }
                                });
                              },
                        icon: const Icon(Icons.delete_outline),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: nama,
                  decoration: const InputDecoration(labelText: 'Nama'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: barcode,
                  decoration: const InputDecoration(labelText: 'Barcode'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: satuan,
                  decoration: const InputDecoration(labelText: 'Satuan'),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  initialValue: opsi.any((k) => k.id == kategoriId)
                      ? kategoriId
                      : null,
                  isExpanded: true,
                  items: [
                    for (final k in opsi)
                      DropdownMenuItem(
                        value: k.id,
                        child: Text(k.nama, overflow: TextOverflow.ellipsis),
                      ),
                  ],
                  onChanged: (v) => set(() => kategoriId = v ?? kategoriId),
                  decoration: const InputDecoration(labelText: 'Kategori'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: harga,
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  decoration: const InputDecoration(
                    labelText: 'Harga jual (Rp)',
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: beli,
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  decoration: const InputDecoration(
                    labelText: 'Harga beli (Rp)',
                  ),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Aktif (tampil di kasir)'),
                  value: aktif,
                  onChanged: (v) => set(() => aktif = v),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: const Text('Batal'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(c, true),
              child: const Text('Simpan'),
            ),
          ],
        ),
      ),
    );
    final data = (
      nama: nama.text.trim(),
      barcode: barcode.text.trim(),
      satuan: satuan.text.trim(),
      harga: digitsToInt(harga.text),
      beli: digitsToInt(beli.text),
    );
    // Dialog masih beranimasi tutup & memakai controller; dispose langsung memicu assertion.
    Future.delayed(const Duration(milliseconds: 500), () {
      for (final c in [nama, barcode, satuan, harga, beli]) {
        c.dispose();
      }
    });
    // Foto diunggah/dihapus langsung (terpisah dari Simpan): muat ulang daftar agar thumbnail ikut berubah.
    if (fotoBerubah && mounted) _cari(reset: true);
    if (ok != true || !mounted) return;

    // Harga memengaruhi penjualan: minta konfirmasi bila harga jual berubah.
    if (data.harga != p.harga) {
      final lanjut = await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          title: const Text('Ubah harga jual?'),
          content: Text(
            '${p.nama}\n${rupiah(p.harga)} → ${rupiah(data.harga)}',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: const Text('Batal'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(c, true),
              child: const Text('Ya, ubah'),
            ),
          ],
        ),
      );
      if (lanjut != true || !mounted) return;
    }

    try {
      final baru = await ref
          .read(apiProvider)
          .ubahProduk(
            p.id,
            nama: data.nama,
            barcode: data.barcode,
            satuan: data.satuan,
            kategoriId: kategoriId,
            harga: data.harga,
            beli: data.beli,
            aktif: aktif,
          );
      if (!mounted) return;
      setState(() {
        final i = _items.indexWhere((e) => e.id == baru.id);
        if (i >= 0) _items[i] = baru;
        // Filter aktif/nonaktif aktif -> produk yang tidak lagi cocok hilang dari daftar.
        if (_aktif == 'Y' && !baru.aktif || _aktif == 'N' && baru.aktif) {
          _items.removeWhere((e) => e.id == baru.id);
        }
      });
      _pesan('Produk disimpan');
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    }
  }

  Widget _filter(String label, String? nilai) => ChoiceChip(
    label: Text(label),
    selected: _aktif == nilai,
    onSelected: (_) {
      setState(() => _aktif = nilai);
      _cari(reset: true);
    },
  );

  @override
  // Dipakai sebagai tab di AdminKatalogPage: header, profil, dan keluar ada di halaman induk.
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: Colors.transparent,
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
          child: TextField(
            controller: _q,
            textInputAction: TextInputAction.search,
            onChanged: (_) {
              _debounce?.cancel();
              _debounce = Timer(
                const Duration(milliseconds: 350),
                () => _cari(reset: true),
              );
            },
            onSubmitted: (_) => _cari(reset: true),
            decoration: const InputDecoration(
              labelText: 'Cari nama, kode, atau barcode',
              prefixIcon: Icon(Icons.search),
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          child: Wrap(
            spacing: 8,
            children: [
              _filter('Semua', null),
              _filter('Aktif', 'Y'),
              _filter('Nonaktif', 'N'),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => _cari(reset: true),
            child: ListView(
              controller: _scroll,
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
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
                            onPressed: () => _cari(reset: true),
                            child: const Text('Coba lagi'),
                          ),
                        ],
                      ),
                    ),
                  ),
                if (!_loading && _error == null && _items.isEmpty)
                  const Padding(
                    padding: EdgeInsets.all(36),
                    child: Center(child: Text('Produk tidak ditemukan.')),
                  ),
                for (final p in _items)
                  Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      onTap: () => _edit(p),
                      leading: SizedBox(
                        width: 56,
                        height: 56,
                        child: FuelArtwork(
                          name: p.nama,
                          height: 56,
                          radius: 10,
                          produkId: p.id,
                          fotoVersi: p.fotoVersi,
                        ),
                      ),
                      title: Text(
                        p.nama,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      subtitle: Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Wrap(
                          spacing: 8,
                          runSpacing: 4,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          children: [
                            if (!p.aktif)
                              const StatePill(
                                text: 'NONAKTIF',
                                color: Color(0xFF78817B),
                              ),
                            Text('${p.kode} · ${p.kategori} · ${p.satuan}'),
                          ],
                        ),
                      ),
                      trailing: Text(
                        rupiah(p.harga),
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                  ),
                if (_loading)
                  const Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                if (_lagi && !_loading && _error == null)
                  TextButton(
                    onPressed: () => _cari(reset: false),
                    child: const Text('Muat produk lainnya'),
                  ),
              ],
            ),
          ),
        ),
      ],
    ),
  );
}

/// Validasi + siapkan foto di perangkat (server memvalidasi ulang): harus gambar yang terbaca, sisi minimal 200 px;
/// dipotong persegi di tengah, diperkecil maks 600 px, JPEG kualitas 85. Top-level agar jalan di isolate (compute).
({Uint8List? bytes, String? galat}) fotoPersegi(Uint8List bytes) {
  final asli = img.decodeImage(bytes);
  if (asli == null)
    return (bytes: null, galat: 'Berkas bukan gambar yang valid');
  final sisi = asli.width < asli.height ? asli.width : asli.height;
  if (sisi < 200) {
    return (
      bytes: null,
      galat:
          'Foto terlalu kecil (${asli.width}x${asli.height}); minimal 200x200 piksel',
    );
  }
  var kotak = img.copyCrop(
    asli,
    x: (asli.width - sisi) ~/ 2,
    y: (asli.height - sisi) ~/ 2,
    width: sisi,
    height: sisi,
  );
  if (sisi > 600) kotak = img.copyResize(kotak, width: 600, height: 600);
  return (
    bytes: Uint8List.fromList(img.encodeJpg(kotak, quality: 85)),
    galat: null,
  );
}
