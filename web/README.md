# Kopkar POS Web

Aplikasi SvelteKit untuk kasir, anggota, admin, dan petugas pembelian. POS Web memakai API Rust yang juga dipakai aplikasi Flutter. Mode offline tersedia untuk input dan antrean pembelian.

## Pengembangan

Pasang Bun, jalankan API Rust sesuai `../rust-api/README.md`, lalu:

```powershell
bun install --frozen-lockfile
bun run dev
```

Buka alamat yang dicetak Vite. Permintaan `/api` diteruskan ke `http://127.0.0.1:8080`; ubah target lokal dengan environment `API_PROXY_TARGET` jika diperlukan. Jalankan `bun run check`, `bun run test`, dan `bun run build` untuk verifikasi. Hasil build statis ada di `build/`.

## Pembelian

Pembelian tersedia di POS Web untuk Admin dan Petugas pembelian. Admin dapat membuat akun petugas dari **Pengguna**, serta mengelola pembayaran dan koreksi di **Hutang Supplier**. Saldo dimulai dari faktur web dan riwayat BackOffice diberi status **Saldo belum dilacak**.

Pembelian memakai supplier aktif pada master BackOffice. Sinkronkan penerimaan sebelum menjalankan opname untuk barang yang sama. Jika faktur salah, balik pembayaran yang sudah tercatat, batalkan faktur dengan alasan, lalu buat faktur pengganti selama periode penerimaan masih terbuka.

Siapkan perangkat dengan login online dan membuka **Pembelian** hingga katalog tersedia. Gunakan scanner USB/Bluetooth mode keyboard dengan akhiran Enter, atau pilih **Scan dengan kamera** dan izinkan akses kamera. Kamera memerlukan HTTPS atau localhost. Barcode barang dan kemasan dicocokkan dengan katalog lokal, termasuk saat offline. Isi barang, tekan Enter untuk berpindah antarkolom, dan tekan Ctrl+Enter untuk meninjau faktur. Saat koneksi pulih, biarkan POS Web terbuka atau pilih **Coba lagi** di tab **Antrean**. Faktur berstatus **Tersinkron** telah diterima server.

Jika browser ditutup saat offline, pemulihan memerlukan sesi yang masih tersimpan dan terakhir diverifikasi dalam delapan jam. Gunakan pilihan **Ingat saya** hanya pada perangkat tepercaya. Antrean berada di profil browser dan perangkat yang dipakai untuk input. Periksa antrean sebelum menghapus data situs atau berpindah perangkat.

Untuk smoke test browser dengan API tiruan, jalankan `bun run build` dan sajikan hasilnya dengan `python -m http.server 4187 --bind 127.0.0.1 --directory build`. Di terminal lain, jalankan `python tools/test-pembelian-browser.py` dengan Python Playwright dan Chromium. Verifikasi service worker dan scanner fisik juga di staging melalui HTTPS.

## Deploy produksi

`.\web\tools\deploy-web.ps1` (dari root repo; `-DryRun` tanpa SSH, `-Rollback` untuk kembali) mengirim `build/` ke `admin.kopkarksk.com` (`D:/admin-web`) dengan verifikasi hash dan rollback otomatis. Deploy API dulu, web sesudahnya. Lihat [`docs/deploy-produksi.md`](../docs/deploy-produksi.md).

## Penyajian

Sajikan `build/` melalui HTTPS dan proxy `/api/` ke API Rust pada origin yang sama. Contoh lokasi nginx (sesuaikan path dan TLS dengan lingkungan):

```nginx
location /api/ { proxy_pass http://127.0.0.1:8080; }
location / { root D:/kopkar-pos-web/build; try_files $uri $uri/ /index.html; }
```

Kamera QR memerlukan HTTPS atau localhost dan izin kamera. Login memerlukan akun mobile Rust, bukan akun desktop. Token disimpan di penyimpanan browser; gunakan perangkat tepercaya dan logout setelah selesai. Draft checkout tersimpan per akun di browser untuk pemulihan; jangan hapus data situs sampai checkout diselesaikan. Server tetap menentukan harga, potongan, limit, periode, dan mode demo belanja anggota.
