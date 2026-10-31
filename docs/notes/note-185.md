# Note 185

A successful GET `/api/tools/timestamp-converter` returns JSON built by `TimestampConverterController.BuildResponse` in `src/UI/Api/Controllers/TimestampConverterController.cs`: `unix` is UTC Unix seconds (`utc.ToUnixTimeSeconds()`), `iso` is `utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)` (`IsoOutputFormat`), and `human` is `utc.ToString("dddd, dd MMMM yyyy HH:mm:ss UTC", CultureInfo.InvariantCulture)` (`HumanOutputFormat`). `docs/api/tools.md` states the same `iso` and `human` formats.
