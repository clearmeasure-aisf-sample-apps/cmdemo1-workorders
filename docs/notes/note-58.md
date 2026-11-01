# Note 58

POST /api/tools/case returns HTTP 400 with detail "JSON body field 'text' is required." when the JSON body is missing or `text` is null. `ToolsCaseController.MissingText` returns that problem detail with status 400. `docs/api/tools.md` states that a missing `text` on POST `/api/tools/case` returns 400.
