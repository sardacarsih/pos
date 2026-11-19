# Preflight sebelum deploy produksi. HANYA MEMBACA: tidak menulis, me-restart, atau mengirim apa pun ke server.
# Pakai: .\tools\preflight-produksi.ps1 [-Uji] [-LewatiServer]
#   -Uji           jalankan juga cargo test + cargo build --release + bun run build (lambat; lokal saja)
#   -LewatiServer  hanya pemeriksaan lokal
# Keluaran akhir: SIAP bila tidak ada [GAGAL]; [PERHATIAN] perlu dibaca tetapi tidak menghalangi.
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$DirApi = 'D:/be-rustapi',
    [string]$DirWeb = 'D:/admin-web',
    [string]$Layanan = 'KopkarPosApi',
    [string]$ApiUrl = 'https://api.kopkarksk.com',
    [string]$WebUrl = 'https://admin.kopkarksk.com',
    [switch]$Uji,
    [switch]$LewatiServer
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$gagal = 0

function Catat([string]$Tingkat, [string]$Nama, [string]$Info) {
    if ($Tingkat -eq 'GAGAL') { $script:gagal++ }
    '[{0}] {1}: {2}' -f $Tingkat, $Nama, $Info
}

# ---------------------------------------------------------------- lokal
Set-Location $repo
$cabang = (git rev-parse --abbrev-ref HEAD).Trim()
$sha = (git rev-parse --short HEAD).Trim()
Catat 'INFO' 'Commit' "$cabang @ $sha"
$kotor = @(git status --porcelain --untracked-files=no)
if ($kotor.Count -gt 0) { Catat 'PERHATIAN' 'Working tree' "$($kotor.Count) berkas terlacak berubah dan belum di-commit (deploy memakai working tree, bukan commit)" }
else { Catat 'OK' 'Working tree' 'bersih (berkas terlacak)' }
$upstream = (git rev-parse --abbrev-ref '@{u}' 2>$null)
if ($LASTEXITCODE -eq 0 -and $upstream) {
    $belumPush = [int](git rev-list --count "$upstream..HEAD")
    if ($belumPush -gt 0) { Catat 'PERHATIAN' 'Push' "$belumPush commit belum di-push ke $upstream" } else { Catat 'OK' 'Push' "sinkron dengan $upstream" }
} else { Catat 'PERHATIAN' 'Push' 'tidak ada upstream' }

$exe = Join-Path $repo 'rust-api/target/release/pos-api.exe'
if ($Uji) {
    Push-Location (Join-Path $repo 'rust-api')
    cargo test; if ($LASTEXITCODE) { Catat 'GAGAL' 'cargo test' 'gagal' } else { Catat 'OK' 'cargo test' 'lulus' }
    cargo build --release; if ($LASTEXITCODE) { Catat 'GAGAL' 'cargo build --release' 'gagal' }
    Pop-Location
    Push-Location (Join-Path $repo 'web')
    bun install --frozen-lockfile; bun run build
    if ($LASTEXITCODE) { Catat 'GAGAL' 'bun run build' 'gagal' } else { Catat 'OK' 'bun run build' 'berhasil' }
    Pop-Location
}
if (Test-Path $exe) {
    $h = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()
    Catat 'INFO' 'Biner lokal' "pos-api.exe sha256 $h, dibuat $((Get-Item $exe).LastWriteTime)"
    $waktuCommit = [DateTime]::Parse((git log -1 --format=%cI).Trim())
    if ((Get-Item $exe).LastWriteTime -lt $waktuCommit) { Catat 'PERHATIAN' 'Biner lokal usang' 'lebih lama dari commit terakhir; deploy-api.ps1 membangun ulang, tetapi hash di atas BUKAN yang akan dikirim' }
} else { Catat 'PERHATIAN' 'Biner lokal' 'belum ada target/release/pos-api.exe (deploy-api.ps1 akan membangunnya)' }
$indexWeb = Join-Path $repo 'web/build/index.html'
if (Test-Path $indexWeb) { Catat 'INFO' 'Build web lokal' "web/build ada, dibuat $((Get-Item $indexWeb).LastWriteTime)" }
else { Catat 'PERHATIAN' 'Build web lokal' 'belum ada web/build (deploy-web.ps1 akan membangunnya)' }

