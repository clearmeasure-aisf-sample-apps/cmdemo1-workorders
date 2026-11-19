#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Protect-PrAcceptanceArtifactBundle.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function New-TarGzWithSymlink {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string]$FileText,
        [Parameter(Mandatory = $true)][string]$LinkName,
        [Parameter(Mandatory = $true)][string]$LinkTarget
    )

    $fileStream = [System.IO.File]::Create($Path)
    $gzip = [System.IO.Compression.GZipStream]::new(
        $fileStream, [System.IO.Compression.CompressionMode]::Compress)
    $writer = [System.Formats.Tar.TarWriter]::new($gzip)
    try {
        $ok = [System.Formats.Tar.PaxTarEntry]::new(
            [System.Formats.Tar.TarEntryType]::RegularFile, $FileName)
        $okBytes = [System.Text.Encoding]::UTF8.GetBytes($FileText)
        $ok.DataStream = [System.IO.MemoryStream]::new($okBytes)
        $writer.WriteEntry($ok)

        $link = [System.Formats.Tar.PaxTarEntry]::new(
            [System.Formats.Tar.TarEntryType]::SymbolicLink, $LinkName)
        $link.LinkName = $LinkTarget
        $writer.WriteEntry($link)
    }
    finally {
        $writer.Dispose()
    }
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

    $cipherBytes = [System.IO.File]::ReadAllBytes((Join-Path $bundle 'payload.tar.gz.enc'))
    $cipherText = [System.Text.Encoding]::UTF8.GetString($cipherBytes)
    Assert-True ($cipherText -notmatch 'acceptance-artifact-secret-payload') 'ciphertext must not contain plaintext'

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

    # Symlink entries must be rejected on Unprotect (trusted Upload hardening).
    $evilPlainTarGz = Join-Path $temp 'evil-plain.tar.gz'
    New-TarGzWithSymlink -Path $evilPlainTarGz `
        -FileName 'ok.txt' -FileText 'ok' `
        -LinkName 'evil-link' -LinkTarget 'ok.txt'

    $evilBundle = Join-Path $temp 'evil-bundle'
    New-Item -ItemType Directory -Force -Path $evilBundle | Out-Null
    $evilSession = Join-Path $evilBundle 'session.key'
    $keyBytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
    [System.IO.File]::WriteAllBytes($evilSession, $keyBytes)
    & openssl enc -aes-256-cbc -pbkdf2 -salt -pass file:$evilSession `
        -in $evilPlainTarGz -out (Join-Path $evilBundle 'payload.tar.gz.enc')
    Assert-True ($LASTEXITCODE -eq 0) 'encrypt evil tar'
    & openssl pkeyutl -encrypt -pubin -inkey $publicKey -pkeyopt rsa_padding_mode:oaep `
        -in $evilSession -out (Join-Path $evilBundle 'session.key.enc')
    Assert-True ($LASTEXITCODE -eq 0) 'wrap evil session key'
    Remove-Item -LiteralPath $evilSession -Force

    $evilOut = Join-Path $temp 'evil-out'
    $null = & pwsh -NoProfile -File $scriptPath -Mode Unprotect `
        -PrivateKeyPath $privateKey `
        -BundleDirectory $evilBundle `
        -OutputDirectory $evilOut 2>&1
    Assert-True ($LASTEXITCODE -ne 0) 'Unprotect must refuse symlink entries'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $evilOut 'evil-link'))) 'symlink not extracted'

    # Regression: the session key must be newline-free text so OpenSSL's `file:`
    # password read always sees the full key (binary keys with 0x0A used to
    # truncate the password, ~12% of random keys). Repeat the round trip to
    # catch the intermittent failure.
    $loopPayload = 'acceptance-artifact-secret-payload'
    for ($i = 1; $i -le 25; $i++) {
        $loopSource = Join-Path $temp "source-$i"
        $loopBundle = Join-Path $temp "bundle-$i"
        $loopOut = Join-Path $temp "out-$i"
        New-Item -ItemType Directory -Force -Path $loopSource | Out-Null
        Set-Content -LiteralPath (Join-Path $loopSource 'hello.txt') -Value $loopPayload

        & pwsh -NoProfile -File $scriptPath -Mode Protect `
            -PublicKeyPath $publicKey `
            -SourceDirectory $loopSource `
            -BundleDirectory $loopBundle
        Assert-True ($LASTEXITCODE -eq 0) "Protect should succeed (iteration $i)"

        & pwsh -NoProfile -File $scriptPath -Mode Unprotect `
            -PrivateKeyPath $privateKey `
            -BundleDirectory $loopBundle `
            -OutputDirectory $loopOut
        Assert-True ($LASTEXITCODE -eq 0) "Unprotect should succeed (iteration $i)"

        $recovered = Get-Content -LiteralPath (Join-Path $loopOut 'hello.txt') -Raw
        Assert-True ($recovered.Trim() -eq $loopPayload) "payload round-trip (iteration $i)"
    }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Protect-PrAcceptanceArtifactBundle.Tests.ps1 passed'
