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
$json = [ordered]@{ versionCode = $code; versionName = $name; apk = 'pos-kasir.apk'; sha256 = $sha; minVersionCode = $MinVersionCode; catatan = $Catatan } | ConvertTo-Json
$tmpJson = Join-Path $env:TEMP 'version.json'
[IO.File]::WriteAllText($tmpJson, $json, (New-Object Text.UTF8Encoding $false))

# Unggah ke nama sementara; APK dipindah lebih dulu, version.json terakhir (aplikasi tidak melihat versi baru sebelum APK siap).
# scp/sftp ke server ini ditolak ("Connection closed"), jadi file dikirim lewat stdin ssh sebagai base64 per potongan.
function Send-File($Local, $Remote) {
    $stream = [IO.File]::OpenRead((Resolve-Path $Local).Path)
    $buf = New-Object byte[] (3MB); $first = $true
    try {
        while (($n = $stream.Read($buf, 0, $buf.Length)) -gt 0) {
            $b64 = [Convert]::ToBase64String($buf, 0, $n)
            $mode = if ($first) { 'Create' } else { 'Append' }
            $script = "`$b=[Convert]::FromBase64String('$b64'); `$f=[IO.File]::Open('$Remote',[IO.FileMode]::$mode); `$f.Write(`$b,0,`$b.Length); `$f.Close()"
            $script | ssh $Server 'powershell -NoProfile -Command -'
            if ($LASTEXITCODE) { throw "kirim $Remote gagal" }
            $first = $false
        }
    } finally { $stream.Dispose() }
}
Send-File $apk "$Dir/pos-kasir.apk.tmp"
Send-File $tmpJson "$Dir/version.json.tmp"
$remote = "Set-Location '$Dir'; Move-Item -Force pos-kasir.apk.tmp pos-kasir.apk; Move-Item -Force version.json.tmp version.json; (Get-FileHash pos-kasir.apk -Algorithm SHA256).Hash.ToLower()"
$hashServer = ($remote | ssh $Server 'powershell -NoProfile -Command -').Trim()
if ($hashServer -ne $sha) { throw "Hash di server ($hashServer) berbeda dari lokal ($sha)" }

$ukuran = (Invoke-WebRequest "$ApiUrl/downloads/pos-kasir.apk" -Method Head).Headers.'Content-Length'
"OK v$name+$code, $ukuran byte, sha256 $sha"
"Unduh: $ApiUrl/downloads/pos-kasir.apk"
