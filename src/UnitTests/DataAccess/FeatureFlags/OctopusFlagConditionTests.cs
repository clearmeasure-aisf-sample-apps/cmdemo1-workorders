using OpenFeature.Error;
using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags.OctopusFlagMother;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

[TestFixture]
public class OctopusFlagConditionTests
{
    [TestCase("eu", true)]
    [TestCase("EU", true)]
    [TestCase("us", false)]
    public void Matches_Should_CompareTheAttributeWithoutCase_When_TheTypeIsOneOf(string region, bool expected)
    {
        IsOneOf("Region", "eu", "uk").Matches(InRegion(region)).ShouldBe(expected);
    }

    [Test]
    public void Matches_Should_NegateTheMatch_When_TheTypeIsNotOneOf()
    {
        IsNotOneOf("region", "eu").Matches(InRegion("us")).ShouldBeTrue();
        IsNotOneOf("region", "eu").Matches(InRegion("eu")).ShouldBeFalse();
    }

    [Test]
    public void Matches_Should_NotMatchAnAttributeCondition_When_ThereIsNoContext()
    {
        IsOneOf("region", "eu").Matches(null).ShouldBeFalse();
    }

    [Test]
    public void Matches_Should_ThrowParseErrors_When_AnAttributeConditionIsIncomplete()
    {
        Should.Throw<ParseErrorException>(() => IsOneOf(null, "eu").Matches(InRegion("eu")));
        Should.Throw<ParseErrorException>(() => IsOneOf("region").Matches(InRegion("eu")));
        Should.Throw<ParseErrorException>(() => IsOneOf("region", null).Matches(InRegion("eu")));
        Should.Throw<ParseErrorException>(() => IsNotOneOf("region", "eu", null).Matches(InRegion("eu")));
    }

    [TestCase(100, true)]
    [TestCase(99, false)]
    [TestCase(0, false)]
    public void Matches_Should_IncludeEveryoneOnlyAtAFullRollout_When_NoTargetingKeyIsSent(int percentage, bool expected)
    {
        Percentage(percentage).Matches(InRegion("eu")).ShouldBe(expected);
    }

    [Test]
    public void Matches_Should_ThrowParseErrors_When_ARolloutPercentageIsMissingOrOutOfRange()
    {
        Should.Throw<ParseErrorException>(() => Percentage(null).Matches(null));
        Should.Throw<ParseErrorException>(() => Percentage(-1).Matches(null));
        Should.Throw<ParseErrorException>(() => Percentage(101).Matches(null));
    }

    [Test]
    public void Matches_Should_ReportATargetingKeyRollout_When_ATargetingKeyIsSent()
    {
        Should.Throw<GeneralException>(() => Percentage(50).Matches(WithTargetingKey("user-1")));
    }

    [Test]
    public void Matches_Should_NotMatch_When_TheTypeIsNewerThanTheProvider()
    {
        OfType("condition-from-the-future").Matches(InRegion("eu")).ShouldBeFalse();
    }

    [Test]
    public void Matches_Should_ThrowAParseError_When_TheTypeIsMissing()
    {
        Should.Throw<ParseErrorException>(() => OfType(null).Matches(InRegion("eu")));
    }
}