# ---------------------------------------------------------------- server (satu sesi SSH, hanya membaca)
if (-not $LewatiServer) {
    $skrip = @"
`$ErrorActionPreference='SilentlyContinue'
'host=' + `$env:COMPUTERNAME
`$s = Get-Service '$Layanan'; 'layanan=' + `$(if (`$s) { `$s.Status } else { 'TIDAK_ADA' })
`$e = Get-Item '$DirApi/pos-api-release.exe'
if (`$e) { 'exe_sha=' + (Get-FileHash `$e.FullName -Algorithm SHA256).Hash.ToLower(); 'exe_waktu=' + `$e.LastWriteTime } else { 'exe_sha=TIDAK_ADA' }
'prev_ada=' + (Test-Path '$DirApi/pos-api-release.prev.exe')
'env_ada=' + (Test-Path '$DirApi/.env')
'tmp_sisa=' + (Test-Path '$DirApi/pos-api-new.exe.tmp')
`$d = Get-PSDrive D; if (`$d) { 'disk_d_gb=' + [math]::Round(`$d.Free/1GB,1) }
if (Test-Path '$DirWeb') { 'web_file=' + (Get-ChildItem '$DirWeb' -Recurse -File | Measure-Object).Count; 'web_waktu=' + (Get-Item '$DirWeb/index.html').LastWriteTime } else { 'web_file=TIDAK_ADA' }
'web_lama_ada=' + (Test-Path '$DirWeb.lama')
'web_baru_sisa=' + (Test-Path '$DirWeb.baru')
`$n = Get-Service | Where-Object { `$_.Name -like '*nginx*' } | Select-Object -First 1; 'nginx=' + `$(if (`$n) { `$n.Status } else { 'TIDAK_ADA' })
try { 'health_lokal=' + (Invoke-WebRequest 'http://127.0.0.1:8080/health' -UseBasicParsing -TimeoutSec 5).StatusCode } catch { 'health_lokal=ERR' }
"@
    try {
        # Skrip dikirim sebagai satu baris base64 (stdin `powershell -Command -` membuang blok multi-baris tanpa baris kosong).
        $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($skrip))
        $baris = "Invoke-Expression ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('$b64')))" | ssh -o ConnectTimeout=15 -o BatchMode=yes $Server 'powershell -NoProfile -Command -'
        if ($LASTEXITCODE) { throw "ssh keluar dengan kode $LASTEXITCODE" }
        $d = @{}
        foreach ($b in $baris) { if ($b -match '^([a-z_]+)=(.*)$') { $d[$Matches[1]] = $Matches[2].Trim() } }
        Catat 'OK' 'SSH' "terhubung ke $($d['host'])"
        if ($d['layanan'] -eq 'Running') { Catat 'OK' "Layanan $Layanan" 'Running' } else { Catat 'GAGAL' "Layanan $Layanan" $d['layanan'] }
        if ($d['exe_sha'] -and $d['exe_sha'] -ne 'TIDAK_ADA') { Catat 'INFO' 'Biner terpasang (CATAT untuk rollback manual)' "sha256 $($d['exe_sha']), $($d['exe_waktu'])" }
        else { Catat 'GAGAL' 'Biner terpasang' "$DirApi/pos-api-release.exe tidak ada" }
        if ($d['prev_ada'] -eq 'True') { Catat 'OK' 'Cadangan biner' 'pos-api-release.prev.exe ada (dibuat ulang saat deploy)' } else { Catat 'PERHATIAN' 'Cadangan biner' 'belum ada pos-api-release.prev.exe (deploy pertama akan membuatnya)' }
        if ($d['env_ada'] -eq 'True') { Catat 'OK' '.env API' 'ada (isi tidak dibaca)' } else { Catat 'GAGAL' '.env API' "tidak ada di $DirApi" }
        if ($d['tmp_sisa'] -eq 'True') { Catat 'PERHATIAN' 'Sisa unggahan' 'pos-api-new.exe.tmp tertinggal dari deploy yang terputus (akan ditimpa)' }
        if ($d['disk_d_gb']) { $gb = [double]($d['disk_d_gb'] -replace ',', '.'); if ($gb -lt 1) { Catat 'GAGAL' 'Ruang drive D' "$gb GB" } else { Catat 'OK' 'Ruang drive D' "$gb GB bebas" } }
        if ($d['web_file'] -eq 'TIDAK_ADA') { Catat 'PERHATIAN' 'Web terpasang' "$DirWeb belum ada (deploy pertama)" }
        else { Catat 'INFO' 'Web terpasang' "$($d['web_file']) file, index.html $($d['web_waktu']); folder .lama ada: $($d['web_lama_ada'])" }
        if ($d['web_baru_sisa'] -eq 'True') { Catat 'PERHATIAN' 'Sisa unggahan web' "$DirWeb.baru tertinggal (akan dibersihkan deploy-web.ps1)" }
        if ($d['nginx'] -eq 'Running') { Catat 'OK' 'nginx' 'Running' } else { Catat 'PERHATIAN' 'nginx' "$($d['nginx']) (periksa nama layanan bila web tak terlayani)" }
        if ($d['health_lokal'] -eq '200') { Catat 'OK' 'Health API (lokal server)' '200' } else { Catat 'GAGAL' 'Health API (lokal server)' $d['health_lokal'] }
    } catch {
        Catat 'GAGAL' 'SSH' "tidak dapat membaca server: $($_.Exception.Message). Periksa kunci SSH/jaringan, atau jalankan dengan -LewatiServer"
    }

    foreach ($u in @(@('Health publik API', "$ApiUrl/health"), @('Situs web admin', "$WebUrl/"))) {
        try { $r = Invoke-WebRequest $u[1] -UseBasicParsing -TimeoutSec 20; if ($r.StatusCode -eq 200) { Catat 'OK' $u[0] "$($u[1]) = 200" } else { Catat 'GAGAL' $u[0] "$($u[1]) = $($r.StatusCode)" } }
        catch { Catat 'GAGAL' $u[0] "$($u[1]) tidak terjangkau: $($_.Exception.Message)" }
    }
}

''
if ($gagal -eq 0) { 'HASIL: SIAP (tidak ada [GAGAL]). Baca semua [PERHATIAN] sebelum deploy.' } else { "HASIL: TIDAK SIAP ($gagal [GAGAL])" }
