namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Path and environment rules for the service-registration and connection-pool diagnostic endpoints
/// (<c>/_diagnostics</c>).
/// </summary>
public static class DiagnosticEndpointPaths
{
    /// <summary>True when the request targets a diagnostic controller route.</summary>
    public static bool Matches(string? pathValue)
    {
        if (string.IsNullOrEmpty(pathValue))
        {
            return false;
        }

        return MatchesPrefix(pathValue, "/_diagnostics");
    }

    /// <summary>
    /// When API-key authentication is not configured, diagnostic endpoints stay reachable only in
    /// Development and the platform <c>tdd</c> environment.
    /// </summary>
    public static bool AllowsAnonymousWhenApiKeyOff(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("tdd");

    private static bool MatchesPrefix(string pathValue, string prefix) =>
        pathValue.Equals(prefix, StringComparison.OrdinalIgnoreCase)
        || pathValue.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}
