# Dot-source: . "$PSScriptRoot\ssh-kirim.ps1"   (butuh variabel $Server di pemanggil)
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
        if ($first) { # file kosong: tetap buat
            "[IO.File]::WriteAllBytes('$Remote', [byte[]]@())" | ssh $Server 'powershell -NoProfile -Command -'
        }
    } finally { $stream.Dispose() }
}

# Jalankan skrip PowerShell MULTI-BARIS di server (butuh $Server di pemanggil), mengembalikan keluarannya.
# Catatan penting: `powershell -Command -` membaca stdin baris demi baris seperti konsol interaktif. Blok yang melintas
# beberapa baris ({ ... } berbaris-baris, for/if/try) baru dieksekusi setelah ada BARIS KOSONG; tanpa itu seluruh sisa
# skrip hilang diam-diam (tanpa galat dan tanpa keluaran). Maka skrip dikirim sebagai SATU baris base64 dan dijalankan
# dengan Invoke-Expression, yang mem-parse skrip utuh.
function Invoke-SshSkrip([string]$Skrip) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Skrip))
    "Invoke-Expression ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('$b64')))" | ssh $Server 'powershell -NoProfile -Command -'
}
