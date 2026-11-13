using System.Net;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

/// <summary>
/// Pool-slot shape (TestSupport on): NeedsReboot is request-scoped and must not poison concurrent probes.
/// </summary>
[TestFixture]
public class NeedsRebootPoolSlotWebTests
{
    [TearDown]
    public void TearDown()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
    }

    [Test]
    public async Task Should_ReportUnhealthy_When_AuthorizedNeedsRebootHeaderIsTrue()
    {
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/_healthcheck");
        request.Headers.TryAddWithoutValidation(
            TestSupportConstants.SecretHeaderName,
            TestSupportEnabledWebApplicationFactory.Secret);
        request.Headers.TryAddWithoutValidation(TestSupportConstants.NeedsRebootHeaderName, "true");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldContain("Unhealthy");
    }

    [Test]
    public async Task Should_StayHealthy_When_ProcessWideFlagSet_OnPoolSlot()
    {
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();
        await client.GetAsync("/_demo/setneedsreboot/true");

        using var response = await client.GetAsync("/_healthcheck");
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain("Unhealthy");
    }

    [Test]
    public async Task Should_IgnoreNeedsRebootHeader_WithoutSecret()
    {
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/_healthcheck");
        request.Headers.TryAddWithoutValidation(TestSupportConstants.NeedsRebootHeaderName, "true");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain("Unhealthy");
    }
}
