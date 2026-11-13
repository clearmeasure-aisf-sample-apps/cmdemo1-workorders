using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>Optional fields for API work-order seeding.</summary>
public sealed class WorkOrderSeedFields
{
    /// <summary>Priority; defaults to Medium.</summary>
    public WorkOrderPriority Priority { get; init; } = WorkOrderPriority.Medium;

    /// <summary>Optional due date.</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Optional assignee user name.</summary>
    public string? AssigneeUserName { get; init; }

    /// <summary>Optional title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Optional instructions.</summary>
    public string? Instructions { get; init; }

    /// <summary>Optional explicit work-order number.</summary>
    public string? Number { get; init; }

    /// <summary>Optional building name.</summary>
    public string? Building { get; init; }

    /// <summary>Optional created date.</summary>
    public DateTime? CreatedDate { get; init; }

    /// <summary>Optional completed date.</summary>
    public DateTime? CompletedDate { get; init; }
}
