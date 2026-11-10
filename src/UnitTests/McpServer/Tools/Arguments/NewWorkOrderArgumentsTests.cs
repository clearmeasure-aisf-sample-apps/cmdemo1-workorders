using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class NewWorkOrderArgumentsTests
{
    [Test]
    public void ShouldBuildADraft_WhenEveryArgumentIsValid()
    {
        var creator = WorkOrderToolMother.Creator();
        var arguments = new NewWorkOrderArguments
        {
            Title = "Fix the boiler", Description = "It is cold.", RoomNumber = "101", DueDate = "2027-03-01",
            Instructions = "Call first", Priority = "high", Building = " Annex "
        };

        var draft = arguments.ToDraft(creator, "WO-42");

        arguments.Error.ShouldBeNull();
        draft.Number.ShouldBe("WO-42");
        draft.Status.ShouldBe(WorkOrderStatus.Draft);
        draft.Creator.ShouldBe(creator);
        draft.DueDate.ShouldBe(new DateOnly(2027, 3, 1));
        draft.Priority.ShouldBe(WorkOrderPriority.High);
        draft.Building.ShouldBe("Annex");
    }

    [Test]
    public void ShouldDefaultTheOptionalFields_WhenTheyAreOmitted()
    {
        var arguments = new NewWorkOrderArguments { Title = "Fix the boiler", Description = "It is cold." };

        var draft = arguments.ToDraft(WorkOrderToolMother.Creator(), "WO-43");

        draft.DueDate.ShouldBeNull();
        draft.Priority.ShouldBe(WorkOrderPriority.Medium);
        draft.Building.ShouldBeNull();
    }

    [TestCase(" ", null, null, "Description is required.")]
    [TestCase("It is cold.", "03/01/2027", null, "Invalid due date '03/01/2027'. Use yyyy-MM-dd.")]
    [TestCase("It is cold.", null, "Urgent", "Invalid priority 'Urgent'. Valid values are: Low, Medium, High.")]
    public void ShouldReportTheFirstError_WhenAnArgumentBreaksItsRule(string description, string? dueDate, string? priority, string expected)
    {
        var arguments = new NewWorkOrderArguments
        {
            Title = "Fix the boiler", Description = description, DueDate = dueDate, Priority = priority
        };

        var error = arguments.Error;

        error.ShouldBe(expected);
    }

    [Test]
    public void ShouldRejectTheDescription_WhenItIsLongerThanTheMaximum()
    {
        var description = new string('d', WorkOrder.DescriptionMaxLength + 1);

        var error = NewWorkOrderArguments.DescriptionError(description);

        error.ShouldBe($"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters.");
    }
}
