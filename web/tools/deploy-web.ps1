# Deploy web SvelteKit ke produksi: https://admin.kopkarksk.com (folder D:/admin-web, disajikan nginx; proxy /api ke Rust API).
# Pakai (dari mana saja):
#   .\web\tools\deploy-web.ps1 -DryRun        # build + uji + manifest, TANPA koneksi SSH
#   .\web\tools\deploy-web.ps1                # deploy nyata
#   .\web\tools\deploy-web.ps1 -Rollback      # kembalikan versi sebelumnya (D:/admin-web.lama)
# Urutan rilis: deploy API (rust-api\tools\deploy-api.ps1) DULU, baru web (web baru memanggil endpoint baru).
# Tidak menyentuh nginx, .env, atau API. Folder lama disimpan sebagai D:/admin-web.lama untuk rollback.
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/admin-web',
    [string]$Url = 'https://admin.kopkarksk.com',
    [switch]$LewatiTest,
    [switch]$DryRun,
    [switch]$Rollback
)
$ErrorActionPreference = 'Stop'
$web = Split-Path $PSScriptRoot
. "$PSScriptRoot\..\..\mobile\tools\ssh-kirim.ps1"   # Send-File (scp/sftp ditolak server; kirim lewat stdin ssh)

$baru = "$Dir.baru"
$lama = "$Dir.lama"
$leaf = Split-Path $Dir -Leaf

# Skrip remote dikirim lewat Invoke-SshSkrip (base64 + Invoke-Expression; lihat ssh-kirim.ps1 untuk alasannya) dan
# dibungkus Remote-Aman (satu blok try {...} catch {...}) sehingga galat menghasilkan 'GAGAL: ...' alih-alih 'OK' palsu.
function Remote-Aman([string]$Isi) {
    return "try {`n`$ErrorActionPreference='Stop'`n$Isi`n} catch { 'GAGAL: ' + `$_.Exception.Message }"
}
function Invoke-Remote([string]$Skrip) {
    $out = Invoke-SshSkrip $Skrip
    if ($LASTEXITCODE) { throw "perintah SSH gagal: $out" }
    return ($out | Select-Object -Last 1)
}

# Smoke publik: halaman utama valid, aset JS yang dirujuk terlayani, index.html tidak di-cache.
function Test-Situs {
    $r = Invoke-WebRequest "$Url/" -UseBasicParsing -TimeoutSec 20
    if ($r.StatusCode -ne 200 -or $r.Content -notmatch '_app/') { throw "index.html di $Url tidak valid (status $($r.StatusCode))" }
    $js = [regex]::Match($r.Content, '/_app/immutable/[^"''\s]+\.js').Value
    if (-not $js) { throw 'index.html tidak merujuk aset JS' }
    $a = Invoke-WebRequest "$Url$js" -UseBasicParsing -TimeoutSec 20
    if ($a.StatusCode -ne 200) { throw "aset $js status $($a.StatusCode)" }
    if ("$($r.Headers['Cache-Control'])" -notmatch 'no-cache') { Write-Warning "Cache-Control index.html bukan no-cache: '$($r.Headers['Cache-Control'])'" }
    "Smoke OK: $Url/ dan $js"
}

