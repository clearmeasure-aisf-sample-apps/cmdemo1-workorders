# Feature flags

Feature flags are evaluated at runtime on the server and enforced in one place: the MediatR bus. The catalog of flag names is fixed in code (`FeatureFlagNames` in `src/Core/FeatureFlags/FeatureFlagNames.cs`); the values come from Octopus Feature Toggles, then configuration, then false.

## Flags

| Flag | Default in `appsettings.json` | Effect |
| --- | --- | --- |
| `ShowPriorityBadges` | true | When enabled, the work order search page shows a High badge on a work order whose priority is High. |
| `BuildingManagement` | false | Reserved for the Buildings & Rooms admin surface and its building-room summary reads. No request is tagged yet. |
| `VendorManagement` | false | Reserved for the vendor directory (list and create, UI and MCP). No request is tagged yet. |
| `SampleFeatureA` | false (true in Development) | Returned by the endpoints. No screen reads it. |
| `SampleFeatureB` | false | Returned by the endpoints. No screen reads it. |

To add a flag, add a constant to `FeatureFlagNames` and to `FeatureFlagNames.All`, and give it a default in the `FeatureFlags` section of `src/UI/Server/appsettings.json` (and `src/McpServer/appsettings.json` if MCP uses it).

## Where values come from

`RuntimeFeatureFlagEvaluator` (`src/DataAccess/FeatureFlags/`) answers every evaluation. The first source that knows the flag wins:

1. **Octopus Feature Toggles through OpenFeature.** Used only when the configuration key `Octopus:FeatureFlags:ClientIdentifier` has a value. In a container that is the environment variable `Octopus__FeatureFlags__ClientIdentifier`, injected at deploy time from the Octopus variable `Octopus.FeatureToggles.ClientIdentifier`. The evaluator looks up the toggle slug as the kebab-case flag name (`building-management`), then the flag name itself (`BuildingManagement`); slugs match ignoring case. The Octopus provider caches the toggle manifest and refreshes it in the background, so a toggle flip shows up without a new image.
2. **The `FeatureFlags` configuration section**, for example `FeatureFlags:BuildingManagement` (environment variable `FeatureFlags__BuildingManagement`). A value that is not `true` or `false` counts as false.
3. **false.** A missing client identifier and an unknown flag both end here, so new flags fail closed.

The client identifier is not a secret, but it is environment-specific: never commit one.

## Enforcing a flag on a request

Put `[RequiresFeature(FeatureFlagNames.X)]` on the request record (command or query). `FeatureFlagBehavior<TRequest, TResponse>` (`src/UI.Shared/FeatureFlagBehavior.cs`) runs in the MediatR pipeline of both hosts, registered with `AddOpenBehavior` in `src/UI/Server/UIServiceRegistry.cs` and `src/McpServer/McpServiceRegistry.cs`. When any required flag is off it throws `FeatureDisabledException` before the handler runs. Apply the attribute more than once to require several flags.

Because Blazor WebAssembly and the Worker reach the server bus through the single API, and MCP tools call the bus in-process, one attribute gates every caller:

| Caller | What a disabled flag returns |
| --- | --- |
| Single API (`POST /api/blazor-wasm-single-api`) | `403` with an `application/problem+json` body: `title` "Feature disabled", `detail` naming the feature and request, and a `feature` extension with the flag name. The Blazor client's remoting call throws. |
| MCP tool | A tool error (`isError: true`) whose text names the disabled feature and request (`FeatureDisabledToolFilter`). |
| In-process `IBus.Send` | `FeatureDisabledException`. |

Only requests sent through `Send` are gated; `Publish` (events) is not.

## Reading flags

`FeatureFlagsQuery` (`src/Core/Queries/FeatureFlagsQuery.cs`) returns `FeatureFlagsResult` with every catalog name and its runtime value. It is remotable, so the Blazor client sends it over the single API like any other query. Use it to hide UI for a disabled feature; enforcement stays in the behavior.

- **Blazor:** inject `IFeatureFlags` (`BusFeatureFlags`). It sends `FeatureFlagsQuery` once per scope. If the query fails, `ShowPriorityBadges` is treated as enabled and every other name as disabled. A name missing from the result is disabled.
- **HTTP:** `GET /api/features/flags` (and `/api/v1.0/features/flags`) returns the same map as one JSON object, through the bus. `GET /api/diagnostics` returns it as `featureFlags`. Both read the same runtime source.

```json
{
  "SampleFeatureA": false,
  "SampleFeatureB": false,
  "ShowPriorityBadges": true,
  "BuildingManagement": false,
  "VendorManagement": false
}
```

```bash
curl -sS "https://<host>/api/features/flags"
```

## Access

Both flag routes use the API sliding-window rate limit (`ApiSlidingWindow`). The default limit is 100 requests per 60 seconds.

`features/flags` is not a public API path. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives `401`.
