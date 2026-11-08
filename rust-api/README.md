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
anggota: `saya/ringkasan`, `saya/transaksi`, `saya/transaksi/{no}` · admin: `admin/users` (+ `reset-password`, `aktif`, `kategori`), `admin/kategori`.

## Idempotensi dan offline
Fase 1 wajib online. `POST /transaksi/nomor` menerbitkan nomor W-yy-nnnnnn (tabel `NOMOR_TRANSAKSI`, FOR UPDATE) dan mencatatnya
di `MOB_NOMOR`; `POST /faktur` dengan nomor yang sama mengembalikan faktur yang sama (`duplikat: true`), tidak menggandakan.
Untuk offline kelak: antrean lokal faktur bernomor yang sudah diterbitkan, kontrol limit tetap di server saat sinkron
(faktur kredit bisa ditolak saat sinkron), cache katalog dan pelanggan yang lebih lengkap.

## Batasan yang diketahui
- Tidak ada OpenAPI; model Dart ditulis tangan di `mobile/lib/core/models.dart`.
- Struk: PDF 58 mm lewat dialog cetak/bagikan; printer thermal Bluetooth langsung belum diimplementasikan.
- Harga/qty di API bilangan bulat; nilai pecahan di DB ditampilkan dibulatkan (hanya tampilan, perhitungan server memakai desimal).
- Cache lokal katalog belum ada (pencarian lewat server dengan paging dan debounce).
