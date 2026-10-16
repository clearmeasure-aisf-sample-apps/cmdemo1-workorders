using ClearMeasure.Bootcamp.Core.Services.Impl;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

public class CommandButtonStyleHelperTests
{
    private static readonly string[] AllValidCommandNames = ["Save", "Assign", "Begin", "Complete", "Cancel", "Reassign"];

    [Test]
    public void GetButtonStyle_SaveCommand_ReturnsBtnOutlineSecondary()
    {
        var style = CommandButtonStyleHelper.GetButtonStyle("Save", AllValidCommandNames);

        style.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_CancelCommand_ReturnsBtnOutlineDanger()
    {
        var style = CommandButtonStyleHelper.GetButtonStyle("Cancel", AllValidCommandNames);

        style.ShouldBe("btn-outline-danger");
    }

    [Test]
    public void GetButtonStyle_ReassignCommand_ReturnsBtnOutlineSecondary()
    {
        var style = CommandButtonStyleHelper.GetButtonStyle("Reassign", AllValidCommandNames);

        style.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_AssignCommandWhenValid_ReturnsBtnPrimary()
    {
        string[] validCommands = ["Save", "Assign", "Cancel"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Assign", validCommands);

        style.ShouldBe("btn-primary");
    }

    [Test]
    public void GetButtonStyle_AssignCommandWhenNotValid_ReturnsBtnOutlineSecondary()
    {
        string[] validCommands = ["Save", "Begin"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Assign", validCommands);

        style.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_BeginCommandWhenValid_ReturnsBtnPrimary()
    {
        string[] validCommands = ["Save", "Begin", "Reassign", "Cancel"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Begin", validCommands);

        style.ShouldBe("btn-primary");
    }

    [Test]
    public void GetButtonStyle_BeginCommandWhenNotValid_ReturnsBtnOutlineSecondary()
    {
        string[] validCommands = ["Save", "Assign", "Cancel"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Begin", validCommands);

        style.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_CompleteCommandWhenValid_ReturnsBtnPrimary()
    {
        string[] validCommands = ["Save", "Complete", "Reassign", "Cancel"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Complete", validCommands);

        style.ShouldBe("btn-primary");
    }

    [Test]
    public void GetButtonStyle_CompleteCommandWhenNotValid_ReturnsBtnOutlineSecondary()
    {
        string[] validCommands = ["Save", "Begin", "Cancel"];

        var style = CommandButtonStyleHelper.GetButtonStyle("Complete", validCommands);

        style.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_DraftState_AssignIsPrimary()
    {
        string[] draftStateCommands = ["Save", "Assign", "Cancel"];

        var saveStyle = CommandButtonStyleHelper.GetButtonStyle("Save", draftStateCommands);
        var assignStyle = CommandButtonStyleHelper.GetButtonStyle("Assign", draftStateCommands);
        var cancelStyle = CommandButtonStyleHelper.GetButtonStyle("Cancel", draftStateCommands);

        saveStyle.ShouldBe("btn-outline-secondary");
        assignStyle.ShouldBe("btn-primary");
        cancelStyle.ShouldBe("btn-outline-danger");
    }

    [Test]
    public void GetButtonStyle_AssignedState_BeginIsPrimary()
    {
        string[] assignedStateCommands = ["Save", "Begin", "Reassign", "Cancel"];

        var saveStyle = CommandButtonStyleHelper.GetButtonStyle("Save", assignedStateCommands);
        var beginStyle = CommandButtonStyleHelper.GetButtonStyle("Begin", assignedStateCommands);
        var reassignStyle = CommandButtonStyleHelper.GetButtonStyle("Reassign", assignedStateCommands);
        var cancelStyle = CommandButtonStyleHelper.GetButtonStyle("Cancel", assignedStateCommands);

        saveStyle.ShouldBe("btn-outline-secondary");
        beginStyle.ShouldBe("btn-primary");
        reassignStyle.ShouldBe("btn-outline-secondary");
        cancelStyle.ShouldBe("btn-outline-danger");
    }

    [Test]
    public void GetButtonStyle_InProgressState_CompleteIsPrimary()
    {
        string[] inProgressStateCommands = ["Save", "Complete", "Reassign"];

        var saveStyle = CommandButtonStyleHelper.GetButtonStyle("Save", inProgressStateCommands);
        var completeStyle = CommandButtonStyleHelper.GetButtonStyle("Complete", inProgressStateCommands);
        var reassignStyle = CommandButtonStyleHelper.GetButtonStyle("Reassign", inProgressStateCommands);

        saveStyle.ShouldBe("btn-outline-secondary");
        completeStyle.ShouldBe("btn-primary");
        reassignStyle.ShouldBe("btn-outline-secondary");
    }

    [Test]
    public void GetButtonStyle_OnlyOnePrimaryButtonPerState()
    {
        string[][] states =
        [
            ["Save", "Assign", "Cancel"],
            ["Save", "Begin", "Reassign", "Cancel"],
            ["Save", "Complete", "Reassign"]
        ];

        foreach (var stateCommands in states)
        {
            var primaryCount = stateCommands.Count(cmd =>
                CommandButtonStyleHelper.GetButtonStyle(cmd, stateCommands) == "btn-primary");

            primaryCount.ShouldBeLessThanOrEqualTo(1, $"State {string.Join(", ", stateCommands)} should have at most one primary button");
        }
    }
}
