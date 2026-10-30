using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Returns the work orders changed most recently. Each date column is read newest first (then by
/// number) and limited to the requested count; the merged rows are ordered by their latest date and
/// number, which gives the exact top N without comparing columns in SQL.
/// </summary>
public class RecentWorkOrdersQueryHandler(DataContext context) : IRequestHandler<RecentWorkOrdersQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(RecentWorkOrdersQuery request, CancellationToken cancellationToken = default)
    {
        var completed = await LatestBy(wo => wo.CompletedDate, request.Count, cancellationToken);
        var assigned = await LatestBy(wo => wo.AssignedDate, request.Count, cancellationToken);
        var created = await LatestBy(wo => wo.CreatedDate, request.Count, cancellationToken);

        return completed.Concat(assigned).Concat(created)
            .DistinctBy(wo => wo.Id)
            .OrderByDescending(wo => WorkOrderActivity.Latest(wo)!.WhenUtc)
            .ThenBy(wo => wo.Number, StringComparer.Ordinal)
            .Take(request.Count)
            .ToArray();
    }

    private Task<List<WorkOrder>> LatestBy(Expression<Func<WorkOrder, DateTime?>> date, int count,
        CancellationToken cancellationToken) =>
        context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(NotNull(date))
            .OrderByDescending(date)
            .ThenBy(wo => wo.Number)
            .Take(count)
            .ToListAsync(cancellationToken);

    private static Expression<Func<WorkOrder, bool>> NotNull(Expression<Func<WorkOrder, DateTime?>> date) =>
        Expression.Lambda<Func<WorkOrder, bool>>(
            Expression.NotEqual(date.Body, Expression.Constant(null, typeof(DateTime?))),
            date.Parameters);
}
