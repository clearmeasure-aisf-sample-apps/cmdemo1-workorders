using System.Net;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// Creates the current-user employee for a browser fixture: test-support when the host
/// exposes it (pool slots), SQL fallback when the probe returns 404 (tdd/uat/prod).
/// Lives in UnitTests so the probe-path can be stub-tested without Playwright.
/// </summary>
public sealed class AcceptanceEmployeeProvisioner(
    Func<Task<HttpStatusCode>> probeAsync,
    Func<string, Task<Employee>> createViaTestSupportAsync,
    Func<string, Employee> createViaSql)
{
    /// <summary>
    /// Probes test-support, then creates via the API path or the SQL fallback.
    /// </summary>
    public async Task<AcceptanceEmployeeProvision> CreateAsync(string testTag)
    {
        var status = await probeAsync();
        if (status == HttpStatusCode.NotFound)
        {
            return new AcceptanceEmployeeProvision(createViaSql(testTag), TestSupportAvailable: false);
        }

        return new AcceptanceEmployeeProvision(
            await createViaTestSupportAsync(testTag),
            TestSupportAvailable: true);
    }
}
