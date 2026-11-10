using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// One employee's slice of the work orders for the search summary's "my" cards: assigned to them (all, open, open and
/// due today) and created by them.
/// </summary>
internal sealed record UserWorkOrders(IQueryable<WorkOrder> Orders, Guid UserId, DateOnly Today)
{
    public IQueryable<WorkOrder> AssignedToMe =>
        Orders.Where(wo => wo.Assignee != null && wo.Assignee.Id == UserId);

    public IQueryable<WorkOrder> OpenAssignedToMe => WorkOrderQueryFilters.OpenOnly(AssignedToMe);

    public IQueryable<WorkOrder> OpenAssignedToMeDueToday => OpenAssignedToMe.Where(wo => wo.DueDate == Today);

    public IQueryable<WorkOrder> CreatedByMe =>
        Orders.Where(wo => wo.Creator != null && wo.Creator.Id == UserId);
}
