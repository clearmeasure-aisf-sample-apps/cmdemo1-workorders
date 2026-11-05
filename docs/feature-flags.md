# Feature flags

`GET /api/features/flags` and `GET /api/diagnostics` report one runtime map. The map starts from the code defaults in `FeatureFlagsCatalog.All` (`src/UI/Api/FeatureFlagsCatalog.cs`). The `FeatureFlags` configuration section overrides a known name. Unknown names are ignored. A value that is not a boolean leaves the code default. Add or remove a flag only in `FeatureFlagsCatalog`.

`FeatureFlagsController` and `DiagnosticsController` both call `IFeatureFlagRuntime.Snapshot` (`ConfigurationFeatureFlagRuntime`). They do not read the database and do not go through MediatR. The actions return JSON (`application/json`) with status 200.

## Routes

| Method | Route |
| --- | --- |
| GET | `/api/features/flags` |
| GET | `/api/v1.0/features/flags` |
| GET | `/api/diagnostics` |
| GET | `/api/v1.0/diagnostics` |

The flags routes return one JSON object. Each property name is a catalog key and each value is a boolean. The diagnostics routes return that same object on `featureFlags`.

With no configuration overlay, the body is the code defaults:

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

## Flags

| Flag | Default | Effect |
| --- | --- | --- |
| `SampleFeatureA` | true | Returned by the endpoints. No screen reads it. |
| `SampleFeatureB` | false | Returned by the endpoints. No screen reads it. |
| `ShowPriorityBadges` | true | When enabled, the work order search page shows a High badge on a work order whose priority is High. |
| `BuildingManagement` | false | Buildings and rooms administration. Off until an environment turns it on. |
| `VendorManagement` | false | Vendor administration. Teams stay available. Off until an environment turns it on. |

`BuildingManagement` and `VendorManagement` are the gates for Buildings & Rooms and for vendors. When a gate is off, the intended effect is: the Buildings & Rooms navigation link is absent and `/admin/buildings` does not load building data; the vendors section is absent and vendor list/create calls, including MCP `list-vendors` and `create-vendor`, fail closed. Work-order building text, the Open by Building report, the waiting-on-vendor work-order step, and Teams are not gated. Enforcement of that hide-and-fail-closed behavior is the Building and Vendor gate work. This runtime is the on/off value those gates read. `GET /api/features/flags` and diagnostics already report it.

## Environment defaults

| Place | `BuildingManagement` | `VendorManagement` |
| --- | --- | --- |
| Code (`FeatureFlagsCatalog.All`) | false | false |
| `appsettings.json` (image default: uat and prod until flipped) | false | false |
| `appsettings.Development.json` (local process and the acceptance host) | true | true |
| Platform tdd | ON | ON |
| Platform uat and prod | OFF | OFF until flipped |

Platform tdd sets `FeatureFlags__BuildingManagement` and `FeatureFlags__VendorManagement` to `true`. Platform uat and prod leave those values false until an operator flips them. Flipping configuration does not require a new image.

The acceptance host sets `ASPNETCORE_ENVIRONMENT` to `Development`, so it loads `appsettings.Development.json` and runs with both gates ON. That matches tdd. The host does not stub `IFeatureFlags`. Existing Buildings and Vendors checks run against the ON state. Flag-off of the shared map (both endpoints report false and match) is an integration test. Flag-off of navigation, the buildings deep link, and MCP or API calls is the Building and Vendor gate work, not a second acceptance host.

## Access

The flags and diagnostics routes use the API sliding-window rate limit (`ApiSlidingWindow`). The default limit is 100 requests per 60 seconds.

These routes are not public API paths. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives `401`.

The Blazor client loads `api/features/flags` once. If that load fails, `ShowPriorityBadges` is treated as enabled and every other name is treated as disabled. A successful body that omits a name is treated as disabled for that name.
