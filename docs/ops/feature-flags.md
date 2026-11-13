# Feature flags

Flag values are evaluated at runtime. The flag names are fixed in `FeatureFlagNames`; each value comes from the first source that knows it: Octopus Feature Toggles (when `Octopus__FeatureFlags__ClientIdentifier` is set), then the `FeatureFlags` configuration section, then false. Full design: [Feature flags](../feature-flags.md).

## Flags

| Flag | Default without Octopus | What it changes |
| --- | --- | --- |
| `ShowPriorityBadges` | true (Octopus slug `showprioritybadges`; on in tdd, uat and prod) | When enabled, a High-priority work order shows a colored High badge on the search list and on the board. Appsettings stays true as the fallback when Octopus is unset or cannot resolve the flag. |
| `BuildingManagement` | false | When off, the Buildings & Rooms nav link and page are hidden, and `BuildingRoomSummaryQuery` and `WorkOrderBuildingsQuery` are denied. The work-order building text stays. |
| `VendorManagement` | false | When off, the Vendors section, `VendorsQuery`, `CreateVendorCommand`, and MCP `list-vendors` / `create-vendor` are denied. Teams and waiting-on-vendor stay available. |
| `SampleFeatureA` | false (true in Development) | The flags endpoint returns it. It changes no screen. |
| `SampleFeatureB` | false | The flags endpoint returns it. It changes no screen. |

## Turning a flag on or off

- **With Octopus Feature Toggles:** the UI container needs `Octopus__FeatureFlags__ClientIdentifier` (from the Octopus variable `Octopus.FeatureToggles.ClientIdentifier`). Create a toggle whose slug is the kebab-case flag name (`show-priority-badges`, `building-management`), the compact lowercase Octopus slug (`showprioritybadges`, `buildingmanagement`), or the flag name (`ShowPriorityBadges`, `BuildingManagement`); the evaluator tries those in that order. Set the toggle per environment. The app picks up a flip within the provider's refresh interval; no redeploy.
- **Without Octopus:** set the configuration value, for example environment variable `FeatureFlags__VendorManagement=true`, and restart the app. `ShowPriorityBadges` remains on from appsettings when Octopus is unset.

A missing client identifier or an unknown flag reads as false, except `ShowPriorityBadges`, which stays on from `FeatureFlags:ShowPriorityBadges` in appsettings when the provider cannot resolve it.

## Environment defaults

| Place | `ShowPriorityBadges` | `BuildingManagement` | `VendorManagement` |
| --- | --- | --- | --- |
| No Octopus client and no configuration value | false | false | false |
| `src/UI/Server/appsettings.json` (image; Octopus fallback) | true | false | false |
| `src/UI/Server/appsettings.Development.json` (local and acceptance host) | true | true | true |
| Platform tdd | ON (`showprioritybadges`) | ON | ON |
| Platform uat and prod | ON (`showprioritybadges`) | OFF until flipped | OFF until flipped |

The acceptance host uses Development, so both gates are ON. That matches platform tdd. The host does not stub `IFeatureFlags`. `BuildingRoomSummaryQuery` and `WorkOrderBuildingsQuery` require `BuildingManagement`. `VendorsQuery` and `CreateVendorCommand` require `VendorManagement`. `GET /api/diagnostics` returns the same map as the flags endpoint.

## How to read the current values

Send GET to either route:

- `/api/features/flags`
- `/api/v1.0/features/flags`

The response is a JSON object. Each property name is a flag name and each value is a boolean. `GET /api/diagnostics` returns the same map as `featureFlags`.

```bash
curl -sS "https://<host>/api/features/flags"
```

Both routes use the API sliding-window rate limit. When that limit is enabled, the default is 100 requests per 60 seconds.

These routes are not public API leaves. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives 401. Send the key in that header.

## What a disabled flag does

A request tagged `[RequiresFeature]` for a disabled flag is rejected before its handler runs: the single API answers `403` problem details naming the feature, and an MCP tool returns error text naming the feature.
