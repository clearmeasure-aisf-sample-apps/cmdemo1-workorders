#Requires -Version 7.0
# Scans App DefaultLayout, @layout, and layout markup (NavMenu) to map shell/layout
# Razor paths to routable pages. Dot-sourced by Get-TestsForChangedRazor.ps1.
function Get-RazorShellPageExpansion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $appRazor = 'src/UI/Client/App.razor'
    $navMenuRazor = 'src/UI.Shared/NavMenu.razor'
    $pageRoots = @('src/UI.Shared/Pages', 'src/UI/Client/Pages')
    $layoutRoots = @('src/UI.Shared', 'src/UI/Client')

    function Get-Canonical {
        param([string]$Path)
        return ($Path.Trim() -replace '\\', '/')
    }

    function Test-TagUsed {
        param([string]$Text, [string]$Stem)
        return [regex]::IsMatch($Text, "<$([regex]::Escape($Stem))([\s/>]|$)")
    }

    function Test-IsEmptyLayout {
        param([string]$Name)
        return $Name -eq 'EmptyLayout'
    }

    function Resolve-LayoutPath {
        param([string]$LayoutName)
        if ([string]::IsNullOrWhiteSpace($LayoutName) -or (Test-IsEmptyLayout -Name $LayoutName)) {
            return $null
        }
        $fileName = "$LayoutName.razor"
        foreach ($root in $layoutRoots) {
            $absolute = Join-Path $RepoRoot $root
            if (-not (Test-Path -LiteralPath $absolute)) { continue }
            $hit = Get-ChildItem -LiteralPath $absolute -Filter $fileName -Recurse -File -ErrorAction SilentlyContinue |
                Select-Object -First 1
            if ($null -ne $hit) {
                return (Get-Canonical -Path ($hit.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')))
            }
        }
        return $null
    }

    function Get-DefaultLayoutName {
        $absolute = Join-Path $RepoRoot $appRazor
        if (-not (Test-Path -LiteralPath $absolute)) { return $null }
        $text = Get-Content -LiteralPath $absolute -Raw
        $match = [regex]::Match($text, 'DefaultLayout\s*=\s*"@typeof\((?<name>[A-Za-z_][A-Za-z0-9_]*)\)"')
        if ($match.Success) { return $match.Groups['name'].Value }
        return $null
    }

    function Get-PageLayoutName {
        param([string]$PageRelative)
        $absolute = Join-Path $RepoRoot $PageRelative
        $text = Get-Content -LiteralPath $absolute -Raw
        $match = [regex]::Match($text, '(?m)^\s*@layout\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b')
        if ($match.Success) { return $match.Groups['name'].Value }
        return $null
    }

    function Test-IsRoutablePage {
        param([string]$PageRelative)
        $absolute = Join-Path $RepoRoot $PageRelative
        $text = Get-Content -LiteralPath $absolute -Raw
        if ([regex]::IsMatch($text, '(?m)^\s*@page\b')) { return $true }
        $codeBehind = "$absolute.cs"
        if (-not (Test-Path -LiteralPath $codeBehind)) { return $false }
        $cs = Get-Content -LiteralPath $codeBehind -Raw
        return [regex]::IsMatch($cs, '\[Route\s*\(')
    }

    $defaultLayoutName = Get-DefaultLayoutName
    $defaultLayoutPath = Resolve-LayoutPath -LayoutName $defaultLayoutName
    $pagesByLayout = @{}

    foreach ($root in $pageRoots) {
        $absolute = Join-Path $RepoRoot $root
        if (-not (Test-Path -LiteralPath $absolute)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $absolute -Filter '*.razor' -File) {
            $relative = Get-Canonical -Path ($file.FullName.Substring($RepoRoot.Length).TrimStart('\', '/'))
            if (-not (Test-IsRoutablePage -PageRelative $relative)) { continue }
            $layoutName = Get-PageLayoutName -PageRelative $relative
            if ([string]::IsNullOrWhiteSpace($layoutName)) {
                $layoutName = $defaultLayoutName
            }
            if ([string]::IsNullOrWhiteSpace($layoutName) -or (Test-IsEmptyLayout -Name $layoutName)) {
                continue
            }
            $layoutPath = Resolve-LayoutPath -LayoutName $layoutName
            if ([string]::IsNullOrWhiteSpace($layoutPath)) { continue }
            if (-not $pagesByLayout.ContainsKey($layoutPath)) {
                $pagesByLayout[$layoutPath] = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            }
            [void]$pagesByLayout[$layoutPath].Add($relative)
        }
    }

    $expansion = @{}
    foreach ($layoutPath in $pagesByLayout.Keys) {
        $expansion[$layoutPath] = @($pagesByLayout[$layoutPath] | Sort-Object)
    }

    $navPages = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($layoutPath in $pagesByLayout.Keys) {
        $layoutAbsolute = Join-Path $RepoRoot $layoutPath
        if (-not (Test-Path -LiteralPath $layoutAbsolute)) { continue }
        $layoutText = Get-Content -LiteralPath $layoutAbsolute -Raw
        if (-not (Test-TagUsed -Text $layoutText -Stem 'NavMenu')) { continue }
        foreach ($page in $pagesByLayout[$layoutPath]) {
            [void]$navPages.Add($page)
        }
    }
    if ($navPages.Count -gt 0) {
        $expansion[$navMenuRazor] = @($navPages | Sort-Object)
    }

    if (-not [string]::IsNullOrWhiteSpace($defaultLayoutPath) -and $pagesByLayout.ContainsKey($defaultLayoutPath)) {
        $expansion[$appRazor] = @($pagesByLayout[$defaultLayoutPath] | Sort-Object)
    }

    return $expansion
}

function Add-ShellPageExpansion {
    param(
        [System.Collections.Generic.HashSet[string]]$Wanted,
        [hashtable]$Expansion
    )
    if ($null -eq $Expansion -or $Wanted.Count -eq 0) {
        return
    }
    foreach ($path in @($Wanted)) {
        if (-not $Expansion.ContainsKey($path)) {
            continue
        }
        foreach ($page in @($Expansion[$path])) {
            [void]$Wanted.Add($page)
        }
    }
}

