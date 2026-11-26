using System.Globalization;
using System.Threading.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// The sliding-window limit in force, normalized once from <see cref="ApiRateLimitingOptions"/> so the limiter and the
/// advertised rate-limit headers share one window and never re-derive validity from raw configuration.
/// </summary>
internal sealed record SlidingWindowRule
{
    /// <summary>
    /// Normalizes raw configuration; prefer <see cref="ApiRateLimitingOptions.ToSlidingWindowRule"/>.
    /// </summary>
    internal SlidingWindowRule(int configuredPermitLimit, int windowSeconds, int segmentsPerWindow, int queueLimit)
    {
        AdvertisedPermitLimit = configuredPermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        PermitLimit = Math.Max(1, configuredPermitLimit);
        WindowSeconds = Math.Max(1, windowSeconds);
        SegmentsPerWindow = Math.Max(1, segmentsPerWindow);
        QueueLimit = Math.Max(0, queueLimit);
    }

    /// <summary>Permits per client per window enforced by the limiter (at least 1).</summary>
    public int PermitLimit { get; }

    /// <summary>Whole seconds in the window (at least 1); also the <c>Retry-After</c> value.</summary>
    public int WindowSeconds { get; }

    /// <summary>Sliding window length used by the limiter and the reset headers.</summary>
    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);

    /// <summary>Segments the window is divided into (at least 1).</summary>
    public int SegmentsPerWindow { get; }

    /// <summary>Queued permits when the window is full (at least 0).</summary>
    public int QueueLimit { get; }

    /// <summary>
    /// The <c>X-RateLimit-Limit</c> header value: the configured permit limit as written in configuration.
    /// </summary>
    public string AdvertisedPermitLimit { get; }

    /// <summary>The <c>X-RateLimit-Remaining</c> header value for the limiter's available permits (never negative).</summary>
    public static string AdvertisedRemaining(long availablePermits) =>
        Math.Max(0, availablePermits).ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>The limiter options for one partition under this rule.</summary>
    public SlidingWindowRateLimiterOptions ToLimiterOptions() => new()
    {
        PermitLimit = PermitLimit,
        Window = Window,
        SegmentsPerWindow = SegmentsPerWindow,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = QueueLimit,
        AutoReplenishment = true
    };
}
