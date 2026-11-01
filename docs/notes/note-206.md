# Note 206

`ApiRequestTimeoutsExtensions.ApiControllersPolicyName` in `src/UI/Server/ApiRequestTimeoutsExtensions.cs` is the constant `ApiControllers` (`public const string ApiControllersPolicyName = "ApiControllers";`). That named policy is applied to MVC API controllers when timeouts are enabled. `ApiRequestTimeoutOptions` in `src/UI/Server/ApiRequestTimeoutOptions.cs` loads section `ApiRequestTimeouts` (`public const string SectionName = "ApiRequestTimeouts";`); `Enabled` defaults to true and `TimeoutSeconds` defaults to 120.
