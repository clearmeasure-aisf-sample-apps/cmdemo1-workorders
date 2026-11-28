using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.AspNetCore.OutputCaching;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers every output-cache policy of the host. Nothing is cached by default; only anonymous GETs that return
/// the same body to every caller opt in by name, so a scrape or flood of those probes is answered from memory
/// instead of running the endpoint. The ASP.NET Core default rules still apply to every named policy: only
/// <c>GET</c>/<c>HEAD</c> with status 200 are stored, and requests carrying <c>Authorization</c> or responses
/// setting a cookie are never cached, so authenticated calls and writes always reach the application.
/// </summary>
public static class HotPathOutputCacheGuard
{
    /// <summary>
    /// Lifetime of a cached hot-path probe; short so a health change is visible within seconds.
    /// </summary>
    public static readonly TimeSpan HotPathTimeToLive = TimeSpan.FromSeconds(5);

    // Build and host metadata changes only on redeploy.
    private static readonly TimeSpan VersionMetadataTimeToLive = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Adds the no-cache base policy and the named policies in <see cref="OutputCachePolicyNames"/>.
    /// </summary>
    public static void AddPolicies(OutputCacheOptions options)
    {
        options.AddBasePolicy(policy => policy.NoCache());
        options.AddPolicy(OutputCachePolicyNames.VersionMetadata, policy => policy
            .Expire(VersionMetadataTimeToLive)
            .SetVaryByQuery("*")
            .SetVaryByHeader("Accept"));
        options.AddPolicy(OutputCachePolicyNames.Ping, policy => policy
            .Expire(HotPathTimeToLive));
        options.AddPolicy(OutputCachePolicyNames.SimpleHealth, policy => policy
            .Expire(HotPathTimeToLive)
            .SetVaryByHeader(TestSupportConstants.NeedsRebootHeaderName));
    }
}
