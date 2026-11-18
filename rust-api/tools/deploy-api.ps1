# Deploy rust-api ke server produksi: test, build, kirim ke .tmp, verifikasi sha256, tukar biner, health check, rollback otomatis.
# Pakai: .\rust-api\tools\deploy-api.ps1 [-LewatiTest] [-DryRun] [-LewatiSelftest]
# Sebelum menukar biner, biner baru menjalankan `pos-api selftest` di server (query BACA saja terhadap Oracle produksi,
# memakai .env yang sama); bila ada yang GAGAL, deploy dibatalkan dan layanan tidak disentuh.
# -DryRun: test + build + hash saja, TANPA koneksi SSH dan tanpa mengirim apa pun.
# Tidak menyentuh .env, kredensial FCM, atau skema DB (migrasi: `pos-api migrate` terpisah bila ada).
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/be-rustapi',
    [string]$Layanan = 'KopkarPosApi',
    [string]$ApiUrl = 'https://api.kopkarksk.com',
    [switch]$LewatiTest,
    [switch]$DryRun,
    [switch]$LewatiSelftest
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot)

if (-not $LewatiTest) { cargo test; if ($LASTEXITCODE) { throw 'cargo test gagal' } }
cargo build --release
if ($LASTEXITCODE) { throw 'build gagal' }

$exe = 'target/release/pos-api.exe'
$sha = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()

if ($DryRun) { "DryRun: biner $exe sha256 $sha; tidak ada yang dikirim ke $Server"; return }

# scp/sftp ditolak server; pakai pengirim stdin yang sama dengan rilis APK.
. "$PSScriptRoot\..\..\mobile\tools\ssh-kirim.ps1"
$tmp = "$Dir/pos-api-new.exe.tmp"
Send-File $exe $tmp

# Selftest: salin ke nama .exe (ekstensi .tmp tidak bisa dijalankan), jalankan dari folder .env, hapus salinannya.
# Keluaran penuh dicetak SEBELUM penanda agar galatnya terlihat; baris terakhir selalu penanda.
$blokSelftest = ''
if (-not $LewatiSelftest) {
    $blokSelftest = @"
`$st='$Dir/pos-api-selftest.exe'
Copy-Item '$tmp' `$st -Force
`$ErrorActionPreference='Continue'
`$out = (& `$st selftest 2>&1 | Out-String)
`$kode = `$LASTEXITCODE
`$ErrorActionPreference='Stop'
Remove-Item `$st -Force
`$out
if (`$kode -ne 0) { Remove-Item '$tmp' -Force; 'SELFTEST_GAGAL'; exit 0 }
"@
}

$remote = @"
`$ErrorActionPreference='Stop'
Set-Location '$Dir'
`$hash=(Get-FileHash '$tmp' -Algorithm SHA256).Hash.ToLower()
if (`$hash -ne '$sha') { Remove-Item '$tmp' -Force; "BEDA `$hash"; exit 0 }
`$cur=(Get-FileHash 'pos-api-release.exe' -Algorithm SHA256).Hash.ToLower()
if (`$cur -eq '$sha') { Remove-Item '$tmp' -Force; 'SAMA'; exit 0 }
$blokSelftest
Copy-Item 'pos-api-release.exe' 'pos-api-release.prev.exe' -Force
Stop-Service '$Layanan'
Move-Item '$tmp' 'pos-api-release.exe' -Force
Start-Service '$Layanan'
Start-Sleep 5
`$ok=`$false
for (`$i=0; `$i -lt 12 -and -not `$ok; `$i++) {
  try { `$ok = ((Invoke-WebRequest 'http://127.0.0.1:8080/health' -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) } catch { Start-Sleep 2 }
}
if (-not `$ok) {
  Stop-Service '$Layanan' -Force
  Copy-Item 'pos-api-release.prev.exe' 'pos-api-release.exe' -Force
  Start-Service '$Layanan'
  'ROLLBACK'; exit 0
}
'OK'
"@
$keluaran = @(Invoke-SshSkrip $remote)
$hasil = $keluaran | Select-Object -Last 1
$keluaran | Where-Object { $_ -match '^(\[OK\]|\[GAGAL\]|RINGKAS)' } | ForEach-Object { "  selftest: $_" }
switch -Regex ($hasil) {
    '^OK'       { "Deploy OK, sha256 $sha, layanan $Layanan berjalan" }
    '^SAMA'     { 'Server sudah menjalankan biner yang sama; tidak ada yang diubah' }
    '^BEDA'     { throw "Hash di server berbeda ($hasil) dari lokal ($sha); layanan tidak disentuh" }
    '^SELFTEST_GAGAL' { throw "pos-api selftest GAGAL di server; deploy dibatalkan, layanan tidak disentuh:`n$($keluaran -join "`n")" }
    '^ROLLBACK' { throw 'Health check gagal; biner lama dipulihkan otomatis' }
    default     { throw "Hasil tak dikenal dari server: $hasil" }
}
(Invoke-WebRequest "$ApiUrl/health" -UseBasicParsing).StatusCode | ForEach-Object { "Health publik: $_" }
