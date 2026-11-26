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
        services.AddSingleton(sp => ApiRateLimitPartitioner.CreateLimiter(sp.GetRequiredService<IOptionsMonitor<ApiRateLimitingOptions>>()));
        return services;
    }
}
