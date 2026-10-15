using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleCharCountTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task TitleCharCount_InitialCaption_ShowsFullLimit()
    {
        await OpenNewWorkOrderFormAsync();

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCount));
        await Expect(caption).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
        await Expect(caption).ToHaveTextAsync("300 characters remaining");
    }

    [Test, Retry(2)]
    public async Task TitleCharCount_UpdatesCaption_AsUserTypes()
    {
        await OpenNewWorkOrderFormAsync();

        var titleField = await EditableTitleFieldAsync();
        await titleField.FillAsync("0123456789");
        await titleField.BlurAsync();

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCount));
        await Expect(caption).ToHaveTextAsync("290 characters remaining");
        await Expect(caption).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));
    }

    [Test, Retry(2)]
    public async Task TitleCharCount_ShowsWarning_WhenLimitReached()
    {
        await OpenNewWorkOrderFormAsync();

        var titleField = await EditableTitleFieldAsync();
        await titleField.FillAsync(new string('A', WorkOrder.TitleMaxLength));
        await titleField.BlurAsync();

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.TitleCharCount));
        await Expect(caption).ToHaveTextAsync("0 characters remaining");
        await Expect(caption).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));

        var valueLength = await titleField.EvaluateAsync<int>("el => el.value.length");
        valueLength.ShouldBe(WorkOrder.TitleMaxLength);
    }

    [Test, Retry(2)]
    public async Task TitleInput_RejectsInputBeyondMaxLength()
    {
        await OpenNewWorkOrderFormAsync();

        var titleField = await EditableTitleFieldAsync();
        await titleField.FillAsync(new string('A', WorkOrder.TitleMaxLength));
        await titleField.PressSequentiallyAsync("B");
        await titleField.BlurAsync();

        var valueLength = await titleField.EvaluateAsync<int>("el => el.value.length");
        valueLength.ShouldBe(WorkOrder.TitleMaxLength);
    }

    private async Task OpenNewWorkOrderFormAsync()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();
    }

    private async Task<ILocator> EditableTitleFieldAsync()
    {
        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        return titleField;
    }
}
