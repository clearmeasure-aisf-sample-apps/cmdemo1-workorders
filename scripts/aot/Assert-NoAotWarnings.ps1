#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when a Native AOT publish log has a trim or AOT warning from code that must stay warning-free.
.DESCRIPTION
  Reads the log of a dotnet publish with PublishAot and -p:TrimmerSingleWarn=false, keeps each distinct IL warning once,
  and fails when the member a warning points at matches -Pattern. The default pattern is the feature-flag path
  (OpenFeature, Octopus, and the FeatureFlags namespaces): under Native AOT a reflection-based serializer call there does
  not fail loudly, it makes every flag read its default (PR #1911).
  -SelfTest runs fixture logs and exits 0 only when every case behaves.
#>
[CmdletBinding()]
param(
    [string]$PublishLog = '',
    [string]$Pattern = '(^|\.)(OpenFeature|Octopus)\.|\.FeatureFlags\.',
    [switch]$SelfTest
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-AotWarningMember([string]$Warning) {
    # "IL2026: Octopus.OpenFeature.Provider.FeatureFlagApiClient.<X>d__6.MoveNext(): Using member ..." -> the member
    return (($Warning -replace '^IL\d+:\s*', '') -replace '\(.*$', '') -replace ':\s.*$', ''
}

function Find-AotWarning([string[]]$Lines, [string]$MemberPattern) {
    $unique = @($Lines |
        Select-String -Pattern 'warning (IL\d+: .*?)( \[[^\]]+\])?$' |
        ForEach-Object { $_.Matches[0].Groups[1].Value } |
        Sort-Object -Unique)
    return @($unique | Where-Object { (Get-AotWarningMember $_) -match $MemberPattern })
}

function Invoke-SelfTest {
    $flagWarning = 'ILC : AOT analysis warning IL3050: Octopus.OpenFeature.Provider.FeatureFlagApiClient.<GetServerSideEvaluations>d__6.MoveNext(): Using member ''System.Text.Json.JsonSerializer.Deserialize<ServerSideEvaluation[]>(String,JsonSerializerOptions)'' which has ''RequiresDynamicCodeAttribute'' [/src/x.csproj]'
    $ourWarning = 'ILC : Trim analysis warning IL2026: ClearMeasure.Bootcamp.DataAccess.FeatureFlags.OctopusFlagsProvider.Read(): Using member ''X'' [/src/x.csproj]'
    $efWarning = 'ILC : Trim analysis warning IL2026: Microsoft.EntityFrameworkCore.DbContext..ctor(): Using member ''Octopus.OpenFeature.Thing'' [/src/x.csproj]'
    $cases = @(
        @{ Name = 'a provider warning fails'; Lines = @($flagWarning); Expected = 1 },
        @{ Name = 'a warning in the FeatureFlags namespace fails'; Lines = @($ourWarning); Expected = 1 },
        @{ Name = 'a duplicate line counts once'; Lines = @($flagWarning, $flagWarning); Expected = 1 },
        @{ Name = 'another library that only mentions the provider in its message passes'; Lines = @($efWarning); Expected = 0 },
        @{ Name = 'an empty log passes'; Lines = @(); Expected = 0 }
    )
    $failed = 0
    foreach ($case in $cases) {
        $found = @(Find-AotWarning -Lines $case.Lines -MemberPattern $Pattern).Count
        if ($found -ne $case.Expected) {
            Write-Output "FAIL $($case.Name): expected $($case.Expected), found $found"
            $failed++
        }
    }
    if ($failed -gt 0) { exit 1 }
    Write-Output 'AOT warning-free self-test passed.'
    exit 0
}

if ($SelfTest) { Invoke-SelfTest }
if ([string]::IsNullOrWhiteSpace($PublishLog)) { throw 'Pass -PublishLog <path to the dotnet publish log>, or -SelfTest.' }

$found = @(Find-AotWarning -Lines (Get-Content -LiteralPath $PublishLog) -MemberPattern $Pattern)
if ($found.Count -gt 0) {
    Write-Output "Native AOT warnings from code that must stay warning-free ($Pattern):"
    $found | ForEach-Object { Write-Output "  $_" }
    exit 1
}
Write-Output "No Native AOT warnings match $Pattern."
