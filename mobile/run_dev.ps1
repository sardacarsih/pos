# Dev di HP asli lewat USB: teruskan port 8080 HP -> PC, lalu jalankan app dengan API_URL dev.
# Pakai: ./run_dev.ps1 [-d <device-id>] [argumen flutter run lain]
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
adb reverse tcp:8080 tcp:8080
if ($LASTEXITCODE -ne 0) { throw 'adb reverse gagal: cek kabel USB, USB debugging, dan `adb devices`.' }
flutter run --dart-define-from-file=env/dev.json @args
