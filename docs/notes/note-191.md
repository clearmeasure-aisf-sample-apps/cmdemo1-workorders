# Note 191

`ChurchTimeZone` in `src/Core/Services/ChurchTimeZone.cs` stores the IANA timezone id in the private constant `IanaId`, whose value is `"America/Chicago"`. `ResolveChicago` tries `FindTimeZone(IanaId)` first, then the Windows id `"Central Standard Time"`, then `TimeZoneInfo.Utc`.
