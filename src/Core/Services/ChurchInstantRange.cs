namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// A range of stored church completion instants with an exclusive end: a value is inside it when it is at or after
/// <see cref="Start"/> and strictly before <see cref="EndExcluded"/>. Both bounds are
/// <see cref="DateTimeKind.Unspecified"/> UTC values, the shape persisted for instants such as
/// <c>WorkOrder.CompletedDate</c>, so they compare directly against stored column values.
/// </summary>
public readonly record struct ChurchInstantRange
{
    /// <summary>
    /// Creates the range from its inclusive start to its exclusive end.
    /// </summary>
    /// <exception cref="ArgumentException">When <paramref name="endExcluded"/> is not after <paramref name="start"/>.</exception>
    public ChurchInstantRange(DateTime start, DateTime endExcluded)
    {
        if (endExcluded <= start)
        {
            throw new ArgumentException(
                $"The church instant range is empty: end {endExcluded:o} must be after start {start:o}.",
                nameof(endExcluded));
        }

        Start = start;
        EndExcluded = endExcluded;
    }

    /// <summary>
    /// The first stored instant inside the range.
    /// </summary>
    public DateTime Start { get; }

    /// <summary>
    /// The first stored instant after the range; never itself inside it.
    /// </summary>
    public DateTime EndExcluded { get; }

    /// <summary>
    /// True when <paramref name="instant"/> is at or after <see cref="Start"/> and strictly before
    /// <see cref="EndExcluded"/>.
    /// </summary>
    public bool Contains(DateTime instant) => instant >= Start && instant < EndExcluded;
}
