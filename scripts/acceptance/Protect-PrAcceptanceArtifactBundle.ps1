<#
.SYNOPSIS
  Hybrid-encrypts PR acceptance videos/traces for a public-repo artifact hand-off (#1671).

.DESCRIPTION
  Mint (trusted + OIDC): RSA wrap key; private PEM to private Azure blob; public PEM to disk.
  Protect (shard, no OIDC): tar videos/traces/trx selection inputs, AES-encrypt, RSA-wrap session key.
  Unprotect (trusted + OIDC): fetch private PEM, decrypt bundle to an output directory.

  Ciphertext may travel as a short-retention Actions artifact. Plain videos/traces must not.
  No account keys, SAS tokens, or connection strings. --auth-mode login only for Azure.

.PARAMETER Mode
  Mint | Protect | Unprotect

.PARAMETER StorageAccountName
  Required for Mint and Unprotect (private key blob).

.PARAMETER RunId
  Blob prefix for the wrap private key.

.PARAMETER PublicKeyPath / PrivateKeyPath / BundleDirectory / OutputDirectory
  Local paths for keys and ciphertext / plaintext.
#>
[CmdletBinding()]
param(
    [ValidateSet('Mint', 'Protect', 'Unprotect', 'FetchWrapKey', 'DeleteWrapKey')]
    [string]$Mode = 'Protect',

    [string]$StorageAccountName = $env:POOL_ARTIFACTS_STORAGE_ACCOUNT,
    [string]$RunId = $env:GITHUB_RUN_ID,
    [string]$PublicKeyPath = '',
    [string]$PrivateKeyPath = '',
    [string]$SourceDirectory = '',
    [string]$BundleDirectory = '',
    [string]$OutputDirectory = '',
    [string]$ContainerName = 'traces',
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

if ($SelfTest) {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Protect-PrAcceptanceArtifactBundle.Tests.ps1')
    exit $LASTEXITCODE
}

function Assert-NoSecretMaterial {
    $forbidden = @('AZURE_STORAGE_KEY', 'AZURE_STORAGE_SAS_TOKEN', 'ACCOUNT_KEY', 'SAS_TOKEN')
    foreach ($name in $forbidden) {
        if (-not [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) {
            throw "Blocker: environment defines $name. Issue #1671 forbids keys/SAS; use OIDC --auth-mode login only."
        }
    }
}

function Get-WrapBlobName {
    param([string]$Id)
    return "_wrap/$Id/private.pem"
}

function Invoke-AzBlobUploadFile {
    param([string]$Account, [string]$Container, [string]$BlobName, [string]$FilePath)
    Assert-NoSecretMaterial
    & az storage blob upload `
        --account-name $Account `
        --container-name $Container `
        --name $BlobName `
        --file $FilePath `
        --auth-mode login `
        --overwrite true `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) {
        throw "az storage blob upload failed for $Container/$BlobName (exit $LASTEXITCODE)."
    }
}

function Invoke-AzBlobDownloadFile {
    param([string]$Account, [string]$Container, [string]$BlobName, [string]$FilePath)
    Assert-NoSecretMaterial
    & az storage blob download `
        --account-name $Account `
        --container-name $Container `
        --name $BlobName `
        --file $FilePath `
        --auth-mode login `
        --overwrite true `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) {
        throw "az storage blob download failed for $Container/$BlobName (exit $LASTEXITCODE)."
    }
}

function Invoke-AzBlobDeleteFile {
    param([string]$Account, [string]$Container, [string]$BlobName)
    Assert-NoSecretMaterial
    & az storage blob delete `
        --account-name $Account `
        --container-name $Container `
        --name $BlobName `
        --auth-mode login `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "az storage blob delete failed for $Container/$BlobName (exit $LASTEXITCODE)."
    }
}

function Invoke-Mint {
    Assert-NoSecretMaterial
    if ([string]::IsNullOrWhiteSpace($StorageAccountName)) {
        throw 'Blocker: StorageAccountName / POOL_ARTIFACTS_STORAGE_ACCOUNT is empty for Mint.'
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) { throw 'RunId is required for Mint.' }
    if ([string]::IsNullOrWhiteSpace($PublicKeyPath)) { throw 'PublicKeyPath is required for Mint.' }
    if ([string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
        $PrivateKeyPath = Join-Path ([System.IO.Path]::GetTempPath()) ("wrap-private-" + [guid]::NewGuid().ToString('N') + '.pem')
    }

    $pubDir = Split-Path -Parent $PublicKeyPath
    if ($pubDir) { New-Item -ItemType Directory -Force -Path $pubDir | Out-Null }

    & openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:4096 -out $PrivateKeyPath
    if ($LASTEXITCODE -ne 0) { throw "openssl genpkey failed (exit $LASTEXITCODE)" }
    & openssl pkey -in $PrivateKeyPath -pubout -out $PublicKeyPath
    if ($LASTEXITCODE -ne 0) { throw "openssl pkey pubout failed (exit $LASTEXITCODE)" }

    $blob = Get-WrapBlobName -Id $RunId
    Invoke-AzBlobUploadFile -Account $StorageAccountName -Container $ContainerName -BlobName $blob -FilePath $PrivateKeyPath
    Remove-Item -LiteralPath $PrivateKeyPath -Force -ErrorAction SilentlyContinue
    Write-Host "Minted wrap key; public key at $PublicKeyPath; private key stored at $ContainerName/$blob"
}

function Invoke-Protect {
    if ([string]::IsNullOrWhiteSpace($PublicKeyPath) -or -not (Test-Path -LiteralPath $PublicKeyPath)) {
        throw "PublicKeyPath missing: $PublicKeyPath"
    }
    if ([string]::IsNullOrWhiteSpace($SourceDirectory) -or -not (Test-Path -LiteralPath $SourceDirectory)) {
        throw "SourceDirectory missing: $SourceDirectory"
    }
    if ([string]::IsNullOrWhiteSpace($BundleDirectory)) {
        throw 'BundleDirectory is required for Protect.'
    }

    New-Item -ItemType Directory -Force -Path $BundleDirectory | Out-Null
    $tarPath = Join-Path $BundleDirectory 'payload.tar.gz'
    $cipherPath = Join-Path $BundleDirectory 'payload.tar.gz.enc'
    $sessionKey = Join-Path $BundleDirectory 'session.key'
    $wrappedKey = Join-Path $BundleDirectory 'session.key.enc'

    & tar -czf $tarPath -C $SourceDirectory .
    if ($LASTEXITCODE -ne 0) { throw "tar failed (exit $LASTEXITCODE)" }

    $bytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    [System.IO.File]::WriteAllBytes($sessionKey, $bytes)

    & openssl enc -aes-256-cbc -pbkdf2 -salt -pass file:$sessionKey -in $tarPath -out $cipherPath
    if ($LASTEXITCODE -ne 0) { throw "openssl enc failed (exit $LASTEXITCODE)" }

    & openssl pkeyutl -encrypt -pubin -inkey $PublicKeyPath -pkeyopt rsa_padding_mode:oaep -in $sessionKey -out $wrappedKey
    if ($LASTEXITCODE -ne 0) { throw "openssl pkeyutl encrypt failed (exit $LASTEXITCODE)" }

    Remove-Item -LiteralPath $tarPath, $sessionKey -Force -ErrorAction SilentlyContinue
    Write-Host "Wrote ciphertext bundle under $BundleDirectory"
}

function Invoke-Unprotect {
    Assert-NoSecretMaterial
    if ([string]::IsNullOrWhiteSpace($BundleDirectory) -or -not (Test-Path -LiteralPath $BundleDirectory)) {
        throw "BundleDirectory missing: $BundleDirectory"
    }
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        throw 'OutputDirectory is required for Unprotect.'
    }

    $cipherPath = Join-Path $BundleDirectory 'payload.tar.gz.enc'
    $wrappedKey = Join-Path $BundleDirectory 'session.key.enc'
    if (-not (Test-Path -LiteralPath $cipherPath) -or -not (Test-Path -LiteralPath $wrappedKey)) {
        throw "Ciphertext incomplete under $BundleDirectory"
    }

    $ownsPrivateKeyFile = $false
    $privateKey = $PrivateKeyPath
    if ([string]::IsNullOrWhiteSpace($privateKey) -or -not (Test-Path -LiteralPath $privateKey)) {
        if ([string]::IsNullOrWhiteSpace($StorageAccountName) -or [string]::IsNullOrWhiteSpace($RunId)) {
            throw 'Unprotect needs PrivateKeyPath or StorageAccountName+RunId to fetch the wrap key.'
        }
        $privateKey = Join-Path ([System.IO.Path]::GetTempPath()) ("wrap-private-" + [guid]::NewGuid().ToString('N') + '.pem')
        $ownsPrivateKeyFile = $true
        $blob = Get-WrapBlobName -Id $RunId
        Invoke-AzBlobDownloadFile -Account $StorageAccountName -Container $ContainerName -BlobName $blob -FilePath $privateKey
    }

    $sessionKey = Join-Path ([System.IO.Path]::GetTempPath()) ("session-" + [guid]::NewGuid().ToString('N') + '.key')
    $tarPath = Join-Path ([System.IO.Path]::GetTempPath()) ("payload-" + [guid]::NewGuid().ToString('N') + '.tar.gz')

    try {
        & openssl pkeyutl -decrypt -inkey $privateKey -pkeyopt rsa_padding_mode:oaep -in $wrappedKey -out $sessionKey
        if ($LASTEXITCODE -ne 0) { throw "openssl pkeyutl decrypt failed (exit $LASTEXITCODE)" }
        & openssl enc -d -aes-256-cbc -pbkdf2 -pass file:$sessionKey -in $cipherPath -out $tarPath
        if ($LASTEXITCODE -ne 0) { throw "openssl enc decrypt failed (exit $LASTEXITCODE)" }

        Assert-TarEntriesAreRegularFilesOrDirectories -TarPath $tarPath
        New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
        & tar -xzf $tarPath -C $OutputDirectory
        if ($LASTEXITCODE -ne 0) { throw "tar extract failed (exit $LASTEXITCODE)" }
        Write-Host "Unprotected bundle into $OutputDirectory"
    }
    finally {
        Remove-Item -LiteralPath $sessionKey, $tarPath -Force -ErrorAction SilentlyContinue
        if ($ownsPrivateKeyFile) {
            Remove-Item -LiteralPath $privateKey -Force -ErrorAction SilentlyContinue
        }
    }
}

function Assert-TarEntriesAreRegularFilesOrDirectories {
    param([Parameter(Mandatory = $true)][string]$TarPath)

    $listing = & tar -tvzf $TarPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "tar -tvzf failed for $TarPath (exit $LASTEXITCODE): $listing"
    }

    foreach ($line in @($listing)) {
        $text = [string]$line
        if ([string]::IsNullOrWhiteSpace($text)) { continue }
        $type = $text[0]
        if ($type -ne '-' -and $type -ne 'd') {
            throw "Blocker: ciphertext tar contains a non-file/dir entry (refusing extract): $text"
        }
        # Path is the last whitespace-separated field in GNU tar -t output.
        $path = ($text -split '\s+', 6)[-1]
        if ($path.StartsWith('/') -or $path.Contains('../')) {
            throw "Blocker: ciphertext tar contains an unsafe path (refusing extract): $text"
        }
    }
}

function Invoke-FetchWrapKey {
    Assert-NoSecretMaterial
    if ([string]::IsNullOrWhiteSpace($StorageAccountName) -or [string]::IsNullOrWhiteSpace($RunId)) {
        throw 'FetchWrapKey requires StorageAccountName and RunId.'
    }
    if ([string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
        throw 'PrivateKeyPath is required for FetchWrapKey.'
    }
    $dir = Split-Path -Parent $PrivateKeyPath
    if ($dir) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    Invoke-AzBlobDownloadFile -Account $StorageAccountName -Container $ContainerName `
        -BlobName (Get-WrapBlobName -Id $RunId) -FilePath $PrivateKeyPath
}

function Invoke-DeleteWrapKey {
    Assert-NoSecretMaterial
    if ([string]::IsNullOrWhiteSpace($StorageAccountName) -or [string]::IsNullOrWhiteSpace($RunId)) {
        throw 'DeleteWrapKey requires StorageAccountName and RunId.'
    }
    Invoke-AzBlobDeleteFile -Account $StorageAccountName -Container $ContainerName -BlobName (Get-WrapBlobName -Id $RunId)
}

Assert-NoSecretMaterial
switch ($Mode) {
    'Mint' { Invoke-Mint }
    'Protect' { Invoke-Protect }
    'Unprotect' { Invoke-Unprotect }
    'FetchWrapKey' { Invoke-FetchWrapKey }
    'DeleteWrapKey' { Invoke-DeleteWrapKey }
}
