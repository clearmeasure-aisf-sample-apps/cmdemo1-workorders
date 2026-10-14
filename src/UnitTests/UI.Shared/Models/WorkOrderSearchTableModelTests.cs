using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderSearchSorterTests
{
    private static WorkOrderSearchResultRow Row(string number, string title, WorkOrderStatus status, WorkOrderPriority priority,
        string? assignee, string? room, DateOnly? due) =>
        new()
        {
            WorkOrder = new WorkOrder
            {
                Number = number, Title = title, Status = status, Priority = priority, RoomNumber = room, DueDate = due,
                Assignee = assignee == null ? null : new Employee(assignee, assignee, "Z", "e")
            }
        };

    private static readonly WorkOrderSearchResultRow[] Rows =
    [
        Row("B", "beta", WorkOrderStatus.InProgress, WorkOrderPriority.Low, "Ned", "Kitchen", new DateOnly(2026, 5, 1)),
        Row("C", "Alpha", WorkOrderStatus.Assigned, WorkOrderPriority.High, null, "attic", null),
        Row("A", "gamma", WorkOrderStatus.Draft, WorkOrderPriority.Medium, "Agnes", null, new DateOnly(2026, 1, 1))
    ];

    [TestCase(SortColumns.Number, false, "A,B,C")]
    [TestCase(SortColumns.Number, true, "C,B,A")]
    [TestCase(SortColumns.Title, false, "C,B,A")]
    [TestCase(SortColumns.Title, true, "A,B,C")]
    [TestCase(SortColumns.Status, false, "C,A,B")]
    [TestCase(SortColumns.Status, true, "B,A,C")]
    [TestCase(SortColumns.Priority, false, "B,A,C")]
    [TestCase(SortColumns.Priority, true, "C,A,B")]
    [TestCase(SortColumns.Assignee, false, "C,A,B")]
    [TestCase(SortColumns.Assignee, true, "B,A,C")]
    [TestCase(SortColumns.Room, false, "A,C,B")]
    [TestCase(SortColumns.Room, true, "B,C,A")]
    [TestCase(SortColumns.DueDate, false, "A,B,C")]
    [TestCase(SortColumns.DueDate, true, "B,A,C")]
    public void Sort_EachColumnBothDirections(string column, bool descending, string expected)
    {
        string.Join(",", WorkOrderSearchSorter.Sort(Rows, column, descending).Select(r => r.Number)).ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("Bogus")]
    public void Sort_NoOrUnknownColumn_KeepsHandlerOrder(string? column)
    {
        WorkOrderSearchSorter.Sort(Rows, column, true).Select(r => r.Number).ShouldBe(["B", "C", "A"]);
    }

    [TestCase("DueDate:desc", "DueDate", true)]
    [TestCase("Title:asc", "Title", false)]
    [TestCase("", null, false)]
    [TestCase(null, null, false)]
    [TestCase("Nope:desc", null, true)]
    public void ParseOption_ReadsColumnAndDirection(string? value, string? column, bool descending)
    {
        WorkOrderSearchSorter.ParseOption(value).ShouldBe((column, descending));
    }
}

[TestFixture]
public class WorkOrderSearchPagerTests
{
    private static WorkOrderSearchResultRow[] Rows(int count) =>
        Enumerable.Range(1, count).Select(i => new WorkOrderSearchResultRow { WorkOrder = new WorkOrder { Number = $"{i}" } }).ToArray();

    [Test]
    public void GetPage_FirstPage_RangeAndNumbers()
    {
        var page = WorkOrderSearchPager.GetPage(Rows(60), 1);

        page.Rows.Length.ShouldBe(25);
        page.PageCount.ShouldBe(3);
        page.RangeText.ShouldBe("Showing 1–25 of 60 work orders");
        page.CountText.ShouldBe("25 of 60");
        page.PageNumbers.ShouldBe([1, 2, 3]);
        page.HasPrevious.ShouldBeFalse();
        page.HasNext.ShouldBeTrue();
    }

    [Test]
    public void GetPage_LastPartialPage()
    {
        var page = WorkOrderSearchPager.GetPage(Rows(60), 3);

        page.Rows.Select(r => r.Number).ShouldBe(Enumerable.Range(51, 10).Select(i => $"{i}"));
        page.RangeText.ShouldBe("Showing 51–60 of 60 work orders");
        page.HasNext.ShouldBeFalse();
    }

    [TestCase(0, 1)]
    [TestCase(-3, 1)]
    [TestCase(99, 2)]
    public void GetPage_ClampsRequestedPage(int requested, int expected)
    {
        WorkOrderSearchPager.GetPage(Rows(30), requested).Page.ShouldBe(expected);
    }

    [Test]
    public void GetPage_Empty()
    {
        var page = WorkOrderSearchPager.GetPage([], 4);

        page.Page.ShouldBe(1);
        page.PageCount.ShouldBe(1);
        page.Rows.ShouldBeEmpty();
        page.RangeText.ShouldBe("Showing 0 of 0 work orders");
        page.CountText.ShouldBe("0 of 0");
    }

    [Test]
    public void GetPage_SingleRow_UsesSingular()
    {
        WorkOrderSearchPager.GetPage(Rows(1), 1).RangeText.ShouldBe("Showing 1–1 of 1 work order");
    }
}

[TestFixture]
public class WorkOrderRowSelectionTests
{
    [Test]
    public void Toggle_AddsThenRemoves()
    {
        var selection = new WorkOrderRowSelection();

        selection.Toggle("A");
        selection.IsSelected("A").ShouldBeTrue();
        selection.Toggle("A");

        selection.IsSelected("A").ShouldBeFalse();
        selection.Count.ShouldBe(0);
    }

