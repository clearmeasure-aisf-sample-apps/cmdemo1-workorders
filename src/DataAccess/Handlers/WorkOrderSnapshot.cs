using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Every work order (creator and assignee auto-included), read by <see cref="WorkOrderSnapshotSource"/> with one precompiled query, and the
/// <see cref="WorkOrderSpecifications"/> as in-memory predicates over it. The UI.Server Native AOT build
/// (<c>UiServerAot=true</c>) answers its search, summary, report and load queries from this snapshot because EF 11
/// cannot precompile the composed queries the JIT handlers send to the database.
/// </summary>
/// <remarks>
/// String keys compare ignoring case, as SQL Server's default collation does for the JIT handlers. That suits a
/// church-sized work-order table; revisit when EF precompiles composed queries.
/// </remarks>
internal sealed class WorkOrderSnapshot
{
    private readonly TimeProvider _time;

    internal WorkOrderSnapshot(WorkOrder[] all, TimeProvider time)
    {
        All = all;
        _time = time;
        Today = ChurchTimeZone.Today(time);
        IsOverdue = WorkOrderSpecifications.Overdue(Today).Compile();
        IsDueThisWeek = WorkOrderSpecifications.DueThisWeek(Today).Compile();
    }

    public IReadOnlyList<WorkOrder> All { get; }

    public DateOnly Today { get; }

    public DateTime UtcNow => _time.GetUtcNow().DateTime;

    public Func<WorkOrder, bool> IsOverdue { get; }

    public Func<WorkOrder, bool> IsDueThisWeek { get; }

    public static Func<WorkOrder, bool> IsOpen { get; } = WorkOrderSpecifications.Open.Compile();

    public Func<WorkOrder, bool> IsCompletedWithin(int days) =>
        WorkOrderSpecifications.CompletedWithin(days, _time).Compile();

    public IEnumerable<WorkOrder> Open => All.Where(IsOpen);

    public IEnumerable<WorkOrder> AssignedTo(Guid employeeId) => All.Where(wo => wo.Assignee?.Id == employeeId);

    public IEnumerable<WorkOrder> CreatedBy(Guid employeeId) => All.Where(wo => wo.Creator?.Id == employeeId);

    /// <summary>Count and average days to close of the work orders completed within <paramref name="days"/>.</summary>
    public (int Count, double? AverageDaysToClose) CompletionsWithin(int days)
    {
        var completed = All.Where(IsCompletedWithin(days)).ToList();
        var durations = completed
            .Where(wo => wo.CreatedDate.HasValue && wo.CompletedDate.HasValue)
            .Select(wo => (wo.CompletedDate!.Value - wo.CreatedDate!.Value).TotalDays)
            .ToList();
        return (completed.Count, durations.Count == 0 ? null : durations.Average());
    }
}
