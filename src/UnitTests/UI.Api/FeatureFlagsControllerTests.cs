using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsControllerTests
{
    private static readonly Dictionary<string, bool> RuntimeFlags = new()
    {
        [FeatureFlagNames.ShowPriorityBadges] = true,
        [FeatureFlagNames.BuildingManagement] = true,
        [FeatureFlagNames.VendorManagement] = false
    };

    [Test]
    public async Task Get_Should_ReturnQueryFlags_When_Called()
    {
        var bus = new StubBus(RuntimeFlags);
        var controller = CreateController(bus);

        var result = await controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.ShouldBe(RuntimeFlags, ignoreOrder: true);
        bus.Requests.Single().ShouldBeOfType<FeatureFlagsQuery>();
    }

    [Test]
    public async Task Get_Should_SerializeFlagKeysAsCatalogNames_When_UsingWebJsonDefaults()
    {
        var controller = CreateController(new StubBus(RuntimeFlags));

        var result = await controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        foreach (string key in RuntimeFlags.Keys)
        {
            doc.RootElement.TryGetProperty(key, out _).ShouldBeTrue($"expected key '{key}' in JSON");
        }
    }

    private static FeatureFlagsController CreateController(IBus bus) =>
        new(bus) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

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
}
