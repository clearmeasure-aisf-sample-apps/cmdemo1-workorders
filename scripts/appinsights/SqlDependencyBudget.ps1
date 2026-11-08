# Functions for the SQL dependency budget query. Dot-source only; the entry scripts call these.

$ErrorActionPreference = 'Stop'

function Get-SqlDependencyBudgetWindowSize {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Lookback
    )

    $windows = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $windows['15m'] = 'PT15M'
    $windows['30m'] = 'PT30M'
    $windows['1h'] = 'PT1H'
    $windows['6h'] = 'PT6H'
    $windows['12h'] = 'PT12H'
    $windows['1d'] = 'P1D'

    if (-not $windows.ContainsKey($Lookback)) {
        throw "Lookback '$Lookback' is not one of: 15m, 30m, 1h, 6h, 12h, 1d."
    }

    return $windows[$Lookback]
}

function ConvertTo-DurationMinutes {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Duration
    )

    if ($Duration -match '^PT([1-9][0-9]*)M$') {
        return [int]$Matches[1]
    }

    if ($Duration -match '^PT([1-9][0-9]*)H$') {
        return [int]$Matches[1] * 60
    }

    if ($Duration -match '^P([1-9][0-9]*)D$') {
        return [int]$Matches[1] * 1440
    }

    throw "Duration '$Duration' is not an ISO-8601 window this script accepts (PT15M, PT1H, P1D)."
}

function Test-SqlDependencyBudgetName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if ($Name -notmatch '^[A-Za-z0-9]([A-Za-z0-9._()-]{0,248}[A-Za-z0-9])?$') {
        throw "$Label '$Name' is not an Azure resource name this script will put in a resource id."
    }
}

function New-SqlDependencyBudgetQuery {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Budget,
        [Parameter(Mandatory = $true)]
        [string]$Lookback,
        [Parameter(Mandatory = $true)]
        [string]$QueryTemplate
    )

    if ($Budget -lt 1 -or $Budget -gt 100000) {
        throw "Budget must be from 1 through 100000. Got $Budget."
    }

    # Reject an unknown lookback before it is substituted into KQL.
    $null = Get-SqlDependencyBudgetWindowSize -Lookback $Lookback

    if ($QueryTemplate -notlike '*{{SQL_BUDGET}}*' -or $QueryTemplate -notlike '*{{LOOKBACK}}*') {
        throw 'Query template must contain {{SQL_BUDGET}} and {{LOOKBACK}}.'
    }

    $rendered = $QueryTemplate.Replace('{{SQL_BUDGET}}', [string]$Budget).Replace('{{LOOKBACK}}', $Lookback)
    if ($rendered -like '*{{*') {
        throw 'Query template still contains an unreplaced token.'
    }

    return $rendered
}

function Resolve-SqlDependencyBudgetTarget {
    param(
        [string]$ResourceGroup,
        [string]$AppInsightsName,
        [string]$ManifestPath,
        [bool]$UseDiscoveredTarget
    )

    $hasResourceGroup = -not [string]::IsNullOrWhiteSpace($ResourceGroup)
    $hasName = -not [string]::IsNullOrWhiteSpace($AppInsightsName)
    if ($hasResourceGroup -xor $hasName) {
        throw 'Pass both -ResourceGroup and -AppInsightsName, or neither when -UseDiscoveredTddTarget is set.'
    }

    if ($hasResourceGroup -and $hasName) {
        Test-SqlDependencyBudgetName -Name $ResourceGroup -Label 'Resource group'
        Test-SqlDependencyBudgetName -Name $AppInsightsName -Label 'App Insights name'
        return [pscustomobject]@{
            ResourceGroup    = $ResourceGroup
            AppInsightsName  = $AppInsightsName
            Source           = 'parameters'
        }
    }

    if (-not $UseDiscoveredTarget) {
        return [pscustomobject]@{
            ResourceGroup   = ''
            AppInsightsName = ''
            Source          = 'none'
        }
    }

    if ([string]::IsNullOrWhiteSpace($ManifestPath) -or -not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Discovered tdd target file not found: $ManifestPath"
    }

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    foreach ($property in @('appInsightsName', 'resourceGroup', 'source', 'provider')) {
        if ([string]::IsNullOrWhiteSpace([string]$manifest.$property)) {
            throw "Discovered tdd target is missing $property."
        }
    }

    $resolvedGroup = [string]$manifest.resourceGroup
    $resolvedName = [string]$manifest.appInsightsName
    Test-SqlDependencyBudgetName -Name $resolvedGroup -Label 'Resource group'
    Test-SqlDependencyBudgetName -Name $resolvedName -Label 'App Insights name'
    return [pscustomobject]@{
        ResourceGroup   = $resolvedGroup
        AppInsightsName = $resolvedName
        Source          = [string]$manifest.source
    }
}

function Get-SqlDependencyBudgetSkipReason {
    param(
        [string]$ResourceGroup,
        [string]$AppInsightsName,
        [string]$SubscriptionId,
        [bool]$AzureAccountPresent
    )

    if ([string]::IsNullOrWhiteSpace($ResourceGroup) -or [string]::IsNullOrWhiteSpace($AppInsightsName)) {
        return 'App Insights target is not configured.'
    }

    if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
        return 'Azure subscription id is absent.'
    }

    if (-not $AzureAccountPresent) {
        return 'Azure credentials are absent.'
    }

    return $null
}

