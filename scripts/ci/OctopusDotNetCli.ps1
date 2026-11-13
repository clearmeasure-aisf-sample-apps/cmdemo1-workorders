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

function Set-OctopusDotNetCliInstallRollForward {
    <#
    .SYNOPSIS
      Sets DOTNET_ROLL_FORWARD=LatestPatch for `dotnet tool install` of Octopus.DotNet.Cli.
    .DESCRIPTION
      The host SDK's TransactionalAction static ctor uses reflection
      (SetTransactionManagerField) on TransactionManager private fields
      s_cachedMaxTimeout / s_maximumTimeout. DOTNET_ROLL_FORWARD=LatestMajor can load a
      newer runtime whose System.Transactions no longer has those fields, so GetField
      returns null and install fails with:
        TypeInitializationException → NullReferenceException in SetTransactionManagerField
      (captured on CI for #1681). LatestPatch keeps the install host on a matching runtime.
      Use Set-OctopusDotNetCliRuntimeRollForward when later invoking the net6-targeted tool.
    #>
    $env:DOTNET_ROLL_FORWARD = 'LatestPatch'
    return $env:DOTNET_ROLL_FORWARD
}

function Set-OctopusDotNetCliRuntimeRollForward {
    <#
    .SYNOPSIS
      Sets DOTNET_ROLL_FORWARD=LatestMajor so net6-targeted dotnet-octo runs on newer runtimes.
    #>
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    return $env:DOTNET_ROLL_FORWARD
}

function Set-OctopusDotNetCliRollForward {
    <#
    .SYNOPSIS
      Obsolete alias: install-time roll-forward is LatestPatch (#1681). Prefer the Install/Runtime helpers.
    #>
    return Set-OctopusDotNetCliInstallRollForward
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
        [string]$Verbosity = 'minimal',
        [bool]$CliContextVerbose = $false
    )

    $previous = $ErrorActionPreference
    $previousCliVerbose = $env:DOTNET_CLI_CONTEXT_VERBOSE
    $ErrorActionPreference = 'Continue'
    try {
        Set-OctopusDotNetCliInstallRollForward | Out-Null
        # CommandLoggingContext.IsVerbose reads DOTNET_CLI_CONTEXT_VERBOSE (not --verbosity).
        # Without it, TypeInitializationException prints only Message (no InnerException).
        if ($CliContextVerbose) {
            $env:DOTNET_CLI_CONTEXT_VERBOSE = '1'
        }
        $lines = & dotnet tool install --global $PackageId --verbosity $Verbosity 2>&1
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = ($lines | Out-String)
        }
    }
    finally {
        $ErrorActionPreference = $previous
        if ($null -eq $previousCliVerbose) {
            Remove-Item Env:DOTNET_CLI_CONTEXT_VERBOSE -ErrorAction SilentlyContinue
        }
        else {
            $env:DOTNET_CLI_CONTEXT_VERBOSE = $previousCliVerbose
        }
    }
}

