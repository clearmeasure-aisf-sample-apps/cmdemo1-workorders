# Note 161

GET `/_build` is anonymous. `BuildFactsExtensions.MapBuildFacts` in `src/UI/Server/BuildFacts/BuildFactsExtensions.cs` maps `/_build` with `AllowAnonymous`. `WriteFacts` sets `Access-Control-Allow-Origin` to `*` and `Cache-Control` to `public, max-age=300` (the `CacheControl` constant), then returns `facts.Json` as UTF-8 JSON. `docs/api/health-endpoints.md` states that `/_build` is anonymous, readable from any origin, and publicly cacheable for five minutes. `docs/health-endpoints.md` states that responses are publicly cacheable for five minutes.
