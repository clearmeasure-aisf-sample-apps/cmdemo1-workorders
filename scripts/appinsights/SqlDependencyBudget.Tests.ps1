#Requires -Version 7.0
# Deterministic checks for the SQL dependency budget query. No Azure calls.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'SqlDependencyBudget.ps1')

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $threw = $false
    try {
        & $Action
    }
    catch {
        $threw = $true
    }

    Assert-True $threw $Message
}

function Get-TokenCount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    $count = 0
    $index = 0
    while (($found = $Text.IndexOf($Token, $index, [StringComparison]::Ordinal)) -ge 0) {
        $count++
        $index = $found + $Token.Length
    }

    return $count
}

$template = Get-Content -LiteralPath (Join-Path $root 'sql-dependency-budget.kql') -Raw
Assert-True ((Get-TokenCount -Text $template -Token '{{SQL_BUDGET}}') -eq 1) 'The KQL file must contain one {{SQL_BUDGET}} token.'
Assert-True ((Get-TokenCount -Text $template -Token '{{LOOKBACK}}') -eq 2) 'The KQL file must contain two {{LOOKBACK}} tokens.'
Assert-True ($template.Contains('join kind=inner')) 'The KQL file must inner-join requests to dependencies.'
Assert-True ($template.Contains('on operation_Id')) 'The KQL file must join on operation_Id.'
Assert-True ($template.Contains('type in~ ("SQL", "SQL Server")')) 'The KQL file must count SQL dependency types.'
Assert-True ($template.StartsWith('requests') -or $template.Contains("`nrequests")) 'The KQL file must query the requests table.'

$query = New-SqlDependencyBudgetQuery -Budget 20 -Lookback '1h' -QueryTemplate $template
Assert-True ($query.Contains('sqlCount > 20')) 'Default budget 20 must be substituted as a strict greater-than.'
Assert-True ($query.Contains('ago(1h)')) 'Lookback 1h must be substituted into ago().'
Assert-True (-not $query.Contains('{{')) 'Rendered KQL must not keep tokens.'
Assert-True (-not $query.Contains('appi-cmdemo1-tdd')) 'The query must not name an Application Insights component.'

$custom = New-SqlDependencyBudgetQuery -Budget 7 -Lookback '15m' -QueryTemplate $template
Assert-True ($custom.Contains('sqlCount > 7')) 'A custom budget must be substituted.'
Assert-True ($custom.Contains('ago(15m)')) 'A custom lookback must be substituted.'
Assert-True ((Get-SqlDependencyBudgetWindowSize -Lookback '1h') -eq 'PT1H') '1h must map to PT1H.'
Assert-True ((Get-SqlDependencyBudgetWindowSize -Lookback '1d') -eq 'P1D') '1d must map to P1D.'

Assert-Throws { New-SqlDependencyBudgetQuery -Budget 20 -Lookback '1h;clear' -QueryTemplate $template } 'A lookback that is not a timespan must be rejected.'
Assert-Throws { New-SqlDependencyBudgetQuery -Budget 20 -Lookback 'PT1H' -QueryTemplate $template } 'An ISO-8601 lookback must be rejected.'
Assert-Throws { New-SqlDependencyBudgetQuery -Budget 0 -Lookback '1h' -QueryTemplate $template } 'Budget 0 must be rejected.'
Assert-Throws { New-SqlDependencyBudgetQuery -Budget -1 -Lookback '1h' -QueryTemplate $template } 'A negative budget must be rejected.'
Assert-Throws { New-SqlDependencyBudgetQuery -Budget 20 -Lookback '1h' -QueryTemplate ($template + '{{EVIL}}') } 'An unknown token must be rejected.'

$atBudget = [pscustomobject]@{ sqlCount = 20; operation_Id = 'at-budget'; requestName = 'Teams' }
$overBudget = [pscustomobject]@{ sqlCount = 21; operation_Id = 'over-budget'; requestName = 'Teams' }
$underBudget = [pscustomobject]@{ sqlCount = 1; operation_Id = 'under-budget'; requestName = 'Home' }
$violations = Get-SqlDependencyBudgetViolations -Rows @($atBudget, $overBudget, $underBudget) -Budget 20
Assert-True ($violations.Count -eq 1) "Exactly one row over the budget should be flagged. Got $($violations.Count)."
Assert-True ($violations[0].operation_Id -eq 'over-budget') 'The flagged row should be the operation over the budget.'
$none = Get-SqlDependencyBudgetViolations -Rows @($atBudget) -Budget 20
Assert-True ($none.Count -eq 0) 'sqlCount equal to the budget must not be flagged.'
Assert-Throws { Get-SqlDependencyBudgetViolations -Rows @([pscustomobject]@{ operation_Id = 'x' }) -Budget 20 } 'A row without sqlCount must fail.'

