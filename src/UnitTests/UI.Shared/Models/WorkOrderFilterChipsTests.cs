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
            Priority = WorkOrderPriority.High, Building = "Kitchen", OpenOnly = true, CompletedWithinDays = 30, Q = "organ"
        };

        var chips = WorkOrderFilterChips.Build(state, Name);

        chips.Select(c => c.Label).ShouldBe([
            "Creator: Timothy Lovejoy Jr", "Assignee: gwillie", "Status: In Progress", "Priority: High",
            "Building: Kitchen", "Not closed", "Completed in last 30 days", "Search: organ"
        ]);
        chips.Select(c => c.Key).Distinct().Count().ShouldBe(8);
    }

    [Test]
    public void Build_NoChipsForSwitchesOrAssignedToMeAssignee()
    {
        var state = new WorkOrderSearchUrlState { AssignedToMe = true, Assignee = "jpalermo", OverdueOnly = true };

        WorkOrderFilterChips.Build(state, Name).ShouldBeEmpty();
    }

    [TestCase(FilterChipKey.Creator)]
    [TestCase(FilterChipKey.Assignee)]
    [TestCase(FilterChipKey.Status)]
    [TestCase(FilterChipKey.Priority)]
    [TestCase(FilterChipKey.Building)]
    [TestCase(FilterChipKey.OpenOnly)]
    [TestCase(FilterChipKey.CompletedWithinDays)]
    [TestCase(FilterChipKey.Q)]
    public void Remove_ClearsOnlyThatFilter_AndResetsPage(FilterChipKey key)
    {
        var state = new WorkOrderSearchUrlState
        {
            Creator = "tlovejoy", Assignee = "gwillie", Status = WorkOrderStatus.Draft.Key, Priority = WorkOrderPriority.Low,
            Building = "Kitchen", OpenOnly = true, CompletedWithinDays = 30, Q = "organ", Page = 4
        };

        var after = WorkOrderFilterChips.Remove(state, key);

        WorkOrderFilterChips.Build(after, Name).Select(c => c.Key).ShouldNotContain(key);
        WorkOrderFilterChips.Build(after, Name).Count.ShouldBe(7);
        after.Page.ShouldBe(1);
    }
}
