import 'models.dart';

class CartLine {
  CartLine(this.barang, this.qty);
  final Barang barang;
  int qty;
  int get subtotal => barang.harga * qty;
}

/// Keranjang murni (tanpa UI). Pemilihan dari daftar dan hasil scan memakai `tambah` yang sama.
/// Potongan diskon qty dan total akhir dihitung server saat simpan; angka di sini hanya perkiraan.
class Cart {
  static const maxQty = 1000;
  final List<CartLine> lines = [];

  int get jumlahItem => lines.fold(0, (a, l) => a + l.qty);
  int get perkiraan => lines.fold(0, (a, l) => a + l.subtotal);
  bool get kosong => lines.isEmpty;

  void tambah(Barang b, {int qty = 1}) {
    final i = lines.indexWhere((l) => l.barang.id == b.id);
    if (i >= 0) {
      lines[i].qty = (lines[i].qty + qty).clamp(1, maxQty);
    } else {
      lines.add(CartLine(b, qty.clamp(1, maxQty)));
    }
  }

  /// qty <= 0 menghapus baris.
  void atur(int barangId, int qty) {
    final i = lines.indexWhere((l) => l.barang.id == barangId);
    if (i < 0) return;
    if (qty <= 0) {
      lines.removeAt(i);
    } else {
      lines[i].qty = qty.clamp(1, maxQty);
    }
  }

  void kosongkan() => lines.clear();

  List<Map<String, int>> toRequest() =>
      [for (final l in lines) {'product_id': l.barang.id, 'qty': l.qty}];

  /// Peringatan stok (hanya informasi: server yang menegakkan bila kontrol stok aktif).
  List<String> peringatanStok() => [
        for (final l in lines)
          if (l.barang.stok != null && l.qty > l.barang.stok!)
            '${l.barang.nama}: qty ${l.qty} melebihi stok ${l.barang.stok}',
      ];
}
