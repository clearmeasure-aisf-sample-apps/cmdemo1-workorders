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
Assert-True ($null -ne $empty) 'An empty tables array must return a list, not null.'
Assert-True ($empty.Count -eq 0) 'An empty tables array should produce no rows.'
Assert-True ((Get-SqlDependencyBudgetViolations -Rows $empty -Budget 20).Count -eq 0) 'An empty tables array should be within budget.'

$zeroRows = @'
{"tables":[{"name":"PrimaryResult","columns":[{"name":"operation_Id","type":"string"},{"name":"sqlCount","type":"long"},{"name":"requestName","type":"string"}],"rows":[]}]}
'@
$zero = ConvertFrom-AppInsightsQueryTable -Json $zeroRows
Assert-True ($null -ne $zero) 'Zero query rows must not come back as null.'
Assert-True ($zero.Count -eq 0) 'Zero query rows should be an empty list.'
Assert-True ((Get-SqlDependencyBudgetViolations -Rows $zero -Budget 20).Count -eq 0) 'Zero query rows should be within budget.'
$nullRows = Get-SqlDependencyBudgetViolations -Rows $null -Budget 20
Assert-True ($null -ne $nullRows -and $nullRows.Count -eq 0) 'A null row list should be within budget.'

$missingRows = @'
{"tables":[{"name":"PrimaryResult","columns":[{"name":"sqlCount","type":"long"}]}]}
'@
$missing = ConvertFrom-AppInsightsQueryTable -Json $missingRows
Assert-True ($null -ne $missing -and $missing.Count -eq 0) 'A table with no rows property should be an empty list.'
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

$skippedAlert = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -CheckAlertConfiguration -Budget 20 -Lookback '1h' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 0) "An alert check with no target should no-op. Output: $skippedAlert"
Assert-True ($skippedAlert.Contains('Skipping SQL dependency budget alert check.')) 'An alert check with no target should say it is skipping.'

$badFrequency = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -CheckAlertConfiguration -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId '00000000-0000-0000-0000-000000000000' -Budget 20 -Lookback '1h' -EvaluationFrequency 'PT6H' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 2) "An evaluation longer than the window should exit 2 before Azure. Output: $badFrequency"
Assert-True ($badFrequency.Contains('longer than window')) 'A bad evaluation frequency should explain the window mismatch.'

$badSubscription = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -CheckAlertConfiguration -ResourceGroup 'rg-cmdemo1-nonprod' -AppInsightsName 'appi-cmdemo1-tdd' -SubscriptionId 'not-a-guid' -Budget 20 -Lookback '1h' 2>&1 | Out-String
Assert-True ($LASTEXITCODE -eq 2) "A non-GUID subscription should exit 2 before Azure. Output: $badSubscription"
Assert-True ($badSubscription.Contains('SubscriptionId must be a GUID.')) 'A non-GUID subscription should be rejected.'
Assert-True ($badSubscription -notlike '*not-a-guid*') 'A rejected subscription should not be echoed.'

$zeroFile = Join-Path ([System.IO.Path]::GetTempPath()) 'sql-dependency-budget-zero-rows.json'
Set-Content -LiteralPath $zeroFile -Value $zeroRows -Encoding utf8NoBOM
try {
    $healthy = & pwsh -NoProfile -File (Join-Path $root 'Invoke-SqlDependencyBudget.ps1') -QueryResultPath $zeroFile -Budget 20 -Lookback '1h' 2>&1 | Out-String
    Assert-True ($LASTEXITCODE -eq 0) "Zero rows should exit 0. Output: $healthy"
    Assert-True ($healthy.Contains('no operations over budget')) "Zero rows should log that no operations are over budget. Output: $healthy"
}
finally {
    Remove-Item -LiteralPath $zeroFile -Force -ErrorAction SilentlyContinue
}

