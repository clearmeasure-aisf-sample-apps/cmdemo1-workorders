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
        if ($tablesProperty.ValueKind -eq [System.Text.Json.JsonValueKind]::Null -or $tablesProperty.GetArrayLength() -eq 0) {
            Write-Output -NoEnumerate $result
            return
        }

        $table = $tablesProperty[0]
        $columns = [System.Collections.Generic.List[string]]::new()
        foreach ($column in $table.GetProperty('columns').EnumerateArray()) {
            $columns.Add($column.GetProperty('name').GetString())
        }

        $rowsElement = [System.Text.Json.JsonElement]::Parse('null')
        $hasRows = $table.TryGetProperty('rows', [ref]$rowsElement)
        if ($hasRows -and $rowsElement.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
            foreach ($row in $rowsElement.EnumerateArray()) {
                $item = [ordered]@{}
                for ($index = 0; $index -lt $columns.Count; $index++) {
                    $item[$columns[$index]] = ConvertFrom-JsonElementValue -Element $row[$index]
                }

                $result.Add([pscustomobject]$item)
            }
        }

        # An empty List enumerates to $null on return. Keep the list so the caller can pass it on.
        Write-Output -NoEnumerate $result
        return
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
        [AllowNull()]
        [AllowEmptyCollection()]
        [System.Collections.IEnumerable]$Rows,
        [Parameter(Mandatory = $true)]
        [int]$Budget
    )

    if ($Budget -lt 1) {
        throw "Budget must be at least 1. Got $Budget."
    }

    $violations = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $Rows) {
        Write-Output -NoEnumerate $violations
        return
    }

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

    Write-Output -NoEnumerate $violations
    return
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

function Get-NormalizedSqlDependencyBudgetQuery {
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Query
    )

    if ([string]::IsNullOrEmpty($Query)) {
        return ''
    }

    $normalized = $Query.Replace("`r`n", "`n").Replace("`r", "`n")
    if ($normalized.Length -gt 0 -and [int]$normalized[0] -eq 0xFEFF) {
        $normalized = $normalized.Substring(1)
    }

    $lines = $normalized.Split([char]10)
    $trimmed = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        $trimmed.Add($line.TrimEnd())
    }

    return ($trimmed -join "`n").Trim()
}

function ConvertTo-SqlDependencyBudgetJsonValue {
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.Json.JsonElement]$Element
    )

    switch ($Element.ValueKind) {
        'Object' {
            $map = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
            foreach ($property in $Element.EnumerateObject()) {
                $map[$property.Name] = ConvertTo-SqlDependencyBudgetJsonValue -Element $property.Value
            }

            return ,$map
        }
        'Array' {
            $list = [System.Collections.Generic.List[object]]::new()
            foreach ($item in $Element.EnumerateArray()) {
                $list.Add((ConvertTo-SqlDependencyBudgetJsonValue -Element $item))
            }

            return ,$list
        }
        default {
            return ConvertFrom-JsonElementValue -Element $Element
        }
    }
}

function ConvertFrom-SqlDependencyBudgetAlertJson {
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$RuleJson
    )

    if ([string]::IsNullOrWhiteSpace($RuleJson)) {
        return $null
    }

    $document = $null
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($RuleJson)
        $value = ConvertTo-SqlDependencyBudgetJsonValue -Element $document.RootElement
        if ($value -isnot [System.Collections.IDictionary]) {
            throw 'Scheduled query rule JSON is not an object.'
        }

        return ,$value
    }
    catch [System.Text.Json.JsonException] {
        throw 'Scheduled query rule JSON is not valid.'
    }
    finally {
        if ($null -ne $document) {
            $document.Dispose()
        }
    }
}

function ConvertTo-SqlDependencyBudgetItemList {
    param($Value)

    $items = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $Value) {
        return ,$items
    }

    if ($Value -is [string] -or $Value -is [System.Collections.IDictionary]) {
        $items.Add($Value)
        return ,$items
    }

    if ($Value -is [System.Collections.IEnumerable]) {
        foreach ($item in $Value) {
            if ($null -ne $item) {
                $items.Add($item)
            }
        }

        return ,$items
    }

    $items.Add($Value)
    return ,$items
}

