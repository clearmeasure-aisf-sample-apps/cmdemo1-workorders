# Note 214

`CorrelationIdConstants.HeaderName` in `src/ChurchBulletin.ServiceDefaults/CorrelationIdConstants.cs` is the constant `X-Correlation-ID` (`public const string HeaderName = "X-Correlation-ID";`). `CorrelationIdMiddleware.InvokeAsync` copies that name onto the response header (`context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;`).