function New-AlertRuleJson {
    param(
        [string]$Query,
        $Enabled = $true,
        [string]$WindowSize = 'PT1H',
        [string]$EvaluationFrequency = 'PT15M',
        [string[]]$ActionGroupIds = @('/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-cmdemo1-tdd-incident'),
        [string]$Operator = 'GreaterThan',
        $Threshold = 0,
        [string]$TimeAggregation = 'Count',
        [string]$Scope = '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/components/appi-cmdemo1-tdd',
        [switch]$AsActionGroupObject,
        [int]$CriterionCount = 1,
        [switch]$OmitCriteria
    )

    $criteria = @()
    for ($criterionIndex = 0; $criterionIndex -lt $CriterionCount; $criterionIndex++) {
        $criteria += [ordered]@{
            query           = $Query
            timeAggregation = $TimeAggregation
            operator        = $Operator
            threshold       = $Threshold
        }
    }

    $groups = if ($AsActionGroupObject) {
        @($ActionGroupIds | ForEach-Object { [ordered]@{ actionGroupId = $_ } })
    }
    else {
        $ActionGroupIds
    }

    $properties = [ordered]@{
        enabled             = $Enabled
        evaluationFrequency = $EvaluationFrequency
        windowSize          = $WindowSize
        scopes              = @($Scope)
        actions             = [ordered]@{ actionGroups = @($groups) }
    }
    if (-not $OmitCriteria) {
        $properties.criteria = [ordered]@{ allOf = @($criteria) }
    }

    $rule = [ordered]@{
        id         = '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/scheduledQueryRules/sql-dependency-count-budget'
        name       = 'sql-dependency-count-budget'
        type       = 'Microsoft.Insights/scheduledQueryRules'
        properties = $properties
    }
    return $rule | ConvertTo-Json -Depth 8 -Compress
}

$alertSubscription = '00000000-0000-0000-0000-000000000000'
$alertScope = "/subscriptions/$alertSubscription/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/components/appi-cmdemo1-tdd"
$alertGroup = 'ag-cmdemo1-tdd-incident'

function Get-AlertDrift {
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$RuleJson,
        [string]$ExpectedQuery = $query,
        [string]$WindowSize = 'PT1H',
        [string]$EvaluationFrequency = 'PT15M'
    )

    return Get-SqlDependencyBudgetAlertDrift -RuleJson $RuleJson -ExpectedQuery $ExpectedQuery -ExpectedWindowSize $WindowSize -ExpectedEvaluationFrequency $EvaluationFrequency -ExpectedActionGroupName $alertGroup -ExpectedScope $alertScope -ExpectedSubscriptionId $alertSubscription
}

$matching = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query)
Assert-True ($matching.Count -eq 0) ("A rule carrying the rendered KQL should not drift. " + ($matching -join ' | '))

$missingRule = Get-AlertDrift -RuleJson ''
Assert-True ($missingRule.Count -eq 1 -and $missingRule[0] -eq "Scheduled query rule 'sql-dependency-count-budget' is missing.") 'Blank rule JSON should be a missing rule.'
$missingNull = Get-AlertDrift -RuleJson $null
Assert-True ($missingNull.Count -eq 1 -and $missingNull[0] -eq "Scheduled query rule 'sql-dependency-count-budget' is missing.") 'Null rule JSON should be a missing rule.'

$disabled = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -Enabled $false)
Assert-True ($disabled.Count -eq 1 -and $disabled[0] -eq "Scheduled query rule 'sql-dependency-count-budget' is disabled.") 'A disabled rule should drift only as disabled.'

$canary = 'CANARY-SECRET-do-not-print'
$leakedQuery = $query.Replace('sqlCount > 20', "sqlCount > 21 // $canary")
$queryDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $leakedQuery)
Assert-True (($queryDrift -join "`n") -eq "Scheduled query rule 'sql-dependency-count-budget' query does not match the rendered KQL.") ("Query drift should be the only finding. " + ($queryDrift -join ' | '))
Assert-True (($queryDrift -join "`n") -notlike "*$canary*") 'Query drift output must not echo the rule query.'

