#!/usr/bin/env pwsh
. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString -and -not (Test-IsGitHubActions)) {
	$env:ConnectionStrings__SqlConnectionString = $null
	[Environment]::SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", $null, "User")
}

$projectName = "ChurchBulletin"
$base_dir = resolve-path .\
$source_dir = Join-Path $base_dir "src"
$solutionName = Join-Path $source_dir "$projectName.sln"
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath =  Join-PathSegments $source_dir "UI" "Server"
$databaseProjectPath = Join-Path $source_dir "Database"
$projectConfig = $env:BuildConfiguration
$framework = "net10.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"
$coverletRunSettings = Join-Path $base_dir "coverlet.runsettings"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Update" }

if (Test-IsArmArchitecture) {
	$env:database_engine = "SQLite"
	$env:DATABASE_ENGINE = "SQLite"
}
$script:databaseEngine = $env:DATABASE_ENGINE

$databaseName = $projectName

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

# ── Main Functions ──────────────────────────────────────────────────────────────

Function Disable-Arm64VsTestDirectoryVerification {
	# TODO: remove once a .NET 10 SDK ships the fix for microsoft/vstest#16657.
	# SDK 10.0.404 is the latest 10.0 patch (2026-11-10). Its vstest, like 9.0.321, opens the
	# session directory with flags 196608 on every OS but macOS. On aarch64 those bits are
	# O_DIRECT|O_LARGEFILE, so open() returns errno 22 and dotnet test exits 1 after every test
	# has passed. A mode 0700 directory does not change that. No newer 10.0 SDK exists.
	# https://github.com/microsoft/vstest/issues/16657
	if (-not (Test-IsLinux) -or -not (Test-IsArmArchitecture)) {
		return
	}

	$env:VSTEST_DISABLE_ARTIFACTS_DIRECTORY_VERIFICATION = "1"
	Log-Message -Message "Skipping vstest's artifacts directory verification on linux-arm64 (microsoft/vstest#16657, SDK 10.0.404)." -Type "INFO"
}

Function Init {
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		throw "PowerShell 7 is required to run this build script."
	}

	Initialize-SqlServerModule

	if (Test-IsLinux) {
		# A cache path the machine already sets wins: /tmp is often a small in-memory disk.
		if (-not (Test-IsGitHubActions) -and -not $env:NUGET_PACKAGES) {
			$env:NUGET_PACKAGES = "/tmp/nuget-packages"
		}
	}

	if ([string]::IsNullOrEmpty($script:databaseServer)) {
		$script:databaseServer = Get-DefaultDatabaseServer -engine $script:databaseEngine
	}

	switch ($script:databaseEngine) {
		"SQL-Container" {
			# SQL_EXTERNAL=true => connect to an already-running SQL Server (a k8s
			# sidecar or a shared server) instead of starting a local Docker
			# container, so no Docker daemon is required in the build environment.
			if ($env:SQL_EXTERNAL -ne "true" -and -not (Test-IsDockerRunning)) {
				throw "Docker is not running. Start Docker (for example, Docker Desktop) so the container-based SQL Server required for 'SQL-Container' builds can run, then rerun this build script."
			}
		}
		"SQLite" {
			if ([string]::IsNullOrEmpty($env:ConnectionStrings__SqlConnectionString)) {
				$env:ConnectionStrings__SqlConnectionString = "Data Source=ChurchBulletin.db"
			}
		}
	}

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}

	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $solutionName -nologo -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}

	exec {
		& dotnet restore $solutionName -nologo --interactive -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}

	Disable-Arm64VsTestDirectoryVerification
}

Function Compile {
	exec {
		& dotnet build $solutionName -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:MSBuildTreatAllWarningsAsErrors="true" `
			/p:SuppressNETCoreSdkPreviewMessage=true `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="Church Bulletin"
	}
}

Function UnitTests {
	Push-Location -Path $unitTestProjectPath

	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "UnitTests") --no-build `
				--no-restore --configuration $projectConfig `
				--settings:$coverletRunSettings `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function Setup-DatabaseForBuild {
	if ($script:databaseEngine -eq "SQL-Container") {
		# With an external/shared SQL Server (SQL_EXTERNAL=true) the server already
		# exists, so skip creating a per-build Docker container. New-SqlServerDatabase
		# still runs, dropping and recreating THIS build's database on that server -
		# so every PrivateBuild starts from a clean, freshly-migrated database.
		if ($env:SQL_EXTERNAL -ne "true") {
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
		}
		New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		$containerName = Get-ContainerName -DatabaseName $script:databaseName
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $script:databaseServer -database $script:databaseName -password $sqlPassword
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	elseif ($script:databaseEngine -eq "LocalDB") {
		$env:ConnectionStrings__SqlConnectionString = New-IntegratedConnectionString -server $script:databaseServer -database $script:databaseName
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
}

Function IntegrationTest {
	Push-Location -Path $integrationTestProjectPath

	try {
		exec {
				if ($script:useSqlite) {
				& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
					--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
					--no-restore --configuration $projectConfig `
					--settings:$coverletRunSettings `
					--collect:"XPlat Code Coverage" `
					--filter "Category!=SqlServerOnly"
			}
			else {
				& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
					--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
					--no-restore --configuration $projectConfig `
					--settings:$coverletRunSettings `
					--collect:"XPlat Code Coverage"
			}
		}
	}
	finally {
		Pop-Location
	}
}

