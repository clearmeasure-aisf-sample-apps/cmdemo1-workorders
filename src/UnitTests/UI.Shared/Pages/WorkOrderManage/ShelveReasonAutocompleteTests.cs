using Bunit;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class ShelveReasonAutocompleteTests
{
    [Test]
    public async Task ShelveReason_InputText_HasAutocompleteOff()
    {
        var user = WorkOrderManageSteps.Fulfiller();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .Editing(WorkOrderManageSteps.InProgressOwnedBy(user))
            .WhenBusReceives<IStateCommand>();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var shelveReason = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ShelveReason));

        shelveReason.GetAttribute("autocomplete").ShouldBe("off");
    }
}
