import 'models.dart';

class CartLine {
  CartLine(this.barang, this.qtyCenti, [this.satuan]);
  final Barang barang;
  int qtyCenti;

  /// Satuan jual tambahan yang dipilih; null = satuan dasar produk.
  final String? satuan;
  SatuanJual? get _lain {
    for (final s in barang.satuanLain) {
      if (s.satuan == satuan) return s;
    }
    return null;
  }

  int get harga => _lain?.harga ?? barang.harga;
  String get namaSatuan => _lain?.satuan ?? barang.satuan;
  double get isi => _lain?.isi ?? 1;
  num get qty => qtyCenti % 100 == 0 ? qtyCenti ~/ 100 : qtyCenti / 100;
  int get subtotal => (harga * qtyCenti + 50) ~/ 100;
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

  /// Kunci baris = (barang, satuan): satuan berbeda dari produk sama = baris terpisah.
  void tambah(Barang b, {int qty = 1, int? qtyCenti, String? satuan}) {
    final amount = qtyCenti ?? qty * 100;
    final sat = b.satuanLain.any((s) => s.satuan == satuan) ? satuan : null;
    final i = lines.indexWhere((l) => l.barang.id == b.id && l.satuan == sat);
    if (i >= 0) {
      lines[i].qtyCenti = (lines[i].qtyCenti + amount).clamp(1, maxQtyCenti);
    } else {
      lines.add(CartLine(b, amount.clamp(1, maxQtyCenti), sat));
    }
  }

  /// Ganti satuan sebuah baris; bila sudah ada baris dengan satuan itu, jumlahnya digabung.
  void ubahSatuan(CartLine l, String? baru) {
    if (l.satuan == baru) return;
    lines.remove(l);
    tambah(l.barang, qtyCenti: l.qtyCenti, satuan: baru);
  }

  /// Whole-unit compatibility for existing callers.
  void atur(int barangId, int qty, {String? satuan}) {
    aturCenti(barangId, qty * 100, satuan: satuan);
  }

  /// qtyCenti <= 0 removes the row; one centi-unit represents 0.01 L.
  void aturCenti(int barangId, int qtyCenti, {String? satuan}) {
    final i = lines.indexWhere(
      (l) => l.barang.id == barangId && l.satuan == satuan,
    );
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
        if (l.satuan != null) 'satuan': l.satuan!,
      },
  ];

  /// Peringatan stok (hanya informasi: server yang menegakkan bila kontrol stok aktif).
  List<String> peringatanStok() => [
    for (final l in lines)
      if (l.barang.stok != null && l.qtyCenti * l.isi > l.barang.stok! * 100)
        '${l.barang.nama}: qty ${qtyString(l.qtyCenti, liter: l.barang.dijualPerLiter)} ${l.namaSatuan} melebihi stok ${l.barang.stok}',
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
