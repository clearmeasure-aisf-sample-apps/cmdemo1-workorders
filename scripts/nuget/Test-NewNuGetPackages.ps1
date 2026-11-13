#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when HeadRef adds a NuGet package ID that BaseRef does not have, unless the same change approves it.

.DESCRIPTION
  Package IDs are read from every tracked *.csproj, *.fsproj, *.vbproj, *.props and *.targets file
  (Directory.Packages.props and Directory.Build.* included): the Include of PackageVersion, PackageReference,
  GlobalPackageReference and PackageDownload items, and MSBuild project SDKs that carry a version
  (Sdk="Name/1.2.3" or <Sdk Name="Name" Version="1.2.3" />), reported as "sdk:Name". IDs compare
  case-insensitively. A version change of a package that BaseRef already has is not a new package.

  Exception: a new ID passes when the change also adds a line for it to the approvals file
  (scripts/nuget/approved-new-packages.txt, owned in .github/CODEOWNERS):
      <PackageId> | <issue link or reason>
  A line that is already on BaseRef approves nothing, so each approval is used once and stays as history.

  -SelfTest runs the fixture cases in a scratch git repository and exits 0 only when all pass.
  See docs/architecture-guardrails.md.
#>
[CmdletBinding()]
param(
    [string]$BaseRef = '',
    [string]$HeadRef = 'HEAD',
    [string]$RepoRoot = '',
    [string]$ApprovalsPath = 'scripts/nuget/approved-new-packages.txt',
    [switch]$SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ProjectFilePattern = '(^|/)[^/]+\.(csproj|fsproj|vbproj|props|targets)$'
$script:ItemPattern = '<(?<item>PackageVersion|PackageReference|GlobalPackageReference|PackageDownload)\b[^>]*?\sInclude\s*=\s*"(?<id>[^"]+)"'
$script:SdkAttributePattern = '\sSdk\s*=\s*"(?<sdks>[^"]+)"'
$script:SdkElementPattern = '<Sdk\b(?=[^>]*\sVersion\s*=\s*"[^"]+")[^>]*\sName\s*=\s*"(?<name>[^"]+)"'

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string]$Root, [Parameter(Mandatory = $true)][string[]]$Arguments)
    $output = & git -C $Root @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed: $output" }
    return @($output | ForEach-Object { "$_" })
}

function Get-GitFileText {
    param([string]$Root, [string]$Ref, [string]$Path)
    return (Invoke-Git $Root @('show', "${Ref}:$Path")) -join "`n"
}

function Get-PackageIdsInText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    $ids = [System.Collections.Generic.List[string]]::new()
    $withoutComments = [regex]::Replace($Text, '<!--.*?-->', '', 'Singleline')
    foreach ($match in [regex]::Matches($withoutComments, $script:ItemPattern)) {
        foreach ($id in $match.Groups['id'].Value -split ';') {
            $trimmed = $id.Trim()
            if ($trimmed -and $trimmed -notmatch '[$@%]\(') { $ids.Add($trimmed) }
        }
    }
    foreach ($match in [regex]::Matches($withoutComments, $script:SdkAttributePattern)) {
        foreach ($sdk in $match.Groups['sdks'].Value -split ';') {
            $parts = $sdk.Trim() -split '/'
            if ($parts.Count -eq 2 -and $parts[0] -and $parts[1]) { $ids.Add("sdk:$($parts[0].Trim())") }
        }
    }
    foreach ($match in [regex]::Matches($withoutComments, $script:SdkElementPattern)) {
        $ids.Add("sdk:$($match.Groups['name'].Value.Trim())")
    }
    return $ids
}

function Get-PackageLocations {
    param([string]$Root, [string]$Ref)
    $locations = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $files = Invoke-Git $Root @('ls-tree', '-r', '--name-only', $Ref) | Where-Object { $_ -match $script:ProjectFilePattern }
    foreach ($file in $files | Sort-Object) {
        foreach ($id in Get-PackageIdsInText (Get-GitFileText $Root $Ref $file)) {
            if (-not $locations.ContainsKey($id)) { $locations[$id] = $file }
        }
    }
    return $locations
}

