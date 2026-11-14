using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>Notifications panel in header: badge count, seeded work orders, Escape to close with focus return.</summary>
[TestFixture]
public class NotificationsPanelTests : AppAcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldShowBadgeCount_MatchingOverdueAndDueTodayWorkOrders()
    {
        await SeedBadgeCoverageWorkOrdersAsync();
        await LogoutViaUserMenu();
        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToBeVisibleAsync();
        await LoginAsCurrentUser();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        var badge = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsBadge));
        await Expect(badge).ToHaveTextAsync("3", new() { Timeout = 30_000 });
        (await bellButton.GetAttributeAsync("aria-disabled")).ShouldBeNull();
    }

    private async Task SeedBadgeCoverageWorkOrdersAsync()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var otherUser = await SeedEmployeeAsync();
        await SeedAssignedAsync("Overdue Work Order 1", WorkOrderStatus.Assigned, today.AddDays(-2));
        await SeedAssignedAsync("Overdue Work Order 2", WorkOrderStatus.InProgress, today.AddDays(-1));
        await SeedAssignedAsync("Due Today Work Order", WorkOrderStatus.Draft, today);
        await SeedAssignedAsync("Due Tomorrow Work Order", WorkOrderStatus.Assigned, today.AddDays(1));
        await SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Completed Overdue (should not count)",
            AssigneeUserName = CurrentUser.UserName,
            DueDate = today.AddDays(-1),
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        });
        await SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Assigned, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Overdue Assigned to Other (should not count)",
            AssigneeUserName = otherUser.UserName,
            DueDate = today.AddDays(-1)
        });
    }

    private Task SeedAssignedAsync(string title, WorkOrderStatus status, DateOnly dueDate) =>
        SeedWorkOrderAsync(CurrentUser, status, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] {title}",
            AssigneeUserName = CurrentUser.UserName,
            DueDate = dueDate
        });

    [Test, Retry(2)]
    public async Task ShouldOpenPanelAndShowThreeGroups()
    {
        await Page.ReloadAsync();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        await bellButton.ClickAsync();

        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Overdue & Due Today")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Newly Assigned (Last 7 Days)")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Status Changed (Last 7 Days)")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldExposeRegionAndAccessibleName_WhenPanelOpens()
    {
        await Page.ReloadAsync();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        await bellButton.ClickAsync();

        var panel = Page.Locator(".notifications-panel");
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToHaveAttributeAsync("role", "region");
        await Expect(panel).ToHaveAttributeAsync("aria-label", "Notifications");
    }

    [Test, Retry(2)]
    public async Task ShouldShowEmptyMessages_WhenUserHasNoNotifications()
    {
        await Page.ReloadAsync();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        await bellButton.ClickAsync();

        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No overdue or due-today work orders")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No newly assigned work orders")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No work orders with status changes")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClosePanel_OnEscapeKey_AndReturnFocusToBell()
    {
        await Page.ReloadAsync();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));

        await bellButton.ClickAsync();
        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();

        await Page.WaitForFunctionAsync("() => !!window.notificationsPanelKeydownHandler");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator(".notifications-panel")).ToBeHiddenAsync();
        await Expect(bellButton).ToBeFocusedAsync();
    }
}
