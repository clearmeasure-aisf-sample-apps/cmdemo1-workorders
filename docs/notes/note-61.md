# Note 61

GET /api/tools/due-date-check returns HTTP 400 with detail "Query parameter 'date' is required (format: YYYY-MM-DD)." when the date query parameter is missing or whitespace. ToolsDueDateCheckController.Get returns that problem when string.IsNullOrWhiteSpace(date) is true, in src/UI/Api/Controllers/ToolsDueDateCheckController.cs. docs/api/tools.md states that a missing or unparseable date returns 400.
