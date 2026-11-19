/// Pesan penolakan hapus permanen produk (server memberi rincian seperti "penjualan (3), stok tahunan (1)").
String pesanTidakBisaHapus(String rincian) => rincian.trim().isEmpty
    ? 'Produk sudah punya riwayat transaksi. Nonaktifkan saja agar tidak muncul di kasir.'
    : 'Produk sudah punya riwayat: $rincian. Nonaktifkan saja agar tidak muncul di kasir.';

/// Pesan kesalahan isian produk baru sebelum dikirim (server tetap memvalidasi); null = lolos.
String? cekProdukBaru({
  required String nama,
  required String satuan,
  required String kategoriId,
  required int harga,
  required int beli,
}) {
  if (nama.trim().isEmpty) return 'Nama wajib diisi';
  if (satuan.trim().isEmpty) return 'Satuan wajib diisi';
  if (kategoriId.isEmpty) return 'Pilih kategori';
  if (harga <= 0 || beli <= 0) return 'Harga jual dan harga beli harus lebih dari 0';
  if (harga <= beli) return 'Harga jual harus lebih besar dari harga beli';
  return null;
}
