using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// One role of one assignable employee with the employee's work-load counts, as the summary query returns it.
/// </summary>
internal sealed class EmployeeWorkLoadRow
{
    public EmployeeRoleRow Member { get; init; } = new();
    public int AssignedAndOpen { get; init; }
    public int InProgress { get; init; }
    public int Overdue { get; init; }
    public int CompletedLast30Days { get; init; }

    /// <summary>One summary per employee, in row order, with every role of the employee.</summary>
    public static EmployeeWorkLoadSummary[] ToSummaries(IEnumerable<EmployeeWorkLoadRow> rows) =>
        rows.GroupBy(row => row.Member.Id)
            .Select(group => new EmployeeWorkLoadSummary
            {
                Employee = EmployeeRoleRow.ToEmployee(group.Select(row => row.Member).ToList()),
                Load = group.First().ToLoad()
            })
            .ToArray();

    private EmployeeWorkLoad ToLoad() => new()
    {
        AssignedAndOpen = AssignedAndOpen,
        InProgress = InProgress,
        Overdue = Overdue,
        CompletedLast30Days = CompletedLast30Days
    };
}
