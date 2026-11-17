using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;
/// <summary>
/// <see cref="WorkOrderReportsQueryHandler"/> for the UI.Server Native AOT build, computed over the
/// <see cref="WorkOrderSnapshot"/> with the same windows and predicates.
/// </summary>
public class AotWorkOrderReportsQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderReportsQuery, WorkOrderReportsData>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    private const int CompletedWindowDays = 30;
    private const int WeeksToShow = 8;

    public async ValueTask<WorkOrderReportsData> Handle(WorkOrderReportsQuery request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        var open = snapshot.Open.ToList();
        var (completedCount, averageDaysToClose) = snapshot.CompletionsWithin(CompletedWindowDays);
        var data = new WorkOrderReportsData
        {
            OverdueCount = snapshot.All.Count(snapshot.IsOverdue),
            CompletedLast30Days = completedCount,
            AverageDaysToClose = averageDaysToClose
        };

        AddCounts(data.CountByStatus, open.Select(wo => wo.Status.Key), StringComparer.Ordinal);
        AddCounts(data.CountByPriority, open.Select(wo => wo.Priority.ToString()), StringComparer.Ordinal);
        AddCounts(data.CountByBuilding, open.Where(wo => wo.Building != null).Select(wo => wo.Building!),
            StringComparer.Ordinal);

        var weeks = new CompletionWeeks(snapshot.Today, WeeksToShow);
        var start = weeks.StartDate.ToDateTime(TimeOnly.MinValue);
        data.CompletedPerWeek = weeks.CountCompletions(snapshot.All
            .Where(wo => wo.Status == WorkOrderStatus.Complete && wo.CompletedDate >= start)
            .Select(wo => wo.CompletedDate!.Value));
        return data;
    }

    private static void AddCounts(IDictionary<string, int> counts, IEnumerable<string> keys, StringComparer comparer)
    {
        foreach (var group in keys.GroupBy(key => key, comparer).OrderBy(group => group.Key, comparer))
        {
            counts[group.Key] = group.Count();
        }
    }
}
