# Feature flags

Flag values are evaluated at runtime. The flag names are fixed in `FeatureFlagNames`; each value comes from the first source that knows it: Octopus Feature Toggles (when `Octopus__FeatureFlags__ClientIdentifier` is set), then the `FeatureFlags` configuration section, then false. Full design: [Feature flags](../feature-flags.md).

## Flags

| Flag | Default without Octopus | What it changes |
| --- | --- | --- |
| `ShowPriorityBadges` | true | When enabled, a High-priority work order shows a colored High badge on the search list and on the board. |
| `BuildingManagement` | false | Reserved for Buildings & Rooms admin; no request is gated yet. |
| `VendorManagement` | false | Reserved for the vendor directory; no request is gated yet. |
| `SampleFeatureA` | false (true in Development) | The flags endpoint returns it. It changes no screen. |
| `SampleFeatureB` | false | The flags endpoint returns it. It changes no screen. |

## Turning a flag on or off

- **With Octopus Feature Toggles:** the UI container needs `Octopus__FeatureFlags__ClientIdentifier` (from the Octopus variable `Octopus.FeatureToggles.ClientIdentifier`). Create a toggle whose slug is the kebab-case flag name (`building-management`) or the flag name, and set it per environment. The app picks up a flip within the provider's refresh interval; no redeploy.
- **Without Octopus:** set the configuration value, for example environment variable `FeatureFlags__VendorManagement=true`, and restart the app.

A missing client identifier or an unknown flag reads as false.

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
