# Note 225

`ApiKeyConstants.HeaderName` in `src/UI.Shared/ApiHttpContracts.cs` is the public constant `X-Api-Key` (`public const string HeaderName = "X-Api-Key";`). `ApiKeyAuthenticationMiddleware.IsAuthorized` in `src/UI/Server/ApiKeyAuthenticationMiddleware.cs` reads that header with `request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues)` and returns false when the header is absent.
