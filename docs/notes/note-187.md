# Note 187

When GET `/api/tools/due-date-check` receives a `date` that is not whitespace and that `DateOnly.TryParseExact` cannot parse as `yyyy-MM-dd` (`CultureInfo.InvariantCulture`, `DateTimeStyles.None`, after `date.Trim()`), `ToolsDueDateCheckController.Get` returns HTTP 400 with detail `Invalid date '{date}'. Expected format: YYYY-MM-DD (e.g. 2025-01-15).` The detail interpolates the original query value. The method is `Get` in `src/UI/Api/Controllers/ToolsDueDateCheckController.cs`. `docs/api/tools.md` states that an unparseable `date` returns 400.
