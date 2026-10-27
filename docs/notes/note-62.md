# Note 62

GET /api/tools/timestamp-converter returns HTTP 400 with detail "Provide exactly one of query parameters 'unix' or 'iso'." when the query contains neither unix nor iso. TimestampConverterController.ValidateQueryKeys returns that string when the query has neither key, and Get returns it as the problem detail, in src/UI/Api/Controllers/TimestampConverterController.cs. docs/api/tools.md states that missing both, or sending both, returns 400.
