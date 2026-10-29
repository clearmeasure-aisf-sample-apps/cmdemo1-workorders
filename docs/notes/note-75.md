# Note 75

POST `/api/tools/case` with JSON `to` of `title` (trimmed, then matched case-insensitively) trims the text, splits it on whitespace, uppercases the first character of each word with the invariant culture, lowercases the rest of each word, and joins the words with a single space. Whitespace-only text yields an empty string. `CaseConverters` maps `title` to `ToTitleCase` in `src/UI/Api/Controllers/ToolsCaseController.cs`. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` lists `title` among the `to` values.
