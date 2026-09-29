# MuroSOC - sign.ps1
param([Parameter(Mandatory = $true)][string]$Path)
$ErrorActionPreference = 'Stop'
if (-not $env:MUROSOC_PFX -or -not (Test-Path $env:MUROSOC_PFX)) { throw 'No hay certificado de firma disponible.' }
$flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($env:MUROSOC_PFX, $env:CODESIGN_PFX_PASSWORD, $flags)
if (-not $cert.HasPrivateKey) { throw 'El certificado no tiene clave privada.' }
$timestamp = if ($env:CODESIGN_TIMESTAMP_URL) { $env:CODESIGN_TIMESTAMP_URL } else { 'http://timestamp.digicert.com' }
$result = Set-AuthenticodeSignature -FilePath $Path -Certificate $cert -HashAlgorithm SHA256 -TimestampServer $timestamp
if ($result.Status -ne 'Valid') { throw "Firma fallida: $($result.Status) $($result.StatusMessage)" }
"Firmado: $Path ($($cert.Subject))"
