# Note 76

`src/UI/Server/appsettings.json` sets `FeatureFlags:SampleFeatureB` to false. When Octopus is not configured, `RuntimeFeatureFlagEvaluator` in `src/DataAccess/FeatureFlags/RuntimeFeatureFlagEvaluator.cs` returns that configuration value. `docs/ops/feature-flags.md` lists that flag's default as false and says the flags endpoint returns it and it changes no screen.