function Get-SqlDependencyBudgetMapValue {
    param(
        $Map,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $dictionary = $Map -as [System.Collections.IDictionary]
    if ($null -eq $dictionary) {
        return $null
    }

    foreach ($key in @($dictionary.Keys)) {
        if ([string]::Equals([string]$key, $Name, [StringComparison]::OrdinalIgnoreCase)) {
            return ,$dictionary[$key]
        }
    }

    return $null
}

function Test-SqlDependencyBudgetAlertEnabledValue {
    param($Value)

    if ($Value -is [bool]) {
        return $Value
    }

    if ($null -eq $Value) {
        return $false
    }

    $text = [string]$Value
    return [string]::Equals($text, 'true', [StringComparison]::OrdinalIgnoreCase) -or $text -eq '1'
}

function Test-SqlDependencyBudgetZeroThreshold {
    param($Value)

    if ($null -eq $Value) {
        return $false
    }

    $number = [double]::NaN
    if ($Value -is [string]) {
        $parsed = [double]::NaN
        if (-not [double]::TryParse($Value, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
            return $false
        }

        $number = $parsed
    }
    else {
        try {
            $number = [double]$Value
        }
        catch {
            return $false
        }
    }

    if ([double]::IsNaN($number) -or [double]::IsInfinity($number)) {
        return $false
    }

    return [Math]::Abs($number) -lt 0.000000001
}

function Get-SqlDependencyBudgetAlertReaderRoleCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Scope
    )

    # Single-quoted so the $(az ...) lookup stays literal text for an operator to run.
    $lookup = 'az identity list --query "[?name==''id-cmdemo1-acr-push''].principalId | [0]" --output tsv'
    return 'az role assignment create --assignee-object-id "$(' + $lookup + ')" --assignee-principal-type ServicePrincipal --role "Monitoring Reader" --scope "' + $Scope + '"'
}

