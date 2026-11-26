namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Path and environment rules for Lamar and connection-pool diagnostic controllers
/// (<c>/_lamar</c>, <c>/_diagnostics</c>).
/// </summary>
public static class DiagnosticEndpointPaths
{
    private static readonly PathString LamarPath = new("/_lamar");
    private static readonly PathString DiagnosticsPath = new("/_diagnostics");

    /// <summary>True when the request targets a diagnostic controller route.</summary>
    public static bool Matches(string? pathValue) =>
        RequestPathPrefix.IsUnder(pathValue, LamarPath) || RequestPathPrefix.IsUnder(pathValue, DiagnosticsPath);

    /// <summary>
    /// When API-key authentication is not configured, diagnostic endpoints stay reachable only in
    /// Development and the platform <c>tdd</c> environment.
    /// </summary>
    public static bool AllowsAnonymousWhenApiKeyOff(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("tdd");

}
