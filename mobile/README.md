# kopkar_pos

## Dev di HP asli (USB)

App memakai `API_URL` dari `--dart-define`; tanpa itu ia memanggil `https://api.example.com` dan selalu gagal terhubung.
Untuk dev, `adb reverse` meneruskan port 8080 HP ke PC, jadi `http://127.0.0.1:8080` di HP = API di PC (tanpa IP/firewall).

1. Colok HP via USB, aktifkan USB debugging (`adb devices` harus menampilkan HP).
2. Jalankan API: `cd ../rust-api && cargo run`.
3. Jalankan app: `./run_dev.ps1 -d <device-id>` (atau F5 konfigurasi "Dev (HP via USB)" di VS Code).

Ulangi langkah 3 setelah kabel dicabut/dicolok (reverse hilang saat koneksi terputus).

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.
