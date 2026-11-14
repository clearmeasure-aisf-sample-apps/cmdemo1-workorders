<#
.SYNOPSIS
  Fails when pr-environment-artifacts.yml checks out or runs untrusted PR head code (#1671).

.DESCRIPTION
  The workflow_run job has id-token: write and pull-requests: write. It must check out only
  github.sha / github.workflow_sha with persist-credentials: false. head_sha / head_ref may
  appear as data (PR lookup) but never as a checkout ref or as a path to execute.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Test-PrEnvironmentArtifactsWorkflowYaml {
    param([Parameter(Mandatory = $true)][string]$Yaml)

    Assert-True ($Yaml -match '(?m)^\s*ref:\s*\$\{\{\s*github\.(sha|workflow_sha)\s*\}\}') `
        'Checkout must use github.sha or github.workflow_sha (trusted default-branch revision).'

    Assert-True ($Yaml -match '(?m)^\s*persist-credentials:\s*false') `
        'Checkout must set persist-credentials: false.'

    # Forbid checking out the untrusted PR head.
    Assert-True ($Yaml -notmatch 'ref:\s*\$\{\{\s*github\.event\.workflow_run\.head_sha') `
        'Must not check out workflow_run.head_sha (pwn-request).'
    Assert-True ($Yaml -notmatch 'ref:\s*\$\{\{\s*github\.event\.workflow_run\.head_ref') `
        'Must not check out workflow_run.head_ref (pwn-request).'
    Assert-True ($Yaml -notmatch 'ref:\s*\$\{\{\s*github\.event\.workflow_run\.head_sha\s*\|\|') `
        'Must not fall back to head_sha for checkout.'

    # Scripts must come from the trusted checkout path, not a downloaded PR tree.
    Assert-True ($Yaml -match 'pwsh\s+-NoProfile\s+-File\s+scripts/acceptance/') `
        'Must run acceptance scripts from the trusted checkout scripts/acceptance/ path.'
    Assert-True ($Yaml -notmatch 'head_sha.*/scripts/') `
        'Must not run scripts from a path derived from head_sha.'

    # Only the checkout step body (until the next step at the same indent) is constrained.
    # head_sha may appear later as data (PR lookup) and must not be treated as a checkout ref.
    $checkoutBlocks = [regex]::Matches(
        $Yaml,
        '(?m)^(?<indent>\s*)-\s*uses:\s*actions/checkout[^\n]*\n(?<body>(?:\k<indent>\s+[^\n]*\n)*)')
    Assert-True ($checkoutBlocks.Count -ge 1) 'Workflow must use actions/checkout.'
    foreach ($block in $checkoutBlocks) {
        $body = $block.Groups['body'].Value
        Assert-True ($body -notmatch 'head_sha|head_ref') `
            'actions/checkout with: block must not reference head_sha or head_ref.'
        Assert-True ($body -match 'persist-credentials:\s*false') `
            'Each actions/checkout must set persist-credentials: false.'
    }

    Assert-True ($Yaml -match '(?m)^\s*id-token:\s*write') 'Job must declare id-token: write for OIDC.'
    Assert-True ($Yaml -match '(?m)^\s*pull-requests:\s*write') 'Job must declare pull-requests: write for sticky comment.'
    Assert-True ($Yaml -match '(?m)^permissions:\s*\{\s*\}') 'Workflow-level permissions must be empty (least privilege).'
}

if ($SelfTest) {
    $good = @'
permissions: {}
jobs:
  sticky-comment:
    permissions:
      contents: read
      id-token: write
      pull-requests: write
    steps:
      - uses: actions/checkout@v6.0.1
        with:
          ref: ${{ github.sha }}
          persist-credentials: false
      - run: pwsh -NoProfile -File scripts/acceptance/Publish-PrAcceptanceArtifacts.ps1
'@
    Test-PrEnvironmentArtifactsWorkflowYaml -Yaml $good

    $bad = $good -replace 'ref: \$\{\{ github\.sha \}\}', 'ref: ${{ github.event.workflow_run.head_sha }}'
    $failed = $false
    try { Test-PrEnvironmentArtifactsWorkflowYaml -Yaml $bad } catch { $failed = $true }
    Assert-True $failed 'SelfTest must reject head_sha checkout'

    Write-Host 'Test-PrEnvironmentArtifactsWorkflow.ps1 self-test passed'
    exit 0
}

$yamlPath = Join-Path $RepoRoot '.github/workflows/pr-environment-artifacts.yml'
Assert-True (Test-Path -LiteralPath $yamlPath) "Missing $yamlPath"
$yaml = Get-Content -LiteralPath $yamlPath -Raw
Test-PrEnvironmentArtifactsWorkflowYaml -Yaml $yaml
Write-Host 'Test-PrEnvironmentArtifactsWorkflow.ps1 passed'
