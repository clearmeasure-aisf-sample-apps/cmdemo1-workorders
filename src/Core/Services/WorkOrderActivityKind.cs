namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The kind of dated event recorded on a work order (created, assigned or completed). Member values
/// are assigned in tie-break order, lowest first: when two events share a date the higher kind wins,
/// so the comparison is the enum itself.
/// </summary>
public enum WorkOrderActivityKind
{
    Created = 1,
    Assigned = 2,
    Completed = 3
}
