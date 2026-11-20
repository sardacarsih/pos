import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/models.dart';
import '../../core/providers.dart';

/// Satu baris yang diedit: jumlah dalam satuan dasar produk, sebagai teks (koma atau titik).
class BarisEditor {
  BarisEditor({
    required this.produkId,
    required this.kode,
    required this.nama,
    required this.satuan,
    String qty = '',
    String keterangan = '',
  }) : qty = TextEditingController(text: qty),
       keterangan = TextEditingController(text: keterangan);

  final int produkId;
  final String kode, nama, satuan;
  final TextEditingController qty, keterangan;

  /// Angka dikirim ke server dengan titik desimal; kosong atau bukan angka = null.
  String? get qtyKirim {
    final t = qty.text.trim().replaceAll(',', '.');
    return double.tryParse(t) == null ? null : t;
  }
}

/// Editor daftar barang untuk Ubah Opname dan Barang Rusak (baru/ubah): ubah jumlah, tambah barang lewat
/// pencarian, hapus baris, isi alasan (bila [perluAlasan]), lalu kirim lewat [simpan] (melempar [ApiException]
/// bila ditolak). Kunci idempotensi dipakai ulang selama isi tidak berubah (retry aman), diganti saat isi berubah.
class AdminDokumenEditorPage extends ConsumerStatefulWidget {
  const AdminDokumenEditorPage({
    super.key,
    required this.judul,
    required this.labelQty,
    required this.awal,
    required this.simpan,
    this.denganKeterangan = false,
    this.perluAlasan = true,
  });

  final String judul, labelQty;
  final List<BarisEditor> awal;
  final bool denganKeterangan, perluAlasan;
  final Future<void> Function(List<BarisEditor> baris, String alasan, String key)
  simpan;

  @override
  ConsumerState<AdminDokumenEditorPage> createState() =>
      _AdminDokumenEditorPageState();
}

class _AdminDokumenEditorPageState
    extends ConsumerState<AdminDokumenEditorPage> {
  late final List<BarisEditor> _baris = [...widget.awal];
  final _cari = TextEditingController();
  final _alasan = TextEditingController();
  List<StokItem> _hasil = [];
  bool _mencari = false, _menyimpan = false;
  String? _error;
  String _key = newCheckoutKey(), _sidik = '';
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _cari.dispose();
    _alasan.dispose();
    for (final b in _baris) {
      b.qty.dispose();
      b.keterangan.dispose();
    }
    super.dispose();
  }

  void _cariBarang(String q) {
    _debounce?.cancel();
    if (q.trim().isEmpty) {
      setState(() => _hasil = []);
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 350), () async {
      setState(() => _mencari = true);
      try {
        final r = await ref.read(apiProvider).cariStok(q: q.trim(), size: 10);
        if (!mounted || _cari.text.trim() != q.trim()) return;
        setState(() => _hasil = r.items);
      } on ApiException catch (e) {
        if (mounted) setState(() => _error = e.pesan);
      } finally {
        if (mounted) setState(() => _mencari = false);
      }
    });
  }

  void _tambah(StokItem p) {
    if (_baris.any((b) => b.produkId == p.id)) {
      setState(() => _error = '${p.nama} sudah ada di daftar.');
      return;
    }
    setState(() {
      _baris.add(
        BarisEditor(produkId: p.id, kode: p.kode, nama: p.nama, satuan: p.satuan),
      );
      _hasil = [];
      _cari.clear();
      _error = null;
    });
  }

  String _sidikIsi() => [
    _alasan.text.trim(),
    for (final b in _baris)
      '${b.produkId}=${b.qty.text.trim()}|${b.keterangan.text.trim()}',
  ].join(';');

  Future<void> _kirim() async {
    if (_baris.isEmpty) {
      setState(() => _error = 'Daftar barang masih kosong.');
      return;
    }
    for (final b in _baris) {
      if (b.qtyKirim == null) {
        setState(() => _error = 'Jumlah ${b.nama} belum diisi dengan angka.');
        return;
      }
    }
    if (widget.perluAlasan && _alasan.text.trim().isEmpty) {
      setState(() => _error = 'Alasan wajib diisi.');
      return;
    }
    final sidik = _sidikIsi();
    if (sidik != _sidik) {
      _key = newCheckoutKey();
      _sidik = sidik;
    }
    setState(() {
      _menyimpan = true;
      _error = null;
    });
    try {
      await widget.simpan(_baris, _alasan.text.trim(), _key);
      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _menyimpan = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(widget.judul)),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: TextField(
                controller: _cari,
                onChanged: _cariBarang,
                decoration: const InputDecoration(
                  labelText: 'Tambah barang (nama, kode, atau barcode)',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
            ),
            if (_mencari) const LinearProgressIndicator(),
            for (final p in _hasil)
              ListTile(
                dense: true,
                title: Text(p.nama, maxLines: 1, overflow: TextOverflow.ellipsis),
                subtitle: Text('${p.kode} · stok ${p.stok} ${p.satuan}'),
                trailing: const Icon(Icons.add),
                onTap: () => _tambah(p),
              ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                children: [
                  if (_baris.isEmpty)
                    const Padding(
                      padding: EdgeInsets.all(24),
                      child: Text('Cari barang di atas untuk menambah.'),
                    ),
                  for (final b in _baris)
                    Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(b.nama, style: tema.textTheme.titleSmall),
                                      Text(b.kode),
                                    ],
                                  ),
                                ),
                                IconButton(
                                  tooltip: 'Hapus ${b.nama}',
                                  icon: const Icon(Icons.close),
                                  onPressed: () => setState(() => _baris.remove(b)),
                                ),
                              ],
                            ),
                            TextField(
                              controller: b.qty,
                              keyboardType: const TextInputType.numberWithOptions(
                                decimal: true,
                              ),
                              decoration: InputDecoration(
                                labelText: b.satuan.isEmpty
                                    ? widget.labelQty
                                    : '${widget.labelQty} (${b.satuan})',
                              ),
                            ),
                            if (widget.denganKeterangan)
                              TextField(
                                controller: b.keterangan,
                                maxLength: 100,
                                decoration: const InputDecoration(
                                  labelText: 'Keterangan (opsional)',
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (widget.perluAlasan)
                    TextField(
                      controller: _alasan,
                      maxLength: 250,
                      decoration: const InputDecoration(labelText: 'Alasan perubahan'),
                    ),
                  if (_error != null)
                    Semantics(
                      liveRegion: true,
                      child: Text(
                        _error!,
                        style: TextStyle(color: tema.colorScheme.error),
                      ),
                    ),
                  const SizedBox(height: 8),
                  FilledButton(
                    onPressed: _menyimpan ? null : _kirim,
                    child: Text(_menyimpan ? 'Menyimpan…' : 'Simpan'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
