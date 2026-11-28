using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Runs a <see cref="WorkOrderSpecificationQuery"/>. The default order is newest first, then by number.
/// Due-date order, when requested, keeps work orders with no due date last.
/// </summary>
public class WorkOrderSearchHandler(DataContext context, TimeProvider time, ChurchCalendar calendar) : IRequestHandler<WorkOrderSpecificationQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(WorkOrderSpecificationQuery specification,
        CancellationToken cancellationToken = default)
    {
        var filtered = WorkOrderQueryFilters.Apply(context.Set<WorkOrder>(), specification, time, calendar);
        var ordered = ApplyOrder(filtered, specification);
        var page = specification.Take is { } take ? ordered.Take(take) : ordered;
        return await page.ToArrayAsync(cancellationToken);
    }

    private static IQueryable<WorkOrder> ApplyOrder(IQueryable<WorkOrder> query, WorkOrderSpecificationQuery specification)
    {
        if (!specification.OrderByDueDate)
        {
            return query.OrderByDescending(wo => wo.CreatedDate).ThenBy(wo => wo.Number);
        }

        var nullsLast = query.OrderBy(wo => wo.DueDate == null);
        return specification.DueDateDescending
            ? nullsLast.ThenByDescending(wo => wo.DueDate).ThenBy(wo => wo.Number)
            : nullsLast.ThenBy(wo => wo.DueDate).ThenBy(wo => wo.Number);
    }
}
