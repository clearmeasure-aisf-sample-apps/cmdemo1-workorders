using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderCommandButtonsTests
{
    private static readonly WorkOrder InProgress = new() { Number = "WO-1", Status = WorkOrderStatus.InProgress };
    private static readonly Employee Fixer = new("fixer", "Fix", "Er", "fixer@example.com");

    [Test]
    public void ShouldPutThePrimaryCommandLast_WhenCommandsAreSorted()
    {
        IStateCommand[] commands = [new InProgressToCompleteCommand(InProgress, Fixer), new InProgressToAssignedCommand(InProgress, Fixer)];

        var buttons = new WorkOrderCommandButtons(commands);

        buttons.Sorted.Last().TransitionVerbPresentTense.ShouldBe(InProgressToCompleteCommand.Name);
        buttons.CssClass(buttons.Sorted.Last()).ShouldBe("btn-app btn-app--primary");
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
