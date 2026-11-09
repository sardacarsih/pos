# Kopkar POS Web

Aplikasi SvelteKit untuk kasir, anggota, dan admin; memakai API Rust yang sama dengan aplikasi Flutter. Aplikasi ini terpisah dari web Flutter dan tidak menyediakan transaksi offline atau PWA.

## Pengembangan

Pasang Bun, jalankan API Rust sesuai `../rust-api/README.md`, lalu:

```powershell
bun install --frozen-lockfile
bun run dev
```

Buka alamat yang dicetak Vite. Permintaan `/api` diteruskan ke `http://127.0.0.1:8080`; ubah target lokal dengan environment `API_PROXY_TARGET` jika diperlukan. Jalankan `bun run check`, `bun run test`, dan `bun run build` untuk verifikasi. Hasil build statis ada di `build/`.

## Penyajian

Sajikan `build/` melalui HTTPS dan proxy `/api/` ke API Rust pada origin yang sama. Contoh lokasi nginx (sesuaikan path dan TLS dengan lingkungan):

```nginx
location /api/ { proxy_pass http://127.0.0.1:8080; }
location / { root D:/kopkar-pos-web/build; try_files $uri $uri/ /index.html; }
```

Kamera QR memerlukan HTTPS atau localhost dan izin kamera. Login memerlukan akun mobile Rust, bukan akun desktop. Token disimpan di penyimpanan browser; gunakan perangkat tepercaya dan logout setelah selesai. Draft checkout tersimpan per akun di browser untuk pemulihan; jangan hapus data situs sampai checkout diselesaikan. Server tetap menentukan harga, potongan, limit, periode, dan mode demo belanja anggota.
