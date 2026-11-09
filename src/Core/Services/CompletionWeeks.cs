using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The last <see cref="Count"/> weeks up to <see cref="Today"/>, each named by its week-ending date: the most recent
/// Sunday on or before a day. Completions are counted per week; a week with none counts zero.
/// </summary>
public readonly record struct CompletionWeeks(DateOnly Today, int Count)
{
    /// <summary>The first day whose completions can fall in one of the weeks.</summary>
    public DateOnly StartDate => Today.AddDays(-(Count * 7));

    public static DateOnly WeekEnding(DateOnly day) => day.AddDays(-(int)day.DayOfWeek);

    public List<WorkOrderReportsData.WeeklyCount> CountCompletions(IEnumerable<DateTime> completedDates)
    {
        var today = Today;
        var weekEndings = Enumerable.Range(0, Count)
            .Select(weeksAgo => WeekEnding(today.AddDays(-weeksAgo * 7)))
            .ToHashSet();
        var completionsPerWeek = completedDates
            .Select(completed => WeekEnding(DateOnly.FromDateTime(completed)))
            .Where(weekEndings.Contains)
            .CountBy(weekEnding => weekEnding)
            .ToDictionary();

        return weekEndings
            .Order()
            .Select(weekEnding => new WorkOrderReportsData.WeeklyCount
            {
                WeekEndingDate = weekEnding,
                Count = completionsPerWeek.GetValueOrDefault(weekEnding)
            })
            .ToList();
    }
}
