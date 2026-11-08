import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/cart.dart';
import 'package:kopkar_pos/core/format.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/main.dart';

Barang barang(int id, {int harga = 1000, int? stok}) => Barang.fromJson({
      'id': id, 'kode': 'K$id', 'barcode': 'B$id', 'nama': 'Barang $id', 'satuan': 'PCS',
      'harga': harga, 'stok': stok, 'kategori_id': '1', 'kategori': 'Umum',
    });

void main() {
  group('format', () {
    test('rupiah memakai pemisah ribuan titik', () {
      expect(rupiah(0), 'Rp 0');
      expect(rupiah(999), 'Rp 999');
      expect(rupiah(1000), 'Rp 1.000');
      expect(rupiah(1234567), 'Rp 1.234.567');
      expect(rupiah(-5000), '-Rp 5.000');
    });

    test('digitsToInt hanya mengambil digit, tidak mengurai desimal', () {
      expect(digitsToInt(''), 0);
      expect(digitsToInt('Rp 50.000'), 50000);
      expect(digitsToInt('abc'), 0);
    });

    test('periode', () {
      expect(geserPeriode(202601, -1), 202512);
      expect(geserPeriode(202612, 1), 202701);
      expect(namaPeriode(202603), 'Maret 2026');
    });
  });

  group('keranjang', () {
    test('tambah barang sama menaikkan qty (jalur daftar dan scan identik)', () {
      final c = Cart()..tambah(barang(1))..tambah(barang(1))..tambah(barang(2, harga: 500));
      expect(c.lines.length, 2);
      expect(c.jumlahItem, 3);
      expect(c.perkiraan, 2500);
    });

    test('atur qty 0 menghapus baris; batas maksimum 1000', () {
      final c = Cart()..tambah(barang(1));
      c.atur(1, 5000);
      expect(c.lines.single.qty, 1000);
      c.atur(1, 0);
      expect(c.kosong, isTrue);
    });

    test('toRequest hanya product_id dan qty (harga dihitung server)', () {
      final c = Cart()..tambah(barang(7), qty: 3);
      expect(c.toRequest(), [
        {'product_id': 7, 'qty': 3}
      ]);
    });

    test('peringatan stok informatif', () {
      final c = Cart()..tambah(barang(1, stok: 2), qty: 5);
      expect(c.peringatanStok().single, contains('melebihi stok 2'));
      expect((Cart()..tambah(barang(1))).peringatanStok(), isEmpty);
    });
  });

  group('routing per role', () {
    Profil p(String role, {bool ganti = false}) => Profil(role: role, nama: 'X', gantiPassword: ganti);

    test('belum login -> /login', () {
      expect(redirectUntuk(const AuthState(), '/kasir'), '/login');
      expect(redirectUntuk(const AuthState(), '/login'), isNull);
    });

    test('ANGGOTA langsung ke layar Transaksi Saya dan tidak bisa membuka kasir/admin', () {
      final a = AuthState(profil: p('ANGGOTA'));
      expect(redirectUntuk(a, '/login'), '/saya');
      expect(redirectUntuk(a, '/kasir'), '/saya');
      expect(redirectUntuk(a, '/admin'), '/saya');
      expect(redirectUntuk(a, '/saya/faktur/W-26-000001'), isNull);
    });

    test('KASIR dan ADMIN dibatasi ke bagiannya', () {
      expect(redirectUntuk(AuthState(profil: p('KASIR')), '/saya'), '/kasir');
      expect(redirectUntuk(AuthState(profil: p('ADMIN')), '/kasir'), '/admin');
    });

    test('password sementara memaksa halaman ganti password', () {
      expect(redirectUntuk(AuthState(profil: p('KASIR', ganti: true)), '/kasir'), '/ganti');
    });
  });
}
