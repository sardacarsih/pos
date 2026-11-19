/// Pesan penolakan hapus permanen produk (server memberi rincian seperti "penjualan (3), stok tahunan (1)").
String pesanTidakBisaHapus(String rincian) => rincian.trim().isEmpty
    ? 'Produk sudah punya riwayat transaksi. Nonaktifkan saja agar tidak muncul di kasir.'
    : 'Produk sudah punya riwayat: $rincian. Nonaktifkan saja agar tidak muncul di kasir.';
