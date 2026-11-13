#Requires -Version 7.0
# Self-tests for Octopus.DotNet.Cli install resilience. No real tool install.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'OctopusDotNetCli.ps1')

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Test-PathIncludesToolsDirectory {
    $tools = Join-Path $TestDrive 'dotnet-tools'
    $original = $env:PATH
    try {
        $env:PATH = '/usr/bin'
        $updated = Add-DotNetToolsDirectoryToPath -ToolsPath $tools -PathValue $env:PATH
        Assert-True ($updated.StartsWith("$tools$([System.IO.Path]::PathSeparator)", [System.StringComparison]::Ordinal)) 'Tools path must be prepended.'
        Assert-True ($env:PATH.StartsWith("$tools$([System.IO.Path]::PathSeparator)", [System.StringComparison]::Ordinal)) 'env:PATH must be updated.'

        $again = Add-DotNetToolsDirectoryToPath -ToolsPath $tools -PathValue $env:PATH
        Assert-True ($again -eq $env:PATH) 'Second call must be a no-op.'
        Write-Output 'path includes tools directory ok'
    }
    finally {
        $env:PATH = $original
    }
}

function Test-ListDetectsInstalledPackage {
    $list = {
        @"
Package Id               Version      Commands
-------------------------------------------------
octopus.dotnet.cli       100.0.0      dotnet-octo
other.tool               1.0.0        other
"@
    }
    Assert-True (Test-GlobalDotNetToolInstalled -ListGlobalTools $list) 'Installed package must be detected case-insensitively.'

    $missing = {
        @"
Package Id               Version      Commands
-------------------------------------------------
other.tool               1.0.0        other
"@
    }
    Assert-True (-not (Test-GlobalDotNetToolInstalled -ListGlobalTools $missing)) 'Missing package must not match.'
    Write-Output 'list detects installed package ok'
}

function Test-AlreadyInstalledMessageHelper {
    Assert-True (Test-IsAlreadyInstalledToolMessage -Message 'Tool ''Octopus.DotNet.Cli'' is already installed.') 'Must tolerate already installed.'
    Assert-True (-not (Test-IsAlreadyInstalledToolMessage -Message 'NU1101: Unable to find package')) 'Other failures must not look installed.'
    Write-Output 'already installed message helper ok'
}

function Test-InstallSkippedWhenListed {
    $script:installs = 0
    Install-OctopusDotNetCliWithRetry `
        -DelaySeconds @(1, 1, 1) `
        -Sleep { } `
        -IsInstalled { $true } `
        -InstallAttempt { $script:installs++; throw 'should not install' }

    Assert-True ($script:installs -eq 0) 'Listed tool must skip install.'
    Write-Output 'install skipped when listed ok'
}

function Test-InstallRetriesThenSucceeds {
    $script:installs = 0
    $script:slept = @()
    Install-OctopusDotNetCliWithRetry `
        -DelaySeconds @(1, 2, 3) `
        -Sleep { param($s) $script:slept += $s } `
        -IsInstalled { $false } `
        -InstallAttempt {
            $script:installs++
            if ($script:installs -lt 3) { throw "fail $script:installs" }
        }

    Assert-True ($script:installs -eq 3) 'Install must retry until success.'
    Assert-True (($script:slept -join ',') -eq '1,2') 'Backoff sleeps must be the first two delays.'
    Write-Output 'install retries then succeeds ok'
}

function Test-InstallExhaustionSurfacesRealError {
    $script:installs = 0
    $threw = $false
    try {
        Install-OctopusDotNetCliWithRetry `
            -DelaySeconds @(1, 1, 1) `
            -Sleep { } `
            -IsInstalled { $false } `
            -InstallAttempt {
                $script:installs++
                throw 'NU1101: Unable to find package Octopus.DotNet.Cli'
            }
    }
    catch {
        $threw = $true
        Assert-True ($_.Exception.Message -match 'NU1101') 'Original install error must surface.'
    }

    Assert-True $threw 'Exhausted retries must throw.'
    Assert-True ($script:installs -eq 3) 'All three attempts must run.'
    Write-Output 'install exhaustion surfaces real error ok'
}

