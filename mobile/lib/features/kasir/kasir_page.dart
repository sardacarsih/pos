import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/api.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/cart.dart';
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
        appBar: AppBar(title: Text(widget.judul)),
        body: MobileScanner(
          onDetect: (cap) {
            final v = cap.barcodes.isEmpty ? null : cap.barcodes.first.rawValue;
            if (v != null && v.isNotEmpty && !_selesai) {
              _selesai = true;
              Navigator.of(context).pop(v);
            }
          },
          errorBuilder: (c, e) => const Center(
            child: Padding(padding: EdgeInsets.all(24), child: Text('Kamera tidak tersedia atau izin ditolak. Gunakan pencarian dari daftar.', textAlign: TextAlign.center)),
          ),
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

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300) _muatLagi();
    });
    ref.read(apiProvider).kategori().then((k) {
      if (mounted) setState(() => _kategori = k);
    }).catchError((_) {});
    _muat();
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
      final r = await ref.read(apiProvider).barang(q: _q.text.trim(), kategori: _chip, page: _page + 1);
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

  /// Hasil scan masuk lewat jalur yang sama dengan memilih dari daftar (cart.tambah).
  Future<void> _scan() async {
    final kode = await Navigator.of(context).push<String>(MaterialPageRoute(builder: (_) => const ScanPage()));
    if (kode == null || !mounted) return;
    try {
      final b = await ref.read(apiProvider).barangBarcode(kode);
      ref.read(cartProvider.notifier).tambah(b);
      if (mounted) pesan(context, '${b.nama} ditambahkan');
    } on ApiException catch (e) {
      if (mounted) pesan(context, e.status == 404 ? 'Barang dengan barcode itu tidak ditemukan' : e.pesan);
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
          builder: (c, sc) => CartPanel(controller: sc, onBayar: () {
            Navigator.of(c).pop();
            Navigator.of(context).push(MaterialPageRoute(builder: (_) => const BayarPage()));
          }),
        ),
      );

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final lebar = MediaQuery.sizeOf(context).width >= 900;

    final daftar = Column(children: [
      if (!online) Container(width: double.infinity, color: Theme.of(context).colorScheme.errorContainer, padding: const EdgeInsets.all(8), child: const Text('Tidak ada koneksi. Pembayaran dinonaktifkan.', textAlign: TextAlign.center)),
      Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
        child: TextField(
          controller: _q,
          onChanged: _cari,
          decoration: InputDecoration(
            hintText: 'Cari nama atau kode barang',
            prefixIcon: const Icon(Icons.search),
            border: const OutlineInputBorder(),
            suffixIcon: _q.text.isEmpty ? null : IconButton(tooltip: 'Hapus', icon: const Icon(Icons.clear), onPressed: () { _q.clear(); _muat(); }),
          ),
        ),
      ),
      if (_kategori.length > 1)
        SizedBox(
          height: 48,
          child: ListView(scrollDirection: Axis.horizontal, padding: const EdgeInsets.symmetric(horizontal: 12), children: [
            Padding(padding: const EdgeInsets.only(right: 8), child: ChoiceChip(label: const Text('Semua'), selected: _chip == null, onSelected: (_) { setState(() => _chip = null); _muat(); })),
            for (final k in _kategori)
              Padding(padding: const EdgeInsets.only(right: 8), child: ChoiceChip(label: Text(k.nama), selected: _chip == k.id, onSelected: (_) { setState(() => _chip = k.id); _muat(); })),
          ]),
        ),
      Expanded(
        child: RefreshIndicator(
          onRefresh: _muat,
          child: ListView(
            controller: _scroll,
            physics: const AlwaysScrollableScrollPhysics(),
            children: [
              if (_loading && _items.isEmpty) const Padding(padding: EdgeInsets.all(48), child: Center(child: CircularProgressIndicator())),
              if (_error != null) Padding(padding: const EdgeInsets.all(24), child: Column(children: [Text(_error!), TextButton(onPressed: _muat, child: const Text('Coba lagi'))])),
              if (!_loading && _error == null && _items.isEmpty) const Padding(padding: EdgeInsets.all(48), child: Center(child: Text('Barang tidak ditemukan'))),
              for (final b in _items) _BarangTile(b: b, onTap: () => ref.read(cartProvider.notifier).tambah(b)),
              if (_lagi) const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator())),
            ],
          ),
        ),
      ),
    ]);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Kasir'),
        actions: [
          IconButton(tooltip: 'Scan barcode (opsional)', icon: const Icon(Icons.qr_code_scanner), onPressed: _scan),
          IconButton(tooltip: 'Keluar', icon: const Icon(Icons.logout), onPressed: () => ref.read(authProvider.notifier).logout()),
        ],
      ),
      body: lebar
          ? Row(children: [
              Expanded(flex: 3, child: daftar),
              const VerticalDivider(width: 1),
              SizedBox(width: 400, child: CartPanel(onBayar: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const BayarPage())))),
            ])
          : daftar,
      bottomNavigationBar: lebar || cart.kosong
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: FilledButton.icon(
                  onPressed: _bukaKeranjang,
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(56)),
                  icon: Badge(label: Text('${cart.jumlahItem}'), child: const Icon(Icons.shopping_cart)),
                  label: Text('Keranjang · ${rupiah(cart.perkiraan)}'),
                ),
              ),
            ),
    );
  }
}

