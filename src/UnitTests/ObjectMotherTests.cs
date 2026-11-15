using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

[TestFixture]
public class ObjectMotherTests
{
    [Test]
    public void ShouldBuildDraftWorkOrder_WithCreator()
    {
        var creator = ObjectMother.Creator("ned");

        var order = ObjectMother.DraftWorkOrder(creator);

        order.Status.ShouldBe(WorkOrderStatus.Draft);
        order.Creator.ShouldBe(creator);
        creator.CanCreateWorkOrder().ShouldBeTrue();
    }

    [Test]
    public void ShouldBuildEmployeeAndFulfiller_WithNamedRoles()
    {
        var employee = ObjectMother.Employee("patty", "Patty", "Bouvier");
        var fulfiller = ObjectMother.Fulfiller("willie");

        employee.UserName.ShouldBe("patty");
        employee.FirstName.ShouldBe("Patty");
        fulfiller.CanFulfillWorkOrder().ShouldBeTrue();
        fulfiller.FirstName.ShouldBe("Groundskeeper");
    }
}
