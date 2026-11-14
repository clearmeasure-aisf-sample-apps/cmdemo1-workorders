<#
.SYNOPSIS
  Lists page-level acceptance fixtures mapped to changed Razor files.

.DESCRIPTION
  Reads src/AcceptanceTests/test-to-page-map.json and returns the distinct test
  class names whose pages include any of the given Razor paths (PR environments
  plan item 23 / #1670). A .razor.cs code-behind path matches its .razor page.

  Used by item 16 to pick changed-UI videos for the sticky PR comment.
  Does not edit .github/workflows/ or loosen gates.

.PARAMETER RazorPaths
  Checkout-relative Razor paths (or .razor.cs). May be repeated.

.PARAMETER PathsFile
  Optional file with one path per line (blank lines and # comments ignored).

.PARAMETER AsFilter
  When set, print a FullyQualifiedName~...|... filter for dotnet test instead of
  one class name per line.

.EXAMPLE
  pwsh scripts/acceptance/Get-TestsForChangedRazor.ps1 -RazorPaths src/UI.Shared/Pages/Login.razor

.EXAMPLE
  git diff --name-only origin/master...HEAD -- '*.razor' '*.razor.cs' |
    pwsh scripts/acceptance/Get-TestsForChangedRazor.ps1 -PathsFile -
#>
[CmdletBinding()]
param(
    [string[]]$RazorPaths = @(),
    [string]$PathsFile = "",
    [switch]$AsFilter
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$mapPath = Join-Path $repoRoot "src/AcceptanceTests/test-to-page-map.json"

function Get-CanonicalRazorPath {
    param([string]$Path)
    $normalized = ($Path.Trim() -replace '\\', '/')
    if ($normalized -match '\.razor\.cs$') {
        return $normalized.Substring(0, $normalized.Length - 3)
    }
    return $normalized
}

$paths = [System.Collections.Generic.List[string]]::new()
foreach ($path in $RazorPaths) {
    if (-not [string]::IsNullOrWhiteSpace($path)) {
        $paths.Add($path)
    }
}

if (-not [string]::IsNullOrWhiteSpace($PathsFile)) {
    $lines = if ($PathsFile -eq '-') {
        @($input)
    }
    else {
        Get-Content -LiteralPath $PathsFile
    }
    foreach ($line in $lines) {
        $trimmed = "$line".Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }
        $paths.Add($trimmed)
    }
}

if ($paths.Count -eq 0) {
    return
}

if (-not (Test-Path -LiteralPath $mapPath)) {
    throw "Missing map: $mapPath"
}

$map = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json
$wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($path in $paths) {
    [void]$wanted.Add((Get-CanonicalRazorPath -Path $path))
}

$selected = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $map.tests) {
    $hit = $false
    foreach ($page in $entry.pages) {
        if ($wanted.Contains((Get-CanonicalRazorPath -Path $page))) {
            $hit = $true
            break
        }
    }
    if ($hit -and -not [string]::IsNullOrWhiteSpace($entry.class)) {
        $selected.Add($entry.class)
    }
}

$distinct = $selected | Sort-Object -Unique
if ($AsFilter) {
    if ($distinct.Count -eq 0) {
        return
    }
    ($distinct | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
}
else {
    $distinct
}
