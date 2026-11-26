using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Gates <c>/_lamar</c> and <c>/_diagnostics</c>: API key when configured; otherwise only
/// Development/tdd. Other environments receive 404 when no key scheme is active.
/// </summary>
public sealed class DiagnosticEndpointAccessMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(
        HttpContext context,
        IOptions<ApiKeyAuthenticationOptions> optionsAccessor,
        IWebHostEnvironment environment) =>
        DiagnosticEndpointAccessPipeline.InvokeAsync(context, next, optionsAccessor.Value, environment);
}

internal static class DiagnosticEndpointAccessPipeline
{
    internal static async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next,
        ApiKeyAuthenticationOptions options,
        IHostEnvironment environment)
    {
        if (!DiagnosticEndpointPaths.Matches(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        if (options.IsConfigured)
        {
            await ChallengeOrContinueAsync(context, next, options.ValidationKey ?? string.Empty);
            return;
        }

        if (DiagnosticEndpointPaths.AllowsAnonymousWhenApiKeyOff(environment))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static async Task ChallengeOrContinueAsync(
        HttpContext context,
        RequestDelegate next,
        string validationKey)
    {
        if (!ApiKeyHeaderAuthorization.IsAuthorized(context.Request, validationKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
