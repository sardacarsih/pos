import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/hitungan.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

/// Monitoring stok habis (stok sistem <= 0) + koreksi stok lewat stock opname per barang.
class AdminStokPage extends ConsumerStatefulWidget {
  const AdminStokPage({super.key});

  @override
  ConsumerState<AdminStokPage> createState() => _AdminStokPageState();
}

class _AdminStokPageState extends ConsumerState<AdminStokPage> {
  final _q = TextEditingController();
  final _scroll = ScrollController();
  final List<StokItem> _items = [];
  int? _jumlah;
  bool _kontrolStok = true;
  bool _loading = false;
  bool _lagi = false;
  String? _error;
  int _page = 1;
  int _requestId = 0;
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_lagi &&
          !_loading &&
          _error == null &&
          _scroll.position.pixels > _scroll.position.maxScrollExtent - 300) {
        _cari(reset: false);
      }
    });
    _cari(reset: true);
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
          .stokHabis(q: _q.text.trim(), page: page);
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _items.addAll(r.items);
        _page = page;
        _lagi = r.lagi;
        if (r.jumlah != null) _jumlah = r.jumlah;
        _kontrolStok = r.kontrolStok;
      });
    } on ApiException catch (e) {
      if (mounted && requestId == _requestId) setState(() => _error = e.pesan);
    } finally {
      if (mounted && requestId == _requestId) setState(() => _loading = false);
    }
  }

  Future<void> _opname(StokItem p) async {
    final no = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      builder: (c) => _OpnameSheet(item: p, api: ref.read(apiProvider)),
    );
    if (no == null || !mounted) return;
    _pesan('Opname $no tersimpan');
    _cari(reset: true);
  }

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
        if (!_kontrolStok)
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Text(
              'Kontrol stok di kasir nonaktif: daftar ini dapat memuat barang yang tidak dilacak stoknya.',
              style: TextStyle(color: Color(0xFFB26A00)),
            ),
          ),
        if (_jumlah != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text('$_jumlah produk aktif dengan stok ≤ 0'),
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
                    child: Center(child: Text('Tidak ada barang stok habis.')),
                  ),
                for (final p in _items)
                  Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      onTap: () => _opname(p),
                      title: Text(
                        p.nama,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      subtitle: Text('${p.kode} · ${p.kategori}'),
                      trailing: Text(
                        '${_fmtQty(p.stok)} ${p.satuan}',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          color: Color(0xFFB3261E),
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

String _fmtQty(double v) =>
    v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

/// Sheet opname: controller, kunci idempotensi, dan panggilan API hidup di State sheet,
/// jadi controller tidak di-dispose saat animasi tutup dan retry memakai kunci yang sama.
/// Satu kolom input per satuan (dasar + tambahan Katalog); kolom kosong dilewati.
class _OpnameSheet extends StatefulWidget {
  const _OpnameSheet({required this.item, required this.api});
  final StokItem item;
  final ApiClient api;

  @override
  State<_OpnameSheet> createState() => _OpnameSheetState();
}

class _OpnameSheetState extends State<_OpnameSheet> {
  final List<KolomHitung> _kolom = [];
  final List<TextEditingController> _ctrl = [];
  final List<FocusNode> _fokus = [];
  final String _key = newCheckoutKey();
  bool _busy = false;
  bool _memuatSatuan = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _isi(kolomHitungan(widget.item));
  }

  void _isi(List<KolomHitung> kolom) {
    for (final c in _ctrl) {
      c.dispose();
    }
    for (final f in _fokus) {
      f.dispose();
    }
    _kolom
      ..clear()
      ..addAll(kolom);
    _ctrl
      ..clear()
      ..addAll([
        for (final k in _kolom)
          TextEditingController(text: k.qty.replaceAll('.', ',')),
      ]);
    _fokus
      ..clear()
      ..addAll([for (final _ in _kolom) FocusNode()]);
  }

  List<KolomHitung> _draf() => [
    for (var i = 0; i < _kolom.length; i++)
      KolomHitung(
        satuan: _kolom[i].satuan,
        isi: _kolom[i].isi,
        qty: _ctrl[i].text,
      ),
  ];

  @override
  void dispose() {
    for (final c in _ctrl) {
      c.dispose();
    }
    for (final f in _fokus) {
      f.dispose();
    }
    super.dispose();
  }

  /// Faktor/satuan berubah sejak sheet dibuka: ambil ulang Katalog, pertahankan isian.
  Future<void> _muatUlangSatuan() async {
    if (_memuatSatuan || _busy) return;
    setState(() {
      _memuatSatuan = true;
      _error = null;
    });
    try {
      final r = await widget.api.stokHabis(q: widget.item.kode);
      final cocok = r.items.where((p) => p.id == widget.item.id);
      if (cocok.isEmpty) {
        throw ApiException(
          'Produk ${widget.item.nama} tidak ditemukan saat memuat ulang satuan.',
        );
      }
      final lama = {
        for (final k in _draf()) k.satuan.trim().toUpperCase(): k.qty,
      };
      _isi(kolomHitungan(cocok.first, qtyLama: lama));
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } catch (_) {
      if (mounted) setState(() => _error = 'Gagal memuat ulang satuan produk.');
    } finally {
      if (mounted) setState(() => _memuatSatuan = false);
    }
  }

  Future<void> _simpan() async {
    if (_busy) return;
    final draf = _draf();
    final st = statusHitungan(draf);
    if (st.galat != null) return setState(() => _error = st.galat);
    if (st.nilai == null) {
      return setState(
        () => _error = 'Isi minimal satu jumlah; isi 0 untuk menyatakan stok fisik kosong.',
      );
    }
    final p = widget.item;
    final total = st.nilai!;
    final beda = double.parse((total - p.stok).toStringAsFixed(4));
    final lanjut = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Koreksi stok?'),
        content: Text(
          '${p.nama}\n${rincianHitungan(draf)} = stok fisik ${_fmtQty(total)} '
          '(selisih ${_fmtQty(beda)} dari sistem ${_fmtQty(p.stok)} ${p.satuan}).\n'
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
      _busy = true;
      _error = null;
    });
    try {
      final no = await widget.api.opnameStok(
        p.id,
        payloadHitungan(draf),
        idempotencyKey: _key,
      );
      if (mounted) Navigator.pop(context, no);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final p = widget.item;
    final st = statusHitungan(_draf());
    return Padding(
      padding: EdgeInsets.fromLTRB(
        16,
        16,
        16,
        16 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(p.nama, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 4),
          Text('${p.kode} · stok sistem ${_fmtQty(p.stok)} ${p.satuan}'),
          const SizedBox(height: 12),
          for (var i = 0; i < _kolom.length; i++)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: TextField(
                controller: _ctrl[i],
                focusNode: _fokus[i],
                autofocus: i == 0,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(
                    RegExp(r'^\d*[.,]?\d{0,4}'),
                  ),
                ],
                onChanged: (_) => setState(() {}),
                decoration: InputDecoration(
                  labelText: _kolom.length == 1
                      ? 'Qty fisik'
                      : 'Qty ${_kolom[i].satuan}',
                  errorText: i == 0 ? _error : null,
                  helperText: _kolom[i].isi == 1
                      ? null
                      : '1 ${_kolom[i].satuan} = ${teksDesimal(_kolom[i].isi)} ${p.satuan}',
                ),
              ),
            ),
          if (_error != null && _error!.contains('muat ulang satuan'))
            OutlinedButton(
              onPressed: _memuatSatuan || _busy ? null : _muatUlangSatuan,
              child: Text(
                _memuatSatuan ? 'Memuat satuan…' : 'Muat ulang satuan produk',
              ),
            ),
          if (st.nilai != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Text(
                'Total fisik ${_fmtQty(st.nilai!)} ${p.satuan} · '
                'selisih ${_fmtQty(double.parse((st.nilai! - p.stok).toStringAsFixed(4)))}',
              ),
            ),
          FilledButton(
            onPressed: _busy ? null : _simpan,
            child: Text(_busy ? 'Menyimpan…' : 'Simpan opname'),
          ),
        ],
      ),
    );
  }
}
