using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for sliding-window rate limiting; which routes it limits is decided by <see cref="ApiRoutePolicy"/>
/// (the <c>/api</c> tree plus the Blazor single-API routes).
/// </summary>
/// <remarks>
/// The <c>ApiRateLimiting</c> section of <c>appsettings.json</c> deliberately restates every default below: it is kept as
/// explicit documentation of the shipped limiter configuration, not as an override. Keep the two in step (a unit test
/// fails when they drift); environment files override only what differs (Development sets <c>Enabled</c> to false).
/// </remarks>
public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "ApiRateLimiting";

    /// <summary>
    /// When false, registration still occurs but all requests are unlimited.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests allowed per client per window when limiting is enabled.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Sliding window length.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Number of segments the window is divided into (higher = smoother sliding).
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>
    /// Queued permits when the window is full; 0 means fail immediately.
    /// </summary>
    public int QueueLimit { get; set; }

    /// <summary>
    /// Optional header for per-client key; when absent, user name or connection remote IP is used.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- required for IOptions<T> configuration binding
    public string ApiKeyHeaderName { get; set; } = ApiKeyConstants.HeaderName;
}
