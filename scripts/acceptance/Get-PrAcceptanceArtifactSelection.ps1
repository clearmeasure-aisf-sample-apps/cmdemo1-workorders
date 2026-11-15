<#
.SYNOPSIS
  Selects Playwright videos/traces for failed tests and changed-UI fixtures.

.DESCRIPTION
  Item 16 / #1671: failed tests and tests whose UI changed (via Get-TestsForChangedRazor.ps1
  + test-to-page-map.json) each contribute a video (and matching trace when present).

.PARAMETER TrxDirectory
  Directory of TRX results (searched recursively). Failed test full names are read from outcomes.

.PARAMETER TraceDirectory
  Directory of playwright-traces *.zip files named Class.Method.zip.

.PARAMETER VideoDirectory
  Directory of playwright-videos (per-test folders or *.webm named Class.Method.webm).

.PARAMETER ChangedRazorPaths
  Optional Razor paths; when set, mapped fixtures are included even when passed.

.PARAMETER ChangedRazorPathsFile
  Optional file of Razor paths (one per line).

.PARAMETER SelfTest
  Runs Get-PrAcceptanceArtifactSelection.Tests.ps1.
#>
[CmdletBinding()]
param(
    [string]$TrxDirectory = '',
    [string]$TraceDirectory = '',
    [string]$VideoDirectory = '',
    [string[]]$ChangedRazorPaths = @(),
    [string]$ChangedRazorPathsFile = '',
    [switch]$AsJson,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

if ($SelfTest) {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Get-PrAcceptanceArtifactSelection.Tests.ps1')
    exit $LASTEXITCODE
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Get-FailedTestNamesFromTrx {
    param([string]$Directory)
    if ([string]::IsNullOrWhiteSpace($Directory) -or -not (Test-Path -LiteralPath $Directory)) {
        return @()
    }
    $names = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    Get-ChildItem -LiteralPath $Directory -Filter '*.trx' -Recurse -File | ForEach-Object {
        [xml]$xml = Get-Content -LiteralPath $_.FullName -Raw
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
        foreach ($result in $xml.SelectNodes('//t:UnitTestResult', $ns)) {
            $outcome = [string]$result.GetAttribute('outcome')
            if ($outcome -ne 'Failed') { continue }
            $testName = [string]$result.GetAttribute('testName')
            if (-not [string]::IsNullOrWhiteSpace($testName)) {
                [void]$names.Add($testName)
            }
        }
    }
    return @($names)
}

function Get-ChangedUiClassNames {
    param([string[]]$RazorPaths, [string]$PathsFile)
    $args = @()
    if (-not [string]::IsNullOrWhiteSpace($PathsFile)) {
        $args += @('-PathsFile', $PathsFile)
    }
    $script = Join-Path $PSScriptRoot 'Get-TestsForChangedRazor.ps1'
    if ($RazorPaths.Count -gt 0) {
        $joined = ($RazorPaths | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join "`n"
        if ([string]::IsNullOrWhiteSpace($joined) -and [string]::IsNullOrWhiteSpace($PathsFile)) {
            return @()
        }
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = 'pwsh'
        $psi.ArgumentList.Add('-NoProfile')
        $psi.ArgumentList.Add('-File')
        $psi.ArgumentList.Add($script)
        $psi.ArgumentList.Add('-AllowEmpty')
        foreach ($a in $args) { $psi.ArgumentList.Add($a) }
        $psi.WorkingDirectory = $repoRoot
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $proc = [System.Diagnostics.Process]::Start($psi)
        $proc.StandardInput.Write($joined)
        $proc.StandardInput.Close()
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()
        if ($proc.ExitCode -ne 0) {
            throw "Get-TestsForChangedRazor.ps1 failed ($($proc.ExitCode)): $stderr"
        }
        return @(
            $stdout -split "`r?`n" |
                ForEach-Object { $_.Trim() } |
                Where-Object { $_ }
        )
    }
    if (-not [string]::IsNullOrWhiteSpace($PathsFile)) {
        $out = & pwsh -NoProfile -File $script -PathsFile $PathsFile -AllowEmpty
        return @($out | ForEach-Object { "$_".Trim() } | Where-Object { $_ })
    }
    return @()
}

function Resolve-ArtifactFiles {
    param(
        [string]$TestName,
        [string]$TraceDirectory,
        [string]$VideoDirectory
    )
    # TRX testName is often "Namespace.Class.Method" or "Namespace.Class.Method(args)".
    $simple = ($TestName -replace '\(.*\)$', '')
    $leaf = ($simple -split '\.')[-2..-1] -join '.'
    if ($leaf -eq '.' -or [string]::IsNullOrWhiteSpace($leaf)) {
        $leaf = $simple
    }

    $trace = $null
    if (-not [string]::IsNullOrWhiteSpace($TraceDirectory) -and (Test-Path -LiteralPath $TraceDirectory)) {
        $candidates = @(
            Get-ChildItem -LiteralPath $TraceDirectory -Filter '*.zip' -Recurse -File |
                Where-Object {
                    $_.BaseName -eq $simple -or
                    $_.BaseName -eq $leaf -or
                    $_.BaseName.EndsWith($leaf, [StringComparison]::OrdinalIgnoreCase) -or
                    $simple.EndsWith($_.BaseName, [StringComparison]::OrdinalIgnoreCase)
                }
        )
        if ($candidates.Count -gt 0) {
            $trace = $candidates[0].FullName
        }
    }

    $video = $null
    if (-not [string]::IsNullOrWhiteSpace($VideoDirectory) -and (Test-Path -LiteralPath $VideoDirectory)) {
        $candidates = @(
            Get-ChildItem -LiteralPath $VideoDirectory -Filter '*.webm' -Recurse -File |
                Where-Object {
                    $_.BaseName -eq $simple -or
                    $_.BaseName -eq $leaf -or
                    $_.Directory.Name -eq $simple -or
                    $_.Directory.Name -eq $leaf -or
                    $_.BaseName.EndsWith($leaf, [StringComparison]::OrdinalIgnoreCase)
                }
        )
        if ($candidates.Count -gt 0) {
            $video = $candidates[0].FullName
        }
    }

    return [pscustomobject]@{
        TestName  = $simple
        TracePath = $trace
        VideoPath = $video
    }
}

$failed = @(Get-FailedTestNamesFromTrx -Directory $TrxDirectory)
$changedClasses = @(Get-ChangedUiClassNames -RazorPaths $ChangedRazorPaths -PathsFile $ChangedRazorPathsFile)

$selectedNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($name in $failed) { [void]$selectedNames.Add($name) }

# Changed-UI: include any on-disk trace/video whose class matches.
$changedClassSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($c in $changedClasses) { [void]$changedClassSet.Add($c) }

if ($changedClassSet.Count -gt 0) {
    $scanRoots = @()
    if (-not [string]::IsNullOrWhiteSpace($TraceDirectory) -and (Test-Path -LiteralPath $TraceDirectory)) {
        $scanRoots += $TraceDirectory
    }
    if (-not [string]::IsNullOrWhiteSpace($VideoDirectory) -and (Test-Path -LiteralPath $VideoDirectory)) {
        $scanRoots += $VideoDirectory
    }
    foreach ($root in $scanRoots) {
        Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
            foreach ($className in $changedClassSet) {
                $short = ($className -split '\.')[-1]
                if ($_.FullName.Contains($className, [StringComparison]::OrdinalIgnoreCase) -or
                    $_.BaseName.StartsWith($short + '.', [StringComparison]::OrdinalIgnoreCase) -or
                    $_.Directory.Name.StartsWith($short + '.', [StringComparison]::OrdinalIgnoreCase)) {
                    [void]$selectedNames.Add($_.BaseName)
                }
            }
        }
    }
    foreach ($className in $changedClassSet) {
        [void]$selectedNames.Add($className)
    }
}

$items = [System.Collections.Generic.List[object]]::new()
foreach ($name in ($selectedNames | Sort-Object)) {
    $resolved = Resolve-ArtifactFiles -TestName $name -TraceDirectory $TraceDirectory -VideoDirectory $VideoDirectory
    $reason = @()
    if ($failed -contains $name -or ($failed | Where-Object { $_ -like "*$name*" -or $name -like "*$_*" })) {
        $reason += 'failed'
    }
    foreach ($className in $changedClassSet) {
        $short = ($className -split '\.')[-1]
        if ($name.Contains($className, [StringComparison]::OrdinalIgnoreCase) -or
            $name.StartsWith($short + '.', [StringComparison]::OrdinalIgnoreCase) -or
            $name -eq $className) {
            $reason += 'changed-ui'
            break
        }
    }
    if ($reason.Count -eq 0 -and $changedClassSet.Count -gt 0) {
        $reason += 'changed-ui'
    }
    if ($null -eq $resolved.VideoPath -and $null -eq $resolved.TracePath -and $reason -notcontains 'failed') {
        continue
    }
    $items.Add([pscustomobject]@{
        TestName  = $resolved.TestName
        Reason    = ($reason | Select-Object -Unique) -join ','
        TracePath = $resolved.TracePath
        VideoPath = $resolved.VideoPath
    })
}

if ($AsJson) {
    $items | ConvertTo-Json -Depth 5
}
else {
    $items
}
