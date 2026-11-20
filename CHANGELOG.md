# Changelog

Ringkasan perubahan per platform. Disusun dari riwayat commit; tanggal sesuai tanggal commit.

## 18–20 November 2026

### Web (admin.kopkarksk.com)

**Login dan tampilan**
- Login: opsi "Ingat saya" (sesi 1 hari atau 30 hari), ikon username/password, tombol tampilkan kata sandi, dan logo Kopkar.
- Antarmuka: ikon Lucide dan sidebar berkelompok dengan bottom-nav "Lainnya" di ponsel; topbar baru (judul h1, breadcrumb, menu pengguna, chip koneksi dan periode); halaman Pengguna didesain ulang; service worker per versi rilis.
- Beranda per peran (Kasir, Anggota, Admin); admin mendapat rincian penjualan tunai vs kredit dan per kasir.

**Pembelian**
- Posting, riwayat, hutang supplier, mode offline, role PEMBELIAN, rekonsiliasi faktur BackOffice, dan pengelolaan supplier.
- Angka berformat Indonesia dan rata kanan; perbaikan metode pembayaran dan kueri detail faktur.
- Multi satuan:
  - Satuan pembelian hanya satuan dasar dan tambahan milik produk.
  - Kolom Satuan (combobox) sebelum Jumlah.
  - Ringkasan konversi stok dan harga per satuan dasar, mis. `1 SLOP × 12 = 12 BOX masuk stok`.
  - Harga beli terakhir per satuan jadi harga awal, dengan peringatan bila menyimpang lebih dari 30%.
  - Scan ulang barang dan satuan yang sama menambah jumlah.
  - Dialog tinjau memuat konversi per baris dan total stok masuk.
  - Baris menjadi kartu di layar kecil.

**Katalog dan satuan**
- Tombol ikon dan hapus permanen; produk tanpa HPP ikut di Harga ≤ HPP.
- Kode produk otomatis `KB-yy-nnnnn`, tambah produk multi satuan, CRUD kategori berikon.
- Satuan jual tambahan (isi, harga, barcode per produk) dan CRUD master Satuan; satuan produk wajib dari master.
- Ubah produk menjadi drawer untuk operasional.

**Persediaan dan keuangan**
- Monitoring Stok habis dan Harga ≤ HPP; koreksi stok lewat stock opname.
- Stock opname: dokumen multi barang, riwayat SO, alur lebih cepat, dan opname multi satuan (hitung fisik per satuan).
- Modul Persediaan 5 tab (kartu stok, penyesuaian berjejak, valuasi, ambang minimum), tab Valuasi Item, dan Qty Stok yang bisa diurutkan di Stok habis.
- Kas & Bank (akun, mutasi, transfer, tarik transaksi) dalam empat tab.
- Pinjaman anggota: pengajuan, verifikasi, persetujuan, antrian dana, pencairan, dan laporan pinjaman aktif.

### Mobile (Android)

**1.0.11 (20 November)**
- Perbaikan pencarian "Opname baru".
- Opname multi satuan: hitung fisik per satuan.
- Beranda disusun ulang per peran (Kasir, Anggota, Admin).
- Halaman Satuan (tab baru) dan dropdown satuan di form produk; crash form produk diperbaiki.

**1.0.10 (19 November)**
- Alur pinjaman untuk anggota dan pengurus, notifikasi push status pinjaman.
- Filter status pinjaman tidak lagi meluber di layar ponsel.
- Tambah produk baru dengan multi satuan.

**18 November**
- Opsi "Ingat saya" di login.
- Rincian penjualan tunai vs kredit dan per kasir di Beranda admin.
- Monitoring Stok habis dan Harga ≤ HPP; opname multi barang dengan riwayat SO; alur opname lebih cepat.

Pembelian tidak tersedia di mobile.

### API dan database
- Migrasi: Migrator C# dipindah ke `pos-api migrate`. Tambahan `0011_satuan_master` dan `0012_satuan_master_id` (master Satuan), serta `0013_product_satuan_beli` (kolom `HARGA_BELI` di `POS_PRODUCT_SATUAN`).
- Endpoint baru atau diperluas: `/api/v1/admin/satuan`, Persediaan, Kas & Bank, pinjaman, dan Pembelian.
- Alat operasional: `pos-api selftest` (query baca saja) berjalan otomatis sebelum biner ditukar saat deploy; `user-add-anggota` untuk akun anggota massal dengan ekspor xlsx; skrip deploy web, preflight, dan runbook (`docs/deploy-produksi.md`).

### Produksi
- API, migrasi `0011`–`0013`, dan web terpasang pada 20 November 2026.
- APK 1.0.10 dan 1.0.11 dirilis lewat jalur terpisah (`mobile\tools\rilis-apk.ps1`).
