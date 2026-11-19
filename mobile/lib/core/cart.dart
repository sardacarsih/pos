import 'models.dart';

class CartLine {
  CartLine(this.barang, this.qtyCenti);
  final Barang barang;
  int qtyCenti;
  num get qty => qtyCenti % 100 == 0 ? qtyCenti ~/ 100 : qtyCenti / 100;
  int get subtotal => (barang.harga * qtyCenti + 50) ~/ 100;
}

/// Keranjang murni (tanpa UI). Pemilihan dari daftar dan hasil scan memakai `tambah` yang sama.
/// Potongan diskon qty dan total akhir dihitung server saat simpan; angka di sini hanya perkiraan.
class Cart {
  static const maxQty = 1000;
  static const maxQtyCenti = maxQty * 100;
  final List<CartLine> lines = [];

  int get jumlahItem => lines.length;
  int get perkiraan => lines.fold(0, (a, l) => a + l.subtotal);
  bool get kosong => lines.isEmpty;

  void tambah(Barang b, {int qty = 1, int? qtyCenti}) {
    final amount = qtyCenti ?? qty * 100;
    final i = lines.indexWhere((l) => l.barang.id == b.id);
    if (i >= 0) {
      lines[i].qtyCenti = (lines[i].qtyCenti + amount).clamp(1, maxQtyCenti);
    } else {
      lines.add(CartLine(b, amount.clamp(1, maxQtyCenti)));
    }
  }

  /// Whole-unit compatibility for existing callers.
  void atur(int barangId, int qty) {
    aturCenti(barangId, qty * 100);
  }

  /// qtyCenti <= 0 removes the row; one centi-unit represents 0.01 L.
  void aturCenti(int barangId, int qtyCenti) {
    final i = lines.indexWhere((l) => l.barang.id == barangId);
    if (i < 0) return;
    if (qtyCenti <= 0) {
      lines.removeAt(i);
    } else {
      lines[i].qtyCenti = qtyCenti.clamp(1, maxQtyCenti);
    }
  }

  void kosongkan() => lines.clear();

  List<Map<String, Object>> toRequest() => [
    for (final l in lines)
      {
        'product_id': l.barang.id,
        'qty': l.qtyCenti % 100 == 0 ? l.qtyCenti ~/ 100 : l.qtyCenti / 100,
      },
  ];

  /// Peringatan stok (hanya informasi: server yang menegakkan bila kontrol stok aktif).
  List<String> peringatanStok() => [
    for (final l in lines)
      if (l.barang.stok != null && l.qtyCenti > l.barang.stok! * 100)
        '${l.barang.nama}: qty ${qtyString(l.qtyCenti, liter: l.barang.dijualPerLiter)} melebihi stok ${l.barang.stok}',
  ];
}

String qtyString(int qtyCenti, {required bool liter}) {
  if (!liter) return '${qtyCenti ~/ 100}';
  final whole = qtyCenti ~/ 100;
  final fraction = (qtyCenti % 100).toString().padLeft(2, '0');
  return '$whole,$fraction L';
}

int? parseLiterCenti(String value) {
  final text = value.trim().replaceAll(' ', '').replaceAll(',', '.');
  if (!RegExp(r'^\d+(\.\d{1,2})?$').hasMatch(text)) return null;
  final parts = text.split('.');
  final whole = int.tryParse(parts.first);
  if (whole == null) return null;
  final decimal = parts.length == 1
      ? 0
      : int.parse(parts.last.padRight(2, '0'));
  return whole * 100 + decimal;
}
