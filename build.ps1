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
		& pwsh $playwrightScript install chromium --with-deps
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to install Playwright chromium"
		}
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

Function Get-ShardTestFilter {
	param (
		[Parameter(Mandatory = $true)]
		[ValidateRange(0, 6)]
		[int]$ShardNumber,
		[Parameter(Mandatory = $false)]
		[int]$ShardCount = 6
	)

	if ($ShardNumber -eq 0) {
		# Shard 0 means all tests (no filter)
		return ""
	}

	# SIX-WAY SHARDING (189 tests total; split by test class, NonParallelizable fixtures stay whole in one shard).
	# Estimated test time uses measured x64 cost per test (App/AIAgents/Extensions/NServiceBus/Api/DueDate/Dictation/SaveDraft
	# ~4.6 s, Authentication+McpServer ~6.6 s, other WorkOrders ~7 s) plus about 30-60 s fixed overhead per shard; the
	# six shards are balanced to about 180-190 s of test time each (about 4 min wall time with overhead).
	# Shard 1: WorkOrderSearchTests (22), WorkOrderSpeechTests (5) = 27 tests (~189 s)
	$shard1Filter = "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSpeechTests"

	# Shard 2: WorkOrderSearchRedesignTests (13), DueDate (6), Dictation (5), SaveDraft (6), AIAgents (4) = 34 tests (~188 s)
	$shard2Filter = "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchRedesignTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDueDateTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDictationTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSaveDraftTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.AIAgents."

	# Shard 3: Api (8), Extensions (5), NServiceBus (1), and the small WorkOrders classes (17: AiChat, Assign, Attachment, Begin, Cancel,
	# Complete, DescriptionCharCount, RoomNumberLength, SaveThenAssignPersistence [NonParallelizable], Shelved, StatusDashboard) = 31 tests (~183 s)
	$shard3Filter = "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.Api.|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.Extensions.|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.NServiceBus.|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAiChatTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAssignTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAttachmentTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderBeginTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderCancelTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderCompleteTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDescriptionCharCountTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderRoomNumberLengthTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSaveThenAssignPersistenceTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderShelvedTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderStatusDashboardTests"

	# Shard 4: Authentication (25) + App AiAgentPage, WarmUp, LandingPage (4) = 29 tests (~183 s)
	$shard4Filter = "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.Authentication.|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.App.AiAgentPageTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.App.WarmUpTests|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests"

	# Shard 5: whole McpServer namespace (26; ListWorkOrders and Lifecycle are NonParallelizable and stay together) + App NavMenuTests (3) = 29 tests (~185 s)
	$shard5Filter = "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.McpServer.|FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.App.NavMenuTests"

	# Shard 6: EXPLICIT EXCLUSION CHAIN - everything not in Shards 1-5 = 39 tests (~179 s):
	# the remaining App classes (includes NonParallelizable NeedsRebootHealthCheckTests), plus any newly added test class
	$shard6Filter = "FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSpeechTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchRedesignTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDueDateTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDictationTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSaveDraftTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.AIAgents.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.Api.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.Extensions.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.NServiceBus.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAiChatTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAssignTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderAttachmentTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderBeginTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderCancelTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderCompleteTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderDescriptionCharCountTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderRoomNumberLengthTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSaveThenAssignPersistenceTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderShelvedTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderStatusDashboardTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.Authentication.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.App.AiAgentPageTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.App.WarmUpTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.McpServer.&FullyQualifiedName!~ClearMeasure.Bootcamp.AcceptanceTests.App.NavMenuTests"

	switch ($ShardNumber) {
		1 { return $shard1Filter }
		2 { return $shard2Filter }
		3 { return $shard3Filter }
		4 { return $shard4Filter }
		5 { return $shard5Filter }
		6 { return $shard6Filter }
		default {
			throw "Invalid shard number: $ShardNumber"
		}
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
