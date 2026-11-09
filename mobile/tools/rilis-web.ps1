# Build PWA (untuk iPhone) lalu unggah ke server: disajikan nginx di https://ios.kopkarksk.com (folder D:/ios-web).
# Pakai: .\tools\rilis-web.ps1   (tanpa restart service; pengguna mendapat versi baru saat membuka ulang aplikasi)
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/ios-web',
    [string]$ApiUrl = 'https://ios.kopkarksk.com'
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot)

flutter build web --release --base-href / --dart-define=API_URL=$ApiUrl
if ($LASTEXITCODE) { throw 'build gagal' }

. "$PSScriptRoot\ssh-kirim.ps1"
$root = (Resolve-Path build/web).Path
# Folder baru dulu, lalu ditukar: pengguna tidak pernah melihat setengah file lama setengah baru.
$baru = "$Dir.baru"
"Remove-Item -Recurse -Force '$baru' -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force '$baru' | Out-Null" | ssh $Server 'powershell -NoProfile -Command -'
$files = Get-ChildItem $root -Recurse -File
$dirs = $files | ForEach-Object { Split-Path $_.FullName -Parent } | Sort-Object -Unique
foreach ($d in $dirs) {
    $rel = $d.Substring($root.Length).TrimStart('\').Replace('\', '/')
    if ($rel) { "New-Item -ItemType Directory -Force '$baru/$rel' | Out-Null" | ssh $Server 'powershell -NoProfile -Command -' }
}
foreach ($f in $files) {
    $rel = $f.FullName.Substring($root.Length).TrimStart('\').Replace('\', '/')
    Send-File $f.FullName "$baru/$rel"
}
"if (Test-Path '$Dir') { Remove-Item -Recurse -Force '$Dir' }; Rename-Item '$baru' (Split-Path '$Dir' -Leaf)" | ssh $Server 'powershell -NoProfile -Command -'
if ($LASTEXITCODE) { throw 'tukar folder gagal' }
"OK $($files.Count) file. Buka: $ApiUrl/"
