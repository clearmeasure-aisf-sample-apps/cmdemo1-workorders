// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderManageModel : IValidatableObject
{
    public WorkOrder? WorkOrder { get; set; }
    public EditMode Mode { get; set; }

    public string? WorkOrderNumber { get; set; }

    public string? Status { get; set; }

    public string? CreatorFullName { get; set; }

    public string? AssignedToUserName { get; set; }

    [Required, StringLength(WorkOrder.TitleMaxLength, ErrorMessage = "Title cannot exceed 300 characters.")]
    public string? Title { get; set; }

    [Required, StringLength(WorkOrder.DescriptionMaxLength, ErrorMessage = "Description cannot exceed 4000 characters.")]
    public string? Description { get; set; }

    [StringLength(WorkOrder.InstructionsMaxLength, ErrorMessage = "Instructions cannot exceed 4000 characters.")]
    public string? Instructions { get; set; }

    public bool IsReadOnly { get; set; }

    public string? AssignedDate { get; set; }

    public string? CompletedDate { get; set; }

    public string? CreatedDate { get; set; }

    /// <summary>
    /// How long ago the work order was created, such as "3 days ago". Null when there is no created date.
    /// </summary>
    public string? CreatedRelativeTime { get; init; }

    /// <summary>
    /// Optional due date bound to the native date picker. Null when unset.
    /// </summary>
    public DateOnly? DueDateInput { get; set; }

    /// <summary>
    /// Due date loaded with the work order. An unchanged value is allowed even when it is before today.
    /// </summary>
    public DateOnly? OriginalDueDate { get; init; }

    /// <summary>
    /// Church calendar day the form validates against. The system clock is used when unset.
    /// </summary>
    public DateOnly? ChurchToday { get; init; }

    /// <summary>
    /// Display text for the due date in the client culture's short-date pattern, when set.
    /// </summary>
    public string? DueDateDisplay { get; set; }

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
