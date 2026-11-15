using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class ShelveReasonTests
{
    [TestCase("Shelve")]
    [TestCase("InProgressToAssignedCommand")]
    public void ShouldTrimTheReason_WhenAShelvingCommandGivesOne(string commandName)
    {
        var reason = ShelveReason.For(commandName, "  Waiting on parts  ");

        reason.Text.ShouldBe("Waiting on parts");
        reason.Error.ShouldBeNull();
    }

    [TestCase(null)]
    [TestCase("   ")]
    public void ShouldRequireAReason_WhenAShelvingCommandGivesNone(string? text)
    {
        var reason = ShelveReason.For("Shelve", text);

        reason.Error.ShouldBe(WorkOrderFieldRules.ShelveRequiresReason);
    }

    [Test]
    public void ShouldRejectTheReason_WhenItIsLongerThanTheMaximum()
    {
        var reason = ShelveReason.For("Shelve", new string('r', InProgressToAssignedCommand.ReasonMaxLength + 1));

        reason.Error.ShouldBe(WorkOrderFieldRules.ReasonCannotExceed);
    }

    [Test]
    public void ShouldIgnoreTheReason_WhenTheCommandDoesNotShelve()
    {
        var reason = ShelveReason.For("AssignedToInProgressCommand", "anything");

        reason.Text.ShouldBeNull();
        reason.Error.ShouldBeNull();
    }
}
