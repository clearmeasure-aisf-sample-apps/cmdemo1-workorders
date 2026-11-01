param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",

    [Parameter(Mandatory=$false)]
    [string]$databaseName = "",

    [Parameter(Mandatory=$false)]
    [switch]$Headful,

    [Parameter(Mandatory=$false)]
    [ValidateRange(0, 6)]
    [int]$ShardNumber = 0,

    [Parameter(Mandatory=$false)]
    [int]$ShardCount = 6
)

. .\build.ps1

if ($Headful) {
    $env:HeadlessTestBrowser = "false"
    Log-Message -Message "Running acceptance tests with headful browser windows." -Type "INFO"
}

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
if ($ShardNumber -gt 0) {
    $buildArgs["ShardNumber"] = $ShardNumber
}
if ($ShardCount -ne 6) {
    $buildArgs["ShardCount"] = $ShardCount
}
Invoke-AcceptanceTests @buildArgs