# Tukar folder dengan percobaan ulang (nginx bisa sedang membuka file); gagal di tengah = kembalikan folder lama.
function Tukar-Folder([string]$Dari, [string]$Ke, [string]$Cadangan) {
    $skrip = Remote-Aman @"
function Ulang(`$blok) { for (`$i=0; `$i -lt 5; `$i++) { try { & `$blok; return } catch { if (`$i -eq 4) { throw }; Start-Sleep 2 } } }
if (-not (Test-Path '$Dari')) { 'TIDAK_ADA'; exit 0 }
if (Test-Path '$Cadangan') { Ulang { Remove-Item -Recurse -Force '$Cadangan' } }
`$ada = Test-Path '$Ke'
if (`$ada) { Ulang { Rename-Item '$Ke' '$(Split-Path $Cadangan -Leaf)' } }
try { Ulang { Rename-Item '$Dari' '$(Split-Path $Ke -Leaf)' } } catch { if (`$ada) { Rename-Item '$Cadangan' '$(Split-Path $Ke -Leaf)' }; 'GAGAL_TUKAR'; exit 0 }
'OK'
"@
    return Invoke-Remote $skrip
}

if ($Rollback) {
    "Rollback: $lama -> $Dir (versi sekarang disimpan sebagai $baru)"
    $hasil = Tukar-Folder $lama $Dir $baru
    switch -Regex ($hasil) {
        '^OK'          { Test-Situs; 'Rollback selesai'; return }
        '^TIDAK_ADA'   { throw "Tidak ada $lama di server; tidak ada yang bisa dikembalikan" }
        default        { throw "Rollback gagal ($hasil); periksa folder $Dir / $lama / $baru di server" }
    }
}

# ---------------------------------------------------------------- build + uji lokal
Set-Location $web
bun install --frozen-lockfile
if ($LASTEXITCODE) { throw 'bun install gagal' }
if (-not $LewatiTest) {
    bun run check; if ($LASTEXITCODE) { throw 'bun run check gagal' }
    bun run test;  if ($LASTEXITCODE) { throw 'bun run test gagal' }
}
bun run build
if ($LASTEXITCODE) { throw 'bun run build gagal' }

$root = (Resolve-Path 'build').Path
if (-not (Test-Path (Join-Path $root 'index.html'))) { throw 'build/index.html tidak ada' }
$files = @(Get-ChildItem $root -Recurse -File)
$manifest = foreach ($f in $files) {
    $rel = $f.FullName.Substring($root.Length).TrimStart('\').Replace('\', '/')
    '{0}|{1}' -f (Get-FileHash $f.FullName -Algorithm SHA256).Hash.ToLower(), $rel
}
$ukuran = [math]::Round((($files | Measure-Object Length -Sum).Sum) / 1MB, 2)
"Build siap: $($files.Count) file, $ukuran MB, tujuan ${Server}:$Dir ($Url)"

if ($DryRun) {
    'DryRun: tidak ada koneksi SSH dan tidak ada yang dikirim.'
    return
}

# ---------------------------------------------------------------- kirim ke folder baru + verifikasi
if ((Invoke-Remote "'ok'") -ne 'ok') { throw 'SSH tidak merespons' }
Invoke-Remote "Remove-Item -Recurse -Force '$baru' -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force '$baru' | Out-Null; 'ok'" | Out-Null
$dirs = $files | ForEach-Object { Split-Path $_.FullName -Parent } | Sort-Object -Unique
foreach ($d in $dirs) {
    $rel = $d.Substring($root.Length).TrimStart('\').Replace('\', '/')
    if ($rel) { Invoke-Remote "New-Item -ItemType Directory -Force '$baru/$rel' | Out-Null; 'ok'" | Out-Null }
}
$i = 0
foreach ($f in $files) {
    $i++
    $rel = $f.FullName.Substring($root.Length).TrimStart('\').Replace('\', '/')
    Send-File $f.FullName "$baru/$rel"
    if ($i % 25 -eq 0) { "terkirim $i/$($files.Count)" }
}

# Bandingkan hash semua file di server SEBELUM menyentuh situs hidup.
$daftar = ($manifest | ForEach-Object { "'$_'" }) -join ','
$verif = Remote-Aman @"
`$salah = @()
foreach (`$baris in @($daftar)) {
  `$p = `$baris.Split('|'); `$path = '$baru/' + `$p[1]
  if (-not (Test-Path `$path)) { `$salah += "HILANG `$(`$p[1])"; continue }
  if ((Get-FileHash `$path -Algorithm SHA256).Hash.ToLower() -ne `$p[0]) { `$salah += "BEDA `$(`$p[1])" }
}
`$jumlah = (Get-ChildItem '$baru' -Recurse -File | Measure-Object).Count
if (`$salah.Count -gt 0) { 'GAGAL: ' + ((`$salah | Select-Object -First 5) -join '; '); exit 0 }
if (`$jumlah -ne $($files.Count)) { "GAGAL: jumlah file `$jumlah (seharusnya $($files.Count))"; exit 0 }
'OK'
"@
$hasil = Invoke-Remote $verif
if ($hasil -notmatch '^OK') { throw "Verifikasi hash di server gagal ($hasil); situs hidup tidak disentuh" }
'Verifikasi hash OK: semua file identik dengan build lokal.'

# Pertahankan chunk lama (tanpa menimpa) agar tab yang masih membuka index.html lama tidak 404; buang yang > 30 hari.
$gabung = Remote-Aman @"
`$src = '$Dir/_app/immutable'; `$dst = '$baru/_app/immutable'
if (Test-Path `$src) {
  `$akar = (Resolve-Path `$src).Path
  Get-ChildItem `$src -Recurse -File | ForEach-Object {
    `$rel = `$_.FullName.Substring(`$akar.Length).TrimStart('\')
    `$to = Join-Path `$dst `$rel
    if (-not (Test-Path `$to)) { New-Item -ItemType Directory -Force (Split-Path `$to) | Out-Null; Copy-Item `$_.FullName `$to }
  }
  Get-ChildItem `$dst -Recurse -File | Where-Object { `$_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item -Force
}
'OK'
"@
if ((Invoke-Remote $gabung) -notmatch '^OK') { throw 'gagal menggabungkan chunk lama; situs hidup tidak disentuh' }

# ---------------------------------------------------------------- tukar + smoke (otomatis rollback bila smoke gagal)
$hasil = Tukar-Folder $baru $Dir $lama
if ($hasil -notmatch '^OK') { throw "Tukar folder gagal ($hasil); situs hidup tidak berubah atau sudah dikembalikan" }
try {
    Test-Situs
} catch {
    Write-Warning "Smoke gagal: $($_.Exception.Message). Mengembalikan versi sebelumnya..."
    $r = Tukar-Folder $lama $Dir $baru
    throw "Deploy web dibatalkan (rollback: $r). Penyebab: $($_.Exception.Message)"
}
"Deploy web OK: $($files.Count) file di $Url (versi sebelumnya: $lama; rollback: .\web\tools\deploy-web.ps1 -Rollback)"
