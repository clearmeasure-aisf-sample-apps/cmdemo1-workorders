using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Buildings;

/// <summary>
/// Buildings acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// </summary>
public abstract class BuildingsAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>Unique building label for this test (avoids peer locator collisions).</summary>
    protected string TaggedBuilding => $"Bldg{TestTag}";

    /// <summary>Opens the Buildings admin page.</summary>
    protected Task GotoBuildingsPageAsync() => Page.GotoAsync("/admin/buildings");

    /// <summary>Seeds a work order in <see cref="TaggedBuilding"/> with the given room and status.</summary>
    protected Task<WorkOrder> SeedBuildingOrderAsync(
        string room,
        WorkOrderStatus status,
        WorkOrderSeedFields? fields = null)
    {
        fields ??= new WorkOrderSeedFields();
        var needsAssignee = status == WorkOrderStatus.Assigned || status == WorkOrderStatus.InProgress;
        return SeedWorkOrderAsync(CurrentUser, status, new WorkOrderSeedFields
        {
            Title = fields.Title ?? $"[{TestTag}] {room} {status.FriendlyName}",
            Building = TaggedBuilding,
            RoomNumber = room,
            DueDate = fields.DueDate,
            CompletedDate = fields.CompletedDate,
            AssigneeUserName = fields.AssigneeUserName
                ?? (needsAssignee ? CurrentUser.UserName : null)
        });
    }

    /// <summary>Reads the numeric count on a buildings room link before navigation.</summary>
    protected async Task<int> ReadRoomCountAsync(string testId)
    {
        var link = Page.GetByTestId(testId);
        await Expect(link).ToBeVisibleAsync();
        return int.Parse(await link.Locator(".count-value").InnerTextAsync());
    }

    /// <summary>Clicks a room count link and waits for the matching search URL fragment.</summary>
    protected async Task FollowRoomCountLinkAsync(string testId, Func<string, bool> urlMatches)
    {
        await Page.GetByTestId(testId).ClickAsync();
        await Page.WaitForURLAsync(url => urlMatches(url.ToString()));
    }

    /// <summary>Asserts search result rows equal <paramref name="expected"/>.</summary>
    protected async Task AssertSearchRowCountAsync(int expected)
    {
        var rows = Page.Locator(".grid-data tbody tr");
        if (expected > 0)
        {
            await Expect(rows.First).ToBeVisibleAsync();
        }

        (await rows.CountAsync()).ShouldBe(expected);
    }
}
