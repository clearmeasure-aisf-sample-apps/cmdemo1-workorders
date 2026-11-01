using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderBoardTests
{
    private static WorkOrderSearchResultRow Row(string number, WorkOrderStatus status,
        DueDateUrgency urgency = DueDateUrgency.None) =>
        new() { WorkOrder = new WorkOrder { Number = number, Title = number, Status = status }, Urgency = urgency };

    [Test]
    public void Build_NoRows_ReturnsFiveEmptyColumnsInStatusOrder()
    {
        var columns = WorkOrderBoard.Build([]);

        columns.Select(c => c.Status.Key).ShouldBe(["Draft", "Assigned", "InProgress", "Complete", "Cancelled"]);
        columns.ShouldAllBe(c => c.Cards.Length == 0);
    }

    [Test]
    public void Build_RowsOfEachStatus_PutsEachRowInItsStatusColumn()
    {
        var rows = new[]
        {
            Row("1", WorkOrderStatus.Complete), Row("2", WorkOrderStatus.Draft), Row("3", WorkOrderStatus.InProgress),
            Row("4", WorkOrderStatus.Cancelled), Row("5", WorkOrderStatus.Assigned), Row("6", WorkOrderStatus.Draft)
        };

        var columns = WorkOrderBoard.Build(rows);

        columns.Sum(c => c.Cards.Length).ShouldBe(rows.Length);
        foreach (var column in columns)
        {
            column.Cards.ShouldAllBe(card => card.Status == column.Status);
        }

        columns.Single(c => c.Status == WorkOrderStatus.Draft).Cards.Length.ShouldBe(2);
    }

    [Test]
    public void Build_KeepsInputOrderWithinColumn()
    {
        var rows = new[]
        {
            Row("C", WorkOrderStatus.Assigned), Row("X", WorkOrderStatus.Draft), Row("A", WorkOrderStatus.Assigned),
            Row("B", WorkOrderStatus.Assigned)
        };

        var columns = WorkOrderBoard.Build(rows);

        columns.Single(c => c.Status == WorkOrderStatus.Assigned).Cards.Select(r => r.Number).ShouldBe(["C", "A", "B"]);
    }

    [Test]
    public void Build_ColumnPill_IsStatusPill()
    {
        var column = WorkOrderBoard.Build([]).Single(c => c.Status == WorkOrderStatus.InProgress);

        column.Pill.CssClass.ShouldContain("status-badge--in-progress");
        column.Pill.Text.ShouldBe("In Progress");
        column.Pill.HiddenText.ShouldBeNull();
    }

    [Test]
    public void Build_WaitingOnVendorRow_StaysInInProgressColumn()
    {
        var waiting = Row("W1", WorkOrderStatus.InProgress);
        waiting.WorkOrder.WaitingOnVendorSince = new DateTime(2026, 10, 14, 9, 0, 0);

        var columns = WorkOrderBoard.Build([waiting]);

        columns.Count.ShouldBe(5);
        var card = columns.Single(c => c.Status == WorkOrderStatus.InProgress).Cards.ShouldHaveSingleItem();
        card.Number.ShouldBe("W1");
        card.StatusPill.CssClass.ShouldContain("status-badge--waiting");
    }

    [Test]
    public void Build_OverdueOpenRow_StaysInStoredStatusColumn()
    {
        var overdue = Row("LATE", WorkOrderStatus.Assigned, DueDateUrgency.Overdue);

        var columns = WorkOrderBoard.Build([overdue]);

        columns.Single(c => c.Status == WorkOrderStatus.Assigned).Cards.ShouldBe([overdue]);
        columns.Where(c => c.Status != WorkOrderStatus.Assigned).ShouldAllBe(c => c.Cards.Length == 0);
    }

    [TestCase(30, "30 of 30")]
    [TestCase(0, "0 of 0")]
    public void CountText_ReturnsTotalOfTotal(int total, string expected)
    {
        WorkOrderBoard.CountText(total).ShouldBe(expected);
    }
}
