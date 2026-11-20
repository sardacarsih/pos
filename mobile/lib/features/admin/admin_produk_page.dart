import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/foundation.dart' show compute;
import 'package:image/image.dart' as img;
import 'package:image_picker/image_picker.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/beranda_widgets.dart' show statusMargin;
import '../../core/katalog.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Kelola produk (master POS_PRODUCT): cari, filter aktif, tambah, ubah, nonaktifkan, hapus (bila tanpa riwayat).
class AdminProdukPage extends ConsumerStatefulWidget {
  const AdminProdukPage({super.key, this.hargaRendah = false});

  /// true = monitoring: hanya produk aktif dengan harga jual <= harga beli (HPP), paling rugi dulu.
  final bool hargaRendah;

  @override
  ConsumerState<AdminProdukPage> createState() => _AdminProdukPageState();
}

class _AdminProdukPageState extends ConsumerState<AdminProdukPage> {
  final _q = TextEditingController();
  final _scroll = ScrollController();
  final List<ProdukAdmin> _items = [];
  List<Kategori> _kategori = [];
  List<String> _satuan = [];
  String? _aktif; // null = semua, 'Y', 'N'
  int? _rugi, _tanpaHpp;
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
    ref
        .read(apiProvider)
        .semuaSatuan()
        .then((s) {
          if (mounted) setState(() => _satuan = [for (final x in s) x.nama]);
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
          .produkAdmin(
            q: _q.text.trim(),
            aktif: _aktif,
            hargaRendah: widget.hargaRendah,
            page: page,
          );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _items.addAll(r.items);
        _page = page;
        _lagi = r.lagi;
        if (r.rugi != null) _rugi = r.rugi;
        if (r.tanpaHpp != null) _tanpaHpp = r.tanpaHpp;
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

  Future<void> _toggleAktif(ProdukAdmin p) async {
    try {
      final baru = await ref
          .read(apiProvider)
          .ubahProduk(
            p.id,
            nama: p.nama,
            barcode: p.barcode,
            satuan: p.satuan,
            kategoriId: p.kategoriId,
            harga: p.harga,
            beli: p.beli,
            aktif: !p.aktif,
          );
      if (!mounted) return;
      setState(() {
        final i = _items.indexWhere((e) => e.id == baru.id);
        if (i >= 0) _items[i] = baru;
        if (_aktif == 'Y' && !baru.aktif || _aktif == 'N' && baru.aktif) {
          _items.removeWhere((e) => e.id == baru.id);
        }
      });
      _pesan(baru.aktif ? 'Produk diaktifkan' : 'Produk dinonaktifkan');
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    }
  }

  /// Hapus permanen: periksa riwayat dulu (server memeriksa ulang saat menghapus), lalu minta konfirmasi.
  Future<void> _hapus(ProdukAdmin p) async {
    final api = ref.read(apiProvider);
    String? tolak;
    try {
      final j = await api.jejakProduk(p.id);
      if (!j.bisaHapus) tolak = pesanTidakBisaHapus(j.rincian);
    } on ApiException catch (e) {
      tolak = e.pesan;
    }
    if (!mounted) return;
    if (tolak != null) {
      await showDialog<void>(
        context: context,
        builder: (c) => AlertDialog(
          title: const Text('Tidak bisa dihapus'),
          content: Text('${p.nama}\n\n$tolak'),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(c),
              child: const Text('Mengerti'),
            ),
          ],
        ),
      );
      return;
    }
    final lanjut = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Hapus permanen?'),
        content: Text(
          '${p.nama} (${p.kode})\n\nProduk belum pernah dipakai di transaksi. Data dihapus dan tidak dapat dibatalkan.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('Batal'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFB3261E),
            ),
            onPressed: () => Navigator.pop(c, true),
            child: const Text('Hapus permanen'),
          ),
        ],
      ),
    );
    if (lanjut != true || !mounted) return;
    try {
      await api.hapusProduk(p.id);
      if (!mounted) return;
      setState(() => _items.removeWhere((e) => e.id == p.id));
      _pesan('Produk dihapus permanen');
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    }
  }

  /// Produk baru: kode item dibuat server. Kunci idempotensi dibuat sekali per dialog agar retry tidak membuat ganda.
  Future<void> _tambah() async {
    FocusManager.instance.primaryFocus?.unfocus();
    final nama = TextEditingController();
    final barcode = TextEditingController();
    final satuan = TextEditingController(
      text: _satuan.contains('PCS')
          ? 'PCS'
          : (_satuan.isEmpty ? '' : _satuan.first),
    );
    final harga = TextEditingController();
    final beli = TextEditingController();
    var kategoriId = _kategori.isEmpty ? '' : _kategori.first.id;
    final kunci = newCheckoutKey();
    final satuanLain = <_DrafSatuanJual>[];
    String? galat;
    var sibuk = false;
    final baru = await showDialog<ProdukAdmin>(
      context: context,
      barrierDismissible: false,
      builder: (c) => StatefulBuilder(
        builder: (c, set) => PopScope(
          canPop: !sibuk,
          child: AlertDialog(
            title: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Tambah produk'),
                if (galat != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      galat!,
                      style: Theme.of(c).textTheme.bodyMedium
                          ?.copyWith(color: Theme.of(c).colorScheme.error),
                    ),
                  ),
              ],
            ),
            content: SingleChildScrollView(
              child: AbsorbPointer(
                absorbing: sibuk,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    TextField(
                      controller: nama,
                      textCapitalization: TextCapitalization.words,
                      decoration: const InputDecoration(labelText: 'Nama'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: barcode,
                      decoration: const InputDecoration(
                        labelText: 'Barcode (opsional)',
                      ),
                    ),
                    const SizedBox(height: 12),
                    _PilihSatuan(
                      label: 'Satuan dasar',
                      nilai: satuan.text,
                      opsi: _satuan,
                      onChanged: (v) => satuan.text = v,
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<String>(
                      initialValue: _kategori.any((k) => k.id == kategoriId)
                          ? kategoriId
                          : null,
                      isExpanded: true,
                      items: [
                        for (final k in _kategori)
                          DropdownMenuItem(
                            value: k.id,
                            child: Text(
                              k.nama,
                              overflow: TextOverflow.ellipsis,
                            ),
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
                    const SizedBox(height: 16),
                    Text(
                      'Satuan jual tambahan',
                      style: Theme.of(c).textTheme.titleSmall,
                    ),
                    const Text(
                      'Isi adalah jumlah satuan dasar per satuan jual.',
                    ),
                    for (final draf in satuanLain)
                      _SatuanJualFields(
                        key: ObjectKey(draf),
                        draf: draf,
                        opsi: _satuan,
                        onHapus: () => set(() => satuanLain.remove(draf)),
                      ),
                    OutlinedButton(
                      onPressed: satuanLain.length >= 10
                          ? null
                          : () => set(() => satuanLain.add(_DrafSatuanJual())),
                      child: const Text('Tambah satuan'),
                    ),
                  ],
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: sibuk ? null : () => Navigator.pop(c),
                child: const Text('Batal'),
              ),
              FilledButton(
                onPressed: sibuk
                    ? null
                    : () async {
                        final h = digitsToInt(harga.text),
                            b = digitsToInt(beli.text);
                        final salah = cekProdukBaru(
                          nama: nama.text,
                          satuan: satuan.text,
                          kategoriId: kategoriId,
                          harga: h,
                          beli: b,
                        );
                        if (salah != null) {
                          set(() => galat = salah);
                          return;
                        }
                        final daftar = <IsianSatuanJual>[];
                        for (final (i, draf) in satuanLain.indexed) {
                          final nilai = draf.nilai();
                          if (nilai == null) {
                            set(
                              () => galat =
                                  'Lengkapi satuan tambahan ${i + 1}. Isi harus lebih dari 1 (maksimal 4 desimal), harga lebih dari 0 (maksimal 2 desimal).',
                            );
                            return;
                          }
                          daftar.add(nilai);
                        }
                        FocusScope.of(c).unfocus();
                        set(() {
                          sibuk = true;
                          galat = null;
                        });
                        try {
                          final p = await ref
                              .read(apiProvider)
                              .buatProduk(
                                nama: nama.text.trim(),
                                barcode: barcode.text.trim(),
                                satuan: satuan.text.trim(),
                                kategoriId: kategoriId,
                                harga: h,
                                beli: b,
                                idempotencyKey: kunci,
                                satuanLain: daftar,
                              );
                          if (c.mounted) Navigator.pop(c, p);
                        } on ApiException catch (e) {
                          if (!c.mounted) return;
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
      ),
    );
    // Dialog masih beranimasi tutup & memakai controller; dispose langsung memicu assertion.
    Future.delayed(const Duration(milliseconds: 500), () {
      for (final c in [nama, barcode, satuan, harga, beli]) {
        c.dispose();
      }
    });
    if (baru == null || !mounted) return;
    setState(() {
      // Daftar ber-filter: tampilkan produk baru hanya bila cocok dengan filter aktif.
      if (_aktif != 'N' && !widget.hargaRendah) _items.insert(0, baru);
    });
    _pesan('Produk ${baru.nama} ditambahkan (${baru.kode})');
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
                _PilihSatuan(
                  label: 'Satuan',
                  nilai: satuan.text,
                  opsi: _satuan,
                  onChanged: (v) => satuan.text = v,
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
        if (_aktif == 'Y' && !baru.aktif ||
            _aktif == 'N' && baru.aktif ||
            widget.hargaRendah &&
                (!baru.aktif || (baru.beli > 0 && baru.harga > baru.beli))) {
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
    floatingActionButton: widget.hargaRendah
        ? null
        : FloatingActionButton.extended(
            onPressed: _tambah,
            icon: const Icon(Icons.add),
            label: const Text('Tambah produk'),
          ),
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
        if (widget.hargaRendah)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                _rugi == null || _tanpaHpp == null
                    ? 'Harga jual ≤ harga beli (HPP) atau HPP belum diisi'
                    : '$_rugi produk harga jual ≤ HPP · $_tanpaHpp belum ada harga beli',
              ),
            ),
          )
        else
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
                  Padding(
                    padding: const EdgeInsets.all(36),
                    child: Center(
                      child: Text(
                        widget.hargaRendah
                            ? 'Tidak ada produk dengan harga jual ≤ HPP atau tanpa harga beli.'
                            : 'Produk tidak ditemukan.',
                      ),
                    ),
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
                            if (widget.hargaRendah)
                              if (statusMargin(p.harga, p.beli).tanpaHpp)
                                const Text(
                                  'HPP belum diisi',
                                  style: TextStyle(
                                    color: Color(0xFF8A5A00),
                                    fontWeight: FontWeight.w700,
                                  ),
                                )
                              else
                                Text(
                                  'beli ${rupiah(p.beli)} · margin ${rupiah(p.harga - p.beli)}',
                                  style: const TextStyle(
                                    color: Color(0xFFB3261E),
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                          ],
                        ),
                      ),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            rupiah(p.harga),
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          PopupMenuButton<String>(
                            tooltip: 'Aksi untuk ${p.nama}',
                            icon: const Icon(Icons.more_vert),
                            onSelected: (a) async {
                              switch (a) {
                                case 'ubah':
                                  await _edit(p);
                                case 'foto':
                                  final v = await _gantiFoto(p.id);
                                  if (v != null && mounted) _cari(reset: true);
                                case 'aktif':
                                  await _toggleAktif(p);
                                case 'hapus':
                                  await _hapus(p);
                              }
                            },
                            itemBuilder: (_) => [
                              const PopupMenuItem(
                                value: 'ubah',
                                child: ListTile(
                                  leading: Icon(Icons.edit_outlined),
                                  title: Text('Ubah'),
                                  dense: true,
                                ),
                              ),
                              const PopupMenuItem(
                                value: 'foto',
                                child: ListTile(
                                  leading: Icon(Icons.add_a_photo_outlined),
                                  title: Text('Ganti foto'),
                                  dense: true,
                                ),
                              ),
                              PopupMenuItem(
                                value: 'aktif',
                                child: ListTile(
                                  leading: Icon(
                                    p.aktif
                                        ? Icons.visibility_off_outlined
                                        : Icons.visibility_outlined,
                                  ),
                                  title: Text(
                                    p.aktif ? 'Nonaktifkan' : 'Aktifkan',
                                  ),
                                  dense: true,
                                ),
                              ),
                              const PopupMenuItem(
                                value: 'hapus',
                                child: ListTile(
                                  leading: Icon(
                                    Icons.delete_outline,
                                    color: Color(0xFFB3261E),
                                  ),
                                  title: Text(
                                    'Hapus permanen',
                                    style: TextStyle(color: Color(0xFFB3261E)),
                                  ),
                                  dense: true,
                                ),
                              ),
                            ],
                          ),
                        ],
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

class _DrafSatuanJual {
  String satuan = '', isi = '', harga = '', barcode = '';

  IsianSatuanJual? nilai() {
    final isiTeks = isi.trim().replaceAll(',', '.');
    final hargaTeks = harga.trim().replaceAll(',', '.');
    if (!RegExp(r'^\d+(\.\d{1,4})?$').hasMatch(isiTeks) ||
        !RegExp(r'^\d+(\.\d{1,2})?$').hasMatch(hargaTeks)) {
      return null;
    }
    final jumlah = num.tryParse(isiTeks);
    final rupiah = num.tryParse(hargaTeks);
    if (satuan.trim().isEmpty ||
        jumlah == null ||
        jumlah <= 1 ||
        jumlah > 1000000 ||
        rupiah == null ||
        !rupiah.isFinite ||
        rupiah <= 0) {
      return null;
    }
    return (
      satuan: satuan.trim(),
      isi: jumlah,
      harga: rupiah,
      barcode: barcode.trim(),
    );
  }
}

class _SatuanJualFields extends StatelessWidget {
  const _SatuanJualFields({
    super.key,
    required this.draf,
    required this.opsi,
    required this.onHapus,
  });

  final _DrafSatuanJual draf;
  final List<String> opsi;
  final VoidCallback onHapus;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _PilihSatuan(
            label: 'Satuan tambahan',
            nilai: draf.satuan,
            opsi: opsi,
            onChanged: (v) => draf.satuan = v,
          ),
          TextFormField(
            initialValue: draf.isi,
            onChanged: (v) => draf.isi = v,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Isi per satuan'),
          ),
          TextFormField(
            initialValue: draf.harga,
            onChanged: (v) => draf.harga = v,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(
              labelText: 'Harga jual satuan (Rp)',
            ),
          ),
          TextFormField(
            initialValue: draf.barcode,
            onChanged: (v) => draf.barcode = v,
            maxLength: 50,
            decoration: const InputDecoration(
              labelText: 'Barcode satuan (opsional)',
              counterText: '',
            ),
          ),
          TextButton(onPressed: onHapus, child: const Text('Hapus satuan')),
        ],
      ),
    );
  }
}

/// Pilihan satuan dari master. Nilai awal yang tidak ada di master (produk warisan) tetap ditampilkan.
class _PilihSatuan extends StatelessWidget {
  const _PilihSatuan({
    required this.label,
    required this.nilai,
    required this.opsi,
    required this.onChanged,
  });

  final String label, nilai;
  final List<String> opsi;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    final daftar = [
      if (nilai.isNotEmpty && !opsi.contains(nilai)) nilai,
      ...opsi,
    ];
    return DropdownButtonFormField<String>(
      initialValue: daftar.contains(nilai) ? nilai : null,
      isExpanded: true,
      decoration: InputDecoration(labelText: label),
      items: [
        for (final n in daftar) DropdownMenuItem(value: n, child: Text(n)),
      ],
      onChanged: (v) => onChanged(v ?? ''),
    );
  }
}
