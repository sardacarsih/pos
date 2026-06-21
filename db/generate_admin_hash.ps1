<#
.SYNOPSIS
    Membuat hash password PBKDF2 (kompatibel dengan PasswordCryptographyPbkdf2)
    dan mencetak skrip SQL untuk membuat user admin awal.

.DESCRIPTION
    Format hash (self-describing): PBKDF2-SHA256$<iterations>$<saltB64>$<hashB64>
    PBKDF2/SHA256, 600000 iterasi, salt 16 byte, derived key 32 byte.
    HARUS sama dengan Shared/Auth/PasswordCryptographyPbkdf2.cs.

.EXAMPLE
    pwsh ./db/generate_admin_hash.ps1 -Username admin -Password "Rahasia123"
#>
param(
    [string]$Username = "admin",
    [Parameter(Mandatory = $true)][string]$Password,
    [string]$FullName = "Administrator"
)

$ErrorActionPreference = "Stop"

$saltSize   = 16
$hashSize   = 32
$iterations = 600000

# 1. salt acak
$salt = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes($saltSize)

# 2. PBKDF2/SHA256 (sama seperti Rfc2898DeriveBytes.Pbkdf2)
$hash = [System.Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2(
    $Password, $salt, $iterations,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256, $hashSize)

# 3. rakit string self-describing
$hashB64 = "PBKDF2-SHA256`$$iterations`$" +
           [Convert]::ToBase64String($salt) + "`$" +
           [Convert]::ToBase64String($hash)

$u = $Username.ToLower()

Write-Host "PASSWORD_HASH = $hashB64" -ForegroundColor Cyan
Write-Host ""
Write-Host "-- Jalankan di Oracle (setelah login_schema.sql):" -ForegroundColor Yellow
@"
INSERT INTO POS_USER (USERNAME, FULL_NAME, PASSWORD_HASH)
VALUES ('$u', '$FullName', '$hashB64');

-- Beri akses ADMIN ke semua aplikasi
INSERT INTO POS_USER_ACCESS (USER_ID, APP_ID, ROLE_ID)
SELECT u.USER_ID, a.APP_ID, r.ROLE_ID
FROM   POS_USER u
CROSS  JOIN POS_APP a
JOIN   POS_ROLE r ON r.ROLE_NAME = 'ADMIN'
WHERE  u.USERNAME = '$u';

COMMIT;
"@ | Write-Output