$twoRows = @'
{"tables":[{"name":"PrimaryResult","columns":[{"name":"operation_Id","type":"string"},{"name":"sqlCount","type":"long"},{"name":"requestName","type":"string"}],"rows":[["over-budget",21,"Teams"],["under-budget",3,"Home"]]}]}
'@
$parsed = ConvertFrom-AppInsightsQueryTable -Json $twoRows
Assert-True ($parsed.Count -eq 2) 'Two result rows should parse.'
Assert-True ((Get-SqlDependencyBudgetViolations -Rows $parsed -Budget 20).Count -eq 1) 'Parsed rows should use the same budget rule.'

$oneRow = @'
{"tables":[{"name":"PrimaryResult","columns":[{"name":"sqlCount","type":"long"}],"rows":[[20]]}]}
'@
$single = ConvertFrom-AppInsightsQueryTable -Json $oneRow
Assert-True ($single.Count -eq 1 -and [long]$single[0].sqlCount -eq 20) 'A single result row should parse without collapsing cells.'

$empty = ConvertFrom-AppInsightsQueryTable -Json '{"tables":[]}'
Assert-True ($empty.Count -eq 0) 'An empty tables array should produce no rows.'
Assert-Throws { ConvertFrom-AppInsightsQueryTable -Json 'not-json' } 'Invalid JSON should fail.'
Assert-Throws { ConvertFrom-AppInsightsQueryTable -Json '{"error":true}' } 'A payload without tables should fail.'

$manifestPath = Join-Path $root 'tdd-app-insights.json'
$discovered = Resolve-SqlDependencyBudgetTarget -ManifestPath $manifestPath -UseDiscoveredTarget $true
Assert-True ($discovered.AppInsightsName -eq 'appi-cmdemo1-tdd') 'The discovered tdd component name should be appi-cmdemo1-tdd.'
Assert-True ($discovered.ResourceGroup -eq 'rg-cmdemo1-nonprod') 'The discovered tdd resource group should be rg-cmdemo1-nonprod.'
Assert-True ($discovered.Source -eq 'https://blue-bush-041023510.1.azurestaticapps.net/topology.json') 'The discovered target should cite the platform topology.'
$override = Resolve-SqlDependencyBudgetTarget -ResourceGroup 'rg-other' -AppInsightsName 'appi-other' -ManifestPath $manifestPath -UseDiscoveredTarget $true
Assert-True ($override.AppInsightsName -eq 'appi-other' -and $override.Source -eq 'parameters') 'Explicit names should override the discovered target.'
Assert-Throws { Resolve-SqlDependencyBudgetTarget -ResourceGroup 'rg-only' -ManifestPath $manifestPath -UseDiscoveredTarget $true } 'A resource group without a component name should be rejected.'
Assert-Throws { Resolve-SqlDependencyBudgetTarget -ResourceGroup 'rg;rm -rf' -AppInsightsName 'appi-other' -ManifestPath $manifestPath -UseDiscoveredTarget $false } 'A resource group that is not an Azure name should be rejected.'

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
Assert-True ($manifest.provider -eq 'Microsoft.Insights/components') 'The manifest should name the Application Insights resource type.'
Assert-True ($manifest.PSObject.Properties.Name -notcontains 'subscriptionId') 'The manifest must not carry a subscription id.'
$manifestRaw = Get-Content -LiteralPath $manifestPath -Raw
Assert-True ($manifestRaw -notmatch '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}') 'The manifest must not embed a GUID.'

$skipCredentials = Get-SqlDependencyBudgetSkipReason -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '00000000-0000-0000-0000-000000000000' -AzureAccountPresent $false
Assert-True ($skipCredentials -eq 'Azure credentials are absent.') 'Missing Azure credentials should skip.'
$skipSubscription = Get-SqlDependencyBudgetSkipReason -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '' -AzureAccountPresent $true
Assert-True ($skipSubscription -eq 'Azure subscription id is absent.') 'A blank subscription should skip.'
$skipTarget = Get-SqlDependencyBudgetSkipReason -ResourceGroup '' -AppInsightsName '' -SubscriptionId '00000000-0000-0000-0000-000000000000' -AzureAccountPresent $true
Assert-True ($skipTarget -eq 'App Insights target is not configured.') 'A blank target should skip.'
$ready = Get-SqlDependencyBudgetSkipReason -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '00000000-0000-0000-0000-000000000000' -AzureAccountPresent $true
Assert-True ($null -eq $ready) 'A complete target with an Azure account should not skip.'

