# Feature flags

Runtime feature flags are a static catalog. `GET /api/features/flags` returns every flag and whether it is enabled. The catalog is `src/UI/Api/FeatureFlagsCatalog.cs` (`FeatureFlagsCatalog.All`). Add or remove a flag only in that file.

`FeatureFlagsController` (`src/UI/Api/Controllers/FeatureFlagsController.cs`) serves the catalog. It does not read the database and does not go through MediatR. The action returns the dictionary as JSON (`application/json`) with status 200.

## Routes

| Method | Route |
| --- | --- |
| GET | `/api/features/flags` |
| GET | `/api/v1.0/features/flags` |

The second route is the same action under API version `1.0`.

The response is one JSON object. Each property name is a catalog key and each value is a boolean. With the catalog as checked in, the body is:

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

## Flags

| Flag | Default | Effect |
| --- | --- | --- |
| `SampleFeatureA` | true | Returned by the endpoint. No screen reads it. |
| `SampleFeatureB` | false | Returned by the endpoint. No screen reads it. |
| `ShowPriorityBadges` | true | When enabled, the work order search page shows a High badge on a work order whose priority is High. |

## Access

Both routes use the API sliding-window rate limit (`ApiSlidingWindow`). The default limit is 100 requests per 60 seconds.

`features/flags` is not a public API path. When API-key middleware is enabled and a validation key is configured, a request without a matching `X-Api-Key` header receives `401`.

The Blazor client loads `api/features/flags` once. If that load fails, `ShowPriorityBadges` is treated as enabled and every other name is treated as disabled. A successful body that omits a name is treated as disabled for that name.