function Get-Approvals {
    param([string]$Root, [string]$Ref, [string]$Path)
    $approvals = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $exists = @(Invoke-Git $Root @('ls-tree', '--name-only', $Ref, '--', $Path)).Count -gt 0
    if (-not $exists) { return $approvals }
    foreach ($line in (Get-GitFileText $Root $Ref $Path) -split "`n") {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
        $parts = $trimmed -split '\|', 2
        $reason = if ($parts.Count -eq 2) { $parts[1].Trim() } else { '' }
        $approvals[$parts[0].Trim()] = $reason
    }
    return $approvals
}

function Get-PackageGateResult {
    param([string]$Root, [string]$Base, [string]$Head, [string]$Approvals)
    $before = Get-PackageLocations $Root $Base
    $after = Get-PackageLocations $Root $Head
    $approvedBefore = Get-Approvals $Root $Base $Approvals
    $approvedAfter = Get-Approvals $Root $Head $Approvals
    $added = @($after.Keys | Where-Object { -not $before.ContainsKey($_) } | Sort-Object)
    $approved = @($added | Where-Object { $approvedAfter.ContainsKey($_) -and -not $approvedBefore.ContainsKey($_) -and $approvedAfter[$_] })
    $rejected = @($added | Where-Object { $approved -notcontains $_ })
    return [pscustomobject]@{
        Added    = $added
        Approved = @($approved | ForEach-Object { [pscustomobject]@{ Id = $_; File = $after[$_]; Reason = $approvedAfter[$_] } })
        Rejected = @($rejected | ForEach-Object { [pscustomobject]@{ Id = $_; File = $after[$_] } })
    }
}

function Write-GateReport {
    param([object]$Result, [string]$Approvals)
    $summary = [System.Collections.Generic.List[string]]::new()
    $summary.Add('## NuGet package gate')
    foreach ($entry in $Result.Approved) {
        Write-Host "Approved new package '$($entry.Id)' ($($entry.File)): $($entry.Reason)"
        $summary.Add("- Approved new package ``$($entry.Id)`` in ``$($entry.File)``: $($entry.Reason)")
    }
    foreach ($entry in $Result.Rejected) {
        $message = "New NuGet package '$($entry.Id)' is not allowed without approval. Remove it, or get it approved: add the line '$($entry.Id) | <issue link or reason>' to $Approvals in this pull request and request review from the code owners. See docs/architecture-guardrails.md."
        if ($env:GITHUB_ACTIONS -eq 'true') { Write-Host "::error file=$($entry.File),title=New NuGet package::$message" }
        else { Write-Host "ERROR: $message ($($entry.File))" }
        $summary.Add("- **Blocked** new package ``$($entry.Id)`` in ``$($entry.File)``")
    }
    if ($Result.Added.Count -eq 0) {
        Write-Host 'No new NuGet package IDs.'
        $summary.Add('No new NuGet package IDs.')
    }
    if ($env:GITHUB_STEP_SUMMARY) { $summary | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY }
}

function New-FixtureRepository {
    $root = Join-Path ([IO.Path]::GetTempPath()) "nuget-gate-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $root | Out-Null
    Invoke-Git $root @('init', '--quiet', '--initial-branch=main') | Out-Null
    Invoke-Git $root @('config', 'user.email', 'gate@example.invalid') | Out-Null
    Invoke-Git $root @('config', 'user.name', 'gate') | Out-Null
    Invoke-Git $root @('config', 'commit.gpgsign', 'false') | Out-Null
    Invoke-Git $root @('config', 'core.fsmonitor', 'false') | Out-Null
    return $root
}

function Save-FixtureCommit {
    param([string]$Root, [hashtable]$Files)
    foreach ($path in $Files.Keys) {
        $full = Join-Path $Root $path
        New-Item -ItemType Directory -Path (Split-Path $full) -Force | Out-Null
        Set-Content -LiteralPath $full -Value $Files[$path] -NoNewline
    }
    Invoke-Git $Root @('add', '--all') | Out-Null
    Invoke-Git $Root @('commit', '--quiet', '--allow-empty', '-m', 'fixture') | Out-Null
    return @(Invoke-Git $Root @('rev-parse', 'HEAD'))[0]
}

