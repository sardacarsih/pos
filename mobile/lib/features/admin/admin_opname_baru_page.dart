import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/models.dart';
import '../../core/providers.dart';
import '../kasir/kasir_page.dart' show ScanPage;
import 'admin_opname_page.dart' show fmtQty, OpnameAngka;

const _maksBarang = 200;
final _polaJumlah = RegExp(r'^(?:\d+(?:\.\d{0,4})?|\.\d{1,4})$');

String? _jumlahFisik(String teks) {
  final nilai = teks.trim().replaceAll(',', '.');
  if (!_polaJumlah.hasMatch(nilai)) return null;
  final jumlah = double.tryParse(nilai);
  return jumlah != null && jumlah <= 1000000000 ? nilai : null;
}

double _selisih(double fisik, double sistem) =>
    double.parse((fisik - sistem).toStringAsFixed(4));

typedef _BarisKirim = ({int produkId, String qty});
typedef _Permintaan = ({String kunci, List<_BarisKirim> items});

class _Entri {
  _Entri(this.item);

  final StokItem item;
  String? qty;

  double? get fisik => qty == null ? null : double.parse(qty!);
  double? get selisih {
    final jumlah = fisik;
    return jumlah == null ? null : _selisih(jumlah, item.stok);
  }
}

enum _Filter {
  semua('Semua'),
  belum('Belum dihitung'),
  beda('Berselisih');

  const _Filter(this.label);
  final String label;
}

class AdminOpnameBaruPage extends ConsumerStatefulWidget {
  const AdminOpnameBaruPage({super.key});

  @override
  ConsumerState<AdminOpnameBaruPage> createState() =>
      _AdminOpnameBaruPageState();
}

class _AdminOpnameBaruPageState extends ConsumerState<AdminOpnameBaruPage> {
  final _q = TextEditingController();
  final _daftar = <_Entri>[];
  List<StokItem> _hasil = [];
  Timer? _debounce;
  int _requestId = 0, _halaman = 1;
  bool _lagi = false, _sudahCari = false, _mencari = false;
  bool _menyimpan = false, _tinjau = false, _menutup = false;
  _Filter _filter = _Filter.semua;
  _Permintaan? _tertunda;
  String? _galatCari, _galatHitung, _galatSimpan;

  @override
  void dispose() {
    _debounce?.cancel();
    _q.dispose();
    super.dispose();
  }

  void _resetCari() {
    _debounce?.cancel();
    ++_requestId;
    _hasil = [];
    _mencari = false;
    _lagi = false;
    _sudahCari = false;
    _galatCari = null;
  }

  void _ubahPencarian(String teks) {
    setState(_resetCari);
    if (teks.trim().isNotEmpty) {
      _debounce = Timer(const Duration(milliseconds: 350), _cari);
    }
  }

  Future<void> _cari({bool pilihBarcode = false, bool lanjut = false}) async {
    _debounce?.cancel();
    final teks = _q.text.trim();
    if (teks.isEmpty) {
      if (pilihBarcode) {
        setState(
          () => _galatCari = 'Masukkan nama, kode, atau barcode barang.',
        );
      }
      return;
    }
    if (lanjut && _mencari) return;
    final halaman = lanjut ? _halaman + 1 : 1;
    setState(() {
      if (!lanjut) _resetCari();
      _mencari = true;
      _galatCari = null;
    });
    final id = ++_requestId;
    try {
      final r = await ref.read(apiProvider).cariStok(q: teks, page: halaman);
      if (!mounted || id != _requestId) return;
      final persis = r.items.where((p) => p.barcode == teks);
      if (pilihBarcode && !lanjut && !r.lagi && persis.length == 1) {
        setState(() => _mencari = false);
        await _pilih(persis.single);
        return;
      }
      setState(() {
        _hasil = lanjut ? [..._hasil, ...r.items] : r.items;
        _halaman = halaman;
        _lagi = r.lagi;
        _sudahCari = true;
      });
    } on ApiException catch (e) {
      if (mounted && id == _requestId) setState(() => _galatCari = e.pesan);
    } finally {
      if (mounted && id == _requestId) setState(() => _mencari = false);
    }
  }

  Future<void> _scan() async {
    FocusScope.of(context).unfocus();
    setState(() {
      _q.clear();
      _resetCari();
    });
    final kode = await Navigator.of(context)
        .push<String>(MaterialPageRoute(builder: (_) => const ScanPage()));
    if (kode == null || !mounted) return;
    _q.text = kode;
    await _cari(pilihBarcode: true);
  }

