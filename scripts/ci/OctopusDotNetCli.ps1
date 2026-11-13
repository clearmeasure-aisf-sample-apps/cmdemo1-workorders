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

function Invoke-OctopusDotNetCliInstallAttempt {
    param(
        [scriptblock]$InstallTool = {
            $previous = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                Set-OctopusDotNetCliRollForward | Out-Null
                $lines = & dotnet tool install --global $script:OctopusDotNetCliPackageId 2>&1
                return [pscustomobject]@{
                    ExitCode = $LASTEXITCODE
                    Output   = ($lines | Out-String)
                }
            }
            finally {
                $ErrorActionPreference = $previous
            }
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

    throw "Failed to install $($script:OctopusDotNetCliPackageId) (exit $($result.ExitCode)): $output".Trim()
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
