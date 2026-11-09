#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when the repo root has an entry outside the allowlist, or a stray artifact exists.

.DESCRIPTION
  Root names must be listed in root-allowlist.txt. The list is the ratchet: a name that
  is not listed fails the build, and a listed name that is absent still passes. Remove
  the row when that entry is deleted so it cannot come back.

  Stray artifacts (*.bak and the other scratch names below) fail anywhere outside
  compiler and tool output. .git is ignored. build/, private-build.json, build-facts.json,
  build_version.txt, crap-metrics/, and ChurchBulletin.db* are build output and are not root entries.

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

$script:StrayArtifactExtensions = @('.bak', '.orig', '.rej', '.tmp', '.tmp_proj', '.swp', '.swo')
$script:StrayArtifactFileNames = @('nul', '.DS_Store', 'Thumbs.db', 'desktop.ini')
$script:GeneratedRootNames = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@(
        'build',
        'private-build.json',
        'build-facts.json',
        'build_version.txt',
        'crap-metrics',
        'ChurchBulletin.db',
        'ChurchBulletin.db-shm',
        'ChurchBulletin.db-wal'),
    [StringComparer]::OrdinalIgnoreCase)
$script:SkippedDirectoryNames = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('.git', 'bin', 'obj', 'node_modules', 'TestResults', '.vs', 'artifacts', 'crap-metrics'),
    [StringComparer]::OrdinalIgnoreCase)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}
if ([string]::IsNullOrWhiteSpace($AllowlistPath)) {
    $AllowlistPath = Join-Path $PSScriptRoot 'root-allowlist.txt'
}

function Test-GeneratedRootName {
    param([Parameter(Mandatory = $true)][string]$Name)
    return $script:GeneratedRootNames.Contains($Name)
}

