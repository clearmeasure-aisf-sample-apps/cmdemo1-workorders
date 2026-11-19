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
    }

    [Test]
    public async Task Handle_Should_EvaluateEveryCatalogNameForTheQueryUserName_When_QueryCarriesOne()
    {
        var evaluator = new StubFeatureFlagEvaluator();
        var handler = new FeatureFlagsQueryHandler(evaluator);

        await handler.Handle(new FeatureFlagsQuery("tlovejoy"), CancellationToken.None);

        evaluator.UserNames.Count.ShouldBe(FeatureFlagNames.All.Count);
        evaluator.UserNames.ShouldAllBe(userName => userName == "tlovejoy");
    }

    private sealed class StubFeatureFlagEvaluator(params string[] enabled) : IFeatureFlagEvaluator
    {
        /// <summary>The username every evaluation was asked for, in order.</summary>
        public List<string?> UserNames { get; } = [];

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            IsEnabledAsync(featureName, null, cancellationToken);

        public Task<bool> IsEnabledAsync(
            string featureName,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            UserNames.Add(userName);
            return Task.FromResult(enabled.Contains(featureName));
        }
    }
}
