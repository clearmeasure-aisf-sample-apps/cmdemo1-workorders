#Requires -Version 7.0
<#
.SYNOPSIS
  Install and verify the Octopus.DotNet.Cli global tool (dotnet-octo) for packaging.

.DESCRIPTION
  Ensures ~/.dotnet/tools is on PATH, installs Octopus.DotNet.Cli with retries when
  missing (without swallowing install errors), and fails clearly when dotnet-octo
  cannot be resolved before pack. "Already installed" is detected via
  `dotnet tool list --global` so install is skipped without a red message.
#>

$script:OctopusDotNetCliPackageId = 'Octopus.DotNet.Cli'
$script:OctopusDotNetCliCommandName = 'dotnet-octo'
$script:OctopusDotNetCliRetryDelaySeconds = @(2, 4, 8)

function Get-DotNetGlobalToolsPath {
    return [System.IO.Path]::Combine(
        [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile),
        '.dotnet',
        'tools')
}

function Add-DotNetToolsDirectoryToPath {
    param(
        [string]$ToolsPath = (Get-DotNetGlobalToolsPath),
        [string]$PathValue = $env:PATH
    )

    $separator = [System.IO.Path]::PathSeparator
    $entries = $PathValue -split [regex]::Escape($separator)
    $alreadyPresent = $entries | Where-Object {
        $_.Trim().Equals($ToolsPath.Trim(), [System.StringComparison]::OrdinalIgnoreCase)
    }
    if ($alreadyPresent) {
        $env:PATH = $PathValue
        return $PathValue
    }

    $updated = "$ToolsPath$separator$PathValue"
    $env:PATH = $updated
    return $updated
}

