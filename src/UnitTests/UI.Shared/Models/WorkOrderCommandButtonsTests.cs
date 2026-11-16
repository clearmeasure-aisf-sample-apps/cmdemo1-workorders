using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderCommandButtonsTests
{
    private static readonly WorkOrder Draft = WorkOrderBuilder.Create()
        .WithNumber("WO-D")
        .WithStatus(WorkOrderStatus.Draft)
        .Build();

    private static readonly WorkOrder Assigned = WorkOrderBuilder.Create()
        .WithNumber("WO-A")
        .WithStatus(WorkOrderStatus.Assigned)
        .Build();

    private static readonly WorkOrder InProgress = WorkOrderBuilder.Create()
        .WithNumber("WO-1")
        .WithStatus(WorkOrderStatus.InProgress)
        .Build();

    private static readonly Employee Creator = EmployeeBuilder.Create("creator")
        .WithFirstName("Cre")
        .WithLastName("Ator")
        .WithEmail("creator@example.com")
        .Build();

    private static readonly Employee Fixer = EmployeeBuilder.Create("fixer")
        .WithFirstName("Fix")
        .WithLastName("Er")
        .WithEmail("fixer@example.com")
        .Build();

    [Test]
    public void ShouldPutThePrimaryCommandLast_WhenCommandsAreSorted()
    {
        IStateCommand[] commands =
        [
            new InProgressToCompleteCommand(InProgress, Fixer),
            new InProgressToAssignedCommand(InProgress, Fixer)
        ];

        var buttons = new WorkOrderCommandButtons(commands);

        buttons.Sorted.Last().TransitionVerbPresentTense.ShouldBe(InProgressToCompleteCommand.Name);
        buttons.CssClass(buttons.Sorted.Last()).ShouldBe("btn-app btn-app--primary");
    }

    [Test]
    public void ShouldOrderSecondaryThenDangerThenPrimary_WhenDraftCommandsAreSorted()
    {
        IStateCommand[] commands =
        [
            new DraftToAssignedCommand(Draft, Creator),
            new SaveDraftCommand(Draft, Creator),
            new AssignedToCancelledCommand(Assigned, Creator)
        ];

        var buttons = new WorkOrderCommandButtons(commands);

        buttons.Sorted[0].Role.ShouldBe(CommandRole.Secondary);
        buttons.Sorted[1].Role.ShouldBe(CommandRole.Destructive);
        buttons.Sorted[2].Role.ShouldBe(CommandRole.Forward);
        buttons.CssClass(buttons.Sorted[0]).ShouldBe("btn-app btn-app--secondary");
        buttons.CssClass(buttons.Sorted[1]).ShouldBe("btn-app btn-app--danger");
        buttons.CssClass(buttons.Sorted[2]).ShouldBe("btn-app btn-app--primary");
    }

    [Test]
    public void ShouldMapBeginToPrimary_WhenAssignedCommandsAreSorted()
    {
        var begin = new AssignedToInProgressCommand(Assigned, Fixer);
        IStateCommand[] commands = [new AssignedToCancelledCommand(Assigned, Creator), begin];

        var buttons = new WorkOrderCommandButtons(commands);

        buttons.Sorted.Last().ShouldBe(begin);
        buttons.CssClass(begin).ShouldBe("btn-app btn-app--primary");
        buttons.CssClass(commands[0]).ShouldBe("btn-app btn-app--danger");
    }

    [Test]
    public void ShouldAskForAShelveReason_WhenShelvingIsOffered()
    {
        IStateCommand[] commands = [new InProgressToAssignedCommand(InProgress, Fixer)];

        var buttons = new WorkOrderCommandButtons(commands);

        buttons.ShowsShelveReason.ShouldBeTrue();
        buttons.Any.ShouldBeTrue();
    }

    [Test]
    public void ShouldOfferNothing_WhenNoCommandIsValid()
    {
        var buttons = WorkOrderCommandButtons.None;

        buttons.Any.ShouldBeFalse();
        buttons.ShowsShelveReason.ShouldBeFalse();
        buttons.Sorted.ShouldBeEmpty();
    }
}
