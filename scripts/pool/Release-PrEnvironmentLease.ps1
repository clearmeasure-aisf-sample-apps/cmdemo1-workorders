#!/usr/bin/env pwsh
#Requires -Version 7.4

<#
.SYNOPSIS
  Release a pull-request pool slot lease (issue #1664).

.PARAMETER SystemRoot
  Checkout root of cmdemo1-system.

.PARAMETER Slot
  pool-N. Empty means nothing was leased; the script exits 0 (safe under if: always()).

.PARAMETER LeaseId
  Lease id from Acquire (dry-run uses 'dry-run').
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $SystemRoot,

    [string] $Slot = '',

    [string] $LeaseId = '',

    [string] $StorageAccount = '',

    [string] $Container = 'leases',

    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Slot)) {
    Write-Host 'No slot leased; nothing to release.'
    exit 0
}

$leaseScript = Join-Path $SystemRoot 'scripts/pool/lease.ps1'
if (-not (Test-Path -LiteralPath $leaseScript)) {
    throw "System lease script not found at $leaseScript"
}

$useDryRun = $DryRun -or [string]::IsNullOrWhiteSpace($StorageAccount) -or $LeaseId -eq 'dry-run'
& $leaseScript -Root $SystemRoot -Action release -Slot $Slot -LeaseId $LeaseId `
    -StorageAccount $StorageAccount -Container $Container -DryRun:$useDryRun
