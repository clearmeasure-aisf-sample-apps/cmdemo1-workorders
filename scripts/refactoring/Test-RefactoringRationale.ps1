<#
.SYNOPSIS
    Checks a pull request against docs/refactoring-oversized-code.md.

.DESCRIPTION
    Deterministic checks, no AI review:
    1. Ratchet: no guardrail baseline (src/UnitTests/BuildGates/Refactoring/Baselines/*.txt) and no oversized
       baseline gains a line between BaseRef and HeadRef.
    2. When the pull request changes a file on the oversized baseline, its body has a "Refactoring rationale"
       section: a table with one row per move (Smell | Refactoring | New abstraction | Why). Each smell and each
       refactoring is a row of the playbook's Smells and Preferred moves tables (or the move starts with "Other:"),
       each new abstraction appears in the added code, and the reason has at least 8 words.
    3. In such a pull request every type added to production code has a fixture named <Type>Tests or
       <Type><Behavior>Tests.

    The oversized baseline is the size gate's (#1536) committed list: any src/...cs or src/...razor path in the file at
    -OversizedBaselinePath. Without that file, the oversized files are the production files over 300 lines and the
    test files over 500 lines at BaseRef.

.EXAMPLE
    pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -PrBodyPath pr-body.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BaseRef,
    [Parameter(Mandatory)] [string] $PrBodyPath,
    [string] $HeadRef = 'HEAD',
    [string] $RepoRoot = '',
    [string] $OversizedBaselinePath = '',
    [string] $PlaybookPath = 'docs/refactoring-oversized-code.md'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$GuardrailBaselineFolder = 'src/UnitTests/BuildGates/Refactoring/Baselines'
$TestProjects = @('src/UnitTests', 'src/IntegrationTests', 'src/AcceptanceTests')
$ProductionFileLimit = 300
$TestFileLimit = 500
$MinimumReasonWords = 8
$TopLevelType = '(?m)^(?:(?:public|internal|file|sealed|abstract|static|partial|readonly|ref|unsafe)\s+)*(?:class|struct|record(?:\s+(?:class|struct))?)\s+(?<name>[A-Za-z_]\w*)'

if (-not $RepoRoot) { $RepoRoot = (& git rev-parse --show-toplevel).Trim() }
$script:Failures = [System.Collections.Generic.List[string]]::new()

function Invoke-Git([string[]] $Arguments) {
    $output = & git -C $RepoRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE." }
    return @($output)
}

function Find-InGit([string[]] $Arguments) {
    $output = & git -C $RepoRoot grep @Arguments 2>$null
    if ($LASTEXITCODE -gt 1) { throw "git grep $($Arguments -join ' ') failed with exit code $LASTEXITCODE." }
    return @($output)
}

function Test-GitPath([string] $Ref, [string] $Path) {
    & git -C $RepoRoot cat-file -e "${Ref}:$Path" 2>$null
    return $LASTEXITCODE -eq 0
}

function Get-GitFile([string] $Ref, [string] $Path) {
    if (-not (Test-GitPath $Ref $Path)) { return @() }
    return Invoke-Git @('show', "${Ref}:$Path")
}

function Add-Failure([string] $Message) {
    $script:Failures.Add($Message)
    $prefix = if ($env:GITHUB_ACTIONS -eq 'true') { '::error::' } else { 'ERROR: ' }
    Write-Host "$prefix$Message"
}

function Test-IsTestPath([string] $Path) {
    foreach ($project in $TestProjects) { if ($Path.StartsWith("$project/")) { return $true } }
    return $false
}

function Test-IsHandWrittenSource([string] $Path) {
    return $Path -match '^src/.+\.(cs|razor)$' -and $Path -notmatch '/(bin|obj|Generated)/' -and $Path -notmatch '\.g\.cs$'
}

function Get-BaselineEntries([string] $Ref, [string] $Path) {
    return @(Get-GitFile $Ref $Path | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.StartsWith('#') })
}

function Test-Ratchet {
    $baselines = @(Invoke-Git @('ls-tree', '-r', '--name-only', $HeadRef, '--', $GuardrailBaselineFolder))
    if ($OversizedBaselinePath) { $baselines += $OversizedBaselinePath }
    foreach ($baseline in $baselines | Where-Object { $_ } | Sort-Object -Unique) {
        $before = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($entry in Get-BaselineEntries $BaseRef $baseline) { [void]$before.Add($entry) }
        $added = @(Get-BaselineEntries $HeadRef $baseline | Where-Object { -not $before.Contains($_) })
        if ($added.Count -gt 0 -and (Test-GitPath $BaseRef $baseline)) {
            Add-Failure "Baseline $baseline gained $($added.Count) line(s); baselines only shrink. Fix the code instead of baselining it: $($added -join '; ')"
        }
    }
}

function Get-OversizedFiles {
    if ($OversizedBaselinePath -and (Test-GitPath $BaseRef $OversizedBaselinePath)) {
        $text = (Get-GitFile $BaseRef $OversizedBaselinePath) -join "`n"
        return @([regex]::Matches($text, 'src/[^\s"''|,;:]+?\.(?:cs|razor)\b') | ForEach-Object { $_.Value } | Sort-Object -Unique)
    }

    return @(Find-InGit @('-c', '', $BaseRef, '--', 'src') | ForEach-Object {
        $parts = $_.Substring($BaseRef.Length + 1) -split ':'
        $path = $parts[0]
        $lines = [int]$parts[-1]
        $limit = if (Test-IsTestPath $path) { $TestFileLimit } else { $ProductionFileLimit }
        if ((Test-IsHandWrittenSource $path) -and $lines -gt $limit) { $path }
    })
}

function Get-PlaybookNames([string[]] $Playbook, [string] $Heading) {
    $start = [Array]::IndexOf($Playbook, $Heading)
    if ($start -lt 0) { throw "Heading '$Heading' not found in $PlaybookPath." }
    $rows = [System.Collections.Generic.List[string]]::new()
    for ($i = $start + 1; $i -lt $Playbook.Count; $i++) {
        if ($Playbook[$i].StartsWith('|')) { $rows.Add($Playbook[$i]) }
        elseif ($rows.Count -gt 0) { break }
    }
    return @($rows | Select-Object -Skip 2 | ForEach-Object {
        $name = ($_.Trim('|') -split '\|')[0]
        $name -split '[()]' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    })
}

function Get-RationaleRows([string[]] $Body) {
    $text = ($Body -join "`n") -replace '(?s)<!--.*?-->', ''
    $lines = $text -split "`n"
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*#{2,6}\s*Refactoring rationale\s*$') { $start = $i; break }
    }
    if ($start -lt 0) { return $null }

    $rows = [System.Collections.Generic.List[object]]::new()
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if ($line -match '^#{1,2}\s') { break }
        if (-not $line.StartsWith('|')) { continue }
        $cells = [string[]]@($line.Trim('|') -split '\|' | ForEach-Object { $_.Trim() })
        if ($cells[0] -match '^(Smell|:?-+:?)$' -or ($cells -join '') -eq '') { continue }
        $rows.Add($cells)
    }
    return , $rows
}

function Test-Rationale([string[]] $Touched) {
    $playbook = @(Get-Content -LiteralPath (Join-Path $RepoRoot $PlaybookPath))
    $smells = Get-PlaybookNames $playbook '### Smells'
    $moves = Get-PlaybookNames $playbook '## Preferred moves'
    $rows = Get-RationaleRows @(Get-Content -LiteralPath $PrBodyPath)
    $touchedList = $Touched -join ', '

    if ($null -eq $rows -or $rows.Count -eq 0) {
        Add-Failure "This pull request changes oversized-baseline file(s) ($touchedList) but its body has no filled-in '## Refactoring rationale' table (Smell | Refactoring | New abstraction | Why). See $PlaybookPath."
        return
    }

    $addedCode = (Invoke-Git @('diff', '-U0', '--no-renames', $BaseRef, $HeadRef, '--', 'src') |
        Where-Object { $_.StartsWith('+') -and -not $_.StartsWith('+++') }) -join "`n"

    foreach ($cells in $rows) {
        $row = "| $($cells -join ' | ') |"
        if ($cells.Count -lt 4 -or @($cells[0..3] | Where-Object { -not $_ }).Count -gt 0) {
            Add-Failure "Rationale row needs four filled cells (Smell | Refactoring | New abstraction | Why): $row"
            continue
        }

        foreach ($smell in $cells[0] -split ',' | ForEach-Object { $_.Trim() }) {
            if ($smells -notcontains $smell) {
                Add-Failure "Unknown smell '$smell' in: $row. Use a name from the Smells table of $PlaybookPath."
            }
        }

        $move = $cells[1]
        if ($move -match '^Other:\s*(?<named>\S.*)$') {
            Write-Host "Other move '$($Matches.named)' accepted with its written reason."
        }
        elseif ($moves -notcontains $move) {
            Add-Failure "Unknown refactoring '$move' in: $row. Use a Preferred moves name from $PlaybookPath, or 'Other: <move>' with the reason."
        }

        foreach ($abstraction in $cells[2] -split '[,\s]+' | ForEach-Object { $_.Trim('`', ' ') } | Where-Object { $_ }) {
            if ($abstraction -notmatch '^[A-Za-z_]\w*$' -or $addedCode -notmatch "\b$([regex]::Escape($abstraction))\b") {
                Add-Failure "New abstraction '$abstraction' does not appear in the code this pull request adds: $row"
            }
        }

        $reasonWords = @($cells[3] -split '\s+' | Where-Object { $_ }).Count
        if ($reasonWords -lt $MinimumReasonWords) {
            Add-Failure "The reason has $reasonWords word(s); write at least ${MinimumReasonWords}: $row"
        }
    }
}

function Get-TypeNames([string[]] $Lines) {
    return @([regex]::Matches(($Lines -join "`n"), $TopLevelType) | ForEach-Object { $_.Groups['name'].Value })
}

function Test-ExtractedTypesHaveFixtures([string[]] $Changed) {
    $fixtures = @(Find-InGit (@('-h', '-o', '-E', 'class [A-Za-z_][A-Za-z0-9_]*Tests\b', $HeadRef, '--') + $TestProjects) |
        ForEach-Object { $_.Substring('class '.Length) })

    foreach ($path in $Changed | Where-Object { (Test-IsHandWrittenSource $_) -and -not (Test-IsTestPath $_) }) {
        if (-not (Test-GitPath $HeadRef $path)) { continue }
        if ($path.EndsWith('.razor')) {
            $added = if (Test-GitPath $BaseRef $path) { @() } else { @([IO.Path]::GetFileNameWithoutExtension($path)) }
        }
        else {
            $before = Get-TypeNames (Get-GitFile $BaseRef $path)
            $added = @(Get-TypeNames (Get-GitFile $HeadRef $path) | Where-Object { $before -notcontains $_ })
        }

        foreach ($type in $added) {
            $fixture = $fixtures | Where-Object { $_.StartsWith($type) } | Select-Object -First 1
            if (-not $fixture) {
                Add-Failure "Extracted type $type ($path) has no test fixture. Add ${type}Tests (or ${type}<Behavior>Tests)."
            }
        }
    }
}

Test-Ratchet

$changed = @(Invoke-Git @('diff', '--name-only', '--no-renames', $BaseRef, $HeadRef))
$oversized = Get-OversizedFiles
$touched = @($changed | Where-Object { $oversized -contains $_ })

if ($touched.Count -eq 0) {
    Write-Host 'No oversized-baseline file changed; no Refactoring rationale required.'
}
else {
    Write-Host "Oversized-baseline file(s) changed: $($touched -join ', ')"
    Test-Rationale $touched
    Test-ExtractedTypesHaveFixtures $changed
}

if ($script:Failures.Count -gt 0) {
    Write-Host "$($script:Failures.Count) refactoring check(s) failed. Read $PlaybookPath."
    exit 1
}

Write-Host 'Refactoring checks passed.'
exit 0
