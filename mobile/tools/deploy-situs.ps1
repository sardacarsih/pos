# Kirim halaman unduh (mobile/site/*) ke server: D:\update-site, disajikan nginx sebagai update.kopkarksk.com.
param(
    [string]$Server = 'ssh.kopkarksk.com',
    [string]$Dir = 'D:/update-site'
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\ssh-kirim.ps1"
$site = Join-Path (Split-Path $PSScriptRoot) 'site'

"New-Item -ItemType Directory -Force '$Dir' | Out-Null" | ssh $Server 'powershell -NoProfile -Command -'
foreach ($f in Get-ChildItem $site -File) { Send-File $f.FullName "$Dir/$($f.Name)"; "terkirim $($f.Name)" }
