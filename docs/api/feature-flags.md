# Feature Flags

The read-only endpoint returns the current application feature flags as a JSON object. Each property name is a flag name and its Boolean value indicates whether that flag is enabled.

Routes:

- `GET /api/features/flags`
- `GET /api/v1.0/features/flags`

The in-memory catalog currently contains:

```json
{
  "SampleFeatureA": true,
  "SampleFeatureB": false
}
```

The catalog is defined in `FeatureFlagsCatalog`.
