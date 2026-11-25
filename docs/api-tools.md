# API Tools

The sample text/utility tools, the echo endpoint and the WeatherForecast sample were retired in #1819. The remaining tool endpoints:

- **GET `/api/tools/due-date-check`** returns the due-date urgency for the required `date` (`YYYY-MM-DD`), relative to today in the America/Chicago timezone, assuming the work order is open.
- **GET `/api/tools/work-order-statuses`** lists the work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order.

Both routes are also available under the versioned prefix `/api/v1.0/tools/`. All tool endpoints in
`src/UI/Api/Controllers/` are anonymous, rate-limited and have no database access.

| Route | Method | Description |
|---|---|---|
| `/api/tools/due-date-check` | GET | Due-date urgency for required `date` query param (`YYYY-MM-DD`), relative to today in America/Chicago, assuming an open work order |
| `/api/tools/work-order-statuses` | GET | Work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order |

## Adding an API-layer-only endpoint (agents)

Read when adding a pure utility endpoint in `src/UI/Api/Controllers/` with no Core/Domain/DataAccess changes. Moved
from `AGENTS.md`.

- Mirror the dual `[Route]` pattern: `"api/tools/<name>"` + `$"{ApiRoutes.VersionedApiPrefix}/tools/<name>"` (see
  `ToolsWorkOrderStatusesController`, `ToolsDueDateCheckController`).
- Use `[ApiVersion("1.0")]`, `[AllowAnonymous]`, `[EnableRateLimiting(ApiRateLimiting.PolicyName)]`.
- Return plain text via `ContentResult` with `ContentType = "text/plain; charset=utf-8"`.
- Return structured errors via `Problem(detail: "...", statusCode: 400)`.
- No DI needed for stateless generators: use `Random.Shared` (thread-safe) and static helpers.
- For any read-only GET that returns JSON, reuse `src/UI/Api/ConditionalGetEtag.cs` (serializes with
  `JsonSerializerDefaults.Web`, returns `ContentResult`).
- Build verification: `dotnet build src/ChurchBulletin.sln --configuration Release` (warnings are errors).
