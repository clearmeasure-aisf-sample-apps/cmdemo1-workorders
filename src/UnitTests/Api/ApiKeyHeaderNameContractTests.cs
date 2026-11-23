using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

/// <summary>
/// The one request-header name for the shared secret: the rate-limiting partition header and the shipped
/// configuration both come from <see cref="ApiKeyConstants.HeaderName"/>.
/// </summary>
[TestFixture]
public class ApiKeyHeaderNameContractTests
{
    [Test]
    public void Should_UseSharedHeaderName_When_OptionsDefaultIsRead()
    {
        new ApiRateLimitingOptions().ApiKeyHeaderName.ShouldBe(ApiKeyConstants.HeaderName);
    }

    [Test]
    public void Should_UseSharedHeaderName_When_AppsettingsRateLimitingSectionIsRead()
    {
        using var document = JsonDocument.Parse(RepositoryCheckout.Current.ReadText("src/UI/Server/appsettings.json"));

        document.RootElement.GetProperty("ApiRateLimiting").GetProperty("ApiKeyHeaderName").GetString()
            .ShouldBe(ApiKeyConstants.HeaderName);
    }
}
