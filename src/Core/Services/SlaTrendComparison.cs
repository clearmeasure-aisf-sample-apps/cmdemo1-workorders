namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The result of comparing this month's SLA on-time percentage with last month's: the <see cref="SlaTrend"/> and the
/// last-month percentage it was compared against (null when <see cref="Trend"/> is <see cref="SlaTrend.Unknown"/>).
/// </summary>
/// <param name="Trend">Direction of the change.</param>
/// <param name="ComparedPercentage">Last month's percentage, or null when no comparison is possible.</param>
public readonly record struct SlaTrendComparison(SlaTrend Trend, decimal? ComparedPercentage);
