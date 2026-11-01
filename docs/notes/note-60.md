# Note 60

POST /api/tools/base64 returns HTTP 400 with detail "JSON body field 'text' is required and must be non-empty." when the JSON body is missing or `text` is null or empty. `ToolsBase64Controller.Post` returns that problem when `request?.Text` is null or has length 0, in `src/UI/Api/Controllers/ToolsBase64Controller.cs`. `docs/api/tools.md` states that empty `text` returns 400.
