#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Protect-PrAcceptanceArtifactBundle.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$env:AZURE_STORAGE_SAS_TOKEN = 'not-a-real-sas'
try {
    & pwsh -NoProfile -File $scriptPath -Mode Protect `
        -PublicKeyPath '/tmp/missing.pem' `
        -SourceDirectory '/tmp' `
        -BundleDirectory '/tmp/bundle-should-fail' 2>&1 | Out-Null
    Assert-True ($LASTEXITCODE -ne 0) 'Must refuse AZURE_STORAGE_SAS_TOKEN'
}
finally {
    Remove-Item Env:AZURE_STORAGE_SAS_TOKEN -ErrorAction SilentlyContinue
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('wrap-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $privateKey = Join-Path $temp 'private.pem'
    $publicKey = Join-Path $temp 'public.pem'
    & openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out $privateKey
    Assert-True ($LASTEXITCODE -eq 0) 'openssl genpkey'
    & openssl pkey -in $privateKey -pubout -out $publicKey
    Assert-True ($LASTEXITCODE -eq 0) 'openssl pkey'

    $source = Join-Path $temp 'source'
    $bundle = Join-Path $temp 'bundle'
    $out = Join-Path $temp 'out'
    New-Item -ItemType Directory -Force -Path $source | Out-Null
    Set-Content -LiteralPath (Join-Path $source 'hello.txt') -Value 'acceptance-artifact-secret-payload'

    & pwsh -NoProfile -File $scriptPath -Mode Protect `
        -PublicKeyPath $publicKey `
        -SourceDirectory $source `
        -BundleDirectory $bundle
    Assert-True ($LASTEXITCODE -eq 0) 'Protect should succeed'
    Assert-True (Test-Path -LiteralPath (Join-Path $bundle 'payload.tar.gz.enc')) 'ciphertext present'
    Assert-True (Test-Path -LiteralPath (Join-Path $bundle 'session.key.enc')) 'wrapped key present'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $bundle 'session.key'))) 'session key removed'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $bundle 'payload.tar.gz'))) 'plaintext tar removed'

    $plain = Get-Content -LiteralPath (Join-Path $bundle 'payload.tar.gz.enc') -Raw -ErrorAction SilentlyContinue
    Assert-True ($plain -notmatch 'acceptance-artifact-secret-payload') 'ciphertext must not contain plaintext'

    # Local unprotect without Azure: decrypt with the minted private key via openssl (unit path).
    $sessionKey = Join-Path $temp 'session.key'
    $tarPath = Join-Path $temp 'payload.tar.gz'
    & openssl pkeyutl -decrypt -inkey $privateKey -pkeyopt rsa_padding_mode:oaep `
        -in (Join-Path $bundle 'session.key.enc') -out $sessionKey
    Assert-True ($LASTEXITCODE -eq 0) 'unwrap session key'
    & openssl enc -d -aes-256-cbc -pbkdf2 -pass file:$sessionKey `
        -in (Join-Path $bundle 'payload.tar.gz.enc') -out $tarPath
    Assert-True ($LASTEXITCODE -eq 0) 'decrypt payload'
    New-Item -ItemType Directory -Force -Path $out | Out-Null
    & tar -xzf $tarPath -C $out
    Assert-True ($LASTEXITCODE -eq 0) 'extract'
    $recovered = Get-Content -LiteralPath (Join-Path $out 'hello.txt') -Raw
    Assert-True ($recovered.Trim() -eq 'acceptance-artifact-secret-payload') 'round-trip'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Protect-PrAcceptanceArtifactBundle.Tests.ps1 passed'