$deployment = New-SqlDependencyBudgetDeployment -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '00000000-0000-0000-0000-000000000000' -Query $query -WindowSize 'PT1H' -Location '' -ActionGroupId ''
Assert-True ($deployment.ResourceId -eq '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/components/appi-cmdemo1-tdd') 'The component resource id should be built from parameters.'
Assert-True ($deployment.Parameters.query.value -eq $query) 'The alert parameters should carry the rendered query.'
Assert-True ($deployment.Parameters.actionGroupIds.value.Count -eq 0) 'An omitted action group should be an empty list.'
Assert-Throws { New-SqlDependencyBudgetDeployment -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '00000000-0000-0000-0000-000000000000' -Query $query -WindowSize 'PT1H' -EvaluationFrequency 'PT6H' } 'An evaluation longer than the window should be rejected.'
Assert-Throws { New-SqlDependencyBudgetDeployment -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId 'not-a-guid' -Query $query -WindowSize 'PT1H' } 'A non-GUID subscription should be rejected.'

$bicep = Get-Content -LiteralPath (Join-Path $root 'sql-dependency-budget-alert.bicep') -Raw
Assert-True ($bicep.Contains("resourceId(subscriptionId, appInsightsResourceGroupName, 'Microsoft.Insights/components', appInsightsName)")) 'The Bicep template should build the component id from parameters.'
Assert-True ($bicep.Contains('Microsoft.Insights/scheduledQueryRules@2023-03-15-preview')) 'The Bicep template should define a scheduled query rule.'
Assert-True ($bicep.Contains("operator: 'GreaterThan'") -and $bicep.Contains('threshold: 0')) 'The alert should fire when the query returns any row.'
Assert-True (-not $bicep.Contains('appi-cmdemo1-tdd')) 'The Bicep template must not hardcode the tdd component name.'
Assert-True (-not $bicep.Contains('rg-cmdemo1-nonprod')) 'The Bicep template must not hardcode the tdd resource group.'

$invokeSource = Get-Content -LiteralPath (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -Raw
Assert-True ($invokeSource -match '\[int\]\$Budget = 20') 'The query script default budget should be 20.'
$registerSource = Get-Content -LiteralPath (Join-Path $root 'Register-SqlDependencyBudgetAlert.ps1') -Raw
$notApplied = $registerSource.IndexOf('SQL dependency budget alert was not applied.')
$deploy = $registerSource.IndexOf('deployment group create')
Assert-True ($notApplied -ge 0 -and $deploy -gt $notApplied) 'The Azure deployment command must sit after the not-applied exit.'

$workflowPath = [System.IO.Path]::GetFullPath((Join-Path $root '../../.github/workflows/tdd-sql-dependency-budget.yml'))
$workflow = Get-Content -LiteralPath $workflowPath -Raw
Assert-True ($workflow.Contains('cron:')) 'The workflow should be scheduled.'
Assert-True ($workflow.Contains('azure/login@v3')) 'The workflow should use the repo Azure login action.'
Assert-True ($workflow.Contains('vars.AZURE_CLIENT_ID_ACR_PUSH')) 'The workflow should use the existing ACR push client id variable.'
Assert-True ($workflow.Contains('vars.AZURE_TENANT_ID')) 'The workflow should use the existing tenant variable.'
Assert-True ($workflow.Contains('vars.AZURE_SUBSCRIPTION_ID')) 'The workflow should use the existing subscription variable.'
Assert-True ($workflow.Contains('skip=true')) 'The workflow should record a skip when Azure login settings are absent.'
Assert-True ($workflow.Contains('-UseDiscoveredTddTarget')) 'The workflow should pass the discovered tdd target into the script.'
Assert-True ($workflow.Contains('-RequireAzure')) 'The workflow should fail closed once it has decided credentials are present.'
Assert-True (-not $workflow.Contains('Register-SqlDependencyBudgetAlert.ps1')) 'The workflow must not deploy the alert.'
Assert-True (-not $workflow.Contains('deployment group create')) 'The workflow must not create an Azure deployment.'

$dryRun = & pwsh -NoProfile -File (Join-Path $root 'Register-SqlDependencyBudgetAlert.ps1') -ResourceGroup 'rg-example' -AppInsightsName 'appi-example' -SubscriptionId '00000000-0000-0000-0000-000000000000' -Budget 20 -Lookback '1h' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 0) "A dry run should exit 0. Output: $dryRun"
Assert-True ($dryRun.Contains('SQL dependency budget alert was not applied.')) 'A dry run should say the alert was not applied.'
Assert-True ($dryRun.Contains('/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-example/providers/Microsoft.Insights/components/appi-example')) 'A dry run should print the parameterized component id.'
Assert-True ($dryRun.Contains('sqlCount > 20')) 'A dry run should print the rendered query.'

$skipped = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -Budget 20 -Lookback '1h' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 0) "A query with no target should no-op. Output: $skipped"
Assert-True ($skipped.Contains('Skipping SQL dependency budget query.')) 'A query with no target should say it is skipping.'

$badLookback = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -UseDiscoveredTddTarget -Budget 20 -Lookback '1h;clear' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 2) "A bad lookback should exit 2. Output: $badLookback"

Write-Output 'SQL dependency budget self-test passed.'
exit 0
