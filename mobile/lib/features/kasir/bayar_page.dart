import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:printing/printing.dart';

import '../../core/api.dart';
import '../../core/checkout.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'kasir_page.dart';
import 'struk_pdf.dart';
import 'struk_bluetooth.dart';

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
  CheckoutDraft? _draft;

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
    final p = await Navigator.of(context).push<Pelanggan>(
      MaterialPageRoute(builder: (_) => const PelangganPicker()),
    );
    if (p != null) setState(() => _pel = p);
  }

  Future<void> _bayarSekarang() async {
    final pel = _pel;
    final cart = ref.read(cartProvider).cart;
    if (pel == null || _busy || cart.kosong) return;
    final profile = ref.read(authProvider).profil;
    final userId = profile?.id ?? 0;
    if (userId <= 0) {
      setState(() => _error = 'Sesi kasir tidak valid. Silakan masuk kembali.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      var draft = _draft;
      if (draft == null) {
        draft = CheckoutDraft(
          key: newCheckoutKey(),
          nik: pel.nik,
          tenor: pel.tunai ? 1 : _tenor,
          uangBayar: pel.tunai ? digitsToInt(_bayar.text) : null,
          baris: cart.toRequest(),
        );
        await ref.read(checkoutStoreProvider).write(userId, draft);
        if (mounted) setState(() => _draft = draft);
      }
      var nomor = draft.nomor;
      if (nomor == null) {
        nomor = await api.nomor(idempotencyKey: draft.key);
        draft = draft.withNomor(nomor);
        await ref.read(checkoutStoreProvider).write(userId, draft);
        if (mounted) setState(() => _draft = draft);
      }
      final f = await api.simpan(
        idempotencyKey: draft.key,
        nomor: nomor,
        nik: draft.nik,
        tenor: draft.tenor,
        uangBayar: draft.uangBayar,
        baris: draft.baris,
      );
      await ref.read(checkoutStoreProvider).clear(userId);
      if (!mounted) return;
      setState(() => _draft = null);
      // Tersimpan: halaman struk menggantikan halaman ini sehingga tidak bisa disimpan ulang.
      await Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => StrukPage(faktur: f)),
      );
    } on ApiException catch (e) {
      var pesanError = e.offline
          ? 'Koneksi terputus. Coba lagi - transaksi tidak akan tersimpan ganda.'
          : e.pesan;
      if ((e.status == 400 || e.status == 422) && userId > 0) {
        try {
          await ref.read(checkoutStoreProvider).clear(userId);
          if (mounted) setState(() => _draft = null);
        } on Object {
          pesanError = '$pesanError Draft checkout belum bisa dibersihkan.';
        }
      }
      if (mounted) {
        setState(() => _error = pesanError);
        if (e.kode == 'LIMIT_TERLAMPAUI') await _dialogLimit(e.pesan);
      }
    } on Object catch (e) {
      if (mounted) setState(() => _error = 'Gagal menyiapkan transaksi: $e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _dialogLimit(String pesanLimit) => showDialog<void>(
    context: context,
    builder: (c) => AlertDialog(
      icon: const Icon(
        Icons.warning_amber_rounded,
        color: Color(0xFFD68A00),
        size: 32,
      ),
      title: const Text('Limit hutang terlampaui'),
      content: Text(pesanLimit),
      actions: [
        FilledButton(
          onPressed: () => Navigator.pop(c),
          child: const Text('Mengerti'),
        ),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final pel = _pel;
    final uang = digitsToInt(_bayar.text);
    final kembali = uang - cart.perkiraan;
    return PopScope(
      canPop: _draft == null && !_busy,
      child: TropicalScaffold(
        title: 'Pembayaran',
        leading: IconButton(
          tooltip: 'Kembali',
          icon: const Icon(Icons.arrow_back, color: Colors.white),
          onPressed: _draft == null && !_busy
              ? () => Navigator.pop(context)
              : null,
        ),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
          children: [
            Card(
              child: ListTile(
                leading: pel == null || pel.tunai
                    ? CircleAvatar(
                        backgroundColor: Theme.of(context)
                            .colorScheme
                            .primaryContainer,
                        child: Icon(
                          Icons.person_outline,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                      )
                    : FotoAnggotaAvatar(nik: pel.nik, nama: pel.nama),
                title: Text(pel?.nama ?? 'Pilih pelanggan'),
                subtitle: pel == null
                    ? null
                    : Text(
                        '${pel.nik}${pel.tunai ? ' · Tunai' : ' · ${pel.unit}'}',
                      ),
                trailing: const Icon(Icons.chevron_right),
                onTap: _busy || _draft != null ? null : _pilih,
              ),
            ),
            const SizedBox(height: 12),
            Card(
              color: Theme.of(context).colorScheme.primaryContainer,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Perkiraan total',
                      style: Theme.of(context).textTheme.labelLarge,
                    ),
                    Text(
                      rupiah(cart.perkiraan),
                      style: Theme.of(context).textTheme.headlineMedium
                          ?.copyWith(
                            color: Theme.of(context).colorScheme.primary,
                            fontWeight: FontWeight.w700,
                          ),
                    ),
                    Text(
                      'Total akhir, termasuk diskon qty, dihitung oleh server.',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            if (pel != null && pel.tunai) ...[
              TextField(
                enabled: !_busy && _draft == null,
                controller: _bayar,
                keyboardType: TextInputType.number,
                onChanged: (_) => setState(() {}),
                decoration: const InputDecoration(
                  labelText: 'Uang bayar (Rp)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                uang == 0
                    ? ''
                    : (kembali >= 0
                          ? 'Kembalian perkiraan ${rupiah(kembali)}'
                          : 'Kurang ${rupiah(-kembali)}'),
              ),
            ],
            if (pel != null && !pel.tunai) ...[
              Text(
                'Kredit anggota${pel.limit == 0 ? ' (limit tidak dibatasi)' : ' · limit ${rupiah(pel.limit)}/periode'}',
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<int>(
                initialValue: _tenor,
                decoration: const InputDecoration(
                  labelText: 'Tenor (1 = sekali bayar)',
                  border: OutlineInputBorder(),
                ),
                items: [
                  for (var i = 1; i <= 12; i++)
                    DropdownMenuItem(
                      value: i,
                      child: Text(
                        i == 1 ? '1x (sekali bayar)' : '${i}x angsuran',
                      ),
                    ),
                ],
                onChanged: _busy || _draft != null
                    ? null
                    : (v) => setState(() => _tenor = v ?? 1),
              ),
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text(
                  'Limit hutang diperiksa server saat menyimpan.',
                  style: TextStyle(fontSize: 12),
                ),
              ),
            ],
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Semantics(
                  liveRegion: true,
                  child: Text(
                    _error!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ),
              ),
            const SizedBox(height: 24),
            GradientActionButton(
              label: _busy
                  ? 'Menyimpan…'
                  : online
                  ? _draft == null
                        ? 'Simpan & Cetak'
                        : 'Ulangi transaksi aman'
                  : 'Offline',
              icon: _busy ? Icons.hourglass_top : Icons.receipt_long,
              onPressed: (_busy || pel == null || !online)
                  ? null
                  : _bayarSekarang,
            ),
          ],
        ),
      ),
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
    final kode = await Navigator.of(context).push<String>(
      MaterialPageRoute(
        builder: (_) => const ScanPage(judul: 'Scan kartu anggota'),
      ),
    );
    if (kode == null || !mounted) return;
    try {
      final p = await ref.read(apiProvider).pelangganNik(kode.trim());
      if (!mounted) return;
      final pilih = await showModalBottomSheet<bool>(
        context: context,
        showDragHandle: true,
        builder: (c) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const CircleAvatar(
                  radius: 28,
                  child: Icon(Icons.verified_user_outlined),
                ),
                const SizedBox(height: 12),
                Text(p.nama, style: Theme.of(c).textTheme.titleLarge),
                Text('${p.nik} · ${p.unit}'),
                const SizedBox(height: 16),
                GradientActionButton(
                  label: 'Pilih anggota ini',
                  icon: Icons.check,
                  onPressed: () => Navigator.pop(c, true),
                ),
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  onPressed: () => Navigator.pop(c, false),
                  icon: const Icon(Icons.qr_code_scanner),
                  label: const Text('Scan ulang'),
                ),
              ],
            ),
          ),
        ),
      );
      if (pilih == true && mounted) Navigator.of(context).pop(p);
    } on ApiException catch (e) {
      if (mounted && e.status == 404) {
        await showModalBottomSheet<void>(
          context: context,
          showDragHandle: true,
          builder: (c) => SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const CircleAvatar(
                    backgroundColor: Color(0xFFFBE4E1),
                    child: Icon(
                      Icons.person_search_outlined,
                      color: Color(0xFF9F2D27),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Pelanggan tidak ditemukan',
                    style: Theme.of(c).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'QR tidak cocok dengan anggota aktif. Pilihan saat ini tidak berubah.',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                  GradientActionButton(
                    label: 'Scan ulang',
                    icon: Icons.qr_code_scanner,
                    onPressed: () => Navigator.pop(c),
                  ),
                  TextButton(
                    onPressed: () => Navigator.pop(c),
                    child: const Text('Cari anggota manual'),
                  ),
                ],
              ),
            ),
          ),
        );
      } else if (mounted) {
        pesan(context, e.pesan);
      }
    }
  }

  @override
  Widget build(BuildContext context) => TropicalScaffold(
    title: 'Pilih Pelanggan',
    actions: [
      IconButton(
        tooltip: 'Scan kartu anggota (opsional)',
        icon: const Icon(Icons.qr_code_scanner, color: Colors.white),
        onPressed: _scan,
      ),
    ],
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: Column(
            children: [
              TextField(
                controller: _q,
                onChanged: (_) {
                  _deb?.cancel();
                  _deb = Timer(const Duration(milliseconds: 350), _muat);
                },
                decoration: const InputDecoration(
                  hintText: 'Cari nama atau NIK',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: _scan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Scan QR kartu anggota'),
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size.fromHeight(46),
                ),
              ),
            ],
          ),
        ),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              _error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ),
        Expanded(
          child: _loading && _items.isEmpty
              ? const Center(child: CircularProgressIndicator())
              : RefreshIndicator(
                  onRefresh: _muat,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                    children: [
                      if (_items.isEmpty && _error == null)
                        const Padding(
                          padding: EdgeInsets.all(36),
                          child: Center(
                            child: Text('Pelanggan tidak ditemukan'),
                          ),
                        ),
                      for (final p in _items)
                        Card(
                          margin: const EdgeInsets.only(bottom: 8),
                          child: ListTile(
                            leading: p.tunai
                                ? CircleAvatar(
                                    backgroundColor: Theme.of(context)
                                        .colorScheme
                                        .primaryContainer,
                                    child: Icon(
                                      Icons.person_outline,
                                      color: Theme.of(context).colorScheme.primary,
                                    ),
                                  )
                                : FotoAnggotaAvatar(nik: p.nik, nama: p.nama),
                            title: Text(
                              p.nama,
                              style: const TextStyle(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            subtitle: Text('${p.nik} · ${p.unit}'),
                            trailing: p.tunai
                                ? const StatePill(text: 'TUNAI')
                                : const Icon(Icons.chevron_right),
                            onTap: () => Navigator.of(context).pop(p),
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

class StrukPage extends ConsumerStatefulWidget {
  const StrukPage({super.key, required this.faktur, this.dariRiwayat = false});
  final Faktur faktur;

  /// Dibuka dari riwayat: hanya tampilan detail (keranjang tidak disentuh, boleh kembali).
  final bool dariRiwayat;
  @override
  ConsumerState<StrukPage> createState() => _StrukPageState();
}

class _StrukPageState extends ConsumerState<StrukPage> {
  bool _mencetakBluetooth = false;
  @override
  void initState() {
    super.initState();
    // Faktur sudah tersimpan di server: keranjang dikosongkan; gagal cetak hanya peringatan.
    if (!widget.dariRiwayat) {
      Future.microtask(() => ref.read(cartProvider.notifier).kosongkan());
    }
  }

  Future<void> _cetak() async {
    try {
      await Printing.layoutPdf(
        name: widget.faktur.no,
        onLayout: (_) => bangunStrukPdf(widget.faktur),
      );
    } catch (e) {
      if (mounted)
        pesan(
          context,
          'Faktur sudah tersimpan, tetapi gagal dicetak. Coba lagi dari tombol Cetak.',
        );
    }
  }

  Future<void> _cetakBluetooth() async {
    if (_mencetakBluetooth) return;
    setState(() => _mencetakBluetooth = true);
    const layanan = LayananPrinterBluetooth();
    try {
      final daftar = await layanan.daftar();
      if (!mounted) return;
      if (daftar.isEmpty) {
        pesan(context, 'Belum ada perangkat Bluetooth yang dipasangkan. Pasangkan printer di pengaturan Bluetooth Android.');
        return;
      }
      final printer = await showDialog<PrinterBluetooth>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Pilih printer Bluetooth'),
          content: SizedBox(
            width: double.maxFinite,
            child: ListView(
              shrinkWrap: true,
              children: [
                for (final p in daftar)
                  ListTile(
                    title: Text(p.nama),
                    subtitle: Text(p.alamat),
                    onTap: () => Navigator.pop(context, p),
                  ),
              ],
            ),
          ),
          actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Batal'))],
        ),
      );
      if (printer == null || !mounted) return;
      await layanan.cetak(printer, widget.faktur);
      if (mounted) pesan(context, 'Nota dikirim ke printer.');
    } on PlatformException catch (e) {
      if (mounted) pesan(context, e.message ?? 'Gagal mengakses printer Bluetooth.');
    } catch (_) {
      if (mounted) pesan(context, 'Gagal mengirim nota ke printer Bluetooth.');
    } finally {
      if (mounted) setState(() => _mencetakBluetooth = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final f = widget.faktur;
    return PopScope(
      canPop: widget.dariRiwayat,
      child: TropicalScaffold(
        title: widget.dariRiwayat ? 'Detail Transaksi' : 'Faktur Tersimpan',
        leading: widget.dariRiwayat
            ? IconButton(
                tooltip: 'Kembali',
                icon: const Icon(Icons.arrow_back, color: Colors.white),
                onPressed: () => Navigator.of(context).pop(),
              )
            : null,
        body: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
          children: [
            Card(
              color: Theme.of(context).colorScheme.primaryContainer,
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  children: [
                    CircleAvatar(
                      radius: 30,
                      backgroundColor: Theme.of(context).colorScheme.surface,
                      child: Icon(
                        Icons.check,
                        size: 32,
                        color: Theme.of(context).colorScheme.primary,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      f.no,
                      style: Theme.of(context).textTheme.titleLarge,
                      textAlign: TextAlign.center,
                    ),
                    Text(
                      '${f.tanggal} ${f.jam}',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                    Text('Kasir: ${f.identitasKasir}'),
                  ],
                ),
              ),
            ),
            if (f.duplikat)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: StatePill(text: 'Faktur ini sudah tersimpan sebelumnya'),
              ),
            const SizedBox(height: 12),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${f.namaPelanggan} · ${f.jenisBayar}${f.tenor > 1 ? ' · ${f.tenor}x angsuran' : ''}',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    Text(f.nik, style: Theme.of(context).textTheme.bodySmall),
                    const Divider(height: 22),
                    for (final b in f.baris)
                      ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(b.nama),
                        subtitle: Text(
                          '${formatQty(b.qty, b.satuan)} × ${rupiah(b.harga)}${b.potongan > 0 ? ' − potongan ${rupiah(b.potongan)}' : ''}',
                        ),
                        trailing: Text(
                          rupiah(b.total),
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                      ),
                    const Divider(height: 22),
                    _totalBaris(context, 'Bruto', rupiah(f.bruto)),
                    _totalBaris(context, 'Potongan', rupiah(f.potongan)),
                    _totalBaris(context, 'Total', rupiah(f.total), bold: true),
                    if (f.uangBayar != null)
                      _totalBaris(
                        context,
                        'Bayar · Kembalian',
                        '${rupiah(f.uangBayar!)} · ${rupiah(f.kembalian ?? 0)}',
                      ),
                  ],
                ),
              ),
            ),
            if (f.jadwal.isNotEmpty) ...[
              const SizedBox(height: 12),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Jadwal angsuran',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      for (final j in f.jadwal)
                        Padding(
                          padding: const EdgeInsets.only(top: 8),
                          child: _totalBaris(
                            context,
                            'Ke-${j.ke} · ${j.jatuhTempo}',
                            rupiah(j.angsuran),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ],
            const SizedBox(height: 16),
            if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) ...[
              OutlinedButton.icon(
                onPressed: _mencetakBluetooth ? null : _cetakBluetooth,
                icon: const Icon(Icons.bluetooth),
                label: Text(_mencetakBluetooth ? 'Menghubungkan printer...' : 'Cetak printer Bluetooth'),
              ),
              const SizedBox(height: 8),
            ],
            OutlinedButton.icon(
              onPressed: _cetak,
              icon: const Icon(Icons.print_outlined),
              label: const Text('Cetak / Bagikan PDF'),
            ),
            const SizedBox(height: 8),
            GradientActionButton(
              label: widget.dariRiwayat ? 'Kembali' : 'Transaksi baru',
              icon: widget.dariRiwayat ? Icons.arrow_back : Icons.add,
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _totalBaris(
    BuildContext context,
    String label,
    String value, {
    bool bold = false,
  }) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 3),
    child: Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: bold ? const TextStyle(fontWeight: FontWeight.w700) : null,
        ),
        Text(
          value,
          style: bold
              ? TextStyle(
                  fontWeight: FontWeight.w700,
                  color: Theme.of(context).colorScheme.primary,
                )
              : null,
        ),
      ],
    ),
  );
}
