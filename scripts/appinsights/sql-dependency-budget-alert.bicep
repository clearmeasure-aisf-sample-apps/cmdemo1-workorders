targetScope = 'resourceGroup'

@description('Application Insights component name. No environment default is baked in.')
param appInsightsName string

@description('Resource group that contains the Application Insights component.')
param appInsightsResourceGroupName string

@description('Subscription id of the Application Insights component.')
param subscriptionId string

@description('KQL returning one row per request operation over the SQL dependency budget.')
param query string

@description('Region of the scheduled query rule. Use the component region. This template does not guess it.')
param location string

@description('Scheduled query rule name.')
param alertName string = 'sql-dependency-count-budget'

@minValue(0)
@maxValue(4)
param severity int = 3

@description('How often the rule evaluates. Must be less than or equal to windowSize.')
param evaluationFrequency string = 'PT15M'

@description('Query window. Pair with the KQL lookback (1h with PT1H).')
param windowSize string = 'PT1H'

@description('Action group resource ids. Empty creates the rule with no notifications.')
param actionGroupIds array = []

param enabled bool = true

@description('Set true only to skip Azure query validation at deploy time.')
param skipQueryValidation bool = false

var appInsightsResourceId = resourceId(subscriptionId, appInsightsResourceGroupName, 'Microsoft.Insights/components', appInsightsName)

resource sqlDependencyBudget 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: alertName
  location: location
  properties: {
    displayName: 'SQL dependency count per request'
    description: 'Fires when a request operation records more SQL dependencies than the budget. The query joins requests to dependencies on operation_Id.'
    severity: severity
    enabled: enabled
    evaluationFrequency: evaluationFrequency
    scopes: [
      appInsightsResourceId
    ]
    windowSize: windowSize
    criteria: {
      allOf: [
        {
          query: query
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    skipQueryValidation: skipQueryValidation
    actions: {
      actionGroups: actionGroupIds
    }
  }
}

output scheduledQueryRuleId string = sqlDependencyBudget.id
