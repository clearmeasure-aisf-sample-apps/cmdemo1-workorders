using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class BuildingsAcceptanceTestBaseTests
{
    [Test]
    public void BuildingsAcceptanceTestBase_WhenRead_SeedsThroughApiBase()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Buildings/BuildingsAcceptanceTestBase.cs");

        source.ShouldContain("class BuildingsAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("SeedBuildingOrderAsync");
        source.ShouldContain("TaggedBuilding");
        source.ShouldNotContain("NewDbContext");
    }
}
