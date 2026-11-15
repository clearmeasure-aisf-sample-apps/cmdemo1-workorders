using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

[TestFixture]
public class HttpAllowListTypesTests
{
    [Test]
    public void MasterNames_WhenListed_HasExactlyFortyThreeEntries()
    {
        HttpAllowListTypes.MasterNames.Count.ShouldBe(43);
    }

    [Test]
    public void MasterNames_WhenListed_AreUniqueAndSorted()
    {
        HttpAllowListTypes.MasterNames.ShouldBe(
            HttpAllowListTypes.MasterNames.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal));
    }
}
