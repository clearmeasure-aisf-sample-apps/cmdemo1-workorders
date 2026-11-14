using ClearMeasure.Bootcamp.AcceptanceTests.Builders;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

/// <summary>Optional fields when seeding a work order through test-support.</summary>
public sealed class WorkOrderSeedSpec
{
    /// <summary>Status; defaults to Draft when null.</summary>
    public WorkOrderStatus? Status { get; init; }

    /// <summary>Priority; defaults to Medium.</summary>
    public WorkOrderPriority Priority { get; init; } = WorkOrderPriority.Medium;

    /// <summary>Optional due date (may be in the past for overdue seeds).</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Optional assignee.</summary>
    public Employee? Assignee { get; init; }

    /// <summary>Optional title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Maps this spec onto mother seed fields.</summary>
    public WorkOrderSeedFields ToFields() => new()
    {
        Priority = Priority,
        DueDate = DueDate,
        AssigneeUserName = Assignee?.UserName,
        Title = Title,
        RoomNumber = RoomNumber
    };
}
