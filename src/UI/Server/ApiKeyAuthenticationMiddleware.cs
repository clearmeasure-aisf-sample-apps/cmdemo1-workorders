using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IOptions<ApiKeyAuthenticationOptions> optionsAccessor) =>
        ApiKeyAuthenticationPipeline.InvokeAsync(context, next, optionsAccessor.Value);
}
