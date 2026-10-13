#!/usr/bin/env pwsh
#Requires -Version 7.4

<#
.SYNOPSIS
    Takes the private build's word into the build's facts: the object "build" (command, ranBy, run) that says which
    one command built this artifact and that the integration build ran it.

.DESCRIPTION
    The kit's Release of an application runs this after it has stamped the build's facts (the kit owns this file in
    the repository, like the Release itself). The fleet reads the object from the deployed application: "Private
    build" is met when every production application's facts name the command and say the integration build ran it
    (the kit's decisions 0028 and 0046).

    Where the word comes from. The private build is one command of the repository that a developer runs before a
    commit and that the integration build runs too. When that command runs in the integration build it writes
    private-build.json, { "command": "<the command as a developer types it>" }, and the Build workflow keeps the
    file as the artifact "private-build". The Release downloads that artifact of the Build run it releases and
    hands the file to this script (-Word). Nothing of the Release decides what the command is.

    What this script checks before it writes, and what it says when it does not (a SKIP line, exit code 0, the facts
    as they were, so the application reads as behind and nothing fails):
      - the Build run kept no word: the build has no private build that says so;
      - the word is no JSON object with a "command" of 1 to 200 printable characters on one line;
      - the Build workflow of this commit (-Workflow, .github/workflows/build.yml) does not run that command: the
        integration build would then have a definition of the build of its own.
    What it writes: "build": { "command", "ranBy": "integration", "run": <-Run, the address of the Build run> }.
    "ranBy" is "integration" because the word is an artifact of the integration build's run; the run's address is
    the Release's own knowledge, not the word's.

    The facts are a file (-FactsPath) or the entry build-facts.json at the root of a zip (-Package, an App Service
    package). A facts file that is absent or no JSON object is left alone, with a SKIP line.

.EXAMPLE
    pwsh -NoProfile -File .github/private-build/add-private-build.ps1 -FactsPath built/build-facts.json -Word word/private-build.json -Run https://github.com/example-org/app/actions/runs/1
#>
[CmdletBinding()]
param(
    [string] $FactsPath = '',
    [string] $Package = '',
    # The file the Build run kept as artifact "private-build"; absent when it kept none.
    [string] $Word = '',
    [Parameter(Mandatory)] [string] $Run,
    [string] $Workflow = '.github/workflows/build.yml'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Read-PrivateBuildCommand {
    # The command the word names, or why it names none.
    param([AllowNull()] [string] $Text)
    $word = try { "$Text" | ConvertFrom-Json -AsHashtable -NoEnumerate } catch { $null }
    if ($word -isnot [Collections.IDictionary]) { return @{ Command = ''; Why = 'private-build.json is no JSON object' } }
    $command = if ($word['command'] -is [string]) { ([string] $word['command']).Trim() } else { '' }
    if ($command.Length -lt 1 -or $command.Length -gt 200 -or $command -cnotmatch '^[\x20-\x7E]+$') {
        return @{ Command = ''; Why = 'private-build.json names no "command" of 1 to 200 printable characters on one line' }
    }
    return @{ Command = $command; Why = '' }
}

function Test-WorkflowRunsCommand {
    # True when a "run:" of the Build workflow holds the command, on its own line or in a block: the integration
    # build runs what a developer runs. Spaces are compared as one.
    param([AllowNull()] [string] $WorkflowText, [Parameter(Mandatory)] [string] $Command)
    $plain = { param([string] $Line) ($Line -replace '\s+', ' ').Trim() }
    $wanted = & $plain $Command
    foreach ($line in @("$WorkflowText" -split '\r?\n')) {
        $text = & $plain ($line -replace '^\s*(-\s*)?run:\s*', '' -replace '^\s*[|>][+-]?\s*$', '')
        if ($text -and $line -notmatch '^\s*#' -and $text.Contains($wanted)) { return $true }
    }
    return $false
}

function Add-BuildObject {
    # The facts with the object, as text; $null when the text is no JSON object.
    param([AllowNull()] [string] $FactsText, [Parameter(Mandatory)] [string] $Command, [Parameter(Mandatory)] [string] $Run)
    $facts = try { "$FactsText" | ConvertFrom-Json -AsHashtable -NoEnumerate } catch { $null }
    if ($facts -isnot [Collections.IDictionary]) { return $null }
    $facts['build'] = [ordered] @{ command = $Command; ranBy = 'integration'; run = $Run }
    return ($facts | ConvertTo-Json -Depth 20) + "`n"
}

if (-not $FactsPath -and -not $Package) { throw 'Name the facts: -FactsPath <file>, or -Package <zip with build-facts.json at its root>.' }
if (-not $Word -or -not (Test-Path -LiteralPath $Word -PathType Leaf)) {
    Write-Host 'SKIP private build: the Build run kept no artifact "private-build", so the facts name no command (the application reads as behind on "Private build")'
    exit 0
}
$named = Read-PrivateBuildCommand -Text (Get-Content -LiteralPath $Word -Raw)
if (-not $named.Command) { Write-Host "SKIP private build: $($named.Why)"; exit 0 }
$workflowText = if (Test-Path -LiteralPath $Workflow -PathType Leaf) { Get-Content -LiteralPath $Workflow -Raw } else { '' }
if (-not (Test-WorkflowRunsCommand -WorkflowText $workflowText -Command $named.Command)) {
    Write-Host "SKIP private build: $Workflow does not run `"$($named.Command)`", the command the word names: the integration build must run the private build's command"
    exit 0
}

if ($Package) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::Open((Resolve-Path -LiteralPath $Package).Path, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = $zip.GetEntry('build-facts.json')
        if (-not $entry) { Write-Host "SKIP private build: $Package has no build-facts.json at its root"; exit 0 }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $before = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $after = Add-BuildObject -FactsText $before -Command $named.Command -Run $Run
        if ($null -eq $after) { Write-Host "SKIP private build: build-facts.json of $Package is no JSON object"; exit 0 }
        $entry.Delete()
        $writer = [IO.StreamWriter]::new($zip.CreateEntry('build-facts.json').Open())
        try { $writer.Write($after) } finally { $writer.Dispose() }
    }
    finally { $zip.Dispose() }
}
else {
    if (-not (Test-Path -LiteralPath $FactsPath -PathType Leaf)) { Write-Host "SKIP private build: $FactsPath is not there (the repository writes no build facts)"; exit 0 }
    $after = Add-BuildObject -FactsText (Get-Content -LiteralPath $FactsPath -Raw) -Command $named.Command -Run $Run
    if ($null -eq $after) { Write-Host "SKIP private build: $FactsPath is no JSON object"; exit 0 }
    [IO.File]::WriteAllText((Resolve-Path -LiteralPath $FactsPath).Path, $after)
}
Write-Host "PASS private build: the facts say `"$($named.Command)`" built this artifact, run by the integration build ($Run)"
exit 0
