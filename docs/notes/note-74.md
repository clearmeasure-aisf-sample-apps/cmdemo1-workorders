# Note 74

`FeatureFlagsCatalog.All` in `src/UI/Api/FeatureFlagsCatalog.cs` sets `ShowPriorityBadges` to true. GET `/api/features/flags` and GET `/api/v1.0/features/flags` return that catalog as JSON. `FeatureFlagsController.Get` in `src/UI/Api/Controllers/FeatureFlagsController.cs` returns `ConditionalGetEtag.JsonContent(FeatureFlagsCatalog.All)`. The versioned route is `api/v{version:apiVersion}/features/flags` with API version 1.0. `docs/ops/feature-flags.md` lists `ShowPriorityBadges` as true.
