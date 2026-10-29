# Note 79

POST `/api/tools/case` with JSON `to` of `lower` (trimmed, then matched case-insensitively) converts the text with `string.ToLowerInvariant`. `CaseConverters` maps `lower` to `static text => text.ToLowerInvariant()` in `src/UI/Api/Controllers/ToolsCaseController.cs`. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` lists `lower` among the `to` values.
