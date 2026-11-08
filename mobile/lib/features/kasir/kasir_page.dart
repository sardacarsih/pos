import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/api.dart';
import '../../core/checkout.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/cart.dart';
import '../profil/profil_aksi.dart';
import '../../core/widgets.dart';
import 'bayar_page.dart';

void pesan(BuildContext c, String m) =>
    ScaffoldMessenger.of(c).showSnackBar(SnackBar(content: Text(m)));

/// Layar kamera untuk scan barcode / kartu anggota. Opsional: aplikasi tetap penuh tanpa kamera.
class ScanPage extends StatefulWidget {
  const ScanPage({super.key, this.judul = 'Scan barcode'});
  final String judul;
  @override
  State<ScanPage> createState() => _ScanPageState();
}

class _ScanPageState extends State<ScanPage> {
  bool _selesai = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(widget.judul),
      backgroundColor: const Color(0xFF201D1A),
      foregroundColor: Colors.white,
    ),
    body: Stack(
      fit: StackFit.expand,
      children: [
        MobileScanner(
          onDetect: (cap) {
            final v = cap.barcodes.isEmpty ? null : cap.barcodes.first.rawValue;
            if (v != null && v.isNotEmpty && !_selesai) {
              _selesai = true;
              Navigator.of(context).pop(v);
            }
          },
          errorBuilder: (c, e) => Center(
            child: Card(
              margin: const EdgeInsets.all(24),
              child: const Padding(
                padding: EdgeInsets.all(20),
                child: Text(
                  'Kamera tidak tersedia atau izin ditolak. Gunakan pencarian dari daftar.',
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          ),
        ),
        IgnorePointer(
          child: Center(
            child: Container(
              width: 260,
              height: 260,
              decoration: BoxDecoration(
                border: Border.all(color: const Color(0xFF8BE0A6), width: 3),
                borderRadius: BorderRadius.circular(28),
              ),
            ),
          ),
        ),
        const Positioned(
          left: 24,
          right: 24,
          bottom: 32,
          child: Text(
            'Arahkan kamera ke barcode barang atau kartu anggota',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w600,
              shadows: [Shadow(color: Colors.black87, blurRadius: 8)],
            ),
          ),
        ),
      ],
    ),
  );
}

class KasirPage extends ConsumerStatefulWidget {
  const KasirPage({super.key});
  @override
  ConsumerState<KasirPage> createState() => _KasirPageState();
}

