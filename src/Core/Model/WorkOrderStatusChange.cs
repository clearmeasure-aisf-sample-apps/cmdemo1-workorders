namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrderStatusChange : EntityBase<WorkOrderStatusChange>
{
    public override Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    public WorkOrderStatus FromStatus { get; set; } = WorkOrderStatus.Draft;

    public WorkOrderStatus ToStatus { get; set; } = WorkOrderStatus.Draft;

    public Guid ChangedById { get; set; }

    public Employee? ChangedBy { get; set; }

    public DateTime ChangedDate { get; set; }
}
