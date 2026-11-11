#Requires -Version 7.0
<#
.SYNOPSIS
  Linux apt resilience before Playwright chromium --with-deps.

.DESCRIPTION
  Configures apt Acquire retries, runs apt-get update with backoff, and only after
  every primary attempt fails rewrites Ubuntu archive/security URIs to
  http://mirrors.edge.kernel.org/ubuntu (signature verification stays enabled).
  Also exposes the same backoff wrapper for the Playwright install step.
#>

$script:AptFallbackMirrorUri = 'http://mirrors.edge.kernel.org/ubuntu'
$script:AptRetryDelaySeconds = @(10, 20, 40)
$script:AptHttpTimeoutSeconds = 30
$script:AptAcquireRetries = 5
$script:AptFallbackWarning = 'Ubuntu apt fallback mirror is in use (http://mirrors.edge.kernel.org/ubuntu). Primary archive/security.ubuntu.com updates failed.'

function Get-AptRetryConfigContent {
    @"
Acquire::Retries "$script:AptAcquireRetries";
Acquire::http::Timeout "$script:AptHttpTimeoutSeconds";
Acquire::https::Timeout "$script:AptHttpTimeoutSeconds";
"@
}

function ConvertTo-KernelOrgUbuntuMirrorContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $updated = $Content
    $updated = $updated -replace 'https?://([a-z0-9.-]+\.)?archive\.ubuntu\.com/ubuntu/?', "$script:AptFallbackMirrorUri/"
    $updated = $updated -replace 'https?://([a-z0-9.-]+\.)?security\.ubuntu\.com/ubuntu/?', "$script:AptFallbackMirrorUri/"
    return $updated
}

function Write-AptResilienceMessage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [ValidateSet('INFO', 'WARNING', 'ERROR', 'DEBUG')]
        [string]$Type = 'INFO'
    )

    if (Get-Command Log-Message -ErrorAction SilentlyContinue) {
        Log-Message -Message $Message -Type $Type
        return
    }

    Write-Host "[$Type] $Message"
}

function Invoke-WithBackoff {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,
        [int[]]$DelaySeconds = $script:AptRetryDelaySeconds,
        [scriptblock]$OnRetry,
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds }
    )

    $attemptCount = $DelaySeconds.Count
    $lastError = $null
    for ($attempt = 1; $attempt -le $attemptCount; $attempt++) {
        try {
            & $Action
            return
        }
        catch {
            $lastError = $_
            if ($attempt -ge $attemptCount) {
                break
            }

            $delay = $DelaySeconds[$attempt - 1]
            if ($null -ne $OnRetry) {
                & $OnRetry -Attempt $attempt -DelaySeconds $delay -ErrorRecord $lastError
            }

            & $Sleep $delay
        }
    }

    throw $lastError
}

function Set-AptAcquireRetries {
    param(
        [scriptblock]$WriteFile = {
            param([string]$Path, [string]$Content)
            $Content | sudo tee $Path > $null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to write apt retry config to $Path (exit $LASTEXITCODE)."
            }
        }
    )

    $path = '/etc/apt/apt.conf.d/80-retries'
    & $WriteFile $path (Get-AptRetryConfigContent)
    Write-AptResilienceMessage -Message "Configured apt Acquire retries at $path." -Type 'INFO'
}

function Invoke-AptGetUpdate {
    param(
        [scriptblock]$Run = {
            sudo apt-get update
            if ($LASTEXITCODE -ne 0) {
                throw "apt-get update failed with exit code $LASTEXITCODE."
            }
        }
    )

    & $Run
}

