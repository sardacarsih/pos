# Deploy produksi: rust-api dan web (ssh.kopkarksk.com, Windows)

Server produksi (Windows) dijangkau lewat SSH. `scp`/`sftp` ditolak, jadi semua pengiriman berjalan lewat stdin SSH
(`mobile/tools/ssh-kirim.ps1`). Dijalankan dari **PowerShell di mesin pengembang**, dari root repo.

| Komponen | Skrip | Tujuan di server | Penyaji |
|---|---|---|---|
| rust-api | `rust-api\tools\deploy-api.ps1` | `D:/be-rustapi/pos-api-release.exe`, layanan `KopkarPosApi` | `https://api.kopkarksk.com` |
| web SvelteKit | `web\tools\deploy-web.ps1` | `D:/admin-web` | nginx -> `https://admin.kopkarksk.com` (proxy `/api/` ke API) |
| cek sebelum deploy | `tools\preflight-produksi.ps1` | (hanya membaca) | - |

**Urutan wajib: API dulu, web sesudahnya.** Web baru memanggil endpoint yang hanya ada di API baru. API kompatibel mundur
(hanya menambah field/endpoint dan melonggarkan `GET /periode/status` untuk anggota/admin), jadi APK yang sudah beredar
tetap berjalan.

## 1. Sebelum deploy

```powershell
.\tools\preflight-produksi.ps1            # lokal + server (SSH hanya baca); tambah -Uji untuk cargo test + build
```

Baca setiap `[PERHATIAN]`, dan **catat `sha256` biner terpasang** yang dicetak preflight (untuk rollback manual).
Hasil harus `SIAP`. Latih skrip tanpa menyentuh server:

```powershell
.\rust-api\tools\deploy-api.ps1 -DryRun   # cargo test + build release + hash, tanpa SSH
.\web\tools\deploy-web.ps1 -DryRun        # bun install/check/test/build + manifest hash, tanpa SSH
```

Deploy memakai **working tree**, bukan commit: pastikan yang dideploy sudah di-commit dan di-push (preflight memperingatkan).

### Migrasi database
Tidak perlu `pos-api migrate` untuk rilis ini: `rust-api/src/db/schema.sql` tidak berubah sejak `3b0babd`, dan fitur baru
(stok habis, opname, harga <= HPP, rincian penjualan, lencana) memakai tabel `POS_*` dan `MOB_IDEMPOTENCY` yang sudah ada.
Jalankan `pos-api migrate` di server **hanya** bila rilis berikutnya mengubah `schema.sql`.

## 2. Deploy API

```powershell
.\rust-api\tools\deploy-api.ps1
```

Test, build release, kirim ke `.tmp`, cocokkan sha256, cadangkan ke `pos-api-release.prev.exe`, tukar biner, restart
layanan, health check `http://127.0.0.1:8080/health`, dan **rollback otomatis** bila health gagal. Layanan berhenti beberapa
detik: lakukan di luar jam transaksi puncak. `.env`, kredensial FCM, dan skema DB tidak disentuh.

### Smoke test API (read-only dulu)
SQL baru belum pernah diuji terhadap Oracle sungguhan, jadi mulai dari endpoint GET. Login sebagai ADMIN, lalu:

1. `GET https://api.kopkarksk.com/health` = 200.
2. `GET /api/v1/admin/dashboard` memuat `kemarin`, `tren` (7 hari), `perhatian`.
3. `GET /api/v1/admin/dashboard/stok-habis`, `GET /api/v1/admin/menu-badge`.
4. `GET /api/v1/admin/produk?harga_rendah=Y`.
5. `GET /api/v1/admin/penjualan/rincian?dari=YYYY-MM-DD&sampai=YYYY-MM-DD` (hari ini), lalu 7 hari.
6. `GET /api/v1/admin/stok-opname?tahun=2026`.
7. `GET /api/v1/periode/status` sebagai ADMIN, KASIR, dan ANGGOTA (semua 200).
8. Bila ada pesan galat `ORA-xxxxx` di respons/log, **rollback** (bagian 5) dan laporkan SQL-nya.

Endpoint **tulis** (`POST /admin/stok-opname`, `/dokumen`) membuat dokumen SO permanen di `POS_STOCKOPNAME` (bisa dihapus
dari desktop, tidak dari web/mobile). Uji hanya dengan produk uji dan catat nomor SO-nya.

## 3. Deploy web

```powershell
.\web\tools\deploy-web.ps1
```

Build + uji, kirim ke `D:/admin-web.baru`, **verifikasi hash semua file di server** sebelum menyentuh situs hidup, gabungkan
chunk lama (agar tab yang masih terbuka tidak 404; chunk > 30 hari dibuang), tukar folder (`admin-web` -> `admin-web.lama`),
lalu smoke publik. Bila smoke gagal, versi sebelumnya dikembalikan otomatis. nginx tidak perlu diubah atau di-reload.

### Smoke test web
Buka `https://admin.kopkarksk.com` (muat ulang keras): login ADMIN, KASIR, dan ANGGOTA; Beranda tampil per peran; sidebar,
topbar (chip periode, menu pengguna), lencana Stok habis/Harga <= HPP; menu Stok habis, Harga <= HPP, Stock opname
(lihat saja), rincian penjualan di Beranda admin.

## 4. Yang tidak ikut deploy ini
**Aplikasi mobile (APK)**: tab Stok habis/Opname/Harga <= HPP dan Beranda baru di APK butuh rilis terpisah
(`mobile\tools\rilis-apk.ps1`). API tetap melayani APK lama tanpa masalah.

## 5. Rollback
- **API**: otomatis bila health gagal. Manual (di server): 
  `Stop-Service KopkarPosApi; Copy-Item D:/be-rustapi/pos-api-release.prev.exe D:/be-rustapi/pos-api-release.exe -Force; Start-Service KopkarPosApi`,
  lalu cocokkan `Get-FileHash` dengan sha256 yang dicatat saat preflight. Tidak ada migrasi, jadi skema tidak terpengaruh;
  dokumen SO yang sudah dibuat tetap ada.
- **Web**: `.\web\tools\deploy-web.ps1 -Rollback` (menukar `admin-web.lama` kembali; versi yang baru dibatalkan disimpan
  sebagai `admin-web.baru` sampai deploy berikutnya).

## 6. Catatan teknis skrip
`powershell -Command -` membaca stdin baris demi baris seperti konsol: blok yang melintasi beberapa baris baru dijalankan
setelah ada baris kosong, dan **tanpa itu sisa skrip hilang diam-diam** (tanpa galat). Karena itu skrip remote dikirim sebagai
satu baris base64 dan dijalankan dengan `Invoke-Expression` (`Invoke-SshSkrip` di `mobile/tools/ssh-kirim.ps1`). Skrip
`deploy-api.ps1` yang lama mengirim loop health check dan rollback sebagai blok multi-baris, jadi bagian itu mungkin tidak
pernah berjalan di server yang memakai Windows PowerShell 5.1; kini ia memakai pengirim yang sama.
