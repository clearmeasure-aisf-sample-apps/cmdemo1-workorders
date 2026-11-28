using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Applies <see cref="KestrelSlowlorisGuardOptions"/> to <see cref="KestrelServerLimits"/> so slow-header and
/// slow-body (Slowloris) clients cannot hold connections indefinitely. Every value is clamped to a safe range:
/// zero, negative or absurdly large settings never disable a defense.
/// </summary>
public static class KestrelSlowlorisGuard
{
    private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(10);

    // Kestrel rejects a grace period that is not longer than its one-second heartbeat.
    private static readonly TimeSpan MinGracePeriod = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxGracePeriod = TimeSpan.FromSeconds(60);

    private const double MinBytesPerSecond = 1;
    private const double MaxBytesPerSecond = 1024 * 1024;
    private const double FallbackBytesPerSecond = 240;
    private const long MinConnections = 1;
    private const long MaxConnections = 1_000_000;

    /// <summary>
    /// Binds the <see cref="KestrelSlowlorisGuardOptions.SectionName"/> section (defaults when absent) and applies
    /// it to <paramref name="limits"/>.
    /// </summary>
    public static void Apply(KestrelServerLimits limits, IConfiguration configuration)
    {
        var options = configuration.GetSection(KestrelSlowlorisGuardOptions.SectionName)
            .Get<KestrelSlowlorisGuardOptions>() ?? new KestrelSlowlorisGuardOptions();
        Apply(limits, options);
    }

    /// <summary>Applies <paramref name="options"/>, clamped to safe ranges, to <paramref name="limits"/>.</summary>
    public static void Apply(KestrelServerLimits limits, KestrelSlowlorisGuardOptions options)
    {
        limits.RequestHeadersTimeout = Clamp(options.RequestHeadersTimeout, MinTimeout, MaxTimeout);
        limits.KeepAliveTimeout = Clamp(options.KeepAliveTimeout, MinTimeout, MaxTimeout);
        var gracePeriod = Clamp(options.MinDataRateGracePeriod, MinGracePeriod, MaxGracePeriod);
        limits.MinRequestBodyDataRate = new MinDataRate(ClampRate(options.MinRequestBodyBytesPerSecond), gracePeriod);
        limits.MinResponseDataRate = new MinDataRate(ClampRate(options.MinResponseBytesPerSecond), gracePeriod);
        limits.MaxConcurrentConnections = Math.Clamp(options.MaxConcurrentConnections, MinConnections, MaxConnections);
        limits.MaxConcurrentUpgradedConnections =
            Math.Clamp(options.MaxConcurrentUpgradedConnections, MinConnections, MaxConnections);
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max) =>
        value < min ? min : value > max ? max : value;

    private static double ClampRate(double bytesPerSecond) =>
        double.IsNaN(bytesPerSecond)
            ? FallbackBytesPerSecond
            : Math.Clamp(bytesPerSecond, MinBytesPerSecond, MaxBytesPerSecond);
}