    [Test]
    public void TogglePage_SelectsPage_ThenClearsIt_KeepingOtherPages()
    {
        var selection = new WorkOrderRowSelection();
        selection.Toggle("Z");

        selection.TogglePage(["A", "B"]);
        selection.Numbers.ShouldBe(["Z", "A", "B"], ignoreOrder: true);
        selection.AllSelected(["A", "B"]).ShouldBeTrue();

        selection.TogglePage(["A", "B"]);
        selection.Numbers.ShouldBe(["Z"]);
    }

    [Test]
    public void IsIndeterminate_WhenPartOfPageSelected()
    {
        var selection = new WorkOrderRowSelection();
        selection.IsIndeterminate(["A", "B"]).ShouldBeFalse();

        selection.Toggle("A");
        selection.IsIndeterminate(["A", "B"]).ShouldBeTrue();
        selection.AllSelected(["A", "B"]).ShouldBeFalse();

        selection.Toggle("B");
        selection.IsIndeterminate(["A", "B"]).ShouldBeFalse();
        selection.AllSelected([]).ShouldBeFalse();
    }

    [Test]
    public void Clear_RemovesAll()
    {
        var selection = new WorkOrderRowSelection();
        selection.TogglePage(["A", "B", "C"]);

        selection.Clear();

        selection.Count.ShouldBe(0);
    }
}

[TestFixture]
public class WorkOrderCsvExporterTests
{
    [Test]
    public void ToCsv_WritesHeaderAndRows()
    {
        var row = new WorkOrderSearchResultRow
        {
            WorkOrder = new WorkOrder
            {
                Number = "38173BA", Title = "Mow grass, north lawn", Status = WorkOrderStatus.Assigned,
                Priority = WorkOrderPriority.High, Building = "Grounds", RoomNumber = "Lawn", DueDate = new DateOnly(2026, 10, 18),
                CreatedDate = new DateTime(2026, 10, 1, 9, 30, 0),
                Assignee = new Employee("gw", "Groundskeeper", "Willie", "e"), Creator = new Employee("tl", "Timothy", "Lovejoy", "e")
            }
        };

        var lines = WorkOrderCsvExporter.ToCsv([row]).Split("\r\n");

        lines[0].ShouldBe("Work Order,Title,Status,Priority,Assignee,Creator,Building,Room,Due Date,Created");
        lines[1].ShouldBe("38173BA,\"Mow grass, north lawn\",Assigned,High,Groundskeeper Willie,Timothy Lovejoy,Grounds,Lawn,2026-10-18,2026-10-01 09:30");
        lines[2].ShouldBe(string.Empty);
    }

    [TestCase("plain", "plain")]
    [TestCase("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [TestCase("two\nlines", "\"two\nlines\"")]
    [TestCase(null, "")]
    [TestCase("=SUM(A1)", "'=SUM(A1)")]
    [TestCase("+1", "'+1")]
    [TestCase("-2", "'-2")]
    [TestCase("@cmd", "'@cmd")]
    [TestCase("=1,2", "\"'=1,2\"")]
    [TestCase("\t=1+1", "'\t=1+1")]
    [TestCase("\r=1+1", "\"'\r=1+1\"")]
    public void Escape_QuotesAndGuardsFormulas(string? value, string expected)
    {
        WorkOrderCsvExporter.Escape(value).ShouldBe(expected);
    }

    [Test]
    public void FileName_UsesDate()
    {
        WorkOrderCsvExporter.FileName(new DateOnly(2026, 10, 13)).ShouldBe("work-orders-2026-10-13.csv");
    }
}

[TestFixture]
public class StatusPillPresenterTests
{
    [TestCase("Draft", "Draft", "status-badge status-badge--draft")]
    [TestCase("Assigned", "Assigned", "status-badge status-badge--assigned")]
    [TestCase("InProgress", "In Progress", "status-badge status-badge--in-progress")]
    [TestCase("Complete", "Complete", "status-badge status-badge--complete")]
    [TestCase("Cancelled", "Cancelled", "status-badge status-badge--cancelled")]
    public void Present_EachStatus_UsesFriendlyNameAndClass(string key, string text, string css)
    {
        var pill = StatusPillPresenter.Present(new WorkOrder { Status = WorkOrderStatus.FromKey(key) }, false);

        pill.ShouldBe(new StatusPill(text, css, null));
    }

    [Test]
    public void Present_Overdue_ShowsOverdueWithStoredStatusHidden()
    {
        var pill = StatusPillPresenter.Present(new WorkOrder { Status = WorkOrderStatus.Assigned }, true);

        pill.ShouldBe(new StatusPill("Overdue", "status-badge status-badge--overdue", ", status Assigned"));
    }

    [Test]
    public void Row_ExposesDisplayNumberPriorityAndAvatar()
    {
        var row = new WorkOrderSearchResultRow
        {
            WorkOrder = new WorkOrder { Number = "38173BA", Priority = WorkOrderPriority.Low, Assignee = new Employee("ned", "Ned", "Flanders", "e") },
            Urgency = DueDateUrgency.Overdue
        };

        row.DisplayNumber.ShouldBe("WO-38173BA");
        row.PriorityCssClass.ShouldBe("priority--low");
        row.AssigneeInitials.ShouldBe("NF");
        row.AvatarCssClass.ShouldMatch("^avatar--[0-5]$");
        row.StatusPill.Text.ShouldBe("Overdue");
    }
}
