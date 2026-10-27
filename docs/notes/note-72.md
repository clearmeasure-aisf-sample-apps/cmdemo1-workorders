# Note 72

POST `/api/tools/case` with JSON `to` of `snake` (trimmed, then matched case-insensitively) replaces each run of non-alphanumeric characters with one underscore, trims leading and trailing underscores, and lowercases the result with the invariant culture. Whitespace-only text yields an empty string. `CaseConverters` maps `snake` to `ToSeparated` with separator `_` in `src/UI/Api/Controllers/ToolsCaseController.cs`. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` lists `snake` among the `to` values.