  Future<void> _pilih(StokItem item) async {
    final index = _daftar.indexWhere((e) => e.item.id == item.id);
    if (index < 0 && _daftar.length >= _maksBarang) {
      setState(() => _galatHitung = 'Maksimal 200 barang per dokumen.');
      return;
    }
    final entri = index < 0 ? _Entri(item) : _daftar[index];
    setState(() {
      _resetCari();
      _q.clear();
      _galatHitung = null;
      _filter = _Filter.semua;
      if (index < 0) _daftar.insert(0, entri);
    });
    await _edit(entri);
  }

  Future<void> _edit(_Entri entri) async {
    FocusScope.of(context).unfocus();
    final hasil = await showDialog<String>(
      context: context,
      builder: (_) => _EditorJumlah(entri: entri),
    );
    if (!mounted || hasil == null) return;
    setState(() {
      entri.qty = hasil;
      _galatHitung = null;
    });
  }

  void _bukaTinjauan() {
    for (final e in _daftar) {
      if (e.qty != null) continue;
      setState(() {
        _filter = _Filter.semua;
        _galatHitung = 'Isi jumlah fisik untuk ${e.item.nama}.';
      });
      unawaited(_edit(e));
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() {
      _resetCari();
      _q.clear();
      _galatSimpan = null;
      _tinjau = true;
    });
  }

  Future<void> _simpan() async {
    if (_menyimpan || _daftar.isEmpty || _daftar.any((e) => e.qty == null)) {
      return;
    }
    final kirim =
        _tertunda ??
        (
          kunci: newCheckoutKey(),
          items: [for (final e in _daftar) (produkId: e.item.id, qty: e.qty!)],
        );
    setState(() {
      _menyimpan = true;
      _galatSimpan = null;
    });
    try {
      final dokumen = await ref
          .read(apiProvider)
          .simpanSo(kirim.items, idempotencyKey: kirim.kunci);
      if (dokumen.nomor.isEmpty) {
        throw ApiException('Nomor dokumen belum diterima.');
      }
      if (mounted) Navigator.of(context).pop(dokumen);
    } catch (e) {
      if (!mounted) return;
      final status = e is ApiException ? e.status : null;
      setState(() {
        if (status == null || status < 400 || status >= 500 || status == 408) {
          _tertunda = kirim;
        }
        _galatSimpan = e is ApiException
            ? e.pesan
            : 'Gagal menerima hasil simpan.';
      });
    } finally {
      if (mounted) setState(() => _menyimpan = false);
    }
  }

