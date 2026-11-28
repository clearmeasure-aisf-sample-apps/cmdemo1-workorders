namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Named ASP.NET Core output-cache policies registered in the host application.
/// </summary>
public static class OutputCachePolicyNames
{
    /// <summary>
    /// Build and host metadata; long TTL is acceptable because values change only on redeploy.
    /// </summary>
    public const string VersionMetadata = nameof(VersionMetadata);

    /// <summary>
    /// Anonymous <c>/api/ping</c> reachability probe; same body for every caller, short TTL.
    /// </summary>
    public const string Ping = nameof(Ping);

    /// <summary>
    /// Anonymous plain-text <c>/_healthcheck</c>; short TTL so a health change shows within seconds.
    /// The detailed health check is deliberately not cached.
    /// </summary>
    public const string SimpleHealth = nameof(SimpleHealth);
}