function Test-StrayArtifactName {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name.EndsWith('~', [StringComparison]::Ordinal)) {
        return $true
    }

    foreach ($exact in $script:StrayArtifactFileNames) {
        if ([string]::Equals($Name, $exact, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($extension in $script:StrayArtifactExtensions) {
        if ($Name.EndsWith($extension, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Read-RepoRootAllowlist {
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
            throw "Allowlist entry must be a single root name (line ${lineNumber}): $trimmed"
        }
        if (Test-GeneratedRootName $trimmed) {
            throw "Allowlist must not contain generated root name (line ${lineNumber}): $trimmed"
        }
        if (-not $seen.Add($trimmed)) {
            throw "Duplicate allowlist entry (line ${lineNumber}): $trimmed"
        }
        $names.Add($trimmed)
    }

    Write-Output -NoEnumerate $names.ToArray()
}

class RepoRootViolationBag {
    [System.Collections.Generic.List[string]]$Items = [System.Collections.Generic.List[string]]::new()
}

function Add-RepoRootViolations {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Allowlist,
        [Parameter(Mandatory = $true)][RepoRootViolationBag]$Violations
    )

    $root = (Resolve-Path -LiteralPath $RepoRoot).Path
    $allowed = [System.Collections.Generic.HashSet[string]]::new($Allowlist, [StringComparer]::Ordinal)

    foreach ($child in @(Get-ChildItem -LiteralPath $root -Force)) {
        if ($child.Name -eq '.git' -or (Test-GeneratedRootName $child.Name)) {
            continue
        }
        if (-not $allowed.Contains($child.Name)) {
            $Violations.Items.Add("root entry not allowlisted: $($child.Name)")
        }
    }

    $pending = [System.Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($root)
    while ($pending.Count -gt 0) {
        $directory = $pending.Dequeue()
        $atRoot = [string]::Equals($directory, $root, [StringComparison]::Ordinal)
        foreach ($child in @(Get-ChildItem -LiteralPath $directory -Force)) {
            $reparse = [System.IO.FileAttributes]::ReparsePoint
            if (($child.Attributes -band $reparse) -ne 0) {
                continue
            }
            $skipDirectory = $child.PSIsContainer -and (
                $script:SkippedDirectoryNames.Contains($child.Name) -or
                ($atRoot -and (Test-GeneratedRootName $child.Name)))
            if ($skipDirectory) {
                continue
            }
            if (Test-StrayArtifactName $child.Name) {
                $relative = [System.IO.Path]::GetRelativePath($root, $child.FullName).Replace('\', '/')
                $Violations.Items.Add("stray artifact: $relative")
            }
            if ($child.PSIsContainer) {
                $pending.Enqueue($child.FullName)
            }
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

function Get-ExpectedViolations {
    param([Parameter(Mandatory = $true)]$Case)

    if (-not $Case.Contains('Expect') -or $null -eq $Case.Expect) {
        Write-Output -NoEnumerate @()
        return
    }
    $raw = $Case.Expect
    if ($raw -is [string]) {
        Write-Output -NoEnumerate @($raw)
        return
    }
    Write-Output -NoEnumerate @($raw)
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

    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('repo-root-' + [guid]::NewGuid().ToString('n'))
    $allowlistFile = Join-Path ([System.IO.Path]::GetTempPath()) ('repo-root-allow-' + [guid]::NewGuid().ToString('n') + '.txt')
    New-Item -ItemType Directory -Path $root | Out-Null
    try {
        $allowlistText = $Case.Allowlist
        if (-not $allowlistText.EndsWith("`n")) {
            $allowlistText += "`n"
        }
        [System.IO.File]::WriteAllText($allowlistFile, $allowlistText)
        New-FixtureTree -Root $root -Entries @($Case.Entries)
        $names = [string[]](Read-RepoRootAllowlist -Path $allowlistFile)
        $found = [RepoRootViolationBag]::new()
        Add-RepoRootViolations -RepoRoot $root -Allowlist $names -Violations $found
        Assert-SameViolations -Name $Case.Name -Actual @($found.Items) -Expected (Get-ExpectedViolations $Case)
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

    $path = Join-Path ([System.IO.Path]::GetTempPath()) ('repo-root-bad-' + [guid]::NewGuid().ToString('n') + '.txt')
    try {
        [System.IO.File]::WriteAllText($path, $Content)
        $threw = $false
        $message = ''
        try {
            Read-RepoRootAllowlist -Path $path | Out-Null
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

function Invoke-RepoRootAllowlistSelfTest {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AllowlistPath
    )

    $listed = "README.md`nsrc"
    $cases = @(
        @{ Name = 'allowlisted tree passes'; Allowlist = $listed; Entries = @('README.md', 'src/Program.cs') }
        @{
            Name = 'extra root file fails'
            Allowlist = $listed
            Entries = @('README.md', 'src/Program.cs', 'NOTES.txt')
            Expect = 'root entry not allowlisted: NOTES.txt'
        }
        @{
            Name = 'extra root directory fails'
            Allowlist = $listed
            Entries = @('README.md', 'TicTacToeGame/readme.txt')
            Expect = 'root entry not allowlisted: TicTacToeGame'
        }
        @{ Name = 'fewer root entries pass'; Allowlist = $listed; Entries = @('README.md') }
        @{
            Name = 'bak outside build output fails'
            Allowlist = $listed
            Entries = @('README.md', 'src/Notes.cs.bak')
            Expect = 'stray artifact: src/Notes.cs.bak'
        }
        @{ Name = 'bak inside bin is build output'; Allowlist = $listed; Entries = @('README.md', 'src/bin/generated.bak') }
        @{ Name = 'tmp inside obj is build output'; Allowlist = $listed; Entries = @('README.md', 'src/obj/out.tmp') }
        @{
            Name = 'generated root output is ignored'
            Allowlist = 'README.md'
            Entries = @(
                'README.md', 'build/noise.tmp', 'private-build.json', 'build-facts.json', 'build_version.txt',
                'crap-metrics/report.json', 'ChurchBulletin.db', 'ChurchBulletin.db-shm', 'ChurchBulletin.db-wal')
        }
        @{
            Name = 'tmp outside build output fails'
            Allowlist = $listed
            Entries = @('README.md', 'src/nested/err.tmp')
            Expect = 'stray artifact: src/nested/err.tmp'
        }
        @{
            Name = 'Thumbs.db at the root fails'
            Allowlist = $listed
            Entries = @('README.md', 'Thumbs.db')
            Expect = @('root entry not allowlisted: Thumbs.db', 'stray artifact: Thumbs.db')
        }
        @{
            Name = 'tilde backup fails'
            Allowlist = $listed
            Entries = @('README.md', 'src/Editor.cs~')
            Expect = 'stray artifact: src/Editor.cs~'
        }
        @{ Name = 'git directory is ignored'; Allowlist = 'README.md'; Entries = @('README.md', '.git/HEAD') }
        @{
            Name = 'root name case must match'
            Allowlist = 'README.md'
            Entries = @('readme.md')
            Expect = 'root entry not allowlisted: readme.md'
        }
        @{ Name = 'inline allowlist comment is not part of the name'; Allowlist = 'README.md # owner decision pending'; Entries = @('README.md') }
        @{
            Name = 'orig file fails'
            Allowlist = $listed
            Entries = @('README.md', 'src/Program.cs.orig')
            Expect = 'stray artifact: src/Program.cs.orig'
        }
        @{
            Name = 'desktop.ini fails'
            Allowlist = $listed
            Entries = @('README.md', 'docs/desktop.ini')
            Expect = @('root entry not allowlisted: docs', 'stray artifact: docs/desktop.ini')
        }
    )

    foreach ($case in $cases) {
        Invoke-FixtureCase $case
    }

    # nul is a Windows device name, so a fixture file of that name never appears in the directory listing.
    if (-not (Test-StrayArtifactName 'nul') -or -not (Test-StrayArtifactName 'NUL')) {
        throw 'nul must be a stray artifact name on every platform.'
    }
    if (Test-StrayArtifactName 'README.md') {
        throw 'A normal root name must not be a stray artifact.'
    }

    Assert-AllowlistRejected -Content "README.md`nREADME.md`n" -Pattern 'Duplicate allowlist entry*'
    Assert-AllowlistRejected -Content "src/Foo`n" -Pattern 'Allowlist entry must be a single root name*'
    Assert-AllowlistRejected -Content "build`n" -Pattern 'Allowlist must not contain generated root name*'

    $live = [RepoRootViolationBag]::new()
    Add-RepoRootViolations -RepoRoot $RepoRoot -Allowlist ([string[]](Read-RepoRootAllowlist -Path $AllowlistPath)) -Violations $live
    if ($live.Items.Count -ne 0) {
        throw "Repository root failed the allowlist gate:`n$($live.Items -join "`n")"
    }
}

if ($SelfTest) {
    Invoke-RepoRootAllowlistSelfTest -RepoRoot $RepoRoot -AllowlistPath $AllowlistPath
    Write-Output 'Repo root allowlist self-test passed.'
    exit 0
}

$liveViolations = [RepoRootViolationBag]::new()
Add-RepoRootViolations -RepoRoot $RepoRoot -Allowlist ([string[]](Read-RepoRootAllowlist -Path $AllowlistPath)) -Violations $liveViolations
if ($liveViolations.Items.Count -gt 0) {
    foreach ($violation in $liveViolations.Items) {
        [Console]::Error.WriteLine($violation)
    }
    exit 1
}

Write-Output 'Repo root allowlist passed.'
exit 0
