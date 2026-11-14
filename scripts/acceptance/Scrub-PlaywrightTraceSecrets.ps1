<#
.SYNOPSIS
  Scrubs secret headers (and known secret substrings) from Playwright trace zip files.

.DESCRIPTION
  Playwright traces are zip archives of JSONL (trace.trace, trace.network, *.json).
  Request/response header arrays are rewritten in place. Does not loosen gates.
  Used by item 16 / #1671 before uploading traces to private Azure storage.

.PARAMETER Path
  A .zip trace file, or a directory to scrub recursively (*.zip).

.PARAMETER Destination
  Optional output path for a single zip. Default: overwrite the source zip in place
  (writes via a temp file). Ignored when -Path is a directory.

.PARAMETER SelfTest
  Runs Scrub-PlaywrightTraceSecrets.Tests.ps1.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Path = '',

    [string]$Destination = '',

    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

if ($SelfTest) {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Scrub-PlaywrightTraceSecrets.Tests.ps1')
    exit $LASTEXITCODE
}

. (Join-Path $PSScriptRoot 'SecretHeaders.ps1')

function Scrub-HeaderArrayJson {
    param([string]$JsonLine)
    # Match "name":"Header-Name" ... "value":"..." within header objects (order may vary).
    $pattern = '"name"\s*:\s*"([^"\\]*(?:\\.[^"\\]*)*)"\s*,\s*"value"\s*:\s*"([^"\\]*(?:\\.[^"\\]*)*)"'
    $altPattern = '"value"\s*:\s*"([^"\\]*(?:\\.[^"\\]*)*)"\s*,\s*"name"\s*:\s*"([^"\\]*(?:\\.[^"\\]*)*)"'

    $scrubbed = [regex]::Replace($JsonLine, $pattern, {
        param($m)
        $headerName = $m.Groups[1].Value
        $headerValue = $m.Groups[2].Value
        if (Test-PrAcceptanceSecretHeaderName -Name $headerName) {
            return ('"name":"{0}","value":"{1}"' -f $headerName, (Get-PrAcceptanceRedactedValue))
        }
        return $m.Value
    })

    $scrubbed = [regex]::Replace($scrubbed, $altPattern, {
        param($m)
        $headerValue = $m.Groups[1].Value
        $headerName = $m.Groups[2].Value
        if (Test-PrAcceptanceSecretHeaderName -Name $headerName) {
            return ('"value":"{0}","name":"{1}"' -f (Get-PrAcceptanceRedactedValue), $headerName)
        }
        return $m.Value
    })

    return (Protect-PrAcceptanceSecretText -Text $scrubbed)
}

function Test-IsTextTraceEntry {
    param([string]$EntryName)
    $n = $EntryName.ToLowerInvariant()
    return ($n.EndsWith('.trace') -or $n.EndsWith('.network') -or $n.EndsWith('.json') -or $n.EndsWith('.txt') -or $n.EndsWith('.md'))
}

function Invoke-ScrubTraceZip {
    param(
        [Parameter(Mandatory = $true)][string]$SourceZip,
        [Parameter(Mandatory = $true)][string]$OutputZip
    )

    if (-not (Test-Path -LiteralPath $SourceZip)) {
        throw "Trace zip not found: $SourceZip"
    }

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("scrub-trace-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $SourceZip -DestinationPath $tempRoot -Force
        Get-ChildItem -LiteralPath $tempRoot -Recurse -File | ForEach-Object {
            if (-not (Test-IsTextTraceEntry -EntryName $_.Name)) {
                return
            }
            $text = [System.IO.File]::ReadAllText($_.FullName)
            $lines = $text -split "`r?`n", -1
            $out = New-Object System.Collections.Generic.List[string]
            foreach ($line in $lines) {
                if ([string]::IsNullOrEmpty($line)) {
                    [void]$out.Add($line)
                    continue
                }
                [void]$out.Add((Scrub-HeaderArrayJson -JsonLine $line))
            }
            $joined = [string]::Join("`n", $out)
            # Preserve final newline if the source had one.
            if ($text.EndsWith("`n") -or $text.EndsWith("`r`n")) {
                if (-not $joined.EndsWith("`n")) { $joined += "`n" }
            }
            [System.IO.File]::WriteAllText($_.FullName, $joined)
        }

        $outDir = Split-Path -Parent $OutputZip
        if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path -LiteralPath $outDir)) {
            New-Item -ItemType Directory -Force -Path $outDir | Out-Null
        }
        if (Test-Path -LiteralPath $OutputZip) {
            Remove-Item -LiteralPath $OutputZip -Force
        }
        Compress-Archive -Path (Join-Path $tempRoot '*') -DestinationPath $OutputZip -Force
    }
    finally {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ([string]::IsNullOrWhiteSpace($Path)) {
    throw 'Path is required (trace .zip or directory), or pass -SelfTest.'
}

$resolved = Resolve-Path -LiteralPath $Path
if (Test-Path -LiteralPath $resolved -PathType Container) {
    Get-ChildItem -LiteralPath $resolved -Filter '*.zip' -Recurse -File | ForEach-Object {
        Write-Host "Scrubbing $($_.FullName)"
        Invoke-ScrubTraceZip -SourceZip $_.FullName -OutputZip $_.FullName
    }
}
else {
    $dest = if ([string]::IsNullOrWhiteSpace($Destination)) { $resolved.Path } else { $Destination }
    if ($dest -eq $resolved.Path) {
        $tempOut = Join-Path ([System.IO.Path]::GetTempPath()) ("scrubbed-" + [guid]::NewGuid().ToString('N') + '.zip')
        Invoke-ScrubTraceZip -SourceZip $resolved.Path -OutputZip $tempOut
        Move-Item -LiteralPath $tempOut -Destination $resolved.Path -Force
    }
    else {
        Invoke-ScrubTraceZip -SourceZip $resolved.Path -OutputZip $dest
    }
    Write-Host "Scrubbed trace -> $dest"
}
