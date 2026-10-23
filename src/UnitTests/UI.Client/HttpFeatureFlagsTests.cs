using System.Net;
using System.Text;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class HttpFeatureFlagsTests
{
    [Test]
    public async Task IsEnabledAsync_Should_ReturnTrue_When_FlagIsTrue()
    {
        var handler = new StubFlagsHandler(HttpStatusCode.OK, """{"ShowPriorityBadges":true}""");
        var flags = new HttpFeatureFlags(Client(handler));

        var enabled = await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges);

        enabled.ShouldBeTrue();
        handler.LastPath.ShouldBe("/api/features/flags");
        (await flags.IsEnabledAsync("SampleFeatureB")).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_FlagIsFalse()
    {
        var flags = new HttpFeatureFlags(Client(new StubFlagsHandler(HttpStatusCode.OK, """{"ShowPriorityBadges":false}""")));

        var enabled = await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges);

        enabled.ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_ResponseOmitsTheFlag()
    {
        var flags = new HttpFeatureFlags(Client(new StubFlagsHandler(HttpStatusCode.OK, """{"SampleFeatureA":true}""")));

        var enabled = await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges);

        enabled.ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_UseCatalogDefault_When_ResponseIsNotSuccess()
    {
        var flags = new HttpFeatureFlags(Client(new StubFlagsHandler(HttpStatusCode.Unauthorized, "")));

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await flags.IsEnabledAsync("SampleFeatureB")).ShouldBeFalse();
    }

    private static HttpClient Client(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://localhost/") };

    private sealed class StubFlagsHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.AbsolutePath;
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
