using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns every employee who can fulfill work orders, each with that employee's work-order load.
/// One handler runs one grouped SQL query. Vendors stay on <see cref="VendorsQuery"/> and buildings
/// stay on <see cref="BuildingRoomSummaryQuery"/>; this query is not feature-gated.
/// </summary>
public record EmployeeWorkLoadSummaryQuery : IRequest<EmployeeWorkLoadSummary[]>, IRemotableRequest;

/// <summary>
/// One assignable employee and the work-order counts shown for that employee on Teams &amp; Vendors.
/// </summary>
public class EmployeeWorkLoadSummary
{
    /// <summary>The employee, including roles.</summary>
    public Employee Employee { get; init; } = null!;

    /// <summary>Open, in-progress, overdue and recently completed counts for <see cref="Employee"/>.</summary>
    public EmployeeWorkLoad Load { get; init; } = null!;
}
