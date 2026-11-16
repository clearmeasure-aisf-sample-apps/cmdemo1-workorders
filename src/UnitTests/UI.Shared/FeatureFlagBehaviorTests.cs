using System.Reflection;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class FeatureFlagBehaviorTests
{
    [Test]
    public async Task Handle_Should_ThrowFeatureDisabled_When_RequiredFlagIsOff()
    {
        var evaluator = new StubFeatureFlagEvaluator();
        var behavior = new FeatureFlagBehavior<GatedProbeRequest, string>(evaluator);
        var handlerCalled = false;

        var ex = await Should.ThrowAsync<FeatureDisabledException>(async () => await behavior.Handle(
            new GatedProbeRequest(),
            (_, _) =>
            {
                handlerCalled = true;
                return ValueTask.FromResult("handled");
            },
            CancellationToken.None));

        ex.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        ex.RequestName.ShouldBe(nameof(GatedProbeRequest));
        ex.Message.ShouldContain(FeatureFlagNames.VendorManagement);
        handlerCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_Should_CallNext_When_RequiredFlagIsOn()
    {
        var evaluator = new StubFeatureFlagEvaluator { [FeatureFlagNames.VendorManagement] = true };
        var behavior = new FeatureFlagBehavior<GatedProbeRequest, string>(evaluator);

        var result = await behavior.Handle(new GatedProbeRequest(), (_, _) => ValueTask.FromResult("handled"),
            CancellationToken.None);

        result.ShouldBe("handled");
        evaluator.Evaluated.ShouldBe([FeatureFlagNames.VendorManagement]);
    }

    [Test]
    public async Task Handle_Should_SkipEvaluation_When_RequestHasNoRequiresFeature()
    {
        var evaluator = new StubFeatureFlagEvaluator();
        var behavior = new FeatureFlagBehavior<OpenProbeRequest, string>(evaluator);

        var result = await behavior.Handle(new OpenProbeRequest(), (_, _) => ValueTask.FromResult("handled"),
            CancellationToken.None);

        result.ShouldBe("handled");
        evaluator.Evaluated.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_Should_RequireEveryFlag_When_RequestCarriesSeveral()
    {
        var evaluator = new StubFeatureFlagEvaluator { [FeatureFlagNames.BuildingManagement] = true };
        var behavior = new FeatureFlagBehavior<DoublyGatedProbeRequest, string>(evaluator);

        var ex = await Should.ThrowAsync<FeatureDisabledException>(async () => await behavior.Handle(
            new DoublyGatedProbeRequest(), (_, _) => ValueTask.FromResult("handled"), CancellationToken.None));

        ex.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
    }

    [Test]
    public async Task Handle_Should_DenyBuildingQueries_When_BuildingManagementIsOff()
    {
        var evaluator = new StubFeatureFlagEvaluator();

        var summary = await Should.ThrowAsync<FeatureDisabledException>(async () => await
            new FeatureFlagBehavior<BuildingRoomSummaryQuery, BuildingSummary[]>(evaluator).Handle(
                new BuildingRoomSummaryQuery(),
                (_, _) => ValueTask.FromResult(Array.Empty<BuildingSummary>()),
                CancellationToken.None));
        summary.FeatureName.ShouldBe(FeatureFlagNames.BuildingManagement);
        summary.RequestName.ShouldBe(nameof(BuildingRoomSummaryQuery));

        var buildings = await Should.ThrowAsync<FeatureDisabledException>(async () => await
            new FeatureFlagBehavior<WorkOrderBuildingsQuery, string[]>(evaluator).Handle(
                new WorkOrderBuildingsQuery(),
                (_, _) => ValueTask.FromResult(Array.Empty<string>()),
                CancellationToken.None));
        buildings.FeatureName.ShouldBe(FeatureFlagNames.BuildingManagement);
        buildings.RequestName.ShouldBe(nameof(WorkOrderBuildingsQuery));
        evaluator.Evaluated.ShouldBe([
            FeatureFlagNames.BuildingManagement,
            FeatureFlagNames.BuildingManagement
        ]);
    }

    [Test]
    public async Task Handle_Should_RunBuildingQueries_When_BuildingManagementIsOn()
    {
        var evaluator = new StubFeatureFlagEvaluator { [FeatureFlagNames.BuildingManagement] = true };
        var summaryCalled = false;
        var namesCalled = false;

        var summary = await new FeatureFlagBehavior<BuildingRoomSummaryQuery, BuildingSummary[]>(evaluator).Handle(
            new BuildingRoomSummaryQuery(),
            (_, _) =>
            {
                summaryCalled = true;
                return ValueTask.FromResult(Array.Empty<BuildingSummary>());
            },
            CancellationToken.None);
        var names = await new FeatureFlagBehavior<WorkOrderBuildingsQuery, string[]>(evaluator).Handle(
            new WorkOrderBuildingsQuery(),
            (_, _) =>
            {
                namesCalled = true;
                return ValueTask.FromResult(new[] { "Kitchen" });
            },
            CancellationToken.None);

        summary.ShouldBeEmpty();
        names.ShouldBe(["Kitchen"]);
        summaryCalled.ShouldBeTrue();
        namesCalled.ShouldBeTrue();
    }

    [Test]
    public void SearchReportsAndDraftCommands_Should_NotRequireBuildingManagement()
    {
        foreach (var type in new[]
                 {
                     typeof(WorkOrderSpecificationQuery),
                     typeof(WorkOrderReportsQuery),
                     typeof(SaveDraftCommand),
                     typeof(EmployeeWorkLoadSummaryQuery)
                 })
        {
            type.GetCustomAttributes<RequiresFeatureAttribute>().ShouldBeEmpty();
        }
    }

    [Test]
    public async Task Send_Should_DenyGatedRequestThroughMediatorPipeline_When_FlagIsOff()
    {
        await using var provider = BuildPipeline(new StubFeatureFlagEvaluator());
        var bus = new Bus(provider.GetRequiredService<IMediator>());

        await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new VendorsQuery()));
        (await bus.Send(new TeamsQuery())).ShouldBeSameAs(ProbeTeams);
    }

    [Test]
    public async Task Send_Should_RunGatedHandlerThroughMediatorPipeline_When_FlagIsOn()
    {
        await using var provider = BuildPipeline(
            new StubFeatureFlagEvaluator { [FeatureFlagNames.VendorManagement] = true });
        var bus = new Bus(provider.GetRequiredService<IMediator>());

        (await bus.Send(new VendorsQuery())).ShouldBeSameAs(ProbeVendors);
        (await bus.Send((object)new VendorsQuery())).ShouldBeSameAs(ProbeVendors);
    }

    private static readonly Vendor[] ProbeVendors = [];
    private static readonly Team[] ProbeTeams = [];

    /// <summary>The MCP host's generated pipeline with canned handlers for one gated and one open query.</summary>
    private static ServiceProvider BuildPipeline(IFeatureFlagEvaluator evaluator) =>
        HostPipeline.Build(evaluator, services =>
        {
            services.AddTransient<IRequestHandler<VendorsQuery, Vendor[]>>(_ =>
                new CannedHandler<VendorsQuery, Vendor[]>(ProbeVendors));
            services.AddTransient<IRequestHandler<TeamsQuery, Team[]>>(_ =>
                new CannedHandler<TeamsQuery, Team[]>(ProbeTeams));
        });

    [RequiresFeature(FeatureFlagNames.VendorManagement)]
    private record GatedProbeRequest : IRequest<string>;

    private record OpenProbeRequest : IRequest<string>;

    [RequiresFeature(FeatureFlagNames.BuildingManagement)]
    [RequiresFeature(FeatureFlagNames.VendorManagement)]
    private record DoublyGatedProbeRequest : IRequest<string>;

    private sealed class CannedHandler<TRequest, TResponse>(TResponse response) : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(response);
    }

    private sealed class StubFeatureFlagEvaluator : Dictionary<string, bool>, IFeatureFlagEvaluator
    {
        public List<string> Evaluated { get; } = [];

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            Evaluated.Add(featureName);
            return Task.FromResult(TryGetValue(featureName, out var enabled) && enabled);
        }
    }
}
