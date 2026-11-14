using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class WorkOrderSeedSpecTests
{
    [Test]
    public void WorkOrderSeedSpec_WhenRead_MapsOntoMotherSeedFields()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/WorkOrders/WorkOrderSeedSpec.cs");

        source.ShouldContain("public WorkOrderSeedFields ToFields()");
        source.ShouldContain("AssigneeUserName = Assignee?.UserName");
        source.ShouldContain("Priority = Priority");
    }
}
