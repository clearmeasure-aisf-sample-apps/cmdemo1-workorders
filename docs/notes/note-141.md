# Note 141

ChurchTimeZone.Chicago is a static TimeZoneInfo set once by ResolveChicago. ResolveChicago returns the first zone FindTimeZone can load: America/Chicago, then Central Standard Time, then TimeZoneInfo.Utc. FindTimeZone calls TimeZoneInfo.FindSystemTimeZoneById and returns null when that throws TimeZoneNotFoundException or InvalidTimeZoneException. The property summary comment is "Gets America/Chicago, then Central Standard Time, with UTC as a safe final fallback." The property is ChurchTimeZone.Chicago in src/Core/Services/ChurchTimeZone.cs.
