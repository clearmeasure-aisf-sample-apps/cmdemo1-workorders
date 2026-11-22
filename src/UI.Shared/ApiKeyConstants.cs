namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Shared names for optional API key authentication between server middleware and HTTP clients.
/// </summary>
public static class ApiKeyConstants
{
    /// <summary>
    /// Request header carrying the shared secret for machine clients.
    /// </summary>
    public const string HeaderName = "X-Api-Key";
}
