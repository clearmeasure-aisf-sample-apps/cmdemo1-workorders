# Feature flags

`GET /api/features/flags` returns a JSON object containing each known feature
flag and its enabled status. The versioned route is
`GET /api/v1.0/features/flags`.

The flag names and their current values are defined in
[`src/UI/Api/FeatureFlagsCatalog.cs`](../src/UI/Api/FeatureFlagsCatalog.cs).
Update `FeatureFlagsCatalog.All` to add or change a flag.

Example response:

```json
{
  "SampleFeatureA": true,
  "SampleFeatureB": false
}
```
