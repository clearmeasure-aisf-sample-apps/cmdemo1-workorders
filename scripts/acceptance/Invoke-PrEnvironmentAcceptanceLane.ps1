<#
.SYNOPSIS
  Runs one PR-environment acceptance lane (parallel shard or serial) against a host.

.DESCRIPTION
  Parallel: duration-balanced class filter for -ShardNumber, excluding SerialLane
  classes from the pack, combined with TestCategory!=SerialLane so method-level
  SerialLane cases (e.g. one DashboardTests method) are skipped.

  Serial: TestCategory==SerialLane with a single worker. Call this only after every
  parallel shard on the same slot has finished — never beside the parallel lane.

  Does not edit .github/workflows/ (see issue #1664). Does not loosen gates.
  Sets StartLocalServer=false so ServerFixture skips ZDataLoader and the sweep.

.PARAMETER Lane
  Parallel or Serial.

.PARAMETER ShardNumber
  Required for Parallel (1..ShardCount). Ignored for Serial.

.PARAMETER ApplicationBaseUrl
  Pool-slot base URL (https://...).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Parallel", "Serial")]
    [string]$Lane,

    [int]$ShardNumber = 0,

    [int]$ShardCount = 12,

    [Parameter(Mandatory = $true)]
    [string]$ApplicationBaseUrl,

    [string]$Configuration = "Release",

    [string]$ResultsDirectory = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$acceptanceProject = Join-Path $repoRoot "src/AcceptanceTests/AcceptanceTests.csproj"
$runSettings = Join-Path $repoRoot "src/AcceptanceTests/AcceptanceTests.runsettings"

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path $repoRoot "build/test/AcceptanceTests-$Lane"
}

$env:ApplicationBaseUrl = $ApplicationBaseUrl.TrimEnd('/')
$env:StartLocalServer = "false"
$env:StartWorker = "false"

if ($Lane -eq "Parallel") {
    if ($ShardNumber -lt 1 -or $ShardNumber -gt $ShardCount) {
        throw "Parallel lane requires -ShardNumber 1..$ShardCount"
    }

    & (Join-Path $PSScriptRoot "Write-AcceptanceRunSettings.ps1") -OutputPath $runSettings
    $classFilter = & (Join-Path $PSScriptRoot "Get-DurationShardAssignment.ps1") `
        -ShardNumber $ShardNumber -ShardCount $ShardCount -ExcludeSerialLane
    $filter = "($classFilter)&TestCategory!=SerialLane"
}
else {
    & (Join-Path $PSScriptRoot "Write-AcceptanceRunSettings.ps1") -OutputPath $runSettings -Workers 1
    $filter = & (Join-Path $PSScriptRoot "Get-SerialLaneFilter.ps1")
}

Write-Host "Lane=$Lane filter=$filter"
New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null

dotnet test $acceptanceProject `
    --configuration $Configuration `
    --settings $runSettings `
    --filter $filter `
    --logger "trx;LogFilePrefix=$Lane" `
    --results-directory $ResultsDirectory

exit $LASTEXITCODE
