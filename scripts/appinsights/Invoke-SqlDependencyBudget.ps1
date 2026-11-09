#Requires -Version 7.0
<#
.SYNOPSIS
  Run the SQL dependency budget query against Application Insights, or skip when Azure is not configured.

.DESCRIPTION
  Joins requests to SQL dependencies on operation_Id and exits 1 when any operation's sqlCount
  exceeds the budget (default 20). Exits 0 when nothing exceeds the budget, or when the target
  or Azure credentials are absent and -RequireAzure is not set. Exits 2 for a bad argument,
  a query failure, or a missing credential when -RequireAzure is set.

  -CheckAlertConfiguration reads the scheduled query rule (default sql-dependency-count-budget)
  and exits 1 when it is missing, disabled, or different from the rendered KQL, window,
  evaluation frequency, and action group. An authorization failure exits 2 and names the
  Monitoring Reader assignment. The command output never includes the Azure error body.

  -SelfTest runs scripts/appinsights/SqlDependencyBudget.Tests.ps1 and does not call Azure.
#>
[CmdletBinding()]
param(
    [switch]$SelfTest,
    [switch]$UseDiscoveredTddTarget,
    [switch]$CheckAlertConfiguration,
    [string]$ResourceGroup,
    [string]$AppInsightsName,
    [string]$SubscriptionId,
    [int]$Budget = 20,
    [string]$Lookback = '1h',
    [string]$EvaluationFrequency = 'PT15M',
    [string]$ActionGroupName = 'ag-cmdemo1-tdd-incident',
    [string]$AlertName = 'sql-dependency-count-budget',
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

if ($CheckAlertConfiguration) {
    try {
        $windowSize = Get-SqlDependencyBudgetWindowSize -Lookback $Lookback
        if ($EvaluationFrequency -notmatch '^PT[1-9][0-9]*(M|H)$') {
            throw "EvaluationFrequency '$EvaluationFrequency' must look like PT15M or PT1H."
        }

        $windowMinutes = ConvertTo-DurationMinutes -Duration $windowSize
        $evaluationMinutes = ConvertTo-DurationMinutes -Duration $EvaluationFrequency
        if ($evaluationMinutes -gt $windowMinutes) {
            throw "EvaluationFrequency $EvaluationFrequency is longer than window $windowSize."
        }

        Test-SqlDependencyBudgetName -Name $AlertName -Label 'Alert name'
        Test-SqlDependencyBudgetName -Name $ActionGroupName -Label 'Action group name'
        if (-not [string]::IsNullOrWhiteSpace($SubscriptionId) -and
            $SubscriptionId -notmatch '^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$') {
            throw 'SubscriptionId must be a GUID.'
        }
    }
    catch {
        [Console]::Error.WriteLine($_.Exception.Message)
        exit 2
    }

    $accountPresent = $false
    if (Get-Command az -ErrorAction SilentlyContinue) {
        & az account show --output none 2>$null
        $accountPresent = $LASTEXITCODE -eq 0
    }

    $skipReason = Get-SqlDependencyBudgetSkipReason -ResourceGroup $target.ResourceGroup -AppInsightsName $target.AppInsightsName -SubscriptionId $SubscriptionId -AzureAccountPresent $accountPresent
    if ($null -ne $skipReason) {
        $message = "Skipping SQL dependency budget alert check. $skipReason"
        Write-Output $message
        Write-BudgetStepSummary $message
        if ($RequireAzure) {
            exit 2
        }

        exit 0
    }

    & az account set --subscription $SubscriptionId --output none
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine('az account set failed for the configured subscription.')
        exit 2
    }

    $ruleScope = "/subscriptions/$SubscriptionId/resourceGroups/$($target.ResourceGroup)/providers/Microsoft.Insights/scheduledQueryRules/$AlertName"
    $stderrFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sql-dependency-budget-alert-{0}.err" -f [guid]::NewGuid().ToString('n'))
    $errorText = ''
    $readExit = 1
    try {
        $output = & az resource show --name $AlertName --resource-group $target.ResourceGroup --resource-type 'Microsoft.Insights/scheduledQueryRules' --api-version '2023-03-15-preview' --subscription $SubscriptionId --output json 2>$stderrFile
        $readExit = $LASTEXITCODE
        if (Test-Path -LiteralPath $stderrFile) {
            $errorText = [string](Get-Content -LiteralPath $stderrFile -Raw -ErrorAction SilentlyContinue)
        }
    }
    finally {
        Remove-Item -LiteralPath $stderrFile -Force -ErrorAction SilentlyContinue
    }

    if ($readExit -ne 0) {
        $failure = Get-SqlDependencyBudgetAlertReadFailure -ExitCode $readExit -ErrorText $errorText -Scope $ruleScope
        if ($failure.Kind -eq 'Missing') {
            Write-Output $failure.Message
            Write-BudgetStepSummary $failure.Message
            exit 1
        }

        [Console]::Error.WriteLine($failure.Message)
        exit 2
    }

    $componentScope = "/subscriptions/$SubscriptionId/resourceGroups/$($target.ResourceGroup)/providers/Microsoft.Insights/components/$($target.AppInsightsName)"
    try {
        $drifts = Get-SqlDependencyBudgetAlertDrift -RuleJson ($output | Out-String) -ExpectedQuery $query -ExpectedWindowSize $windowSize -ExpectedEvaluationFrequency $EvaluationFrequency -ExpectedActionGroupName $ActionGroupName -ExpectedScope $componentScope -ExpectedSubscriptionId $SubscriptionId -AlertName $AlertName
    }
    catch {
        [Console]::Error.WriteLine($_.Exception.Message)
        exit 2
    }

    if ($null -eq $drifts -or $drifts.Count -eq 0) {
        $passed = "SQL dependency budget alert matches. Rule $AlertName, budget $Budget, lookback $Lookback, window $windowSize, evaluation $EvaluationFrequency, action group $ActionGroupName."
        Write-Output $passed
        Write-BudgetStepSummary $passed
        exit 0
    }

    Write-Output "SQL dependency budget alert drifted. Rule ${AlertName}:"
    foreach ($drift in $drifts) {
        Write-Output $drift
    }

    Write-BudgetStepSummary (("SQL dependency budget alert drifted. Rule ${AlertName}:`n" + ($drifts -join "`n")))
    exit 1
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

if ($null -eq $violations -or $violations.Count -eq 0) {
    $passed = "SQL dependency budget passed. Budget ${Budget}, lookback ${Lookback}. no operations over budget."
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
