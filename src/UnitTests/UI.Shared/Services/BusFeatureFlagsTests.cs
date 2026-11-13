using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class BusFeatureFlagsTests
{
    [Test]
    public async Task IsEnabledAsync_Should_ReturnTrue_When_QueryReportsTrue()
    {
        var bus = new StubBus(() => Flags((FeatureFlagNames.ShowPriorityBadges, true)));
        var flags = new BusFeatureFlags(bus);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();

        bus.Requests.Count.ShouldBe(1);
        bus.Requests[0].ShouldBeOfType<FeatureFlagsQuery>();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_QueryReportsFalse()
    {
        var flags = new BusFeatureFlags(new StubBus(() => Flags(
            (FeatureFlagNames.ShowPriorityBadges, false), (FeatureFlagNames.VendorManagement, false))));

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeFalse();
        (await flags.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_KeyIsMissing()
    {
        var flags = new BusFeatureFlags(new StubBus(() => Flags((FeatureFlagNames.VendorManagement, true))));

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeFalse();
        (await flags.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnBothManagementFlags_When_QueryReportsThem()
    {
        var flags = new BusFeatureFlags(new StubBus(() => Flags(
            (FeatureFlagNames.BuildingManagement, true), (FeatureFlagNames.VendorManagement, true))));

        (await flags.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeTrue();
        (await flags.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
    }

    [TestCase(typeof(InvalidOperationException))]
    [TestCase(typeof(HttpRequestException))]
    [TestCase(typeof(TaskCanceledException))]
    [TestCase(typeof(JsonException))]
    public async Task IsEnabledAsync_Should_UseCatalogDefault_When_QueryFails(Type exceptionType)
    {
        var bus = new StubBus(() => throw (Exception)Activator.CreateInstance(exceptionType)!);
        var flags = new BusFeatureFlags(bus);

        (await flags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await flags.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
        (await flags.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
        bus.Requests.Count.ShouldBe(1);
    }

    private static FeatureFlagsResult Flags(params (string Name, bool Enabled)[] flags) =>
        new(flags.ToDictionary(flag => flag.Name, flag => flag.Enabled));

    private sealed class StubBus(Func<FeatureFlagsResult> respond) : IBus
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            Requests.Add(request);
            return Task.FromResult((TResponse)(object)respond());
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
