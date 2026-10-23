// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrder : EntityBase<WorkOrder>
{
    /// <summary>
    /// Maximum length of <see cref="RoomNumber"/> entry in the form and validator.
    /// </summary>
    public const int RoomNumberEntryMaxLength = 50;

    /// <summary>
    /// Maximum length of <see cref="RoomNumber"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int RoomNumberMaxLength = 900;

    /// <summary>
    /// Maximum length of <see cref="Description"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>
    /// Maximum length of <see cref="Instructions"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int InstructionsMaxLength = 4000;

    /// <summary>
    /// Maximum length of <see cref="Building"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int BuildingMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="Floor"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int FloorMaxLength = 20;

    /// <summary>
    /// Maximum length of an optional external reference.
    /// </summary>
    public const int ExternalReferenceMaxLength = 100;

    /// <summary>
    /// Maximum length of optional notes on how to reach the room. The limit is 500 characters.
    /// </summary>
    public const int AccessNotesMaxLength = 500;

    /// <summary>
    /// Maximum length of <see cref="Title"/> accepted by persistence and the work-order form.
    /// </summary>
    public const int TitleMaxLength = 300;

    public string? Title { get; set; } = "";

    public string? Description { get; set; } = "";

    public string? Instructions
    {
        get;
        set => field = GetTruncatedString(value);
    } = "";

    public string? RoomNumber { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    public Employee? Creator { get; set; }

    public Employee? Assignee { get; set; }

    public string? Number { get; set; }

    public string FriendlyStatus => GetTextForStatus();


    public DateTime? AssignedDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Optional calendar due date (date-only, no time). Null when unset.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// How urgently the work order should be handled. Defaults to <see cref="WorkOrderPriority.Medium"/>.
    /// </summary>
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    /// <summary>
    /// Optional name of the building the work happens in (for example "Sanctuary"). Null when unset.
    /// </summary>
    public string? Building { get; set; }

    /// <summary>
    /// Optional floor the work happens on. Null when unset.
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// Optional reference to an external system.
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Optional notes on how to reach the room. Null when unset.
    /// </summary>
    public string? AccessNotes { get; set; }

    /// <summary>
    /// When the assignee started waiting on a vendor (parts, contractor, quote). Set while the assignee
    /// waits on a vendor; only meaningful while <see cref="Status"/> is In Progress. Null when not waiting.
    /// </summary>
    public DateTime? WaitingOnVendorSince { get; set; }

    /// <summary>
    /// <c>true</c> when the work order is In Progress and waiting on a vendor. A stale
    /// <see cref="WaitingOnVendorSince"/> on any other status is ignored.
    /// </summary>
    public bool IsWaitingOnVendor => Status == WorkOrderStatus.InProgress && WaitingOnVendorSince != null;

    private string GetTruncatedString(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var maxLength = Math.Min(InstructionsMaxLength, value.Length);
        return value.Substring(0, maxLength);
    }

    protected string GetTextForStatus()
    {
        return Status.ToString();
    }

    public override Guid Id { get; set; }

    public override string ToString()
    {
        return "Work Order " + Number;
    }

    public void ChangeStatus(WorkOrderStatus status)
    {
        Status = status;
    }

    public void ChangeStatus(Employee employee, DateTime date, WorkOrderStatus status)
    {
        Status = status;
        if (status != WorkOrderStatus.InProgress)
        {
            WaitingOnVendorSince = null;
        }
    }

    public string GetMessage()
    {
        return "Work Order " + Number + " is now in Status " + Status;
    }

    public bool CanReassign()
    {
        return Status == WorkOrderStatus.Draft;
    }

    /// <summary>
    /// Returns the urgency classification for this work order's due date relative to today
    /// in America/Chicago.
    /// </summary>
    public DueDateUrgency GetDueDateUrgency() =>
        DueDateUrgencyCalculator.Calculate(this, TimeProvider.System);

    /// <summary>
    /// Returns <c>true</c> when this work order's due date has passed and the work order
    /// is still open.
    /// </summary>
    public bool IsOverdue() => GetDueDateUrgency() == DueDateUrgency.Overdue;

    public ICollection<WorkOrderAttachment> Attachments { get; set; } = new List<WorkOrderAttachment>();
}