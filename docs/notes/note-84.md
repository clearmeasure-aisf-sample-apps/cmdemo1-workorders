# Note 84

GET `/api/tools/timestamp-converter` returns HTTP 400 with detail "Provide exactly one of query parameters 'unix' or 'iso', not both." when the query contains both `unix` and `iso`. `TimestampConverterController.ValidateQueryKeys` in `src/UI/Api/Controllers/TimestampConverterController.cs` returns that string when both keys are present, and `Get` returns it as the problem detail. `docs/api/tools.md` states that sending both returns 400.
