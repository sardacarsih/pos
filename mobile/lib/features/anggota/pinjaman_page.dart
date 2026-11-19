import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';

const _langkah = ['DIAJUKAN', 'DIVERIFIKASI', 'DISETUJUI', 'DICAIRKAN'];
const labelStatusPinjaman = {
  'DIAJUKAN': 'Diajukan',
  'DIVERIFIKASI': 'Diverifikasi',
  'DISETUJUI': 'Disetujui · antre dana',
  'DICAIRKAN': 'Dicairkan',
  'DITOLAK': 'Ditolak',
  'DIBATALKAN': 'Dibatalkan',
};

/// Pinjaman tunai anggota: pinjaman aktif, status pengajuan + nomor antrian, form ajukan dengan simulasi, riwayat.
class PinjamanPage extends ConsumerStatefulWidget {
  const PinjamanPage({super.key});
  @override
  ConsumerState<PinjamanPage> createState() => _PinjamanPageState();
}

class _PinjamanPageState extends ConsumerState<PinjamanPage> {
  PinjamanSaya? _data;
  String? _error;
  bool _loading = true, _sibuk = false;
  final _jumlah = TextEditingController();
  final _keperluan = TextEditingController();
  int? _tenor;
  List<BarisAngsuran> _jadwal = [];
  // Dipakai ulang sampai berhasil agar kirim ulang (jaringan putus) tidak membuat pengajuan ganda.
  String _kunci = const Uuid().v4();

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void dispose() {
    _jumlah.dispose();
    _keperluan.dispose();
    super.dispose();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await ref.read(apiProvider).pinjamanSaya();
      if (!mounted) return;
      setState(() {
        _data = d;
        _tenor ??= d.aturan.maksTenor;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  String? _cek({bool keperluan = true}) {
    final a = _data!.aturan;
    final j = digitsToInt(_jumlah.text);
    if (j <= 0) return 'Isi jumlah pinjaman';
    if (a.maksPinjaman > 0 && j > a.maksPinjaman) return 'Maksimal ${rupiah(a.maksPinjaman)}';
    if (keperluan && _keperluan.text.trim().isEmpty) return 'Isi keperluan pinjaman';
    return null;
  }

  Future<void> _aksi(Future<void> Function() f) async {
    if (_sibuk) return;
    setState(() => _sibuk = true);
    try {
      await f();
    } on ApiException catch (e) {
      _pesan(e.pesan);
    } finally {
      if (mounted) setState(() => _sibuk = false);
    }
  }

  void _pesan(String s) {
    if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(s)));
  }

  void _simulasi() {
    final salah = _cek(keperluan: false);
    if (salah != null) return _pesan(salah);
    _aksi(() async {
      final j = await ref.read(apiProvider).simulasiPinjaman(digitsToInt(_jumlah.text), _tenor!);
      if (mounted) setState(() => _jadwal = j);
    });
  }

  void _ajukan() {
    final salah = _cek();
    if (salah != null) return _pesan(salah);
    _aksi(() async {
      try {
        await ref.read(apiProvider).ajukanPinjaman(
          idempotencyKey: _kunci,
          jumlah: digitsToInt(_jumlah.text),
          tenor: _tenor!,
          keperluan: _keperluan.text.trim(),
        );
      } on ApiException catch (e) {
        if (!e.offline) _kunci = const Uuid().v4();
        rethrow;
      }
      _kunci = const Uuid().v4();
      _jumlah.clear();
      _keperluan.clear();
      _jadwal = [];
      _pesan('Pengajuan terkirim');
      await _muat();
    });
  }

  Future<void> _batal(PengajuanPinjaman p) async {
    final ya = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Batalkan pengajuan?'),
        content: Text('${rupiah(p.jumlah)} · ${p.tenor} bulan'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Tidak')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Batalkan')),
        ],
      ),
    );
    if (ya != true) return;
    _aksi(() async {
      await ref.read(apiProvider).batalPinjaman(p.id, idempotencyKey: const Uuid().v4());
      _pesan('Pengajuan dibatalkan');
      await _muat();
    });
  }

  @override
  Widget build(BuildContext context) {
    final d = _data;
    final berjalan = d?.pengajuan.where((p) => p.berjalan).firstOrNull;
    final bisaAju = d != null && berjalan == null && d.aktif.isEmpty;
    return TropicalScaffold(
      title: 'Pinjaman',
      body: RefreshIndicator(
        onRefresh: _muat,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            if (_loading && d == null) const Padding(padding: EdgeInsets.all(48), child: LoadingKopkar()),
            if (_error != null)
              Card(
                child: ListTile(
                  title: Text(_error!),
                  trailing: TextButton(onPressed: _muat, child: const Text('Ulangi')),
                ),
              ),
            if (d != null) ...[
              for (final a in d.aktif)
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.account_balance_wallet_outlined),
                    title: Text('Pinjaman aktif ${rupiah(a.pinjaman)}'),
                    subtitle: Text(
                      '${a.noTransaksi} · cair ${a.tanggal}\n'
                      'Angsuran ${rupiah(a.angsuran)}/bln · ${a.angsuranLewat} dari ${a.tenor} sudah jatuh tempo',
                    ),
                    isThreeLine: true,
                  ),
                ),
              if (berjalan != null) _StatusCard(p: berjalan, sibuk: _sibuk, onBatal: () => _batal(berjalan)),
              if (bisaAju) _form(d.aturan),
              const SizedBox(height: 16),
              Text('Riwayat pengajuan', style: Theme.of(context).textTheme.titleMedium),
              if (d.pengajuan.isEmpty)
                const Padding(padding: EdgeInsets.all(24), child: Center(child: Text('Belum ada pengajuan'))),
              for (final p in d.pengajuan)
                ListTile(
                  title: Text('${rupiah(p.jumlah)} · ${p.tenor} bln'),
                  subtitle: Text(
                    [p.tglAju, labelStatusPinjaman[p.status] ?? p.status, ?p.noTransaksi, ?p.alasan].join(' · '),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _form(AturanPinjaman a) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Ajukan pinjaman', style: Theme.of(context).textTheme.titleMedium),
          Text(
            'Bunga ${a.bunga}%/bulan · tenor maks. ${a.maksTenor} bulan'
            '${a.maksPinjaman > 0 ? ' · maks. ${rupiah(a.maksPinjaman)}' : ''}. Angsuran dipotong dari gaji.',
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _jumlah,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Jumlah pinjaman (Rp)'),
            onChanged: (_) => setState(() => _jadwal = []),
          ),
          DropdownButtonFormField<int>(
            initialValue: _tenor,
            decoration: const InputDecoration(labelText: 'Tenor'),
            items: [for (var t = 1; t <= a.maksTenor; t++) DropdownMenuItem(value: t, child: Text('$t bulan'))],
            onChanged: (v) => setState(() {
              _tenor = v;
              _jadwal = [];
            }),
          ),
          TextField(
            controller: _keperluan,
            maxLength: 200,
            decoration: const InputDecoration(labelText: 'Keperluan'),
          ),
          if (_jadwal.isNotEmpty) ...[
            Text('Angsuran ${rupiah(_jadwal.first.angsuran)}/bulan', style: const TextStyle(fontWeight: FontWeight.w600)),
            for (final b in _jadwal)
              Text('${b.ke}. ${b.jatuhTempo} · ${rupiah(b.angsuran)} · sisa ${rupiah(b.sisa)}'),
            const SizedBox(height: 8),
          ],
          Row(
            children: [
              Expanded(
                child: OutlinedButton(onPressed: _sibuk ? null : _simulasi, child: const Text('Hitung angsuran')),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: FilledButton(onPressed: _sibuk ? null : _ajukan, child: const Text('Kirim pengajuan')),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}

class _StatusCard extends StatelessWidget {
  const _StatusCard({required this.p, required this.sibuk, required this.onBatal});
  final PengajuanPinjaman p;
  final bool sibuk;
  final VoidCallback onBatal;

  @override
  Widget build(BuildContext context) {
    final posisi = _langkah.indexOf(p.status);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Pengajuan ${rupiah(p.jumlah)} · ${p.tenor} bulan', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                for (var i = 0; i < _langkah.length; i++)
                  Chip(
                    avatar: Icon(i <= posisi ? Icons.check_circle : Icons.radio_button_unchecked, size: 18),
                    label: Text(labelStatusPinjaman[_langkah[i]]!),
                  ),
              ],
            ),
            if (p.antrian != null) Text('Nomor antrian pencairan: ${p.antrian}', style: const TextStyle(fontWeight: FontWeight.w600)),
            Text('Perkiraan angsuran ${rupiah(p.angsuran)}/bulan · ${p.keperluan}'),
            if (p.status == 'DIAJUKAN')
              TextButton(onPressed: sibuk ? null : onBatal, child: const Text('Batalkan pengajuan')),
          ],
        ),
      ),
    );
  }
}