class _KasirPageState extends ConsumerState<KasirPage> {
  final _q = TextEditingController();
  final _scroll = ScrollController();
  Timer? _deb;
  final List<Barang> _items = [];
  List<Kategori> _kategori = [];
  String? _chip;
  bool _loading = true, _lagi = false, _memuatLagi = false;
  String? _error;
  int _page = 1;
  CheckoutDraft? _draftTertunda;
  String? _errorPemulihan;
  bool _memulihkanDraft = false;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300)
        _muatLagi();
    });
    ref
        .read(apiProvider)
        .kategori()
        .then((k) {
          if (mounted) setState(() => _kategori = k);
        })
        .catchError((_) {});
    _muat();
    Future.microtask(_cobaPulihkanDraft);
  }

  @override
  void dispose() {
    _deb?.cancel();
    _q.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _cari(String _) {
    _deb?.cancel();
    _deb = Timer(const Duration(milliseconds: 350), _muat);
  }

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final teks = _q.text.trim();
    try {
      final r = await ref.read(apiProvider).barang(q: teks, kategori: _chip);
      if (!mounted || teks != _q.text.trim()) return;
      setState(() {
        _items
          ..clear()
          ..addAll(r.items);
        _lagi = r.lagi;
        _page = 1;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _muatLagi() async {
    if (!_lagi || _memuatLagi || _loading) return;
    _memuatLagi = true;
    try {
      final r = await ref
          .read(apiProvider)
          .barang(q: _q.text.trim(), kategori: _chip, page: _page + 1);
      if (!mounted) return;
      setState(() {
        _items.addAll(r.items);
        _lagi = r.lagi;
        _page++;
      });
    } on ApiException {
      // abaikan; tarik untuk menyegarkan
    } finally {
      _memuatLagi = false;
    }
  }

  Future<void> _cobaPulihkanDraft() async {
    if (_memulihkanDraft) return;
    final userId = ref.read(authProvider).profil?.id ?? 0;
    if (userId <= 0) return;
    final draft = await ref.read(checkoutStoreProvider).read(userId);
    if (!mounted || draft == null) return;
    setState(() {
      _draftTertunda = draft;
      _memulihkanDraft = true;
      _errorPemulihan = null;
    });
    try {
      final api = ref.read(apiProvider);
      final faktur = await pulihkanCheckout(
        userId: userId,
        draft: draft,
        store: ref.read(checkoutStoreProvider),
        terbitkanNomor: (key) => api.nomor(idempotencyKey: key),
        cariFaktur: (nomor) async {
          try {
            return await api.fakturKasir(nomor);
          } on ApiException catch (e) {
            if (e.status == 404) return null;
            rethrow;
          }
        },
        simpan: (siap) => api.simpan(
          idempotencyKey: siap.key,
          nomor: siap.nomor!,
          nik: siap.nik,
          tenor: siap.tenor,
          uangBayar: siap.uangBayar,
          baris: siap.baris,
        ),
      );
      if (!mounted) return;
      setState(() => _draftTertunda = null);
      await Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => StrukPage(faktur: faktur)),
      );
    } on ApiException catch (e) {
      if ((e.status == 400 || e.status == 422) && mounted) {
        try {
          await ref.read(checkoutStoreProvider).clear(userId);
        } on Object catch (clearError) {
          if (mounted) {
            setState(
              () => _errorPemulihan =
                  'Transaksi belum tersimpan, tetapi draft belum bisa dibersihkan: $clearError',
            );
          }
          return;
        }
        if (!mounted) return;
        setState(() {
          _draftTertunda = null;
          _errorPemulihan =
              'Transaksi belum tersimpan dan perlu dibuat ulang: ${e.pesan}';
        });
        pesan(context, _errorPemulihan!);
      } else if (mounted) {
        setState(() => _errorPemulihan = e.pesan);
      }
    } on Object catch (e) {
      if (mounted) {
        setState(() => _errorPemulihan = 'Belum dapat dipulihkan: $e');
      }
    } finally {
      if (mounted) setState(() => _memulihkanDraft = false);
    }
  }

  Widget _pemulihanPanel(BuildContext context) => Center(
    child: SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 480),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  _memulihkanDraft ? Icons.sync : Icons.receipt_long_outlined,
                  size: 44,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(height: 12),
                Text(
                  _memulihkanDraft
                      ? 'Memeriksa transaksi'
                      : 'Transaksi belum dipastikan',
                  style: Theme.of(context).textTheme.titleLarge,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  _errorPemulihan ?? 'Aplikasi menemukan checkout yang belum mendapat konfirmasi. Cek status server atau ulangi dengan kunci yang sama.',
                  textAlign: TextAlign.center,
                ),
                if (_memulihkanDraft) ...[
                  const SizedBox(height: 20),
                  const CircularProgressIndicator(),
                ] else ...[
                  const SizedBox(height: 20),
                  GradientActionButton(
                    label: 'Coba pulihkan transaksi',
                    icon: Icons.refresh,
                    onPressed: _cobaPulihkanDraft,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    ),
  );

  /// Hasil scan masuk lewat jalur yang sama dengan memilih dari daftar (cart.tambah).
  Future<void> _scan() async {
    final kode = await Navigator.of(context)
        .push<String>(MaterialPageRoute(builder: (_) => const ScanPage()));
    if (kode == null || !mounted) return;
    try {
      final b = await ref.read(apiProvider).barangBarcode(kode);
      ref.read(cartProvider.notifier).tambah(b);
      if (mounted) pesan(context, '${b.nama} ditambahkan');
    } on ApiException catch (e) {
      if (mounted)
        pesan(
          context,
          e.status == 404
              ? 'Barang dengan barcode itu tidak ditemukan'
              : e.pesan,
        );
    }
  }

  void _bukaKeranjang() => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.8,
      maxChildSize: 0.95,
      builder: (c, sc) => CartPanel(
        controller: sc,
        onBayar: () {
          Navigator.of(c).pop();
          Navigator.of(context)
              .push(MaterialPageRoute(builder: (_) => const BayarPage()));
        },
      ),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final lebar = MediaQuery.sizeOf(context).width >= 900;
    final daftar = Column(
      children: [
        if (!online)
          Container(
            width: double.infinity,
            color: Theme.of(context).colorScheme.errorContainer,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.wifi_off, size: 18),
                SizedBox(width: 8),
                Flexible(
                  child: Text(
                    'Tidak ada koneksi. Pembayaran dinonaktifkan.',
                    textAlign: TextAlign.center,
                  ),
                ),
              ],
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: TextField(
            controller: _q,
            onChanged: (value) {
              setState(() {});
              _cari(value);
            },
            decoration: InputDecoration(
              hintText: 'Cari produk',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _q.text.isEmpty
                  ? null
                  : IconButton(
                      tooltip: 'Hapus',
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _q.clear();
                        _muat();
                      },
                    ),
            ),
          ),
        ),
        if (_kategori.isNotEmpty)
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: ChoiceChip(
                    label: const Text('Semua'),
                    selected: _chip == null,
                    onSelected: (_) {
                      setState(() => _chip = null);
                      _muat();
                    },
                  ),
                ),
                for (final k in _kategori)
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ChoiceChip(
                      label: Text(k.nama),
                      selected: _chip == k.id,
                      onSelected: (_) {
                        setState(() => _chip = k.id);
                        _muat();
                      },
                    ),
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
                    physics: AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(height: 100),
                      Center(child: Text('Barang tidak ditemukan')),
                    ],
                  )
                : LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = constraints.maxWidth < 420
                          ? 2
                          : (constraints.maxWidth < 720 ? 3 : 4);
                      // Area gambar kartu dibuat persegi (foto produk persegi mengisi penuh):
                      // tinggi sel = lebar gambar (lebar sel - padding 16) + blok teks/harga (~120).
                      final lebarSel =
                          (constraints.maxWidth - 32 - (columns - 1) * 12) /
                          columns;
                      return GridView.builder(
                        controller: _scroll,
                        keyboardDismissBehavior:
                            ScrollViewKeyboardDismissBehavior.onDrag,
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
                        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: columns,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                          mainAxisExtent: lebarSel + 120,
                        ),
                        itemCount: _items.length + (_lagi ? 1 : 0),
                        itemBuilder: (context, index) => index >= _items.length
                            ? const Center(child: CircularProgressIndicator())
                            : BarangCard(
                                b: _items[index],
                                onAdd: () => _tambahCepat(_items[index]),
                                onOpen: () => _bukaDetail(_items[index]),
                              ),
                      );
                    },
                  ),
          ),
        ),
      ],
    );

    final per = ref.watch(periodeProvider).value;
    final blokPeriode = per != null && !per.terbuka ? per : null;
    return TropicalScaffold(
      title: 'Pilih Produk',
      actions: [
        IconButton(
          tooltip: 'Scan barcode',
          icon: const Icon(Icons.qr_code_scanner, color: Colors.white),
          onPressed: blokPeriode != null ? null : _scan,
        ),
        const ProfilAksi(),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      // Periode bermasalah: layar jual diblokir sejak awal (limit hutang bergantung pada periode).
      body: blokPeriode != null
          ? _periodeBlokir(blokPeriode)
          : _isiJual(context, lebar, daftar),
      bottomNavigationBar: lebar ||
              cart.kosong ||
              blokPeriode != null ||
              MediaQuery.viewInsetsOf(context).bottom > 0
          ? null
          : SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: GradientActionButton(
                  label: 'Keranjang · ${rupiah(cart.perkiraan)}',
                  icon: Icons.shopping_cart_outlined,
                  onPressed: _bukaKeranjang,
                ),
              ),
            ),
    );
  }

  Widget _periodeBlokir(
    ({bool terbuka, String? pesan, String? kode}) p,
  ) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.lock_clock,
            size: 56,
            color: Theme.of(context).colorScheme.error,
          ),
          const SizedBox(height: 16),
          Text(
            judulPeriode(p.kode),
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          Text(
            '${p.pesan ?? ''}\nTransaksi tidak dapat dilakukan. $hubungiAdminPeriode',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 16),
          OutlinedButton.icon(
            onPressed: () => ref.invalidate(periodeProvider),
            icon: const Icon(Icons.refresh),
            label: const Text('Muat ulang'),
          ),
        ],
      ),
    ),
  );

  Widget _isiJual(BuildContext context, bool lebar, Widget daftar) =>
      _draftTertunda != null
      ? _pemulihanPanel(context)
      : lebar
      ? Row(
          children: [
            Expanded(child: daftar),
            const VerticalDivider(width: 1),
            SizedBox(
              width: 400,
              child: CartPanel(
                onBayar: () => Navigator.of(context)
                    .push(MaterialPageRoute(builder: (_) => const BayarPage())),
              ),
            ),
          ],
        )
      : daftar;

  void _tambahCepat(Barang b) {
    ref.read(cartProvider.notifier).tambah(b);
    pesan(context, '${b.nama} ditambahkan');
  }

  Future<void> _bukaDetail(Barang b) =>
      Navigator.of(context)
          .push(MaterialPageRoute(builder: (_) => BarangDetailPage(barang: b)));
}