function Test-FixtureCase {
    param([string]$Name, [hashtable]$Base, [hashtable]$Head, [string[]]$ExpectedRejected)
    $root = New-FixtureRepository
    try {
        $baseSha = Save-FixtureCommit $root $Base
        $headSha = Save-FixtureCommit $root $Head
        $result = Get-PackageGateResult $root $baseSha $headSha 'scripts/nuget/approved-new-packages.txt'
        $actual = @($result.Rejected | ForEach-Object Id) -join ','
        $expected = @($ExpectedRejected) -join ','
        if ($actual -ne $expected) { throw "Self-test '$Name': expected rejected [$expected], got [$actual]." }
        Write-Host "ok - $Name"
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-SelfTest {
    $central = '<Project><ItemGroup><PackageVersion Include="Shouldly" Version="4.3.0" /></ItemGroup></Project>'
    $project = '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Shouldly" /></ItemGroup></Project>'
    $base = @{ 'src/Directory.Packages.props' = $central; 'src/App/App.csproj' = $project }
    $approvals = 'scripts/nuget/approved-new-packages.txt'

    Test-FixtureCase 'unchanged packages pass' $base @{} @()
    Test-FixtureCase 'a version bump passes' $base @{ 'src/Directory.Packages.props' = $central.Replace('4.3.0', '4.4.0') } @()
    Test-FixtureCase 'a removed package passes' $base @{ 'src/App/App.csproj' = '<Project Sdk="Microsoft.NET.Sdk" />' } @()
    Test-FixtureCase 'a case-only rename passes' $base @{ 'src/App/App.csproj' = $project.Replace('Shouldly', 'shouldly') } @()
    Test-FixtureCase 'a new PackageVersion fails' $base @{ 'src/Directory.Packages.props' = $central.Replace('</ItemGroup>', '<PackageVersion Include="Polly" Version="8.0.0" /></ItemGroup>') } @('Polly')
    Test-FixtureCase 'a new PackageReference with a version fails' $base @{ 'src/Other/Other.csproj' = '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Moq" Version="4.0.0" /></ItemGroup></Project>' } @('Moq')
    Test-FixtureCase 'a new GlobalPackageReference in Directory.Build.props fails' $base @{ 'src/Directory.Build.props' = '<Project><ItemGroup><GlobalPackageReference Include="Nerdbank.GitVersioning" Version="3.0.0" /></ItemGroup></Project>' } @('Nerdbank.GitVersioning')
    Test-FixtureCase 'a new versioned MSBuild SDK fails' $base @{ 'src/Host/Host.csproj' = '<Project Sdk="Aspire.AppHost.Sdk/13.6.5" />' } @('sdk:Aspire.AppHost.Sdk')
    Test-FixtureCase 'a commented-out package is ignored' $base @{ 'src/App/App.csproj' = $project.Replace('</ItemGroup>', '<!-- <PackageReference Include="Moq" /> --></ItemGroup>') } @()
    Test-FixtureCase 'an approval added in the same change passes' $base @{ 'src/Directory.Packages.props' = $central.Replace('</ItemGroup>', '<PackageVersion Include="Polly" Version="8.0.0" /></ItemGroup>'); $approvals = "# approvals`nPolly | https://example.invalid/issues/1`n" } @()
    Test-FixtureCase 'an approval without a reason fails' $base @{ 'src/Directory.Packages.props' = $central.Replace('</ItemGroup>', '<PackageVersion Include="Polly" Version="8.0.0" /></ItemGroup>'); $approvals = "Polly |`n" } @('Polly')
    $approvedBase = $base.Clone()
    $approvedBase[$approvals] = "Polly | approved long ago`n"
    Test-FixtureCase 'an approval already on the base approves nothing' $approvedBase @{ 'src/Directory.Packages.props' = $central.Replace('</ItemGroup>', '<PackageVersion Include="Polly" Version="8.0.0" /></ItemGroup>') } @('Polly')
    Write-Host 'NuGet package gate self-test passed.'
}

if ($SelfTest) {
    Invoke-SelfTest
    exit 0
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path }
if ([string]::IsNullOrWhiteSpace($BaseRef)) { throw 'BaseRef is required: the commit the pull request is compared with (its merge base).' }

$result = Get-PackageGateResult $RepoRoot $BaseRef $HeadRef $ApprovalsPath
Write-GateReport $result $ApprovalsPath
if ($result.Rejected.Count -gt 0) {
    Write-Host "$($result.Rejected.Count) new NuGet package(s) need approval."
    exit 1
}
Write-Host 'NuGet package gate passed.'
