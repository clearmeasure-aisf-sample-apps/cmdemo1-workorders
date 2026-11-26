using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
[Explicit("Starts full app")]
public class RetiredSampleEndpointsTests
{
    [TestCase("POST", "/api/tools/base64")]
    [TestCase("POST", "/api/tools/case")]
    [TestCase("POST", "/api/tools/uppercase")]
    [TestCase("POST", "/api/tools/hash")]
    [TestCase("POST", "/api/tools/slugify")]
    [TestCase("POST", "/api/tools/word-count")]
    [TestCase("GET", "/api/tools/random")]
    [TestCase("POST", "/api/tools/guid-generator")]
    [TestCase("GET", "/api/tools/timestamp-converter")]
    [TestCase("GET", "/api/echo")]
    [TestCase("GET", "/weatherforecast")]
    [TestCase("GET", "/api/v1.0/tools/base64")]
    [TestCase("GET", "/api/v1.0/echo")]
    [TestCase("GET", "/api/v1.0/WeatherForecast")]
    public async Task Should_Return404_When_RetiredSampleEndpointIsCalled(string method, string path)
    {
        await using var factory = new RetiredSampleEndpointsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await SendAsync(client, method, path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_Return200_When_WorkOrderStatusesEndpointIsCalled()
    {
        await using var factory = new RetiredSampleEndpointsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/work-order-statuses");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path) =>
        await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
}
