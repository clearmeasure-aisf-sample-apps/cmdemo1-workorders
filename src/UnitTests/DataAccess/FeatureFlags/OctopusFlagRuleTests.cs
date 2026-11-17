using OpenFeature.Error;
using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags.OctopusFlagMother;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

[TestFixture]
public class OctopusFlagRuleTests
{
    [Test]
    public void Matches_Should_RequireEveryCondition_When_TheRuleHasSeveral()
    {
        var rule = Rule("EU at full rollout", IsOneOf("region", "eu"), Percentage(100));

        rule.Matches(InRegion("eu")).ShouldBeTrue();
        rule.Matches(InRegion("us")).ShouldBeFalse();
    }

    [Test]
    public void Matches_Should_NotReadLaterConditions_When_AnEarlierOneFails()
    {
        var rule = Rule("EU then malformed", IsOneOf("region", "eu"), Percentage(null));

        rule.Matches(InRegion("us")).ShouldBeFalse();
    }

    [Test]
    public void Matches_Should_ThrowParseErrors_When_TheRuleIsIncomplete()
    {
        Should.Throw<ParseErrorException>(() => Rule(null, Percentage(100)).Matches(null));
        Should.Throw<ParseErrorException>(() => Rule("No conditions").Matches(null));
        Should.Throw<ParseErrorException>(() => Rule("Null conditions", null).Matches(null));
        Should.Throw<ParseErrorException>(() => Rule("Missing condition", [null]).Matches(null));
    }
}
