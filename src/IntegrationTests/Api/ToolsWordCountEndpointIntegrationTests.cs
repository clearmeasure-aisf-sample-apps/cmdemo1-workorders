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

    [TestCase("hello world", 2)]
    [TestCase("", 0)]
    [TestCase("word", 1)]
    [TestCase("  one   two\nthree\tfour  ", 4)]
    [TestCase(" \t\r\n", 0)]
    public async Task Should_Return200WithWordCount_When_PostUnversioned(string text, int expectedWords)
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/word-count", new { text });

        await AssertWordCountAsync(response, expectedWords);
    }

    [TestCase("one\ntwo", 2)]
    [TestCase("", 0)]
    [TestCase("word", 1)]
    [TestCase("  one   two\nthree\tfour  ", 4)]
    [TestCase(" \t\r\n", 0)]
    public async Task Should_Return200WithWordCount_When_PostVersioned(string text, int expectedWords)
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/word-count", new { text });

        await AssertWordCountAsync(response, expectedWords);
    }

    [TestCase("/api/tools/word-count")]
    [TestCase("/api/v1.0/tools/word-count")]
    public async Task Should_ReturnZeroWords_When_TextMissing(string path)
    {
        var missingField = await _client!.PostAsJsonAsync(path, new { });
        await AssertWordCountAsync(missingField, 0);

        var nullText = await _client!.PostAsJsonAsync(path, new { text = (string?)null });
        await AssertWordCountAsync(nullText, 0);
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

    private static async Task AssertWordCountAsync(HttpResponseMessage response, int expectedWords)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.EnumerateObject().Select(property => property.Name).ShouldBe(["words"]);
        json.GetProperty("words").GetInt32().ShouldBe(expectedWords);
    }
}
