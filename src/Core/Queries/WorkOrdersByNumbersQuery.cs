using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Loads the work orders whose numbers are in <paramref name="Numbers"/>, in one query.
/// Duplicates and blanks are ignored. At most <see cref="MaxNumbers"/> distinct numbers.
/// </summary>
public record WorkOrdersByNumbersQuery(string[] Numbers) : IRequest<WorkOrder[]>, IRemotableRequest
{
    /// <summary>Largest distinct-number set the handler will read in one query.</summary>
    public const int MaxNumbers = 100;
}
