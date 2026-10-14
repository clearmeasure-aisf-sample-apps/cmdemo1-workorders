using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchRedesignTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldShowEachWorkOrderOnce_WhenFilteredByCreator()
    {
        var creator = Faker<Employee>();
        creator.UserName = $"dup_{TestTag}";
        creator.AddRole(new Role("Pastor", true, false));
        creator.AddRole(new Role("Requester", true, false));
        var orders = Enumerable.Range(0, 3).Select(_ =>
        {
            var order = Faker<WorkOrder>();
            order.Creator = creator;
            order.Assignee = null;
            order.Title = $"[{TestTag}] duplicate check";
            return order;
        }).ToArray();

        await using (var context = TestHost.NewDbContext())
        {
            context.Add(creator);
            context.AddRange(orders);
            await context.SaveChangesAsync();
        }

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await Expect(creatorSelect.Locator($"option[value='{creator.UserName}']")).ToHaveCountAsync(1);
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        foreach (var order in orders)
        {
            await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number))
                .ToHaveCountAsync(1);
        }

        await Expect(Page.Locator(".grid-data tbody tr")).ToHaveCountAsync(orders.Length);
    }
    [Test, Retry(2)]
    public async Task ShouldKeepFiltersChipsAndRows_WhenPageReloaded()
    {
        var creator = await SeedCreatorAsync("reload");
        var overdueHigh = await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2099, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await SelectFilterAsync(WorkOrderSearch.Elements.PrioritySelect, nameof(WorkOrderPriority.High));
        await Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}").CheckAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Rows()).ToHaveCountAsync(1);

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Page.Url.ShouldContain($"Creator={creator.UserName}");
        Page.Url.ShouldContain("Priority=High");
        Page.Url.ShouldContain("OverdueOnly=true");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToHaveValueAsync(creator.UserName);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync(nameof(WorkOrderPriority.High));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}")).ToBeCheckedAsync();
        await Expect(Chip("Creator")).ToContainTextAsync(creator.GetFullName());
        await Expect(Chip("Priority")).ToContainTextAsync("Priority: High");
        await Expect(Rows()).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + overdueHigh.Number)).ToHaveCountAsync(1);
    }

    [Test, Retry(2)]
    public async Task ShouldFilterTable_WhenSummaryCardClicked_AndShowMatchingCount()
    {
        var creator = await SeedCreatorAsync("cards");
        await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Assigned, null, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, null, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.Complete, null, CurrentUser);

        await OpenSearchAsync();
        var card = Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardAssignedToMe));
        await Expect(card.Locator(".summary-card-value")).ToHaveTextAsync("2");

        await card.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Page.Url.ShouldContain("AssignedToMe=true");
        Page.Url.ShouldContain("OpenOnly=true");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}")).ToBeCheckedAsync();
        await Expect(Chip("OpenOnly")).ToHaveTextAsync("Not closed");
        await Expect(Rows()).ToHaveCountAsync(2);
    }

    [Test, Retry(2)]
    public async Task ShouldRemoveFilter_WhenChipClicked()
    {
        var creator = await SeedCreatorAsync("chips");
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.Draft, null);
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await SelectFilterAsync(WorkOrderSearch.Elements.PrioritySelect, nameof(WorkOrderPriority.High));
        await Expect(Rows()).ToHaveCountAsync(1);

        await Chip("Priority").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Chip("Priority")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync("");
        await Expect(Rows()).ToHaveCountAsync(2);
        Page.Url.ShouldNotContain("Priority=");
    }

    [Test, Retry(2)]
    public async Task ShouldNavigateToSearchWithQ_FromGlobalSearch()
    {
        var creator = await SeedCreatorAsync("global");
        var order = await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Draft, null);

        var search = Page.GetByTestId(nameof(GlobalSearchBox.Elements.GlobalSearchInput));
        await search.FillAsync(order.Number!);
        await search.PressAsync("Enter");
        await Page.WaitForURLAsync($"**/workorder/search?Q={order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Chip("Q")).ToHaveTextAsync($"Search: {order.Number}");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToHaveCountAsync(1);
    }

    private ILocator Rows() => Page.Locator(".grid-data tbody tr");

    private ILocator Chip(string key) => Page.GetByTestId($"{WorkOrderSearch.Elements.ActiveFilterChip}{key}");

    private async Task OpenSearchAsync()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
    }

    private async Task SelectFilterAsync(WorkOrderSearch.Elements select, string value)
    {
        var locator = Page.Locator($"#{select}");
        await Expect(locator.Locator($"option[value='{value}']")).ToHaveCountAsync(1);
        await locator.SelectOptionAsync(value);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

    private async Task<WorkOrder> SeedOrderAsync(Employee creator, WorkOrderPriority priority, WorkOrderStatus status,
        DateOnly? dueDate, Employee? assignee = null)
    {
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Priority = priority;
        order.Status = status;
        order.DueDate = dueDate;
        order.CompletedDate = null;
        order.Title = $"[{TestTag}] {priority} {status.FriendlyName}";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        if (assignee != null && assignee != creator)
        {
            context.Attach(assignee);
        }

        context.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}
