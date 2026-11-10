using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

/// <summary>Search result rows for the search component fixtures.</summary>
public static class SearchResultRowMother
{
    public static WorkOrderSearchResultRow Unassigned(string number = "101") =>
        new() { WorkOrder = WorkOrderNumbered(number) };

    public static WorkOrderSearchResultRow AssignedToAda(string number = "102")
    {
        var workOrder = WorkOrderNumbered(number);
        workOrder.Assignee = new Employee("ada", "Ada", "Lovelace", "ada@example.com");
        return new WorkOrderSearchResultRow { WorkOrder = workOrder };
    }

    public static WorkOrderSearchResultRow WithPriority(WorkOrderPriority priority, string number = "103")
    {
        var workOrder = WorkOrderNumbered(number);
        workOrder.Priority = priority;
        return new WorkOrderSearchResultRow { WorkOrder = workOrder };
    }

    public static WorkOrderSearchResultRow WithStatus(WorkOrderStatus status, string number)
    {
        var workOrder = WorkOrderNumbered(number);
        workOrder.Status = status;
        return new WorkOrderSearchResultRow { WorkOrder = workOrder };
    }

    public static WorkOrderSearchResultRow OverdueSince(DateOnly dueDate, string number = "104")
    {
        var workOrder = WorkOrderNumbered(number);
        workOrder.Status = WorkOrderStatus.Assigned;
        workOrder.DueDate = dueDate;
        return new WorkOrderSearchResultRow
        {
            WorkOrder = workOrder,
            DueDateDisplay = dueDate.ToString("MMM d, yyyy"),
            DueDateCssClass = "overdue",
            DueDateUrgencyText = "Overdue",
            Urgency = DueDateUrgency.Overdue
        };
    }

    public static WorkOrderSearchResultRow[] Numbered(params string[] numbers) => numbers.Select(n => Unassigned(n)).ToArray();

    private static WorkOrder WorkOrderNumbered(string number) =>
        new() { Number = number, Title = $"Work order {number}", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Medium };
}
