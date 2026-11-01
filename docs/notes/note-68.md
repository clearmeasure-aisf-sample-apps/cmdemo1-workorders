# Note 68

GET /api/tools/random with query `type=number` returns plain text (`text/plain; charset=utf-8`) of `Random.Shared.Next()` formatted with `CultureInfo.InvariantCulture`. `ToolsRandomController` maps `number` to `Random.Shared.Next().ToString(CultureInfo.InvariantCulture)` in `src/UI/Api/Controllers/ToolsRandomController.cs`. The same GET is also routed at `api/v{version:apiVersion}/tools/random` with API version 1.0. `docs/api/tools.md` lists `number` as a supported `type`.
