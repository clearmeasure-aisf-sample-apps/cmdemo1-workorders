#Requires -Version 7.0
# Self-tests for the suppression budget. No repo scan.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SuppressionBudget.ps1')

function New-BudgetFixture {
    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('suppression-budget-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $root | Out-Null
    return $root
}

function Set-BudgetFixtureFile {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $path = Join-Path $Root $Name
    $parent = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }

    $text = $Content.Replace("`r`n", "`n")
    [System.IO.File]::WriteAllText($path, $text)
}

function Assert-Budget {
    param(
        [Parameter(Mandatory = $true)]$Report,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ([bool]$Report.Passed -ne $Passed) {
        throw "$Message Passed=$($Report.Passed) count=$($Report.SuppressionCount) missing=$(@($Report.MissingReasons).Count) markers=$(@($Report.TaskMarkers).Count)"
    }
}

function Test-MissingReasonFails {
    $root = New-BudgetFixture
    try {
        Set-BudgetFixtureFile $root 'Sample.cs' "// ReSharper disable once UnusedMember.Global`npublic class Sample { }`n"
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 5
        Assert-Budget $report $false 'A suppression without a reason must fail under the baseline.'
        if (@($report.MissingReasons).Count -ne 1) { throw 'A missing reason must be reported.' }
        Write-Output 'missing reason fails'
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-IncreaseFails {
    $root = New-BudgetFixture
    try {
        $line = "// ReSharper disable once UnusedMember.Global -- fixture keeps the member`n"
        Set-BudgetFixtureFile $root 'Sample.cs' ($line + $line)
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1
        Assert-Budget $report $false 'A count above the baseline must fail.'
        if ($report.SuppressionCount -le $report.MaxSuppressions) { throw 'The increase fixture must exceed the baseline.' }
        Write-Output 'increase fails'
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-DecreasePasses {
    $root = New-BudgetFixture
    try {
        $line = "// ReSharper disable once UnusedMember.Global -- fixture keeps the member`npublic class Sample { }`n"
        Set-BudgetFixtureFile $root 'Sample.cs' $line
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 4
        Assert-Budget $report $true 'A count below the baseline must pass.'
        if ($report.SuppressionCount -ge $report.MaxSuppressions) { throw 'The decrease fixture must sit under the baseline.' }
        Write-Output 'decrease passes'
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-EqualBaselinePasses {
    $root = New-BudgetFixture
    try {
        Set-BudgetFixtureFile $root 'Sample.cs' "// ReSharper disable once UnusedMember.Global -- fixture keeps the member"
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1
        Assert-Budget $report $true 'A count equal to the baseline must pass.'
        if ($report.SuppressionCount -ne 1) { throw 'A one-line file must count as one suppression.' }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-TaskMarkerFails {
    $root = New-BudgetFixture
    try {
        foreach ($token in @('TODO', 'FIXME', 'HACK')) {
            Set-BudgetFixtureFile $root 'Sample.cs' "public class Sample { } // $token left in source`n"
            $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 0
            Assert-Budget $report $false "A $token marker must fail."
            if (@($report.TaskMarkers).Count -ne 1 -or $report.TaskMarkers[0].Token -ne $token) {
                throw "The $token marker must be reported."
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Invoke-SuppressMessageCase {
    $root = New-BudgetFixture
    try {
        $bare = "[System.Diagnostics.CodeAnalysis.SuppressMessage(`"Usage`", `"CA1000`")]`npublic class Sample { }`n"
        Set-BudgetFixtureFile $root 'Bare.cs' $bare
        $missing = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 5
        Assert-Budget $missing $false 'SuppressMessage without Justification must fail.'

        $justified = "[System.Diagnostics.CodeAnalysis.SuppressMessage(`n    `"Usage`",`n    `"CA1000`",`n    Justification = `"fixture reason`")]`npublic class Sample { }`n"
        Set-BudgetFixtureFile $root 'Bare.cs' $justified
        $present = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1
        Assert-Budget $present $true 'SuppressMessage with Justification must pass.'
        if ($present.SuppressionCount -ne 1) { throw 'A multiline SuppressMessage must count once.' }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-ReasonShapes {
    $root = New-BudgetFixture
    try {
        $preceding = "// The member is part of the fixture contract.`n// ReSharper disable once UnusedMember.Global`n"
        Set-BudgetFixtureFile $root 'Preceding.cs' $preceding
        Assert-Budget (Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1) $true 'A preceding comment must count as a reason.'

        $banner = "// </auto-generated>`n#pragma warning disable CS1591`n"
        Set-BudgetFixtureFile $root 'Preceding.cs' $banner
        Assert-Budget (Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 5) $false 'A generated-file banner must not count as a reason.'

        $pragma = "#pragma warning disable CS1591 // generated fixture`n"
        Set-BudgetFixtureFile $root 'Preceding.cs' $pragma
        Assert-Budget (Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1) $true 'A trailing pragma comment must count as a reason.'

        $restore = "// ReSharper disable once UnusedMember.Global -- fixture keeps the member`n// ReSharper restore UnusedMember.Global`n#pragma warning disable CS0219 // fixture`n#pragma warning restore CS0219`n"
        Set-BudgetFixtureFile $root 'Preceding.cs' $restore
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 2
        Assert-Budget $report $true 'Restore directives must not count.'
        if ($report.SuppressionCount -ne 2) { throw 'Only disable directives count.' }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-SkippedAndCommentedMentions {
    $root = New-BudgetFixture
    try {
        Set-BudgetFixtureFile $root 'bin/Hidden.cs' "// ReSharper disable once UnusedMember.Global`n"
        Set-BudgetFixtureFile $root 'Note.cs' "// Do not add SuppressMessage( here.`npublic class Sample { }`n"
        Set-BudgetFixtureFile $root 'View.razor' "@code {`n    // ReSharper disable once UnusedMember.Local -- fixture keeps the member`n}`n"
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1
        Assert-Budget $report $true 'bin output and comment mentions must not count; Razor disables must.'
        if ($report.SuppressionCount -ne 1) { throw 'Only the Razor disable should count.' }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

function Test-BomPrefixStillCounts {
    $root = New-BudgetFixture
    try {
        $path = Join-Path $root 'Sample.cs'
        $payload = "// ReSharper disable once UnusedMember.Global -- fixture keeps the member`n"
        $bytes = [System.Text.Encoding]::UTF8.GetPreamble() + [System.Text.Encoding]::UTF8.GetBytes($payload)
        [System.IO.File]::WriteAllBytes($path, $bytes)
        $report = Get-SuppressionBudgetReport -SourceRoot $root -MaxSuppressions 1
        Assert-Budget $report $true 'A BOM-prefixed disable must still count and keep its reason.'
        if ($report.SuppressionCount -ne 1) { throw 'A BOM-prefixed disable must count once.' }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

Test-MissingReasonFails
Test-IncreaseFails
Test-DecreasePasses
Test-EqualBaselinePasses
Test-TaskMarkerFails
Invoke-SuppressMessageCase
Test-ReasonShapes
Test-SkippedAndCommentedMentions
Test-BomPrefixStillCounts

Write-Output 'suppression budget self-test passed.'
exit 0
