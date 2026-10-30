# Note 157

WorkOrderActivity.ChurchDate treats WhenUtc as UTC (`DateTime.SpecifyKind(WhenUtc, DateTimeKind.Utc)`), converts that instant into ChurchTimeZone.Chicago (`TimeZoneInfo.ConvertTimeFromUtc`), and returns the calendar date (`DateOnly.FromDateTime`). The summary comment is "Gets the church calendar date (America/Chicago) of the event. Stored values come back with DateTimeKind.Unspecified, so the value is treated as UTC." The property is `WorkOrderActivity.ChurchDate` in `src/Core/Services/WorkOrderActivity.cs`.
