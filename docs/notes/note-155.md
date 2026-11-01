# Note 155

SlaHealth.ChurchDate treats the completion time as UTC (`DateTime.SpecifyKind(completedUtc, DateTimeKind.Utc)`), converts that instant from UTC into ChurchTimeZone.Chicago (`TimeZoneInfo.ConvertTimeFromUtc`), and returns the calendar date (`DateOnly.FromDateTime`). The summary comment is "Gets the church calendar date (America/Chicago) of a completion time. Stored values are UTC and come back with DateTimeKind.Unspecified, so the value is treated as UTC." The method is `SlaHealth.ChurchDate` in `src/Core/Queries/SlaHealth.cs`.