$both = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $leakedQuery -Enabled $false)
Assert-True ($both.Count -eq 2) 'A disabled rule with a different query should report both drifts.'
Assert-True (($both -join "`n").Contains('is disabled.')) 'The combined case should say the rule is disabled.'
Assert-True (($both -join "`n").Contains('query does not match the rendered KQL.')) 'The combined case should say the query drifted.'
Assert-True (($both -join "`n") -notlike "*$canary*") 'Combined drift output must not echo the rule query.'

$windowDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -WindowSize 'PT30M')
Assert-True ($windowDrift.Count -eq 1 -and $windowDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' windowSize is 'PT30M'; expected 'PT1H'.") 'A window other than PT1H should drift.'

$frequencyDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -EvaluationFrequency 'PT30M')
Assert-True ($frequencyDrift.Count -eq 1 -and $frequencyDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' evaluationFrequency is 'PT30M'; expected 'PT15M'.") 'A frequency other than PT15M should drift.'

$noAction = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -ActionGroupIds @())
Assert-True ($noAction.Count -eq 1 -and $noAction[0] -eq "Scheduled query rule 'sql-dependency-count-budget' actions do not include 'ag-cmdemo1-tdd-incident'.") 'A rule with no action group should drift.'

$wrongAction = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -ActionGroupIds @('/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-other'))
Assert-True ($wrongAction.Count -eq 1 -and $wrongAction[0] -eq "Scheduled query rule 'sql-dependency-count-budget' actions do not include 'ag-cmdemo1-tdd-incident'.") 'A different action group name should drift.'

$otherSubscription = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -ActionGroupIds @('/subscriptions/ffffffff-ffff-ffff-ffff-ffffffffffff/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-cmdemo1-tdd-incident'))
Assert-True ($otherSubscription.Count -eq 1) 'The same action group name in another subscription should drift.'

$extraAction = '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-cmdemo1-tdd-incident'
$withExtra = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -ActionGroupIds @('/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-other', $extraAction) -AsActionGroupObject)
Assert-True ($withExtra.Count -eq 0) ("An extra action group should still count when the expected one is present. " + ($withExtra -join ' | '))

$thresholdDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -Threshold 5)
Assert-True ($thresholdDrift.Count -eq 1 -and $thresholdDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' criterion threshold is '5'; expected 0.") 'A non-zero threshold should drift.'

$operatorDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -Operator 'GreaterThanOrEqual')
Assert-True ($operatorDrift.Count -eq 1 -and $operatorDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' criterion operator is 'GreaterThanOrEqual'; expected GreaterThan.") 'A different operator should drift.'

$aggregationDrift = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -TimeAggregation 'Average')
Assert-True ($aggregationDrift.Count -eq 1 -and $aggregationDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' criterion timeAggregation is 'Average'; expected Count.") 'A different aggregation should drift.'

$twoCriteria = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -CriterionCount 2)
Assert-True ($twoCriteria.Count -eq 1 -and $twoCriteria[0] -eq "Scheduled query rule 'sql-dependency-count-budget' must have one criterion.") 'Two criteria should drift.'

$noCriteria = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -OmitCriteria)
Assert-True ($noCriteria.Count -eq 1 -and $noCriteria[0] -eq "Scheduled query rule 'sql-dependency-count-budget' must have one criterion.") 'A rule with no criteria should drift.'

$wrongScope = Get-AlertDrift -RuleJson (New-AlertRuleJson -Query $query -Scope '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/components/appi-other')
Assert-True ($wrongScope.Count -eq 1 -and $wrongScope[0].Contains('scopes do not include')) 'A rule scoped to another component should drift.'