function Test-GlobalDotNetToolInstalled {
    param(
        [string]$PackageId = $script:OctopusDotNetCliPackageId,
        [scriptblock]$ListGlobalTools = {
            & dotnet tool list --global 2>&1 | Out-String
        }
    )

    $output = & $ListGlobalTools
    if ($null -eq $output) {
        return $false
    }

    foreach ($line in ($output -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0) {
            continue
        }

        $columns = $trimmed -split '\s+', 2
        if ($columns[0].Equals($PackageId, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-IsAlreadyInstalledToolMessage {
    param([Parameter(Mandatory = $true)][string]$Message)

    return $Message -match '(?i)already\s+installed'
}

function Set-OctopusDotNetCliRollForward {
    <#
    .SYNOPSIS
      Sets DOTNET_ROLL_FORWARD=LatestMajor for Octopus.DotNet.Cli (targets net6).
    .DESCRIPTION
      Job-level LatestMinor (build-linux, for dotnet-script) makes `dotnet tool install`
      fail with TransactionalAction on GitHub-hosted runners. publish-image.yml and the
      build-linux pack steps use LatestMajor for the same reason; this keeps callers safe
      even when the ambient env is still LatestMinor.
    #>
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    return $env:DOTNET_ROLL_FORWARD
}

function Get-OctopusDotNetCliInstallEnvironmentSnapshot {
    <#
    .SYNOPSIS
      Captures temp/home/dotnet env and disk state for TransactionalAction failures.
    #>
    $dotnetVars = Get-ChildItem Env:DOTNET_* -ErrorAction SilentlyContinue |
        Sort-Object Name |
        ForEach-Object { "$($_.Name)=$($_.Value)" }

    $toolsRoot = Get-DotNetGlobalToolsPath
    $storePath = [System.IO.Path]::Combine($toolsRoot, '.store')
    $stagePath = [System.IO.Path]::Combine($storePath, '.stage')
    $tmpPath = [System.IO.Path]::GetTempPath()

    $lines = @(
        "HOME=$env:HOME"
        "USERPROFILE=$env:USERPROFILE"
        "TMPDIR=$env:TMPDIR"
        "TMP=$env:TMP"
        "TEMP=$env:TEMP"
        "DOTNET_CLI_HOME=$env:DOTNET_CLI_HOME"
        "GetTempPath=$tmpPath"
        "ToolsPath=$toolsRoot"
        "StorePathExists=$([System.IO.Directory]::Exists($storePath))"
        "StagePathExists=$([System.IO.Directory]::Exists($stagePath))"
    )

    if ($dotnetVars) {
        $lines += $dotnetVars
    }

    foreach ($path in @($tmpPath, $toolsRoot, $env:HOME)) {
        if ([string]::IsNullOrWhiteSpace($path) -or -not [System.IO.Directory]::Exists($path)) {
            continue
        }

        try {
            $drive = Get-PSDrive -PSProvider FileSystem |
                Where-Object { $path.StartsWith($_.Root, [System.StringComparison]::OrdinalIgnoreCase) } |
                Select-Object -First 1
            if ($null -ne $drive) {
                $lines += "Disk[$($drive.Name)] Used=$([math]::Round(($drive.Used / 1MB), 1))MB Free=$([math]::Round(($drive.Free / 1MB), 1))MB Path=$path"
            }
        }
        catch {
            $lines += "DiskProbeFailed Path=$path Error=$($_.Exception.Message)"
        }
    }

    if ([System.IO.Directory]::Exists($stagePath)) {
        $stageEntries = @(Get-ChildItem -LiteralPath $stagePath -Force -ErrorAction SilentlyContinue |
            Select-Object -First 20 |
            ForEach-Object { $_.Name })
        $lines += "StageEntries=$($stageEntries -join ',')"
    }

    return ($lines -join [Environment]::NewLine)
}

function Invoke-DotNetToolInstallGlobal {
    param(
        [string]$PackageId = $script:OctopusDotNetCliPackageId,
        [string]$Verbosity = 'minimal'
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        Set-OctopusDotNetCliRollForward | Out-Null
        $lines = & dotnet tool install --global $PackageId --verbosity $Verbosity 2>&1
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = ($lines | Out-String)
        }
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Format-OctopusDotNetCliInstallFailure {
    <#
    .SYNOPSIS
      Builds the install-failure message, including env snapshot and a diag re-run.
    .DESCRIPTION
      TypeInitializationException.Message omits InnerException unless the CLI is verbose
      (dotnet Program.cs uses e.ToString() only when CommandLoggingContext.IsVerbose).
    #>
    param(
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [Parameter(Mandatory = $true)][string]$Output,
        [scriptblock]$DiagInstall = { Invoke-DotNetToolInstallGlobal -Verbosity 'diag' },
        [scriptblock]$EnvironmentSnapshot = { Get-OctopusDotNetCliInstallEnvironmentSnapshot }
    )

    $diag = & $DiagInstall
    $envSnapshot = [string](& $EnvironmentSnapshot)
    Write-Host "[ERROR] $($script:OctopusDotNetCliPackageId) install failed. Environment snapshot:"
    Write-Host $envSnapshot
    Write-Host "[ERROR] $($script:OctopusDotNetCliPackageId) install diag re-run (exit $($diag.ExitCode)):"
    Write-Host ([string]$diag.Output)

    return ("Failed to install $($script:OctopusDotNetCliPackageId) (exit $ExitCode): $Output" +
        [Environment]::NewLine +
        "--- environment ---" +
        [Environment]::NewLine +
        $envSnapshot +
        [Environment]::NewLine +
        "--- diag re-run (exit $($diag.ExitCode)) ---" +
        [Environment]::NewLine +
        ([string]$diag.Output)).Trim()
}

function Invoke-OctopusDotNetCliInstallAttempt {
    param(
        [scriptblock]$InstallTool = { Invoke-DotNetToolInstallGlobal },
        [scriptblock]$FormatFailure = {
            param([int]$ExitCode, [string]$Output)
            Format-OctopusDotNetCliInstallFailure -ExitCode $ExitCode -Output $Output
        }
    )

    $result = & $InstallTool
    if ($result.ExitCode -eq 0) {
        return
    }

    $output = [string]$result.Output
    if (Test-IsAlreadyInstalledToolMessage -Message $output) {
        return
    }

    throw (& $FormatFailure $result.ExitCode $output)
}

function Install-OctopusDotNetCliWithRetry {
    param(
        [int[]]$DelaySeconds = $script:OctopusDotNetCliRetryDelaySeconds,
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds },
        [scriptblock]$IsInstalled = { Test-GlobalDotNetToolInstalled },
        [scriptblock]$InstallAttempt = { Invoke-OctopusDotNetCliInstallAttempt }
    )

    if (& $IsInstalled) {
        return
    }

    $attemptCount = $DelaySeconds.Count
    $lastError = $null
    for ($attempt = 1; $attempt -le $attemptCount; $attempt++) {
        try {
            & $InstallAttempt
            return
        }
        catch {
            $lastError = $_
            if ($attempt -ge $attemptCount) {
                break
            }

            $delay = $DelaySeconds[$attempt - 1]
            Write-Host "[WARNING] $($script:OctopusDotNetCliPackageId) install attempt $attempt failed; retrying in ${delay}s. $($_.Exception.Message)"
            & $Sleep $delay

            if (& $IsInstalled) {
                return
            }
        }
    }

    throw $lastError
}

function Assert-DotNetOctoCommandAvailable {
    param(
        [string]$ToolsPath = (Get-DotNetGlobalToolsPath),
        [scriptblock]$ResolveCommand = {
            Get-Command $script:OctopusDotNetCliCommandName -ErrorAction SilentlyContinue
        }
    )

    Add-DotNetToolsDirectoryToPath -ToolsPath $ToolsPath | Out-Null
    $command = & $ResolveCommand
    if ($null -ne $command) {
        return
    }

    throw "$($script:OctopusDotNetCliCommandName) was not found after installing $($script:OctopusDotNetCliPackageId). Ensured PATH includes '$ToolsPath'. Current PATH: $env:PATH"
}

function Initialize-OctopusDotNetCli {
    param(
        [int[]]$DelaySeconds = $script:OctopusDotNetCliRetryDelaySeconds,
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds },
        [scriptblock]$IsInstalled = { Test-GlobalDotNetToolInstalled },
        [scriptblock]$InstallAttempt = { Invoke-OctopusDotNetCliInstallAttempt },
        [scriptblock]$ResolveCommand = {
            Get-Command $script:OctopusDotNetCliCommandName -ErrorAction SilentlyContinue
        },
        [string]$ToolsPath = (Get-DotNetGlobalToolsPath)
    )

    Set-OctopusDotNetCliRollForward | Out-Null
    Add-DotNetToolsDirectoryToPath -ToolsPath $ToolsPath | Out-Null
    Install-OctopusDotNetCliWithRetry `
        -DelaySeconds $DelaySeconds `
        -Sleep $Sleep `
        -IsInstalled $IsInstalled `
        -InstallAttempt $InstallAttempt
    Assert-DotNetOctoCommandAvailable -ToolsPath $ToolsPath -ResolveCommand $ResolveCommand
}