function Set-UbuntuSourcesToKernelOrgMirror {
    param(
        [string[]]$SourcePaths = @(
            '/etc/apt/sources.list.d/ubuntu.sources',
            '/etc/apt/sources.list'
        ),
        [scriptblock]$ReadFile = {
            param([string]$Path)
            if (-not (Test-Path -LiteralPath $Path)) { return $null }
            return Get-Content -LiteralPath $Path -Raw
        },
        [scriptblock]$WriteFile = {
            param([string]$Path, [string]$Content)
            $temp = [System.IO.Path]::GetTempFileName()
            try {
                [System.IO.File]::WriteAllText($temp, $Content)
                sudo cp $temp $Path
                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to rewrite apt sources at $Path (exit $LASTEXITCODE)."
                }
            }
            finally {
                Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
            }
        }
    )

    $rewroteAny = $false
    foreach ($path in $SourcePaths) {
        $content = & $ReadFile $path
        if ([string]::IsNullOrWhiteSpace($content)) {
            continue
        }

        $updated = ConvertTo-KernelOrgUbuntuMirrorContent -Content $content
        if ($updated -eq $content) {
            continue
        }

        & $WriteFile $path $updated
        $rewroteAny = $true
        Write-AptResilienceMessage -Message "Rewrote Ubuntu apt sources at $path to kernel.org fallback mirror." -Type 'INFO'
    }

    if (-not $rewroteAny) {
        throw 'Primary apt-get update failed and no Ubuntu sources contained archive/security.ubuntu.com URIs to rewrite.'
    }
}

function Initialize-AptForPlaywrightDeps {
    param(
        [bool]$IsLinuxHost = $(if ($null -ne $IsLinux) { [bool]$IsLinux } else { $false }),
        [int[]]$DelaySeconds = $script:AptRetryDelaySeconds,
        [scriptblock]$WriteRetryConfig,
        [scriptblock]$RunAptUpdate,
        [scriptblock]$RewriteSources,
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds }
    )

    if (-not $IsLinuxHost) {
        return
    }

    if ($null -eq $WriteRetryConfig) {
        $WriteRetryConfig = { Set-AptAcquireRetries }
    }
    if ($null -eq $RunAptUpdate) {
        $RunAptUpdate = { Invoke-AptGetUpdate }
    }
    if ($null -eq $RewriteSources) {
        $RewriteSources = { Set-UbuntuSourcesToKernelOrgMirror }
    }

    & $WriteRetryConfig

    $primarySucceeded = $false
    try {
        Invoke-WithBackoff -Action $RunAptUpdate -DelaySeconds $DelaySeconds -Sleep $Sleep -OnRetry {
            param($Attempt, $DelaySeconds, $ErrorRecord)
            Write-AptResilienceMessage -Message "apt-get update attempt $Attempt failed; retrying in ${DelaySeconds}s. $($ErrorRecord.Exception.Message)" -Type 'WARNING'
        }
        $primarySucceeded = $true
    }
    catch {
        Write-AptResilienceMessage -Message $script:AptFallbackWarning -Type 'WARNING'
        & $RewriteSources
        Invoke-AptGetUpdate -Run $RunAptUpdate
    }

    if ($primarySucceeded) {
        Write-AptResilienceMessage -Message 'apt-get update succeeded against primary Ubuntu mirrors.' -Type 'INFO'
    }
}

function Invoke-PlaywrightInstallWithBackoff {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PlaywrightScript,
        [bool]$IsLinuxHost = $(if ($null -ne $IsLinux) { [bool]$IsLinux } else { $false }),
        [int[]]$DelaySeconds = $script:AptRetryDelaySeconds,
        [scriptblock]$Install,
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds }
    )

    if ($null -eq $Install) {
        $Install = {
            & pwsh $PlaywrightScript install chromium --with-deps
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to install Playwright chromium (exit $LASTEXITCODE)."
            }
        }
    }

    if (-not $IsLinuxHost) {
        & $Install
        return
    }

    Invoke-WithBackoff -Action $Install -DelaySeconds $DelaySeconds -Sleep $Sleep -OnRetry {
        param($Attempt, $DelaySeconds, $ErrorRecord)
        Write-AptResilienceMessage -Message "Playwright chromium --with-deps attempt $Attempt failed; retrying in ${DelaySeconds}s. $($ErrorRecord.Exception.Message)" -Type 'WARNING'
    }
}
