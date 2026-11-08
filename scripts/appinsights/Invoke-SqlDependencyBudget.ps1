#Requires -Version 7.0
<#
.SYNOPSIS
  Run the SQL dependency budget query against Application Insights, or skip when Azure is not configured.

.DESCRIPTION
  Joins requests to SQL dependencies on operation_Id and exits 1 when any operation's sqlCount
  exceeds the budget (default 20). Exits 0 when nothing exceeds the budget, or when the target
  or Azure credentials are absent and -RequireAzure is not set. Exits 2 for a bad argument,
  a query failure, or a missing credential when -RequireAzure is set.

  -SelfTest runs scripts/appinsights/SqlDependencyBudget.Tests.ps1 and does not call Azure.
#>
[CmdletBinding()]
param(
    [switch]$SelfTest,
    [switch]$UseDiscoveredTddTarget,
    [string]$ResourceGroup,
    [string]$AppInsightsName,
    [string]$SubscriptionId,
    [int]$Budget = 20,
    [string]$Lookback = '1h',
    [switch]$RequireAzure,
    [string]$QueryResultPath,
    [string]$QueryTemplatePath
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
. (Join-Path $PSScriptRoot 'SqlDependencyBudget.ps1')

function Write-BudgetStepSummary {
    param([Parameter(Mandatory = $true)][string]$Markdown)

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        return
    }

    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $Markdown -Encoding utf8NoBOM
}

if ($SelfTest) {
    $testScript = Join-Path $PSScriptRoot 'SqlDependencyBudget.Tests.ps1'
    & pwsh -NoProfile -File $testScript
    exit $LASTEXITCODE
}

$templatePath = if ([string]::IsNullOrWhiteSpace($QueryTemplatePath)) {
    Join-Path $PSScriptRoot 'sql-dependency-budget.kql'
}
else {
    $QueryTemplatePath
}

$manifestPath = Join-Path $PSScriptRoot 'tdd-app-insights.json'

try {
    $template = Get-Content -LiteralPath $templatePath -Raw
    $query = New-SqlDependencyBudgetQuery -Budget $Budget -Lookback $Lookback -QueryTemplate $template
    $target = Resolve-SqlDependencyBudgetTarget -ResourceGroup $ResourceGroup -AppInsightsName $AppInsightsName -ManifestPath $manifestPath -UseDiscoveredTarget ([bool]$UseDiscoveredTddTarget)
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 2
}

if (-not [string]::IsNullOrWhiteSpace($QueryResultPath)) {
    if (-not (Test-Path -LiteralPath $QueryResultPath)) {
        [Console]::Error.WriteLine("Query result file not found: $QueryResultPath")
        exit 2
    }

    try {
        $saved = Get-Content -LiteralPath $QueryResultPath -Raw
        $rows = ConvertFrom-AppInsightsQueryTable -Json $saved
        $violations = Get-SqlDependencyBudgetViolations -Rows $rows -Budget $Budget
    }
    catch {
        [Console]::Error.WriteLine($_.Exception.Message)
        exit 2
    }
}
else {
    $accountPresent = $false
    if (Get-Command az -ErrorAction SilentlyContinue) {
        & az account show --output none 2>$null
        $accountPresent = $LASTEXITCODE -eq 0
    }

    $skipReason = Get-SqlDependencyBudgetSkipReason -ResourceGroup $target.ResourceGroup -AppInsightsName $target.AppInsightsName -SubscriptionId $SubscriptionId -AzureAccountPresent $accountPresent
    if ($null -ne $skipReason) {
        $message = "Skipping SQL dependency budget query. $skipReason"
        Write-Output $message
        Write-BudgetStepSummary $message
        if ($RequireAzure) {
            exit 2
        }

        exit 0
    }

    & az account set --subscription $SubscriptionId --output none
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("az account set failed for the configured subscription.")
        exit 2
    }

    $json = & az monitor app-insights query --app $target.AppInsightsName --resource-group $target.ResourceGroup --analytics-query $query --output json
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("az monitor app-insights query failed for $($target.AppInsightsName) in $($target.ResourceGroup).")
        exit 2
    }

    try {
        $rows = ConvertFrom-AppInsightsQueryTable -Json ($json | Out-String)
        $violations = Get-SqlDependencyBudgetViolations -Rows $rows -Budget $Budget
    }
    catch {
        [Console]::Error.WriteLine($_.Exception.Message)
        exit 2
    }
}

if ($violations.Count -eq 0) {
    $passed = "SQL dependency budget passed. Budget $Budget, lookback $Lookback. No request operation exceeded the budget."
    Write-Output $passed
    Write-BudgetStepSummary $passed
    exit 0
}

Write-Output "SQL dependency budget exceeded. Budget $Budget, lookback $Lookback. $($violations.Count) request operation(s):"
$summaryLines = @('| sqlCount | operation_Id | request |', '| --- | --- | --- |')
foreach ($violation in $violations) {
    $line = "sqlCount=$($violation.sqlCount) operation_Id=$($violation.operation_Id) request=$($violation.requestName)"
    Write-Output $line
    $requestName = ([string]$violation.requestName).Replace('|', '/')
    $summaryLines += "| $($violation.sqlCount) | $($violation.operation_Id) | $requestName |"
}

Write-BudgetStepSummary ($summaryLines -join "`n")
exit 1
