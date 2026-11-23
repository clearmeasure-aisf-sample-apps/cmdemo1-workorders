using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMethodReturnValue.Global -- Qodana C6 (#9039): fluent
// IServiceCollection extension-method pattern; chained return value is by design, not always used.
namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers partitioned sliding-window rate limiting used by <see cref="RateLimiting.RateLimitingMiddleware"/>.
/// </summary>
public static class ApiRateLimitingExtensions
{
    /// <summary>
    /// Binds <see cref="ApiRateLimitingOptions"/> and registers the shared <see cref="PartitionedRateLimiter{TResource}"/> for API routes.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRateLimitingOptions>(configuration.GetSection(ApiRateLimitingOptions.SectionName));
        // Qualified with the type name on purpose: extension-method call sites never name this class, and the
        // TestOnlyReferenceScan gate reads a type named only by tests as production code kept for tests only.
        services.AddSingleton<PartitionedRateLimiter<HttpContext>>(ApiRateLimitingExtensions.CreatePartitionedLimiter);
        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreatePartitionedLimiter(IServiceProvider sp)
    {
        var monitor = sp.GetRequiredService<IOptionsMonitor<ApiRateLimitingOptions>>();
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var opts = monitor.CurrentValue;
            if (!opts.Enabled || !ApiRoutePolicy.IsRateLimitedPath(httpContext.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter(string.Empty);
            }

            var partitionKey = ResolvePartitionKey(httpContext, opts.ApiKeyHeaderName);
            return CreateSlidingWindowPartition(partitionKey, opts);
        });
    }

    private static RateLimitPartition<string> CreateSlidingWindowPartition(
        string partitionKey,
        ApiRateLimitingOptions opts) =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, opts.PermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds)),
                SegmentsPerWindow = Math.Max(1, opts.SegmentsPerWindow),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = Math.Max(0, opts.QueueLimit),
                AutoReplenishment = true
            });

    internal static string ResolvePartitionKey(HttpContext httpContext, string apiKeyHeaderName) =>
        ApiRateLimitPartitionResolver.Resolve(httpContext, apiKeyHeaderName);
}