function ConvertFrom-JsonElementValue {
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.Json.JsonElement]$Element
    )

    switch ($Element.ValueKind) {
        'Number' {
            $longValue = [long]0
            if ($Element.TryGetInt64([ref]$longValue)) {
                return $longValue
            }

            return $Element.GetDouble()
        }
        'String' { return $Element.GetString() }
        'True' { return $true }
        'False' { return $false }
        'Null' { return $null }
        'Undefined' { return $null }
        default { return $Element.GetRawText() }
    }
}

function ConvertFrom-AppInsightsQueryTable {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Json
    )

    if ([string]::IsNullOrWhiteSpace($Json)) {
        throw 'Application Insights query result JSON is empty.'
    }

    $document = $null
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($Json)
    }
    catch {
        throw "Application Insights query result is not JSON. $($_.Exception.Message)"
    }

    try {
        $root = $document.RootElement
        $tablesProperty = [System.Text.Json.JsonElement]::Parse('null')
        if (-not $root.TryGetProperty('tables', [ref]$tablesProperty)) {
            throw 'Application Insights query result has no tables property.'
        }
        $result = [System.Collections.Generic.List[object]]::new()
        if ($tablesProperty.GetArrayLength() -eq 0) {
            return $result
        }

        $table = $tablesProperty[0]
        $columns = [System.Collections.Generic.List[string]]::new()
        foreach ($column in $table.GetProperty('columns').EnumerateArray()) {
            $columns.Add($column.GetProperty('name').GetString())
        }

        foreach ($row in $table.GetProperty('rows').EnumerateArray()) {
            $item = [ordered]@{}
            for ($index = 0; $index -lt $columns.Count; $index++) {
                $item[$columns[$index]] = ConvertFrom-JsonElementValue -Element $row[$index]
            }

            $result.Add([pscustomobject]$item)
        }

        return $result
    }
    finally {
        if ($null -ne $document) {
            $document.Dispose()
        }
    }
}

function Get-SqlDependencyBudgetViolations {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.IEnumerable]$Rows,
        [Parameter(Mandatory = $true)]
        [int]$Budget
    )

    if ($Budget -lt 1) {
        throw "Budget must be at least 1. Got $Budget."
    }

    $violations = [System.Collections.Generic.List[object]]::new()
    foreach ($row in $Rows) {
        if ($null -eq $row) {
            throw 'Query row is null.'
        }

        $count = [long]0
        if (-not [long]::TryParse([string]$row.sqlCount, [ref]$count)) {
            throw 'Query row is missing integer sqlCount.'
        }

        if ($count -gt $Budget) {
            $violations.Add($row)
        }
    }

    return $violations
}

function New-SqlDependencyBudgetDeployment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroup,
        [Parameter(Mandatory = $true)]
        [string]$AppInsightsName,
        [Parameter(Mandatory = $true)]
        [string]$SubscriptionId,
        [Parameter(Mandatory = $true)]
        [string]$Query,
        [Parameter(Mandatory = $true)]
        [string]$WindowSize,
        [string]$Location = '',
        [string]$AlertName = 'sql-dependency-count-budget',
        [int]$Severity = 3,
        [string]$EvaluationFrequency = 'PT15M',
        [string]$ActionGroupId = '',
        [bool]$Enabled = $true,
        [bool]$SkipQueryValidation = $false
    )

    Test-SqlDependencyBudgetName -Name $ResourceGroup -Label 'Resource group'
    Test-SqlDependencyBudgetName -Name $AppInsightsName -Label 'App Insights name'
    Test-SqlDependencyBudgetName -Name $AlertName -Label 'Alert name'
    if ($SubscriptionId -notmatch '^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$') {
        throw 'SubscriptionId must be a GUID.'
    }

    if ($EvaluationFrequency -notmatch '^PT[1-9][0-9]*(M|H)$') {
        throw "EvaluationFrequency '$EvaluationFrequency' must look like PT15M or PT1H."
    }

    $windowMinutes = ConvertTo-DurationMinutes -Duration $WindowSize
    $evaluationMinutes = ConvertTo-DurationMinutes -Duration $EvaluationFrequency
    if ($evaluationMinutes -gt $windowMinutes) {
        throw "EvaluationFrequency $EvaluationFrequency is longer than window $WindowSize."
    }

    if ($Severity -lt 0 -or $Severity -gt 4) {
        throw "Severity must be from 0 through 4. Got $Severity."
    }

    if (-not [string]::IsNullOrWhiteSpace($ActionGroupId) -and
        $ActionGroupId -notmatch '^/subscriptions/[^/]+/resourceGroups/[^/]+/providers/microsoft\.insights/actionGroups/[^/]+$') {
        throw 'ActionGroupId must be a Microsoft.Insights/actionGroups resource id.'
    }

    $actionGroupIds = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($ActionGroupId)) {
        $actionGroupIds.Add($ActionGroupId)
    }

    $resourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Insights/components/$AppInsightsName"
    return [pscustomobject]@{
        ResourceId = $resourceId
        WindowSize = $WindowSize
        Parameters = [ordered]@{
            appInsightsName              = @{ value = $AppInsightsName }
            appInsightsResourceGroupName = @{ value = $ResourceGroup }
            subscriptionId               = @{ value = $SubscriptionId }
            query                        = @{ value = $Query }
            location                     = @{ value = $Location }
            alertName                    = @{ value = $AlertName }
            severity                     = @{ value = $Severity }
            evaluationFrequency          = @{ value = $EvaluationFrequency }
            windowSize                   = @{ value = $WindowSize }
            actionGroupIds               = @{ value = $actionGroupIds }
            enabled                      = @{ value = $Enabled }
            skipQueryValidation          = @{ value = $SkipQueryValidation }
        }
    }
}
