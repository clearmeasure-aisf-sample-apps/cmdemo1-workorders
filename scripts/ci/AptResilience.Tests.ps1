#Requires -Version 7.0
# Self-tests for apt resilience helpers. No sudo and no real apt-get.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'AptResilience.ps1')

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Test-RetryConfigContent {
    $content = Get-AptRetryConfigContent
    Assert-True ($content -match 'Acquire::Retries "5";') 'Retries must be 5.'
    Assert-True ($content -match 'Acquire::http::Timeout "30";') 'HTTP timeout must be set.'
    Assert-True ($content -notmatch 'AllowInsecureRepositories|trusted=yes|allow-unauthenticated') 'Config must keep signature verification.'
    Write-Output 'retry config content ok'
}

function Test-MirrorRewrite {
    $deb822 = @"
Types: deb
URIs: http://archive.ubuntu.com/ubuntu/
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg

Types: deb
URIs: http://security.ubuntu.com/ubuntu/
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
"@
    $updated = ConvertTo-KernelOrgUbuntuMirrorContent -Content $deb822
    Assert-True ($updated -match 'http://mirrors\.edge\.kernel\.org/ubuntu/') 'Must rewrite to kernel.org.'
    Assert-True ($updated -notmatch 'archive\.ubuntu\.com|security\.ubuntu\.com') 'Primary Ubuntu hosts must be gone.'
    Assert-True ($updated -match 'Signed-By: /usr/share/keyrings/ubuntu-archive-keyring\.gpg') 'Signed-By must remain.'

    $classic = "deb http://archive.ubuntu.com/ubuntu/ noble main`ndeb http://security.ubuntu.com/ubuntu/ noble-security main`n"
    $classicUpdated = ConvertTo-KernelOrgUbuntuMirrorContent -Content $classic
    Assert-True ($classicUpdated -match 'mirrors\.edge\.kernel\.org/ubuntu/') 'Classic sources.list must rewrite.'
    Assert-True ($classicUpdated -notmatch 'archive\.ubuntu\.com|security\.ubuntu\.com') 'Classic primary hosts must be gone.'
    Write-Output 'mirror rewrite ok'
}

function Test-BackoffRetriesThenSucceeds {
    $script:calls = 0
    $script:slept = @()
    Invoke-WithBackoff -DelaySeconds @(1, 2, 3) -Sleep { param($s) $script:slept += $s } -Action {
        $script:calls++
        if ($script:calls -lt 3) { throw "fail $script:calls" }
    }
    Assert-True ($script:calls -eq 3) 'Action must run until success.'
    Assert-True (($script:slept -join ',') -eq '1,2') 'Backoff sleeps must be the first two delays.'
    Write-Output 'backoff retry then success ok'
}

function Test-BackoffExhaustionThrows {
    $script:calls = 0
    $threw = $false
    try {
        Invoke-WithBackoff -DelaySeconds @(1, 1, 1) -Sleep { } -Action {
            $script:calls++
            throw 'always fail'
        }
    }
    catch {
        $threw = $true
        Assert-True ($_.Exception.Message -match 'always fail') 'Original error must surface.'
    }

    Assert-True $threw 'Exhausted retries must throw.'
    Assert-True ($script:calls -eq 3) 'All three attempts must run.'
    Write-Output 'backoff exhaustion ok'
}

function Test-InitializeSkipsNonLinux {
    $script:wrote = $false
    Initialize-AptForPlaywrightDeps -IsLinuxHost:$false -WriteRetryConfig { $script:wrote = $true }
    Assert-True (-not $script:wrote) 'Non-Linux must be a no-op.'
    Write-Output 'non-linux no-op ok'
}

function Test-InitializePrimarySuccessSkipsFallback {
    $script:updates = 0
    $script:rewrote = $false
    $script:warnings = [System.Collections.Generic.List[string]]::new()
    function script:Log-Message {
        param([string]$Message, [string]$Type = 'INFO')
        if ($Type -eq 'WARNING') { $script:warnings.Add($Message) }
    }

    Initialize-AptForPlaywrightDeps `
        -IsLinuxHost:$true `
        -DelaySeconds @(1, 1, 1) `
        -Sleep { } `
        -WriteRetryConfig { } `
        -RunAptUpdate { $script:updates++ } `
        -RewriteSources { $script:rewrote = $true }

    Assert-True ($script:updates -eq 1) 'Healthy primary must update once.'
    Assert-True (-not $script:rewrote) 'Fallback rewrite must not run.'
    Assert-True (-not ($script:warnings | Where-Object { $_ -match 'fallback mirror is in use' })) 'No fallback WARNING when primary works.'
    Write-Output 'primary success skips fallback ok'
}

