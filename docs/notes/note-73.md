# Note 73

GET `/api/echo` and GET `/api/v1.0/echo` replace the values of the `Authorization`, `X-Api-Key`, and `Cookie` request headers with the literal `[REDACTED]`. Other header values are copied as sent. `EchoController` keeps those three names in `SensitiveHeaderNames` and the replacement in `RedactedValue` in `src/UI/Api/Controllers/EchoController.cs`. The versioned route is `api/v{version:apiVersion}/echo` with API version 1.0.
