using System.Text.Json;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.McpServer;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

/// <summary>
/// #9118 UX contract — Lovejoy Save then Assign stays Assigned after reload / Willie login /
/// get-work-order (Assign Update already persisted ASD; Cancel must not leave a Willie+CNL shape).
/// </summary>
[NonParallelizable, Category("SerialLane")]
public class WorkOrderSaveThenAssignPersistenceTests : WorkOrderAcceptanceTestBase
{
    private static McpTestHelper? _helper;
    private McpTestHelper Helper =>
        _helper ?? throw new InvalidOperationException("MCP helper is not initialized.");

    [OneTimeSetUp]
    public async Task McpSetUp()
    {
        _helper = new McpTestHelper(TestHost.GetRequiredService<ChatClientFactory>());
        await _helper.ConnectAsync();
    }

    [OneTimeTearDown]
    public async Task McpTearDown()
    {
        if (_helper != null)
        {
            await _helper.DisposeAsync();
        }
    }

    [SetUp]
    public void EnsureMcpAvailable()
    {
        if (!Helper.Connected)
        {
            Assert.Inconclusive("MCP server is not available");
        }
    }

    [Test, Retry(2)]
    public async Task ShouldKeepAssignedAfterSaveThenAssign_Reload_WillieAssignedToMe_AndGetWorkOrder()
    {
        var token = TestTag[..4];
        var title = $"mow front grass {token}";
        const string description = "edge the walk";
        const string instructions = "do a good job";
        const string room = "front lawn";

        var inProgressNumber = await SeedNearbyInProgressAsync();

        await LoginAsTlovejoyAsync();

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var workOrderNumber = (await woNumberLocator.InnerTextAsync()).Trim();
        workOrderNumber.Length.ShouldBeLessThanOrEqualTo(7);

        await Select(nameof(WorkOrderManage.Elements.Assignee), "gwillie");
        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), room);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await Page.GotoAsync($"/workorder/manage/{workOrderNumber}?mode=Edit");
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(woNumberLocator).ToHaveTextAsync(workOrderNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await Page.GotoAsync($"/workorder/manage/{workOrderNumber}?mode=Edit");
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AssignedDate)))
            .Not.ToBeEmptyAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .Not.ToHaveTextAsync(WorkOrderStatus.Cancelled.FriendlyName);

        await Page.ReloadAsync();
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AssignedDate)))
            .Not.ToBeEmptyAsync();

        await LogoutViaUserMenu();
        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToBeVisibleAsync();

        await LoginAsGwillieAsync();

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        var assignedLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Expect(assignedLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        var assignedRow = Page.Locator("tr", new PageLocatorOptions { Has = assignedLink });
        await Expect(assignedRow.Locator(".status-badge"))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        await Expect(assignedRow.Locator(".status-badge"))
            .Not.ToHaveTextAsync(WorkOrderStatus.Cancelled.FriendlyName);

        await assignedLink.ClickAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(title);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToHaveValueAsync(description);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(instructions);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber))).ToHaveValueAsync(room);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee))).ToHaveValueAsync("gwillie");

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}")
            .SelectOptionAsync(WorkOrderStatus.Assigned.Key);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        await Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}")
            .SelectOptionAsync(WorkOrderStatus.Cancelled.Key);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber))
            .ToHaveCountAsync(0);

        var getResult = await Helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?> { ["workOrderNumber"] = workOrderNumber });
        using var doc = JsonDocument.Parse(getResult);
        var detail = doc.RootElement;
        detail.GetProperty("Status").GetString().ShouldBe("Assigned");
        detail.GetProperty("AssignedDate").ValueKind.ShouldNotBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").GetString().ShouldBe("gwillie");

        var frozen = await Bus.Send(new WorkOrderByNumberQuery(inProgressNumber));
        frozen.ShouldNotBeNull();
        frozen.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    private async Task<string> SeedNearbyInProgressAsync()
    {
        var number = $"IP{TestTag[..4]}";
        var created = await Mother.WorkOrderAsync(
            "tlovejoy",
            WorkOrderStatus.InProgress,
            new WorkOrderSeedFields
            {
                AssigneeUserName = "gwillie",
                Title = $"[{TestTag}] nearby in progress",
                Description = "freeze check",
                RoomNumber = "yard",
                Instructions = "leave alone",
                Number = number
            });
        return created.Number;
    }

    private async Task LoginAsTlovejoyAsync()
    {
        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });
        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");
    }

    private async Task LoginAsGwillieAsync()
    {
        await Page.GotoAsync("/login");
        await Expect(Page.GetByTestId(nameof(Login.Elements.User))).ToBeVisibleAsync();
        await Select(nameof(Login.Elements.User), "gwillie");
        await Click(nameof(Login.Elements.LoginButton));
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome gwillie!");
    }
}
