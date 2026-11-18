# Desain UI/UX mobile Kopkar POS

Dokumen FigCraft mengikuti navigasi dan fitur yang saat ini aktif di `mobile/lib`, dengan satu dokumen per area aplikasi. Frame ponsel memakai ukuran 375 × 812; layout tablet kasir memakai 1024 × 768 sesuai breakpoint 900 px pada aplikasi.

| File | Cakupan |
|---|---|
| `umum.figcraft` | Login, ganti password wajib, profil, ganti password dari profil, foto/crop, riwayat versi, pembaruan Android, dan status memuat/gagal/offline. |
| `kasir.figcraft` | Dashboard, katalog BBM dan pelumas, jumlah, keranjang, pelanggan dan QR, pembayaran tunai/kredit, limit, faktur/cetak, riwayat per tanggal, printer Bluetooth, serta tablet. |
| `anggota.figcraft` | Dashboard, limit periode, belanja mandiri, tenor/demo, Transaksi Saya, detail faktur/angsuran, dan kartu QR anggota. |
| `admin.figcraft` | Dashboard, produk dan filter kategori kasir, editor produk, foto produk, konfirmasi perubahan harga, urutan produk, rekap anggota/limit, dan pengelolaan user. |

Frame utama dan status dialog tersedia dalam mode terang serta gelap. Layar kamera QR dirender gelap seperti kamera perangkat. PNG tiap frame diekspor ke `png/<role>/` dengan nomor urut yang sama seperti nama frame.

## Bangun ulang

Jalankan dari direktori `design`:

```powershell
node build.mjs
```

Generator membutuhkan `figcraft-cli.exe`; lokasi alternatif dapat diatur lewat `FIGCRAFT_CLI`. Sumber layar ada di `screens_*.mjs`, token warna di `theme.mjs`, primitive FigCraft di `lib.mjs`, dan pembagian dokumen/urutan frame di `build.mjs`.

## Batas perilaku yang jadi acuan desain

- Kasir memakai tab Dashboard, Jual, dan Riwayat. Riwayat memilih satu tanggal; detail transaksi dapat dicetak ulang.
- Anggota mulai di Dashboard, lalu dapat membuka Belanja, Riwayat/Transaksi Saya, dan Kartu Anggota.
- Admin memakai tab Dashboard, Produk, Rekap, dan User. Produk diedit lewat dialog; kategori mengatur filter katalog kasir dan dapat diurutkan. Aplikasi tidak membuat atau menghapus produk.
- Total belanja dan angsuran sebelum disimpan adalah perkiraan. Server menentukan total akhir dan memeriksa limit.
- Saat offline, transaksi tidak dapat disimpan. Status limit yang terlampaui memakai teks, angka, dan ikon; tidak hanya warna.
- QR di file desain bersifat dekoratif dan tidak dapat dipindai. QR anggota memuat NIK; jangan gunakan contoh sebagai data nyata.
- Cetak Bluetooth Classic/ESC-POS hanya tersedia pada Android. PDF/berbagi tetap tersedia.
- Cek/pemasangan update APK hanya untuk Android native. iPhone memakai PWA dan mendapat versi terbaru saat aplikasi web dibuka ulang.

## Integrasi live FigCraft

Untuk membuka file di aplikasi FigCraft yang berjalan, mulai dengan `figcraft.exe --control 9333`, lalu jalankan dari direktori `design`:

- `node open_live.mjs [role]` membuka file role.
- `node show.mjs <role>` menampilkan tab dan menyimpan screenshot ke `live.png`.
- `node live.mjs tools|call <tool> <json>` memanggil tool MCP aplikasi.
- `node reload_live.mjs [role]` membuka ulang dokumen hasil generator dari disk.

Role yang tersedia: `umum`, `kasir`, `anggota`, `admin`.

## Sistem visual

Warna dan tipografi menjaga tema yang digunakan aplikasi:

- Terang: latar `#EEF7EA`, permukaan `#FFFFFF`, teks `#1E2B22`, aksen `#0E7A2E`.
- Gelap: latar `#10241D`, permukaan `#17211B`, teks `#E6F5EA`, aksen `#8BE0A6`.
- Poppins dipakai untuk teks. Login memakai ilustrasi/foto kebun yang telah tersedia; ilustrasi produk BBM tetap generatif, sedangkan produk lain memakai thumbnail netral jika tidak ada foto.
