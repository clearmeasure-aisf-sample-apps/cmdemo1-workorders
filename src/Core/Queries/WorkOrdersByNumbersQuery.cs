using ClearMeasure.Bootcamp.Core.Model;
using Mediator;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Loads the work orders whose numbers are in <paramref name="Numbers"/>, in one query.
/// Duplicates and blanks are ignored. At most <see cref="MaxNumbers"/> distinct numbers.
/// </summary>
public record WorkOrdersByNumbersQuery(string[] Numbers) : IRequest<WorkOrder[]>, IRemotableRequest
{
    /// <summary>Largest distinct-number set the handler will read in one query.</summary>
    public const int MaxNumbers = 100;

    /// <summary>The distinct, non-blank numbers to read. More than <see cref="MaxNumbers"/> is refused.</summary>
    public string[] DistinctNumbers()
    {
        var numbers = (Numbers ?? [])
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return numbers.Length <= MaxNumbers
            ? numbers
            : throw new InvalidOperationException($"WorkOrdersByNumbersQuery accepts at most {MaxNumbers} numbers.");
    }}
