# Deploy rust-api ke server produksi: test, build, kirim ke .tmp, verifikasi sha256, tukar biner, health check, rollback otomatis.
# Pakai: .\rust-api\tools\deploy-api.ps1 [-LewatiTest]
# Tidak menyentuh .env, kredensial FCM, atau skema DB (migrasi: `pos-api migrate` terpisah bila ada).
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/be-rustapi',
    [string]$Layanan = 'KopkarPosApi',
    [string]$ApiUrl = 'https://api.kopkarksk.com',
    [switch]$LewatiTest
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot)

if (-not $LewatiTest) { cargo test; if ($LASTEXITCODE) { throw 'cargo test gagal' } }
cargo build --release
if ($LASTEXITCODE) { throw 'build gagal' }

$exe = 'target/release/pos-api.exe'
$sha = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()

# scp/sftp ditolak server; pakai pengirim stdin yang sama dengan rilis APK.
. "$PSScriptRoot\..\..\mobile\tools\ssh-kirim.ps1"
$tmp = "$Dir/pos-api-new.exe.tmp"
Send-File $exe $tmp

$remote = @"
`$ErrorActionPreference='Stop'
Set-Location '$Dir'
`$hash=(Get-FileHash '$tmp' -Algorithm SHA256).Hash.ToLower()
if (`$hash -ne '$sha') { Remove-Item '$tmp' -Force; "BEDA `$hash"; exit 0 }
`$cur=(Get-FileHash 'pos-api-release.exe' -Algorithm SHA256).Hash.ToLower()
if (`$cur -eq '$sha') { Remove-Item '$tmp' -Force; 'SAMA'; exit 0 }
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
$hasil = ($remote | ssh $Server 'powershell -NoProfile -Command -') | Select-Object -Last 1
switch -Regex ($hasil) {
    '^OK'       { "Deploy OK, sha256 $sha, layanan $Layanan berjalan" }
    '^SAMA'     { 'Server sudah menjalankan biner yang sama; tidak ada yang diubah' }
    '^BEDA'     { throw "Hash di server berbeda ($hasil) dari lokal ($sha); layanan tidak disentuh" }
    '^ROLLBACK' { throw 'Health check gagal; biner lama dipulihkan otomatis' }
    default     { throw "Hasil tak dikenal dari server: $hasil" }
}
(Invoke-WebRequest "$ApiUrl/health" -UseBasicParsing).StatusCode | ForEach-Object { "Health publik: $_" }
