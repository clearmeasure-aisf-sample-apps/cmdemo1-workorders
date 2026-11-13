using ClearMeasure.Bootcamp.UI.Server.TestSupport;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Health check that reports <see cref="HealthStatus.Unhealthy"/> when NeedsReboot is active,
/// simulating memory corruption that requires a process restart.
/// On tdd/uat/prod (TestSupport off), toggled process-wide via <c>/_demo/setneedsreboot/true|false</c>.
/// On pool slots (TestSupport on), request-scoped via <see cref="TestSupportConstants.NeedsRebootHeaderName"/>.
/// </summary>
public class NeedsRebootHealthCheck(
    ILogger<NeedsRebootHealthCheck> logger,
    IHttpContextAccessor httpContextAccessor,
    TestSupportOptions testSupportOptions) : IHealthCheck
{
    /// <summary>
    /// Process-wide flag for non-pool hosts. Ignored when TestSupport is enabled.
    /// </summary>
    public static bool NeedsReboot { get; set; }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (IsNeedsRebootActive())
        {
            const string description = "memory is corrupted. Restart process";
            logger.LogWarning(description);
            return Task.FromResult(HealthCheckResult.Unhealthy(description));
        }

        logger.LogDebug("Health check success");
        return Task.FromResult(HealthCheckResult.Healthy("Process memory is healthy"));
    }

    private bool IsNeedsRebootActive()
    {
        var requestScoped = NeedsRebootRequestScope.TryGetRequestScopedFlag(
            httpContextAccessor.HttpContext,
            testSupportOptions);
        return requestScoped ?? NeedsReboot;
    }
}
