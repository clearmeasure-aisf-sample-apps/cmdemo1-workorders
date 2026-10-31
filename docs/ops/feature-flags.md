# Feature flags

`FeatureFlagsCatalog` is the in-memory catalog of runtime feature flags. Each entry is a flag name and its default enabled value. This page lists every flag in that catalog, what the default changes, and how to read the current values.

## Flags

| Flag | Default | What it changes |
| --- | --- | --- |
| `SampleFeatureA` | true | The flags endpoint returns it. It changes no screen. |
| `SampleFeatureB` | false | The flags endpoint returns it. It changes no screen. |
| `ShowPriorityBadges` | true | When enabled, a High-priority work order shows a colored High badge on the search list and on the board. |

## How to read the catalog

Send GET to either route:

- `/api/features/flags`
- `/api/v1.0/features/flags`

The response is a JSON object. Each property name is a catalog key and each value is a boolean. With the defaults above the body is:

```json
{
  "SampleFeatureA": true,
  "SampleFeatureB": false,
  "ShowPriorityBadges": true
}
```

```bash
curl -sS "https://<host>/api/features/flags"
```

Both routes use the API sliding-window rate limit. When that limit is enabled, the default is 100 requests per 60 seconds.

These routes are not public API leaves. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives 401. Send the key in that header.

The Blazor client loads `api/features/flags` once. If that load fails, `ShowPriorityBadges` is treated as enabled and every other name is treated as disabled.
