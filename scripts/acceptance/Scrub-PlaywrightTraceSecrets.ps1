<#
.SYNOPSIS
  Scrubs secret headers (and known secret substrings) from Playwright trace zip files.

.DESCRIPTION
  Playwright traces are zip archives of JSONL (trace.trace, trace.network) and JSON resources.
  Entries are parsed as JSON. Secret header values are redacted in every shape:
  name/value object arrays, header maps, [name,value] pair arrays, cookies, and split-line
  JSON. Live secrets from env (see SecretHeaders.ps1) are redacted without logging.
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
. (Join-Path $PSScriptRoot 'PlaywrightTraceScrub.ps1')

if ([string]::IsNullOrWhiteSpace($Path)) {
    throw 'Path is required (trace .zip or directory), or pass -SelfTest.'
}

$resolved = Resolve-Path -LiteralPath $Path
if (Test-Path -LiteralPath $resolved -PathType Container) {
    Get-ChildItem -LiteralPath $resolved -Filter '*.zip' -Recurse -File | ForEach-Object {
        Write-Host "Scrubbing $($_.FullName)"
        Invoke-ScrubTraceZip -SourceZip $_.FullName -OutputZip $_.FullName
        Assert-TraceZipScrubbed -ZipPath $_.FullName
    }
}
else {
    $dest = if ([string]::IsNullOrWhiteSpace($Destination)) { $resolved.Path } else { $Destination }
    if ($dest -eq $resolved.Path) {
        $tempOut = Join-Path ([System.IO.Path]::GetTempPath()) ("scrubbed-" + [guid]::NewGuid().ToString('N') + '.zip')
        Invoke-ScrubTraceZip -SourceZip $resolved.Path -OutputZip $tempOut
        Assert-TraceZipScrubbed -ZipPath $tempOut
        Move-Item -LiteralPath $tempOut -Destination $resolved.Path -Force
    }
    else {
        Invoke-ScrubTraceZip -SourceZip $resolved.Path -OutputZip $dest
        Assert-TraceZipScrubbed -ZipPath $dest
    }
    Write-Host "Scrubbed trace -> $dest"
}
