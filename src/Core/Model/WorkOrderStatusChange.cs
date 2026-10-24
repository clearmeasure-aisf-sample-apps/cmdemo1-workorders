namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrderStatusChange : EntityBase<WorkOrderStatusChange>
{
    public override Guid Id { get; set; }

    public Guid WorkOrderId { get; init; }

    public WorkOrderStatus FromStatus { get; init; } = null!;

    public WorkOrderStatus ToStatus { get; init; } = null!;

    public Guid ChangedById { get; init; }

    public Employee? ChangedBy { get; init; }

    public DateTime ChangedDate { get; init; }

    /// <summary>
    /// Reason recorded for this change. Set when a work order is shelved; null for every other transition.
    /// </summary>
    public string? Reason { get; init; }
}
