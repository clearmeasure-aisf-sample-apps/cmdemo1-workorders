using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderFilterChipsTests
{
    private static string Name(string userName) => userName == "tlovejoy" ? "Timothy Lovejoy Jr" : userName;

    [Test]
    public void Build_OneChipPerFilter_WithLabels()
    {
        var state = new WorkOrderSearchUrlState
        {
            Creator = "tlovejoy", Assignee = "gwillie", Status = WorkOrderStatus.InProgress.Key,
            Priority = WorkOrderPriority.High, Building = "Kitchen", Room = "101", OpenOnly = true, CompletedWithinDays = 30, Q = "organ"
        };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.Select(c => c.Label).ShouldBe([
            "Creator: Timothy Lovejoy Jr", "Assignee: gwillie", "Status: In Progress", "Priority: High",
            "Building: Kitchen", "Room: 101", "Not closed", "Completed in last 30 days", "Search: organ"
        ]);
        chips.Select(c => c.Key).Distinct().Count().ShouldBe(9);
    }

    [Test]
    public void Build_NoChipsForSwitchesOrAssignedToMeAssignee()
    {
        var state = new WorkOrderSearchUrlState { AssignedToMe = true, Assignee = "jpalermo", OverdueOnly = true };

        WorkOrderFilterChips.Build(state, Name).ShouldBeEmpty();
    }

    [Test]
    public void Build_NoBuildingChip_WhenNoBuildingTextButNoBuildingFlagSet()
    {
        var state = new WorkOrderSearchUrlState { NoBuilding = true };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.ShouldHaveSingleItem();
        chips[0].Key.ShouldBe(FilterChipKey.NoBuilding);
        chips[0].Label.ShouldBe("Building: No building");
    }

    [Test]
    public void Build_NoNoBuildingChip_WhenBuildingTextSet()
    {
        var state = new WorkOrderSearchUrlState { Building = "Kitchen", NoBuilding = true };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.Select(c => c.Key).ShouldNotContain(FilterChipKey.NoBuilding);
        chips.ShouldHaveSingleItem();
        chips[0].Key.ShouldBe(FilterChipKey.Building);
    }

    [Test]
    public void Build_NoRoomChip_WhenNoRoomTextButNoRoomFlagSet()
    {
        var state = new WorkOrderSearchUrlState { NoRoom = true };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.ShouldHaveSingleItem();
        chips[0].Key.ShouldBe(FilterChipKey.NoRoom);
        chips[0].Label.ShouldBe("Room: No room");
    }

    [Test]
    public void Build_NoNoRoomChip_WhenRoomTextSet()
    {
        var state = new WorkOrderSearchUrlState { Room = "101", NoRoom = true };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.Select(c => c.Key).ShouldNotContain(FilterChipKey.NoRoom);
        chips.ShouldHaveSingleItem();
        chips[0].Key.ShouldBe(FilterChipKey.Room);
    }

    [TestCase(FilterChipKey.Creator)]
    [TestCase(FilterChipKey.Assignee)]
    [TestCase(FilterChipKey.Status)]
    [TestCase(FilterChipKey.Priority)]
    [TestCase(FilterChipKey.Building)]
    [TestCase(FilterChipKey.Room)]
    [TestCase(FilterChipKey.OpenOnly)]
    [TestCase(FilterChipKey.CompletedWithinDays)]
    [TestCase(FilterChipKey.Q)]
    public void Remove_ClearsOnlyThatFilter_AndResetsPage(FilterChipKey key)
    {
        var state = new WorkOrderSearchUrlState
        {
            Creator = "tlovejoy", Assignee = "gwillie", Status = WorkOrderStatus.Draft.Key, Priority = WorkOrderPriority.Low,
            Building = "Kitchen", Room = "101", OpenOnly = true, CompletedWithinDays = 30, Q = "organ", Page = 4
        };

        var after = WorkOrderFilterChips.Remove(state, key);

        WorkOrderFilterChips.Build(after, Name).Select(c => c.Key).ShouldNotContain(key);
        WorkOrderFilterChips.Build(after, Name).Count.ShouldBe(8);
        after.Page.ShouldBe(1);
    }

    [TestCase(FilterChipKey.NoBuilding)]
    [TestCase(FilterChipKey.NoRoom)]
    public void Remove_ClearsNoFilterWithoutTextValue_AndResetsPage(FilterChipKey key)
    {
        var state = key == FilterChipKey.NoBuilding
            ? new WorkOrderSearchUrlState { NoBuilding = true, Page = 4 }
            : new WorkOrderSearchUrlState { NoRoom = true, Page = 4 };

        var after = WorkOrderFilterChips.Remove(state, key);

        WorkOrderFilterChips.Build(after, Name).Select(c => c.Key).ShouldNotContain(key);
        after.Page.ShouldBe(1);
    }
}
