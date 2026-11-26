namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>Person-facing text for each <see cref="WorkOrderActivityKind"/>.</summary>
public static class WorkOrderActivityKindText
{
    /// <summary>Gets the event name shown to people, for example "Completed".</summary>
    public static string DisplayName(WorkOrderActivityKind kind) => kind switch
    {
        WorkOrderActivityKind.Created => "Created",
        WorkOrderActivityKind.Assigned => "Assigned",
        WorkOrderActivityKind.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