Function Package-Everything{

	# Allow Octopus.DotNet.Cli (targets net6.0) to run on the current .NET SDK
	$env:DOTNET_ROLL_FORWARD = "LatestMajor"

	dotnet tool install --global Octopus.DotNet.Cli 2>$null # prevents red 'already installed' message

	# Ensure dotnet tools are in PATH
	$dotnetToolsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
	$pathEntries = $env:PATH -split [System.IO.Path]::PathSeparator
	$dotnetToolsPathPresent = $pathEntries | Where-Object { $_.Trim().ToLowerInvariant() -eq $dotnetToolsPath.Trim().ToLowerInvariant() }
	if (-not $dotnetToolsPathPresent) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
	}

	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function Build {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",

		[Parameter(Mandatory = $false)]
		[string]$databaseName = "",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)

	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
		}
		$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
	}

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	Init
	Compile
	UnitTests
	Setup-DatabaseForBuild
	IntegrationTest

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
	}

	# The private build says which command it is, for the build facts of what the integration build releases
	# (the kit's .github/private-build/add-private-build.ps1 reads it; build.yml keeps the file as an artifact).
	# Only the command as a developer types it says so: a variant (-UseSqlite, a named database) does not.
	if ($env:GITHUB_ACTIONS -eq 'true' -and -not $UseSqlite -and [string]::IsNullOrEmpty($databaseServer) -and [string]::IsNullOrEmpty($databaseName)) {
		@{ command = '. ./build.ps1 ; Build' } | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'private-build.json') -Encoding utf8
	}
}

Function Invoke-CIBuild {
	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		$script:databaseName = Get-ResolvedDatabaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild $false
	}

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	Init
	Compile
	UnitTests
	Setup-DatabaseForBuild
	IntegrationTest

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
	}
}

# ── Helper Functions (in call order) ────────────────────────────────────────────

Function Resolve-DatabaseEngine {
	$onLinux = Test-IsLinux
	$dockerAvailable = $false
	if ($onLinux) {
		$dockerAvailable = Test-IsDockerRunning
	}
	$script:databaseEngine = Get-ResolvedDatabaseEngine -currentEngine $script:databaseEngine -onLinux $onLinux -dockerAvailable $dockerAvailable
	$script:useSqlite = ($script:databaseEngine -eq "SQLite")
}

Function MigrateDatabaseLocal {
	param (
	 [Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseServerFunc,

		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseNameFunc
	)
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"

	if (Test-IsLinux) {
		$containerName = Get-ContainerName -DatabaseName $databaseNameFunc
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts, "sa", $sqlPassword)
	}
	else {
		$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts)
	}

	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
}

Function PackageUI {
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}

	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $(Join-PathSegments $uiProjectPath "bin" $projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}

}

Function PackageDatabase {
	exec {
		& dotnet publish $databaseProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}

}

Function PackageAcceptanceTests {
	# Use Debug configuration so full symbols are available to display better error messages in test failures
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}

	# Copy the .playwright metadata folder into the publish output so the nupkg
	# is self-contained.  The playwright.ps1 install command needs this folder to
	# know which browser versions to download on the target machine.
	$publishDir = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework "publish"
	$playwrightSource = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework ".playwright"
	if (Test-Path $playwrightSource) {
		Copy-Item -Path $playwrightSource -Destination (Join-Path $publishDir ".playwright") -Recurse -Force
	}

	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $publishDir --outFolder $build_dir --overwrite
	}

}

Function PackageScript {
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}

}

