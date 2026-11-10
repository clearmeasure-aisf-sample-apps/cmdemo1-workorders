using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CreateDatedWorkOrdersResultTests
{
    [Test]
    public void ShouldListNothing_WhenTheBatchFailed()
    {
        var result = CreateDatedWorkOrdersResult.Failed("Assignee 'x' was not found. No work orders were created.");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Assignee 'x' was not found. No work orders were created.");
        result.WorkOrders.ShouldBeEmpty();
    }

    [Test]
    public void ShouldListEveryNumberAndDueDate_WhenTheBatchWasCreated()
    {
        WorkOrder[] created =
        [
            new() { Number = "WO-1", DueDate = new DateOnly(2026, 11, 14) },
            new() { Number = "WO-2", DueDate = new DateOnly(2026, 11, 21) }
        ];

        var result = CreateDatedWorkOrdersResult.Created(created);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe("Created 2 work orders.");
        result.WorkOrders.ShouldBe(
        [
            new CreatedDatedWorkOrder("WO-1", new DateOnly(2026, 11, 14)),
            new CreatedDatedWorkOrder("WO-2", new DateOnly(2026, 11, 21))
        ]);
    }
}
