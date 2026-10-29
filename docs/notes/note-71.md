# Note 71

POST `/api/tools/case` with JSON `to` of `kebab` (trimmed, then matched case-insensitively) replaces each run of non-alphanumeric characters with one hyphen, trims leading and trailing hyphens, and lowercases the result with the invariant culture. Whitespace-only text yields an empty string. `CaseConverters` maps `kebab` to `ToSeparated` with separator `-` in `src/UI/Api/Controllers/ToolsCaseController.cs`. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` lists `kebab` among the `to` values.
