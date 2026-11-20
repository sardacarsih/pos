import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import 'admin_dokumen_editor_page.dart';
import 'admin_opname_baru_page.dart';

final _formatQty = NumberFormat('#,##0.####', 'id_ID');
String fmtQty(double v) => _formatQty.format(v);

/// Riwayat tetap menggunakan tab Opname dari halaman Produk admin.
class AdminOpnamePage extends ConsumerStatefulWidget {
  const AdminOpnamePage({super.key});

  @override
  ConsumerState<AdminOpnamePage> createState() => _AdminOpnamePageState();
}

class _AdminOpnamePageState extends ConsumerState<AdminOpnamePage> {
  final _items = <SoRingkas>[];
  int _tahun = DateTime.now().year.clamp(2000, 2100);
  int _page = 1, _requestId = 0;
  bool _loading = true, _lagi = false;
  String? _error, _tersimpan;

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
      final r = await ref.read(apiProvider).daftarSo(tahun: _tahun, page: page);
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

  Future<void> _gantiTahun() async {
    final tahun = await showDialog<int>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Tahun dokumen'),
        content: SizedBox(
          width: 320,
          height: 272,
          child: YearPicker(
            firstDate: DateTime(2000),
            lastDate: DateTime(2100),
            selectedDate: DateTime(_tahun),
            onChanged: (tanggal) => Navigator.pop(c, tanggal.year),
          ),
        ),
      ),
    );
    if (tahun == null || tahun == _tahun || !mounted) return;
    setState(() => _tahun = tahun);
    await _muat();
  }

  Future<void> _baru() async {
    final dokumen = await Navigator.of(context).push<SoDokumen>(
      MaterialPageRoute(builder: (_) => const AdminOpnameBaruPage()),
    );
    if (dokumen == null || !mounted) return;
    final tanggal = DateTime.tryParse(dokumen.tanggal);
    setState(() {
      _tahun = tanggal?.year.clamp(2000, 2100) ?? _tahun;
      _tersimpan = dokumen.nomor;
    });
    await _muat();
  }

  Future<void> _detail(String nomor) async {
    await Navigator.of(context)
        .push<void>(MaterialPageRoute(builder: (_) => _OpnameDetail(nomor)));
    if (mounted) await _muat(); // dokumen mungkin diubah atau dihapus
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: Wrap(
            spacing: 12,
            runSpacing: 4,
            crossAxisAlignment: WrapCrossAlignment.center,
            alignment: WrapAlignment.spaceBetween,
            children: [
              OutlinedButton.icon(
                onPressed: _gantiTahun,
                icon: const Icon(Icons.calendar_today_outlined, size: 18),
                label: Text('Tahun $_tahun'),
              ),
              FilledButton.icon(
                onPressed: _baru,
                icon: const Icon(Icons.add),
                label: const Text('Opname baru'),
              ),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _muat,
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                  sliver: SliverList.list(
                    children: [
                      if (_tersimpan case final nomor?)
                        Card(
                          color: tema.colorScheme.primaryContainer,
                          child: Padding(
                            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Semantics(
                                  liveRegion: true,
                                  child: Text(
                                    'Dokumen $nomor tersimpan.',
                                    style: TextStyle(
                                      color:
                                          tema.colorScheme.onPrimaryContainer,
                                    ),
                                  ),
                                ),
                                TextButton(
                                  onPressed: () => _detail(nomor),
                                  child: const Text('Buka dokumen'),
                                ),
                              ],
                            ),
                          ),
                        ),
                      Text(
                        'Riwayat dokumen',
                        style: tema.textTheme.titleMedium,
                      ),
                      const SizedBox(height: 8),
                      if (!_loading && _error == null && _items.isEmpty)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 24),
                          child: Text('Belum ada dokumen pada tahun $_tahun.'),
                        ),
                    ],
                  ),
                ),
                SliverPadding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  sliver: SliverList.builder(
                    itemCount: _items.length,
                    itemBuilder: (context, i) {
                      final s = _items[i];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          title: Text(
                            s.nomor,
                            style: tema.textTheme.titleSmall,
                          ),
                          subtitle: Text(
                            '${s.tanggal} · ${s.jumlahItem} barang\n'
                            'Nilai selisih ${rupiah(s.nilaiSelisih.round())}',
                          ),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => _detail(s.nomor),
                        ),
                      );
                    },
                  ),
                ),
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  sliver: SliverToBoxAdapter(
                    child: _loading
                        ? const LoadingKopkar()
                        : _error != null
                        ? _RiwayatGalat(
                            pesan: _error!,
                            ulang: () => _muat(lanjut: _items.isNotEmpty),
                          )
                        : _lagi
                        ? OutlinedButton(
                            onPressed: () => _muat(lanjut: true),
                            child: const Text('Muat lebih banyak'),
                          )
                        : const SizedBox.shrink(),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _RiwayatGalat extends StatelessWidget {
  const _RiwayatGalat({required this.pesan, required this.ulang});
  final String pesan;
  final VoidCallback ulang;

  @override
  Widget build(BuildContext context) => Card(
    color: Theme.of(context).colorScheme.errorContainer,
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Semantics(liveRegion: true, child: Text(pesan)),
          TextButton(onPressed: ulang, child: const Text('Coba lagi')),
        ],
      ),
    ),
  );
}

