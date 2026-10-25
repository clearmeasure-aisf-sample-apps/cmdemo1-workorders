using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Utility methods for WorkOrder string fields.
/// </summary>
public static class WorkOrderStringUtility
{
    /// <summary>
    /// Truncates a description to <see cref="WorkOrder.DescriptionMaxLength"/>.
    /// </summary>
    public static string TruncateDescription(string description) =>
        description.Length <= WorkOrder.DescriptionMaxLength
            ? description
            : description[..WorkOrder.DescriptionMaxLength];
}
