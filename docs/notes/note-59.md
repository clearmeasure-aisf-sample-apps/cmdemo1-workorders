# Note 59

GET /api/tools/random with query `type=color` returns plain text (`text/plain; charset=utf-8`) of the form `#` plus six uppercase hexadecimal digits. `ToolsRandomController` maps `color` to `$"#{Random.Shared.Next(0x1000000):X6}"` in `src/UI/Api/Controllers/ToolsRandomController.cs`. `docs/api/tools.md` states that query `type` `color` is `#RRGGBB`.
