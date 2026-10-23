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
    public async Task IsEnabledAsync_Should_ReturnTrue_When_ResponseIsTrue()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"ShowPriorityBadges":true}""");
        var flags = Client(handler);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();

        handler.Calls.ShouldBe(1);
        handler.LastUri!.AbsolutePath.ShouldBe("/api/features/flags");
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_ResponseIsFalse()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"ShowPriorityBadges":false}""");
        var flags = Client(handler);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_KeyIsMissing()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"SampleFeatureA":true}""");
        var flags = Client(handler);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_UseCatalogDefault_When_StatusIsNotSuccess()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, "");
        var flags = Client(handler);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await flags.IsEnabledAsync("SampleFeatureA")).ShouldBeFalse();
        handler.Calls.ShouldBe(1);
    }

    private static HttpFeatureFlags Client(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new HttpFeatureFlags(http);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
