import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'bayar_page.dart';

/// Muat faktur milik kasir lalu tampilkan sebagai detail (tanpa mengubah keranjang).
Future<void> bukaDetailTransaksi(
  BuildContext context,
  WidgetRef ref,
  String no,
) async {
  final messenger = ScaffoldMessenger.of(context);
  final nav = Navigator.of(context);
  try {
    final f = await ref.read(apiProvider).fakturKasir(no);
    await nav.push(
      MaterialPageRoute(builder: (_) => StrukPage(faktur: f, dariRiwayat: true)),
    );
  } on ApiException catch (e) {
    messenger.showSnackBar(
      SnackBar(
        content: Text(e.status == 404 ? 'Faktur tidak ditemukan' : e.pesan),
      ),
    );
  }
}

String _fmt(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// Riwayat transaksi kasir per tanggal. [aktif] berubah true saat tab dibuka -> dimuat ulang.
class RiwayatPage extends ConsumerStatefulWidget {
  const RiwayatPage({super.key, required this.aktif});
  final bool aktif;
  @override
  ConsumerState<RiwayatPage> createState() => _RiwayatPageState();
}

class _RiwayatPageState extends ConsumerState<RiwayatPage> {
  DateTime? _hari; // tanggal server (batas atas); null sebelum pertama dimuat
  DateTime? _tgl; // null = hari ini server
  List<TransaksiTerakhir> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void didUpdateWidget(covariant RiwayatPage old) {
    super.didUpdateWidget(old);
    if (widget.aktif && !old.aktif) _muat();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref
          .read(apiProvider)
          .riwayat(tanggal: _tgl == null ? null : _fmt(_tgl!));
      if (!mounted) return;
      final t = DateTime.parse(r.tanggal);
      setState(() {
        _items = r.items;
        _tgl = t;
        _hari ??= t;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _geser(int hari) {
    final t = (_tgl ?? DateTime.now()).add(Duration(days: hari));
    if (_hari != null && t.isAfter(_hari!)) return;
    setState(() => _tgl = t);
    _muat();
  }

  Future<void> _pilih() async {
    final batas = _hari ?? DateTime.now();
    final t = await showDatePicker(
      context: context,
      initialDate: _tgl ?? batas,
      firstDate: DateTime(batas.year - 2),
      lastDate: batas,
    );
    if (t == null || !mounted) return;
    setState(() => _tgl = t);
    _muat();
  }

  @override
  Widget build(BuildContext context) {
    final tgl = _tgl;
    final hariIni = tgl != null && _hari != null && _fmt(tgl) == _fmt(_hari!);
    return TropicalScaffold(
      title: 'Riwayat',
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
            child: Row(
              children: [
                IconButton(
                  tooltip: 'Hari sebelumnya',
                  icon: const Icon(Icons.chevron_left),
                  onPressed: () => _geser(-1),
                ),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _pilih,
                    icon: const Icon(Icons.calendar_today, size: 18),
                    label: Text(
                      tgl == null
                          ? 'Memuat…'
                          : '${_fmt(tgl)}${hariIni ? ' (hari ini)' : ''}',
                    ),
                  ),
                ),
                IconButton(
                  tooltip: 'Hari berikutnya',
                  icon: const Icon(Icons.chevron_right),
                  onPressed: hariIni ? null : () => _geser(1),
                ),
              ],
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _muat,
              child: _loading && _items.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.all(24),
                      children: [
                        Text(_error!, textAlign: TextAlign.center),
                        TextButton(
                          onPressed: _muat,
                          child: const Text('Coba lagi'),
                        ),
                      ],
                    )
                  : _items.isEmpty
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 100),
                        Center(child: Text('Tidak ada transaksi pada tanggal ini.')),
                      ],
                    )
                  : ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
                      children: [
                        for (final t in _items)
                          Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              onTap: () => bukaDetailTransaksi(context, ref, t.no),
                              title: Text(
                                t.pelanggan.isEmpty ? t.no : t.pelanggan,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              subtitle: Padding(
                                padding: const EdgeInsets.only(top: 4),
                                child: Wrap(
                                  spacing: 8,
                                  crossAxisAlignment: WrapCrossAlignment.center,
                                  children: [
                                    StatePill(text: t.jenisBayar),
                                    Text('${t.jam} · ${t.no}'),
                                  ],
                                ),
                              ),
                              trailing: Text(
                                rupiah(t.total),
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
            ),
          ),
        ],
      ),
    );
  }
}
