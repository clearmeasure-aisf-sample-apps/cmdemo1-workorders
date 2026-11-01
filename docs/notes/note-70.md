# Note 70

GET `/api/time` returns status 200 and plain text (`text/plain; charset=utf-8`) equal to `TimeProvider.GetUtcNow()` formatted with the round-trip pattern `"O"` and `CultureInfo.InvariantCulture`. `TimeController.Get` in `src/UI/Api/Controllers/TimeController.cs` returns that `ContentResult`. The same GET is also routed at `api/v{version:apiVersion}/time` with API version 1.0.
