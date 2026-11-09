#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when src suppressions lack a reason, rise above the baseline, or carry task markers.

.DESCRIPTION
  Counts `#pragma warning disable`, `// ReSharper disable`, and SuppressMessage attributes
  under src (.cs, .razor, .cshtml; skips bin and obj). Each one needs a reason on the same
  line or the preceding comment. The count may sit at or below scripts/quality/suppression-budget.json.
  TODO, FIXME, and HACK in that same source set fail the run.

  -SelfTest runs the fixture checks and does not scan the repository.
#>
[CmdletBinding()]
param(
    [string]$SourceRoot,
    [string]$BaselinePath,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SuppressionBudget.ps1')

if ($SelfTest) {
    $testScript = Join-Path $PSScriptRoot 'SuppressionBudget.Tests.ps1'
    & pwsh -NoProfile -File $testScript
    exit $LASTEXITCODE
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $repoRoot 'src'
}

if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $BaselinePath = Join-Path $PSScriptRoot 'suppression-budget.json'
}

try {
    $max = Read-SuppressionBudgetBaseline -Path $BaselinePath
    $report = Get-SuppressionBudgetReport -SourceRoot $SourceRoot -MaxSuppressions $max
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 2
}

Write-SuppressionBudgetResult -Report $report
if ($report.Passed) { exit 0 }
exit 1
