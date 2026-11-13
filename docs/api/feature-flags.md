# Feature Flags

The read-only endpoint returns the current application feature flags as a JSON object. Each property name is a flag name and its Boolean value indicates whether that flag is enabled right now.

Routes:

- `GET /api/features/flags`
- `GET /api/v1.0/features/flags`

With the checked-in `appsettings.json` and no Octopus client identifier, the body is:

```json
{
  "SampleFeatureA": false,
  "SampleFeatureB": false,
  "ShowPriorityBadges": true,
  "BuildingManagement": false,
  "VendorManagement": false
}
```

The controller sends `FeatureFlagsQuery` on the bus; values come from Octopus Feature Toggles, then the `FeatureFlags` configuration section, then false. `GET /api/diagnostics` returns the same map as `featureFlags`.

`ShowPriorityBadges` is an Octopus Feature Toggle (slug `showprioritybadges`) that stays on in tdd, uat and prod. `FeatureFlags:ShowPriorityBadges` in appsettings remains true as the fallback when Octopus is unset or the provider cannot resolve the flag. `appsettings.Development.json` turns `BuildingManagement` and `VendorManagement` on for the local process and the acceptance host (platform tdd). The image and platform uat and prod leave Building and Vendor off until flipped. See [Feature flags](../feature-flags.md).
