using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Buildings;

public class BuildingsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldNavigateToBuildingsPageFromSidebar()
    {
        await LoginAsCurrentUser();

        var buildingsLink = Page.GetByTestId(nameof(NavMenu.Elements.BuildingsAndRooms));
        await Expect(buildingsLink).ToBeVisibleAsync();
        await Expect(buildingsLink).ToContainTextAsync("Buildings & Rooms");

        await Click(nameof(NavMenu.Elements.BuildingsAndRooms));
        await Page.WaitForURLAsync("**/admin/buildings");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buildings & Rooms" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldRenderDarkModeWithoutBreakage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/buildings");

        await Page.EvaluateAsync("() => { document.documentElement.setAttribute('data-theme', 'dark'); }");
        await Task.Delay(500);

        var cards = Page.Locator(".building-card");
        var cardCount = await cards.CountAsync();
        if (cardCount > 0)
        {
            await Expect(cards.First).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task ShouldSeedBuildingAndFollowCountLinks()
    {
        await LoginAsCurrentUser();

        // Create a unique building name so parallel tests never share it
        var buildingName = $"TestBuilding{TestData.Tag()}";
        var room1 = "TestRoom1";
        var room2 = "TestRoom2";

        // Create test work orders
        var wo1 = await CreateAndSaveNewWorkOrder();
        wo1.Building = buildingName;
        wo1.RoomNumber = room1;
        wo1.ChangeStatus(WorkOrderStatus.Draft);

        var wo2 = await CreateAndSaveNewWorkOrder();
        wo2.Building = buildingName;
        wo2.RoomNumber = room1;
        wo2.ChangeStatus(WorkOrderStatus.Assigned);

        var wo3 = await CreateAndSaveNewWorkOrder();
        wo3.Building = buildingName;
        wo3.RoomNumber = room2;
        wo3.ChangeStatus(WorkOrderStatus.InProgress);

        // Update the work orders in the database
        await using (var ctxWo1 = TestHost.NewDbContext())
        {
            ctxWo1.Update(wo1);
            await ctxWo1.SaveChangesAsync();
        }
        await using (var ctxWo2 = TestHost.NewDbContext())
        {
            ctxWo2.Update(wo2);
            await ctxWo2.SaveChangesAsync();
        }
        await using (var ctxWo3 = TestHost.NewDbContext())
        {
            ctxWo3.Update(wo3);
            await ctxWo3.SaveChangesAsync();
        }

        // Navigate to Buildings page
        await Page.GotoAsync("/admin/buildings");

        // Find the building card for our test building
        var buildingCard = Page.GetByTestId($"building-card-{buildingName}");
        await Expect(buildingCard).ToBeVisibleAsync();

        // Find the room items within the building card
        var room1Item = Page.GetByTestId($"room-item-{buildingName}-{room1}");
        await Expect(room1Item).ToBeVisibleAsync();

        // Find and click the "Open" count link for room 1 (should have 2 open: wo1 Draft + wo2 Assigned)
        var openLink = Page.GetByTestId($"room-open-link-{buildingName}-{room1}");
        var openCountText = await openLink.Locator(".count-value").InnerTextAsync();
        var expectedOpenCount = int.Parse(openCountText);

        // Click the open link
        await openLink.ClickAsync();

        // Verify we're on the search page with the correct filters
        await Page.WaitForURLAsync(url =>
            url.ToString().Contains("/workorder/search") &&
            url.ToString().Contains($"Building={Uri.EscapeDataString(buildingName)}") &&
            url.ToString().Contains($"Room={Uri.EscapeDataString(room1)}") &&
            url.ToString().Contains("OpenOnly=true")
        );

        // Count the results
        var searchResultRows = Page.Locator(".grid-data tbody tr");
        await Expect(searchResultRows.First).ToBeVisibleAsync();
        var actualOpenCount = await searchResultRows.CountAsync();

        // Counts should match
        actualOpenCount.ShouldBe(expectedOpenCount);
        actualOpenCount.ShouldBe(2); // wo1 and wo2 are open
    }

    [Test]
    public async Task ShouldSeedBuildingAndFollowOverdueCountLinks()
    {
        await LoginAsCurrentUser();

        // Create a unique building name
        var buildingName = $"TestBuilding{TestData.Tag()}";
        var room = "TestRoom";

        // Create test work orders
        var wo1 = await CreateAndSaveNewWorkOrder();
        wo1.Building = buildingName;
        wo1.RoomNumber = room;
        // Overdue at the church (Chicago) at any UTC time of day
        wo1.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        wo1.ChangeStatus(WorkOrderStatus.Assigned);

        var wo2 = await CreateAndSaveNewWorkOrder();
        wo2.Building = buildingName;
        wo2.RoomNumber = room;
        wo2.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)); // Not overdue
        wo2.ChangeStatus(WorkOrderStatus.Assigned);

        // Update in database
        await using (var ctxWo1 = TestHost.NewDbContext())
        {
            ctxWo1.Update(wo1);
            await ctxWo1.SaveChangesAsync();
        }
        await using (var ctxWo2 = TestHost.NewDbContext())
        {
            ctxWo2.Update(wo2);
            await ctxWo2.SaveChangesAsync();
        }

        // Navigate to Buildings page
        await Page.GotoAsync("/admin/buildings");

        // Find the overdue count link
        var overdueLink = Page.GetByTestId($"room-overdue-link-{buildingName}-{room}");
        await Expect(overdueLink).ToBeVisibleAsync();

        var overdueCountText = await overdueLink.Locator(".count-value").InnerTextAsync();
        var expectedOverdueCount = int.Parse(overdueCountText);

        // Click the overdue link
        await overdueLink.ClickAsync();

        // Verify we're on search page with overdue filter
        await Page.WaitForURLAsync(url =>
            url.ToString().Contains("/workorder/search") &&
            url.ToString().Contains("OverdueOnly=true")
        );

        // Count results
        var searchResultRows = Page.Locator(".grid-data tbody tr");
        if (expectedOverdueCount > 0)
        {
            await Expect(searchResultRows.First).ToBeVisibleAsync();
        }
        var actualOverdueCount = await searchResultRows.CountAsync();

        // Should match
        actualOverdueCount.ShouldBe(expectedOverdueCount);
        actualOverdueCount.ShouldBeGreaterThanOrEqualTo(1); // wo1 is overdue
    }

    [Test]
    public async Task ShouldSeedBuildingAndFollowCompletedCountLinks()
    {
        await LoginAsCurrentUser();

        // Create a unique building name
        var buildingName = $"TestBuilding{TestData.Tag()}";
        var room = "TestRoom";

        // Create test work orders
        var wo1 = await CreateAndSaveNewWorkOrder();
        wo1.Building = buildingName;
        wo1.RoomNumber = room;
        wo1.ChangeStatus(WorkOrderStatus.Complete);
        wo1.CompletedDate = DateTime.UtcNow.AddDays(-5); // Within last 30 days

        var wo2 = await CreateAndSaveNewWorkOrder();
        wo2.Building = buildingName;
        wo2.RoomNumber = room;
        wo2.ChangeStatus(WorkOrderStatus.Draft); // Not completed

        // Update in database
        await using (var ctxWo1 = TestHost.NewDbContext())
        {
            ctxWo1.Update(wo1);
            await ctxWo1.SaveChangesAsync();
        }
        await using (var ctxWo2 = TestHost.NewDbContext())
        {
            ctxWo2.Update(wo2);
            await ctxWo2.SaveChangesAsync();
        }

        // Navigate to Buildings page
        await Page.GotoAsync("/admin/buildings");

        // Find the completed count link
        var completedLink = Page.GetByTestId($"room-completed-link-{buildingName}-{room}");
        await Expect(completedLink).ToBeVisibleAsync();

        var completedCountText = await completedLink.Locator(".count-value").InnerTextAsync();
        var expectedCompletedCount = int.Parse(completedCountText);

        // Click the completed link
        await completedLink.ClickAsync();

        // Verify we're on search page with completed filters
        await Page.WaitForURLAsync(url =>
            url.ToString().Contains("/workorder/search") &&
            url.ToString().Contains("Status=Complete") &&
            url.ToString().Contains("CompletedWithinDays=30")
        );

        // Count results
        var searchResultRows = Page.Locator(".grid-data tbody tr");
        if (expectedCompletedCount > 0)
        {
            await Expect(searchResultRows.First).ToBeVisibleAsync();
        }
        var actualCompletedCount = await searchResultRows.CountAsync();

        // Should match
        actualCompletedCount.ShouldBe(expectedCompletedCount);
        actualCompletedCount.ShouldBeGreaterThanOrEqualTo(1); // wo1 is completed
    }
}
