using System.Text.Json;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

[TestFixture]
public class OctopusFlagsJsonContextTests
{
    [Test]
    public void Deserialize_Should_ReadServerResolvedToggles_When_TheResponseIsRecorded()
    {
        var toggles = Read(OctopusTogglesMother.TddManagementFlagsOn);

        toggles.Select(toggle => (toggle.Slug, toggle.Value)).ShouldBe(
            [("building-management", true), ("vendor-management", true), ("show-priority-badges", false)]);
        toggles.ShouldAllBe(toggle => toggle.Reason != null && toggle.Rules == null);
    }

    [Test]
    public void Deserialize_Should_ReadRulesAndConditions_When_TheToggleIsDeferred()
    {
        var toggle = Read(OctopusTogglesMother.EuRegionOnly)[0];

        toggle.EvaluationKey.ShouldBe("evaluation-key");
        var condition = toggle.Rules.ShouldHaveSingleItem()!.Conditions.ShouldHaveSingleItem();
        condition.ShouldBe(new OctopusFlagCondition("context-attribute-is-one-of", "region", ["eu"], null), new ConditionComparer());
    }

    [Test]
    public void Deserialize_Should_KeepNullsAndIgnoreUnknownFields_When_TheResponseIsMalformedOrNewer()
    {
        var malformed = Read(OctopusTogglesMother.MalformedToggles);
        var newer = Read(OctopusTogglesMother.ConditionFromANewerServer);

        malformed.Single(toggle => toggle.Slug == "a-missing-rule").Rules.ShouldBe([null]);
        malformed.Single(toggle => toggle.Slug == "neither-value-nor-rules").Rules.ShouldBeNull();
        newer.ShouldHaveSingleItem().Rules!.Single()!.Conditions!.Single()!.Type.ShouldBe("condition-from-the-future");
    }

    private static OctopusFlagEvaluation[] Read(string json) =>
        JsonSerializer.Deserialize(json, OctopusFlagsJsonContext.Default.OctopusFlagEvaluationArray)!;

    /// <summary>Records compare arrays by reference; conditions match when every field and value matches.</summary>
    private sealed class ConditionComparer : IEqualityComparer<OctopusFlagCondition?>
    {
        public bool Equals(OctopusFlagCondition? x, OctopusFlagCondition? y) =>
            x is not null && y is not null && x.Type == y.Type && x.Key == y.Key && x.Percentage == y.Percentage
            && (x.Values ?? []).SequenceEqual(y.Values ?? []);

        public int GetHashCode(OctopusFlagCondition? obj) => obj?.Type?.GetHashCode(StringComparison.Ordinal) ?? 0;
    }
}
