using ClearMeasure.Bootcamp.AcceptanceTests.Builders;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Buildings;

[TestFixture]
public class BuildingsCountLinkTests : BuildingsAcceptanceTestBase
{
    [Test]
    public async Task ShouldSeedBuildingAndFollowCountLinks()
    {
        const string room1 = "R1";
        const string room2 = "R2";
        await SeedBuildingOrderAsync(room1, WorkOrderStatus.Draft);
        await SeedBuildingOrderAsync(room1, WorkOrderStatus.Assigned);
        await SeedBuildingOrderAsync(room2, WorkOrderStatus.InProgress);

        await LoginAsCurrentUser();
        await GotoBuildingsPageAsync();

        await Expect(Page.GetByTestId($"building-card-{TaggedBuilding}")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId($"room-item-{TaggedBuilding}-{room1}")).ToBeVisibleAsync();

        var openTestId = $"room-open-link-{TaggedBuilding}-{room1}";
        var openCount = await ReadRoomCountAsync(openTestId);
        openCount.ShouldBe(2);
        await FollowRoomCountLinkAsync(
            openTestId,
            url => url.Contains("/workorder/search")
                   && url.Contains($"Building={Uri.EscapeDataString(TaggedBuilding)}")
                   && url.Contains($"Room={Uri.EscapeDataString(room1)}")
                   && url.Contains("OpenOnly=true"));
        await AssertSearchRowCountAsync(openCount);
    }

    [Test]
    public async Task ShouldSeedBuildingAndFollowOverdueCountLinks()
    {
        const string room = "R";
        await SeedBuildingOrderAsync(room, WorkOrderStatus.Assigned, new WorkOrderSeedFields
        {
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3))
        });
        await SeedBuildingOrderAsync(room, WorkOrderStatus.Assigned, new WorkOrderSeedFields
        {
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        });

        await LoginAsCurrentUser();
        await GotoBuildingsPageAsync();

        var overdueTestId = $"room-overdue-link-{TaggedBuilding}-{room}";
        var overdueCount = await ReadRoomCountAsync(overdueTestId);
        overdueCount.ShouldBe(1);
        await FollowRoomCountLinkAsync(
            overdueTestId,
            url => url.Contains("/workorder/search")
                   && url.Contains($"Building={Uri.EscapeDataString(TaggedBuilding)}")
                   && url.Contains("OverdueOnly=true"));
        await AssertSearchRowCountAsync(overdueCount);
    }

    [Test]
    public async Task ShouldSeedBuildingAndFollowCompletedCountLinks()
    {
        const string room = "R";
        await SeedBuildingOrderAsync(room, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            CompletedDate = DateTime.UtcNow.AddDays(-5)
        });
        await SeedBuildingOrderAsync(room, WorkOrderStatus.Draft);

        await LoginAsCurrentUser();
        await GotoBuildingsPageAsync();

        var completedTestId = $"room-completed-link-{TaggedBuilding}-{room}";
        var completedCount = await ReadRoomCountAsync(completedTestId);
        completedCount.ShouldBe(1);
        await FollowRoomCountLinkAsync(
            completedTestId,
            url => url.Contains("/workorder/search")
                   && url.Contains($"Building={Uri.EscapeDataString(TaggedBuilding)}")
                   && url.Contains("Status=Complete")
                   && url.Contains("CompletedWithinDays=30"));
        await AssertSearchRowCountAsync(completedCount);
    }
}
