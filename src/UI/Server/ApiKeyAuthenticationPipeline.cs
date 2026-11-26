namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiKeyAuthenticationPipeline
{
    internal static async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next,
        ApiKeyAuthenticationOptions options)
    {
        if (!ApiRoutePolicy.RequiresApiKey(context.Request.Path.Value, options))
        {
            await next(context);
            return;
        }

        if (!ApiKeyHeaderAuthorization.IsAuthorized(context.Request, options.RequiredValidationKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
