using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchSavedViewsTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldApplySameFilters_WhenSavedViewChosenAfterReload()
    {
        var creator = await SeedCreatorAsync("saved");
        var overdueHigh = await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2099, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await SelectFilterAsync(WorkOrderSearch.Elements.PrioritySelect, nameof(WorkOrderPriority.High));
        await Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}").CheckAsync();
        await Expect(Rows()).ToHaveCountAsync(1);

        var viewName = $"View {TestTag}";
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.SavedViewsButton)).ClickAsync();
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.SaveCurrentViewButton)).ClickAsync();
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.SavedViewNameInput)).FillAsync(viewName);
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.SavedViewSaveButton)).ClickAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.SavedViewNameInput))).ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await Page.Locator($"#{WorkOrderSearch.Elements.ClearFiltersButton}").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("^(?!.*Priority=)"));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync("");

        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.SavedViewsButton)).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = viewName, Exact = true }).ClickAsync();
        await Page.WaitForURLAsync(new Regex("Priority=High"));

        Page.Url.ShouldContain($"Creator={creator.UserName}");
        Page.Url.ShouldContain("Priority=High");
        Page.Url.ShouldContain("OverdueOnly=true");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToHaveValueAsync(creator.UserName);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync(nameof(WorkOrderPriority.High));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}")).ToBeCheckedAsync();
        await Expect(Rows()).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + overdueHigh.Number)).ToHaveCountAsync(1);
    }

    private ILocator Rows() => Page.Locator(".grid-data tbody tr");

    private async Task OpenSearchAsync()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
    }

    private async Task SelectFilterAsync(WorkOrderSearch.Elements select, string value)
    {
        var locator = Page.Locator($"#{select}");
        await Expect(locator.Locator($"option[value='{value}']")).ToHaveCountAsync(1);
        await locator.SelectOptionAsync(value);
        var searchTable = Page.Locator(".grid-data tbody");
        await searchTable.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    }

    private async Task<Employee> SeedCreatorAsync(string prefix)
    {
        var creator = Faker<Employee>();
        creator.UserName = $"{prefix}_{TestTag}";
        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        await context.SaveChangesAsync();
        return creator;
    }

    private async Task<WorkOrder> SeedOrderAsync(Employee creator, WorkOrderPriority priority, WorkOrderStatus status, DateOnly? dueDate)
    {
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = null;
        order.Priority = priority;
        order.Status = status;
        order.DueDate = dueDate;
        order.CompletedDate = null;
        order.Title = $"[{TestTag}] {priority} {status.FriendlyName}";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        context.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}
