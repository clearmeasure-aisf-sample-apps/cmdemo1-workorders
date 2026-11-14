<#
.SYNOPSIS
  Shared secret-header denylist and redaction helpers for Playwright traces (issue #1671).

.DESCRIPTION
  Header names that must never keep their values in stored traces. Built-in denylist:
  X-Test-Support-Secret, Authorization, Proxy-Authorization, Cookie, Set-Cookie, X-Api-Key.
  Extra names come from PR_ACCEPTANCE_SECRET_HEADERS (comma/semicolon-separated) so acceptance
  config can extend the list without editing this file.

  Live secret *values* (never logged) come from, in order of presence:
  TestSupport__Secret, ACCEPTANCE_TEST_SUPPORT_SECRET, PR_ACCEPTANCE_SECRET_VALUES
  (comma/semicolon-separated). The known non-production constant is always included as a
  substring denylist entry. Values are replaced with [REDACTED].
#>

$script:PrAcceptanceRedactedValue = '[REDACTED]'

$script:PrAcceptanceBuiltinSecretHeaderNames = @(
    'X-Test-Support-Secret',
    'Authorization',
    'Proxy-Authorization',
    'Cookie',
    'Set-Cookie',
    'X-Api-Key'
)

# Known non-production test-support secret used by AcceptanceTestSupportSettings.Secret.
$script:PrAcceptanceBuiltinSecretSubstrings = @(
    'acceptance-test-support-secret'
)

function Split-PrAcceptanceConfigList {
    param([AllowEmptyString()][string]$Raw)
    if ([string]::IsNullOrWhiteSpace($Raw)) { return @() }
    return @(
        $Raw -split '[,;]' |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Get-PrAcceptanceSecretHeaderNames {
    $names = [System.Collections.Generic.List[string]]::new()
    foreach ($n in $script:PrAcceptanceBuiltinSecretHeaderNames) {
        [void]$names.Add($n)
    }
    foreach ($extra in (Split-PrAcceptanceConfigList -Raw $env:PR_ACCEPTANCE_SECRET_HEADERS)) {
        $already = $false
        foreach ($existing in $names) {
            if ([string]::Equals($existing, $extra, [StringComparison]::OrdinalIgnoreCase)) {
                $already = $true
                break
            }
        }
        if (-not $already) {
            [void]$names.Add($extra)
        }
    }
    return @($names)
}

function Get-PrAcceptanceSecretSubstrings {
    # Never Write-Host / log these values.
    $values = [System.Collections.Generic.List[string]]::new()
    foreach ($builtin in $script:PrAcceptanceBuiltinSecretSubstrings) {
        [void]$values.Add($builtin)
    }

    foreach ($envName in @('TestSupport__Secret', 'ACCEPTANCE_TEST_SUPPORT_SECRET', 'PR_ACCEPTANCE_SECRET_VALUES')) {
        $raw = [Environment]::GetEnvironmentVariable($envName)
        if ([string]::IsNullOrEmpty($raw)) { continue }
        if ($envName -eq 'PR_ACCEPTANCE_SECRET_VALUES') {
            foreach ($part in (Split-PrAcceptanceConfigList -Raw $raw)) {
                [void]$values.Add($part)
            }
        }
        else {
            [void]$values.Add($raw)
        }
    }

    # Distinct, case-insensitive, without echoing contents.
    $distinct = [System.Collections.Generic.List[string]]::new()
    foreach ($v in $values) {
        if ([string]::IsNullOrEmpty($v)) { continue }
        $found = $false
        foreach ($d in $distinct) {
            if ([string]::Equals($d, $v, [StringComparison]::OrdinalIgnoreCase)) {
                $found = $true
                break
            }
        }
        if (-not $found) {
            [void]$distinct.Add($v)
        }
    }
    return @($distinct)
}

function Get-PrAcceptanceRedactedValue {
    return $script:PrAcceptanceRedactedValue
}

function Test-PrAcceptanceSecretHeaderName {
    param([Parameter(Mandatory = $true)][string]$Name)
    foreach ($secretName in (Get-PrAcceptanceSecretHeaderNames)) {
        if ([string]::Equals($Name, $secretName, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Protect-PrAcceptanceSecretText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    $result = $Text
    foreach ($secret in (Get-PrAcceptanceSecretSubstrings)) {
        if ([string]::IsNullOrEmpty($secret)) { continue }
        $result = $result.Replace($secret, $script:PrAcceptanceRedactedValue, [StringComparison]::OrdinalIgnoreCase)
    }
    return $result
}

function Test-PrAcceptanceValueLooksSecret {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$HeaderName,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$HeaderValue
    )
    if ([string]::IsNullOrEmpty($HeaderValue)) { return $false }
    if ($HeaderValue -eq $script:PrAcceptanceRedactedValue) { return $false }
    if (Test-PrAcceptanceSecretHeaderName -Name $HeaderName) { return $true }
    foreach ($secret in (Get-PrAcceptanceSecretSubstrings)) {
        if ([string]::IsNullOrEmpty($secret)) { continue }
        if ($HeaderValue.Contains($secret, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}
