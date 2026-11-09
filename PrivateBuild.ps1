param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
	
    [Parameter(Mandatory=$false)]
    [string]$databaseName = ""
)

. .\build.ps1

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
Build @buildArgs

# #1535: style drift fails the private build, not only the CI code-analysis job.
$formatSolution = Join-Path $PSScriptRoot "src/ChurchBulletin.sln"
Write-Host "Checking formatting (dotnet format --verify-no-changes)..."
& dotnet format $formatSolution --verify-no-changes --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet format --verify-no-changes failed. Run 'dotnet format src/ChurchBulletin.sln' and commit the result."
}

$crapAudit = Join-Path $PSScriptRoot "scripts/crap/run-crap-audit.ps1"
$crapThresholdConfig = Join-Path $PSScriptRoot "scripts/crap/crap-gate-threshold.json"
if (-not (Test-Path -LiteralPath $crapThresholdConfig)) {
    throw "CRAP gate threshold file not found: $crapThresholdConfig"
}
$crapThresholdPayload = Get-Content -LiteralPath $crapThresholdConfig -Raw | ConvertFrom-Json
if ($null -eq $crapThresholdPayload.productionThreshold) {
    throw "CRAP gate threshold file missing productionThreshold: $crapThresholdConfig"
}
$crapThreshold = 0
if (-not [int]::TryParse([string]$crapThresholdPayload.productionThreshold, [ref]$crapThreshold) -or $crapThreshold -le 0) {
    throw "CRAP gate productionThreshold must be a positive integer in $crapThresholdConfig (got '$($crapThresholdPayload.productionThreshold)')."
}
# Threshold comes from crap-gate-threshold.json (single source of truth); do not pass -Threshold here.
& $crapAudit -SkipTests -FailOnViolations -Quiet
if ($LASTEXITCODE -ne 0) {
    throw "CRAP gate failed: in-scope production methods exceed threshold $crapThreshold. See crap-metrics/crap-production-violations.json"
}

$deadCodeGates = Join-Path $PSScriptRoot "scripts/dead-code/Invoke-DeadCodeGates.ps1"
& $deadCodeGates
if ($LASTEXITCODE -ne 0) {
    throw "Dead-code gates failed: duplicated lines or unused NuGet packages exceeded the ratchet. See scripts/dead-code/."
}