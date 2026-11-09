# Build APK produksi lalu unggah ke server + tulis version.json (dibaca aplikasi untuk notifikasi update).
# Sebelum menjalankan: naikkan angka setelah "+" pada `version:` di pubspec.yaml (versionCode harus naik tiap rilis).
# Pakai: .\tools\rilis-apk.ps1 -Catatan "Perbaikan struk" [-MinVersionCode 3]   (MinVersionCode = versi di bawahnya wajib update)
param(
    [string]$Catatan = '',
    [int]$MinVersionCode = 0,
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/be-rustapi/downloads',
    [string]$ApiUrl = 'https://api.kopkarksk.com'
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot)

if ((Get-Content pubspec.yaml -Raw) -notmatch '(?m)^version:\s*(\S+)\+(\d+)') { throw 'version: tidak ditemukan di pubspec.yaml' }
$name = $Matches[1]; $code = [int]$Matches[2]

flutter build apk --release --dart-define=API_URL=$ApiUrl
if ($LASTEXITCODE) { throw 'build gagal' }

$apk = 'build/app/outputs/flutter-apk/app-release.apk'
$sha = (Get-FileHash $apk -Algorithm SHA256).Hash.ToLower()
$nama = "pos-kasir-$code.apk"   # nama berversi: unduhan berjalan tak rusak saat rilis baru, rollback tinggal ganti version.json
$json = [ordered]@{ versionCode = $code; versionName = $name; apk = $nama; sha256 = $sha; minVersionCode = $MinVersionCode; catatan = $Catatan } | ConvertTo-Json
$tmpJson = Join-Path $env:TEMP 'version.json'
[IO.File]::WriteAllText($tmpJson, $json, (New-Object Text.UTF8Encoding $false))

# Unggah ke nama sementara; APK dipindah lebih dulu, version.json terakhir (aplikasi tidak melihat versi baru sebelum APK siap).
. "$PSScriptRoot\ssh-kirim.ps1"
try {
    $lama = (Invoke-RestMethod "$ApiUrl/downloads/version.json").versionCode
    if ($code -le $lama) { throw "versionCode $code harus lebih besar dari yang di server ($lama); naikkan angka setelah '+' di pubspec.yaml" }
} catch [System.Net.WebException] { Write-Warning 'version.json lama tidak terbaca; validasi versionCode dilewati' }

Send-File $apk "$Dir/$nama.tmp"
Send-File $tmpJson "$Dir/version.json.tmp"
# Simpan 2 APK terbaru (rollback); /downloads/pos-kasir.apk lama tetap ada untuk aplikasi lama & halaman unduh nginx.
$remote = "Set-Location '$Dir'; Move-Item -Force '$nama.tmp' '$nama'; Copy-Item -Force '$nama' pos-kasir.apk; Move-Item -Force version.json.tmp version.json; Get-ChildItem 'pos-kasir-*.apk' | Sort-Object LastWriteTime -Descending | Select-Object -Skip 2 | Remove-Item; (Get-FileHash '$nama' -Algorithm SHA256).Hash.ToLower()"
$hashServer = ($remote | ssh $Server 'powershell -NoProfile -Command -').Trim()
if ($hashServer -ne $sha) { throw "Hash di server ($hashServer) berbeda dari lokal ($sha)" }

$ukuran = (Invoke-WebRequest "$ApiUrl/downloads/$nama" -Method Head).Headers.'Content-Length'
"OK v$name+$code, $ukuran byte, sha256 $sha"
"Unduh: $ApiUrl/downloads/$nama (tautan tetap: $ApiUrl/downloads/pos-kasir.apk)"
