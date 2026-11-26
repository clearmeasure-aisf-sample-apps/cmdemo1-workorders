using System.Net;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
[Explicit("Starts full app")]
public class DiagnosticEndpointAccessWebTests
{
    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_Return401_When_ApiKeyEnabledAndAnonymous(string path)
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyEnabledAndAnonymousPostsReset()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/_diagnostics/reset-db-connections", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_Return200_When_ApiKeyEnabledAndAuthorized(string path)
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyEnabledAndAuthorizedPostsReset()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.PostAsync("/_diagnostics/reset-db-connections", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_Return404_When_ApiKeyOffOutsideDevelopmentAndTdd(string path)
    {
        await using var factory = new DiagnosticsEndpointAccessWebApplicationFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_Return404_When_ApiKeyOffOutsideDevelopmentAndTdd_PostsReset()
    {
        await using var factory = new DiagnosticsEndpointAccessWebApplicationFactory("Testing");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/_diagnostics/reset-db-connections", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_Return200_When_ApiKeyOffInDevelopment(string path)
    {
        await using var factory = new DiagnosticsEndpointAccessWebApplicationFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyOffInDevelopment_PostsReset()
    {
        await using var factory = new DiagnosticsEndpointAccessWebApplicationFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/_diagnostics/reset-db-connections", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_Return200_When_ApiKeyOffInTdd(string path)
    {
        await using var factory = new DiagnosticsEndpointAccessWebApplicationFactory("tdd");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
