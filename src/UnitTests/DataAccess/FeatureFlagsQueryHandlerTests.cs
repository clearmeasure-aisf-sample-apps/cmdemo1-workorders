using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class FeatureFlagsQueryHandlerTests
{
    [Test]
    public async Task Handle_Should_EvaluateEveryCatalogName_When_Queried()
    {
        var evaluator = new StubFeatureFlagEvaluator(FeatureFlagNames.BuildingManagement, FeatureFlagNames.ShowPriorityBadges);
        var handler = new FeatureFlagsQueryHandler(evaluator);

        var result = await handler.Handle(new FeatureFlagsQuery(), CancellationToken.None);

        result.Flags.Keys.ShouldBe(FeatureFlagNames.All);
        result.Flags[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        result.Flags[FeatureFlagNames.VendorManagement].ShouldBeFalse();
        result.Flags[FeatureFlagNames.ShowPriorityBadges].ShouldBeTrue();
        result.Flags[FeatureFlagNames.SampleFeatureA].ShouldBeFalse();
    }

    private sealed class StubFeatureFlagEvaluator(params string[] enabled) : IFeatureFlagEvaluator
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(enabled.Contains(featureName));
    }
}