$rawRule = @'
{
  "name": "sql-dependency-count-budget",
  "properties": {
    "enabled": "true",
    "evaluationFrequency": "pt15m",
    "windowSize": "pt1h",
    "scopes": [
      "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/MICROSOFT.INSIGHTS/components/appi-cmdemo1-tdd"
    ],
    "criteria": {
      "allOf": [
        {
          "query": "requests\r\n| where sqlCount > 20  ",
          "timeAggregation": "count",
          "operator": "greaterthan",
          "threshold": 0
        }
      ]
    },
    "actions": {
      "actionGroups": [
        "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-other/providers/Microsoft.Insights/actionGroups/AG-CMDEMO1-TDD-INCIDENT"
      ]
    }
  }
}
'@
$rawDrift = Get-AlertDrift -RuleJson $rawRule -ExpectedQuery "requests`n| where sqlCount > 20"
Assert-True ($rawDrift.Count -eq 0) ("Normalized casing and newlines should match. " + ($rawDrift -join ' | '))

$flatRule = @'
{
  "enabled": false,
  "evaluationFrequency": "PT15M",
  "windowSize": "PT1H",
  "scopes": ["/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/components/appi-cmdemo1-tdd"],
  "criteria": { "allOf": [ { "query": "requests | where sqlCount > 20", "timeAggregation": "Count", "operator": "GreaterThan", "threshold": 0.0 } ] },
  "actions": { "actionGroups": [ { "actionGroupId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-cmdemo1-nonprod/providers/microsoft.insights/actionGroups/ag-cmdemo1-tdd-incident" } ] }
}
'@
$flatDrift = Get-AlertDrift -RuleJson $flatRule -ExpectedQuery 'requests | where sqlCount > 20'
Assert-True ($flatDrift.Count -eq 1 -and $flatDrift[0] -eq "Scheduled query rule 'sql-dependency-count-budget' is disabled.") ("A flattened disabled rule should drift only as disabled. " + ($flatDrift -join ' | '))

$invalidJson = $false
$invalidMessage = ''
try {
    Get-AlertDrift -RuleJson "{ $canary" | Out-Null
}
catch {
    $invalidJson = $true
    $invalidMessage = $_.Exception.Message
}
Assert-True $invalidJson 'Invalid rule JSON should fail.'
Assert-True ($invalidMessage -eq 'Scheduled query rule JSON is not valid.') "Invalid rule JSON should use a fixed message. Got: $invalidMessage"
Assert-True ($invalidMessage -notlike "*$canary*") 'Invalid JSON errors must not echo the payload.'

$readScope = '/subscriptions/4a4dfa6d-d434-4b9e-8a88-63bbf61cfb69/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/scheduledQueryRules/sql-dependency-count-budget'
$roleCommand = Get-SqlDependencyBudgetAlertReaderRoleCommand -Scope $readScope
Assert-True ($roleCommand.Contains('$(az identity list')) 'The role command must keep the identity lookup as text.'
Assert-True ($roleCommand.Contains("name=='id-cmdemo1-acr-push'")) 'The role command must name id-cmdemo1-acr-push.'
Assert-True ($roleCommand.Contains('--role "Monitoring Reader"')) 'The role command must assign Monitoring Reader.'
Assert-True ($roleCommand.Contains($readScope)) 'The role command must scope to the scheduled query rule.'
$readme = Get-Content -LiteralPath (Join-Path $root 'README.md') -Raw
Assert-True ($readme.Contains($roleCommand)) 'The README must document the same role assignment command the failure prints.'

$authText = "ERROR: (AuthorizationFailed) The client '$canary' with object id '$canary' does not have authorization to perform action 'Microsoft.Insights/scheduledQueryRules/read'."
$authFailure = Get-SqlDependencyBudgetAlertReadFailure -ExitCode 1 -ErrorText $authText -Scope $readScope
Assert-True ($authFailure.Kind -eq 'Authorization') 'AuthorizationFailed should be an authorization result.'
Assert-True ($authFailure.Message.Contains('Not authorized to read scheduled query rule')) 'The authorization message should say the read was refused.'
Assert-True ($authFailure.Message.Contains($roleCommand)) 'The authorization message should include the role assignment command.'
Assert-True ($authFailure.Message -notlike "*$canary*") 'The authorization message must not echo the Azure error body.'