Function AcceptanceTests {
	param (
		[Parameter(Mandatory = $false)]
		[string]$TestFilter = ""
	)

	$projectConfig = "Release"
	Push-Location -Path $acceptanceTestProjectPath

	$playwrightScript = Join-PathSegments "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		# Linux CI: apt retries + optional kernel.org mirror fallback before Playwright --with-deps (issue #1617).
		$aptResilience = Join-PathSegments $base_dir "scripts" "ci" "AptResilience.ps1"
		. $aptResilience
		Initialize-AptForPlaywrightDeps -IsLinuxHost:(Test-IsLinux)
		Invoke-PlaywrightInstallWithBackoff -PlaywrightScript $playwrightScript -IsLinuxHost:(Test-IsLinux)
	}
	else {
		throw "Playwright script not found at $playwrightScript. Cannot run acceptance tests without the browsers."
	}

	$uiServerProcess = Get-Process -Name "ClearMeasure.Bootcamp.UI.Server" -ErrorAction SilentlyContinue
	if ($uiServerProcess) {
		Log-Message -Message "Warning: ClearMeasure.Bootcamp.UI.Server is already running in background (PID: $($uiServerProcess.Id)). This may interfere with acceptance tests." -Type "WARNING"
	}

	$runSettingsPath = Join-Path $acceptanceTestProjectPath "AcceptanceTests.runsettings"
	try {
		$dotnetArgs = @(
			"test",
			"/p:CopyLocalLockFileAssemblies=true",
			"-nologo",
			"-v", "minimal",
			"--logger:trx",
			"--results-directory", $(Join-Path $test_dir "AcceptanceTests"),
			"--no-build",
			"--no-restore",
			"--configuration", $projectConfig,
			"--settings:$runSettingsPath",
			"--collect:XPlat Code Coverage"
		)

		if (-not [string]::IsNullOrEmpty($TestFilter)) {
			$dotnetArgs += @("--filter", $TestFilter)
		}

		exec {
			& dotnet @dotnetArgs
		}
	}
	finally {
		Pop-Location
	}
}

