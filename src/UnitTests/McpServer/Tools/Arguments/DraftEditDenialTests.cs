using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class DraftEditDenialTests
{
    [Test]
    public void ShouldAllowTheEdit_WhenTheCreatorEditsTheirDraft()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);

        var denial = DraftEditDenial.ForSave(draft, creator);

        denial.ShouldBeNull();
    }

    [Test]
    public void ShouldNameTheCreator_WhenSomeoneElseUpdatesTheDraft()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        var denial = DraftEditDenial.ForUpdate(draft, WorkOrderToolMother.OtherEmployee());

        denial.ShouldBe("Only the creator 'jsmith' can update draft work order 'WO-1'.");
    }

    [Test]
    public void ShouldOmitTheCreatorName_WhenTheDraftHasNoCreator()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        draft.Creator = null;

        var denial = DraftEditDenial.UpdateNotAllowed(draft);

        denial.ShouldBe("Only the creator can update draft work order 'WO-1'.");
    }

    [Test]
    public void ShouldExplainTheRequiredStatus_WhenTheWorkOrderIsNoLongerADraft()
    {
        var creator = WorkOrderToolMother.Creator();
        var assigned = WorkOrderToolMother.Assigned(creator);

        var saveDenial = DraftEditDenial.ForSave(assigned, creator);
        var updateDenial = DraftEditDenial.ForUpdate(assigned, creator);

        saveDenial.ShouldBe("Command 'Save' cannot be executed. Work order is in 'Assigned' status but the command requires 'Draft' status.");
        updateDenial.ShouldBe(saveDenial);
    }
}
