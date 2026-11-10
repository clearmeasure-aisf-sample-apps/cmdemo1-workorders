<#
.SYNOPSIS
    Checks a pull request against docs/refactoring-oversized-code.md.

.DESCRIPTION
    Deterministic checks, no AI review:
    1. Ratchet: no guardrail baseline (src/UnitTests/BuildGates/Refactoring/Baselines/*.txt) and no size baseline
       gains a line between BaseRef and HeadRef.
    2. When the pull request changes a file on the size baseline (src/UnitTests/BuildGates/size-nesting-baseline.txt:
       every src/...cs or src/...razor path on a file, method or parameters line at BaseRef), its body has a
       "## Refactoring plan" section with:
       - "### Refactoring rationale": a table with one row per move (Smell | Refactoring | New abstraction | Why).
         Each smell and each refactoring is a row of the playbook's Smells and Preferred moves tables (or the move
         starts with "Other:"), each new abstraction appears in the added code, and the reason has at least 8 words.
       - Mermaid diagrams: ```mermaid blocks of kind classDiagram or flowchart, each labelled by the line above its
         fence starting with "Before" or "After", as many Before as After. Every type the pull request adds to
         production code (and every new .razor component) is named in the diagram text.
       - "### Line counts": the table this script writes with -WriteLineCountTable, unchanged. It lists every
         size-baselined file the pull request changes and every file whose size-baseline lines change.
    3. In such a pull request every type added to production code has a fixture named <Type>Tests or
       <Type><Behavior>Tests.

.EXAMPLE
    pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -PrBodyPath pr-body.md

.EXAMPLE
    pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -WriteLineCountTable
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BaseRef,
    [string] $PrBodyPath = '',
    [string] $HeadRef = 'HEAD',
    [string] $RepoRoot = '',
    [string] $OversizedBaselinePath = 'src/UnitTests/BuildGates/size-nesting-baseline.txt',
    [string] $PlaybookPath = 'docs/refactoring-oversized-code.md',
    [switch] $WriteLineCountTable
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$GuardrailBaselineFolder = 'src/UnitTests/BuildGates/Refactoring/Baselines'
$TestProjects = @('src/UnitTests', 'src/IntegrationTests', 'src/AcceptanceTests')
$MinimumReasonWords = 8
$BaselinedPath = 'src/[^\s"''|,;:]+\.(?:cs|razor)(?=$|[\s"''|,;:])'
$DiagramKinds = @('classDiagram', 'flowchart')
$LineCountHeader = @(
    '| File | Lines before | Lines after | Size-gate entries before | Size-gate entries after |',
    '|------|-------------:|------------:|-------------------------:|------------------------:|'
)
$TopLevelType = '(?m)^(?:(?:public|internal|file|sealed|abstract|static|partial|readonly|ref|unsafe)\s+)*(?:class|struct|record(?:\s+(?:class|struct))?)\s+(?<name>[A-Za-z_]\w*)'

if (-not $RepoRoot) { $RepoRoot = (& git rev-parse --show-toplevel).Trim() }
if (-not $WriteLineCountTable -and -not $PrBodyPath) { throw 'Pass -PrBodyPath, or -WriteLineCountTable to print the line-count table.' }
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

function Get-BaselinedPath([string] $Entry) {
    $match = [regex]::Match($Entry, $BaselinedPath)
    if ($match.Success) { return $match.Value }
    return $null
}

function Get-OversizedFiles {
    return @(Get-BaselineEntries $BaseRef $OversizedBaselinePath | ForEach-Object { Get-BaselinedPath $_ } |
        Where-Object { $_ } | Sort-Object -Unique)
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
        $name = ($_.Trim('|') -split '\|')[0].Trim()
        $name
        $name -split '[()]' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    })
}

function Get-BodyLines {
    $text = (Get-Content -LiteralPath $PrBodyPath -Raw) -replace '(?s)<!--.*?-->', ''
    return @($text -split "\r?\n")
}

function Get-Section([string[]] $Lines, [string] $Heading, [int] $Level) {
    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match "^\s*#{$Level}\s*$([regex]::Escape($Heading))\s*$") { $start = $i; break }
    }
    if ($start -lt 0) { return $null }

    $section = [System.Collections.Generic.List[string]]::new()
    for ($i = $start + 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match "^\s*#{1,$Level}\s") { break }
        $section.Add($Lines[$i])
    }
    return , [string[]]$section
}

function Get-TableRows([string[]] $Lines) {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $Lines | ForEach-Object { $_.Trim() } | Where-Object { $_.StartsWith('|') }) {
        $cells = [string[]]@($line.Trim('|') -split '\|' | ForEach-Object { $_.Trim().Trim('`').Trim() })
        if ($cells[0] -match '^:?-+:?$' -or ($cells -join '') -eq '') { continue }
        $rows.Add($cells)
    }
    return , $rows
}

function Get-RationaleRows([string[]] $Plan) {
    $section = Get-Section $Plan 'Refactoring rationale' 3
    if ($null -eq $section) { return $null }
    return , @((Get-TableRows $section) | Where-Object { $_[0] -ne 'Smell' })
}

function Test-Rationale([string[]] $Plan, [string] $TouchedList) {
    $playbook = @(Get-Content -LiteralPath (Join-Path $RepoRoot $PlaybookPath))
    $smells = Get-PlaybookNames $playbook '### Smells'
    $moves = Get-PlaybookNames $playbook '## Preferred moves'
    $rows = Get-RationaleRows $Plan

    if ($null -eq $rows -or $rows.Count -eq 0) {
        Add-Failure "This pull request changes size-baselined file(s) ($TouchedList) but its Refactoring plan has no filled-in '### Refactoring rationale' table (Smell | Refactoring | New abstraction | Why). See $PlaybookPath."
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

function Get-AddedProductionTypes([string[]] $Changed) {
    foreach ($path in $Changed | Where-Object { (Test-IsHandWrittenSource $_) -and -not (Test-IsTestPath $_) }) {
        if (-not (Test-GitPath $HeadRef $path)) { continue }
        if ($path.EndsWith('.razor')) {
            $added = if (Test-GitPath $BaseRef $path) { @() } else { @([IO.Path]::GetFileNameWithoutExtension($path)) }
        }
        else {
            $before = Get-TypeNames (Get-GitFile $BaseRef $path)
            $added = @(Get-TypeNames (Get-GitFile $HeadRef $path) | Where-Object { $before -notcontains $_ })
        }

        foreach ($type in $added) { [pscustomobject]@{ Type = $type; Path = $path } }
    }
}

function Test-ExtractedTypesHaveFixtures([object[]] $AddedTypes) {
    $fixtures = @(Find-InGit (@('-h', '-o', '-E', 'class [A-Za-z_][A-Za-z0-9_]*Tests\b', $HeadRef, '--') + $TestProjects) |
        ForEach-Object { $_.Substring('class '.Length) })

    foreach ($added in $AddedTypes) {
        $fixture = $fixtures | Where-Object { $_.StartsWith($added.Type) } | Select-Object -First 1
        if (-not $fixture) {
            Add-Failure "Extracted type $($added.Type) ($($added.Path)) has no test fixture. Add $($added.Type)Tests (or $($added.Type)<Behavior>Tests)."
        }
    }
}

function Get-MermaidDiagrams([string[]] $Plan) {
    $label = ''
    for ($i = 0; $i -lt $Plan.Count; $i++) {
        $line = $Plan[$i].Trim()
        if ($line -notmatch '^```\s*mermaid\s*$') {
            if ($line) { $label = $line }
            continue
        }

        $diagram = [System.Collections.Generic.List[string]]::new()
        for ($i++; $i -lt $Plan.Count -and $Plan[$i].Trim() -notmatch '^```\s*$'; $i++) { $diagram.Add($Plan[$i]) }
        $statements = @($diagram | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.StartsWith('%%') })
        $kind = if ($statements.Count -gt 0) { ($statements[0] -split '\s+')[0] } else { '' }
        $side = if ($label -match '^[\W_]*(?<side>Before|After)\b') { $Matches.side } else { '' }
        [pscustomobject]@{ Label = $label; Side = $side; Kind = $kind; Text = $diagram -join "`n" }
        $label = ''
    }
}

function Test-Diagrams([string[]] $Plan, [object[]] $AddedTypes) {
    $diagrams = @(Get-MermaidDiagrams $Plan)
    if ($diagrams.Count -eq 0) {
        Add-Failure "The Refactoring plan has no mermaid diagram. Add a Before and an After diagram (``````mermaid, classDiagram or flowchart) for each concept cluster. See $PlaybookPath."
        return
    }

    foreach ($diagram in $diagrams) {
        if ($DiagramKinds -cnotcontains $diagram.Kind) {
            Add-Failure "Mermaid diagram '$($diagram.Label)' is a '$($diagram.Kind)'; use classDiagram or flowchart."
        }
        if (-not $diagram.Side) {
            Add-Failure "Mermaid diagram '$($diagram.Label)' is not labelled: the line above its fence starts with Before or After."
        }
    }

    $before = @($diagrams | Where-Object Side -eq 'Before').Count
    $after = @($diagrams | Where-Object Side -eq 'After').Count
    if ($before -ne $after) {
        Add-Failure "The Refactoring plan has $before Before and $after After diagram(s); give each concept cluster one of each."
    }

    $text = ($diagrams | ForEach-Object Text) -join "`n"
    foreach ($added in $AddedTypes) {
        if ($text -notmatch "\b$([regex]::Escape($added.Type))\b") {
            Add-Failure "New type $($added.Type) ($($added.Path)) does not appear in any Refactoring plan diagram."
        }
    }
}

function Get-EntriesByPath([string] $Ref) {
    $byPath = @{}
    foreach ($entry in Get-BaselineEntries $Ref $OversizedBaselinePath) {
        $path = Get-BaselinedPath $entry
        if (-not $path) { continue }
        if (-not $byPath.ContainsKey($path)) { $byPath[$path] = [System.Collections.Generic.List[string]]::new() }
        $byPath[$path].Add($entry)
    }
    return $byPath
}

function Get-LineCountRows([string[]] $Touched) {
    $before = Get-EntriesByPath $BaseRef
    $after = Get-EntriesByPath $HeadRef
    $changedEntries = @(@($before.Keys) + @($after.Keys) | Sort-Object -Unique | Where-Object {
        $was = if ($before.ContainsKey($_)) { @($before[$_] | Sort-Object) -join "`n" } else { '' }
        $now = if ($after.ContainsKey($_)) { @($after[$_] | Sort-Object) -join "`n" } else { '' }
        $was -cne $now
    })

    $files = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($file in @($Touched) + $changedEntries) { if ($file) { [void]$files.Add($file) } }
    foreach ($file in $files) {
        , [string[]]@(
            $file,
            [string]@(Get-GitFile $BaseRef $file).Count,
            [string]@(Get-GitFile $HeadRef $file).Count,
            [string]$(if ($before.ContainsKey($file)) { $before[$file].Count } else { 0 }),
            [string]$(if ($after.ContainsKey($file)) { $after[$file].Count } else { 0 }))
    }
}

function Format-LineCountTable([object[]] $Rows) {
    $LineCountHeader
    foreach ($cells in $Rows) { "| ``$($cells[0])`` | $($cells[1..4] -join ' | ') |" }
}

function Test-LineCounts([string[]] $Plan, [object[]] $Expected) {
    $expectedTable = (Format-LineCountTable $Expected) -join "`n"
    $section = Get-Section $Plan 'Line counts' 3
    if ($null -eq $section) {
        Add-Failure "The Refactoring plan has no '### Line counts' table. Paste this table, generated from the size-baseline diff (-WriteLineCountTable):`n$expectedTable"
        return
    }

    $actual = @((Get-TableRows $section) | Where-Object { $_[0] -ne 'File' } | ForEach-Object { $_ -join ' | ' })
    $wanted = @($Expected | ForEach-Object { $_ -join ' | ' })
    if (($actual -join "`n") -cne ($wanted -join "`n")) {
        Add-Failure "The '### Line counts' table is stale. Replace it with this table, generated from the size-baseline diff (-WriteLineCountTable):`n$expectedTable"
    }
}

$changed = @(Invoke-Git @('diff', '--name-only', '--no-renames', $BaseRef, $HeadRef))
$oversized = Get-OversizedFiles
$touched = @($changed | Where-Object { $oversized -contains $_ })

if ($WriteLineCountTable) {
    Format-LineCountTable @(Get-LineCountRows $touched)
    exit 0
}

Test-Ratchet

if ($touched.Count -eq 0) {
    Write-Host 'No size-baselined file changed; no Refactoring plan required.'
}
else {
    $touchedList = $touched -join ', '
    Write-Host "Size-baselined file(s) changed: $touchedList"
    $addedTypes = @(Get-AddedProductionTypes $changed)
    $plan = Get-Section (Get-BodyLines) 'Refactoring plan' 2
    if ($null -eq $plan) {
        Add-Failure "This pull request changes size-baselined file(s) ($touchedList) but its body has no '## Refactoring plan' section (rationale table, Before/After mermaid diagrams, line counts). See $PlaybookPath."
    }
    else {
        Test-Rationale $plan $touchedList
        Test-Diagrams $plan $addedTypes
        Test-LineCounts $plan @(Get-LineCountRows $touched)
    }
    Test-ExtractedTypesHaveFixtures $addedTypes
}

if ($script:Failures.Count -gt 0) {
    Write-Host "$($script:Failures.Count) refactoring check(s) failed. Read $PlaybookPath."
    exit 1
}

Write-Host 'Refactoring checks passed.'
exit 0
