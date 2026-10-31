# Note 184

When POST `/api/tools/slugify` receives a missing body or a null `text`, `ToolsSlugifyController.Post` returns HTTP 400 with detail `JSON body field 'text' is required.` The method is `Post` in `src/UI/Api/Controllers/ToolsSlugifyController.cs`. `docs/api/tools.md` states that a missing `text` on POST `/api/tools/slugify` returns 400.
