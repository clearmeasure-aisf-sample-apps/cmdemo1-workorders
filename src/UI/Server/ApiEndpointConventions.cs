namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>Request timeouts and CORS for every API endpoint.</summary>
internal static class ApiEndpointConventions
{
    internal static void ApplyRequestTimeout(WebApplication app, IEndpointConventionBuilder api)
    {
        var apiRequestTimeoutOpts = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiRequestTimeoutOptions>>().Value;
        if (!apiRequestTimeoutOpts.Enabled || apiRequestTimeoutOpts.TimeoutSeconds <= 0)
        {
            return;
        }

        api.WithRequestTimeout(ApiRequestTimeoutsExtensions.ApiControllersPolicyName);
    }

    internal static void ApplyCorsWhenActive(WebApplication app, IEndpointConventionBuilder api)
    {
        if (!app.Services.IsServerCorsActive())
        {
            return;
        }

        api.RequireCors(ServerCorsOptions.PolicyName);
    }
}
