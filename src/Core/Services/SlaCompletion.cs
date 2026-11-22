namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The due date and stored completion instant of one completed work order, the only inputs the SLA math needs.
/// </summary>
public readonly record struct SlaCompletion(DateOnly DueDate, DateTime CompletedDate);
