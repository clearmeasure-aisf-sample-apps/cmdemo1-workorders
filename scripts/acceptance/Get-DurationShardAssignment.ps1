<#
.SYNOPSIS
  Packs acceptance test classes into duration-balanced shards for PR-environment runs.

.DESCRIPTION
  Reads src/AcceptanceTests/acceptance-durations.json (measured class durations) and
  assigns each class to one of -ShardCount shards using greedy bin-packing (longest
  first, lightest shard wins). Prints a FullyQualifiedName filter for -ShardNumber,
  or the full assignment JSON when -ShardNumber is 0.

  With -ExcludeSerialLane (default for PR-environment parallel packs), classes listed
  in acceptance-serial-lane.json are omitted so the serial lane can run after every
  parallel shard on the same slot — never beside it.

  Does not edit .github/workflows/. Wire this into CI only after Jeffrey approves a
  workflow change (see issue #1661 / #1662 / PR environments plan).

.PARAMETER ShardNumber
  1..ShardCount for a filter; 0 to emit the full assignment.

.PARAMETER ShardCount
  Number of runners (default 12 for the PR-environment plan).

.PARAMETER ExcludeSerialLane
  When set, drop entirely-serial fixtures from the pack (default: $true).
#>
[CmdletBinding()]
param(
    [int]$ShardNumber = 0,
    [int]$ShardCount = 12,
    [string]$DurationsPath = "",
    [string]$SerialLanePath = "",
    [switch]$ExcludeSerialLane
)

$ErrorActionPreference = "Stop"

# Default: exclude serial fixtures from parallel packs unless the caller passes -ExcludeSerialLane:$false
if (-not $PSBoundParameters.ContainsKey('ExcludeSerialLane')) {
    $ExcludeSerialLane = $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

if ([string]::IsNullOrWhiteSpace($DurationsPath)) {
    $DurationsPath = Join-Path $repoRoot "src/AcceptanceTests/acceptance-durations.json"
}

if ([string]::IsNullOrWhiteSpace($SerialLanePath)) {
    $SerialLanePath = Join-Path $repoRoot "src/AcceptanceTests/acceptance-serial-lane.json"
}

if (-not (Test-Path -LiteralPath $DurationsPath)) {
    throw "Durations file not found: $DurationsPath"
}

$doc = Get-Content -LiteralPath $DurationsPath -Raw | ConvertFrom-Json
$classes = @($doc.classes)
if ($classes.Count -eq 0) {
    throw "No classes in $DurationsPath"
}

$serialNames = @{}
if ($ExcludeSerialLane) {
    if (-not (Test-Path -LiteralPath $SerialLanePath)) {
        throw "Serial lane file not found: $SerialLanePath"
    }

    $serialDoc = Get-Content -LiteralPath $SerialLanePath -Raw | ConvertFrom-Json
    foreach ($name in @($serialDoc.classes)) {
        $serialNames[$name] = $true
    }

    $classes = @($classes | Where-Object { -not $serialNames.ContainsKey($_.name) })
    if ($classes.Count -eq 0) {
        throw "No classes remain after excluding the serial lane."
    }
}

$buckets = 1..$ShardCount | ForEach-Object {
    [pscustomobject]@{ Shard = $_; Total = 0.0; Classes = New-Object System.Collections.Generic.List[object] }
}

$ordered = $classes | Sort-Object @{ Expression = { [double]$_.seconds }; Descending = $true }, @{ Expression = { $_.name }; Ascending = $true }
foreach ($item in $ordered) {
    $lightest = $buckets | Sort-Object Total, Shard | Select-Object -First 1
    $lightest.Classes.Add($item)
    $lightest.Total += [double]$item.seconds
}

if ($ShardNumber -lt 0 -or $ShardNumber -gt $ShardCount) {
    throw "ShardNumber must be 0 (all) or 1..$ShardCount"
}

if ($ShardNumber -eq 0) {
    $buckets | ConvertTo-Json -Depth 6
    return
}

$bucket = $buckets | Where-Object { $_.Shard -eq $ShardNumber } | Select-Object -First 1
if ($null -eq $bucket -or $bucket.Classes.Count -eq 0) {
    throw "Shard $ShardNumber has no classes."
}

$parts = $bucket.Classes | ForEach-Object { "FullyQualifiedName~" + $_.name }
$parts -join "|"