Function Get-AcceptanceShardDocument {
	$path = Join-Path $acceptanceTestProjectPath "acceptance-shards.json"
	if (-not (Test-Path -LiteralPath $path)) {
		throw "Acceptance shard assignment was not found at $path."
	}

	return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

Function Get-ShardTestFilter {
	param (
		[Parameter(Mandatory = $true)]
		[ValidateRange(0, 6)]
		[int]$ShardNumber,
		[Parameter(Mandatory = $false)]
		[int]$ShardCount = 6
	)

	# Shard 0 runs the whole suite. Shards 1..N are an explicit class list in
	# src/AcceptanceTests/acceptance-shards.json. There is no catch-all shard.
	if ($ShardNumber -eq 0) {
		return ""
	}

	$doc = Get-AcceptanceShardDocument
	$expected = [int]$doc.shardCount
	if ($ShardCount -ne $expected) {
		throw "ShardCount $ShardCount does not match acceptance-shards.json shardCount $expected."
	}

	if ($ShardNumber -gt $expected) {
		throw "Invalid shard number: $ShardNumber"
	}

	$names = @(
		$doc.classes |
			Where-Object { [int]$_.shard -eq $ShardNumber } |
			ForEach-Object { [string]$_.name } |
			Sort-Object
	)
	if ($names.Count -eq 0) {
		throw "Shard $ShardNumber has no acceptance tests in acceptance-shards.json."
	}

	return (($names | ForEach-Object { "FullyQualifiedName~$_" }) -join "|")
}

Function Assert-AcceptanceShardBalance {
	param (
		[Parameter(Mandatory = $true)]
		[hashtable]$ListedTests
	)

	$doc = Get-AcceptanceShardDocument
	$shardCount = [int]$doc.shardCount
	$workers = [int]$doc.workers
	$classes = @($doc.classes)
	$problems = [System.Collections.Generic.List[string]]::new()
	$counts = @(0) * $shardCount
	$x64 = @(0.0) * $shardCount
	$arm = @(0.0) * $shardCount

	foreach ($n in 1..$shardCount) {
		foreach ($test in @($ListedTests[$n])) {
			if ([string]::IsNullOrWhiteSpace($test)) {
				continue
			}

			$owners = @($classes | Where-Object { $test.StartsWith("$($_.name).") })
			if ($owners.Count -ne 1) {
				$problems.Add("Listed test '$test' on shard $n matches $($owners.Count) assigned classes.")
				continue
			}

			$ownerShard = [int]$owners[0].shard
			if ($ownerShard -ne $n) {
				$problems.Add("Listed test '$test' is on shard $n but $($owners[0].name) is assigned to shard $ownerShard.")
				continue
			}

			$counts[$n - 1]++
		}
	}

	for ($index = 0; $index -lt $shardCount; $index++) {
		$shard = $index + 1
		$group = @($classes | Where-Object { [int]$_.shard -eq $shard })
		$x64[$index] = Get-AcceptanceShardSpan -Classes $group -SecondsName "x64Seconds" -LongestName "longestX64Seconds" -Workers $workers
		$arm[$index] = Get-AcceptanceShardSpan -Classes $group -SecondsName "armSeconds" -LongestName "longestArmSeconds" -Workers $workers
	}

	Write-Host "Acceptance shard balance (listed tests / estimated seconds):"
	foreach ($n in 1..$shardCount) {
		Write-Host ("  Shard {0}: tests={1} x64={2:n0}s arm={3:n0}s" -f $n, $counts[$n - 1], $x64[$n - 1], $arm[$n - 1])
	}

	Add-AcceptanceShardRatio -Problems $problems -Label "listed tests" -Values $counts -Limit ([double]$doc.maxCountRatio)
	Add-AcceptanceShardRatio -Problems $problems -Label "x64 duration" -Values $x64 -Limit ([double]$doc.maxDurationRatio)
	Add-AcceptanceShardRatio -Problems $problems -Label "arm duration" -Values $arm -Limit ([double]$doc.maxDurationRatio)
	if ($problems.Count -gt 0) {
		throw ($problems -join [Environment]::NewLine)
	}
}

Function Get-AcceptanceShardSpan {
	param (
		$Classes,
		[string]$SecondsName,
		[string]$LongestName,
		[int]$Workers
	)

	$sum = 0.0
	$longest = 0.0
	foreach ($class in @($Classes)) {
		$sum += [double]$class.$SecondsName
		$value = [double]$class.$LongestName
		if ($value -gt $longest) {
			$longest = $value
		}
	}

	$parallel = 0.0
	if ($Workers -gt 0) {
		$parallel = $sum / $Workers
	}

	if ($longest -gt $parallel) {
		return $longest
	}

	return $parallel
}

Function Add-AcceptanceShardRatio {
	param (
		$Problems,
		[string]$Label,
		$Values,
		[double]$Limit
	)

	$items = @($Values)
	$sum = 0.0
	foreach ($value in $items) {
		$sum += [double]$value
	}

	$mean = 0.0
	if ($items.Count -gt 0) {
		$mean = $sum / $items.Count
	}

	$max = 0.0
	$shard = 1
	for ($i = 0; $i -lt $items.Count; $i++) {
		if ([double]$items[$i] -gt $max) {
			$max = [double]$items[$i]
			$shard = $i + 1
		}
	}

	$ratio = 0.0
	if ($mean -gt 0) {
		$ratio = $max / $mean
	}

	Write-Host ("  {0}: max shard {1} is {2:n2}x the mean {3:n1} (limit {4})" -f $Label, $shard, $ratio, $mean, $Limit)
	if ($mean -le 0 -or $ratio -gt $Limit) {
		$Problems.Add("$Label shard $shard is $([math]::Round($ratio, 2))x the mean $([math]::Round($mean, 1)); limit is $Limit.")
	}
}

Function Invoke-AcceptanceTests {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",
		[Parameter(Mandatory=$false)]
		[string]$databaseName ="",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite,

		[Parameter(Mandatory = $false)]
		[ValidateRange(0, 6)]
		[int]$ShardNumber = 0,

		[Parameter(Mandatory = $false)]
		[int]$ShardCount = 6
	)

	$projectConfig = "Release"
	$sw = [Diagnostics.Stopwatch]::StartNew()

	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
		}
		$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
	}

	Init
	Compile
	Setup-DatabaseForBuild

	$testFilter = Get-ShardTestFilter -ShardNumber $ShardNumber -ShardCount $ShardCount
	AcceptanceTests -TestFilter $testFilter

	$sw.Stop()
	$elapsed = $sw.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
	}
}

Function Create-SqlServerInDocker {
	param (
		[Parameter(Mandatory = $true)]
			[ValidateNotNullOrEmpty()]
			[string]$serverName,
		[Parameter(Mandatory = $true)]
			[ValidateNotNullOrEmpty()]
			[string]$dbAction,
		[Parameter(Mandatory = $true)]
			[ValidateNotNullOrEmpty()]
			[string]$scriptDir
		)
	$tempDatabaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
	$containerName = Get-ContainerName -DatabaseName $tempDatabaseName
	$sqlPassword = Get-SqlServerPassword -ContainerName $containerName

	New-DockerContainerForSqlServer -containerName $containerName
	New-SqlServerDatabase -serverName $serverName -databaseName $tempDatabaseName

	$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $serverName -database $tempDatabaseName -password $sqlPassword
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	$dbArgs = @($databaseDll, $dbAction, $serverName, $tempDatabaseName, $scriptDir, "sa", $sqlPassword)
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
	# Restore connection string to default project database
	$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $script:databaseServer -database $projectName -password (Get-SqlServerPassword -ContainerName (Get-ContainerName -DatabaseName $projectName))
}
