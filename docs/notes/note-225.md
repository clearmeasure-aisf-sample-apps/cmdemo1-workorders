# Note 225

`ApiKeyConstants.HeaderName` in `src/UI.Shared/ApiKeyConstants.cs` is the public constant `X-Api-Key` (`public const string HeaderName = "X-Api-Key";`). `ApiKeyHeaderAuthorization.IsAuthorized` in `src/UI/Server/ApiKeyHeaderAuthorization.cs` reads that header with `request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues)` and returns false when the header is absent. It is the single constant-time comparison path, shared by `ApiKeyAuthenticationPipeline` and `DiagnosticEndpointAccessPipeline`.