function Test-InstallAttemptToleratesAlreadyInstalledExit {
    Invoke-OctopusDotNetCliInstallAttempt -InstallTool {
        [pscustomobject]@{
            ExitCode = 1
            Output   = "Tool 'Octopus.DotNet.Cli' is already installed."
        }
    }
    Write-Output 'install attempt tolerates already installed exit ok'
}

function Test-InstallAttemptSurfacesFailure {
    $threw = $false
    try {
        Invoke-OctopusDotNetCliInstallAttempt -InstallTool {
            [pscustomobject]@{
                ExitCode = 1
                Output   = 'NU1101: Unable to find package Octopus.DotNet.Cli'
            }
        }
    }
    catch {
        $threw = $true
        Assert-True ($_.Exception.Message -match 'NU1101') 'Failure output must be in the thrown message.'
        Assert-True ($_.Exception.Message -match 'Failed to install') 'Failure must be labeled.'
    }

    Assert-True $threw 'Non-zero install without already-installed must throw.'
    Write-Output 'install attempt surfaces failure ok'
}

function Test-AssertResolvesOrFailsClearly {
    Assert-DotNetOctoCommandAvailable -ResolveCommand { [pscustomobject]@{ Name = 'dotnet-octo' } }

    $threw = $false
    try {
        Assert-DotNetOctoCommandAvailable -ToolsPath '/tmp/missing-dotnet-tools' -ResolveCommand { $null }
    }
    catch {
        $threw = $true
        Assert-True ($_.Exception.Message -match 'dotnet-octo was not found') 'Missing command must name dotnet-octo.'
        Assert-True ($_.Exception.Message -match '/tmp/missing-dotnet-tools') 'Missing command must mention tools path.'
    }

    Assert-True $threw 'Unresolved command must throw.'
    Write-Output 'assert resolves or fails clearly ok'
}

function Test-InitializeInstallsThenVerifies {
    $script:installs = 0
    $script:resolved = $false
    Initialize-OctopusDotNetCli `
        -DelaySeconds @(1) `
        -Sleep { } `
        -IsInstalled { $false } `
        -InstallAttempt { $script:installs++ } `
        -ResolveCommand { $script:resolved = $true; [pscustomobject]@{ Name = 'dotnet-octo' } } `
        -ToolsPath (Join-Path ([System.IO.Path]::GetTempPath()) 'octo-cli-test-tools')

    Assert-True ($script:installs -eq 1) 'Initialize must install when missing.'
    Assert-True $script:resolved 'Initialize must verify dotnet-octo resolves.'
    Write-Output 'initialize installs then verifies ok'
}

function Test-HelperDoesNotSwallowStderr {
    $path = Join-Path $PSScriptRoot 'OctopusDotNetCli.ps1'
    $text = Get-Content -LiteralPath $path -Raw
    Assert-True ($text.IndexOf('2>$null', [System.StringComparison]::Ordinal) -lt 0) 'Helper must not swallow stderr with 2>$null.'
    Write-Output 'helper does not swallow stderr ok'
}

$TestDrive = Join-Path ([System.IO.Path]::GetTempPath()) ("octo-cli-tests-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $TestDrive | Out-Null
try {
    Test-PathIncludesToolsDirectory
    Test-ListDetectsInstalledPackage
    Test-AlreadyInstalledMessageHelper
    Test-InstallSkippedWhenListed
    Test-InstallRetriesThenSucceeds
    Test-InstallExhaustionSurfacesRealError
    Test-InstallAttemptToleratesAlreadyInstalledExit
    Test-InstallAttemptSurfacesFailure
    Test-AssertResolvesOrFailsClearly
    Test-InitializeInstallsThenVerifies
    Test-HelperDoesNotSwallowStderr

    Write-Output 'octopus dotnet cli self-test passed.'
    exit 0
}
finally {
    Remove-Item -LiteralPath $TestDrive -Recurse -Force -ErrorAction SilentlyContinue
}
