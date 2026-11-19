import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../anggota/pinjaman_page.dart' show labelStatusPinjaman;

const _labelAksi = {
  'verifikasi': 'Verifikasi',
  'setujui': 'Setujui',
  'tolak': 'Tolak',
  'cairkan': 'Cairkan',
};

/// Pinjaman untuk pengurus (ADMIN dengan hak tahap): proses pengajuan, antrian dana, dan laporan pinjaman aktif.
/// Plafon dana dan hak tahap diatur di web.
class AdminPinjamanPage extends ConsumerStatefulWidget {
  const AdminPinjamanPage({super.key});
  @override
  ConsumerState<AdminPinjamanPage> createState() => _AdminPinjamanPageState();
}

class _AdminPinjamanPageState extends ConsumerState<AdminPinjamanPage>
    with SingleTickerProviderStateMixin {
  late final TabController _tab = TabController(length: 3, vsync: this)
    ..addListener(() {
      if (_tab.indexIsChanging) return;
      setState(() {});
      if (_tab.index == 2 && _laporan == null && _errorLaporan == null) {
        _muatLaporan();
      }
    });

  List<PengajuanPinjaman> _items = [];
  List<String> _hak = [];
  int _userId = 0;
  String? _status = 'DIAJUKAN';
  AntrianPinjaman? _antrian;
  LaporanPinjaman? _laporan;
  String? _error;
  String? _errorLaporan;
  bool _loading = false, _loadingLaporan = false, _sibuk = false;
  final _cari = TextEditingController();

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void dispose() {
    _tab.dispose();
    _cari.dispose();
    super.dispose();
  }

  Future<void> _muat() async {
    if (_loading) return;
    final api = ref.read(apiProvider);
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await api.daftarPinjaman(status: _status);
      if (!mounted) return;
      setState(() {
        _items = d.items;
        _hak = d.hak;
        _userId = d.userId;
      });
      final a = await api.antrianPinjaman();
      if (mounted) setState(() => _antrian = a);
    } on ApiException catch (e) {
      if (mounted) {
        setState(() {
          _antrian = null;
          _error = e.pesan;
        });
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _muatLaporan() async {
    if (_loadingLaporan) return;
    final cari = _cari.text.trim();
    setState(() {
      _loadingLaporan = true;
      _errorLaporan = null;
      _laporan = null;
    });
    try {
      final laporan = await ref
          .read(apiProvider)
          .laporanPinjaman(q: cari.isEmpty ? null : cari);
      if (mounted) setState(() => _laporan = laporan);
    } on ApiException catch (e) {
      if (mounted) setState(() => _errorLaporan = e.pesan);
    } finally {
      if (mounted) setState(() => _loadingLaporan = false);
    }
  }

  int? get _kepala => _antrian?.items.firstOrNull?.id;

  Future<void> _proses(PengajuanPinjaman p, String aksi) async {
    final catatan = await showDialog<String>(
      context: context,
      builder: (_) => _DialogAksi(p: p, aksi: aksi),
    );
    if (catatan == null || !mounted) return;
    setState(() => _sibuk = true);
    try {
      final hasil = await ref
          .read(apiProvider)
          .prosesPinjaman(
            p.id,
            aksi,
            catatan: catatan.isEmpty ? null : catatan,
            idempotencyKey: const Uuid().v4(),
          );
      if (aksi == 'cairkan' &&
          mounted &&
          (_laporan != null || _tab.index == 2)) {
        await _muatLaporan();
      }
      _pesan(
        aksi == 'cairkan'
            ? 'Dicairkan sebagai $hasil. Catat kas lewat web: Kas & Bank → Tarik transaksi.'
            : 'Pengajuan #${p.id}: ${_labelAksi[aksi]!.toLowerCase()} berhasil',
      );
      await _muat();
    } on ApiException catch (e) {
      _pesan(e.pesan);
    } finally {
      if (mounted) setState(() => _sibuk = false);
    }
  }

  void _pesan(String s) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(s)));
    }
  }

  Widget _kartu(PengajuanPinjaman p, {bool hanyaCair = false}) {
    var aksi = aksiPinjaman(p, _hak, _userId, p.id == _kepala);
    if (hanyaCair) aksi = aksi.where((a) => a == 'cairkan').toList();
    final sisa = _antrian?.sisa;
    final kurang = p.id == _kepala && sisa != null && p.jumlah > sisa;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '${p.antrian != null ? '#${p.antrian} · ' : ''}${p.nama ?? '-'} · ${rupiah(p.jumlah)}',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            Text(
              '${p.nik ?? ''} · ${p.tenor} bln · angsuran ${rupiah(p.angsuran)}',
            ),
            Text(
              '${labelStatusPinjaman[p.status] ?? p.status} · ${p.keperluan}',
            ),
            if (kurang)
              Text(
                'Sisa plafon dana belum cukup',
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            if (hanyaCair && p.id != _kepala) const Text('Menunggu antrian'),
            if (aksi.isNotEmpty)
              Wrap(
                spacing: 8,
                children: [
                  for (final a in aksi)
                    a == 'tolak'
                        ? OutlinedButton(
                            onPressed: _sibuk || _loading
                                ? null
                                : () => _proses(p, a),
                            child: Text(_labelAksi[a]!),
                          )
                        : FilledButton(
                            onPressed:
                                _sibuk ||
                                    _loading ||
                                    (a == 'cairkan' && (kurang || sisa == null))
                                ? null
                                : () => _proses(p, a),
                            child: Text(_labelAksi[a]!),
                          ),
                ],
              ),
          ],
        ),
      ),
    );
  }

  Widget _pengajuan() => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      if (_hak.isEmpty && !_loading)
        const Card(
          child: ListTile(
            title: Text(
              'Akun Anda belum diberi hak tahap pinjaman. Atur di web → Pinjaman → Pengaturan.',
            ),
          ),
        ),
      DropdownButtonFormField<String?>(
        initialValue: _status,
        isExpanded: true,
        decoration: const InputDecoration(labelText: 'Status'),
        items: [
          const DropdownMenuItem(value: null, child: Text('Semua')),
          for (final e in labelStatusPinjaman.entries)
            DropdownMenuItem(value: e.key, child: Text(e.value)),
        ],
        onChanged: _loading || _sibuk
            ? null
            : (v) {
                _status = v;
                _muat();
              },
      ),
      const SizedBox(height: 12),
      if (_items.isEmpty && !_loading)
        const Padding(
          padding: EdgeInsets.all(24),
          child: Center(child: Text('Tidak ada pengajuan')),
        ),
      for (final p in _items) _kartu(p),
    ],
  );

  Widget _antrianView() {
    final a = _antrian;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (a == null)
          const Text(
            'Antrian belum dapat dimuat (periode hari ini belum diatur?).',
          )
        else ...[
          Text(
            'Periode ${a.periode}',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          Text(
            'Plafon ${a.plafon == null ? 'belum diatur (atur di web)' : rupiah(a.plafon!)} · terpakai ${rupiah(a.terpakai)}'
            ' · sisa ${a.sisa == null ? '-' : rupiah(a.sisa!)}',
          ),
          const SizedBox(height: 12),
          if (a.items.isEmpty)
            const Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: Text('Antrian kosong')),
            ),
          for (final p in a.items) _kartu(p, hanyaCair: true),
        ],
      ],
    );
  }

  Widget _laporanView() {
    final l = _laporan;
    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: 1 + (l?.items.length ?? 0),
      itemBuilder: (context, index) {
        if (index > 0) {
          final b = l!.items[index - 1];
          return Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: ListTile(
              title: Text('${b.nama ?? '-'} · sisa ${rupiah(b.sisaPokok)}'),
              subtitle: Text(
                [
                  [b.nik, b.unitKerja].nonNulls.join(' · '),
                  '${b.noTransaksi} · ${b.tanggal}',
                  '${rupiah(b.pinjaman)} · ${rupiah(b.angsuran)}/bln',
                  '${b.angsuranLewat}/${b.tenor} jatuh tempo · akhir ${b.jatuhTempoAkhir ?? '-'}',
                ].where((baris) => baris.isNotEmpty).join('\n'),
              ),
            ),
          );
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            TextField(
              controller: _cari,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                labelText: 'Cari nama / NIK',
                suffixIcon: IconButton(
                  tooltip: 'Cari',
                  onPressed: _loadingLaporan ? null : _muatLaporan,
                  icon: const Icon(Icons.search),
                ),
              ),
              onSubmitted: (_) => _muatLaporan(),
            ),
            const SizedBox(height: 12),
            if (l != null) ...[
              Text(
                '${l.jumlah} pinjaman aktif · total ${rupiah(l.totalPinjaman)}',
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
              Text('Sisa pokok ${rupiah(l.totalSisaPokok)}'),
              const SizedBox(height: 8),
              if (l.items.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(child: Text('Tidak ada pinjaman aktif')),
                ),
            ],
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final isLaporan = _tab.index == 2;
    final loading = isLaporan ? _loadingLaporan : _loading;
    final error = isLaporan ? _errorLaporan : _error;
    final refresh = isLaporan ? _muatLaporan : _muat;
    return TropicalScaffold(
      title: 'Admin · Pinjaman',
      actions: [
        IconButton(
          tooltip: 'Muat ulang',
          icon: const Icon(Icons.refresh, color: Colors.white),
          onPressed: loading || _sibuk ? null : refresh,
        ),
      ],
      body: Column(
        children: [
          TabBar(
            controller: _tab,
            tabs: const [
              Tab(text: 'Pengajuan'),
              Tab(text: 'Antrian'),
              Tab(text: 'Laporan'),
            ],
          ),
          if (loading) const LinearProgressIndicator(),
          if (error != null)
            Card(
              child: ListTile(
                title: Text(error),
                trailing: TextButton(
                  onPressed: refresh,
                  child: const Text('Ulangi'),
                ),
              ),
            ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: refresh,
              child: IndexedStack(
                index: _tab.index,
                children: [_pengajuan(), _antrianView(), _laporanView()],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Konfirmasi aksi + catatan; tolak wajib alasan. Mengembalikan catatan (boleh kosong) atau null bila batal.
class _DialogAksi extends StatefulWidget {
  const _DialogAksi({required this.p, required this.aksi});
  final PengajuanPinjaman p;
  final String aksi;
  @override
  State<_DialogAksi> createState() => _DialogAksiState();
}

class _DialogAksiState extends State<_DialogAksi> {
  final _catatan = TextEditingController();
  String? _galat;

  @override
  void dispose() {
    _catatan.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tolak = widget.aksi == 'tolak';
    return AlertDialog(
      scrollable: true,
      title: Text('${_labelAksi[widget.aksi]} pengajuan #${widget.p.id}'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '${widget.p.nama ?? '-'} · ${rupiah(widget.p.jumlah)} · ${widget.p.tenor} bulan',
          ),
          if (widget.aksi == 'cairkan')
            const Padding(
              padding: EdgeInsets.only(top: 8),
              child: Text('Pencairan akan mencatat pinjaman di BackOffice.'),
            ),
          TextField(
            controller: _catatan,
            autofocus: true,
            maxLength: 200,
            maxLines: 3,
            decoration: InputDecoration(
              labelText: tolak
                  ? 'Alasan penolakan (wajib)'
                  : 'Catatan (opsional)',
              errorText: _galat,
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Batal'),
        ),
        FilledButton(
          onPressed: () {
            final isi = _catatan.text.trim();
            if (tolak && isi.isEmpty) {
              setState(() => _galat = 'Alasan penolakan wajib diisi');
              return;
            }
            Navigator.pop(context, isi);
          },
          child: Text(_labelAksi[widget.aksi]!),
        ),
      ],
    );
  }
}
