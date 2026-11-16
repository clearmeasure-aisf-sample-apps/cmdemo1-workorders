using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="WorkOrderSearchSummaryQueryHandler"/> for the UI.Server Native AOT build. Each count uses the predicate
/// its card's filter applies, evaluated over the <see cref="WorkOrderSnapshot"/>.
/// </summary>
public class AotWorkOrderSearchSummaryQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderSearchSummaryQuery, WorkOrderSearchSummary>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    private const int CompletedWindowDays = 30;
    private const int OpenedWindowDays = 7;

    public async ValueTask<WorkOrderSearchSummary> Handle(WorkOrderSearchSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        var openedSince = snapshot.UtcNow.AddDays(-OpenedWindowDays);
        var open = snapshot.Open.ToList();
        var (completedCount, averageDaysToClose) = snapshot.CompletionsWithin(CompletedWindowDays);
        var summary = new WorkOrderSearchSummary
        {
            OpenCount = open.Count,
            OpenedLast7Days = open.Count(wo => wo.CreatedDate >= openedSince),
            OverdueCount = snapshot.All.Count(snapshot.IsOverdue),
            DueThisWeekCount = snapshot.All.Count(snapshot.IsDueThisWeek),
            InProgressCount = snapshot.All.Count(wo => wo.Status == WorkOrderStatus.InProgress),
            CompletedLast30Days = completedCount,
            AverageDaysToClose = averageDaysToClose
        };

        var user = string.IsNullOrWhiteSpace(request.UserName)
            ? null
            : await _source.FindEmployeeAsync(request.UserName, cancellationToken);
        if (user != null)
        {
            var assigned = snapshot.AssignedTo(user.Id).ToList();
            summary.AssignedToMeTotal = assigned.Count;
            summary.AssignedToMeCount = assigned.Count(WorkOrderSnapshot.IsOpen);
            summary.AssignedToMeDueToday = assigned.Count(wo => WorkOrderSnapshot.IsOpen(wo) && wo.DueDate == snapshot.Today);
            summary.CreatedByMeCount = snapshot.CreatedBy(user.Id).Count();
        }

        return summary;
    }
}
