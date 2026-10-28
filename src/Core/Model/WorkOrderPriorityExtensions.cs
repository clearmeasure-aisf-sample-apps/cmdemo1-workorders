namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Extension methods for <see cref="WorkOrderPriority"/>.
/// </summary>
public static class WorkOrderPriorityExtensions
{
    /// <summary>
    /// Returns a human-readable label for this priority.
    /// </summary>
    /// <param name="priority">The priority to label.</param>
    /// <returns>The label for the priority.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The priority is not defined.</exception>
    public static string ToLabel(this WorkOrderPriority priority) =>
        priority switch
        {
            WorkOrderPriority.Low => "Low",
            WorkOrderPriority.Medium => "Medium",
            WorkOrderPriority.High => "High",
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };
}
