# Note 64

GET /api/tools/random with query `type=string` returns plain text (`text/plain; charset=utf-8`) of exactly 12 characters, each drawn from `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`. `ToolsRandomController.AlphanumericLength` is 12 and `GenerateAlphanumeric` builds that string in `src/UI/Api/Controllers/ToolsRandomController.cs`. The same GET is also routed at `api/v{version:apiVersion}/tools/random` with API version 1.0. `docs/api/tools.md` states that query `type` `string` is 12 alphanumeric characters.
