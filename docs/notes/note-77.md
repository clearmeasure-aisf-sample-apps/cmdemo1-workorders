# Note 77

`src/UI/Server/appsettings.json` sets `FeatureFlags:SampleFeatureA` to false, and `src/UI/Server/appsettings.Development.json` sets it to true. When Octopus is not configured, `RuntimeFeatureFlagEvaluator` in `src/DataAccess/FeatureFlags/RuntimeFeatureFlagEvaluator.cs` returns the configuration value. `docs/ops/feature-flags.md` lists that flag's default as false (true in Development) and says the flags endpoint returns it and it changes no screen.
