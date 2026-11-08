#Requires -Version 7.0
<#
.SYNOPSIS
  Print the scheduled query alert parameters, or deploy them when -Apply is set.

.DESCRIPTION
  Does not call Azure unless -Apply is present. CI does not run this script.
  -Apply deploys sql-dependency-budget-alert.bicep to the component's resource group.
  Pass -Location with -Apply. The script does not guess the component region.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$UseDiscoveredTddTarget,
    [string]$ResourceGroup,
    [string]$AppInsightsName,
    [string]$SubscriptionId,
    [string]$Location,
    [int]$Budget = 20,
    [string]$Lookback = '1h',
    [string]$ActionGroupId,
    [string]$AlertName = 'sql-dependency-count-budget',
    [int]$Severity = 3,
    [string]$EvaluationFrequency = 'PT15M',
    [switch]$SkipQueryValidation
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
. (Join-Path $PSScriptRoot 'SqlDependencyBudget.ps1')

$templatePath = Join-Path $PSScriptRoot 'sql-dependency-budget.kql'
$bicepPath = Join-Path $PSScriptRoot 'sql-dependency-budget-alert.bicep'
$manifestPath = Join-Path $PSScriptRoot 'tdd-app-insights.json'

try {
    $template = Get-Content -LiteralPath $templatePath -Raw
    $query = New-SqlDependencyBudgetQuery -Budget $Budget -Lookback $Lookback -QueryTemplate $template
    $windowSize = Get-SqlDependencyBudgetWindowSize -Lookback $Lookback
    $target = Resolve-SqlDependencyBudgetTarget -ResourceGroup $ResourceGroup -AppInsightsName $AppInsightsName -ManifestPath $manifestPath -UseDiscoveredTarget ([bool]$UseDiscoveredTddTarget)
    if ([string]::IsNullOrWhiteSpace($target.ResourceGroup)) {
        throw 'App Insights target is not configured. Pass -ResourceGroup and -AppInsightsName, or -UseDiscoveredTddTarget.'
    }

    if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
        throw 'SubscriptionId is required to build the component resource id.'
    }

    $deployment = New-SqlDependencyBudgetDeployment -ResourceGroup $target.ResourceGroup -AppInsightsName $target.AppInsightsName -SubscriptionId $SubscriptionId -Query $query -WindowSize $windowSize -Location $Location -AlertName $AlertName -Severity $Severity -EvaluationFrequency $EvaluationFrequency -ActionGroupId $ActionGroupId -SkipQueryValidation ([bool]$SkipQueryValidation)
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 2
}

Write-Output "Component: $($deployment.ResourceId)"
Write-Output "Window: $($deployment.WindowSize)"
Write-Output 'Query:'
Write-Output $query

if (-not $Apply) {
    Write-Output 'SQL dependency budget alert was not applied.'
    Write-Output 'Re-run with -Apply and -Location set to the component region to create the scheduled query rule.'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Location)) {
    [Console]::Error.WriteLine('-Location is required with -Apply. Read the region from the Application Insights overview blade.')
    exit 2
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    [Console]::Error.WriteLine('Azure CLI is not installed, so -Apply cannot deploy the scheduled query rule.')
    exit 2
}

$parameterFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sql-dependency-budget-{0}.json" -f [guid]::NewGuid().ToString('n'))
$parameterBody = [ordered]@{
    '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters     = $deployment.Parameters
}
$parameterBody | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $parameterFile -Encoding utf8NoBOM

try {
    & az deployment group create --name $AlertName --resource-group $target.ResourceGroup --subscription $SubscriptionId --template-file $bicepPath --parameters "@$parameterFile" --output none
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine('az deployment group create failed. The scheduled query rule was not confirmed.')
        exit 2
    }
}
finally {
    Remove-Item -LiteralPath $parameterFile -Force -ErrorAction SilentlyContinue
}

Write-Output "Applied scheduled query rule $AlertName to resource group $($target.ResourceGroup)."
exit 0