  Future<void> _keluar() async {
    if (_menutup || _menyimpan) return;
    _menutup = true;
    final keluar =
        _daftar.isEmpty ||
        await showDialog<bool>(
              context: context,
              builder: (c) => AlertDialog(
                title: const Text('Batalkan opname?'),
                content: Text(
                  _tertunda == null
                      ? 'Hitungan yang belum tersimpan akan dibuang.'
                      : 'Hasil simpan belum terkonfirmasi. Mengakhiri sesi akan membuang draf dari layar ini.',
                ),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(c, false),
                    child: const Text('Lanjutkan'),
                  ),
                  FilledButton(
                    onPressed: () => Navigator.pop(c, true),
                    child: const Text('Buang draf'),
                  ),
                ],
              ),
            ) ==
            true;
    _menutup = false;
    if (keluar && mounted) Navigator.of(context).pop();
  }

  void _kembali() {
    if (_menyimpan) return;
    if (_tinjau && _tertunda == null) {
      setState(() => _tinjau = false);
    } else {
      unawaited(_keluar());
    }
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final dihitung = _daftar.where((e) => e.qty != null).length;
    final berselisih = _daftar.where((e) => (e.selisih ?? 0) != 0).length;
    final tampil = switch (_filter) {
      _Filter.semua => _daftar,
      _Filter.belum => _daftar.where((e) => e.qty == null).toList(),
      _Filter.beda => _daftar.where((e) => (e.selisih ?? 0) != 0).toList(),
    };
    final baris = _tinjau ? _daftar : tampil;

    return PopScope<SoDokumen>(
      canPop: !_menyimpan && !_tinjau && _daftar.isEmpty,
      onPopInvokedWithResult: (keluar, _) {
        if (!keluar) _kembali();
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(_tinjau ? 'Tinjau opname' : 'Opname baru'),
          leading: IconButton(
            tooltip: 'Kembali',
            icon: const BackButtonIcon(),
            onPressed: _menyimpan ? null : _kembali,
          ),
          actions: [
            if (_tinjau && !_menyimpan)
              TextButton(onPressed: _keluar, child: const Text('Batalkan')),
          ],
        ),
        body: SafeArea(
          bottom: false,
          child: Column(
            children: [
              if (!_tinjau)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
                  child: TextField(
                    controller: _q,
                    textInputAction: TextInputAction.search,
                    onChanged: _ubahPencarian,
                    onSubmitted: (_) => _cari(pilihBarcode: true),
                    decoration: const InputDecoration(
                      labelText: 'Cari nama, kode, atau barcode',
                      prefixIcon: Icon(Icons.search),
                    ),
                  ),
                ),
              Expanded(
                child: CustomScrollView(
                  key: ValueKey(_tinjau),
                  keyboardDismissBehavior:
                      ScrollViewKeyboardDismissBehavior.onDrag,
                  slivers: [
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                      sliver: SliverList.list(
                        children: _tinjau
                            ? [
                                Text(
                                  '${_daftar.length} barang · $berselisih berselisih',
                                  style: tema.textTheme.titleMedium,
                                ),
                                const SizedBox(height: 8),
                                const Text(
                                  'Selisih masih perkiraan. Menyimpan dokumen akan memperbarui stok sistem.',
                                ),
                              ]
                            : _kepalaHitung(tema, baris.isEmpty),
                      ),
                    ),
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                      sliver: SliverList.builder(
                        itemCount: baris.length,
                        itemBuilder: (context, i) =>
                            _barisHitung(baris[i], tema),
                      ),
                    ),
                  ],
                ),
              ),
              if (MediaQuery.viewInsetsOf(context).bottom == 0 || _tinjau)
                Material(
                  color: tema.colorScheme.surface,
                  child: SafeArea(
                    top: false,
                    minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: _tinjau
                          ? [
                              if (_galatSimpan case final galat?)
                                _PesanOpname(
                                  _tertunda == null
                                      ? galat
                                      : 'Hasil simpan belum terkonfirmasi. Coba simpan lagi untuk memeriksa dokumen yang sama.\n$galat',
                                ),
                              if (_menyimpan)
                                const LinearProgressIndicator(
                                  semanticsLabel: 'Menyimpan dokumen',
                                ),
                              FilledButton(
                                onPressed: _menyimpan ? null : _simpan,
                                child: Text(
                                  _menyimpan
                                      ? 'Menyimpan…'
                                      : _tertunda == null
                                      ? 'Simpan dokumen'
                                      : 'Coba simpan lagi',
                                ),
                              ),
                              if (_tertunda == null)
                                OutlinedButton(
                                  onPressed: _menyimpan ? null : _kembali,
                                  child: const Text('Kembali ke hitungan'),
                                ),
                            ]
                          : [
                              Semantics(
                                liveRegion: true,
                                child: Text(
                                  '$dihitung/${_daftar.length} dihitung · $berselisih berselisih',
                                ),
                              ),
                              const SizedBox(height: 6),
                              FilledButton.icon(
                                onPressed: _scan,
                                icon: const Icon(Icons.qr_code_scanner),
                                label: const Text('Scan barcode'),
                              ),
                              OutlinedButton(
                                onPressed: _daftar.isEmpty
                                    ? null
                                    : _bukaTinjauan,
                                child: const Text('Tinjau dan simpan'),
                              ),
                            ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  List<Widget> _kepalaHitung(ThemeData tema, bool kosong) => [
    if (_mencari)
      const LinearProgressIndicator(semanticsLabel: 'Mencari barang'),
    if (_galatCari case final galat?)
      _PesanOpname(
        galat,
        cobaLagi: _q.text.trim().isEmpty
            ? null
            : () => _cari(pilihBarcode: true, lanjut: _sudahCari),
      ),
    for (final p in _hasil)
      ListTile(
        contentPadding: EdgeInsets.zero,
        title: Text(p.nama),
        subtitle: Text('${p.kode} · ${fmtQty(p.stok)} ${p.satuan}'),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => _pilih(p),
      ),
    if (_sudahCari && _hasil.isEmpty && !_mencari && _galatCari == null)
      const Padding(
        padding: EdgeInsets.only(bottom: 12),
        child: Text('Barang tidak ditemukan.'),
      ),
    if (_lagi && _galatCari == null)
      OutlinedButton(
        onPressed: _mencari ? null : () => _cari(lanjut: true),
        child: const Text('Muat lagi'),
      ),
    if (_galatHitung case final galat?) _PesanOpname(galat),
    Text('Daftar hitung', style: tema.textTheme.titleMedium),
    if (_daftar.isNotEmpty)
      Wrap(
        spacing: 6,
        children: [
          for (final f in _Filter.values)
            ChoiceChip(
              label: Text(f.label),
              selected: _filter == f,
              onSelected: (_) => setState(() => _filter = f),
            ),
        ],
      ),
    if (kosong)
      Padding(
        padding: const EdgeInsets.symmetric(vertical: 20),
        child: Text(
          _daftar.isEmpty
              ? 'Scan barcode atau cari barang untuk mulai menghitung.'
              : 'Tidak ada barang pada filter ini.',
        ),
      ),
  ];

  Widget _barisHitung(_Entri e, ThemeData tema) => Card(
    key: ValueKey(e.item.id),
    margin: const EdgeInsets.only(bottom: 8),
    child: Padding(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(e.item.nama, style: tema.textTheme.titleSmall),
          Text('${e.item.kode} · ${e.item.satuan}'),
          const SizedBox(height: 8),
          OpnameAngka(sistem: e.item.stok, fisik: e.fisik, selisih: e.selisih),
          if (!_tinjau)
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                TextButton(
                  onPressed: () => _edit(e),
                  child: Text(e.qty == null ? 'Isi jumlah' : 'Ubah jumlah'),
                ),
                IconButton(
                  tooltip: 'Hapus ${e.item.nama}',
                  onPressed: () => setState(() {
                    _daftar.remove(e);
                    _galatHitung = null;
                  }),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
        ],
      ),
    ),
  );
}

class _PesanOpname extends StatelessWidget {
  const _PesanOpname(this.pesan, {this.cobaLagi});
  final String pesan;
  final VoidCallback? cobaLagi;

  @override
  Widget build(BuildContext context) => Card(
    color: Theme.of(context).colorScheme.errorContainer,
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Semantics(liveRegion: true, child: Text(pesan)),
          if (cobaLagi != null)
            TextButton(onPressed: cobaLagi, child: const Text('Coba lagi')),
        ],
      ),
    ),
  );
}