class _BarangTile extends StatelessWidget {
  const _BarangTile({required this.b, required this.onTap});
  final Barang b;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) {
    final habis = b.stok != null && b.stok! <= 0;
    return ListTile(
      minVerticalPadding: 12,
      title: Text(b.nama),
      subtitle: Text('${b.kode} · ${b.kategori}${b.stok != null ? ' · stok ${b.stok}' : ''}'),
      trailing: Text(rupiah(b.harga), style: TextStyle(fontWeight: FontWeight.w600, color: habis ? Theme.of(context).disabledColor : null)),
      onTap: onTap,
    );
  }
}

class CartPanel extends ConsumerWidget {
  const CartPanel({super.key, required this.onBayar, this.controller});
  final VoidCallback onBayar;
  final ScrollController? controller;

  Future<void> _ubahQty(BuildContext c, WidgetRef ref, CartLine l) async {
    final t = TextEditingController(text: '${l.qty}');
    final v = await showDialog<int>(
      context: c,
      builder: (_) => AlertDialog(
        title: Text(l.barang.nama),
        content: TextField(controller: t, autofocus: true, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Qty')),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c), child: const Text('Batal')),
          FilledButton(onPressed: () => Navigator.pop(c, digitsToInt(t.text)), child: const Text('OK')),
        ],
      ),
    );
    t.dispose();
    if (v != null) ref.read(cartProvider.notifier).atur(l.barang.id, v);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cart = ref.watch(cartProvider).cart;
    final online = ref.watch(onlineProvider).value ?? true;
    final n = ref.read(cartProvider.notifier);
    final peringatan = cart.peringatanStok();
    return Column(children: [
      Padding(padding: const EdgeInsets.fromLTRB(16, 8, 16, 0), child: Row(children: [
        Expanded(child: Text('Keranjang', style: Theme.of(context).textTheme.titleLarge)),
        if (!cart.kosong) TextButton(onPressed: n.kosongkan, child: const Text('Kosongkan')),
      ])),
      Expanded(
        child: cart.kosong
            ? const Center(child: Text('Ketuk barang untuk menambah'))
            : ListView(controller: controller, children: [
                for (final l in cart.lines)
                  ListTile(
                    title: Text(l.barang.nama),
                    subtitle: Text('${rupiah(l.barang.harga)} × ${l.qty} = ${rupiah(l.subtotal)}'),
                    trailing: Row(mainAxisSize: MainAxisSize.min, children: [
                      IconButton(tooltip: 'Kurangi', icon: const Icon(Icons.remove_circle_outline), onPressed: () => n.atur(l.barang.id, l.qty - 1)),
                      InkWell(onTap: () => _ubahQty(context, ref, l), child: Padding(padding: const EdgeInsets.all(8), child: Text('${l.qty}', style: Theme.of(context).textTheme.titleMedium))),
                      IconButton(tooltip: 'Tambah', icon: const Icon(Icons.add_circle_outline), onPressed: () => n.atur(l.barang.id, l.qty + 1)),
                    ]),
                  ),
              ]),
      ),
      if (peringatan.isNotEmpty) Padding(padding: const EdgeInsets.symmetric(horizontal: 16), child: Text(peringatan.join('\n'), style: TextStyle(color: Theme.of(context).colorScheme.error))),
      Padding(
        padding: const EdgeInsets.all(16),
        child: Column(children: [
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [const Text('Perkiraan total'), Text(rupiah(cart.perkiraan), style: Theme.of(context).textTheme.titleLarge)]),
          const Text('Potongan diskon qty dan total akhir dihitung server saat bayar.', style: TextStyle(fontSize: 12)),
          const SizedBox(height: 12),
          FilledButton(
            onPressed: cart.kosong || !online ? null : onBayar,
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(56)),
            child: Text(online ? 'Bayar' : 'Offline - tidak bisa bayar'),
          ),
        ]),
      ),
    ]);
  }
}
