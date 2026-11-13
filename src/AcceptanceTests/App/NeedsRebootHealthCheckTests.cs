using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Pool-slot coverage: NeedsReboot is request-scoped via test-support headers so this fixture
/// can run in the parallel lane without flipping <c>/_healthcheck</c> for other workers.
/// Process-wide behavior for tdd/uat/prod is covered by unit/web tests with TestSupport off.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NeedsRebootHealthCheckTests : AppAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test, Retry(2)]
    public async Task SetNeedsRebootTrue_HealthCheckReturnsUnhealthy()
    {
        LocalAcceptanceHost.Require();
        using var client = CreateHttpClient();

        using var request = CreateNeedsRebootHealthRequest(needsReboot: true);
        using var healthResponse = await client.SendAsync(request);
        var healthBody = await healthResponse.Content.ReadAsStringAsync();

        healthBody.ShouldContain("Unhealthy");
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootFalse_HealthCheckDoesNotReturnUnhealthy()
    {
        LocalAcceptanceHost.Require();
        using var client = CreateHttpClient();

        using var request = CreateNeedsRebootHealthRequest(needsReboot: false);
        using var healthResponse = await client.SendAsync(request);
        var healthBody = await healthResponse.Content.ReadAsStringAsync();

        healthBody.ShouldNotContain("Unhealthy");
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootRoute_ReturnsSummaryText()
    {
        using var client = CreateHttpClient();

        var trueResponse = await client.GetAsync("/_demo/setneedsreboot/true");
        trueResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trueText = await trueResponse.Content.ReadAsStringAsync();
        trueText.ShouldBe("NeedsReboot set to True");

        var falseResponse = await client.GetAsync("/_demo/setneedsreboot/false");
        falseResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var falseText = await falseResponse.Content.ReadAsStringAsync();
        falseText.ShouldBe("NeedsReboot set to False");
    }

    [Test, Retry(2)]
    public async Task SetNeedsRebootTrue_DetailedHealthCheckShowsCorruptionMessage()
    {
        LocalAcceptanceHost.Require();
        using var client = CreateHttpClient();

        using var request = CreateNeedsRebootHealthRequest(needsReboot: true, path: "/_healthcheck/detailed");
        using var detailedResponse = await client.SendAsync(request);
        var detailedBody = await detailedResponse.Content.ReadAsStringAsync();

        detailedBody.ShouldContain("memory is corrupted");
        detailedBody.ShouldContain("Restart process");
    }

    [Test, Retry(2)]
    public async Task ProcessWideDemoSwitch_DoesNotPoisonBareHealthCheck_OnPoolSlot()
    {
        LocalAcceptanceHost.Require();
        using var client = CreateHttpClient();

        await client.GetAsync("/_demo/setneedsreboot/true");
        using var healthResponse = await client.GetAsync("/_healthcheck");
        var healthBody = await healthResponse.Content.ReadAsStringAsync();

        healthBody.ShouldNotContain("Unhealthy");
        await client.GetAsync("/_demo/setneedsreboot/false");
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl)
        };
    }

    private static HttpRequestMessage CreateNeedsRebootHealthRequest(bool needsReboot, string path = "/_healthcheck")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(
            AcceptanceTestSupportSettings.SecretHeaderName,
            AcceptanceTestSupportSettings.Secret);
        request.Headers.TryAddWithoutValidation(
            AcceptanceTestSupportSettings.NeedsRebootHeaderName,
            needsReboot ? "true" : "false");
        return request;
    }
}
