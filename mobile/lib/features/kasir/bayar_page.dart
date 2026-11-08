import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:printing/printing.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import 'kasir_page.dart';
import 'struk_pdf.dart';

class BayarPage extends ConsumerStatefulWidget {
  const BayarPage({super.key});
  @override
  ConsumerState<BayarPage> createState() => _BayarPageState();
}

class _BayarPageState extends ConsumerState<BayarPage> {
  Pelanggan? _pel;
  final _bayar = TextEditingController();
  int _tenor = 1;
  bool _busy = false;
  String? _error;
  // Nomor ditahan selama pembayaran berlangsung: retry jaringan memakai nomor yang sama (idempotent).
  String? _nomor;

  @override
  void initState() {
    super.initState();
    _pelangganDefault();
  }

  @override
  void dispose() {
    _bayar.dispose();
    super.dispose();
  }

  Future<void> _pelangganDefault() async {
    final nik = ref.read(authProvider).profil?.defaultNik ?? '';
    if (nik.isEmpty) return;
    try {
      final p = await ref.read(apiProvider).pelangganNik(nik);
      if (mounted) setState(() => _pel ??= p);
    } on ApiException {
      // kasir memilih pelanggan manual
    }
  }

  Future<void> _pilih() async {
    final p = await Navigator.of(context).push<Pelanggan>(MaterialPageRoute(builder: (_) => const PelangganPicker()));
    if (p != null) setState(() => _pel = p);
  }

  Future<void> _bayarSekarang() async {
    final pel = _pel;
    final cart = ref.read(cartProvider).cart;
    if (pel == null || _busy || cart.kosong) return;
    final uang = digitsToInt(_bayar.text);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      _nomor ??= await api.nomor();
      final f = await api.simpan(
        nomor: _nomor!,
        nik: pel.nik,
        tenor: pel.tunai ? 1 : _tenor,
        uangBayar: pel.tunai ? uang : null,
        baris: cart.toRequest(),
      );
      if (!mounted) return;
      // Tersimpan: halaman struk menggantikan halaman ini sehingga tidak bisa disimpan ulang.
      await Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => StrukPage(faktur: f)));
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _error = e.offline ? 'Koneksi terputus. Coba lagi - transaksi tidak akan tersimpan ganda.' : e.pesan);
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final pel = _pel;
    final uang = digitsToInt(_bayar.text);
    final kembali = uang - cart.perkiraan;
    return Scaffold(
      appBar: AppBar(title: const Text('Pembayaran')),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        Card(
          child: ListTile(
            leading: const Icon(Icons.person),
            title: Text(pel?.nama ?? 'Pilih pelanggan'),
            subtitle: pel == null ? null : Text('${pel.nik}${pel.tunai ? ' · Tunai' : ' · ${pel.unit}'}'),
            trailing: const Icon(Icons.chevron_right),
            onTap: _busy ? null : _pilih,
          ),
        ),
        const SizedBox(height: 12),
        Text('Perkiraan total ${rupiah(cart.perkiraan)}', style: Theme.of(context).textTheme.titleLarge),
        const Text('Total akhir (termasuk diskon qty) dihitung server.'),
        const SizedBox(height: 16),
        if (pel != null && pel.tunai) ...[
          TextField(
            controller: _bayar,
            keyboardType: TextInputType.number,
            onChanged: (_) => setState(() {}),
            decoration: const InputDecoration(labelText: 'Uang bayar (Rp)', border: OutlineInputBorder()),
          ),
          const SizedBox(height: 8),
          Text(uang == 0 ? '' : (kembali >= 0 ? 'Kembalian perkiraan ${rupiah(kembali)}' : 'Kurang ${rupiah(-kembali)}')),
        ],
        if (pel != null && !pel.tunai) ...[
          Text('Kredit anggota${pel.limit == 0 ? ' (limit tidak dibatasi)' : ' · limit ${rupiah(pel.limit)}/periode'}'),
          const SizedBox(height: 8),
          DropdownButtonFormField<int>(
            initialValue: _tenor,
            decoration: const InputDecoration(labelText: 'Tenor (1 = sekali bayar)', border: OutlineInputBorder()),
            items: [for (var i = 1; i <= 12; i++) DropdownMenuItem(value: i, child: Text(i == 1 ? '1x (sekali bayar)' : '${i}x angsuran'))],
            onChanged: _busy ? null : (v) => setState(() => _tenor = v ?? 1),
          ),
          const Padding(padding: EdgeInsets.only(top: 8), child: Text('Limit hutang diperiksa server saat menyimpan.', style: TextStyle(fontSize: 12))),
        ],
        if (_error != null)
          Padding(padding: const EdgeInsets.only(top: 12), child: Semantics(liveRegion: true, child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)))),
        const SizedBox(height: 24),
        FilledButton(
          onPressed: (_busy || pel == null || !online) ? null : _bayarSekarang,
          style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(56)),
          child: _busy ? const SizedBox(height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2)) : Text(online ? 'Simpan & Cetak' : 'Offline'),
        ),
      ]),
    );
  }
}

