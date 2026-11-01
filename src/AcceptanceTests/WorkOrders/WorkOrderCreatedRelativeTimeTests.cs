using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCreatedRelativeTimeTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowRelativeCreatedTime_WithFullDateTooltip()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);

        var created = Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate));
        var relative = Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedRelativeTime));
        await Expect(created).ToBeVisibleAsync();
        await Expect(relative).ToBeVisibleAsync();

        var createdText = (await created.InnerTextAsync()).Trim();
        await Expect(relative).ToHaveAttributeAsync("title", createdText);
        createdText.ShouldNotContain("ago");

        var relativeText = (await relative.InnerTextAsync()).Trim();
        (relativeText is "just now" or "1 minute ago").ShouldBeTrue();
    }
}
