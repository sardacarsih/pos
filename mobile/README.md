# kopkar_pos

## Dev di HP asli (USB)

App memakai `API_URL` dari `--dart-define`; tanpa itu ia memanggil `https://api.example.com` dan selalu gagal terhubung.
Untuk dev, `adb reverse` meneruskan port 8080 HP ke PC, jadi `http://127.0.0.1:8080` di HP = API di PC (tanpa IP/firewall).

1. Colok HP via USB, aktifkan USB debugging (`adb devices` harus menampilkan HP).
2. Jalankan API: `cd ../rust-api && cargo run`.
3. Jalankan app: `./run_dev.ps1 -d <device-id>` (atau F5 konfigurasi "Dev (HP via USB)" di VS Code).

Ulangi langkah 3 setelah kabel dicabut/dicolok (reverse hilang saat koneksi terputus).

## Build APK produksi

Build release memerlukan keystore produksi dan `android/key.properties` (keduanya tidak boleh di-commit). Isi `key.properties`:

```properties
storeFile=path/ke/keystore.jks
storePassword=PASSWORD_KEYSTORE
keyAlias=ALIAS_KUNCI
keyPassword=PASSWORD_KUNCI
```

`storeFile` adalah path absolut atau relatif terhadap direktori `android`. Simpan cadangan keystore dan password di tempat yang aman: pembaruan Android harus ditandatangani dengan kunci yang sama. APK lama yang ditandatangani dengan kunci debug tidak dapat diperbarui langsung memakai kunci produksi yang berbeda.

```powershell
flutter build apk --release --dart-define=API_URL=https://api.kopkarksk.com
```

APK hasil build berada di `build/app/outputs/flutter-apk/app-release.apk`. Periksa tanda tangan, versi, dan koneksi ke server produksi sebelum distribusi.

## Rilis & update otomatis

Aplikasi memeriksa `https://api.kopkarksk.com/downloads/version.json` saat dibuka/kembali ke depan dan menampilkan dialog "Versi X tersedia" (unduh, verifikasi SHA-256, lalu layar pasang Android). Pemasangan pertama perlu mengizinkan "Pasang aplikasi tidak dikenal"; update tidak bisa sepenuhnya senyap.

Tiap rilis: naikkan angka setelah `+` di `version:` pada `pubspec.yaml` (versionCode), lalu:

```powershell
.\tools\rilis-apk.ps1 -Catatan "Isi perubahan" [-MinVersionCode N]
```

Skrip membangun APK, mengunggahnya ke `D:\be-rustapi\downloads` di server lewat `ssh.kopkarksk.com`, menulis `version.json`, dan memverifikasi hash. `-MinVersionCode N` memaksa versi di bawah N untuk update (dialog tidak bisa ditutup).

## Halaman unduh (update.kopkarksk.com)

Halaman HTML/CSS di `site/` disajikan nginx (service `KopkarNginx`, `D:\nginx`, hanya `127.0.0.1:8081`) lewat Cloudflare Tunnel. Halaman membaca `version.json` dan APK dari folder `downloads` yang sama dengan API, jadi `rilis-apk.ps1` tidak perlu diubah.

- Ubah tampilan: edit `site/`, lalu `.\tools\deploy-situs.ps1`.
- Konfigurasi nginx dan service ada di `tools/nginx/` (salin ke `D:\nginx\conf\nginx.conf` dan `D:\nginx\nginx-service.xml`; `nginx-service.exe` adalah salinan WinSW dari `D:\be-rustapi\pos-api-service.exe`).
- Hostname publik diatur di Cloudflare Zero Trust > Tunnels > Public hostname: `update.kopkarksk.com` -> `HTTP` `localhost:8081`.

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.
