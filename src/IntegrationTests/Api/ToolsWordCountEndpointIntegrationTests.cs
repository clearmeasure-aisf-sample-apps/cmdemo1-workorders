using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsWordCountEndpointIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

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

    [TestCase("/api/tools/word-count", "hello world", 2)]
    [TestCase("/api/v1.0/tools/word-count", "hello world", 2)]
    [TestCase("/api/tools/word-count", "", 0)]
    [TestCase("/api/v1.0/tools/word-count", "", 0)]
    [TestCase("/api/tools/word-count", "word", 1)]
    [TestCase("/api/v1.0/tools/word-count", "word", 1)]
    [TestCase("/api/tools/word-count", "  one   two\nthree\tfour  ", 4)]
    [TestCase("/api/v1.0/tools/word-count", "  one   two\nthree\tfour  ", 4)]
    [TestCase("/api/tools/word-count", " \t\r\n ", 0)]
    [TestCase("/api/v1.0/tools/word-count", " \t\r\n ", 0)]
    public async Task Should_Return200WithWords_When_TextProvided(string path, string text, int expectedWords)
    {
        var response = await _client!.PostAsJsonAsync(path, new { text });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("words").GetInt32().ShouldBe(expectedWords);
        json.EnumerateObject().Count().ShouldBe(1);
    }

    [TestCase("/api/tools/word-count")]
    [TestCase("/api/v1.0/tools/word-count")]
    public async Task Should_Return200WithZeroWords_When_TextMissing(string path)
    {
        var missingField = await _client!.PostAsJsonAsync(path, new { });
        var nullText = await _client!.PostAsJsonAsync(path, new { text = (string?)null });

        foreach (var response in new[] { missingField, nullText })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            json.GetProperty("words").GetInt32().ShouldBe(0);
            json.EnumerateObject().Count().ShouldBe(1);
        }
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/word-count", new { text = "test" });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/word-count", new { text = "test" });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
