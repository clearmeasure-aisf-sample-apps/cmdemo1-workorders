#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when docs/ has a top-level entry outside the allowlist.

.DESCRIPTION
  Top-level names under docs/ must be listed in docs-allowlist.txt. The list is the
  ratchet: a name that is not listed fails the build. Remove the row when that entry
  is deleted so it cannot come back. Nested files are not listed; only the top-level
  name is gated (same shape as scripts/repo-root/root-allowlist.txt).

  -SelfTest runs the fixture cases, then checks this repo. Exits 0 only when both pass.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = '',
    [string]$AllowlistPath = '',
    [switch]$SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}
if ([string]::IsNullOrWhiteSpace($AllowlistPath)) {
    $AllowlistPath = Join-Path $PSScriptRoot 'docs-allowlist.txt'
}

function Read-DocsAllowlist {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Allowlist not found: $Path"
    }

    $names = [System.Collections.Generic.List[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $lineNumber++
        $trimmed = $line.Trim()
        $hash = $trimmed.IndexOf('#')
        if ($hash -ge 0) {
            $trimmed = $trimmed.Substring(0, $hash).Trim()
        }
        if ($trimmed.Length -eq 0) {
            continue
        }
        if ($trimmed.Contains('/') -or $trimmed.Contains('\') -or $trimmed -eq '.' -or $trimmed -eq '..') {
            throw "Allowlist entry must be a single docs top-level name (line ${lineNumber}): $trimmed"
        }
        if (-not $seen.Add($trimmed)) {
            throw "Duplicate allowlist entry (line ${lineNumber}): $trimmed"
        }
        $names.Add($trimmed)
    }

    Write-Output -NoEnumerate $names.ToArray()
}

class DocsAllowlistViolationBag {
    [System.Collections.Generic.List[string]]$Items = [System.Collections.Generic.List[string]]::new()
}

function Add-DocsAllowlistViolations {
    param(
        [Parameter(Mandatory = $true)][string]$DocsRoot,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Allowlist,
        [Parameter(Mandatory = $true)][DocsAllowlistViolationBag]$Violations
    )

    if (-not (Test-Path -LiteralPath $DocsRoot)) {
        $Violations.Items.Add("docs directory missing: $DocsRoot")
        return
    }

    $allowed = [System.Collections.Generic.HashSet[string]]::new($Allowlist, [StringComparer]::Ordinal)
    foreach ($child in @(Get-ChildItem -LiteralPath $DocsRoot -Force)) {
        if ($child.Name -eq '.git') {
            continue
        }
        if (-not $allowed.Contains($child.Name)) {
            $Violations.Items.Add("docs entry not allowlisted: $($child.Name)")
        }
    }
}

function New-FixtureTree {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Entries
    )

    foreach ($entry in $Entries) {
        $parts = @($Root) + @($entry -split '/')
        $path = [System.IO.Path]::Combine([string[]]$parts)
        $parent = Split-Path -Parent $path
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        if (-not (Test-Path -LiteralPath $path)) {
            New-Item -ItemType File -Path $path -Force | Out-Null
        }
    }
}

function Assert-SameViolations {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Actual,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Expected
    )

    $left = @($Actual | Sort-Object)
    $right = @($Expected | Sort-Object)
    $same = $left.Count -eq $right.Count
    if ($same) {
        for ($index = 0; $index -lt $left.Count; $index++) {
            if ($left[$index] -cne $right[$index]) {
                $same = $false
                break
            }
        }
    }
    if (-not $same) {
        throw "$Name. Expected [$($right -join ' | ')] but got [$($left -join ' | ')]."
    }
}

