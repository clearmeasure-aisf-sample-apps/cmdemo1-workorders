using ClearMeasure.Bootcamp.UI.Shared;
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
        await Page.Locator($"#{WorkOrderSearch.Elements.SearchButton}").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        foreach (var order in orders)
        {
            await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number))
                .ToHaveCountAsync(1);
        }

        await Expect(Page.Locator(".grid-data tbody tr")).ToHaveCountAsync(orders.Length);
    }
}
