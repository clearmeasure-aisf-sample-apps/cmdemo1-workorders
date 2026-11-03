# Note 269

`ChurchTimeZone.ComingSaturday` in `src/Core/Services/ChurchTimeZone.cs` returns today's calendar date in America/Chicago when today is Saturday, and otherwise the next Saturday. It sets `daysUntilSaturday` to `((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7` and returns `today.AddDays(daysUntilSaturday)`. The same behavior is stated in `docs/notes/note-140.md`.
