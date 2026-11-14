#!/usr/bin/env pwsh
#Requires -Version 7.4

<#
.SYNOPSIS
  Lease a warm pull-request pool slot for the PR environment workflow (issue #1664).

.DESCRIPTION
  Wraps cmdemo1-system scripts/pool/lease.ps1. Polls until a free slot is selected,
  prints "waiting for a slot" while empty, and sets WaitNotify=true after
  pool.lease.waitNotifyMinutes (default 5) so the workflow can post on the PR.

  Live Azure lease store requires system #652; until StorageAccount is set this
  uses -DryRun and selects pool-1 (shadow mode). Does not loosen gates.

.PARAMETER SystemRoot
  Checkout root of clearmeasure-aisf-sample-apps/cmdemo1-system (contains scripts/pool).

.PARAMETER PrNumber, CommitSha, RunId, Repository
  Lease metadata written into the blob / DryRun record.

.PARAMETER PreferWarmCount
  How many leading slots are warm (shadow: 2).

.PARAMETER PollSeconds
  Sleep between attempts when every slot is leased (live mode).

.PARAMETER MaxWaitMinutes
  Give up after this many minutes of waiting (default 30 = pool.lease.maxMinutes).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $SystemRoot,

    [Parameter(Mandatory)]
    [int] $PrNumber,

    [Parameter(Mandatory)]
    [string] $CommitSha,

    [Parameter(Mandatory)]
    [string] $RunId,

    [string] $Repository = '',

    [string] $StorageAccount = '',

    [string] $Container = 'leases',

    [int] $PreferWarmCount = 2,

    [int] $PollSeconds = 20,

    [int] $MaxWaitMinutes = 30,

    [int] $WaitNotifyMinutes = 5,

    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$leaseScript = Join-Path $SystemRoot 'scripts/pool/lease.ps1'
if (-not (Test-Path -LiteralPath $leaseScript)) {
    throw "System lease script not found at $leaseScript"
}

. $leaseScript -LoadOnly -Root $SystemRoot

$system = Get-PoolSystem -RepositoryRoot $SystemRoot
$pool = if ($system.ContainsKey('pool')) { $system.pool } else { @{} }
$warm = Get-PoolWarmConfig -Pool $pool
$leaseCfg = Get-PoolLeaseConfig -Pool $pool
if ($PreferWarmCount -le 0) { $PreferWarmCount = [int] $warm.count }
if ($WaitNotifyMinutes -le 0) { $WaitNotifyMinutes = [int] $leaseCfg.waitNotifyMinutes }
if ($MaxWaitMinutes -le 0) { $MaxWaitMinutes = [int] $leaseCfg.maxMinutes }

$slotNames = @(Get-PoolSlotNames -Slots ([int] $pool.slots))
$waitStarted = [datetime]::SpecifyKind([datetime]::UtcNow, [DateTimeKind]::Utc)
$notified = $false
$deadline = $waitStarted.AddMinutes($MaxWaitMinutes)
$useDryRun = $DryRun -or [string]::IsNullOrWhiteSpace($StorageAccount)

function Write-LeaseOutputs {
    param(
        [Parameter(Mandatory)][string] $Slot,
        [string] $LeaseId = '',
        [bool] $WaitNotify = $false,
        [bool] $IsDryRun = $false
    )
    $slotNumber = if ($Slot -match '(\d+)$') { $Matches[1] } else { '' }
    $waitNotifyText = if ($WaitNotify) { 'true' } else { 'false' }
    $dryRunText = if ($IsDryRun) { 'true' } else { 'false' }
    if ($env:GITHUB_OUTPUT) {
        @"
slot=$Slot
slot_number=$slotNumber
lease_id=$LeaseId
wait_notify=$waitNotifyText
dry_run=$dryRunText
"@ | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "slot=$Slot lease_id=$LeaseId wait_notify=$waitNotifyText dry_run=$dryRunText"
}

while ([datetime]::UtcNow -lt $deadline) {
    $now = [datetime]::SpecifyKind([datetime]::UtcNow, [DateTimeKind]::Utc)
    if (-not $notified -and (Test-PoolWaitShouldNotify -WaitStartedAt $waitStarted -Now $now -WaitNotifyMinutes $WaitNotifyMinutes)) {
        $notified = $true
        Write-Host "WAIT_NOTIFY=true"
    }

    if ($useDryRun) {
        # Shadow / no lease store: select the preferred warm slot and record a DryRun lease.
        $slot = Select-PoolLeaseToAcquire -SlotNames $slotNames -LeasedSlots @() -PreferWarmCount $PreferWarmCount
        if (-not $slot) { $slot = 'pool-1' }
        & $leaseScript -Root $SystemRoot -Action lease -Slot $slot -PrNumber $PrNumber `
            -CommitSha $CommitSha -RunId $RunId -Repository $Repository -DryRun
        Write-LeaseOutputs -Slot $slot -LeaseId 'dry-run' -WaitNotify:$notified -IsDryRun:$true
        exit 0
    }

    # Live: list leased blobs via az (when configured). Until #652 wires list helpers into lease.ps1,
    # attempt each warm-preferred slot and treat "already leased" as continue waiting.
    $ordered = @(
        $slotNames | Select-Object -First ([Math]::Min($PreferWarmCount, $slotNames.Count))
    ) + @(
        $slotNames | Select-Object -Skip ([Math]::Min($PreferWarmCount, $slotNames.Count))
    )

    foreach ($candidate in $ordered) {
        try {
            $json = & $leaseScript -Root $SystemRoot -Action lease -Slot $candidate `
                -PrNumber $PrNumber -CommitSha $CommitSha -RunId $RunId -Repository $Repository `
                -StorageAccount $StorageAccount -Container $Container 2>&1
            $record = $json | Select-Object -Last 1 | ConvertFrom-Json
            Write-LeaseOutputs -Slot $candidate -LeaseId ([string] $record.leaseId) -WaitNotify:$notified -IsDryRun:$false
            exit 0
        }
        catch {
            Write-Host "Slot $candidate not acquired: $($_.Exception.Message)"
        }
    }

    Write-Host (Get-PoolWaitingForSlotMessage)
    Start-Sleep -Seconds $PollSeconds
}

throw "Timed out after $MaxWaitMinutes minutes waiting for a pool slot."
