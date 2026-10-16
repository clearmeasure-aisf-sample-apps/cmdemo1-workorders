using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Retrieves work order load information for a specific employee.
/// </summary>
public record EmployeeWorkLoadQuery(string EmployeeUserName) : IRequest<EmployeeWorkLoad>, IRemotableRequest;

/// <summary>
/// Work order load statistics for an employee.
/// </summary>
public class EmployeeWorkLoad
{
    /// <summary>
    /// Count of work orders assigned to the employee with status Draft, Assigned, or InProgress.
    /// </summary>
    public int AssignedAndOpen { get; init; }

    /// <summary>
    /// Count of work orders assigned to the employee with status InProgress.
    /// </summary>
    public int InProgress { get; init; }

    /// <summary>
    /// Count of open work orders (Draft, Assigned, InProgress) assigned to the employee with due date in the past.
    /// </summary>
    public int Overdue { get; init; }

    /// <summary>
    /// Count of work orders assigned to the employee with status Complete and completed within the last 30 days.
    /// </summary>
    public int CompletedLast30Days { get; init; }
}