$notFound = Get-SqlDependencyBudgetAlertReadFailure -ExitCode 3 -ErrorText "ERROR: (ResourceNotFound) The Resource was not found. Details: $canary" -Scope $readScope
Assert-True ($notFound.Kind -eq 'Missing') 'ResourceNotFound should be a missing rule.'
Assert-True ($notFound.Message -eq "Scheduled query rule is missing at '$readScope'.") 'The missing message should name the scope.'
Assert-True ($notFound.Message -notlike "*$canary*") 'The missing message must not echo the Azure error body.'

$emptyMissing = Get-SqlDependencyBudgetAlertReadFailure -ExitCode 3 -ErrorText '' -Scope $readScope
Assert-True ($emptyMissing.Kind -eq 'Missing') 'Azure CLI exit 3 with no body should be a missing rule.'

$otherFailure = Get-SqlDependencyBudgetAlertReadFailure -ExitCode 1 -ErrorText "network timeout $canary" -Scope $readScope
Assert-True ($otherFailure.Kind -eq 'Failed') 'An unrecognized Azure error should fail closed.'
Assert-True ($otherFailure.Message -eq "Reading the scheduled query rule at '$readScope' failed (Azure CLI exit 1).") 'An unrecognized Azure error should use a fixed message.'
Assert-True ($otherFailure.Message -notlike "*$canary*") 'A read failure must not echo the Azure error body.'
$codedFailure = Get-SqlDependencyBudgetAlertReadFailure -ExitCode 1 -ErrorText "ERROR: (InvalidApiVersion) rejected $canary" -Scope $readScope
Assert-True ($codedFailure.Message -eq "Reading the scheduled query rule at '$readScope' failed (Azure CLI exit 1, InvalidApiVersion).") 'A read failure should keep the Azure error code.'
Assert-True ($codedFailure.Message -notlike "*$canary*") 'A read failure code must not echo the Azure error body.'
Assert-True ($null -eq (Get-SqlDependencyBudgetAlertReadFailure -ExitCode 0 -ErrorText $canary -Scope $readScope)) 'A successful read should not be classified as a failure.'

Assert-True ($workflow.Contains('-CheckAlertConfiguration')) 'The workflow should compare the live scheduled query rule.'
Assert-True ($workflow.Contains('-Budget 20 -Lookback 1h -EvaluationFrequency PT15M')) 'The workflow should pin budget 20, a 1h window, and a 15m frequency.'
Assert-True ($workflow.Contains('-ActionGroupName ag-cmdemo1-tdd-incident')) 'The workflow should require the tdd incident action group.'
Assert-True ($workflow.Contains('-AlertName sql-dependency-count-budget')) 'The workflow should name the scheduled query rule.'
$checkAt = $workflow.IndexOf('-CheckAlertConfiguration')
$loginAt = $workflow.IndexOf('azure/login@v3')
$queryAt = $workflow.LastIndexOf('Invoke-SqlDependencyBudget.ps1')
Assert-True (($loginAt -ge 0) -and ($checkAt -gt $loginAt) -and ($queryAt -gt $checkAt)) 'The alert check should run after Azure login and before the budget query.'
$checkLine = [string](@($workflow -split "`n" | Where-Object { $_ -like '*-CheckAlertConfiguration*' } | Select-Object -First 1))
Assert-True (-not $checkLine.Contains('SQL_BUDGET')) 'The alert check must not follow a workflow_dispatch budget override.'
Assert-True ($invokeSource.Contains('CheckAlertConfiguration')) 'The query script should accept the alert check switch.'
Assert-True ($invokeSource.Contains('Get-SqlDependencyBudgetAlertReadFailure')) 'The query script should classify Azure read failures.'
Assert-True (-not $invokeSource.Contains('Write-Output $errorText')) 'The query script must not print the Azure error body.'
Assert-True ($invokeSource.Contains('az resource show')) 'The alert check should use a read-only resource show.'
Assert-True (-not $invokeSource.Contains('az role assignment create')) 'The workflow script must not assign roles.'

Write-Output 'SQL dependency budget self-test passed.'
exit 0
