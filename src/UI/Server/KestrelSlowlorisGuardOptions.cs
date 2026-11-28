namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for <see cref="KestrelSlowlorisGuard"/>: Kestrel timeouts, minimum data rates and connection caps
/// that stop slow-header and slow-body clients from holding connections open. Out-of-range values are clamped by
/// the guard to safe bounds.
/// </summary>
public sealed class KestrelSlowlorisGuardOptions
{
    /// <summary>Configuration section bound to these options.</summary>
    public const string SectionName = "KestrelSlowlorisGuard";

    /// <summary>Most time a client may take to send all request headers. Default 15 seconds.</summary>
    public TimeSpan RequestHeadersTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Most time an idle keep-alive connection stays open. Default 130 seconds (the Kestrel default).</summary>
    public TimeSpan KeepAliveTimeout { get; init; } = TimeSpan.FromSeconds(130);

    /// <summary>Slowest request body upload, in bytes per second, before the connection is closed. Default 240.</summary>
    public double MinRequestBodyBytesPerSecond { get; init; } = 240;

    /// <summary>Slowest response download, in bytes per second, before the connection is closed. Default 240.</summary>
    public double MinResponseBytesPerSecond { get; init; } = 240;

    /// <summary>Time a transfer may stay below the minimum data rate before it is cut. Default 5 seconds.</summary>
    public TimeSpan MinDataRateGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Most open HTTP connections at once (not counting upgraded ones). Default 2000.</summary>
    public long MaxConcurrentConnections { get; init; } = 2000;

    /// <summary>Most open upgraded (WebSocket) connections at once. Default 1000.</summary>
    public long MaxConcurrentUpgradedConnections { get; init; } = 1000;
}