function Get-SqlDependencyBudgetActionGroupIds {
    param($Properties)

    $actions = Get-SqlDependencyBudgetMapValue -Map $Properties -Name 'actions'
    $groups = Get-SqlDependencyBudgetMapValue -Map $actions -Name 'actionGroups'
    if ($null -eq $groups) {
        $groups = Get-SqlDependencyBudgetMapValue -Map $Properties -Name 'actionGroups'
    }

    $ids = [System.Collections.Generic.List[string]]::new()
    foreach ($group in (ConvertTo-SqlDependencyBudgetItemList $groups)) {
        $id = $null
        if ($group -is [string]) {
            $id = $group
        }
        elseif ($group -is [System.Collections.IDictionary]) {
            foreach ($key in @('actionGroupId', 'id', 'resourceId')) {
                $candidate = Get-SqlDependencyBudgetMapValue -Map $group -Name $key
                if (-not [string]::IsNullOrWhiteSpace([string]$candidate)) {
                    $id = [string]$candidate
                    break
                }
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($id)) {
            $ids.Add($id.Trim())
        }
    }

    return ,$ids
}

function Test-SqlDependencyBudgetActionGroupId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ActionGroupId,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedName,
        [string]$ExpectedSubscriptionId = ''
    )

    $trimmed = $ActionGroupId.Trim().TrimEnd('/')
    if ($trimmed -notmatch '(?i)^/subscriptions/([^/]+)/resourceGroups/[^/]+/providers/microsoft\.insights/actionGroups/([^/]+)$') {
        return $false
    }

    if (-not [string]::Equals($Matches[2], $ExpectedName, [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedSubscriptionId) -and
        -not [string]::Equals($Matches[1], $ExpectedSubscriptionId, [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    return $true
}

# Compares a scheduled query rule payload to the rendered alert. Does not call Azure.
function Get-SqlDependencyBudgetAlertDrift {
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$RuleJson,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedQuery,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedWindowSize,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedEvaluationFrequency,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedActionGroupName,
        [string]$ExpectedScope = '',
        [string]$ExpectedSubscriptionId = '',
        [string]$AlertName = 'sql-dependency-count-budget'
    )

    $drifts = [System.Collections.Generic.List[string]]::new()
    $document = ConvertFrom-SqlDependencyBudgetAlertJson -RuleJson $RuleJson
    if ($null -eq $document) {
        $drifts.Add("Scheduled query rule '$AlertName' is missing.")
        Write-Output -NoEnumerate $drifts
        return
    }

    $properties = Get-SqlDependencyBudgetMapValue -Map $document -Name 'properties'
    if ($properties -isnot [System.Collections.IDictionary]) {
        $properties = $document
    }

    if (-not (Test-SqlDependencyBudgetAlertEnabledValue (Get-SqlDependencyBudgetMapValue -Map $properties -Name 'enabled'))) {
        $drifts.Add("Scheduled query rule '$AlertName' is disabled.")
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedScope)) {
        $scopes = ConvertTo-SqlDependencyBudgetItemList (Get-SqlDependencyBudgetMapValue -Map $properties -Name 'scopes')
        $scopeFound = $false
        foreach ($scope in $scopes) {
            $actualScope = ([string]$scope).Trim().TrimEnd('/')
            if ([string]::Equals($actualScope, $ExpectedScope.Trim().TrimEnd('/'), [StringComparison]::OrdinalIgnoreCase)) {
                $scopeFound = $true
                break
            }
        }

        if (-not $scopeFound) {
            $drifts.Add("Scheduled query rule '$AlertName' scopes do not include '$ExpectedScope'.")
        }
    }

    $window = [string](Get-SqlDependencyBudgetMapValue -Map $properties -Name 'windowSize')
    if (-not [string]::Equals($window.Trim(), $ExpectedWindowSize.Trim(), [StringComparison]::OrdinalIgnoreCase)) {
        $drifts.Add("Scheduled query rule '$AlertName' windowSize is '$window'; expected '$ExpectedWindowSize'.")
    }

    $frequency = [string](Get-SqlDependencyBudgetMapValue -Map $properties -Name 'evaluationFrequency')
    if (-not [string]::Equals($frequency.Trim(), $ExpectedEvaluationFrequency.Trim(), [StringComparison]::OrdinalIgnoreCase)) {
        $drifts.Add("Scheduled query rule '$AlertName' evaluationFrequency is '$frequency'; expected '$ExpectedEvaluationFrequency'.")
    }

    $criteria = Get-SqlDependencyBudgetMapValue -Map $properties -Name 'criteria'
    $allOf = ConvertTo-SqlDependencyBudgetItemList (Get-SqlDependencyBudgetMapValue -Map $criteria -Name 'allOf')
    if ($allOf.Count -ne 1 -or $allOf[0] -isnot [System.Collections.IDictionary]) {
        $drifts.Add("Scheduled query rule '$AlertName' must have one criterion.")
    }
    else {
        $criterion = $allOf[0]
        $actualQuery = Get-NormalizedSqlDependencyBudgetQuery -Query ([string](Get-SqlDependencyBudgetMapValue -Map $criterion -Name 'query'))
        $expectedQuery = Get-NormalizedSqlDependencyBudgetQuery -Query $ExpectedQuery
        if (-not [string]::Equals($actualQuery, $expectedQuery, [StringComparison]::Ordinal)) {
            $drifts.Add("Scheduled query rule '$AlertName' query does not match the rendered KQL.")
        }

        $aggregation = [string](Get-SqlDependencyBudgetMapValue -Map $criterion -Name 'timeAggregation')
        if (-not [string]::Equals($aggregation.Trim(), 'Count', [StringComparison]::OrdinalIgnoreCase)) {
            $drifts.Add("Scheduled query rule '$AlertName' criterion timeAggregation is '$aggregation'; expected Count.")
        }

        $operatorName = [string](Get-SqlDependencyBudgetMapValue -Map $criterion -Name 'operator')
        if (-not [string]::Equals($operatorName.Trim(), 'GreaterThan', [StringComparison]::OrdinalIgnoreCase)) {
            $drifts.Add("Scheduled query rule '$AlertName' criterion operator is '$operatorName'; expected GreaterThan.")
        }

        $threshold = Get-SqlDependencyBudgetMapValue -Map $criterion -Name 'threshold'
        if (-not (Test-SqlDependencyBudgetZeroThreshold $threshold)) {
            $drifts.Add("Scheduled query rule '$AlertName' criterion threshold is '$threshold'; expected 0.")
        }
    }

    $actionGroupIds = Get-SqlDependencyBudgetActionGroupIds -Properties $properties
    $actionGroupFound = $false
    foreach ($actionGroupId in $actionGroupIds) {
        if (Test-SqlDependencyBudgetActionGroupId -ActionGroupId $actionGroupId -ExpectedName $ExpectedActionGroupName -ExpectedSubscriptionId $ExpectedSubscriptionId) {
            $actionGroupFound = $true
            break
        }
    }

    if (-not $actionGroupFound) {
        $drifts.Add("Scheduled query rule '$AlertName' actions do not include '$ExpectedActionGroupName'.")
    }

    Write-Output -NoEnumerate $drifts
    return
}

function Get-SqlDependencyBudgetAlertReadFailure {
    param(
        [Parameter(Mandatory = $true)]
        [int]$ExitCode,
        [AllowNull()]
        [AllowEmptyString()]
        [string]$ErrorText,
        [Parameter(Mandatory = $true)]
        [string]$Scope
    )

    if ($ExitCode -eq 0) {
        return $null
    }

    $text = [string]$ErrorText
    $authorization = $text -match '(?i)AuthorizationFailed' -or
        $text -match '(?i)does not have authorization' -or
        $text -match '(?i)\(403\)' -or
        $text -match '(?i)StatusCode:\s*403'
    if ($authorization) {
        $command = Get-SqlDependencyBudgetAlertReaderRoleCommand -Scope $Scope
        $message = ('Not authorized to read scheduled query rule ''{0}''. Assign Monitoring Reader to id-cmdemo1-acr-push on that rule: {1}' -f $Scope, $command)
        return [pscustomobject]@{
            Kind    = 'Authorization'
            Message = $message
        }
    }

    $notFoundText = $text -match '(?i)ResourceNotFound' -or
        $text -match '(?i)ResourceGroupNotFound' -or
        $text -match '(?i)was not found' -or
        $text -match '(?i)could not be found'
    if ($notFoundText -or ($ExitCode -eq 3 -and [string]::IsNullOrWhiteSpace($text))) {
        return [pscustomobject]@{
            Kind    = 'Missing'
            Message = "Scheduled query rule is missing at '$Scope'."
        }
    }

    $errorCode = ''
    if ($text -match '\(([A-Za-z][A-Za-z0-9]+)\)') {
        $errorCode = $Matches[1]
    }

    $detail = "Azure CLI exit $ExitCode"
    if (-not [string]::IsNullOrWhiteSpace($errorCode)) {
        $detail = "$detail, $errorCode"
    }

    return [pscustomobject]@{
        Kind    = 'Failed'
        Message = "Reading the scheduled query rule at '$Scope' failed ($detail)."
    }
}
