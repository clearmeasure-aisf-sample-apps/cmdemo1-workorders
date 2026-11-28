using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Runs <see cref="LlmRequestThrottleGuard"/> on single-API posts that carry an <see cref="ApplicationChatQuery"/>:
/// an oversize chat request gets 400 and a client over the chat rate gets 429, before the handler or LLM is reached.
/// </summary>
internal sealed class LlmRequestThrottleMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiRateLimitingOptions> apiRateLimitingOptions)
{
    public async Task InvokeAsync(HttpContext context, LlmRequestThrottleGuard guard)
    {
        var query = await ReadChatQueryAsync(context.Request);
        var verdict = query is null
            ? LlmThrottleVerdict.Allowed
            : guard.Evaluate(query, PartitionKeyFor(context));
        if (verdict.IsAllowed)
        {
            await next(context);
            return;
        }

        await (verdict.StatusCode == StatusCodes.Status429TooManyRequests
            ? RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, guard.Rule)
            : ProblemDetailsWriter.WriteAsync(context, verdict.StatusCode, "Bad Request", verdict.Detail));
    }

    /// <summary>The chat query a single-API post carries, or null for any other request.</summary>
    internal static async Task<ApplicationChatQuery?> ReadChatQueryAsync(HttpRequest request)
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

    private string PartitionKeyFor(HttpContext context) =>
        ApiRateLimitPartitionResolver.Resolve(context, apiRateLimitingOptions.CurrentValue.ApiKeyHeaderName);
}
