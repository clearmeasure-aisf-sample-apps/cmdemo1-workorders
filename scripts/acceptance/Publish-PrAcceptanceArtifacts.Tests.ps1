#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Publish-PrAcceptanceArtifacts.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# Blocker path: SAS/key env must fail fast.
$env:AZURE_STORAGE_SAS_TOKEN = 'not-a-real-sas'
try {
    & pwsh -NoProfile -File $scriptPath -Mode Upload -DryRun -StorageAccountName 'acct' -RunId '1' 2>&1 | Out-Null
    Assert-True ($LASTEXITCODE -ne 0) 'Must refuse AZURE_STORAGE_SAS_TOKEN'
}
finally {
    Remove-Item Env:AZURE_STORAGE_SAS_TOKEN -ErrorAction SilentlyContinue
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("pub-tests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $trxDir = Join-Path $temp 'trx'
    $traceDir = Join-Path $temp 'traces'
    $videoDir = Join-Path $temp 'videos'
    New-Item -ItemType Directory -Force -Path $trxDir, $traceDir, $videoDir | Out-Null

    $trx = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests.Should_Fail" outcome="Failed" />
  </Results>
</TestRun>
"@
    Set-Content -LiteralPath (Join-Path $trxDir 'a.trx') -Value $trx

    $extract = Join-Path $temp 'raw'
    New-Item -ItemType Directory -Force -Path $extract | Out-Null
    $network = '{"request":{"headers":[{"name":"X-Test-Support-Secret","value":"acceptance-test-support-secret"}]}}'
    Set-Content -LiteralPath (Join-Path $extract 'trace.network') -Value $network -NoNewline
    Compress-Archive -Path (Join-Path $extract '*') -DestinationPath (Join-Path $traceDir 'LandingPageTests.Should_Fail.zip') -Force

    $vdir = Join-Path $videoDir 'LandingPageTests.Should_Fail'
    New-Item -ItemType Directory -Force -Path $vdir | Out-Null
    Set-Content -LiteralPath (Join-Path $vdir 'x.webm') -Value 'webm'

    $manifest = Join-Path $temp 'manifest.json'
    & pwsh -NoProfile -File $scriptPath `
        -Mode Upload `
        -DryRun `
        -StorageAccountName 'stcmdemo1poolart' `
        -RunId '42' `
        -TrxDirectory $trxDir `
        -TraceDirectory $traceDir `
        -VideoDirectory $videoDir `
        -ManifestPath $manifest
    Assert-True ($LASTEXITCODE -eq 0) "DryRun upload exit $LASTEXITCODE"
    Assert-True (Test-Path -LiteralPath $manifest) 'Manifest not written'
    $parsed = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
    $rows = @($parsed)
    Assert-True ($rows.Count -ge 1) 'Manifest empty'
    Assert-True ($rows[0].VideoBlob -like 'videos/42/*') 'Video blob path prefix'
    Assert-True ($rows[0].TraceBlob -like 'traces/42/*') 'Trace blob path prefix'

    & pwsh -NoProfile -File $scriptPath `
        -Mode Comment `
        -DryRun `
        -StorageAccountName 'stcmdemo1poolart' `
        -RunId '42' `
        -ManifestPath $manifest `
        -PullRequestNumber '1' `
        -Repository 'clearmeasure-aisf-sample-apps/cmdemo1-workorders'
    Assert-True ($LASTEXITCODE -eq 0) "DryRun comment exit $LASTEXITCODE"

    Write-Host 'Publish-PrAcceptanceArtifacts.Tests.ps1 passed'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
