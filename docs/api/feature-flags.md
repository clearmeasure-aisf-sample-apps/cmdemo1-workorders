# Feature Flags

The read-only endpoint returns the current application feature flags as a JSON object. Each property name is a flag name and its Boolean value indicates whether that flag is enabled. `GET /api/diagnostics` returns the same object on `featureFlags`. Both read `IFeatureFlagRuntime`: code defaults from `FeatureFlagsCatalog`, then the `FeatureFlags` configuration section for any known name.

Routes:

- `GET /api/features/flags`
- `GET /api/v1.0/features/flags`
- `GET /api/diagnostics`
- `GET /api/v1.0/diagnostics`

Code defaults, before configuration overlay:

```json
{
  "SampleFeatureA": true,
  "SampleFeatureB": false,
  "ShowPriorityBadges": true,
  "BuildingManagement": false,
  "VendorManagement": false
}
```

`BuildingManagement` and `VendorManagement` are false in code and in `appsettings.json` (the image, and platform uat and prod until flipped). They are true in `appsettings.Development.json`, which is the local process, the acceptance host, and the stand-in for platform tdd. Platform tdd sets `FeatureFlags__BuildingManagement` and `FeatureFlags__VendorManagement` to `true`.

The catalog is defined in `FeatureFlagsCatalog`.