class _EditorJumlah extends StatefulWidget {
  const _EditorJumlah({required this.entri});
  final _Entri entri;

  @override
  State<_EditorJumlah> createState() => _EditorJumlahState();
}

class _EditorJumlahState extends State<_EditorJumlah> {
  final _form = GlobalKey<FormState>();
  late final _qty = TextEditingController(
    text: widget.entri.qty?.replaceAll('.', ',') ?? '',
  );

  @override
  void dispose() {
    _qty.dispose();
    super.dispose();
  }

  void _simpan() {
    if (_form.currentState?.validate() != true) return;
    Navigator.pop(context, _jumlahFisik(_qty.text));
  }

  @override
  Widget build(BuildContext context) {
    final barang = widget.entri.item;
    final jumlah = _jumlahFisik(_qty.text);
    final beda = jumlah == null
        ? null
        : _selisih(double.parse(jumlah), barang.stok);
    return AlertDialog(
      scrollable: true,
      title: Text(barang.nama),
      content: Form(
        key: _form,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '${barang.kode} · Sistem ${fmtQty(barang.stok)} ${barang.satuan}',
            ),
            const SizedBox(height: 12),
            TextFormField(
              autofocus: true,
              controller: _qty,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              textInputAction: TextInputAction.done,
              onChanged: (_) => setState(() {}),
              onFieldSubmitted: (_) => _simpan(),
              decoration: InputDecoration(
                labelText: 'Jumlah fisik',
                suffixText: barang.satuan,
              ),
              validator: (teks) => _jumlahFisik(teks ?? '') == null
                  ? 'Masukkan jumlah 0–1 miliar, hingga 4 desimal.'
                  : null,
            ),
            if (beda != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text('Selisih ${beda > 0 ? '+' : ''}${fmtQty(beda)}'),
              ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Batal'),
        ),
        FilledButton(onPressed: _simpan, child: const Text('Simpan hitungan')),
      ],
    );
  }
}