class PelangganPicker extends ConsumerStatefulWidget {
  const PelangganPicker({super.key});
  @override
  ConsumerState<PelangganPicker> createState() => _PelangganPickerState();
}

class _PelangganPickerState extends ConsumerState<PelangganPicker> {
  final _q = TextEditingController();
  Timer? _deb;
  List<Pelanggan> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void dispose() {
    _deb?.cancel();
    _q.dispose();
    super.dispose();
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final teks = _q.text.trim();
    try {
      final r = await ref.read(apiProvider).pelanggan(q: teks);
      if (mounted && teks == _q.text.trim()) setState(() => _items = r.items);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// Scan kartu/NIK: cocok -> pilih; tidak cocok -> pesan, pilihan saat ini tidak berubah.
  Future<void> _scan() async {
    final kode = await Navigator.of(context).push<String>(MaterialPageRoute(builder: (_) => const ScanPage(judul: 'Scan kartu anggota')));
    if (kode == null || !mounted) return;
    try {
      final p = await ref.read(apiProvider).pelangganNik(kode.trim());
      if (mounted) Navigator.of(context).pop(p);
    } on ApiException catch (e) {
      if (mounted) pesan(context, e.status == 404 ? 'Pelanggan tidak ditemukan' : e.pesan);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Pilih Pelanggan'), actions: [
          IconButton(tooltip: 'Scan kartu anggota (opsional)', icon: const Icon(Icons.qr_code_scanner), onPressed: _scan),
        ]),
        body: Column(children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              controller: _q,
              onChanged: (_) {
                _deb?.cancel();
                _deb = Timer(const Duration(milliseconds: 350), _muat);
              },
              decoration: const InputDecoration(hintText: 'Cari nama atau NIK', prefixIcon: Icon(Icons.search), border: OutlineInputBorder()),
            ),
          ),
          if (_error != null) Padding(padding: const EdgeInsets.all(16), child: Text(_error!)),
          Expanded(
            child: _loading && _items.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : ListView(children: [
                    for (final p in _items)
                      ListTile(
                        title: Text(p.nama),
                        subtitle: Text('${p.nik} · ${p.unit}${p.tunai ? ' · Tunai' : ''}'),
                        onTap: () => Navigator.of(context).pop(p),
                      ),
                  ]),
          ),
        ]),
      );
}

class StrukPage extends ConsumerStatefulWidget {
  const StrukPage({super.key, required this.faktur});
  final Faktur faktur;
  @override
  ConsumerState<StrukPage> createState() => _StrukPageState();
}

class _StrukPageState extends ConsumerState<StrukPage> {
  @override
  void initState() {
    super.initState();
    // Faktur sudah tersimpan di server: keranjang dikosongkan; gagal cetak hanya peringatan.
    Future.microtask(() => ref.read(cartProvider.notifier).kosongkan());
  }

  Future<void> _cetak() async {
    try {
      await Printing.layoutPdf(name: widget.faktur.no, onLayout: (_) => bangunStrukPdf(widget.faktur));
    } catch (e) {
      if (mounted) pesan(context, 'Faktur sudah tersimpan, tetapi gagal dicetak. Coba lagi dari tombol Cetak.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final f = widget.faktur;
    return PopScope(
      canPop: false,
      child: Scaffold(
        appBar: AppBar(title: const Text('Faktur Tersimpan'), automaticallyImplyLeading: false),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          if (f.duplikat) const Text('Faktur ini sudah tersimpan sebelumnya.'),
          Text(f.no, style: Theme.of(context).textTheme.titleLarge),
          Text('${f.tanggal} ${f.jam} · ${f.jenisBayar}${f.tenor > 1 ? ' ${f.tenor}x' : ''}'),
          Text('${f.nik}, ${f.namaPelanggan}'),
          const Divider(height: 24),
          for (final b in f.baris) ListTile(contentPadding: EdgeInsets.zero, title: Text(b.nama), subtitle: Text('${b.qty} × ${rupiah(b.harga)}${b.potongan > 0 ? ' − ${rupiah(b.potongan)}' : ''}'), trailing: Text(rupiah(b.total))),
          const Divider(height: 24),
          Text('Total ${rupiah(f.total)}', style: Theme.of(context).textTheme.headlineSmall),
          if (f.uangBayar != null) Text('Bayar ${rupiah(f.uangBayar!)} · Kembalian ${rupiah(f.kembalian ?? 0)}'),
          const SizedBox(height: 24),
          OutlinedButton.icon(onPressed: _cetak, icon: const Icon(Icons.print), label: const Text('Cetak / Bagikan PDF'), style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(52))),
          const SizedBox(height: 12),
          FilledButton(onPressed: () => Navigator.of(context).pop(), style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(56)), child: const Text('Transaksi baru')),
        ]),
      ),
    );
  }
}
