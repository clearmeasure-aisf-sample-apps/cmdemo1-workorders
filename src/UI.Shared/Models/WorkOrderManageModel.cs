// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderManageModel : IValidatableObject
{
    /// <summary>The form for <paramref name="workOrder"/>, with its dates shown in the church time zone.</summary>
    public static WorkOrderManageModel For(EditMode mode, WorkOrder workOrder, TimeProvider clock) => new()
    {
        WorkOrder = workOrder,
        Mode = mode,
        WorkOrderNumber = workOrder.Number,
        CreatorFullName = workOrder.Creator!.DisplayName,
        AssignedToUserName = workOrder.Assignee?.UserName,
        Title = workOrder.Title,
        Description = workOrder.Description,
        Instructions = workOrder.Instructions,
        RoomNumber = workOrder.RoomNumber,
        OriginalRoomNumber = workOrder.RoomNumber,
        CreatedDate = ClientDateFormatter.Format(workOrder.CreatedDate),
        CreatedRelativeTime = RelativeTimeFormatter.Format(workOrder.CreatedDate, clock),
        AssignedDate = ClientDateFormatter.Format(workOrder.AssignedDate),
        CompletedDate = ClientDateFormatter.Format(workOrder.CompletedDate),
        DueDateInput = workOrder.DueDate,
        OriginalDueDate = workOrder.DueDate,
        ChurchToday = ChurchTimeZone.Today(clock),
        Priority = workOrder.Priority,
        Building = workOrder.Building,
        Floor = workOrder.Floor,
        DueDateDisplay = ClientDateFormatter.Format(workOrder.DueDate)
    };

    /// <summary>Copies the edited fields onto <paramref name="workOrder"/>; blank building and floor are cleared.</summary>
    public void ApplyTo(WorkOrder workOrder, Employee? assignee)
    {
        workOrder.Number = WorkOrderNumber;
        workOrder.Assignee = assignee;
        workOrder.Title = Title;
        workOrder.Description = Description;
        workOrder.Instructions = Instructions;
        workOrder.RoomNumber = RoomNumber;
        workOrder.DueDate = DueDateInput;
        workOrder.Priority = Priority;
        workOrder.Building = string.IsNullOrWhiteSpace(Building) ? null : Building.Trim();
        workOrder.Floor = string.IsNullOrWhiteSpace(Floor) ? null : Floor.Trim();
    }

    public WorkOrder? WorkOrder { get; private set; }
    public EditMode Mode { get; private set; }

    public string? WorkOrderNumber { get; private set; }

    public string? CreatorFullName { get; private set; }

    public string? AssignedToUserName { get; set; }

    [Required, StringLength(WorkOrder.TitleMaxLength, ErrorMessage = "Title cannot exceed 300 characters.")]
    public string? Title { get; set; }

    [Required, StringLength(WorkOrder.DescriptionMaxLength, ErrorMessage = "Description cannot exceed 4000 characters.")]
    public string? Description { get; set; }

    [StringLength(WorkOrder.InstructionsMaxLength, ErrorMessage = "Instructions cannot exceed 4000 characters.")]
    public string? Instructions { get; set; }

    public bool IsReadOnly { get; set; }

    public string? AssignedDate { get; private set; }

    public string? CompletedDate { get; private set; }

    public string? CreatedDate { get; private set; }

    /// <summary>
    /// How long ago the work order was created, such as "3 days ago". Null when there is no created date.
    /// </summary>
    public string? CreatedRelativeTime { get; private init; }

    /// <summary>
    /// Optional due date bound to the native date picker. Null when unset.
    /// </summary>
    public DateOnly? DueDateInput { get; set; }

    /// <summary>
    /// Due date loaded with the work order. An unchanged value is allowed even when it is before today.
    /// </summary>
    public DateOnly? OriginalDueDate { get; private init; }

    /// <summary>
    /// Church calendar day the form validates against. The system clock is used when unset.
    /// </summary>
    private DateOnly? ChurchToday { get; init; }

    /// <summary>
    /// Display text for the due date in the client culture's short-date pattern, when set.
    /// </summary>
    public string? DueDateDisplay { get; private set; }

    public string? RoomNumber { get; set; }

    /// <summary>
    /// The room number persisted on the work order when the model was filled.
    /// Used to skip entry-length validation when the room number is unchanged.
    /// </summary>
    public string? OriginalRoomNumber { get; set; }

    /// <summary>
    /// Priority chosen on the form. Defaults to Medium.
    /// </summary>
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    /// <summary>
    /// Optional building the work happens in.
    /// </summary>
    [StringLength(WorkOrder.BuildingMaxLength, ErrorMessage = "Building cannot exceed 100 characters.")]
    public string? Building { get; set; }

    /// <summary>
    /// Optional floor the work happens on.
    /// </summary>
    [StringLength(WorkOrder.FloorMaxLength, ErrorMessage = "Floor cannot exceed 20 characters.")]
    public string? Floor { get; set; }

    /// <summary>
    /// Reason typed when shelving an in-progress work order. At most 200 characters.
    /// </summary>
    public string? ShelveReason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        // Skip room number entry length validation when the value is unchanged
        if (RoomNumber != OriginalRoomNumber && RoomNumber?.Length > WorkOrder.RoomNumberEntryMaxLength)
        {
            yield return new ValidationResult(
                "Room number cannot exceed 50 characters.",
                [nameof(RoomNumber)]);
        }

        var today = ChurchToday ?? ChurchTimeZone.Today(TimeProvider.System);
        if (WorkOrder.IsNewlySetPastDueDate(DueDateInput, OriginalDueDate, today))
        {
            yield return new ValidationResult(
                WorkOrder.DueDateInThePastMessage,
                [nameof(DueDateInput)]);
        }
    }
}
