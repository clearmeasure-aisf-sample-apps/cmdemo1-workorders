#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Get-PrAcceptanceArtifactSelection.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("sel-tests-" + [guid]::NewGuid().ToString('N'))
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
    <UnitTestResult testName="ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests.Should_Pass" outcome="Passed" />
  </Results>
</TestRun>
"@
    Set-Content -LiteralPath (Join-Path $trxDir 'a.trx') -Value $trx

    $failedTrace = 'LandingPageTests.Should_Fail.zip'
    Set-Content -LiteralPath (Join-Path $traceDir $failedTrace) -Value 'PK'
    # Compress-Archive needs a real zip; create via .NET empty zip-like file by compressing a tiny file
    Remove-Item (Join-Path $traceDir $failedTrace) -Force
    $tiny = Join-Path $temp 't.txt'
    Set-Content $tiny 'x'
    Compress-Archive -Path $tiny -DestinationPath (Join-Path $traceDir 'LandingPageTests.Should_Fail.zip') -Force

    $videoFolder = Join-Path $videoDir 'LandingPageTests.Should_Fail'
    New-Item -ItemType Directory -Force -Path $videoFolder | Out-Null
    Set-Content -LiteralPath (Join-Path $videoFolder 'video.webm') -Value 'webm'

    $json = & pwsh -NoProfile -File $scriptPath -TrxDirectory $trxDir -TraceDirectory $traceDir -VideoDirectory $videoDir -AsJson
    Assert-True ($LASTEXITCODE -eq 0) "Selection exit $LASTEXITCODE"
    $items = @($json | ConvertFrom-Json)
    Assert-True ($items.Count -ge 1) 'Expected at least the failed test'
    $failed = $items | Where-Object { $_.TestName -like '*Should_Fail*' } | Select-Object -First 1
    Assert-True ($null -ne $failed) 'Failed test not selected'
    Assert-True ($failed.Reason -match 'failed') 'Reason should include failed'
    Assert-True (-not [string]::IsNullOrWhiteSpace($failed.VideoPath)) 'Video path missing for failed test'

    # Changed-UI via Login.razor map (fixture exists on master)
    $pathsFile = Join-Path $temp 'razor.txt'
    Set-Content -LiteralPath $pathsFile -Value 'src/UI.Shared/Pages/Login.razor'
    $changedJson = & pwsh -NoProfile -File $scriptPath `
        -TraceDirectory $traceDir `
        -VideoDirectory $videoDir `
        -ChangedRazorPathsFile $pathsFile `
        -AsJson
    Assert-True ($LASTEXITCODE -eq 0) "Changed-UI selection exit $LASTEXITCODE"

    Write-Host 'Get-PrAcceptanceArtifactSelection.Tests.ps1 passed'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
