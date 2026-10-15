using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>Notifications panel in header: badge count, seeded work orders, Escape to close with focus return.</summary>
[TestFixture]
public class NotificationsPanelTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldShowBadgeCount_MatchingOverdueAndDueTodayWorkOrders()
    {
        var today = ClearMeasure.Bootcamp.Core.Services.ChurchTimeZone.Today(TimeProvider.System);
        var yesterday = today.AddDays(-1);
        var dayBeforeYesterday = today.AddDays(-2);
        var tomorrow = today.AddDays(1);
        // WorkOrder.Number is limited to 7 characters: 1 letter + 5 random digits + 1 sequence digit
        var numberPrefix = $"N{Random.Shared.Next(0, 100000):D5}";
        var otherUser = new Employee($"other_user_{TestTag}", "Other", "User", $"other_{TestTag}@test.com");

        // Create work orders in various states assigned to CurrentUser
        var overdueAssigned = new WorkOrder
        {
            Number = $"{numberPrefix}1",
            Title = "Overdue Work Order 1",
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.Assigned,
            DueDate = dayBeforeYesterday,
            AssignedDate = DateTime.UtcNow.AddDays(-5)
        };

        var overdueInProgress = new WorkOrder
        {
            Number = $"{numberPrefix}2",
            Title = "Overdue Work Order 2",
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.InProgress,
            DueDate = yesterday,
            AssignedDate = DateTime.UtcNow.AddDays(-5)
        };

        var dueToday = new WorkOrder
        {
            Number = $"{numberPrefix}3",
            Title = "Due Today Work Order",
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.Draft,
            DueDate = today,
            AssignedDate = DateTime.UtcNow.AddDays(-3)
        };

        var dueTomorrow = new WorkOrder
        {
            Number = $"{numberPrefix}4",
            Title = "Due Tomorrow Work Order",
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.Assigned,
            DueDate = tomorrow,
            AssignedDate = DateTime.UtcNow.AddDays(-2)
        };

        var overdueCompleted = new WorkOrder
        {
            Number = $"{numberPrefix}5",
            Title = "Completed Overdue (should not count)",
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.Complete,
            DueDate = yesterday,
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        };

        var overdueOtherUser = new WorkOrder
        {
            Number = $"{numberPrefix}6",
            Title = "Overdue Assigned to Other (should not count)",
            Creator = CurrentUser,
            Assignee = otherUser,
            Status = WorkOrderStatus.Assigned,
            DueDate = yesterday,
            AssignedDate = DateTime.UtcNow.AddDays(-3)
        };

        // Seed work orders via database context (no special commands needed for acceptance test setup)
        var context = TestHost.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>();
        // CurrentUser already exists in the database; attach it so adding work orders does not re-insert it.
        context.Attach(CurrentUser);
        context.Add(otherUser);
        context.Add(overdueAssigned);
        context.Add(overdueInProgress);
        context.Add(dueToday);
        context.Add(dueTomorrow);
        context.Add(overdueCompleted);
        context.Add(overdueOtherUser);
        await context.SaveChangesAsync();

        // Sign out and back in through the normal login flow (no manual reload): the badge must
        // reflect the seeded work orders as soon as the user is signed in.
        await LogoutViaUserMenu();
        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToBeVisibleAsync();
        await LoginAsCurrentUser();

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        var badge = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsBadge));

        // Badge should show count of overdue + due-today items (3: overdue1, overdue2, today)
        // Not including: completed (3), assigned to other (1), due tomorrow (1)
        var expectedCount = 3;
        await Expect(badge).ToHaveTextAsync(expectedCount.ToString(), new() { Timeout = 30_000 });

        // Bell button should be enabled (not disabled)
        var ariaDisabled = await bellButton.GetAttributeAsync("aria-disabled");
        ariaDisabled.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldOpenPanelAndShowThreeGroups()
    {
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));
        await bellButton.ClickAsync();

        // Panel should be visible with three sections
        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Overdue & Due Today")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Newly Assigned (Last 7 Days)")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Status Changed (Last 7 Days)")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowEmptyMessages_WhenUserHasNoNotifications()
    {
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));

        // For a fresh user with no assigned work orders, badge should not be visible
        await bellButton.ClickAsync();

        // Panel should show empty messages
        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No overdue or due-today work orders")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No newly assigned work orders")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=No work orders with status changes")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldClosePanel_OnEscapeKey_AndReturnFocusToBell()
    {
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var bellButton = Page.GetByTestId(nameof(MainLayout.Elements.NotificationsButton));

        await bellButton.ClickAsync();
        await Expect(Page.Locator(".notifications-panel")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator(".notifications-panel")).ToBeHiddenAsync();
        await Expect(bellButton).ToBeFocusedAsync();
    }
}
