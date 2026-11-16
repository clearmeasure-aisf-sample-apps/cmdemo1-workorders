using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForBeginTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldBeginWorkOrder()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("begin_cu")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty)
            .CreatedBy(currentUser).Assigned(currentUser).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, order);
        ApplyEdits(order);

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new AssignedToInProgressCommand(order, currentUser));

        ThenInProgress(result.WorkOrder.Id, currentUser);
    }

    private static void ApplyEdits(WorkOrder order)
    {
        order.Title = "new title";
        order.Description = "new desc";
        order.RoomNumber = "new room";
    }

    private static void ThenInProgress(Guid id, Employee currentUser)
    {
        var order = StateCommandHandlerSteps.RequireOrder(id);
        order.Title.ShouldBe(order.Title);
        order.Description.ShouldBe(order.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(currentUser);
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
    }
}
