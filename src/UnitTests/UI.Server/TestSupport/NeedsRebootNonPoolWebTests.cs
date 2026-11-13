using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

/// <summary>
/// Non-pool shape (TestSupport off): process-wide NeedsReboot still drives <c>/_healthcheck</c> for tdd/uat/prod.
/// </summary>
[TestFixture]
public class NeedsRebootNonPoolWebTests
{
    [TearDown]
    public void TearDown()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
    }

    [Test]
    public async Task Should_ReportUnhealthy_When_ProcessWideFlagSet_OnNonPoolHost()
    {
        await using var factory = new TestSupportDisabledWebApplicationFactory();
        using var client = factory.CreateClient();
        await client.GetAsync("/_demo/setneedsreboot/true");

        using var response = await client.GetAsync("/_healthcheck");
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldContain("Unhealthy");
    }

    [Test]
    public async Task Should_ReportHealthy_When_ProcessWideFlagCleared_OnNonPoolHost()
    {
        await using var factory = new TestSupportDisabledWebApplicationFactory();
        using var client = factory.CreateClient();
        await client.GetAsync("/_demo/setneedsreboot/true");
        await client.GetAsync("/_demo/setneedsreboot/false");

        using var response = await client.GetAsync("/_healthcheck");
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain("Unhealthy");
    }
}
