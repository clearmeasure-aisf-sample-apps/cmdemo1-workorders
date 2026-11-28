using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Runs <see cref="LlmRequestThrottleGuard"/> on single-API posts that carry an <see cref="ApplicationChatQuery"/>:
/// an oversize chat request gets 400 and a client over the chat rate gets 429, before the handler or LLM is reached.
/// The chat rate follows <see cref="ApiRateLimitingOptions.Enabled"/>; the size rules always apply.
/// </summary>
internal sealed class LlmRequestThrottleMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiRateLimitingOptions> apiRateLimitingOptions)
{
    public async Task InvokeAsync(HttpContext context, LlmRequestThrottleGuard guard)
    {
        var query = await ReadChatQueryAsync(context.Request);
        if (query is null)
        {
            await next(context);
            return;
        }

        var sizeViolation = guard.FindSizeViolation(query);
        if (sizeViolation is not null)
        {
            await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, "Bad Request", sizeViolation);
            return;
        }

        var apiOptions = apiRateLimitingOptions.CurrentValue;
        if (apiOptions.Enabled
            && !guard.TryAcquire(ApiRateLimitPartitionResolver.Resolve(context, apiOptions.ApiKeyHeaderName)))
        {
            await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, guard.Rule);
            return;
        }

        await next(context);
    }

    private static async Task<ApplicationChatQuery?> ReadChatQueryAsync(HttpRequest request)
    {
        if (!WebServiceMessageValidationMiddleware.IsBlazorWasmSingleApiPost(request))
        {
            return null;
        }

        var body = await WebServiceMessageValidationMiddleware.ReadRequestBodyAsync(request);
        return WebServiceMessageValidationMiddleware.TryDeserializeMessage(body, out var message, out _)
            && IsChatMessage(message!)
            ? message!.GetBodyObject() as ApplicationChatQuery
            : null;
    }

    private static bool IsChatMessage(WebServiceMessage message) =>
        Type.GetType(message.TypeName, throwOnError: false) == typeof(ApplicationChatQuery);
}
