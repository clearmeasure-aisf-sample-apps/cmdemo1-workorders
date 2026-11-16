using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DiagnosticsEndpointTests
{
    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubBus(IReadOnlyDictionary<string, bool> flags) : IBus
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            Requests.Add(request);
            return Task.FromResult((TResponse)(object)new FeatureFlagsResult(flags));
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }

    [Test]
    public async Task Get_Should_ReturnJson_WithEnvironmentUptimeAndRuntimeFeatureFlags_When_Called()
    {
        var clock = FixedClock();
        var bus = new StubBus(RuntimeFeatureFlags());
        var endpoint = DiagnosticsEndpointUnderTest(clock, bus);

        var result = await endpoint.Get();

        var payload = ReadDiagnosticsPayload(result);
        payload.Environment.ShouldBe("UnitTestEnv");
        payload.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
        payload.FeatureFlags[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        payload.FeatureFlags[FeatureFlagNames.ShowPriorityBadges].ShouldBeFalse();
        payload.FeatureFlags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
        bus.Requests.Single().ShouldBeOfType<FeatureFlagsQuery>();
    }

    private static FixedUtcTimeProvider FixedClock() =>
        new(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));

    private static Dictionary<string, bool> RuntimeFeatureFlags() => new()
    {
        [FeatureFlagNames.BuildingManagement] = true,
        [FeatureFlagNames.ShowPriorityBadges] = false,
        [FeatureFlagNames.VendorManagement] = true
    };

    private static DiagnosticsUnderTest DiagnosticsEndpointUnderTest(TimeProvider clock, StubBus bus) =>
        new(new StubHostEnvironment("UnitTestEnv"), clock, bus);

    private sealed class DiagnosticsUnderTest(IHostEnvironment hostEnvironment, TimeProvider clock, IBus bus)
    {
        public Task<IResult> Get() => DiagnosticsEndpoint.Get(hostEnvironment, clock, bus);
    }

    private static DiagnosticsResponse ReadDiagnosticsPayload(IResult result)
    {
        var content = result.ShouldBeContentResult();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<DiagnosticsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload;
    }
}
