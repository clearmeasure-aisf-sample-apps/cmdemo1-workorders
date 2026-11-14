#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Scrub-PlaywrightTraceSecrets.ps1'
. (Join-Path $PSScriptRoot 'SecretHeaders.ps1')
. (Join-Path $PSScriptRoot 'PlaywrightTraceScrub.ps1')

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("scrub-tests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $extract = Join-Path $temp 'raw'
    New-Item -ItemType Directory -Force -Path $extract | Out-Null

    # Faithful Playwright-style JSONL covering every header shape the scrubber must handle.
    $liveSecret = 'live-env-support-secret-value-9f3a'
    $env:TestSupport__Secret = $liveSecret
    $env:PR_ACCEPTANCE_SECRET_HEADERS = 'X-Custom-Acceptance-Secret'

    # Build JSONL outside @() so commas / + concatenation cannot split array elements.
    $liveLine = '{"type":"resource-snapshot","snapshot":{"request":{"headers":[{"name":"X-Test-Support-Secret","value":"REPLACE_LIVE"},{"name":"X-Custom-Acceptance-Secret","value":"custom-extra-secret"}]}}}'
    $liveLine = $liveLine.Replace('REPLACE_LIVE', $liveSecret)
    $pairLine = '{"type":"resource-snapshot","snapshot":{"request":{"headers":[["X-Test-Support-Secret","acceptance-test-support-secret"],["Accept","*/*"]]}}}'
    $networkLines = [System.Collections.Generic.List[string]]::new()
    [void]$networkLines.Add('{"type":"resource-snapshot","snapshot":{"request":{"headers":[{"name":"X-Test-Support-Secret","value":"acceptance-test-support-secret"},{"name":"Accept","value":"text/html"}],"cookies":[{"name":"session","value":"sess-cookie-abc"}]},"response":{"headers":[{"name":"Set-Cookie","value":"session=sess-cookie-abc; Path=/"}]}}}')
    [void]$networkLines.Add('{"type":"resource-snapshot","snapshot":{"request":{"headers":[{"value":"Bearer abc","name":"Authorization"}]}}}')
    [void]$networkLines.Add('{"type":"resource-snapshot","snapshot":{"request":{"headers":{"X-Test-Support-Secret":"acceptance-test-support-secret","X-Api-Key":"api-key-should-go","Accept":"application/json"}}}}')
    [void]$networkLines.Add($pairLine)
    [void]$networkLines.Add($liveLine)
    [void]$networkLines.Add('{"type":"console","text":"ok"}')
    Set-Content -LiteralPath (Join-Path $extract 'trace.network') -Value (($networkLines -join "`n") + "`n") -NoNewline

    # Shape 6: pretty-printed / split across lines (whole-document JSON in resources/)
    $resourcesDir = Join-Path $extract 'resources'
    New-Item -ItemType Directory -Force -Path $resourcesDir | Out-Null
    $pretty = @'
{
  "request": {
    "headers": [
      {
        "name": "Cookie",
        "value": "auth=super-secret-cookie"
      },
      {
        "name": "Accept",
        "value": "text/plain"
      }
    ]
  }
}
'@
    Set-Content -LiteralPath (Join-Path $resourcesDir '0.json') -Value $pretty

    # Minimal trace.trace entry referencing a header map
    $traceLine = '{"type":"before","params":{"headers":{"Authorization":"Bearer trace-token"}}}'
    Set-Content -LiteralPath (Join-Path $extract 'trace.trace') -Value $traceLine -NoNewline

    $zip = Join-Path $temp 'sample.zip'
    Compress-Archive -Path (Join-Path $extract '*') -DestinationPath $zip -Force

    $out = Join-Path $temp 'scrubbed.zip'
    & pwsh -NoProfile -File $scriptPath -Path $zip -Destination $out
    Assert-True ($LASTEXITCODE -eq 0) "Scrub script exit $LASTEXITCODE"

    # Upload verifier path: must pass on scrubbed zip and fail if a secret is re-injected.
    Assert-TraceZipScrubbed -ZipPath $out

    $verify = Join-Path $temp 'verify'
    Expand-Archive -LiteralPath $out -DestinationPath $verify -Force
    $networkText = Get-Content -LiteralPath (Join-Path $verify 'trace.network') -Raw
    $resourceText = Get-Content -LiteralPath (Join-Path $verify 'resources' '0.json') -Raw
    $traceText = Get-Content -LiteralPath (Join-Path $verify 'trace.trace') -Raw
    $all = $networkText + $resourceText + $traceText

    Assert-True ($all -notmatch 'acceptance-test-support-secret') 'Plaintext test-support secret must be scrubbed'
    Assert-True ($all -notmatch [regex]::Escape($liveSecret)) 'Live env secret must be scrubbed without remaining plaintext'
    Assert-True ($all -notmatch 'Bearer abc') 'Authorization value must be scrubbed'
    Assert-True ($all -notmatch 'api-key-should-go') 'X-Api-Key map value must be scrubbed'
    Assert-True ($all -notmatch 'custom-extra-secret') 'Configurable denylist header value must be scrubbed'
    Assert-True ($all -notmatch 'sess-cookie-abc') 'Cookie collection values must be scrubbed'
    Assert-True ($all -notmatch 'super-secret-cookie') 'Split-line Cookie header must be scrubbed'
    Assert-True ($all -notmatch 'Bearer trace-token') 'trace.trace Authorization map must be scrubbed'
    Assert-True ($all -match '\[REDACTED\]') 'Redacted marker must appear'
    Assert-True ($all -match 'X-Test-Support-Secret') 'Header name may remain'
    Assert-True ($all -match 'text/html|application/json|\*/\*') 'Non-secret Accept values must remain'

    Assert-True (Test-PrAcceptanceSecretHeaderName -Name 'x-test-support-secret') 'Header name match is case-insensitive'
    Assert-True (Test-PrAcceptanceSecretHeaderName -Name 'X-Custom-Acceptance-Secret') 'Configurable header is denylisted'
    Assert-True (-not (Test-PrAcceptanceSecretHeaderName -Name 'Accept')) 'Accept is not secret'

    # Verifier must fail when a denylisted value is still present.
    $bad = Join-Path $temp 'bad'
    New-Item -ItemType Directory -Force -Path $bad | Out-Null
    Set-Content -LiteralPath (Join-Path $bad 'trace.network') -Value '{"request":{"headers":[{"name":"Authorization","value":"Bearer still-here"}]}}' -NoNewline
    $badZip = Join-Path $temp 'bad.zip'
    Compress-Archive -Path (Join-Path $bad '*') -DestinationPath $badZip -Force
    $failed = $false
    try {
        Assert-TraceZipScrubbed -ZipPath $badZip
    }
    catch {
        $failed = $true
    }
    Assert-True $failed 'Verifier must fail when denylisted header values survive'

    Write-Host 'Scrub-PlaywrightTraceSecrets.Tests.ps1 passed'
}
finally {
    Remove-Item Env:TestSupport__Secret -ErrorAction SilentlyContinue
    Remove-Item Env:PR_ACCEPTANCE_SECRET_HEADERS -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
