namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/diagnostics</c> and <c>GET /api/v1.0/diagnostics</c>.
/// <see cref="FeatureFlags"/> is the same map as <c>GET /api/features/flags</c>.
/// </summary>
public sealed record DiagnosticsResponse(
    string Environment,
    TimeSpan Uptime,
    IReadOnlyDictionary<string, bool> FeatureFlags);
