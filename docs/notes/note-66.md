# Note 66

GET /api/tools/random with query `type=uuid` returns plain text (`text/plain; charset=utf-8`) of a newly generated GUID in D format. `ToolsRandomController` maps `uuid` to `Guid.NewGuid().ToString("D")` in `src/UI/Api/Controllers/ToolsRandomController.cs`. The same GET is also routed at `api/v{version:apiVersion}/tools/random` with API version 1.0. `docs/api/tools.md` lists `uuid` as a supported `type` and shows `type=uuid` in its example.
