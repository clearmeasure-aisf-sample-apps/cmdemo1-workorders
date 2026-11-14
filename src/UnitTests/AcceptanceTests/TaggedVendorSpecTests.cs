using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class TaggedVendorSpecTests
{
    [Test]
    public void TaggedVendorSpec_WhenRead_HoldsVendorCreateFields()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TeamsAndVendors/TaggedVendorSpec.cs");

        source.ShouldContain("class TaggedVendorSpec");
        source.ShouldContain("NameSuffix");
        source.ShouldContain("Trade");
        source.ShouldContain("ContactName");
        source.ShouldContain("EmailAddress");
        source.ShouldContain("PhoneNumber");
        source.ShouldContain("IsActive");
    }
}
