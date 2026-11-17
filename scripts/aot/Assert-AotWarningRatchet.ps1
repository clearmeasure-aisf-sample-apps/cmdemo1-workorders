# Native AOT warning ratchet for the McpServer AOT publish.
# Reads the log of
#   dotnet publish src/McpServer/McpServer.csproj -c Release -r linux-x64 -p:McpServerAot=true -p:TrimmerSingleWarn=false
# keeps each distinct IL warning once, groups them by the source the warning points at (the member's namespace root),
# and compares the counts with the committed baseline. A new source or a higher count fails. A lower count passes and
# asks for the baseline to be lowered, so the numbers only go down.
#   -Update writes the baseline from the log (use it only after lowering a count, never to accept new warnings).
param(
    [Parameter(Mandatory = $true)][string]$PublishLog,
    [string]$Baseline = (Join-Path $PSScriptRoot 'mcpserver-aot-warning-baseline.json'),
    [switch]$Update
)
$ErrorActionPreference = 'Stop'

function Get-WarningSource([string]$warning) {
    # "IL2026: Microsoft.EntityFrameworkCore.DbContext..ctor(): Using member ..." -> Microsoft.EntityFrameworkCore
    $member = ($warning -replace '^IL\d+:\s*', '') -replace '[(<:`].*$', ''
    $parts = $member.Split('.', [StringSplitOptions]::RemoveEmptyEntries)
    $depth = if ($member.StartsWith('ClearMeasure.Bootcamp.')) { 3 } else { 2 }
    return ($parts | Select-Object -First ([Math]::Min($depth, [Math]::Max(1, $parts.Count - 1)))) -join '.'
}

$unique = Get-Content -LiteralPath $PublishLog |
    Select-String -Pattern 'warning (IL\d+: .*?)( \[[^\]]+\])?$' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } |
    Sort-Object -Unique
$counts = [ordered]@{}
foreach ($group in ($unique | Group-Object { Get-WarningSource $_ } | Sort-Object Name)) {
    $counts[$group.Name] = $group.Count
}
$total = ($counts.Values | Measure-Object -Sum).Sum
if ($null -eq $total) { $total = 0 }

if ($Update) {
    [ordered]@{ total = [int]$total; bySource = $counts } | ConvertTo-Json | Set-Content -LiteralPath $Baseline -Encoding utf8
    "Baseline written: $total distinct AOT warnings."
    exit 0
}

$expected = (Get-Content -LiteralPath $Baseline -Raw | ConvertFrom-Json).bySource
$failures = @(); $lower = @()
foreach ($source in $counts.Keys) {
    $allowed = if ($expected.PSObject.Properties.Name -contains $source) { [int]$expected.$source } else { 0 }
    if ($counts[$source] -gt $allowed) { $failures += "$source $allowed -> $($counts[$source])" }
    elseif ($counts[$source] -lt $allowed) { $lower += "$source $allowed -> $($counts[$source])" }
}
foreach ($source in $expected.PSObject.Properties.Name) {
    if (-not $counts.Contains($source)) { $lower += "$source $($expected.$source) -> 0" }
}

"Distinct AOT warnings: $total"
if ($lower.Count -gt 0) { "Fewer warnings than the baseline; lower it with -Update:"; $lower | ForEach-Object { "  $_" } }
if ($failures.Count -gt 0) {
    "New Native AOT warnings (fix them; do not raise the baseline):"
    $failures | ForEach-Object { "  $_" }
    exit 1
}
"AOT warning ratchet: OK"