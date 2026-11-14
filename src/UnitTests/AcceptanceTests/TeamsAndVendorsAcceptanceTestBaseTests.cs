using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class TeamsAndVendorsAcceptanceTestBaseTests
{
    [Test]
    public void TeamsAndVendorsAcceptanceTestBase_WhenRead_SeedsThroughApiBase()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TeamsAndVendors/TeamsAndVendorsAcceptanceTestBase.cs");

        source.ShouldContain("class TeamsAndVendorsAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("SeedTwoAssignedOpenOrdersAsync");
        source.ShouldContain("CreateTaggedTeamAsync");
        source.ShouldContain("CreateTaggedVendorAsync");
        source.ShouldNotContain("NewDbContext");
    }
}
