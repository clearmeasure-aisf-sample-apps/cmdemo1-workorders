using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// One page of the employee directory, ordered by last name, then first name, then user name.
/// <see cref="Take"/> is clamped to <see cref="MaxTake"/> so the read stays bounded. The default
/// page is the first <see cref="MaxTake"/> employees, which is the full directory below that cap,
/// in the same order the unbounded read used to return.
/// </summary>
public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
    /// <summary>Largest page the handler will read.</summary>
    public const int MaxTake = 1000;

    /// <summary>Rows to skip. Negative values are treated as zero.</summary>
    public int Skip { get; init; }

    /// <summary>Rows to return, from 1 through <see cref="MaxTake"/>.</summary>
    public int Take { get; init; } = MaxTake;
}
