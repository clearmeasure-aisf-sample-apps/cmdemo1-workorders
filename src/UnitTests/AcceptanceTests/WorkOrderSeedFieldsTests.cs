using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class WorkOrderSeedFieldsTests
{
    [Test]
    public void WorkOrderSeedFields_WhenRead_DefaultsPriorityToMedium()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/WorkOrderSeedFields.cs");

        source.ShouldContain("class WorkOrderSeedFields");
        source.ShouldContain("WorkOrderPriority.Medium");
        source.ShouldContain("AssigneeUserName");
        source.ShouldContain("DueDate");
    }
}
