# Note 164

GET `/api/time` returns HTTP 304 when the request's `If-None-Match` includes the weak ETag of the current UTC instant. `TimeController.Get` in `src/UI/Api/Controllers/TimeController.cs` formats `timeProvider.GetUtcNow()` with the round-trip pattern `"O"` and `CultureInfo.InvariantCulture`, builds the tag with `ConditionalGetEtag.CreateWeakEtagForPlainText`, sets `Response.Headers.ETag` to that tag, and returns `StatusCodes.Status304NotModified` when `ConditionalGetEtag.IfNoneMatchIncludesEtag` is true. `docs/notes/note-70.md` describes the 200 plain-text body of the same GET.
