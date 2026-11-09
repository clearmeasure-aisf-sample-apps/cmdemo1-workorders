# Duplication and unused-NuGet gates (#1540).
# Duplication: normalized 8-line clones under src, excluding Generated. Ceiling may only decrease.
# Packages: direct PackageReferences whose compile assemblies are absent from the built assembly.
# Community qodana-cdnet has no unused-NuGet inspection; this script is that gate.
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$configuration = "Release"
if (-not [string]::IsNullOrWhiteSpace($env:BuildConfiguration)) {
    $configuration = $env:BuildConfiguration
}
elseif (-not [string]::IsNullOrWhiteSpace($env:BUILD_CONFIGURATION)) {
    $configuration = $env:BUILD_CONFIGURATION
}

$env:DEAD_CODE_GATE_REQUIRE_OUTPUTS = "1"
$project = Join-Path $repoRoot "src/UnitTests/UnitTests.csproj"
$filter = "FullyQualifiedName~DuplicationGateTests|FullyQualifiedName~UnusedPackageGateTests"

& dotnet test $project `
    --configuration $configuration `
    --no-build `
    --nologo `
    --filter $filter

exit $LASTEXITCODE
