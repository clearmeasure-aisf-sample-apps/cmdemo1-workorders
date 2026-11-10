using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Startup;

[TestFixture]
public class ServerPipelineTests
{
    [Test]
    public async Task ShouldAnswerTheVersion_WhenVersionIsRequested()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var version = JsonDocument.Parse(await client.GetStringAsync("/_version"));

        version.RootElement.TryGetProperty("version", out _).ShouldBeTrue();
    }

    [Test]
    public async Task ShouldAnswerNotFound_WhenAMachinePathMatchesNoEndpoint()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/no-such-endpoint");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldSetTheRebootFlag_WhenTheDemoSwitchIsCalled()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/_demo/setneedsreboot/false");

        body.ShouldBe("NeedsReboot set to False");
    }
}
