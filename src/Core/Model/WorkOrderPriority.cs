namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// How urgently a work order should be handled. Stored as a tinyint; new work orders default to
/// <see cref="Medium"/>.
/// </summary>
public enum WorkOrderPriority
{
    /// <summary>Can wait until the team has capacity.</summary>
    Low = 1,

    /// <summary>Normal handling; the default for new work orders.</summary>
    Medium = 2,

    /// <summary>Handle before other work.</summary>
    High = 3
}
