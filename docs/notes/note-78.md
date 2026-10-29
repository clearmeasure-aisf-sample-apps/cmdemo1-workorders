# Note 78

POST `/api/tools/case` with JSON `to` of `upper` (trimmed, then matched case-insensitively) converts the text with `string.ToUpperInvariant`. `CaseConverters` maps `upper` to `static text => text.ToUpperInvariant()` in `src/UI/Api/Controllers/ToolsCaseController.cs`. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` lists `upper` among the `to` values.
