<#
.SYNOPSIS
  Shared secret-header names and redaction helpers for Playwright traces/videos (issue #1671).

.DESCRIPTION
  Headers that must never appear in stored traces. The test-support secret header is included.
  Values are replaced with [REDACTED]. Also redacts known plaintext secret constants when they
  appear as raw substrings in trace JSONL.
#>

$script:PrAcceptanceRedactedValue = '[REDACTED]'

$script:PrAcceptanceSecretHeaderNames = @(
    'X-Test-Support-Secret',
    'Authorization',
    'Proxy-Authorization',
    'Cookie',
    'Set-Cookie',
    'X-Api-Key'
)

# Known non-production test-support secret used by AcceptanceTestSupportSettings.Secret.
# Defense in depth if it appears outside a header field.
$script:PrAcceptanceSecretSubstrings = @(
    'acceptance-test-support-secret'
)

function Get-PrAcceptanceSecretHeaderNames {
    return @($script:PrAcceptanceSecretHeaderNames)
}

function Get-PrAcceptanceRedactedValue {
    return $script:PrAcceptanceRedactedValue
}

function Test-PrAcceptanceSecretHeaderName {
    param([Parameter(Mandatory = $true)][string]$Name)
    foreach ($secretName in $script:PrAcceptanceSecretHeaderNames) {
        if ([string]::Equals($Name, $secretName, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Protect-PrAcceptanceSecretText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    $result = $Text
    foreach ($secret in $script:PrAcceptanceSecretSubstrings) {
        if ([string]::IsNullOrEmpty($secret)) { continue }
        $result = $result.Replace($secret, $script:PrAcceptanceRedactedValue, [StringComparison]::OrdinalIgnoreCase)
    }
    return $result
}