function Invoke-FixtureCase {
    param([Parameter(Mandatory = $true)]$Case)

    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('docs-allow-' + [guid]::NewGuid().ToString('n'))
    $allowlistFile = Join-Path ([System.IO.Path]::GetTempPath()) ('docs-allow-list-' + [guid]::NewGuid().ToString('n') + '.txt')
    $docsRoot = Join-Path $root 'docs'
    New-Item -ItemType Directory -Path $docsRoot | Out-Null
    try {
        $allowlistText = $Case.Allowlist
        if (-not $allowlistText.EndsWith("`n")) {
            $allowlistText += "`n"
        }
        [System.IO.File]::WriteAllText($allowlistFile, $allowlistText)
        New-FixtureTree -Root $docsRoot -Entries @($Case.Entries)
        $names = [string[]](Read-DocsAllowlist -Path $allowlistFile)
        $found = [DocsAllowlistViolationBag]::new()
        Add-DocsAllowlistViolations -DocsRoot $docsRoot -Allowlist $names -Violations $found
        $expected = @()
        if ($Case.Contains('Expect') -and $null -ne $Case.Expect) {
            $expected = if ($Case.Expect -is [string]) { @($Case.Expect) } else { @($Case.Expect) }
        }
        Assert-SameViolations -Name $Case.Name -Actual @($found.Items) -Expected $expected
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $allowlistFile -Force -ErrorAction SilentlyContinue
    }
}

function Assert-AllowlistRejected {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $path = Join-Path ([System.IO.Path]::GetTempPath()) ('docs-allow-bad-' + [guid]::NewGuid().ToString('n') + '.txt')
    try {
        [System.IO.File]::WriteAllText($path, $Content)
        $threw = $false
        $message = ''
        try {
            Read-DocsAllowlist -Path $path | Out-Null
        }
        catch {
            $threw = $true
            $message = $_.Exception.Message
        }
        if (-not $threw -or $message -notlike $Pattern) {
            throw "Expected allowlist error like '$Pattern' but got threw=$threw message=$message"
        }
    }
    finally {
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-DocsAllowlistSelfTest {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AllowlistPath
    )

    $listed = "environments.md`napi"
    $cases = @(
        @{ Name = 'allowlisted docs tree passes'; Allowlist = $listed; Entries = @('environments.md', 'api/tools.md') }
        @{
            Name = 'extra docs file fails'
            Allowlist = $listed
            Entries = @('environments.md', 'automerge-a.md')
            Expect = 'docs entry not allowlisted: automerge-a.md'
        }
        @{
            Name = 'extra docs directory fails'
            Allowlist = $listed
            Entries = @('environments.md', 'factory-worker-trials/trial.md')
            Expect = 'docs entry not allowlisted: factory-worker-trials'
        }
        @{ Name = 'fewer docs entries pass'; Allowlist = $listed; Entries = @('environments.md') }
        @{
            Name = 'docs name case must match'
            Allowlist = 'environments.md'
            Entries = @('Environments.md')
            Expect = 'docs entry not allowlisted: Environments.md'
        }
        @{ Name = 'inline allowlist comment is not part of the name'; Allowlist = 'environments.md # keep'; Entries = @('environments.md') }
    )

    foreach ($case in $cases) {
        Invoke-FixtureCase $case
    }

    Assert-AllowlistRejected -Content "environments.md`nenvironments.md`n" -Pattern 'Duplicate allowlist entry*'
    Assert-AllowlistRejected -Content "api/tools.md`n" -Pattern 'Allowlist entry must be a single docs top-level name*'

    $docsRoot = Join-Path $RepoRoot 'docs'
    $live = [DocsAllowlistViolationBag]::new()
    Add-DocsAllowlistViolations -DocsRoot $docsRoot -Allowlist ([string[]](Read-DocsAllowlist -Path $AllowlistPath)) -Violations $live
    if ($live.Items.Count -ne 0) {
        throw "Repository docs/ failed the allowlist gate:`n$($live.Items -join "`n")"
    }
}

if ($SelfTest) {
    Invoke-DocsAllowlistSelfTest -RepoRoot $RepoRoot -AllowlistPath $AllowlistPath
    Write-Output 'Docs allowlist self-test passed.'
    exit 0
}

$docsRoot = Join-Path $RepoRoot 'docs'
$liveViolations = [DocsAllowlistViolationBag]::new()
Add-DocsAllowlistViolations -DocsRoot $docsRoot -Allowlist ([string[]](Read-DocsAllowlist -Path $AllowlistPath)) -Violations $liveViolations
if ($liveViolations.Items.Count -gt 0) {
    foreach ($violation in $liveViolations.Items) {
        [Console]::Error.WriteLine($violation)
    }
    exit 1
}

Write-Output 'Docs allowlist passed.'
exit 0
