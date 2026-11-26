using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Computes the Work Order Search summary counts. Each count uses the same predicate as the filter
/// its card applies, so a card's number equals the result total after the card is clicked.
/// </summary>
public class WorkOrderSearchSummaryQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderSearchSummaryQuery, WorkOrderSearchSummary>
{
    private const int CompletedWindowDays = 30;
    private const int OpenedWindowDays = 7;

    public async Task<WorkOrderSearchSummary> Handle(WorkOrderSearchSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var orders = context.Set<WorkOrder>().AsNoTracking();
        var today = ChurchTimeZone.Today(time);
        var openedSince = time.GetUtcNow().DateTime.AddDays(-OpenedWindowDays);
        var inProgress = WorkOrderStatus.InProgress;
        var open = WorkOrderQueryFilters.OpenOnly(orders);

        var summary = new WorkOrderSearchSummary
        {
            OpenCount = await open.CountAsync(cancellationToken),
            OpenedLast7Days = await open.CountAsync(wo => wo.CreatedDate >= openedSince, cancellationToken),
            OverdueCount = await WorkOrderQueryFilters.Overdue(orders, today).CountAsync(cancellationToken),
            DueThisWeekCount = await WorkOrderQueryFilters.DueThisWeek(orders, today).CountAsync(cancellationToken),
            InProgressCount = await orders.CountAsync(wo => wo.Status == inProgress, cancellationToken)
        };

        await AddCompletionsAsync(summary, orders, cancellationToken);
        if (await FindUserAsync(request.UserName, cancellationToken) is { } user)
        {
            await AddUserCountsAsync(summary, new UserWorkOrders(orders, user.Id, today), cancellationToken);
        }

        return summary;
    }

    private async Task AddCompletionsAsync(WorkOrderSearchSummary summary, IQueryable<WorkOrder> orders,
        CancellationToken cancellationToken)
    {
        var spans = await WorkOrderQueryFilters.CompletionSpansWithin(orders, CompletedWindowDays, time)
            .ToListAsync(cancellationToken);
        var statistics = new CompletionStatistics(spans);

        summary.CompletedLast30Days = statistics.Count;
        summary.AverageDaysToClose = statistics.AverageDaysToClose;
    }

    private async Task<Employee?> FindUserAsync(string? userName, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(userName)
            ? null
            : await context.Set<Employee>().AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserName == userName, cancellationToken);

    private static async Task AddUserCountsAsync(WorkOrderSearchSummary summary, UserWorkOrders mine,
        CancellationToken cancellationToken)
    {
        summary.AssignedToMeTotal = await mine.AssignedToMe.CountAsync(cancellationToken);
        summary.AssignedToMeCount = await mine.OpenAssignedToMe.CountAsync(cancellationToken);
        summary.AssignedToMeDueToday = await mine.OpenAssignedToMeDueToday.CountAsync(cancellationToken);
        summary.CreatedByMeCount = await mine.CreatedByMe.CountAsync(cancellationToken);
    }
}
