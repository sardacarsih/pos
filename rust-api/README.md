# pos-api (Rust) + mobile (Flutter)

Kasir mobile dan "Transaksi Saya" anggota untuk Kopkar Kusuma Lestari. Database Oracle yang sama dengan desktop;
proyek C# tidak diubah. Tabel baru berprefiks `MOB_*` (user, sesi, kategori per user, nomor transaksi/idempotensi).

## Menjalankan
1. Pasang Oracle Instant Client (crate `oracle` memuat `oci.dll` saat runtime).
2. Salin `.env.example` ke `.env`, isi koneksi dan `POS_JWT_SECRET` (>= 32 karakter).
3. `cargo run -- migrate`, lalu `cargo run -- user-add --username admin --nama "Admin" --role ADMIN`
   (password sementara dicetak sekali; wajib diganti saat login pertama), lalu `cargo run`.
4. HTTPS: dihentikan di reverse proxy (Caddy/nginx); set `POS_TRUST_PROXY=true`. Server tidak melayani TLS sendiri.
5. Mobile: `flutter run --dart-define=API_URL=https://alamat-server`.

## Identitas
Satu akun = satu role: `KASIR` (penjualan), `ANGGOTA` (hanya data NIK-nya sendiri, NIK dari token), `ADMIN` (kelola user/kategori).
Pembatasan kategori per user kasir (`MOB_USER_KATEGORI`, kosong = semua) ditegakkan server di daftar, scan barcode, dan simpan faktur.

## Endpoint (`/api/v1`)
auth: `login`, `refresh`, `logout`, `ganti-password`, `GET /me` · kasir: `periode/status`, `transaksi/nomor`, `barang`,
`barang/by-barcode/{kode}`, `kategori`, `pelanggan`, `pelanggan/by-nik/{nik}`, `POST faktur`, `GET faktur/{no}` ·
anggota: `saya/ringkasan`, `saya/transaksi`, `saya/transaksi/{no}` · admin: `admin/dashboard`, `admin/produk` (+ `PUT admin/produk/{id}`, `PUT/DELETE admin/produk/{id}/foto`), `GET produk/{id}/foto`, `admin/users` (+ `reset-password`, `aktif`, `kategori`), `admin/kategori`.

## Foto produk
Admin mengunggah foto lewat `PUT /admin/produk/{id}/foto` (body = bytes PNG/JPEG mentah). Server memvalidasi isi file: maks 1 MB,
jenis dari magic bytes, file lengkap (tidak terpotong), sisi 200-2048 px, dan hampir persegi (selisih lebar-tinggi maks 10%).
Disimpan di tabel `MOB_PRODUK_FOTO` (BLOB; dibuat oleh `cargo run -- migrate`, **jalankan sebelum deploy**); `POS_PRODUCT` tidak diubah.
`foto` (di `/barang`) / `foto_versi` (di `/admin/produk`) = epoch detik unggah atau null; klien memakainya sebagai kunci cache.

## Idempotensi dan offline
Fase 1 wajib online. `POST /transaksi/nomor` dan `POST /faktur` wajib menerima header `Idempotency-Key` UUID v4.
Mobile memakai satu kunci per checkout. Penerbitan nomor dengan kunci yang sama mengembalikan nomor yang sama;
penyimpanan faktur dengan kunci dan payload yang sama mengembalikan faktur yang sama (`duplikat: true`),
sedangkan payload berbeda menghasilkan `409 KONFLIK`. Catatan kunci tersimpan di `MOB_IDEMPOTENCY` tanpa kedaluwarsa
agar checkout dapat dipulihkan setelah aplikasi dimulai ulang. Jalankan `cargo run -- migrate` sebelum deploy API.
Nomor BBM-yy-nnnnnn tetap memakai counter `NOMOR_TRANSAKSI` kode `BBM` (terpisah dari counter desktop `PENJUALAN`, FOR UPDATE).
Untuk offline kelak: antrean lokal faktur bernomor yang sudah diterbitkan, kontrol limit tetap di server saat sinkron
(faktur kredit bisa ditolak saat sinkron), cache katalog dan pelanggan yang lebih lengkap.

## Batasan yang diketahui
- Tidak ada OpenAPI; model Dart ditulis tangan di `mobile/lib/core/models.dart`.
- Struk: PDF 58 mm lewat dialog cetak/bagikan; printer thermal Bluetooth langsung belum diimplementasikan.
- Harga rupiah tetap bilangan bulat; qty API menerima angka desimal hingga dua tempat, dengan perhitungan server memakai Decimal.
- Cache lokal katalog belum ada (pencarian lewat server dengan paging dan debounce).
