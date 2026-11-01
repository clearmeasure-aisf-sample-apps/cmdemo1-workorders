# Note 215

`CorrelationIdConstants.HttpContextItemKey` in `src/ChurchBulletin.ServiceDefaults/CorrelationIdConstants.cs` is the constant `CorrelationId` (`public const string HttpContextItemKey = "CorrelationId";`). `CorrelationIdMiddleware.InvokeAsync` in `src/ChurchBulletin.ServiceDefaults/CorrelationIdMiddleware.cs` stores the request's correlation identifier in `HttpContext.Items` under that key (`context.Items[CorrelationIdConstants.HttpContextItemKey] = correlationId;`).
