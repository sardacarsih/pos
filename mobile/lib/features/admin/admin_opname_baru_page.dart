import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../kasir/kasir_page.dart' show ScanPage;
import 'admin_opname_page.dart' show fmtQty;

/// Satu barang yang sedang dihitung. Controller dimiliki entri ini dan di-dispose saat entri dibuang.
class _Entri {
  _Entri(this.item);
  final StokItem item;
  final TextEditingController qty = TextEditingController();

  /// null = belum diisi / tidak valid.
  double? get nilai {
    final v = double.tryParse(qty.text.trim().replaceAll(',', '.'));
    return v == null || v < 0 ? null : v;
  }
}

/// Buat dokumen SO baru: cari/scan barang, isi qty fisik, simpan sebagai satu dokumen (satu nomor SO).
/// Pop dengan nomor SO bila tersimpan.
class AdminOpnameBaruPage extends ConsumerStatefulWidget {
  const AdminOpnameBaruPage({super.key});

  @override
  ConsumerState<AdminOpnameBaruPage> createState() =>
      _AdminOpnameBaruPageState();
}

class _AdminOpnameBaruPageState extends ConsumerState<AdminOpnameBaruPage> {
  final _q = TextEditingController();
  final List<_Entri> _daftar = [];
  List<StokItem> _hasil = [];
  // Kunci idempotensi satu sesi: retry setelah gagal jaringan memakai kunci yang sama (tidak dobel).
  final String _key = newCheckoutKey();
  Timer? _debounce;
  bool _mencari = false;
  bool _menyimpan = false;
  String? _error;
  int _requestId = 0;

  @override
  void dispose() {
    _debounce?.cancel();
    _q.dispose();
    for (final e in _daftar) {
      e.qty.dispose();
    }
    super.dispose();
  }

  void _pesan(String m) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

  Future<void> _cari() async {
    final teks = _q.text.trim();
    final id = ++_requestId;
    if (teks.isEmpty) {
      setState(() {
        _hasil = [];
        _mencari = false;
      });
      return;
    }
    setState(() {
      _mencari = true;
      _error = null;
    });
    try {
      final r = await ref.read(apiProvider).cariStok(q: teks);
      if (!mounted || id != _requestId) return;
      setState(() => _hasil = r.items);
    } on ApiException catch (e) {
      if (mounted && id == _requestId) setState(() => _error = e.pesan);
    } finally {
      if (mounted && id == _requestId) setState(() => _mencari = false);
    }
  }

  Future<void> _scan() async {
    final kode = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const ScanPage()),
    );
    if (kode == null || !mounted) return;
    _q.text = kode;
    await _cari();
    // Barcode persis satu barang -> langsung masuk daftar hitung.
    final cocok = _hasil.where((p) => p.barcode == kode).toList();
    if (cocok.length == 1 && mounted) {
      _tambah(cocok.first);
      _q.clear();
      setState(() => _hasil = []);
    }
  }

  void _tambah(StokItem p) {
    if (_daftar.any((e) => e.item.id == p.id)) {
      return _pesan('${p.nama} sudah ada di daftar hitung');
    }
    setState(() => _daftar.add(_Entri(p)));
  }

  void _hapus(_Entri e) {
    setState(() => _daftar.remove(e));
    // Field masih dibangun pada frame ini; dispose setelah frame selesai.
    WidgetsBinding.instance.addPostFrameCallback((_) => e.qty.dispose());
  }

  Future<void> _simpan() async {
    if (_menyimpan || _daftar.isEmpty) return;
    for (final e in _daftar) {
      if (e.nilai == null) {
        return setState(() => _error = 'Qty fisik ${e.item.nama} belum valid');
      }
    }
    final totalSelisih = _daftar.fold<double>(
      0,
      (a, e) => a + (e.nilai! - e.item.stok).abs(),
    );
    final lanjut = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Simpan dokumen SO?'),
        content: Text(
          '${_daftar.length} barang, total selisih mutlak ${fmtQty(totalSelisih)}.\n'
          'Dokumen SO tidak dapat dibatalkan dari aplikasi.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('Batal'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(c, true),
            child: const Text('Ya, simpan'),
          ),
        ],
      ),
    );
    if (lanjut != true || !mounted) return;
    setState(() {
      _menyimpan = true;
      _error = null;
    });
    try {
      final nomor = await ref.read(apiProvider).simpanSo([
        for (final e in _daftar)
          (
            produkId: e.item.id,
            qty: e.qty.text.trim().replaceAll(',', '.'),
          ),
      ], idempotencyKey: _key);
      if (mounted) Navigator.pop(context, nomor);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _menyimpan = false);
    }
  }

  @override
  Widget build(BuildContext context) => TropicalScaffold(
    title: 'Opname baru',
    actions: const [],
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
          child: TextField(
            controller: _q,
            textInputAction: TextInputAction.search,
            onChanged: (_) {
              _debounce?.cancel();
              _debounce = Timer(const Duration(milliseconds: 350), _cari);
            },
            onSubmitted: (_) => _cari(),
            decoration: InputDecoration(
              labelText: 'Cari nama, kode, atau barcode',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: IconButton(
                tooltip: 'Scan barcode',
                icon: const Icon(Icons.qr_code_scanner),
                onPressed: _scan,
              ),
            ),
          ),
        ),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Text(
              _error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            children: [
              if (_mencari) const LoadingKopkar(),
              for (final p in _hasil)
                Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(
                      p.nama,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    subtitle: Text(
                      '${p.kode} · stok sistem ${fmtQty(p.stok)} ${p.satuan}',
                    ),
                    trailing: IconButton(
                      tooltip: 'Tambah ke daftar hitung',
                      icon: const Icon(Icons.add_circle_outline),
                      onPressed: () => _tambah(p),
                    ),
                  ),
                ),
              const SizedBox(height: 8),
              Text(
                'Daftar hitung (${_daftar.length})',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              if (_daftar.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(
                    child: Text('Cari atau scan barang untuk mulai menghitung.'),
                  ),
                ),
              for (final e in _daftar)
                Card(
                  margin: const EdgeInsets.only(top: 8),
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(12, 8, 4, 8),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                e.item.nama,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              Text(
                                '${e.item.kode} · sistem ${fmtQty(e.item.stok)} ${e.item.satuan}'
                                '${e.nilai == null ? '' : ' · selisih ${fmtQty(e.nilai! - e.item.stok)}'}',
                              ),
                            ],
                          ),
                        ),
                        SizedBox(
                          width: 96,
                          child: TextField(
                            controller: e.qty,
                            keyboardType: const TextInputType.numberWithOptions(
                              decimal: true,
                            ),
                            inputFormatters: [
                              FilteringTextInputFormatter.allow(
                                RegExp(r'^\d*[.,]?\d{0,4}'),
                              ),
                            ],
                            decoration: const InputDecoration(
                              labelText: 'Fisik',
                              isDense: true,
                            ),
                            onChanged: (_) => setState(() {}),
                          ),
                        ),
                        IconButton(
                          tooltip: 'Hapus',
                          icon: const Icon(Icons.close),
                          onPressed: () => _hapus(e),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _menyimpan || _daftar.isEmpty ? null : _simpan,
                child: Text(
                  _menyimpan
                      ? 'Menyimpan…'
                      : 'Simpan dokumen SO (${_daftar.length})',
                ),
              ),
            ),
          ),
        ),
      ],
    ),
  );
}