class _OpnameDetail extends ConsumerStatefulWidget {
  const _OpnameDetail(this.nomor);
  final String nomor;

  @override
  ConsumerState<_OpnameDetail> createState() => _OpnameDetailState();
}

class _OpnameDetailState extends ConsumerState<_OpnameDetail> {
  late Future<SoDokumen> _detail = _muat();
  SoDokumen? _dokumen;

  Future<SoDokumen> _muat() async {
    final d = await ref.read(apiProvider).detailSo(widget.nomor);
    _dokumen = d;
    return d;
  }

  Future<void> _ubah() async {
    final d = _dokumen;
    if (d == null) return;
    if (d.items.any((b) => b.produkId == null)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Ada produk yang sudah tidak ada; dokumen tidak bisa diubah.')),
      );
      return;
    }
    final api = ref.read(apiProvider);
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => AdminDokumenEditorPage(
          judul: 'Ubah ${d.nomor}',
          labelQty: 'Fisik (satuan dasar)',
          awal: [
            for (final b in d.items)
              BarisEditor(
                produkId: b.produkId!,
                kode: b.kode,
                nama: b.nama,
                satuan: '',
                qty: _qtyTeks(b.fisik),
              ),
          ],
          simpan: (baris, alasan, key) async {
            await api.ubahSo(
              d.nomor,
              versi: d.versi,
              alasan: alasan,
              items: [for (final b in baris) (produkId: b.produkId, qty: b.qtyKirim!)],
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
            const Text('Dokumen dihapus permanen dan stok sistem kembali seperti sebelum dokumen ini.'),
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
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Alasan wajib diisi.')));
      return;
    }
    try {
      await ref.read(apiProvider).hapusSo(
        d.nomor,
        versi: d.versi,
        alasan: teks,
        idempotencyKey: newCheckoutKey(),
      );
      if (mounted) Navigator.of(context).pop();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.pesan)));
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(widget.nomor),
      actions: [
        IconButton(tooltip: 'Ubah dokumen', icon: const Icon(Icons.edit_outlined), onPressed: _ubah),
        IconButton(tooltip: 'Hapus dokumen', icon: const Icon(Icons.delete_outline), onPressed: _hapus),
      ],
    ),
    body: SafeArea(
      child: FutureBuilder<SoDokumen>(
        future: _detail,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: LoadingKopkar());
          }
          if (snapshot.hasError) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: _RiwayatGalat(
                pesan: snapshot.error.toString(),
                ulang: () => setState(() => _detail = _muat()),
              ),
            );
          }
          final d = snapshot.requireData;
          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: d.items.length + 1,
            itemBuilder: (context, index) {
              final tema = Theme.of(context);
              if (index == 0) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(
                    '${d.tanggal} · ${d.items.length} barang',
                    style: tema.textTheme.titleMedium,
                  ),
                );
              }
              final b = d.items[index - 1];
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(b.nama, style: tema.textTheme.titleSmall),
                      Text(b.kode),
                      const SizedBox(height: 8),
                      OpnameAngka(
                        sistem: b.sistem,
                        fisik: b.fisik,
                        selisih: b.selisih,
                      ),
                    ],
                  ),
                ),
              );
            },
          );
        },
      ),
    ),
  );
}

String _qtyTeks(double v) =>
    v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

class OpnameAngka extends StatelessWidget {
  const OpnameAngka({
    super.key,
    required this.sistem,
    required this.fisik,
    required this.selisih,
  });
  final double sistem;
  final double? fisik, selisih;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final beda = selisih;
    final jumlahFisik = fisik;
    final nilai = [
      ('Sistem', fmtQty(sistem)),
      ('Fisik', jumlahFisik == null ? 'Belum dihitung' : fmtQty(jumlahFisik)),
      ('Selisih', beda == null ? '—' : '${beda > 0 ? '+' : ''}${fmtQty(beda)}'),
    ];
    return Wrap(
      spacing: 18,
      runSpacing: 6,
      children: [
        for (final (label, angka) in nilai)
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: tema.textTheme.labelSmall),
              Text(angka, style: tema.textTheme.titleSmall),
            ],
          ),
      ],
    );
  }
}