function Get-TransactionalActionTypeInitializerProbe {
    <#
    .SYNOPSIS
      Runs TransactionalAction's type initializer via reflection and returns the full exception.
    #>
    $probeDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ta-probe-" + [Guid]::NewGuid().ToString('N'))
    $projectFile = Join-Path $probeDir 'Probe.csproj'
    $programFile = Join-Path $probeDir 'Program.cs'
    try {
        New-Item -ItemType Directory -Path $probeDir | Out-Null
        Set-Content -LiteralPath $projectFile -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@
        Set-Content -LiteralPath $programFile -Value @'
using System.Reflection;
using System.Runtime.CompilerServices;

var sdkRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".dotnet", "sdk");
if (!Directory.Exists(sdkRoot))
{
    sdkRoot = "/usr/share/dotnet/sdk";
}

static Version? TryParseSdkDirVersion(string dir)
{
    return Version.TryParse(Path.GetFileName(dir), out var v) ? v : null;
}

var dll = Directory.Exists(sdkRoot)
    ? Directory.GetDirectories(sdkRoot)
        .Select(static d => (Dir: d, Ver: TryParseSdkDirVersion(d)))
        .Where(static x => x.Ver is not null)
        .OrderByDescending(static x => x.Ver)
        .Select(static x => Path.Combine(x.Dir, "dotnet.dll"))
        .FirstOrDefault(File.Exists)
    : null;

Console.WriteLine("ProbeSdkDll=" + (dll ?? "(none)"));
if (dll is null)
{
    Console.WriteLine("PROBE_FAIL: dotnet.dll not found");
    return;
}

var asm = Assembly.LoadFrom(dll);
var type = asm.GetType("Microsoft.DotNet.Cli.TransactionalAction")
    ?? asm.GetTypes().FirstOrDefault(static t => t.Name == "TransactionalAction");
Console.WriteLine("ProbeType=" + (type?.FullName ?? "(none)"));
if (type is null)
{
    Console.WriteLine("PROBE_FAIL: TransactionalAction type not found");
    return;
}

try
{
    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    Console.WriteLine("PROBE_OK: TransactionalAction type initializer succeeded");
}
catch (Exception ex)
{
    Console.WriteLine("PROBE_FAIL:");
    for (Exception? current = ex; current is not null; current = current.InnerException)
    {
        Console.WriteLine("---");
        Console.WriteLine(current.GetType().FullName + ": " + current.Message);
        Console.WriteLine(current.StackTrace);
    }
    Console.WriteLine("FULL:");
    Console.WriteLine(ex.ToString());
}
'@
        $previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $lines = & dotnet run --project $projectFile --verbosity quiet 2>&1
            return ($lines | Out-String).Trim()
        }
        finally {
            $ErrorActionPreference = $previous
        }
    }
    catch {
        return "PROBE_FAIL: $($_.Exception.ToString())"
    }
    finally {
        Remove-Item -LiteralPath $probeDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Format-OctopusDotNetCliInstallFailure {
    <#
    .SYNOPSIS
      Builds the install-failure message, including env snapshot and a diag re-run.
    .DESCRIPTION
      TypeInitializationException.Message omits InnerException unless the CLI is verbose
      (dotnet Program.cs uses e.ToString() only when DOTNET_CLI_CONTEXT_VERBOSE is set).
    #>
    param(
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [Parameter(Mandatory = $true)][string]$Output,
        [scriptblock]$DiagInstall = { Invoke-DotNetToolInstallGlobal -Verbosity 'diag' -CliContextVerbose $true },
        [scriptblock]$EnvironmentSnapshot = { Get-OctopusDotNetCliInstallEnvironmentSnapshot },
        [scriptblock]$TypeInitializerProbe = { Get-TransactionalActionTypeInitializerProbe }
    )

    $diag = & $DiagInstall
    $probe = [string](& $TypeInitializerProbe)
    $envSnapshot = [string](& $EnvironmentSnapshot)
    Write-Host "[ERROR] $($script:OctopusDotNetCliPackageId) install failed. Environment snapshot:"
    Write-Host $envSnapshot
    Write-Host "[ERROR] $($script:OctopusDotNetCliPackageId) TransactionalAction type-initializer probe:"
    Write-Host $probe
    Write-Host "[ERROR] $($script:OctopusDotNetCliPackageId) install diag re-run (exit $($diag.ExitCode)):"
    Write-Host ([string]$diag.Output)

    return ("Failed to install $($script:OctopusDotNetCliPackageId) (exit $ExitCode): $Output" +
        [Environment]::NewLine +
        "--- environment ---" +
        [Environment]::NewLine +
        $envSnapshot +
        [Environment]::NewLine +
        "--- type-initializer probe ---" +
        [Environment]::NewLine +
        $probe +
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

    Set-OctopusDotNetCliInstallRollForward | Out-Null
    Add-DotNetToolsDirectoryToPath -ToolsPath $ToolsPath | Out-Null
    Install-OctopusDotNetCliWithRetry `
        -DelaySeconds $DelaySeconds `
        -Sleep $Sleep `
        -IsInstalled $IsInstalled `
        -InstallAttempt $InstallAttempt
    Assert-DotNetOctoCommandAvailable -ToolsPath $ToolsPath -ResolveCommand $ResolveCommand
    # After a successful install, allow the net6-targeted shim to run on newer runtimes.
    Set-OctopusDotNetCliRuntimeRollForward | Out-Null
}
