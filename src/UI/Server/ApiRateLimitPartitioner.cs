using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Constructs the shared <see cref="PartitionedRateLimiter{TResource}"/> for API routes:
/// one partition per API key / user / remote IP / anonymous bucket, each with an identical
/// sliding-window limiter bound to <see cref="ApiRateLimitingOptions"/>.
/// </summary>
internal static class ApiRateLimitPartitioner
{
    /// <summary>
    /// Builds the limiter. Reads current options at call time (not at registration time)
    /// so config reloads take effect without a process restart.
    /// </summary>
    internal static PartitionedRateLimiter<HttpContext> CreateLimiter(IOptionsMonitor<ApiRateLimitingOptions> optionsMonitor) =>
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var opts = optionsMonitor.CurrentValue;
            if (!ApiRateLimitingDecision.LimiterAppliesTo(httpContext, opts))
            {
                return RateLimitPartition.GetNoLimiter(string.Empty);
            }

            var partitionKey = ApiRateLimitPartitionResolver.Resolve(httpContext, opts.ApiKeyHeaderName);
            return CreateSlidingWindowPartition(partitionKey, opts);
        });

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
}
