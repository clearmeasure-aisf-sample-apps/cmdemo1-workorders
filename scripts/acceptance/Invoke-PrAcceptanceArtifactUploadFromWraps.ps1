<#
.SYNOPSIS
  Decrypts shard ciphertext bundles and uploads videos/traces (#1671).

.DESCRIPTION
  Trusted-job helper: fetch the wrap private key once from private Azure storage
  (--auth-mode login), Unprotect each ciphertext bundle under -WrapRoot, then
  Publish-PrAcceptanceArtifacts -Mode Upload. Deletes the wrap key when finished.
  No account keys, SAS tokens, or connection strings.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,

    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [Parameter(Mandatory = $true)]
    [string]$WrapRoot,

    [string]$PublishScript = '',

    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

if ($SelfTest) {
    Write-Host 'Invoke-PrAcceptanceArtifactUploadFromWraps.ps1 self-test: syntax only'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($PublishScript)) {
    $PublishScript = Join-Path $PSScriptRoot 'Publish-PrAcceptanceArtifacts.ps1'
}
$protectScript = Join-Path $PSScriptRoot 'Protect-PrAcceptanceArtifactBundle.ps1'

if (-not (Test-Path -LiteralPath $WrapRoot)) {
    Write-Host "No wrap root at $WrapRoot; nothing to upload."
    exit 0
}

$bundles = @(Get-ChildItem -LiteralPath $WrapRoot -Directory -ErrorAction SilentlyContinue)
if ($bundles.Count -eq 0) {
    $enc = Get-ChildItem -LiteralPath $WrapRoot -Filter 'payload.tar.gz.enc' -Recurse -File -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $enc) {
        Write-Host 'No ciphertext bundles found; nothing to upload.'
        exit 0
    }
    $bundles = @($enc.Directory)
}

$privateKey = Join-Path ([System.IO.Path]::GetTempPath()) ('wrap-private-' + [guid]::NewGuid().ToString('N') + '.pem')
& pwsh -NoProfile -File $protectScript `
    -Mode FetchWrapKey `
    -StorageAccountName $StorageAccountName `
    -RunId $RunId `
    -PrivateKeyPath $privateKey
if ($LASTEXITCODE -ne 0) {
    throw "FetchWrapKey failed (exit $LASTEXITCODE)."
}

$uploaded = 0
try {
    foreach ($bundle in $bundles) {
        $cipher = Join-Path $bundle.FullName 'payload.tar.gz.enc'
        $wrapped = Join-Path $bundle.FullName 'session.key.enc'
        if (-not (Test-Path -LiteralPath $cipher) -or -not (Test-Path -LiteralPath $wrapped)) {
            Write-Host "Skipping incomplete bundle $($bundle.FullName)"
            continue
        }

        $out = Join-Path ([System.IO.Path]::GetTempPath()) ('unwrapped-' + [guid]::NewGuid().ToString('N'))
        & pwsh -NoProfile -File $protectScript `
            -Mode Unprotect `
            -PrivateKeyPath $privateKey `
            -BundleDirectory $bundle.FullName `
            -OutputDirectory $out
        if ($LASTEXITCODE -ne 0) {
            throw "Unprotect failed for $($bundle.FullName) (exit $LASTEXITCODE)"
        }

        & pwsh -NoProfile -File $PublishScript `
            -Mode Upload `
            -StorageAccountName $StorageAccountName `
            -RunId $RunId `
            -TrxDirectory $out `
            -TraceDirectory $out `
            -VideoDirectory $out
        if ($LASTEXITCODE -ne 0) {
            throw "Publish Upload failed for $($bundle.FullName) (exit $LASTEXITCODE)"
        }
        $uploaded++
        Remove-Item -LiteralPath $out -Recurse -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Remove-Item -LiteralPath $privateKey -Force -ErrorAction SilentlyContinue
    & pwsh -NoProfile -File $protectScript `
        -Mode DeleteWrapKey `
        -StorageAccountName $StorageAccountName `
        -RunId $RunId
}

Write-Host "Uploaded from $uploaded ciphertext bundle(s)."
