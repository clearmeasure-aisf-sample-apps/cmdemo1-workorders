# Feature flags

`FeatureFlagsCatalog` holds the code defaults. `IFeatureFlagRuntime` (`ConfigurationFeatureFlagRuntime`) is the runtime map: those defaults, then any known name overridden by the `FeatureFlags` configuration section. `GET /api/features/flags` and `GET /api/diagnostics` (`featureFlags`) return that same map. Unknown configuration names are ignored. A value that is not a boolean leaves the code default.

## Flags

| Flag | Default | What it changes |
| --- | --- | --- |
| `SampleFeatureA` | true | The flags endpoint returns it. It changes no screen. |
| `SampleFeatureB` | false | The flags endpoint returns it. It changes no screen. |
| `ShowPriorityBadges` | true | When enabled, a High-priority work order shows a colored High badge on the search list and on the board. |
| `BuildingManagement` | false | Buildings and rooms administration. Code, the image, and platform uat and prod stay off until flipped. Platform tdd and the local Development host are on. |
| `VendorManagement` | false | Vendor administration. Teams stay available. Same environment defaults as `BuildingManagement`. |

When `BuildingManagement` is off, the intended effect is that the Buildings & Rooms link is absent and `/admin/buildings` does not load building data. When `VendorManagement` is off, the intended effect is that the vendors section is absent and vendor list/create calls, including MCP `list-vendors` and `create-vendor`, fail closed. Work-order building text, the Open by Building report, the waiting-on-vendor step, and Teams are not gated. The Building and Vendor gates enforce that. This page's runtime is the value they read.

## Environment defaults

| Place | `BuildingManagement` | `VendorManagement` |
| --- | --- | --- |
| Code | false | false |
| `src/UI/Server/appsettings.json` (image; platform uat and prod until flipped) | false | false |
| `src/UI/Server/appsettings.Development.json` (local and acceptance host) | true | true |
| Platform tdd | ON (`FeatureFlags__BuildingManagement=true`, `FeatureFlags__VendorManagement=true`) | ON |
| Platform uat and prod | OFF until flipped | OFF until flipped |

The acceptance host uses Development, so both gates are ON. That matches tdd. The host does not stub `IFeatureFlags`. Flag-off navigation, deep links, and MCP or API fail-closed checks are the Building and Vendor gate work. Integration tests assert the shared map is off, and that diagnostics matches the flags endpoint, when configuration sets the gates false.

## How to read the catalog

Send GET to either flags route, or read `featureFlags` on diagnostics:

- `/api/features/flags`
- `/api/v1.0/features/flags`
- `/api/diagnostics`
- `/api/v1.0/diagnostics`

The flags response is a JSON object. Each property name is a catalog key and each value is a boolean. Diagnostics nests that object under `featureFlags`. With the code defaults above, and no configuration overlay, the flags body is:

```json
{
  "SampleFeatureA": true,
  "SampleFeatureB": false,
  "ShowPriorityBadges": true,
  "BuildingManagement": false,
  "VendorManagement": false
}
```

```bash
curl -sS "https://<host>/api/features/flags"
curl -sS "https://<host>/api/diagnostics"
```

Both route families use the API sliding-window rate limit. When that limit is enabled, the default is 100 requests per 60 seconds.

These routes are not public API leaves. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives 401. Send the key in that header.

The Blazor client loads `api/features/flags` once. If that load fails, `ShowPriorityBadges` is treated as enabled and every other name is treated as disabled.
