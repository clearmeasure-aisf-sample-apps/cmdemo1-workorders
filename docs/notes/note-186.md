# Note 186

`ApiRateLimitingOptions` in `src/UI/Server/ApiRateLimitingOptions.cs` defaults `PermitLimit` to 100, `WindowSeconds` to 60, and `SegmentsPerWindow` to 4 (`public int PermitLimit { get; set; } = 100`, `public int WindowSeconds { get; set; } = 60`, `public int SegmentsPerWindow { get; set; } = 4`). `docs/ops/feature-flags.md` states: "Both routes use the API sliding-window rate limit. When that limit is enabled, the default is 100 requests per 60 seconds."
