# Note 152

ChurchTimeZone.Today throws ArgumentNullException when timeProvider is null (`ArgumentNullException.ThrowIfNull(timeProvider)`). Otherwise it converts the clock's UTC instant into ChurchTimeZone.Chicago (`TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Chicago)`) and returns that calendar date (`DateOnly.FromDateTime(local.DateTime)`). The summary comment is "Returns today's calendar date in America/Chicago for the given clock." The method is `ChurchTimeZone.Today` in `src/Core/Services/ChurchTimeZone.cs`.