class BarangCard extends StatelessWidget {
  const BarangCard({
    super.key,
    required this.b,
    required this.onAdd,
    required this.onOpen,
  });
  final Barang b;
  final VoidCallback onAdd, onOpen;
  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onOpen,
      child: Padding(
        padding: const EdgeInsets.all(8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Stack(
                fit: StackFit.expand,
                children: [
                  FuelArtwork(
                    name: b.nama,
                    radius: 16,
                    produkId: b.id,
                    fotoVersi: b.fotoVersi,
                  ),
                  Align(
                    alignment: Alignment.topRight,
                    child: Padding(
                      padding: const EdgeInsets.all(8),
                      child: StatePill(
                        text: b.dijualPerLiter ? 'BBM' : b.kategori,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Text(
              b.nama,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.titleSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            Text(
              b.kode.isEmpty ? b.kategori : b.kode,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            if (b.stok != null)
              Text(
                'Stok ${b.stok} ${b.satuan}',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            Row(
              children: [
                Expanded(
                  child: Text(
                    rupiah(b.harga),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                IconButton.filledTonal(
                  tooltip: 'Tambah ${b.nama}',
                  onPressed: onAdd,
                  visualDensity: VisualDensity.compact,
                  icon: const Icon(Icons.add, size: 20),
                ),
              ],
            ),
            Text(
              b.dijualPerLiter ? 'per liter' : 'per ${b.satuan}',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

enum _JumlahMode { liter, nominal }

class BarangDetailPage extends ConsumerStatefulWidget {
  const BarangDetailPage({super.key, required this.barang});
  final Barang barang;

  @override
  ConsumerState<BarangDetailPage> createState() => _BarangDetailPageState();
}

class _BarangDetailPageState extends ConsumerState<BarangDetailPage> {
  late final TextEditingController _jumlah = TextEditingController(
    text: widget.barang.dijualPerLiter ? '50000' : '1',
  );
  late _JumlahMode _mode = widget.barang.dijualPerLiter
      ? _JumlahMode.nominal
      : _JumlahMode.liter;

  int get _qtyCenti {
    if (!widget.barang.dijualPerLiter) return digitsToInt(_jumlah.text) * 100;
    if (_mode == _JumlahMode.liter) return parseLiterCenti(_jumlah.text) ?? 0;
    return nominalKeCenti(digitsToInt(_jumlah.text), widget.barang.harga);
  }

  int get _perkiraan => (widget.barang.harga * _qtyCenti + 50) ~/ 100;

  @override
  void dispose() {
    _jumlah.dispose();
    super.dispose();
  }

  void _tambah() {
    if (_qtyCenti <= 0 || _qtyCenti > Cart.maxQtyCenti) {
      pesan(
        context,
        'Jumlah harus lebih dari 0 dan maksimal ${Cart.maxQty} ${widget.barang.dijualPerLiter ? 'liter' : 'unit'}',
      );
      return;
    }
    ref.read(cartProvider.notifier).tambah(widget.barang, qtyCenti: _qtyCenti);
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final b = widget.barang;
    return TropicalScaffold(
      title: b.nama,
      leading: IconButton(
        tooltip: 'Kembali',
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        onPressed: () => Navigator.pop(context),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: GradientActionButton(
            label: 'Tambah ke keranjang · ${rupiah(_perkiraan)}',
            icon: Icons.add_shopping_cart,
            onPressed: _qtyCenti > 0 ? _tambah : null,
          ),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          // Foto persegi: tampil persegi di tengah (maks 260); tanpa foto tetap banner lebar.
          if (FuelArtwork.adaFoto(b.nama, b.fotoVersi))
            Center(
              child: SizedBox(
                width: 260,
                height: 260,
                child: FuelArtwork(
                  name: b.nama,
                  height: 260,
                  radius: 26,
                  produkId: b.id,
                  fotoVersi: b.fotoVersi,
                ),
              ),
            )
          else
            FuelArtwork(name: b.nama, height: 190, radius: 26),
          const SizedBox(height: 16),
          Text(b.nama, style: Theme.of(context).textTheme.headlineSmall),
          Text(
            b.kode,
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              StatePill(text: '${rupiah(b.harga)} / ${b.satuan}'),
              StatePill(text: b.kategori),
              if (b.stok != null) StatePill(text: 'Stok ${b.stok} ${b.satuan}'),
            ],
          ),
          const SizedBox(height: 18),
          if (b.dijualPerLiter)
            SegmentedButton<_JumlahMode>(
              segments: const [
                ButtonSegment(value: _JumlahMode.liter, label: Text('Liter')),
                ButtonSegment(
                  value: _JumlahMode.nominal,
                  label: Text('Nominal (Rp)'),
                ),
              ],
              selected: {_mode},
              showSelectedIcon: false,
              onSelectionChanged: (selection) => setState(() {
                _mode = selection.first;
                _jumlah.text = _mode == _JumlahMode.nominal ? '50000' : '1,00';
              }),
            ),
          const SizedBox(height: 14),
          TextField(
            controller: _jumlah,
            keyboardType: b.dijualPerLiter && _mode == _JumlahMode.liter
                ? const TextInputType.numberWithOptions(decimal: true)
                : TextInputType.number,
            inputFormatters: !b.dijualPerLiter || _mode == _JumlahMode.nominal
                ? [FilteringTextInputFormatter.digitsOnly]
                : null,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: !b.dijualPerLiter
                  ? 'Jumlah (${b.satuan})'
                  : _mode == _JumlahMode.nominal
                  ? 'Nominal'
                  : 'Jumlah liter',
              prefixText: b.dijualPerLiter && _mode == _JumlahMode.nominal
                  ? 'Rp '
                  : null,
              suffixText: b.dijualPerLiter && _mode == _JumlahMode.liter
                  ? 'L'
                  : null,
            ),
            style: Theme.of(context).textTheme.headlineSmall
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          if (b.dijualPerLiter && _mode == _JumlahMode.nominal)
            Text(
              _qtyCenti == 0
                  ? 'Masukkan nominal untuk menghitung liter.'
                  : '≈ ${qtyString(_qtyCenti, liter: true)}',
              style: TextStyle(
                color: Theme.of(context).colorScheme.primary,
                fontWeight: FontWeight.w600,
              ),
            ),
          if (_qtyCenti > 0)
            Text(
              'Perkiraan total ${rupiah(_perkiraan)}',
              style: Theme.of(context).textTheme.titleMedium,
            ),
          if (b.dijualPerLiter && _mode == _JumlahMode.nominal)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(
                'Liter dibulatkan ke 2 desimal; total dapat berbeda tipis dari nominal. Total akhir dihitung server.',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ),
          const SizedBox(height: 12),
          if (b.dijualPerLiter && _mode == _JumlahMode.nominal)
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final value in [20000, 50000, 100000])
                  ActionChip(
                    label: Text(rupiah(value)),
                    onPressed: () => setState(() => _jumlah.text = '$value'),
                  ),
              ],
            )
          else
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final value in [1, 5, 10])
                  ActionChip(
                    label: Text(
                      b.dijualPerLiter ? '$value,00 L' : '$value ${b.satuan}',
                    ),
                    onPressed: () => setState(
                      () => _jumlah.text = b.dijualPerLiter
                          ? '$value,00'
                          : '$value',
                    ),
                  ),
              ],
            ),
          if (b.stok != null) ...[
            const SizedBox(height: 12),
            Text(
              'Stok tersedia ${b.stok} ${b.satuan}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ],
      ),
    );
  }
}

class CartPanel extends ConsumerWidget {
  const CartPanel({super.key, required this.onBayar, this.controller});
  final VoidCallback onBayar;
  final ScrollController? controller;

  Future<void> _ubahQty(BuildContext c, WidgetRef ref, CartLine l) async {
    final whole = l.qtyCenti ~/ 100;
    final fraction = (l.qtyCenti % 100).toString().padLeft(2, '0');
    final t = TextEditingController(
      text: l.barang.dijualPerLiter ? '$whole,$fraction' : '$whole',
    );
    final v = await showDialog<int>(
      context: c,
      builder: (_) => AlertDialog(
        title: Text(l.barang.nama),
        content: TextField(
          controller: t,
          autofocus: true,
          keyboardType: l.barang.dijualPerLiter
              ? const TextInputType.numberWithOptions(decimal: true)
              : TextInputType.number,
          inputFormatters: l.barang.dijualPerLiter
              ? null
              : [FilteringTextInputFormatter.digitsOnly],
          decoration: InputDecoration(
            labelText: l.barang.dijualPerLiter
                ? 'Jumlah liter'
                : 'Qty (${l.barang.satuan})',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c),
            child: const Text('Batal'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(
              c,
              l.barang.dijualPerLiter
                  ? parseLiterCenti(t.text)
                  : digitsToInt(t.text) * 100,
            ),
            child: const Text('Simpan'),
          ),
        ],
      ),
    );
    t.dispose();
    if (v != null) ref.read(cartProvider.notifier).aturCenti(l.barang.id, v);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final per = ref.watch(periodeProvider).value;
    final blokPeriode = per != null && !per.terbuka;
    final n = ref.read(cartProvider.notifier);
    final peringatan = cart.peringatanStok();
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  'Keranjang',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
              ),
              if (!cart.kosong)
                TextButton(
                  onPressed: n.kosongkan,
                  child: const Text('Kosongkan'),
                ),
            ],
          ),
        ),
        Expanded(
          child: cart.kosong
              ? const Center(child: Text('Ketuk barang untuk menambah'))
              : ListView(
                  controller: controller,
                  children: [
                    for (final l in cart.lines)
                      ListTile(
                        leading: SizedBox(
                          width: 54,
                          height: 54,
                          child: FuelArtwork(
                            name: l.barang.nama,
                            height: 54,
                            radius: 14,
                            produkId: l.barang.id,
                            fotoVersi: l.barang.fotoVersi,
                          ),
                        ),
                        title: Text(l.barang.nama),
                        subtitle: Text(
                          '${qtyString(l.qtyCenti, liter: l.barang.dijualPerLiter)} × ${rupiah(l.barang.harga)} = ${rupiah(l.subtotal)}',
                        ),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              tooltip: 'Kurangi',
                              icon: const Icon(Icons.remove_circle_outline),
                              onPressed: () =>
                                  n.aturCenti(l.barang.id, l.qtyCenti - 100),
                            ),
                            InkWell(
                              onTap: () => _ubahQty(context, ref, l),
                              child: Padding(
                                padding: const EdgeInsets.all(8),
                                child: Text(
                                  qtyString(
                                    l.qtyCenti,
                                    liter: l.barang.dijualPerLiter,
                                  ),
                                  style: Theme.of(context).textTheme.titleSmall,
                                ),
                              ),
                            ),
                            IconButton(
                              tooltip: 'Tambah',
                              icon: const Icon(Icons.add_circle_outline),
                              onPressed: () =>
                                  n.aturCenti(l.barang.id, l.qtyCenti + 100),
                            ),
                          ],
                        ),
                      ),
                  ],
                ),
        ),
        if (peringatan.isNotEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              peringatan.join('\n'),
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ),
        Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Perkiraan total'),
                  Text(
                    rupiah(cart.perkiraan),
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ],
              ),
              const Text(
                'Potongan diskon qty dan total akhir dihitung server saat bayar.',
                style: TextStyle(fontSize: 12),
              ),
              const SizedBox(height: 12),
              GradientActionButton(
                label: blokPeriode
                    ? 'Periode tidak terbuka'
                    : online
                    ? 'Bayar'
                    : 'Offline - tidak bisa bayar',
                icon: Icons.payments_outlined,
                onPressed: cart.kosong || !online || blokPeriode
                    ? null
                    : onBayar,
              ),
            ],
          ),
        ),
      ],
    );
  }
}
