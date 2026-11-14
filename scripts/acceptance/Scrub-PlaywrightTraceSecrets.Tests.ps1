#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Scrub-PlaywrightTraceSecrets.ps1'
. (Join-Path $PSScriptRoot 'SecretHeaders.ps1')

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("scrub-tests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $extract = Join-Path $temp 'raw'
    New-Item -ItemType Directory -Force -Path $extract | Out-Null
    $network = @(
        '{"type":"resource-snapshot","snapshot":{"request":{"headers":[{"name":"X-Test-Support-Secret","value":"acceptance-test-support-secret"},{"name":"Accept","value":"text/html"}]}}}',
        '{"type":"resource-snapshot","snapshot":{"request":{"headers":[{"value":"Bearer abc","name":"Authorization"}]}}}',
        '{"type":"console","text":"ok"}'
    ) -join "`n"
    Set-Content -LiteralPath (Join-Path $extract 'trace.network') -Value $network -NoNewline
    $zip = Join-Path $temp 'sample.zip'
    Compress-Archive -Path (Join-Path $extract '*') -DestinationPath $zip -Force

    $out = Join-Path $temp 'scrubbed.zip'
    & pwsh -NoProfile -File $scriptPath -Path $zip -Destination $out
    Assert-True ($LASTEXITCODE -eq 0) "Scrub script exit $LASTEXITCODE"

    $verify = Join-Path $temp 'verify'
    Expand-Archive -LiteralPath $out -DestinationPath $verify -Force
    $text = Get-Content -LiteralPath (Join-Path $verify 'trace.network') -Raw
    Assert-True ($text -notmatch 'acceptance-test-support-secret') 'Plaintext test-support secret must be scrubbed'
    Assert-True ($text -notmatch 'Bearer abc') 'Authorization value must be scrubbed'
    Assert-True ($text -match '\[REDACTED\]') 'Redacted marker must appear'
    Assert-True ($text -match 'X-Test-Support-Secret') 'Header name may remain'
    Assert-True ($text -match '"Accept":"text/html"|\"Accept\",\"value\":\"text/html\"|name":"Accept","value":"text/html"') 'Non-secret headers must remain'

    Assert-True (Test-PrAcceptanceSecretHeaderName -Name 'x-test-support-secret') 'Header name match is case-insensitive'
    Assert-True (-not (Test-PrAcceptanceSecretHeaderName -Name 'Accept')) 'Accept is not secret'

    Write-Host 'Scrub-PlaywrightTraceSecrets.Tests.ps1 passed'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
