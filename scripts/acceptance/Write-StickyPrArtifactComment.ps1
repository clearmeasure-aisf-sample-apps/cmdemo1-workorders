<#
.SYNOPSIS
  Builds or updates the sticky PR comment that links private Azure videos/traces.

.DESCRIPTION
  Item 16 / #1671. Links are private blob paths (OIDC/RBAC access; no SAS, no public URLs).
  Marker: <!-- pr-acceptance-artifacts -->

.PARAMETER Items
  Objects with TestName, Reason, VideoBlob, TraceBlob (blob URLs or account-relative paths).

.PARAMETER StorageAccountName
  Account name for display / az download hints.

.PARAMETER RunUrl
  Optional Actions run URL.

.PARAMETER BodyPath
  Write the markdown body to this file (for gh pr comment).

.PARAMETER SelfTest
  Runs Write-StickyPrArtifactComment.Tests.ps1.
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromPipeline = $true)]
    [object[]]$Items = @(),

    [string]$StorageAccountName = '',
    [string]$RunUrl = '',
    [string]$BodyPath = '',
    [switch]$SelfTest
)

begin {
    $ErrorActionPreference = 'Stop'
    if ($SelfTest) {
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Write-StickyPrArtifactComment.Tests.ps1')
        exit $LASTEXITCODE
    }
    $collected = [System.Collections.Generic.List[object]]::new()
}

process {
    foreach ($item in @($Items)) {
        if ($null -ne $item) { [void]$collected.Add($item) }
    }
}

end {
    $marker = '<!-- pr-acceptance-artifacts -->'
    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add($marker)
    [void]$lines.Add('## Acceptance videos and traces')
    [void]$lines.Add('')
    [void]$lines.Add('Private Azure Blob Storage (14-day lifecycle, no public access). Authenticate with the existing OIDC / managed identity (`az login` federated or portal RBAC). **No SAS tokens.**')
    [void]$lines.Add('')
    if (-not [string]::IsNullOrWhiteSpace($StorageAccountName)) {
        # Single-quoted so markdown backticks are literal (PowerShell ` escapes `v/`t).
        [void]$lines.Add(('Storage account: `{0}` (containers `videos`, `traces`).' -f $StorageAccountName))
        [void]$lines.Add('')
    }
    if (-not [string]::IsNullOrWhiteSpace($RunUrl)) {
        [void]$lines.Add(('Run: {0}' -f $RunUrl))
        [void]$lines.Add('')
    }

    if ($collected.Count -eq 0) {
        [void]$lines.Add('_No failed or changed-UI videos/traces for this run._')
    }
    else {
        [void]$lines.Add('| Test | Reason | Video | Trace |')
        [void]$lines.Add('| --- | --- | --- | --- |')
        foreach ($item in $collected) {
            $test = [string]$item.TestName
            $reason = [string]$item.Reason
            $video = [string]$item.VideoBlob
            $trace = [string]$item.TraceBlob
            if ([string]::IsNullOrWhiteSpace($video)) { $video = '—' } else { $video = '`' + $video + '`' }
            if ([string]::IsNullOrWhiteSpace($trace)) { $trace = '—' } else { $trace = '`' + $trace + '`' }
            [void]$lines.Add(('| `{0}` | {1} | {2} | {3} |' -f $test, $reason, $video, $trace))
        }
        [void]$lines.Add('')
        [void]$lines.Add('Download example (OIDC / `--auth-mode login`, never a key or SAS):')
        [void]$lines.Add('')
        [void]$lines.Add('```bash')
        $account = if ([string]::IsNullOrWhiteSpace($StorageAccountName)) { '<account>' } else { $StorageAccountName }
        [void]$lines.Add(('az storage blob download --account-name {0} --container-name videos --name <blob> --auth-mode login -f video.webm' -f $account))
        [void]$lines.Add('```')
    }

    $body = ($lines -join "`n") + "`n"
    if (-not [string]::IsNullOrWhiteSpace($BodyPath)) {
        $dir = Split-Path -Parent $BodyPath
        if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
        }
        Set-Content -LiteralPath $BodyPath -Value $body -NoNewline
    }
    Write-Output $body
}
