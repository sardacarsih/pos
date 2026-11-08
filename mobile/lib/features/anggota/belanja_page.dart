import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/cart.dart';
import '../../core/checkout.dart' show newCheckoutKey;
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import '../kasir/bayar_page.dart' show StrukPage;

/// Belanja mandiri anggota: pilih barang, bayar kredit potong gaji. NIK ditentukan server dari token.
// shortcut: qty bulat (tanpa pecahan liter) dan kunci idempotensi hanya di memori, bukan disimpan
// seperti checkout kasir; upgrade bila anggota perlu beli BBM per liter / pulih setelah app ditutup.
class BelanjaPage extends ConsumerStatefulWidget {
  const BelanjaPage({super.key, required this.onSelesai});
  final VoidCallback onSelesai;

  @override
  ConsumerState<BelanjaPage> createState() => _BelanjaPageState();
}

class _BelanjaPageState extends ConsumerState<BelanjaPage> {
  final _cart = Cart();
  final _cari = TextEditingController();
  final List<Barang> _items = [];
  bool _loading = true, _lagi = false, _busy = false;
  String? _error;
  int _page = 1, _tenor = 1;

  // Satu percobaan checkout = satu kunci + satu nomor; berubah hanya bila keranjang/tenor berubah.
  String? _key, _nomor;
  bool _demo = false;

  @override
  void initState() {
    super.initState();
    _muat();
    ref
        .read(apiProvider)
        .dashboardSaya()
        .then((d) {
          if (mounted) setState(() => _demo = d.belanjaDemo);
        })
        .catchError((_) {}); // banner saja; gagal tidak mengganggu belanja
  }

  @override
  void dispose() {
    _cari.dispose();
    super.dispose();
  }

  void _ubah(VoidCallback f) => setState(() {
    f();
    _key = _nomor = null;
  });

  Future<void> _muat({bool tambah = false}) async {
    if (!tambah) _page = 1;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref
          .read(apiProvider)
          .barang(q: _cari.text.trim(), page: tambah ? _page + 1 : 1);
      if (!mounted) return;
      setState(() {
        if (!tambah) _items.clear();
        _items.addAll(r.items);
        _lagi = r.lagi;
        if (tambah) _page++;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _bayar() async {
    if (_busy || _cart.kosong) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      final key = _key ??= newCheckoutKey();
      final nomor = _nomor ??= await api.nomorSaya(idempotencyKey: key);
      final f = await api.simpanSaya(
        idempotencyKey: key,
        nomor: nomor,
        tenor: _tenor,
        baris: _cart.toRequest(),
      );
      if (!mounted) return;
      setState(() {
        _cart.kosongkan();
        _key = _nomor = null;
      });
      if (f.no.startsWith('DEMO-')) {
        await showDialog<void>(
          context: context,
          builder: (c) => AlertDialog(
            title: const Text('Mode DEMO'),
            content: const Text(
              'Belanja ini hanya simulasi: faktur tidak tersimpan, limit hutang tidak berubah, dan tidak muncul di Riwayat.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(c),
                child: const Text('Mengerti'),
              ),
            ],
          ),
        );
        if (!mounted) return;
      }
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => StrukPage(faktur: f, dariRiwayat: true),
        ),
      );
      widget.onSelesai();
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return TropicalScaffold(
      title: 'Belanja Mandiri',
      body: Column(
        children: [
          if (_demo)
            Container(
              width: double.infinity,
              color: cs.tertiaryContainer,
              padding: const EdgeInsets.all(8),
              child: Text(
                'MODE DEMO · transaksi tidak disimpan',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: cs.onTertiaryContainer,
                ),
              ),
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            child: TextField(
              controller: _cari,
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => _muat(),
              decoration: InputDecoration(
                hintText: 'Cari barang',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(),
                suffixIcon: IconButton(
                  tooltip: 'Cari',
                  icon: const Icon(Icons.arrow_forward),
                  onPressed: _muat,
                ),
              ),
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Card(
                color: cs.errorContainer,
                child: ListTile(title: Text(_error!)),
              ),
            ),
          Expanded(
            child: _loading && _items.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : ListView(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    children: [
                      if (_items.isEmpty && !_loading)
                        const Padding(
                          padding: EdgeInsets.all(32),
                          child: Center(child: Text('Barang tidak ditemukan')),
                        ),
                      for (final b in _items)
                        Card(
                          margin: const EdgeInsets.only(bottom: 8),
                          child: ListTile(
                            title: Text(b.nama),
                            subtitle: Text('${rupiah(b.harga)} / ${b.satuan}'),
                            trailing: _qty(b),
                          ),
                        ),
                      if (_lagi)
                        TextButton(
                          onPressed: _loading ? null : () => _muat(tambah: true),
                          child: const Text('Muat lebih banyak'),
                        ),
                    ],
                  ),
          ),
          if (!_cart.kosong) _bawah(),
        ],
      ),
    );
  }

  Widget _qty(Barang b) {
    final i = _cart.lines.indexWhere((l) => l.barang.id == b.id);
    if (i < 0) {
      return IconButton.filledTonal(
        tooltip: 'Tambah ${b.nama}',
        icon: const Icon(Icons.add_shopping_cart),
        onPressed: _busy ? null : () => _ubah(() => _cart.tambah(b)),
      );
    }
    final q = _cart.lines[i].qtyCenti;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          tooltip: 'Kurangi',
          icon: const Icon(Icons.remove_circle_outline),
          onPressed: _busy ? null : () => _ubah(() => _cart.aturCenti(b.id, q - 100)),
        ),
        Text('${q ~/ 100}', style: const TextStyle(fontWeight: FontWeight.w700)),
        IconButton(
          tooltip: 'Tambah',
          icon: const Icon(Icons.add_circle_outline),
          onPressed: _busy ? null : () => _ubah(() => _cart.tambah(b)),
        ),
      ],
    );
  }

  Widget _bawah() => Material(
    elevation: 8,
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
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
                  child: Text(i == 1 ? '1x (sekali bayar)' : '${i}x angsuran'),
                ),
            ],
            onChanged: _busy ? null : (v) => _ubah(() => _tenor = v ?? 1),
          ),
          const SizedBox(height: 8),
          Text(
            '${_cart.jumlahItem} barang · perkiraan ${rupiah(_cart.perkiraan)} '
            '(total akhir & limit hutang dihitung server)',
          ),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              onPressed: _busy ? null : _bayar,
              child: _busy
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(_demo ? 'Coba Belanja (Demo)' : 'Belanja dengan Kredit'),
            ),
          ),
        ],
      ),
    ),
  );
}
