<#
.SYNOPSIS
  Lists page-level acceptance fixtures mapped to changed Razor files.

.DESCRIPTION
  Reads src/AcceptanceTests/test-to-page-map.json and returns the distinct test
  class names whose pages include any of the given Razor paths (PR environments
  plan item 23 / #1670). A .razor.cs code-behind path matches its .razor page.
  Shared components and shell Razor files expand to their host pages via the map's
  components[] entries. Paths in ignoredRazor (_Imports) select nothing.

  Accepts paths via -RazorPaths, pipeline (ValueFromPipeline), or stdin when
  invoked with pwsh -File (reads redirected Console.In). Prefer:

    git diff --name-only origin/master...HEAD -- '*.razor' '*.razor.cs' |
      pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.ps1

  Use -PathsFile with a real file path (not "-"). When UI Razor paths are given and
  none select a fixture, the script writes an error and exits 1 unless -AllowEmpty.

  Used by item 16 to pick changed-UI videos for the sticky PR comment.
  Does not edit .github/workflows/ or loosen gates.

.PARAMETER RazorPaths
  Checkout-relative Razor paths (or .razor.cs). Accepts pipeline input (one path per line).

.PARAMETER PathsFile
  Optional file with one path per line (blank lines and # comments ignored). Do not use "-".

.PARAMETER AsFilter
  When set, print a FullyQualifiedName~...|... filter for dotnet test instead of
  one class name per line.

.PARAMETER AllowEmpty
  Exit 0 even when UI Razor paths select no fixtures (default: exit 1).

.EXAMPLE
  pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.ps1 -RazorPaths src/UI.Shared/Pages/Login.razor

.EXAMPLE
  git diff --name-only origin/master...HEAD -- '*.razor' '*.razor.cs' |
    pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.ps1

.EXAMPLE
  pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.ps1 -PathsFile changed-razor.txt
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromPipeline = $true)]
    [string[]]$RazorPaths = @(),

    [string]$PathsFile = "",

    [switch]$AsFilter,

    [switch]$AllowEmpty
)

begin {
    $ErrorActionPreference = "Stop"
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
    $mapPath = Join-Path $repoRoot "src/AcceptanceTests/test-to-page-map.json"
    $paths = [System.Collections.Generic.List[string]]::new()

    function Get-CanonicalRazorPath {
        param([string]$Path)
        $normalized = ($Path.Trim() -replace '\\', '/')
        if ($normalized -match '\.razor\.cs$') {
            return $normalized.Substring(0, $normalized.Length - 3)
        }
        return $normalized
    }

    function Test-IsUiRazorPath {
        param([string]$Path)
        $n = Get-CanonicalRazorPath -Path $Path
        return ($n -like 'src/UI.Shared/*' -or $n -like 'src/UI/Client/*') -and ($n -like '*.razor')
    }
}

process {
    foreach ($path in @($RazorPaths)) {
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $paths.Add($path)
        }
    }
}

end {
    if (-not [string]::IsNullOrWhiteSpace($PathsFile)) {
        if ($PathsFile -eq '-') {
            throw "PathsFile '-' is not supported. Pipe paths into the script, or pass -PathsFile with a real file path."
        }
        foreach ($line in Get-Content -LiteralPath $PathsFile) {
            $trimmed = "$line".Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
                continue
            }
            $paths.Add($trimmed)
        }
    }

    # pwsh -File does not bind stdin to ValueFromPipeline; read redirected Console.In.
    if ($paths.Count -eq 0 -and [Console]::IsInputRedirected) {
        foreach ($line in [Console]::In.ReadToEnd() -split "`r?`n") {
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
    $ignored = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @($map.ignoredRazor)) {
        [void]$ignored.Add((Get-CanonicalRazorPath -Path $path))
    }

    $hosts = @{}
    foreach ($entry in @($map.components)) {
        $hosts[(Get-CanonicalRazorPath -Path $entry.path)] = @(
            $entry.pages | ForEach-Object { Get-CanonicalRazorPath -Path $_ }
        )
    }

    $wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $uiInputs = [System.Collections.Generic.List[string]]::new()
    foreach ($path in $paths) {
        $canonical = Get-CanonicalRazorPath -Path $path
        if ([string]::IsNullOrWhiteSpace($canonical)) {
            continue
        }
        if (Test-IsUiRazorPath -Path $canonical -and -not $ignored.Contains($canonical)) {
            $uiInputs.Add($canonical)
        }
        if ($ignored.Contains($canonical)) {
            continue
        }
        if ($hosts.ContainsKey($canonical)) {
            foreach ($page in $hosts[$canonical]) {
                [void]$wanted.Add($page)
            }
        }
        else {
            [void]$wanted.Add($canonical)
        }
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

    $distinct = @($selected | Sort-Object -Unique)
    if ($uiInputs.Count -gt 0 -and $distinct.Count -eq 0 -and -not $AllowEmpty) {
        $joined = ($uiInputs | Select-Object -Unique) -join ', '
        Write-Error "No acceptance fixtures mapped for UI Razor path(s): $joined. Update test-to-page-map.json (tests or components)."
        exit 1
    }

    if ($AsFilter) {
        if ($distinct.Count -eq 0) {
            return
        }
        ($distinct | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
    }
    else {
        $distinct
    }
}