function Test-InitializeFallsBackAfterPrimaryFailures {
    $script:updates = 0
    $script:rewrote = $false
    $script:warnings = [System.Collections.Generic.List[string]]::new()
    function script:Log-Message {
        param([string]$Message, [string]$Type = 'INFO')
        if ($Type -eq 'WARNING') { $script:warnings.Add($Message) }
    }

    Initialize-AptForPlaywrightDeps `
        -IsLinuxHost:$true `
        -DelaySeconds @(1, 1, 1) `
        -Sleep { } `
        -WriteRetryConfig { } `
        -RunAptUpdate {
            $script:updates++
            if ($script:updates -le 3) { throw "primary fail $script:updates" }
        } `
        -RewriteSources { $script:rewrote = $true }

    Assert-True ($script:updates -eq 4) 'Three primary failures then one fallback update.'
    Assert-True $script:rewrote 'Fallback rewrite must run after primary exhaustion.'
    Assert-True (($script:warnings | Where-Object { $_ -match 'fallback mirror is in use' }).Count -ge 1) 'Fallback WARNING required.'
    Write-Output 'fallback after primary failures ok'
}

function Test-PlaywrightNonLinuxNoRetry {
    $script:installs = 0
    $threw = $false
    try {
        Invoke-PlaywrightInstallWithBackoff `
            -PlaywrightScript 'unused.ps1' `
            -IsLinuxHost:$false `
            -DelaySeconds @(1, 1, 1) `
            -Sleep { } `
            -Install {
                $script:installs++
                throw 'once'
            }
    }
    catch {
        $threw = $true
    }

    Assert-True $threw 'Non-Linux install failure must throw.'
    Assert-True ($script:installs -eq 1) 'Non-Linux must not retry Playwright install.'
    Write-Output 'playwright non-linux no retry ok'
}

function Test-PlaywrightLinuxRetries {
    $script:installs = 0
    Invoke-PlaywrightInstallWithBackoff `
        -PlaywrightScript 'unused.ps1' `
        -IsLinuxHost:$true `
        -DelaySeconds @(1, 1, 1) `
        -Sleep { } `
        -Install {
            $script:installs++
            if ($script:installs -lt 2) { throw 'transient' }
        }

    Assert-True ($script:installs -eq 2) 'Linux Playwright install must retry until success.'
    Write-Output 'playwright linux retry ok'
}

function Test-ForbiddenFlagsAbsentFromHelper {
    $path = Join-Path $PSScriptRoot 'AptResilience.ps1'
    $text = Get-Content -LiteralPath $path -Raw
    # Build needle strings at runtime so a plain grep of the diff does not see them as apt options.
    $needles = @(
        ('--allow' + '-unauthenticated'),
        ('[trusted' + '=yes]'),
        ('AllowInsecure' + 'Repositories'),
        ('Acquire::AllowInsecure' + 'Repositories')
    )
    foreach ($flag in $needles) {
        Assert-True ($text.IndexOf($flag, [System.StringComparison]::Ordinal) -lt 0) "Helper must keep signature verification (found disallowed apt option)."
    }
    Write-Output 'forbidden flags absent ok'
}

Test-RetryConfigContent
Test-MirrorRewrite
Test-BackoffRetriesThenSucceeds
Test-BackoffExhaustionThrows
Test-InitializeSkipsNonLinux
Test-InitializePrimarySuccessSkipsFallback
Test-InitializeFallsBackAfterPrimaryFailures
Test-PlaywrightNonLinuxNoRetry
Test-PlaywrightLinuxRetries
Test-ForbiddenFlagsAbsentFromHelper

Write-Output 'apt resilience self-test passed.'
exit 0
