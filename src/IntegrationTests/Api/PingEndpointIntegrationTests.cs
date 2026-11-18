using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class PingEndpointIntegrationTests
{
    private DiagnosticsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DiagnosticsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndPlainTextPong_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBe("pong");
        var nosniff = response.Headers.GetValues("X-Content-Type-Options").ToArray();
        nosniff.ShouldBe(["nosniff"]);
    }

    [Test]
    public async Task Should_Return200AndPlainTextPong_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBe("pong");
    }

}
