# Note 166

`MetricsSummaryBuilder.Build` in `src/UI/Api/MetricsSummaryBuilder.cs` sets `ManagedMemoryBytes` to `GC.GetTotalMemory(false)` (`ManagedMemoryBytes: GC.GetTotalMemory(false)`). The `false` argument does not force a garbage collection. `MetricsController.GetSummary` in `src/UI/Api/Controllers/MetricsController.cs` returns that value on GET `/api/metrics/summary` and GET `/api/v1.0/metrics/summary` as the `MetricsSummaryResponse` property `ManagedMemoryBytes`.
