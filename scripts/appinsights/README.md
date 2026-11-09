# SQL dependency budget (tdd Application Insights)

Deterministic check for request operations that run too many SQL commands. There is no AI step. The motivating defect is a per-employee `Bus.Send` N+1 on Teams & Vendors (#1490, fixed in #1491).

The query joins `requests` to `dependencies` on `operation_Id` and keeps operations whose `sqlCount` is greater than a budget. The default budget is **20**. `sqlCount` equal to the budget is not a failure.

## Where the rows come from

`src/ChurchBulletin.ServiceDefaults/Extensions.cs` turns on the Azure Monitor exporter when the platform sets `APPLICATIONINSIGHTS_CONNECTION_STRING`. SqlClient and EF Core spans are exported as Application Insights dependencies. HTTP server spans are `requests`. Both carry `operation_Id`. The query counts dependency `type` `SQL` and `SQL Server`, which is what that exporter writes for those spans.

SqlClient and EF Core instrumentation are both enabled, so one command can be stored twice. Raise the budget if that double count is the steady state. The script does not dedupe.

## Which Application Insights resource

The live tdd component is not named by the legacy ARM defaults in `src/Core/ContainerEnvironmentARM.json` (`defaultoinionmauiappins`) and not by the `bootcamp-tdd` resource group in `src/pure-azdo-pipeline.yml`.

`arch/runtime-view.md` points at the platform topology. `tdd-app-insights.json` records the tdd link from that file as of `generated` `2026-11-05T22:16:18Z`:

| Field | Value |
| --- | --- |
| Component | `appi-cmdemo1-tdd` |
| Resource group | `rg-cmdemo1-nonprod` |
| Type | `Microsoft.Insights/components` |
| Source | `https://blue-bush-041023510.1.azurestaticapps.net/topology.json` |

The subscription id is not stored here. Use the repository variable `AZURE_SUBSCRIPTION_ID` already used by `.github/workflows/release.yml`. Pass `-ResourceGroup` and `-AppInsightsName` to query a different component. The Bicep template has no environment name of its own.

## Run the query

```powershell
pwsh -NoProfile -File ./scripts/appinsights/Invoke-SqlDependencyBudget.ps1 -SelfTest
pwsh -NoProfile -File ./scripts/appinsights/Invoke-SqlDependencyBudget.ps1 -UseDiscoveredTddTarget -SubscriptionId $env:AZURE_SUBSCRIPTION_ID -RequireAzure
```

`-SelfTest` does not call Azure. Without a target, or without `az` logged in, the script prints a skip line and exits 0. `-RequireAzure` turns that skip into exit 2.

| Exit | Meaning |
| --- | --- |
| 0 | No operations over budget (including a query that returns no rows), the scheduled query rule matches, or the run skipped because the target or credentials are absent |
| 1 | At least one operation has `sqlCount` greater than the budget, or the scheduled query rule is missing or drifted |
| 2 | Bad argument, query command failed, the rule could not be read (including authorization), or credentials were required and absent |

Lookback is one of `15m`, `30m`, `1h` (default), `6h`, `12h`, `1d`.

## Scheduled CI job

`.github/workflows/tdd-sql-dependency-budget.yml` runs the self-test on pull requests that touch this folder, and runs the query every hour at minute 17 (and on `workflow_dispatch`). Login matches release: `azure/login@v3` with `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_CLIENT_ID_ACR_PUSH`. Set `AZURE_CLIENT_ID_APP_INSIGHTS` to use a different application instead of the ACR-push identity.

The query step is skipped, and the job stays green, when any of tenant, subscription, or both client ids is empty. After the gate says the variables are present, both Azure steps are started with `-RequireAzure`, so a login, query, or alert-read failure fails the job.

The alert step calls `-CheckAlertConfiguration` with budget **20**, lookback **1h** (window `PT1H`), evaluation **PT15M**, rule `sql-dependency-count-budget`, and action group `ag-cmdemo1-tdd-incident`. Those values are fixed. A `workflow_dispatch` that overrides the data-query budget does not loosen the rule check. The step fails when the rule is missing, disabled, its KQL is not the rendered query, its window or evaluation frequency differs, its single criterion is not count greater than 0, its scope is not `appi-cmdemo1-tdd`, or that action group is not in its actions. The comparison is `Get-SqlDependencyBudgetAlertDrift` in `SqlDependencyBudget.ps1`. `SqlDependencyBudget.Tests.ps1` feeds it fake rule JSON for each of those cases and does not call Azure.

The read is `az resource show` for `Microsoft.Insights/scheduledQueryRules` at API `2023-03-15-preview`. The action group id is a property of the rule, so the check does not read the action group resource. The signed-in identity still needs **Monitoring Reader** on the rule itself. The workflow does not assign that role. When Azure returns an authorization error, the step exits 2 with this command and does not print the Azure error body:

```bash
az role assignment create --assignee-object-id "$(az identity list --query "[?name=='id-cmdemo1-acr-push'].principalId | [0]" --output tsv)" --assignee-principal-type ServicePrincipal --role "Monitoring Reader" --scope "/subscriptions/4a4dfa6d-d434-4b9e-8a88-63bbf61cfb69/resourceGroups/rg-cmdemo1-nonprod/providers/Microsoft.Insights/scheduledQueryRules/sql-dependency-count-budget"
```

That scope is the rule in subscription `4a4dfa6d-d434-4b9e-8a88-63bbf61cfb69`. It is the narrowest resource the read touches. Run it as an identity that may assign roles; the hourly job only reads.

The workflow does not set the GitHub environment `release`. Naming that environment would write a GitHub deployment on every run. Release keeps its own OIDC subject on that environment.

## Scheduled query alert

`sql-dependency-budget-alert.bicep` is a `Microsoft.Insights/scheduledQueryRules` rule scoped to the component resource id parameters. It fires when the query returns one or more rows (`Count` greater than 0). The default evaluation is `PT15M` and the default window is `PT1H`, matching lookback `1h`.

Preview the parameters without calling Azure:

```powershell
pwsh -NoProfile -File ./scripts/appinsights/Register-SqlDependencyBudgetAlert.ps1 -UseDiscoveredTddTarget -SubscriptionId $env:AZURE_SUBSCRIPTION_ID
```

That command prints `SQL dependency budget alert was not applied` and exits 0. CI never runs it with `-Apply`.

## Manual steps before this is live in tdd

Nothing in this folder is applied to Azure by the pull request.

1. Make `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_CLIENT_ID_ACR_PUSH` (or `AZURE_CLIENT_ID_APP_INSIGHTS`) repository variables. Release reads those three from the `release` environment, which this workflow does not use. If they exist only on that environment, the hourly job skips.
2. Add a federated credential on that application for the subject this schedule uses: `repo:clearmeasure-aisf-sample-apps/cmdemo1-workorders:ref:refs/heads/master`. The credential already used by release is for the `release` environment and does not cover this workflow.
3. Grant that application **Monitoring Reader** on `appi-cmdemo1-tdd` (resource group `rg-cmdemo1-nonprod`). The ACR-push identity can push images without being allowed to query Application Insights. Grant **Monitoring Reader** on the scheduled query rule as well, with the command in the section above. A role on the component does not cover the rule.
4. Run the workflow with `workflow_dispatch` and confirm the alert check exits 0 and the query step exits 0 (or exits 1 because a real operation is over the budget).
5. To create the Azure alert, from an identity that may deploy to that resource group:

```powershell
pwsh -NoProfile -File ./scripts/appinsights/Register-SqlDependencyBudgetAlert.ps1 -Apply -UseDiscoveredTddTarget -SubscriptionId $env:AZURE_SUBSCRIPTION_ID -Location <component region>
```

`-Location` is the component's region from the portal overview. The script does not guess it. Add `-ActionGroupId` with a `Microsoft.Insights/actionGroups` resource id to notify someone. Without it the rule exists and fires in Azure Monitor with no action group.
